using UnityEngine;

/// <summary>
/// Düşman ölünce yere coin bırakır. Düşman prefab'ına (Health'in yanına) eklenir.
/// </summary>
[RequireComponent(typeof(Health))]
public class CoinDrop : MonoBehaviour
{
    [SerializeField] private CoinPickup coinPrefab;
    [SerializeField] private int minCoins = 1;
    [SerializeField] private int maxCoins = 3;
    [SerializeField] private int valuePerCoin = 1;
    [SerializeField] private float scatterRadius = 1f;   // coin'ler cesedin etrafına dağılsın
    [SerializeField] private float dropHeight = 0.6f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        health.OnDied -= HandleDied;
    }

    private void HandleDied(Health _)
    {
        if (coinPrefab == null) return;

        int count = Random.Range(minCoins, maxCoins + 1); // int Range'de üst sınır dahil değil
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            Vector3 pos = transform.position + new Vector3(offset.x, dropHeight, offset.y);
            CoinPickup coin = Instantiate(coinPrefab, pos, Quaternion.identity);
            coin.Init(valuePerCoin);
        }
    }
}
