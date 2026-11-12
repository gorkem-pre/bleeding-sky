#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editör aracı: Tools → Arena → HUD Kur (Arena sahnesi açıkken)
/// Eski HUD canvas'ını siler, yenisini kurar ve PlayerHUD / WaveHUD / CoinHUD alanlarını bağlar.
/// Ayrıca Assets/UI içine şunları üretir (bir kere):
///   HUD_Text.mat  → siyah dış çizgi + gölge (normal yazılar)
///   HUD_Glow.mat  → kızıl parıltı (DALGA başlıkları)
///   HUD_Vignette.png → kenarları dolu, ortası boş daire (hasar efekti)
/// Ctrl+Z ile geri alınabilir.
/// </summary>
public static class HUDBuilder
{
    private const string Folder = "Assets/UI";

    private static readonly Color Gold     = new Color(1f, 0.82f, 0.55f, 1f);
    private static readonly Color CardCol  = new Color(0.06f, 0.02f, 0.03f, 0.72f);
    private static readonly Color BarBg    = new Color(0f, 0f, 0f, 0.65f);
    private static readonly Color GhostCol = new Color(1f, 0.88f, 0.75f, 0.9f);
    private static readonly Color HpCol    = new Color(0.82f, 0.08f, 0.10f, 1f);
    private static readonly Color MpCol    = new Color(0.20f, 0.60f, 1f, 1f);
    private static readonly Color MpLabel  = new Color(0.55f, 0.80f, 1f, 1f);

    private static Sprite rounded, circle;

    [MenuItem("Tools/Arena/HUD Kur (Arena sahnesi açıkken)")]
    private static void Build()
    {
        Selection.activeObject = null;

        // ---- Sahnedeki kaynakları bul
        Health health = Object.FindObjectsByType<Health>(FindObjectsSortMode.None).FirstOrDefault(h => h.Team == Team.Player);
        var mana   = Object.FindFirstObjectByType<PlayerMana>();
        var waves  = Object.FindFirstObjectByType<WaveManager>();
        var wallet = Object.FindFirstObjectByType<CoinWallet>();
        if (health == null || mana == null || waves == null || wallet == null)
        {
            EditorUtility.DisplayDialog("HUD Kur", "Arena sahnesi açık değil ya da Player / WaveManager / CoinWallet bulunamadı.", "Tamam");
            return;
        }

        RemoveOldHud();

        rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        circle  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        EnsureFolder();
        Material textMat = TextMaterial("HUD_Text", Color.black, 0.22f, new Color(0f, 0f, 0f, 0.8f), new Vector2(0.6f, -0.6f), 0f, 0.25f);
        Material glowMat = TextMaterial("HUD_Glow", new Color(0.15f, 0f, 0f, 1f), 0.18f, new Color(0.9f, 0.15f, 0.05f, 0.9f), Vector2.zero, 0.35f, 0.7f);
        Sprite vignette = VignetteSprite();

        // ---- Canvas
        var canvasGo = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0; // PauseCanvas (10) üstte kalsın
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        // GraphicRaycaster yok: HUD tıklamaları engellemesin
        Transform root = canvasGo.transform;

        // ---- Hasar vinyeti (en altta, tam ekran)
        RectTransform vig = NewRect("DamageVignette", root, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(vig);
        Image vigImg = AddImage(vig, vignette, new Color(0.75f, 0f, 0f, 0f), false);

        // ---- Nişangah
        RectTransform cross = NewRect("Crosshair", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 8));
        AddImage(cross, circle, new Color(1f, 1f, 1f, 0.9f), false).gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);

