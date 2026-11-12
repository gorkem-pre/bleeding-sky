using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// FPS mouse ile bakış.
/// Sağa-sola (yaw): tüm Player gövdesi döner.
/// Yukarı-aşağı (pitch): sadece kamera döner.
/// Hassasiyet ayarlardan (kayıt dosyası) okunur. ESC artık PauseMenu'nün işi.
/// </summary>
public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;      // Player'ın child'ı olan kamera
    [SerializeField] private float minPitch = -85f;      // en fazla aşağı bakış açısı
    [SerializeField] private float maxPitch = 85f;       // en fazla yukarı bakış açısı

    [Header("Zıplama koruması")]
    [SerializeField] private int ignoreFramesAfterLock = 3;   // kilitlendikten sonra yok sayılacak frame sayısı
    [SerializeField] private float maxDeltaPerFrame = 150f;   // tek frame'de kabul edilen en büyük mouse hareketi (piksel)

    private float pitch;             // kameranın şu anki yukarı-aşağı açısı
    private int framesToIgnore;      // kalan yok sayılacak frame

    private void Start()
    {
        LockCursor(true);
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Duraklatılmışken hiçbir şey yapma (imleç menü için serbest)
        if (PauseMenu.IsPaused)
        {
            framesToIgnore = ignoreFramesAfterLock;
            return;
        }

        // İmleç bir şekilde serbest kaldıysa (alt-tab vb.) oyun penceresine tıklayınca tekrar kilitle
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            LockCursor(true);

        // Oyun penceresi odakta değilse ya da imleç serbestse kamera dönmesin
        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked)
        {
            framesToIgnore = ignoreFramesAfterLock; // geri dönünce ilk birkaç frame'i yut
            return;
        }

        // Bu frame'de mouse ne kadar hareket etti
        Vector2 rawDelta = mouse.delta.ReadValue();

        // Kilitlendikten hemen sonra gelen "biriken" hareketi yok say
        if (framesToIgnore > 0)
        {
            framesToIgnore--;
            return;
        }

        // Yine de anormal büyük bir sıçrama gelirse sınırla
        rawDelta = Vector2.ClampMagnitude(rawDelta, maxDeltaPerFrame);

        Vector2 delta = rawDelta * SaveSystem.Data.mouseSensitivity;

        // Sağa-sola: gövdeyi Y ekseni etrafında döndür
        transform.Rotate(0f, delta.x, 0f);

        // Yukarı-aşağı: açıyı biriktir, sınırla, sadece kameraya uygula
        pitch -= delta.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        if (locked) framesToIgnore = ignoreFramesAfterLock;
    }
}
