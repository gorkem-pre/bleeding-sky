using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sol tık ile seçili büyüyü atar. Player objesine eklenir.
/// Mermi asanın ucundan çıkar ama ekranın ortasındaki noktaya (crosshair) doğru gider.
/// </summary>
[RequireComponent(typeof(PlayerMana))]
public class SpellCaster : MonoBehaviour
{
    [SerializeField] private SpellData currentSpell;
    [SerializeField] private Transform castPoint;         // asanın ucundaki boş obje
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxAimDistance = 100f;

    public SpellData CurrentSpell => currentSpell;
    public event System.Action<SpellData> OnSpellCast;    // ses/efekt/animasyon için

    private PlayerMana mana;
    private float nextCastTime;

    private void Awake()
    {
        mana = GetComponent<PlayerMana>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || PauseMenu.IsPaused || Cursor.lockState != CursorLockMode.Locked) return;

        // Basılı tutunca cooldown'a göre art arda atar
        if (mouse.leftButton.isPressed) TryCast();
    }

    private void TryCast()
    {
        if (currentSpell == null || currentSpell.projectilePrefab == null) return;
        if (Time.time < nextCastTime) return;            // cooldown dolmadı
        if (!mana.TrySpend(currentSpell.manaCost)) return; // mana yetmiyor

        nextCastTime = Time.time + currentSpell.cooldown;

        // Asanın ucundan, nişan noktasına doğru yön
        Vector3 aimPoint = GetAimPoint();
        Vector3 direction = (aimPoint - castPoint.position).normalized;

        Projectile projectile = Instantiate(
            currentSpell.projectilePrefab,
            castPoint.position,
            Quaternion.LookRotation(direction));

        projectile.Init(transform, Team.Player, currentSpell.damage, currentSpell.speed, currentSpell.lifetime);
        // 2D: kendi büyümüz hep aynı yükseklikte duyulsun. İki ses üst üste = tek, daha zengin bir ses
        Sfx.Play(Sfx.Lib.spellCast);
        Sfx.Play(Sfx.Lib.spellCastLayer);

        OnSpellCast?.Invoke(currentSpell);
    }

    /// <summary>Ekranın tam ortasından ileri ışın gönderir, neye çarparsa orası hedeftir.</summary>
    private Vector3 GetAimPoint()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, ~0, QueryTriggerInteraction.Ignore)
            && !hit.transform.IsChildOf(transform))
        {
            return hit.point;
        }

        return ray.GetPoint(maxAimDistance); // boşluğa bakıyorsa uzak bir nokta
    }
}
