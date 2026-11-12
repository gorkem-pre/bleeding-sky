using UnityEngine;

/// <summary>
/// Yerdeki coin: döner, hafifçe süzülür, oyuncu yaklaşınca ona doğru çekilir, değince toplanır.
/// Coin prefab'ına eklenir. Collider gerekmez, mesafe ile kontrol eder.
/// </summary>
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private float magnetRadius = 4f;     // bu mesafeye girince oyuncuya uçar
    [SerializeField] private float magnetSpeed = 12f;
    [SerializeField] private float collectRadius = 0.8f;  // bu mesafede toplanır
    [SerializeField] private float spinSpeed = 180f;      // derece/saniye
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float lifetime = 30f;        // toplanmazsa kaybolur

    private static Transform player;      // tüm coin'ler aynı oyuncuyu paylaşır
    private static CoinWallet wallet;

    private Vector3 basePos;
    private bool magnetized;
    private float bobOffset;

    /// <summary>Düşman ölünce çağrılır: coin'in değerini ve başlangıç konumunu ayarlar.</summary>
    public void Init(int coinValue) => value = coinValue;

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                wallet = p.GetComponent<CoinWallet>();
            }
        }

        basePos = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (player == null) return;

        // Oyuncunun gövde ortasına doğru (ayakları değil)
        Vector3 target = player.position + Vector3.up * 0.9f;
        float dist = Vector3.Distance(transform.position, target);

        if (dist < magnetRadius) magnetized = true;

        if (magnetized)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);
            if (dist < collectRadius) Collect();
        }
        else
        {
            transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * 3f + bobOffset) * bobHeight);
        }
    }

    private void Collect()
    {
        if (wallet != null) wallet.Add(value);
        Sfx.Play(Sfx.Lib.coin);
        Destroy(gameObject);
    }
}
