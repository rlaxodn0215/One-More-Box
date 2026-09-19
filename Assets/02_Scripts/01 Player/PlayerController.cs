using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreBox
{
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        // 시점 회전 및 상호작용 Raycast의 시작점으로 사용할 카메라 Transform.
        [SerializeField] private Transform cameraTransform;
        // 상자를 든 상태에서 BoxController가 따라갈 기준점. 카메라 앞 거리만 조절한다.
        [SerializeField] private Transform holdPosition;

        [Header("Movement")]
        // 수평 이동 속도(초당 월드 단위).
        [SerializeField] private float moveSpeed;
        // 점프 정점까지의 목표 높이. 초기 수직 속도 계산에 사용한다.
        [SerializeField] private float jumpHeight;
        // 수직 속도에 매 프레임 더할 중력 가속도. 아래 방향이므로 음수여야 한다.
        [SerializeField] private float gravity;

        [Header("Look")]
        // 입력 장치의 시점 이동을 회전량으로 바꾸는 배율.
        [SerializeField] private float mouseSensitivity;
        // 카메라가 위아래로 회전할 수 있는 최대 각도.
        [SerializeField] private float maxLookAngle;

        [Header("Interaction")]
        // 카메라 정면에서 상호작용 대상을 찾을 수 있는 최대 거리.
        [SerializeField] private float interactDistance;
        // 상호작용 Raycast가 감지할 Collider 레이어.
        [SerializeField] private LayerMask interactLayer;

        [Header("Hold & Push Settings")]
        // CharacterController가 물리 오브젝트를 밀 때 가할 충격량.
        [SerializeField] private float pushPower;

        [Header("Box Manipulation")]
        // 스크롤로 조절 가능한 보유 거리의 하한.
        [SerializeField] private float minHoldDistance;
        // 스크롤로 조절 가능한 보유 거리의 상한.
        [SerializeField] private float maxHoldDistance;
        // 스크롤 입력 한 단계에 적용할 보유 거리 변화량.
        [SerializeField] private float scrollSensitivity;

        // 플레이어 이동과 충돌 처리를 담당하는 같은 GameObject의 CharacterController.
        private CharacterController controller;
        // Input System에서 받은 이동 입력. Update에서 플레이어 방향 기준으로 해석한다.
        private Vector2 moveInput;
        // Input System에서 받은 시점 입력. Update에서 본체 Y축/카메라 X축 회전으로 분리한다.
        private Vector2 lookInput;
        // 점프와 중력에 의해 누적되는 수직 이동 속도.
        private float verticalVelocity;
        // 카메라의 누적 상하 회전 각도. maxLookAngle 범위로 제한한다.
        private float cameraPitch;

        // 현재 시선 Raycast가 가리키는, 집을 수 있는 상자.
        private BoxController targetBox;
        // 현재 들고 있는 상자. null이면 상자를 들고 있지 않다.
        private BoxController heldBox;

        // 사용자가 스크롤로 선택한 상자 보유 거리.
        private float currentHoldDistance;

        #region Unity Events

        /// <summary>Unity 초기화 단계에서 CharacterController 참조를 준비한다.</summary>
        private void Awake()
        {
            // 매 프레임 탐색하지 않도록 CharacterController를 한 번 캐시한다.
            controller = GetComponent<CharacterController>();
        }

        /// <summary>게임 시작 시 FPS 조작을 위해 커서를 고정하고 숨긴다.</summary>
        private void Start()
        {
            // FPS 조작 중 커서가 화면 밖으로 나가지 않도록 고정하고 숨긴다.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>매 프레임 이동·시점 처리와 상자 보유/탐색 상태를 갱신한다.</summary>
        private void Update()
        {
            // 입력값을 사용한 이동과 시점 회전을 매 프레임 처리한다.
            Move();
            Look();

            // 보유 중에는 기준점 위치만 갱신하고 새로운 상자 탐색은 하지 않는다.
            if (heldBox != null)
            {
                UpdateHeldBoxPosition();
            }
            else
            {
                // 상자를 들고 있지 않을 때만 카메라 정면의 상호작용 대상을 찾는다.
                CheckInteraction();
            }
        }

        /// <summary>CharacterController가 Rigidbody에 닿았을 때 유효한 대상에 수평 푸시를 적용한다.</summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // CharacterController가 물리적으로 밀 수 있는 것은 동적인 Rigidbody뿐이다.
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic)
            {
                return;
            }
            if (heldBox != null && body.gameObject == heldBox.gameObject)
            {
                // 보유 중인 상자는 BoxController가 직접 추적하므로 중복으로 밀지 않는다.
                return;
            }
            if (hit.moveDirection.y < -0.3f)
            {
                // 위에서 밟는 상황에서는 수평 푸시를 적용하지 않는다.
                return;
            }

            // 수직 성분을 제거해 옆으로 움직일 때만 충격을 가한다.
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            body.AddForce(pushDir * pushPower, ForceMode.Impulse);
        }

        #endregion

        #region Input System Callbacks

        /// <summary>Move 입력 액션의 값을 받아 다음 Update에서 사용할 이동 방향을 저장한다.</summary>
        public void OnMove(InputValue value) 
        {
            // 실제 이동은 Update에서 처리하도록 최신 입력값만 저장한다.
            moveInput = value.Get<Vector2>(); 
        }

        /// <summary>Look 입력 액션의 값을 받아 다음 Update에서 사용할 시점 이동량을 저장한다.</summary>
        public void OnLook(InputValue value) 
        {
            // 실제 회전은 Update에서 처리하도록 최신 입력값만 저장한다.
            lookInput = value.Get<Vector2>(); 
        }

        /// <summary>Jump 입력을 처리하며 접지 상태일 때만 점프 초기 속도를 설정한다.</summary>
        public void OnJump(InputValue value)
        {
            // 지면에 있고 버튼을 누른 순간에만 점프해 공중 점프를 막는다.
            if (value.isPressed && controller.isGrounded)
            {
                // v² = 2gh로부터 목표 높이에 필요한 초기 위쪽 속도를 계산한다.
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            } 
        }

        /// <summary>Interact 입력으로 현재 상자를 집거나 들고 있는 상자를 내려놓는다.</summary>
        public void OnInteract(InputValue value)
        {
            // 버튼을 뗄 때는 처리하지 않아 입력 한 번당 동작이 한 번만 실행된다.
            if (!value.isPressed)
            {
                return;
            }

            if (heldBox != null)
            {
                // 이미 든 상자가 있으면 먼저 내려놓고 같은 입력으로 새 상자를 집지 않는다.
                heldBox.Drop();
                heldBox = null;
                return;
            }

            if (targetBox == null)
            {
                // 시선이 유효한 상자를 가리키지 않으면 상호작용할 수 없다.
                return;
            }

            // 상자에 보유 기준점을 전달해 Rigidbody가 해당 위치를 따라가게 한다.
            heldBox = targetBox;
            heldBox.PickUp(holdPosition);
            // 처음 집을 때는 일반 상호작용 거리에서 시작한다.
            currentHoldDistance = interactDistance;
        }

        /// <summary>ChangeDistance 입력으로 보유 중인 상자의 목표 거리를 조절한다.</summary>
        public void OnChangeDistance(InputValue value)
        {
            // 상자를 들고 있을 때만 스크롤 입력이 보유 거리를 바꾼다.
            if (heldBox == null)
            {
                return;
            }

            Vector2 scroll = value.Get<Vector2>();

            // 스크롤 방향에 따라 목표 보유 거리를 증감한다.
            if (scroll.y > 0)
            {
                currentHoldDistance += scrollSensitivity;
            }
            else if (scroll.y < 0)
            {
                currentHoldDistance -= scrollSensitivity;
            }

            // Inspector에서 정한 안전 범위 안으로 거리를 제한한다.
            currentHoldDistance = Mathf.Clamp(currentHoldDistance, minHoldDistance, maxHoldDistance);
        }

        #endregion

        #region Private Methods

        /// <summary>이동 입력과 중력을 CharacterController의 수평/수직 이동으로 적용한다.</summary>
        private void Move()
        {
            // 플레이어의 좌우/전후 축을 기준으로 입력을 이동 방향으로 변환한다.
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            // 대각선 이동이 직선 이동보다 빨라지지 않도록 크기를 제한한다.
            move = Vector3.ClampMagnitude(move, 1f);
            controller.Move(move * moveSpeed * Time.deltaTime);

            // 접지 상태에서 남아 있는 하강 속도를 작은 음수로 유지해 지면 판정을 안정화한다.
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            // 수평 이동과 별도로 점프 및 중력에 의한 수직 이동을 적용한다.
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        /// <summary>좌우 회전은 플레이어 본체에, 상하 회전은 카메라에 적용한다.</summary>
        private void Look()
        {
            // 플레이어 본체의 Y축 회전으로 좌우 시선을 처리한다.
            float mouseX = lookInput.x * mouseSensitivity;
            transform.Rotate(Vector3.up * mouseX);

            // 카메라 로컬 X축 회전으로 상하 시선을 처리한다.
            float mouseY = lookInput.y * mouseSensitivity;
            cameraPitch -= mouseY;

            // 카메라가 뒤집히지 않도록 상하 회전 범위를 제한한다.
            cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

            // 참조가 연결된 경우에만 카메라 회전을 적용한다.
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            } 
    }

        /// <summary>카메라 정면 Raycast로 현재 집을 수 있는 BoxController를 찾는다.</summary>
        private void CheckInteraction()
        {
            // 카메라가 없으면 시선 방향을 정의할 수 없어 탐색하지 않는다.
            if (cameraTransform == null)
            {
                return;
            }

            // 화면 중앙에서 바라보는 방향으로 상호작용 레이어만 검색한다.
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
            {
                // 맞은 Collider에 BoxController가 있을 때만 상호작용 대상으로 기록한다.
                BoxController box = hit.collider.GetComponent<BoxController>();
                targetBox = (box != null) ? box : null;
            }
            else
            {
                // Raycast가 빗나가면 이전 프레임의 대상 정보를 지운다.
                targetBox = null;
            } 
        }

        /// <summary>현재 보유 거리에 맞춰 카메라 아래의 보유 기준점 위치를 갱신한다.</summary>
        private void UpdateHeldBoxPosition()
        {
            // 보유 기준점이나 카메라가 없으면 현재 위치를 유지한다.
            if (cameraTransform == null || holdPosition == null)
            {
                return;
            }

            // 장애물 거리와 무관하게 선택한 거리로 보유 기준점을 배치한다.
            holdPosition.localPosition = new Vector3(0, 0, currentHoldDistance);
        }

        #endregion
    }
}
