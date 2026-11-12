using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Düşmanları dalgalar halinde doğurur.
/// Dalga başlar → düşmanlar spawn noktalarından birer birer çıkar → hepsi ölünce kısa mola → sonraki dalga.
/// Her dalgada düşman sayısı artar.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Düşman ve doğma noktaları")]
    [SerializeField] private EnemyAI enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnJitter = 1.5f;      // noktanın etrafında rastgele sapma (metre)

    [Header("Dalga ayarları")]
    [SerializeField] private float firstWaveDelay = 3f;     // oyun başında ilk dalgadan önce bekleme
    [SerializeField] private int firstWaveCount = 3;        // ilk dalgadaki düşman sayısı
    [SerializeField] private int extraPerWave = 2;          // her dalgada kaç düşman eklenir
    [SerializeField] private float spawnInterval = 1f;      // aynı dalgada iki düşman arası saniye
    [SerializeField] private float timeBetweenWaves = 5f;   // dalgalar arası mola

    public int CurrentWave { get; private set; }
    public int AliveCount { get; private set; }

    public event System.Action<int> OnWaveStarted;          // dalga numarası
    public event System.Action<int> OnWaveCleared;          // dalga numarası
    public event System.Action<int> OnAliveCountChanged;    // hayattaki düşman sayısı

    private Transform player;
    private Health playerHealth;

    // Prefab'ın oyun başındaki max canı (gizli). Sonradan bellekte prefab değiştirilirse
    // doğan iskeletler bununla karşılaştırılır (Rapor #02, F-06'daki prefab hilesi).
    private SecureFloat expectedEnemyMax;

    private void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<Health>();
        }

        expectedEnemyMax.Value = enemyPrefab.GetComponent<Health>().ConfiguredMax;
        StartCoroutine(RunWaves());
    }

    private void OnDestroy() => expectedEnemyMax.Release();

    private IEnumerator RunWaves()
    {
        yield return new WaitForSeconds(firstWaveDelay);

        // Oyuncu ölene kadar dalgalar devam eder
        while (playerHealth == null || !playerHealth.IsDead)
        {
            CurrentWave++;
            int count = firstWaveCount + (CurrentWave - 1) * extraPerWave;
            OnWaveStarted?.Invoke(CurrentWave);

            // Düşmanları birer birer doğur
            for (int i = 0; i < count; i++)
            {
                if (playerHealth != null && playerHealth.IsDead) yield break;
                SpawnEnemy();
                yield return new WaitForSeconds(spawnInterval);
            }

            // Hepsi ölene kadar bekle
            yield return new WaitUntil(() => AliveCount == 0);
            OnWaveCleared?.Invoke(CurrentWave);

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private void SpawnEnemy()
    {
        // Rastgele bir nokta seç, etrafında biraz kaydır (hepsi üst üste çıkmasın)
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector2 offset = Random.insideUnitCircle * spawnJitter;
        Vector3 position = point.position + new Vector3(offset.x, 0f, offset.y);

        // Konumu NavMesh'in üzerine oturt (duvarın içine ya da boşluğa doğmasın)
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            position = hit.position;

        // Oyuncuya dönük doğsun
        Quaternion rotation = point.rotation;
        if (player != null)
        {
            Vector3 toPlayer = player.position - position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f) rotation = Quaternion.LookRotation(toPlayer);
        }

        EnemyAI enemy = Instantiate(enemyPrefab, position, rotation);

        // Prefab kurcalanmış mı? (ör. CE ile maxHealth 75 → 1)
        Health enemyHealth = enemy.GetComponent<Health>();
        float expected = expectedEnemyMax.Value;
        if (!Mathf.Approximately(enemyHealth.ConfiguredMax, expected))
        {
            AntiTamper.Report();
            enemyHealth.ResetMax(expected);
        }

        // Hayattaki düşman sayısını takip et
        AliveCount++;
        OnAliveCountChanged?.Invoke(AliveCount);

        enemyHealth.OnDied += _ =>
        {
            AliveCount--;
            OnAliveCountChanged?.Invoke(AliveCount);
        };
    }
}
