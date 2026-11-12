#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Editör aracı: Menüleri tek tıkla kurar ve tüm bağlantıları (alanlar + buton OnClick'leri) yapar.
/// Menü: Tools → Arena → ...
///   - "Ana Menüyü Kur"          → MainMenu sahnesi açıkken
///   - "Oyun İçi Menüleri Kur"   → Arena sahnesi açıkken (duraklatma, ayarlar, ölüm ekranı)
/// Eski elle kurulmuş panelleri ve bileşenleri siler, yerine yenilerini koyar. Ctrl+Z ile geri alınabilir.
/// </summary>
public static class MenuBuilder
{
    // ---- Renkler (kızıl ay temasına uygun) ----
    private static readonly Color Overlay = new Color(0f, 0f, 0f, 0.75f);
    private static readonly Color CardColor = new Color(0.10f, 0.04f, 0.05f, 0.95f);
    private static readonly Color Btn = new Color(0.35f, 0.08f, 0.09f, 1f);
    private static readonly Color BtnHi = new Color(0.55f, 0.13f, 0.12f, 1f);
    private static readonly Color BtnPress = new Color(0.22f, 0.05f, 0.05f, 1f);
    private static readonly Color Gold = new Color(1f, 0.82f, 0.55f, 1f);
    private static readonly Color Faded = new Color(1f, 1f, 1f, 0.6f);

    // ===================================================================== ANA MENÜ
    [MenuItem("Tools/Arena/Ana Menüyü Kur (MainMenu sahnesi açıkken)")]
    private static void BuildMainMenu()
    {
        Selection.activeObject = null; // Inspector silinecek bir objeyi göstermesin
        RemoveComponents<MainMenu>();
        RemoveComponents<SettingsMenu>();
        RemoveByName("MenuCanvas", "Canvas", "MainPanel", "LevelPanel", "SettingsPanel");
        EnsureEventSystem();

        Canvas canvas = CreateCanvas("MenuCanvas", 0);
        var mainMenu = canvas.gameObject.AddComponent<MainMenu>();
        var settings = canvas.gameObject.AddComponent<SettingsMenu>();

        CreatePanel(canvas.transform, "Background", new Color(0.08f, 0.02f, 0.03f, 1f));

        GameObject main = CreatePanel(canvas.transform, "MainPanel", new Color(0f, 0f, 0f, 0f));
        CreateText(main.transform, "Title", "BLEEDING SKY", 110, new Vector2(0, 280), new Vector2(1600, 180), Gold, FontStyles.Bold);
        CreateText(main.transform, "Subtitle", "Kızıl ayın altında hayatta kal", 32, new Vector2(0, 180), new Vector2(1200, 60), Faded, FontStyles.Italic);
        CreateButton(main.transform, "PlayButton", "Oyna", new Vector2(0, 40), mainMenu.OpenLevelSelect);
        CreateButton(main.transform, "SettingsButton", "Ayarlar", new Vector2(0, -60), mainMenu.OpenSettings);
        CreateButton(main.transform, "QuitButton", "Çıkış", new Vector2(0, -160), mainMenu.Quit);
        TextMeshProUGUI stats = CreateText(main.transform, "StatsText", "", 30, new Vector2(0, -330), new Vector2(1200, 60), Gold, FontStyles.Normal);
        CreateText(main.transform, "VersionText", "v0.2", 24, new Vector2(800, -500), new Vector2(260, 40), Faded, FontStyles.Normal, TextAlignmentOptions.Right);

        GameObject levelPanel = BuildLevelPanel(canvas.transform, mainMenu);
        GameObject settingsPanel = BuildSettingsPanel(canvas.transform, settings);

        SetString(mainMenu, "gameScene", "Arena");
        SetRef(mainMenu, "mainPanel", main);
        SetRef(mainMenu, "levelPanel", levelPanel);
        SetRef(mainMenu, "settings", settings);
        SetRef(mainMenu, "statsText", stats);

        levelPanel.SetActive(false);
        settingsPanel.SetActive(false);
        Finish(canvas.gameObject, "Ana menü kuruldu.");
    }

