using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Butonun üzerine gelince ve tıklayınca ses çalar. MenuBuilder her butona otomatik ekler.</summary>
public class ButtonSounds : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private Selectable selectable;

    private void Awake() => selectable = GetComponent<Selectable>();

    private bool Active => selectable == null || selectable.interactable; // kilitli bölüm butonları ses çıkarmasın

    public void OnPointerEnter(PointerEventData e) { if (Active) Sfx.Play(Sfx.Lib.uiHover); }
    public void OnPointerClick(PointerEventData e) { if (Active) Sfx.Play(Sfx.Lib.uiClick); }
}
