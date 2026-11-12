using TMPro;
using UnityEngine;

/// <summary>Sağ üstte coin simgesi + sayı. Coin gelince ikisi de kısa bir "zıplama" yapar.</summary>
public class CoinHUD : MonoBehaviour
{
    [SerializeField] private CoinWallet wallet;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private RectTransform icon;
    [SerializeField] private float punchScale = 1.35f;

    private void OnEnable()  => wallet.OnCoinsChanged += HandleCoins;
    private void OnDisable() => wallet.OnCoinsChanged -= HandleCoins;
    private void Start()     => coinText.text = wallet.RunCoins.ToString();

    private void Update()
    {
        float k = 10f * Time.deltaTime;
        icon.localScale = Vector3.Lerp(icon.localScale, Vector3.one, k);
        coinText.transform.localScale = Vector3.Lerp(coinText.transform.localScale, Vector3.one, k);
    }

    private void HandleCoins(int coins)
    {
        coinText.text = coins.ToString();
        icon.localScale = Vector3.one * punchScale;
        coinText.transform.localScale = Vector3.one * punchScale;
    }
}
