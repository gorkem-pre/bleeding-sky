using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Yakın dövüşçü düşman beyni:
///   Takip et (NavMesh) → menzile girince dur, dön, saldır → vurulunca sarsıl → ölünce yere yığıl.
/// Animasyonları kendisi oynatmaz, sadece Animator'a parametre gönderir (Speed, Attack, Hit, Die).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    private enum State { Rising, Chase, Attack, Dead }

    [Header("Hedef")]
    [SerializeField] private Transform target;            // boş bırakılırsa "Player" tag'li obje bulunur

    [Header("Hareket")]
    [SerializeField] private float chaseSpeed = 3.5f;     // m/s (Blend Tree'de 3.5 = koşma)
    [SerializeField] private float turnSpeed = 8f;

    [Header("Saldırı")]
    [SerializeField] private float attackRange = 1.8f;    // bu mesafeye girince saldırır
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackCooldown = 1.5f; // iki saldırı arası saniye
    [SerializeField] private float attackHitDelay = 0.45f;// animasyonda kılıcın isabet ettiği an
    [SerializeField] private float attackRecovery = 0.5f; // isabetten sonra animasyonun bitmesi

    [Header("Doğma (topraktan çıkma)")]
    [SerializeField] private bool riseOnSpawn = true;     // doğunca önce topraktan çıksın mı
    [SerializeField] private float riseDuration = 2f;     // spawn animasyonunun süresi (saniye)

    [Header("Hasar tepkisi ve ölüm")]
    [SerializeField] private float hitStunTime = 0.4f;    // vurulunca kaç saniye duraksar
    [SerializeField] private float corpseLifetime = 4f;   // ceset kaç saniye sonra silinir

    // Animator parametre isimlerini bir kez sayıya çeviriyoruz (her frame string karşılaştırmasın)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int RiseState = Animator.StringToHash("Rise"); // Animator'daki durumun adı

    private NavMeshAgent agent;
    private Animator animator;
    private Health health;
    private Health targetHealth;

    private State state = State.Chase;
    private float nextAttackTime;
    private float stunEndTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();

        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange * 0.8f;

        health.OnDamaged += HandleDamaged;
        health.OnDied += HandleDied;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target != null) targetHealth = target.GetComponent<Health>();

        if (riseOnSpawn) StartCoroutine(RiseRoutine());
    }

    /// <summary>Topraktan çıkış: animasyon bitene kadar hareket etmez, saldırmaz, sarsılmaz.</summary>
    private IEnumerator RiseRoutine()
    {
        state = State.Rising;
        agent.enabled = false;                    // çıkarken NavMesh onu yerinden oynatmasın
        animator.Play(RiseState, 0, 0f);          // "Rise" durumunu doğrudan, en baştan oynat
        Sfx.PlayAt(Sfx.Lib.enemyRise, transform.position);

        yield return new WaitForSeconds(riseDuration);

        agent.enabled = true;
        state = State.Chase;
    }

    private void OnDestroy()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnDied -= HandleDied;
    }

    private void Update()
    {
        if (state == State.Dead || state == State.Rising) return;

        // Gerçek yürüme hızını animasyona gönder (0.1 sn yumuşatmalı)
        animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);

        // Hedef yoksa ya da öldüyse bekle
        if (target == null || (targetHealth != null && targetHealth.IsDead))
        {
            agent.isStopped = true;
            return;
        }

        // Sarsılıyorsa yerinde kal
        if (Time.time < stunEndTime)
        {
            agent.isStopped = true;
            return;
        }

        // Saldırı animasyonu sürüyorsa sadece hedefe dönük kal
        if (state == State.Attack)
        {
            FaceTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= attackRange)
        {
            // Menzilde: dur, dön, cooldown dolduysa saldır
            agent.isStopped = true;
            FaceTarget();
            if (Time.time >= nextAttackTime) StartCoroutine(AttackRoutine());
        }
        else
        {
            // Menzil dışında: takip et
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
    }

    /// <summary>Saldırı: animasyonu başlat → isabet anını bekle → hâlâ yakınsa hasar ver → bitmesini bekle.</summary>
    private IEnumerator AttackRoutine()
    {
        state = State.Attack;
        nextAttackTime = Time.time + attackCooldown;
        animator.SetTrigger(AttackHash);

        // Kılıç sesi isabetten biraz önce: oyuncu "geliyor" diye duyup kaçabilsin
        yield return new WaitForSeconds(attackHitDelay * 0.5f);
        Sfx.PlayAt(Sfx.Lib.enemySwing, transform.position + Vector3.up);
        yield return new WaitForSeconds(attackHitDelay * 0.5f);

        // Oyuncu bu arada kaçtıysa kılıç boşa sallanır
        if (targetHealth != null &&
            Vector3.Distance(transform.position, target.position) <= attackRange + 0.5f)
        {
            targetHealth.TakeDamage(attackDamage);
        }

        yield return new WaitForSeconds(attackRecovery);
        state = State.Chase;
    }

    private void FaceTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // sadece yatayda dön, öne/arkaya eğilme
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    /// <summary>Sadece editörde: iskelet seçiliyken saldırı menzilini kırmızı çember olarak çizer.</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void HandleDamaged(float amount)
    {
        if (state != State.Dead) Sfx.PlayAt(Sfx.Lib.enemyHit, transform.position + Vector3.up);

        // Ölü ya da topraktan çıkarken: hasarı alır ama sarsılma animasyonu oynatmaz
        if (state == State.Dead || state == State.Rising) return;

        animator.SetTrigger(HitHash);
        stunEndTime = Time.time + hitStunTime;

        // Vurulmak saldırıyı böler: oyuncuya taktiksel bir avantaj
        if (state == State.Attack)
        {
            StopAllCoroutines();
            state = State.Chase;
        }
    }

    private void HandleDied(Health _)
    {
        state = State.Dead;
        StopAllCoroutines();
        Sfx.PlayAt(Sfx.Lib.enemyDeath, transform.position);

        // Topraktan çıkarken ölebilir; o an agent zaten kapalı olabilir
        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        animator.ResetTrigger(HitHash);
        animator.SetTrigger(DieHash);

        // Cesede mermi çarpmasın, oyuncu da takılmasın
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        Destroy(gameObject, corpseLifetime);
    }
}
