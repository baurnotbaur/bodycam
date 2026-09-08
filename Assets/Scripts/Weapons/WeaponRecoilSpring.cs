using UnityEngine;

/// <summary>
/// Физическая система процедурной отдачи оружия на основе затухающих гармонических пружин (Damped Harmonic Oscillator).
/// Моделирует резкий импульсивный подброс ствола в плечо с упругой стабилизацией.
/// </summary>
public class WeaponRecoilSpring : MonoBehaviour
{
    [Header("Настройки пружины (Spring Physics)")]
    [Tooltip("Жесткость пружины позиции (Stiffness) — скорость возврата")]
    [SerializeField] private float posStiffness = 180f;

    [Tooltip("Коэффициент демпфирования позиции (Damping) — предотвращает вечное колебание")]
    [SerializeField] private float posDamping = 18f;

    [Tooltip("Жесткость пружины вращения")]
    [SerializeField] private float rotStiffness = 220f;

    [Tooltip("Демпфирование пружины вращения")]
    [SerializeField] private float rotDamping = 20f;

    [Header("Импульс отдачи одиночного выстрела")]
    [Tooltip("Линейный толчок (X = разброс вбок, Y = толчок вверх, Z = удар в плечо назад)")]
    [SerializeField] private Vector3 kickPosition = new Vector3(0.005f, 0.015f, -0.08f);

    [Tooltip("Угловой импульс (X = подброс ствола вверх, Y = увод в сторону, Z = крутящий момент)")]
    [SerializeField] private Vector3 kickRotation = new Vector3(-8f, 2f, 1.5f);

    [Header("Случайный разброс импульса")]
    [Tooltip("Случайная вариация увода ствола влево/вправо")]
    [SerializeField] private float randomYawFactor = 1.2f;

    // Внутренние переменные состояния физических осцилляторов
    private Vector3 currentPosOffset;
    private Vector3 targetPosOffset;
    private Vector3 posVelocity;

    private Vector3 currentRotOffset;
    private Vector3 targetRotOffset;
    private Vector3 rotVelocity;

    public Vector3 CurrentPosOffset => currentPosOffset;
    public Quaternion CurrentRotOffset => Quaternion.Euler(currentRotOffset);

    private void Update()
    {
        SimulateSprings(Time.deltaTime);
        ApplyOffsets();
    }

    private void SimulateSprings(float dt)
    {
        // 1. Пружина позиции: a = -k*(x - target) - c*v
        Vector3 posForce = -posStiffness * (currentPosOffset - targetPosOffset) - posDamping * posVelocity;
        posVelocity += posForce * dt;
        currentPosOffset += posVelocity * dt;

        // Постепенный возврат целевой точки к нулевому положению
        targetPosOffset = Vector3.Lerp(targetPosOffset, Vector3.zero, 1f - Mathf.Exp(-15f * dt));

        // 2. Пружина вращения
        Vector3 rotForce = -rotStiffness * (currentRotOffset - targetRotOffset) - rotDamping * rotVelocity;
        rotVelocity += rotForce * dt;
        currentRotOffset += rotVelocity * dt;

        targetRotOffset = Vector3.Lerp(targetRotOffset, Vector3.zero, 1f - Mathf.Exp(-15f * dt));
    }

    private void ApplyOffsets()
    {
        transform.localPosition = currentPosOffset;
        transform.localRotation = Quaternion.Euler(currentRotOffset);
    }

    /// <summary>
    /// Вызывается скриптом стрельбы при каждом выстреле.
    /// Передает мгновенный физический импульс в пружины.
    /// </summary>
    public void ApplyRecoilImpulse()
    {
        // Добавляем случайный фактор увода в горизонтальной плоскости (влево или вправо)
        float randYaw = Random.Range(-randomYawFactor, randomYawFactor);

        // Линейный импульс (удар назад и легкий толчок вверх)
        Vector3 linearImpulse = new Vector3(
            Random.Range(-kickPosition.x, kickPosition.x),
            kickPosition.y,
            kickPosition.z
        );

        // Угловой импульс (ствол задирается вверх, крутящий момент от нарезки ствола)
        Vector3 angularImpulse = new Vector3(
            kickRotation.x,
            kickRotation.y + randYaw,
            Random.Range(-kickRotation.z, kickRotation.z)
        );

        // Мгновенно передаем импульс в скорости пружин
        posVelocity += linearImpulse * 35f;
        rotVelocity += angularImpulse * 25f;

        // Добавляем смещение в целевую точку для создания пластической деформации отдачи
        targetPosOffset += linearImpulse * 0.3f;
        targetRotOffset += angularImpulse * 0.3f;
    }
}
