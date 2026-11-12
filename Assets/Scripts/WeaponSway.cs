using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Elde tutulan silahı (asa) canlandırır:
///  - Sway: Mouse ile dönünce asa hafifçe geride kalır, sonra yerine oturur.
///  - Bob : Yürürken asa adımlarla birlikte sekiz (∞) şeklinde sallanır.
/// WeaponHolder objesine eklenir (Main Camera'nın child'ı).
/// </summary>
public class WeaponSway : MonoBehaviour
{
    [Header("Sway (mouse ile dönerken)")]
    [SerializeField] private float swayAmount = 0.05f;   // mouse pikseli başına derece
    [SerializeField] private float maxSway = 6f;         // en fazla kaç derece sapabilir
    [SerializeField] private float swaySmooth = 8f;      // yerine oturma hızı

    [Header("Bob (yürürken)")]
    [SerializeField] private float bobFrequency = 8f;    // sallanma hızı
    [SerializeField] private float bobAmplitude = 0.03f; // sallanma genişliği (metre)
    [SerializeField] private float bobSmooth = 10f;

    private CharacterController controller;
    private Vector3 startPos;
    private Quaternion startRot;
    private float bobTimer;

    private void Awake()
    {
        // Player'daki CharacterController'ı bul (bu obje onun torunu)
        controller = GetComponentInParent<CharacterController>();

        // Inspector'da ayarladığın başlangıç pozisyonu = dinlenme pozisyonu
        startPos = transform.localPosition;
        startRot = transform.localRotation;
    }

    private void Update()
    {
        UpdateSway();
        UpdateBob();
    }

    private void UpdateSway()
    {
        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            mouseDelta = Mouse.current.delta.ReadValue();

        // Sağa dönünce asa sola kayar, yukarı bakınca aşağı kayar (atalet hissi)
        float pitch = Mathf.Clamp(mouseDelta.y * swayAmount, -maxSway, maxSway);
        float yaw   = Mathf.Clamp(-mouseDelta.x * swayAmount, -maxSway, maxSway);

        Quaternion target = startRot * Quaternion.Euler(pitch, yaw, 0f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, swaySmooth * Time.deltaTime);
    }

    private void UpdateBob()
    {
        Vector3 target = startPos;

        if (controller != null)
        {
            // Sadece yatay hız önemli (zıplarken/düşerken sallanmasın)
            Vector3 horizontal = controller.velocity;
            horizontal.y = 0f;
            float speed = horizontal.magnitude;

            if (controller.isGrounded && speed > 0.1f)
            {
                // Hızlı giderken daha hızlı sallansın (koşma)
                bobTimer += Time.deltaTime * bobFrequency * (speed / 5f);

                // Sekiz (∞) şekli: yatayda tek, dikeyde çift frekans
                target += new Vector3(
                    Mathf.Cos(bobTimer) * bobAmplitude,
                    Mathf.Sin(bobTimer * 2f) * bobAmplitude * 0.5f,
                    0f);
            }
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, target, bobSmooth * Time.deltaTime);
    }
}