    // ===================================================================== OYUN İÇİ
    [MenuItem("Tools/Arena/Oyun İçi Menüleri Kur (Arena sahnesi açıkken)")]
    private static void BuildGameMenus()
    {
        Selection.activeObject = null; // Inspector silinecek bir objeyi göstermesin
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Menu Builder", "Sahnede 'Player' etiketli obje bulunamadı. Arena sahnesi açık mı?", "Tamam");
            return;
        }

        RemoveComponents<PauseMenu>();
        RemoveComponents<SettingsMenu>();
        RemoveByName("PauseCanvas", "PausePanel", "SettingsPanel", "DeathPanel");
        EnsureEventSystem();

        Canvas canvas = CreateCanvas("PauseCanvas", 10); // HUD'un üstünde çizilsin
        var pause = canvas.gameObject.AddComponent<PauseMenu>();
        var settings = canvas.gameObject.AddComponent<SettingsMenu>();
        var death = player.GetComponent<PlayerDeath>();
        var recorder = Object.FindFirstObjectByType<RunRecorder>();

        // --- Duraklatma ---
        GameObject pausePanel = CreatePanel(canvas.transform, "PausePanel", Overlay);
        Transform pauseCard = CreateCard(pausePanel.transform, new Vector2(620, 540));
        CreateText(pauseCard, "Title", "DURAKLATILDI", 60, new Vector2(0, 190), new Vector2(580, 90), Gold, FontStyles.Bold);
        CreateButton(pauseCard, "ResumeButton", "Devam", new Vector2(0, 60), pause.Resume);
        CreateButton(pauseCard, "SettingsButton", "Ayarlar", new Vector2(0, -40), pause.OpenSettings);
        CreateButton(pauseCard, "MenuButton", "Ana Menü", new Vector2(0, -140), pause.GoToMainMenu);

        // --- Ayarlar ---
        GameObject settingsPanel = BuildSettingsPanel(canvas.transform, settings);

        // --- Ölüm ekranı ---
        GameObject deathPanel = CreatePanel(canvas.transform, "DeathPanel", new Color(0.25f, 0f, 0f, 0.75f));
        CreateText(deathPanel.transform, "DeathTitle", "ÖLDÜN", 150, new Vector2(0, 200), new Vector2(1200, 220), new Color(1f, 0.35f, 0.35f), FontStyles.Bold);
        TextMeshProUGUI deathStats = CreateText(deathPanel.transform, "DeathStats", "", 40, new Vector2(0, 60), new Vector2(1200, 70), Gold, FontStyles.Normal);
        if (death != null)
        {
            CreateButton(deathPanel.transform, "RetryButton", "Tekrar Dene", new Vector2(0, -60), death.Retry);
            CreateButton(deathPanel.transform, "MenuButton", "Ana Menü", new Vector2(0, -160), death.GoToMainMenu);
        }
        CreateText(deathPanel.transform, "RetryHint", "R tuşu ile de tekrar deneyebilirsin", 26, new Vector2(0, -260), new Vector2(1200, 50), Faded, FontStyles.Italic);

        // --- Bağlantılar ---
        SetRef(pause, "pausePanel", pausePanel);
        SetRef(pause, "settings", settings);
        SetRef(pause, "playerHealth", player.GetComponent<Health>());
        SetRef(pause, "recorder", recorder);
        SetString(pause, "menuScene", "MainMenu");

        if (death != null)
        {
            SetRef(death, "deathPanel", deathPanel);
            SetRef(death, "statsText", deathStats);
            SetRef(death, "recorder", recorder);
            SetString(death, "menuScene", "MainMenu");
        }
        else Debug.LogWarning("Menu Builder: Player'da PlayerDeath bileşeni yok, ölüm ekranı bağlanmadı.");

        if (recorder == null) Debug.LogWarning("Menu Builder: Sahnede RunRecorder bulunamadı (WaveManager'a ekli olmalı).");

        // Editörde göz kalabalığı olmasın: hepsi kapalı başlasın (script'ler zaten yönetiyor)
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        deathPanel.SetActive(false);

