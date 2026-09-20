using UnityEngine;

namespace OneMoreBox
{
    public class BoxSpawner : MonoBehaviour
    {
        [Header("Spawn")]
        [SerializeField] private GameObject boxPrefab;
        [SerializeField, Min(0)] private int spawnCount = 10;

        [Header("Position Offset From Spawner")]
        [SerializeField] private Vector2 xRange = new Vector2(-5f, 5f);
        [SerializeField] private Vector2 zRange = new Vector2(-5f, 5f);

        [Header("Scale")]
        [SerializeField] private Vector2 xScaleRange = new Vector2(0.5f, 2f);
        [SerializeField] private Vector2 yScaleRange = new Vector2(0.5f, 2f);
        [SerializeField] private Vector2 zScaleRange = new Vector2(0.5f, 2f);
        [SerializeField, Min(0.01f)] private float scaleStep = 0.5f;

        private void Start()
        {
            if (!ValidateSettings())
            {
                return;
            }

            for (int i = 0; i < spawnCount; i++)
            {
                SpawnBox();
            }
        }

        private bool ValidateSettings()
        {
            if (boxPrefab == null)
            {
                Debug.LogError("BoxSpawner: 박스 프리팹을 지정하세요.", this);
                return false;
            }

            Renderer prefabRenderer = boxPrefab.GetComponentInChildren<Renderer>(true);
            if (prefabRenderer == null || prefabRenderer.sharedMaterial == null)
            {
                Debug.LogError("BoxSpawner: 박스 프리팹에 머티리얼이 있는 Renderer가 필요합니다.", this);
                return false;
            }

            Material material = prefabRenderer.sharedMaterial;
            if (!material.HasProperty("_BaseColor") && !material.HasProperty("_Color"))
            {
                Debug.LogError("BoxSpawner: 박스 머티리얼에 색상 속성이 필요합니다.", this);
                return false;
            }

            if (spawnCount < 0 ||
                !IsValidRange(xRange) ||
                !IsValidRange(zRange) ||
                !IsValidScaleRange(xScaleRange) ||
                !IsValidScaleRange(yScaleRange) ||
                !IsValidScaleRange(zScaleRange) ||
                scaleStep <= 0f)
            {
                Debug.LogError("BoxSpawner: 생성 개수, 위치 범위 또는 스케일 설정을 확인하세요.", this);
                return false;
            }

            return true;
        }

        private static bool IsValidRange(Vector2 range)
        {
            return range.x <= range.y;
        }

        private static bool IsValidScaleRange(Vector2 range)
        {
            return range.x > 0f && range.x <= range.y;
        }

        private void SpawnBox()
        {
            Vector3 position = transform.position + new Vector3(
                Random.Range(xRange.x, xRange.y),
                0f,
                Random.Range(zRange.x, zRange.y)
            );

            GameObject box = Instantiate(boxPrefab, position, boxPrefab.transform.rotation);

            box.transform.localScale = new Vector3(
                RandomScale(xScaleRange),
                RandomScale(yScaleRange),
                RandomScale(zScaleRange)
            );

            Renderer boxRenderer = box.GetComponentInChildren<Renderer>(true);

            // renderer.material은 이 박스 전용 머티리얼 인스턴스를 만듭니다.
            Material boxMaterial = boxRenderer.material;
            Color randomColor = new Color(
                Random.value,
                Random.value,
                Random.value,
                1f
            );

            if (boxMaterial.HasProperty("_BaseColor"))
            {
                boxMaterial.SetColor("_BaseColor", randomColor);
            }
            else
            {
                boxMaterial.SetColor("_Color", randomColor);
            }
        }

        private float RandomScale(Vector2 range)
        {
            int stepCount = Mathf.FloorToInt(
                (range.y - range.x) / scaleStep + 0.0001f
            );

            int selectedStep = Random.Range(0, stepCount + 1);
            return range.x + selectedStep * scaleStep;
        }
    }
}