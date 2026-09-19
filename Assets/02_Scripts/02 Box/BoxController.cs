using UnityEngine;

namespace OneMoreBox
{
    public class BoxController : MonoBehaviour
    {
        [Header("Hold Physics")]
        // 기준점과의 위치 차이를 따라갈 속도로 환산하는 강도.
        [SerializeField] private float holdFollowStrength;
        // 보유 중 상자가 낼 수 있는 최대 선형 속도. 큰 위치 오차에서 튀는 것을 줄인다.
        [SerializeField] private float maxHoldSpeed;

        // 상자의 이동과 회전을 물리적으로 처리하는 Rigidbody.
        private Rigidbody rb;
        // 상자가 따라갈 보유 기준점. null이면 보유 중이 아니다.
        private Transform holdTarget;
        // 집는 순간의 상자와 보유 기준점 사이 Y축 회전 차이.
        private Quaternion holdRotationOffset;

        #region Unity Events

        /// <summary>Unity 초기화 단계에서 상자의 Rigidbody 참조를 준비한다.</summary>
        private void Awake()
        {
            // 같은 GameObject의 Rigidbody를 한 번만 찾아 캐시한다.
            rb = GetComponent<Rigidbody>();
        }

        /// <summary>보유 중일 때 물리 프레임마다 상자의 위치와 Y축 회전을 보유 기준점에 맞춘다.</summary>
        private void FixedUpdate()
        {
            // 보유 중이 아닐 때는 일반 Rigidbody 물리 시뮬레이션에 맡긴다.
            if (holdTarget == null)
            {
                return;
            }

            // 기준점까지의 위치 오차를 목표 속도로 변환한다.
            Vector3 targetVelocity = (holdTarget.position - rb.position) * holdFollowStrength;
            // 최대 속도를 제한해 보유 이동을 안정화한다.
            rb.linearVelocity = Vector3.ClampMagnitude(targetVelocity, maxHoldSpeed);

            // Y축 시선 변화만 적용하면서 집기 전 상대 회전 차이는 유지한다.
            Quaternion targetRotation = GetHoldYawRotation() * holdRotationOffset;
            rb.MoveRotation(targetRotation);
        }

        #endregion

        #region Public Methods

        /// <summary>지정된 보유 기준점을 설정하고 상자를 보유 상태에 맞는 물리 설정으로 전환한다.</summary>
        public void PickUp(Transform holdPosition)
        {
            // 이미 보유 중이거나 기준점이 없으면 상태를 덮어쓰지 않는다.
            if (holdTarget != null || holdPosition == null)
            {
                return;
            }

            // 보유 중에는 중력으로 떨어지지 않게 하고 Rigidbody 물리 이동은 유지한다.
            rb.isKinematic = false;
            rb.useGravity = false;
            // 충돌과 관성에 의한 예기치 않은 회전을 막기 위한 회전 제약이다.
            rb.constraints |= RigidbodyConstraints.FreezeRotation;
            // 집기 직전의 운동량으로 상자가 튀지 않도록 속도를 초기화한다.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // 먼저 기준점을 저장해야 현재 Y축 회전값을 계산할 수 있다.
            holdTarget = holdPosition;

            // 집는 순간에는 상자 회전을 유지하고, 이후 기준점 회전 변화만 반영하기 위한 오프셋이다.
            Quaternion holdYawRotation = GetHoldYawRotation();
            holdRotationOffset = Quaternion.Inverse(holdYawRotation) * rb.rotation;
        }

        /// <summary>보유 기준점을 해제하고 상자의 중력과 회전 제약을 일반 상태로 되돌린다.</summary>
        public void Drop()
        {
            // 보유 중인 상자만 내려놓는다.
            if (holdTarget == null)
            {
                return;
            }

            // 기준점을 제거해 다음 FixedUpdate부터 강제 추적을 중지한다.
            holdTarget = null;
            // 집기 중 변경한 중력과 회전 제약을 해제한다.
            rb.useGravity = true;
            rb.constraints &= ~RigidbodyConstraints.FreezeRotation;
            // 내려놓는 순간 보유 중 속도가 남지 않게 초기화한다.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        #endregion

        #region Private Methods

        /// <summary>보유 기준점의 월드 회전에서 Y축 회전만 추출해 반환한다.</summary>
        private Quaternion GetHoldYawRotation()
        {
            // 카메라의 상하 회전은 제외하고 Y축 각도만 추출해 상자를 수평으로 회전시킨다.
            return Quaternion.Euler(0f, holdTarget.eulerAngles.y, 0f);
        }

        #endregion
    }
}