        Finish(canvas.gameObject, "Oyun içi menüler kuruldu.");
    }

    // ===================================================================== BÖLÜM SEÇME
    private static GameObject BuildLevelPanel(Transform parent, MainMenu mainMenu)
    {
        GameObject panel = CreatePanel(parent, "LevelPanel", new Color(0f, 0f, 0f, 0f));
        CreateText(panel.transform, "Title", "BLEEDING SKY", 110, new Vector2(0, 280), new Vector2(1600, 180), Gold, FontStyles.Bold);
        CreateText(panel.transform, "Subtitle", "Bölüm seç", 36, new Vector2(0, 170), new Vector2(1200, 60), Faded, FontStyles.Italic);

        const int levels = 5;
        const float spacing = 240f;
        float startX = -spacing * (levels - 1) / 2f;
        var circle = new Vector2(170, 170);

        for (int i = 0; i < levels; i++)
        {
            float x = startX + spacing * i;
            bool unlocked = i == 0;

            if (unlocked)
            {
                CreateButton(panel.transform, "Level1Button", "DEMO", new Vector2(x, 0), mainMenu.Play, circle, true, 34);
                CreateText(panel.transform, "Level1Name", "Mezarlık", 28, new Vector2(x, -120), new Vector2(220, 40), Gold, FontStyles.Normal);
            }
            else
            {
                // Kilitli: tıklanamaz, soluk, üstünde X
                Button b = CreateButton(panel.transform, $"Level{i + 1}Button", "X", new Vector2(x, 0), null, circle, true, 70);
                b.interactable = false;
                ColorBlock cb = b.colors;
                cb.disabledColor = new Color(0.18f, 0.14f, 0.15f, 1f);
                b.colors = cb;
                b.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 1f, 1f, 0.25f);
                CreateText(panel.transform, $"Level{i + 1}Name", "Yakında", 26, new Vector2(x, -120), new Vector2(220, 40), new Color(1f, 1f, 1f, 0.35f), FontStyles.Italic);
            }
        }

        CreateButton(panel.transform, "BackButton", "Geri", new Vector2(0, -300), mainMenu.CloseLevelSelect);
        return panel;
    }

    // ===================================================================== AYARLAR PANELİ (ortak)
    private static GameObject BuildSettingsPanel(Transform parent, SettingsMenu settings)
    {
        GameObject panel = CreatePanel(parent, "SettingsPanel", Overlay);
        Transform card = CreateCard(panel.transform, new Vector2(760, 600));

        CreateText(card, "Title", "AYARLAR", 60, new Vector2(0, 220), new Vector2(700, 90), Gold, FontStyles.Bold);

        CreateText(card, "SensitivityLabel", "Fare hassasiyeti", 30, new Vector2(-190, 100), new Vector2(300, 50), Color.white, FontStyles.Normal, TextAlignmentOptions.Right);
        Slider sens = CreateSlider(card, "SensitivitySlider", new Vector2(150, 100), 0.02f, 0.3f, 0.1f);

        CreateText(card, "VolumeLabel", "Ses", 30, new Vector2(-190, 10), new Vector2(300, 50), Color.white, FontStyles.Normal, TextAlignmentOptions.Right);
        Slider vol = CreateSlider(card, "VolumeSlider", new Vector2(150, 10), 0f, 1f, 1f);

        Toggle full = CreateToggle(card, "FullscreenToggle", "Tam ekran", new Vector2(0, -90));

        CreateButton(card, "BackButton", "Geri", new Vector2(0, -210), settings.Close);

        SetRef(settings, "panel", panel);
        SetRef(settings, "sensitivitySlider", sens);
        SetRef(settings, "volumeSlider", vol);
        SetRef(settings, "fullscreenToggle", full);
        return panel;
    }

    // ===================================================================== YAPI TAŞLARI
    private static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Menu Builder");

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    /// <summary>Ekranı kaplayan panel.</summary>
    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0f; // görünür paneller arkadaki tıklamaları engellesin
        return go;
    }

    /// <summary>Ortada çerçeveli kutu.</summary>
    private static Transform CreateCard(Transform parent, Vector2 size)
    {
        var go = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(Outline));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, Vector2.zero, size);
        go.GetComponent<Image>().color = CardColor;

        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
        outline.effectDistance = new Vector2(2f, -2f);
        return go.transform;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Vector2 pos, Vector2 box,
                                              Color color, FontStyles style, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, pos, box);

        var t = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap; // "DURA / KLATI / LDI" gibi bölünmesin
        t.raycastTarget = false;
        return t;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, UnityAction onClick,
                                       Vector2? size = null, bool round = false, float fontSize = 38)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, pos, size ?? new Vector2(400, 80));

        var img = go.GetComponent<Image>();
        if (round)
        {
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"); // Unity'nin hazır daire görseli
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }
        else
        {
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
        }
        img.color = Color.white; // renk butonun ColorBlock'undan gelir

        var btn = go.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Btn;
        cb.highlightedColor = BtnHi;
        cb.selectedColor = Btn;
        cb.pressedColor = BtnPress;
        cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        TextMeshProUGUI txt = CreateText(go.transform, "Text", label, fontSize, Vector2.zero, Vector2.zero, Color.white, FontStyles.Bold);
        var trt = (RectTransform)txt.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        // Inspector'daki "On Click ()" listesine kalıcı olarak ekle (elle sürüklemekle aynı şey)
        if (onClick != null) UnityEventTools.AddPersistentListener(btn.onClick, onClick);
        go.AddComponent<ButtonSounds>(); // üzerine gelme + tıklama sesi
        return btn;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 pos, float min, float max, float value)
    {
        GameObject go = DefaultControls.CreateSlider(Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, pos, new Vector2(340, 28));

        var bg = go.transform.Find("Background")?.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.25f, 0.1f, 0.1f, 1f);
        var fill = go.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
        if (fill != null) fill.color = Gold;

        var s = go.GetComponent<Slider>();
        s.minValue = min;
        s.maxValue = max;
        s.value = value;
        return s;
    }

    private static Toggle CreateToggle(Transform parent, string name, string label, Vector2 pos)
    {
        GameObject go = DefaultControls.CreateToggle(Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, pos, new Vector2(260, 40));

        // Varsayılan küçük kutucuğu büyüt ve dikeyde ortala
        var box = go.transform.Find("Background") as RectTransform;
        if (box != null)
        {
            box.anchorMin = box.anchorMax = new Vector2(0f, 0.5f);
            box.pivot = new Vector2(0f, 0.5f);
            box.anchoredPosition = Vector2.zero;
            box.sizeDelta = new Vector2(36, 36);
        }

        var text = go.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = label;
            text.fontSize = 30;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            var rt = (RectTransform)text.transform;
            rt.offsetMin = new Vector2(50, 0);
            rt.offsetMax = Vector2.zero;
        }
        return go.GetComponent<Toggle>();
    }

    private static DefaultControls.Resources Resources() => new DefaultControls.Resources
    {
        standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
        background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
        knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
        checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
    };

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    // ===================================================================== BAĞLANTI VE TEMİZLİK
    /// <summary>Script'in private [SerializeField] alanını doldurur (Inspector'a sürüklemekle aynı).</summary>
    private static void SetRef(Object target, string field, Object value)
    {
        if (target == null) { Debug.LogError($"Menu Builder: '{field}' bağlanamadı, hedef bileşen bulunamadı."); return; }
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogError($"Menu Builder: {target.GetType().Name} içinde '{field}' alanı yok. Script güncel mi?"); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string field, string value)
    {
        if (target == null) { Debug.LogError($"Menu Builder: '{field}' yazılamadı, hedef bileşen bulunamadı."); return; }
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogError($"Menu Builder: {target.GetType().Name} içinde '{field}' alanı yok."); return; }
        p.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveComponents<T>() where T : Component
    {
        foreach (T c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Undo.DestroyObjectImmediate(c);
    }

    /// <summary>Bu isimlerdeki eski objeleri (gizli olanlar dahil) siler.</summary>
    private static void RemoveByName(params string[] names)
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            if (t == null || !names.Contains(t.name)) continue; // üst objesiyle birlikte zaten silinmiş olabilir
            if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject) && !PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                continue; // prefab'ın içindeki bir parçayı tek başına silemeyiz
            Undo.DestroyObjectImmediate(t.gameObject);
        }
    }

    /// <summary>Butonların tıklanması için EventSystem gerekir. Yeni Input System modülünü kullanır.</summary>
    private static void EnsureEventSystem()
    {
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Menu Builder");
            return;
        }

        var old = es.GetComponent<StandaloneInputModule>();
        if (old != null) Undo.DestroyObjectImmediate(old);
        if (es.GetComponent<InputSystemUIInputModule>() == null) Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
    }

    private static void Finish(GameObject root, string message)
    {
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"Menu Builder: {message} Sahneyi kaydetmeyi unutma (Ctrl+S).");
    }
}
#endif
