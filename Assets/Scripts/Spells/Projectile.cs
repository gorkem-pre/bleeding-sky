using UnityEngine;

/// <summary>
/// Büyü mermisi. Fizik motoru yerine her frame önünü "yoklayarak" ilerler (SphereCast),
/// böylece hızlı giderken ince duvarların içinden geçmez.
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private float radius = 0.15f;          // merminin çarpışma yarıçapı
    [SerializeField] private GameObject impactEffect;       // çarpınca oluşacak efekt (opsiyonel)

    private Transform owner;  // atan kişi (kendine çarpmasın)
    private Team team;
    private float damage;
    private float speed;

    public void Init(Transform owner, Team team, float damage, float speed, float lifetime)
    {
        this.owner = owner;
        this.team = team;
        this.damage = damage;
        this.speed = speed;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime; // bu frame'de gideceği mesafe

        // Gideceği yol boyunca bir küre süpür: arada bir şey var mı?
        if (Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hit,
                               step, ~0, QueryTriggerInteraction.Ignore))
        {
            bool hitOwner = owner != null && hit.transform.IsChildOf(owner);
            if (!hitOwner)
            {
                Health health = hit.collider.GetComponentInParent<Health>();
                if (health != null && health.Team != team)
                    health.TakeDamage(damage);

                Impact(hit.point, hit.normal);
                return;
            }
        }

        transform.position += transform.forward * step;
    }

    private void Impact(Vector3 point, Vector3 normal)
    {
        if (impactEffect != null)
            Instantiate(impactEffect, point, Quaternion.LookRotation(normal));

        Sfx.PlayAt(Sfx.Lib.spellImpact, point); // 3D: çarptığı yerden gelsin

        Destroy(gameObject);
    }
}
