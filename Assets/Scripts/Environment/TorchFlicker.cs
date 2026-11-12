using UnityEngine;

/// <summary>
/// Meşale / ateş ışığını titretir. Point Light'a eklenir.
/// Rastgele zıplamak yerine Perlin gürültüsü kullanır: değer yumuşak ve doğal dalgalanır.
/// </summary>
[RequireComponent(typeof(Light))]
public class TorchFlicker : MonoBehaviour
{
    [SerializeField] private float baseIntensity = 3f;   // ortalama parlaklık
    [SerializeField] private float flickerAmount = 0.8f; // ne kadar titresin (+/-)
    [SerializeField] private float speed = 3f;           // titreme hızı
    [SerializeField] private float rangeFlicker = 0.5f;  // ışığın menzili de hafifçe oynasın

    private Light torchLight;
    private float baseRange;
    private float offset; // her meşale farklı titresin (hepsi aynı anda değil)

    private void Awake()
    {
        torchLight = GetComponent<Light>();
        baseRange = torchLight.range;
        offset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        // PerlinNoise 0..1 arası yumuşak bir değer döndürür, -1..1'e çeviriyoruz
        float n = Mathf.PerlinNoise(offset, Time.time * speed) * 2f - 1f;

        torchLight.intensity = baseIntensity + n * flickerAmount;
        torchLight.range = baseRange + n * rangeFlicker;
    }
}