        // ---- Sol alt: can/mana kartı
        RectTransform card = NewRect("PlayerPanel", root, Vector2.zero, Vector2.zero, new Vector2(40, 40), new Vector2(480, 116));
        AddImage(card, rounded, CardCol, true);
        var cardOutline = card.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.3f);
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Label(card, "HealthLabel", "CAN", new Vector2(20, -22), Gold, textMat);
        Bar(card, "HealthBar", new Vector2(95, -22), new Vector2(365, 32), HpCol, true, textMat,
            out RectTransform hpFill, out RectTransform hpGhost, out Image hpImg, out TextMeshProUGUI hpText);

        Label(card, "ManaLabel", "MANA", new Vector2(20, -70), MpLabel, textMat);
        Bar(card, "ManaBar", new Vector2(95, -70), new Vector2(365, 24), MpCol, false, textMat,
            out RectTransform mpFill, out _, out _, out TextMeshProUGUI mpText);

        // ---- Üst orta: dalga
        RectTransform waveRt = NewRect("WaveText", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(700, 80));
        TextMeshProUGUI waveText = AddText(waveRt, "DALGA 1", 56, Gold, glowMat, TextAlignmentOptions.Center, FontStyles.Bold, 10f);

        RectTransform line = NewRect("Divider", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -102), new Vector2(280, 2));
        AddImage(line, null, new Color(Gold.r, Gold.g, Gold.b, 0.5f), false);

        RectTransform remRt = NewRect("RemainingText", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(700, 40));
        TextMeshProUGUI remText = AddText(remRt, "3 iskelet kaldı", 26, new Color(1f, 1f, 1f, 0.85f), textMat, TextAlignmentOptions.Center, FontStyles.Italic, 2f);

        // ---- Sağ üst: coin
        RectTransform coinIcon = NewRect("CoinIcon", root, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-62, -52), new Vector2(46, 46));
        AddImage(coinIcon, circle, new Color(1f, 0.78f, 0.25f, 1f), false).gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.15f, 0f, 0.9f);
        RectTransform inner = NewRect("Inner", coinIcon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28));
        AddImage(inner, circle, new Color(0.85f, 0.55f, 0.10f, 1f), false);

        RectTransform coinRt = NewRect("CoinText", root, Vector2.one, new Vector2(1f, 0.5f), new Vector2(-98, -52), new Vector2(260, 60));
        TextMeshProUGUI coinText = AddText(coinRt, "0", 46, Gold, textMat, TextAlignmentOptions.Right, FontStyles.Bold);

        // ---- Orta: duyuru
        RectTransform annRt = NewRect("AnnounceText", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(1500, 150));
        TextMeshProUGUI annText = AddText(annRt, "DALGA 1", 100, Gold, glowMat, TextAlignmentOptions.Center, FontStyles.Bold, 12f);
        annText.color = new Color(Gold.r, Gold.g, Gold.b, 0f);

        // ---- Bileşenler + bağlantılar
        var playerHud = canvasGo.AddComponent<PlayerHUD>();
        Set(playerHud, "playerHealth", health);
        Set(playerHud, "playerMana", mana);
        Set(playerHud, "healthFill", hpFill);
        Set(playerHud, "healthGhost", hpGhost);
        Set(playerHud, "healthFillImage", hpImg);
        Set(playerHud, "healthText", hpText);
        Set(playerHud, "manaFill", mpFill);
        Set(playerHud, "manaText", mpText);
        Set(playerHud, "damageVignette", vigImg);

        var waveHud = canvasGo.AddComponent<WaveHUD>();
        Set(waveHud, "waves", waves);
        Set(waveHud, "waveText", waveText);
        Set(waveHud, "remainingText", remText);
        Set(waveHud, "announceText", annText);

        var coinHud = canvasGo.AddComponent<CoinHUD>();
        Set(coinHud, "wallet", wallet);
        Set(coinHud, "coinText", coinText);
        Set(coinHud, "icon", coinIcon);

        Undo.RegisterCreatedObjectUndo(canvasGo, "HUD Kur");
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Selection.activeObject = canvasGo;
        Debug.Log("HUD kuruldu. Ctrl+S ile sahneyi kaydet.");
    }

    // ===================================================================== PARÇALAR

    private static void Label(RectTransform parent, string name, string text, Vector2 pos, Color color, Material mat)
    {
        RectTransform rt = NewRect(name, parent, new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(75, 32));
        AddText(rt, text, 20, color, mat, TextAlignmentOptions.Left, FontStyles.Bold, 4f);
    }

    /// <summary>Arka plan + (isteğe bağlı) beyaz iz + dolgu + parlak şerit + değer yazısı.</summary>
    private static void Bar(RectTransform parent, string name, Vector2 pos, Vector2 size, Color fillColor, bool withGhost, Material mat,
                            out RectTransform fill, out RectTransform ghost, out Image fillImg, out TextMeshProUGUI valueText)
    {
        RectTransform bg = NewRect(name, parent, new Vector2(0, 1), new Vector2(0, 1), pos, size);
        AddImage(bg, rounded, BarBg, true);

        ghost = null;
        if (withGhost)
        {
            ghost = NewRect("Ghost", bg, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(ghost);
            AddImage(ghost, rounded, GhostCol, true);
        }

        fill = NewRect("Fill", bg, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(fill);
        fillImg = AddImage(fill, rounded, fillColor, true);

        // Üst yarıda hafif beyaz şerit → cam/parlak görünüm
        RectTransform gloss = NewRect("Gloss", fill, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        gloss.anchorMin = new Vector2(0f, 0.55f);
        gloss.anchorMax = new Vector2(1f, 0.92f);
        gloss.offsetMin = new Vector2(4f, 0f);
        gloss.offsetMax = new Vector2(-4f, 0f);
        AddImage(gloss, null, new Color(1f, 1f, 1f, 0.16f), false);

        RectTransform txt = NewRect("Value", bg, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(txt);
        valueText = AddText(txt, "100 / 100", size.y * 0.6f, Color.white, mat, TextAlignmentOptions.Center, FontStyles.Bold);
    }

    // ===================================================================== YARDIMCILAR

    private static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Image AddImage(RectTransform rt, Sprite sprite, Color color, bool sliced)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI AddText(RectTransform rt, string text, float size, Color color, Material mat,
                                           TextAlignmentOptions align, FontStyles style, float spacing = 0f)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.characterSpacing = spacing;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        if (mat != null) t.fontSharedMaterial = mat;
        return t;
    }

    private static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"{target.GetType().Name}: '{field}' alanı yok"); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Eski HUD bileşenlerinin bulunduğu canvas'ları siler (PauseCanvas hariç).</summary>
    private static void RemoveOldHud()
    {
        var roots = new HashSet<GameObject>();
        IEnumerable<Component> comps = Object.FindObjectsByType<PlayerHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>()
            .Concat(Object.FindObjectsByType<WaveHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            .Concat(Object.FindObjectsByType<CoinHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        foreach (Component c in comps)
        {
            Canvas cv = c.GetComponentInParent<Canvas>(true);
            roots.Add(cv != null ? cv.rootCanvas.gameObject : c.gameObject);
        }
        GameObject old = GameObject.Find("HUDCanvas");
        if (old != null) roots.Add(old);

        foreach (GameObject go in roots)
        {
            if (go.name == "PauseCanvas") continue;
            Debug.Log($"Eski HUD silindi: {go.name}");
            Undo.DestroyObjectImmediate(go);
        }
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "UI");
    }

    /// <summary>TMP yazı materyali: dış çizgi + alt katman (gölge ya da parıltı). Varsa günceller.</summary>
    private static Material TextMaterial(string name, Color outline, float outlineWidth, Color underlay, Vector2 offset, float dilate, float softness)
    {
        string path = $"{Folder}/{name}.mat";
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(font.material); // fontun atlas'ını taşıyan kopya
            AssetDatabase.CreateAsset(mat, path);
        }
        Shader sdf = Shader.Find("TextMeshPro/Distance Field");
        if (sdf != null) mat.shader = sdf;

        mat.SetFloat("_OutlineWidth", outlineWidth);
        mat.SetColor("_OutlineColor", outline);
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", underlay);
        mat.SetFloat("_UnderlayOffsetX", offset.x);
        mat.SetFloat("_UnderlayOffsetY", offset.y);
        mat.SetFloat("_UnderlayDilate", dilate);
        mat.SetFloat("_UnderlaySoftness", softness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>Ortası saydam, kenarları beyaz bir doku üretir; renk Image'dan gelir (kırmızı).</summary>
    private static Sprite VignetteSprite()
    {
        string path = $"{Folder}/HUD_Vignette.png";
        if (!File.Exists(path))
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f;
                float v = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);                         // merkezde 0, köşede ~1.41
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.3f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
