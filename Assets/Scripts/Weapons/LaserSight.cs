using UnityEngine;

/// <summary>
/// Тактический лазерный целеуказатель (Tactical Laser Sight).
/// В шутерах в стиле Bodycam прицеливание часто осуществляется от бедра по лазерному лучу.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class LaserSight : MonoBehaviour
{
    [Header("Настройки луча")]
    [Tooltip("Максимальная дистанция луча (м)")]
    [SerializeField] private float maxDistance = 75f;

    [Tooltip("Маска слоев, с которыми сталкивается лазер")]
    [SerializeField] private LayerMask hitLayers = ~0;

    [Tooltip("Цвет лазера")]
    [SerializeField] private Color laserColor = new Color(0.1f, 1f, 0.2f, 0.8f);

    [Tooltip("Толщина лазерного луча")]
    [SerializeField] private float beamWidth = 0.006f;

    [Header("Точка попадания (Laser Dot)")]
    [Tooltip("Трансформ точки-маркера на поверхности")]
    [SerializeField] private Transform laserDotTransform;

    [Tooltip("Базовый размер точки попадания")]
    [SerializeField] private float dotBaseScale = 0.035f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        ConfigureLineRenderer();
    }

    private void ConfigureLineRenderer()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth * 1.5f;
        lineRenderer.useWorldSpace = true;

        // Создаем базовый шейдер/материал для лазера, если не назначен
        if (lineRenderer.material == null || lineRenderer.sharedMaterial.name.StartsWith("Default"))
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            
            Material mat = new Material(unlitShader);
            mat.color = laserColor;
            lineRenderer.material = mat;
        }

        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = laserColor;
    }

    private void OnDisable()
    {
        if (laserDotTransform != null)
        {
            laserDotTransform.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        UpdateLaser();
    }

    private void UpdateLaser()
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 endPoint;

        Ray ray = new Ray(origin, forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitLayers, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;

            if (laserDotTransform != null)
            {
                laserDotTransform.gameObject.SetActive(true);
                // Размещаем точку с легким отступом от поверхности по нормали во избежание Z-файтинга
                laserDotTransform.position = hit.point + hit.normal * 0.002f;
                laserDotTransform.rotation = Quaternion.LookRotation(-hit.normal);

                // Динамическое масштабирование точки в зависимости от дистанции
                float dist = hit.distance;
                laserDotTransform.localScale = Vector3.one * (dotBaseScale * Mathf.Clamp(dist * 0.15f, 0.6f, 2.5f));
            }
        }
        else
        {
            endPoint = origin + forward * maxDistance;

            if (laserDotTransform != null)
            {
                laserDotTransform.gameObject.SetActive(false);
            }
        }

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);
    }

    public void SetupDot(Transform dotTransform)
    {
        laserDotTransform = dotTransform;
    }
}
