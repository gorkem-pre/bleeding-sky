using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// FPS hareket: WASD yürüme, Shift koşma, Space zıplama, yerçekimi.
/// Fizik motoru yerine CharacterController kullanır (FPS'te daha kontrollü his verir).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private float walkSpeed = 5f;     // metre/saniye
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Zıplama ve yerçekimi")]
    [SerializeField] private float jumpHeight = 1.2f;  // metre
    [SerializeField] private float gravity = -20f;     // gerçek dünya -9.81, oyunlarda daha "tok" his için daha güçlü

    [Header("Ayak sesi")]
    [SerializeField] private float stepDistance = 2.2f;     // yürürken kaç metrede bir adım sesi
    [SerializeField] private float sprintStepDistance = 2.8f; // koşarken adımlar daha uzun
    [SerializeField] private float landSpeed = 6f;          // bu hızdan sert düşünce iniş sesi

    [Header("Güvenlik ağı")]
    [SerializeField] private float fallLimitY = -10f;  // bunun altına düşersen başlangıç noktasına dönersin

    private CharacterController controller;
    private float verticalVelocity; // dikey hız (zıplama / düşme)
    private Vector3 spawnPosition;
    private float stepProgress;     // son adımdan beri yürünen mesafe
    private bool wasGrounded = true;
    private float lastFallSpeed;    // havadayken ulaşılan en büyük düşüş hızı

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        spawnPosition = transform.position;
    }

    private void Update()
    {
        // Haritadan düştüysek başlangıç noktasına ışınla
        if (transform.position.y < fallLimitY)
        {
            Respawn();
            return;
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        // 1) Girdi: hangi tuşlar basılı
        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        Vector3 input = new Vector3(x, 0f, z);
        if (input.sqrMagnitude > 1f) input.Normalize(); // çapraz giderken hızlanmayı engelle

        // 2) Girdiyi oyuncunun baktığı yöne çevir
        Vector3 move = transform.right * input.x + transform.forward * input.z;
        float speed = kb.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;

        // 3) Yerçekimi ve zıplama
        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // yere hafifçe bastır ki isGrounded titremesin

        if (grounded && kb.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // istenen yüksekliğe çıkacak hız

        verticalVelocity += gravity * Time.deltaTime;

        // 4) Hepsini birleştir ve hareket ettir
        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // 5) Ayak sesleri
        Footsteps(speed > walkSpeed);
    }

    /// <summary>
    /// Zamana göre değil, yürünen mesafeye göre adım sesi: yavaş giderken seyrek, koşarken sık.
    /// Duvara yürüyünce (hareket yoksa) ses çıkmaz, çünkü gerçek hız (controller.velocity) kullanılıyor.
    /// </summary>
    private void Footsteps(bool sprinting)
    {
        bool grounded = controller.isGrounded;
        Vector3 v = controller.velocity;

        if (!grounded) lastFallSpeed = Mathf.Min(lastFallSpeed, v.y);

        // Yere iniş: sert düştüysek tok bir adım sesi
        if (grounded && !wasGrounded && lastFallSpeed < -landSpeed)
        {
            Sfx.Play(Sfx.Lib.footstep, 1.8f);
            stepProgress = 0f;
        }
        if (grounded) lastFallSpeed = 0f;
        wasGrounded = grounded;

        if (!grounded) return;

        float horizontal = new Vector2(v.x, v.z).magnitude;
        stepProgress += horizontal * Time.deltaTime;

        float distance = sprinting ? sprintStepDistance : stepDistance;
        if (stepProgress >= distance)
        {
            stepProgress = 0f;
            Sfx.Play(Sfx.Lib.footstep, sprinting ? 1.25f : 1f);
        }
    }

    private void Respawn()
    {
        // CharacterController açıkken transform.position'ı değiştirmek işe yaramaz,
        // kendi pozisyonunu geri yazar. O yüzden kapat → taşı → aç.
        controller.enabled = false;
        transform.position = spawnPosition;
        controller.enabled = true;
        verticalVelocity = 0f;
    }
}
