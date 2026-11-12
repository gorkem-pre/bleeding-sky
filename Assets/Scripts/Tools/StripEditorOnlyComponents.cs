#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Build sırasında, build'e giden sahne kopyasından IEditorOnlyComponent bileşenlerini siler (F-09).
/// Araçların kendisi #if UNITY_EDITOR ile sarılı olduğu için build'e girmez; bu işlemci de
/// sahnedeki objelerin var olmayan bir script'e referans vermesini ("Missing Script") önler.
/// Ürettikleri mezarlar/ağaçlar/siperler normal alt objeler olarak kalır.
/// Çalıştığı yer: yalnızca build. Play modunda hiçbir şeye dokunmaz.
/// </summary>
public class StripEditorOnlyComponents : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || !BuildPipeline.isBuildingPlayer) return;   // Play modu: dokunma

        int removed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is IEditorOnlyComponent)
                {
                    Object.DestroyImmediate(mb);
                    removed++;
                }
            }
        }

        if (removed > 0)
            Debug.Log($"[Build] {scene.name}: {removed} editör aracı bileşeni build'den çıkarıldı.");
    }
}
#endif
