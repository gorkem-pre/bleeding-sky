#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Arena → Ses Kütüphanesini Kur
/// Assets/Resources/SoundLibrary.asset yoksa oluşturur, BOŞ olan her sesi Assets/Audio içindeki
/// Kenney dosyalarından isme göre doldurur. Elle seçtiğin sesler tekrar çalıştırınca ezilmez.
/// </summary>
public static class SoundLibraryBuilder
{
    private const string AssetPath = "Assets/Resources/SoundLibrary.asset";

    [MenuItem("Tools/Arena/Ses Kütüphanesini Kur")]
    private static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

        var lib = AssetDatabase.LoadAssetAtPath<SoundLibrary>(AssetPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(lib, AssetPath);
        }

        // Assets/Audio altındaki bütün klipler
        AudioClip[] all = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" })
            .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null).ToArray();

        int filled = 0;
        //                  ses               dosya adı başı (ilk bulunan kullanılır)            ses   pitch
        filled += Fill(lib.spellCast, all, new[] { "cloth", "forceField_" }, 0.70f, 1.40f, 1.60f); // hızlı hava "fşuu"
        filled += Fill(lib.spellCastLayer, all, new[] { "glass_", "impactGlass_light_" }, 0.30f, 1.30f, 1.50f); // buz şıngırtısı
        filled += Fill(lib.spellImpact, all, new[] { "impactGlass_light_", "impactGlass_" }, 0.55f, 0.95f, 1.15f);
        filled += Fill(lib.enemyRise, all, new[] { "impactMining_", "impactSoft_heavy_" }, 0.80f, 0.70f, 0.85f);
        filled += Fill(lib.enemySwing, all, new[] { "knifeSlice", "drawKnife" }, 0.60f, 0.85f, 1.00f);
        filled += Fill(lib.enemyHit, all, new[] { "impactPlank_medium_", "impactWood_light_" }, 0.80f, 0.95f, 1.15f);
        filled += Fill(lib.enemyDeath, all, new[] { "impactWood_heavy_", "impactPlank_" }, 1.00f, 0.80f, 0.95f);
        filled += Fill(lib.playerHurt, all, new[] { "impactPunch_medium_", "impactPunch_" }, 0.90f, 0.90f, 1.05f);
        filled += Fill(lib.playerDeath, all, new[] { "lowFrequency_explosion_", "impactBell_heavy_" }, 0.80f, 0.70f, 0.80f);
        filled += Fill(lib.footstep, all, new[] { "footstep_grass_", "footstep" }, 0.35f, 0.90f, 1.10f);
        filled += Fill(lib.coin, all, new[] { "handleCoins" }, 0.55f, 1.00f, 1.20f);
        filled += Fill(lib.waveStart, all, new[] { "impactBell_heavy_" }, 0.70f, 0.55f, 0.60f);
        filled += Fill(lib.waveCleared, all, new[] { "confirmation_" }, 0.60f, 1.00f, 1.00f);
        filled += Fill(lib.uiClick, all, new[] { "click_" }, 0.60f, 0.95f, 1.05f);
        filled += Fill(lib.uiHover, all, new[] { "tick_", "select_" }, 0.35f, 1.00f, 1.10f);

        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Selection.activeObject = lib;
        Debug.Log($"SoundLibrary: {filled} ses dolduruldu ({all.Length} klip tarandı). Boş kalanları elle sürükleyebilirsin.");
    }

    private static int Fill(SoundCue cue, AudioClip[] all, string[] prefixes, float volume, float pitchMin, float pitchMax)
    {
        if (!cue.IsEmpty) return 0; // elle ayarlanmış, dokunma

        foreach (string prefix in prefixes)
        {
            AudioClip[] found = all.Where(c => c.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                   .OrderBy(c => c.name).ToArray();
            if (found.Length == 0) continue;

            cue.clips = found;
            cue.volume = volume;
            cue.pitch = new Vector2(pitchMin, pitchMax);
            return 1;
        }

        Debug.LogWarning($"SoundLibrary: '{string.Join("', '", prefixes)}' ile başlayan dosya bulunamadı, elle doldur.");
        return 0;
    }
}
#endif
