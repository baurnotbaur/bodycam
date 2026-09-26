using UnityEngine;

/// <summary>
/// Тактическая силуэтная мишень для тренировок CQB (Killhouse Silhouette).
/// Реализует зоны попадания (голова / грудь / корпус) и физическую реакцию на пули.
/// </summary>
public class ShootingTarget : MonoBehaviour
{
    public enum HitZone { Head, Chest, Body }

    [Header("Зона попадания этой секции")]
    [SerializeField] private HitZone zone = HitZone.Chest;

    [Header("Параметры здоровья")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private Renderer rend;
    private Color originalColor;
    private float flashTimer;

    private void Awake()
    {
        currentHealth = maxHealth;
        rend = GetComponent<Renderer>();
        if (rend != null && rend.material != null)
        {
            originalColor = rend.material.color;
        }
    }

    private void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && rend != null)
            {
                rend.material.color = originalColor;
            }
        }
    }

    public void ApplyDamage(float damage)
    {
        float multiplier = zone == HitZone.Head ? 2.5f : (zone == HitZone.Chest ? 1.2f : 1.0f);
        currentHealth -= damage * multiplier;

        // Вспышка белым/желтым цветом при попадании для визуального подтверждения
        if (rend != null)
        {
            rend.material.color = (zone == HitZone.Head) ? Color.red : Color.yellow;
            flashTimer = 0.08f;
        }

        if (currentHealth <= 0f)
        {
            // Сброс здоровья для непрерывной тренировки
            currentHealth = maxHealth;
        }
    }
}
