using UnityEngine;

/// <summary>
/// Yürürken kameranın adımlarla birlikte hafifçe inip kalkması.
/// "Kayıyormuş / uçuyormuş" hissini azaltır, ağırlık ve adım hissi verir.
/// Main Camera'ya eklenir. (PlayerLook kameranın açısını, bu script pozisyonunu değiştirir; çakışmazlar.)
/// </summary>
public class HeadBob : MonoBehaviour
{
    [SerializeField] private float frequency = 10f;    // adım hızı
    [SerializeField] private float amplitude = 0.05f;  // inip kalkma miktarı (metre)
    [SerializeField] private float smooth = 10f;

    private CharacterController controller;
    private Vector3 startPos;
    private float timer;

    private void Awake()
    {
        controller = GetComponentInParent<CharacterController>();
        startPos = transform.localPosition;
    }

    private void Update()
    {
        Vector3 target = startPos;

        Vector3 horizontal = controller.velocity;
        horizontal.y = 0f;
        float speed = horizontal.magnitude;

        if (controller.isGrounded && speed > 0.1f)
        {
            timer += Time.deltaTime * frequency * (speed / 5f);

            // Dikeyde her adımda bir iniş (Abs ile iki tepe = iki adım),
            // yatayda hafif sağ-sol salınım
            target += new Vector3(
                Mathf.Sin(timer) * amplitude * 0.5f,
                -Mathf.Abs(Mathf.Cos(timer)) * amplitude,
                0f);
        }
        else
        {
            timer = 0f;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, target, smooth * Time.deltaTime);
    }
}
