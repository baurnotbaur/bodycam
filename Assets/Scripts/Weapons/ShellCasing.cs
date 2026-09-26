using UnityEngine;

/// <summary>
/// Физическая гильза, выбрасываемая из окна экстракции оружия при каждом выстреле.
/// Имеет массу, импульс выброса, крутящий момент (спин) и воспроизводит
/// металлический звон при соударении с полом и стенами полигона.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShellCasing : MonoBehaviour
{
    [Header("Физика и время жизни")]
    [Tooltip("Время жизни гильзы в секундах до удаления")]
    [SerializeField] private float lifetime = 7.0f;

    [Tooltip("Максимальное количество воспроизведений звука удара")]
    [SerializeField] private int maxBounceSounds = 3;

    private Rigidbody rb;
    private int currentBounceCount;
    private float spawnTime;
    private bool isFrozen;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;
    }

    /// <summary>
    /// Инициализация выброса гильзы с линейным импульсом и случайным крутящим моментом.
    /// </summary>
    public void Eject(Vector3 linearImpulse, Vector3 torqueImpulse)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.linearVelocity = linearImpulse;
        rb.angularVelocity = torqueImpulse;
    }

    private void Update()
    {
        float age = Time.time - spawnTime;

        // Если гильза успокоилась через 2.5 секунды, замораживаем Rigidbody для оптимизации PhysX
        if (!isFrozen && age > 2.5f && rb != null && !rb.isKinematic)
        {
            if (rb.linearVelocity.sqrMagnitude < 0.02f && rb.angularVelocity.sqrMagnitude < 0.02f)
            {
                rb.isKinematic = true;
                isFrozen = true;
            }
        }

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentBounceCount < maxBounceSounds)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed > 0.4f)
            {
                Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                BodycamAudioEngine.PlayCasingClink(contactPoint, impactSpeed);
                currentBounceCount++;
            }
        }
    }
}
