#if UNITY_EDITOR
/// <summary>
/// İşaret arayüzü: bu arayüzü taşıyan bileşenler yalnızca editörde kullanılan araçlardır
/// (GraveyardBuilder, BoundaryScatter, CoverScatter). Build sırasında
/// StripEditorOnlyComponents bunları sahneden siler (Rapor #02, F-09).
/// </summary>
public interface IEditorOnlyComponent { }
#endif
