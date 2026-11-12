#if UNITY_EDITOR // F-09: araç yalnızca editörde derlenir, build'e girmez
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Editör aracı: Haritanın kenarındaki halkaya (oynanabilir alanın dışı) ağaç, kaya, çalı dizer.
/// Inspector'daki "Oluştur" butonuyla çalışır, oyun sırasında hiçbir şey yapmaz.
/// Oluşturulan objeler bu objenin child'ı olur; "Temizle" hepsini siler.
/// </summary>
public class BoundaryScatter : MonoBehaviour, IEditorOnlyComponent
{
    [Header("Dizilecek objeler")]
    [SerializeField] private GameObject[] trees;          // ağaç prefab'ları
    [SerializeField] private GameObject[] fillers;        // kaya, çalı, kütük prefab'ları
    [Range(0f, 1f)]
    [SerializeField] private float treeRatio = 0.7f;      // objelerin yüzde kaçı ağaç olsun

    [Header("Alan (merkezden, metre)")]
    [SerializeField] private float innerHalfSize = 40f;   // oynanabilir alanın yarısı (80x80 için 40)
    [SerializeField] private float outerHalfSize = 50f;   // zeminin yarısı (100x100 için 50)

    [Header("Yoğunluk ve çeşitlilik")]
    [SerializeField] private int count = 250;
    [SerializeField] private float minScale = 1.2f;
    [SerializeField] private float maxScale = 2.2f;
    [SerializeField] private int seed = 1;                // aynı seed = aynı dizilim

#if UNITY_EDITOR
    public void Generate()
    {
        Clear();
        Random.InitState(seed);

        for (int i = 0; i < count; i++)
        {
            // Ağaç mı dolgu mu?
            bool isTree = fillers == null || fillers.Length == 0 || Random.value < treeRatio;
            GameObject[] pool = isTree ? trees : fillers;
            if (pool == null || pool.Length == 0) continue;

            GameObject prefab = pool[Random.Range(0, pool.Length)];
            if (prefab == null) continue;

            // Kare halka içinde rastgele nokta: içteki kareye düşerse tekrar dene
            Vector3 pos;
            do
            {
                pos = new Vector3(
                    Random.Range(-outerHalfSize, outerHalfSize),
                    0f,
                    Random.Range(-outerHalfSize, outerHalfSize));
            }
            while (Mathf.Max(Mathf.Abs(pos.x), Mathf.Abs(pos.z)) < innerHalfSize);

            // Prefab bağlantısını koruyarak oluştur (sonradan prefab'ı değiştirirsen hepsi güncellenir)
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            obj.transform.position = transform.position + pos;
            obj.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Dolgular (kaya, çalı) ağaçlardan biraz küçük olsun
            float scale = Random.Range(minScale, maxScale) * (isTree ? 1f : 0.7f);
            obj.transform.localScale = obj.transform.localScale * scale;

            GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic);
            Undo.RegisterCreatedObjectUndo(obj, "Boundary Scatter");
        }
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
    }

    /// <summary>Scene penceresinde iç ve dış sınırı çizer (sadece seçiliyken).</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(innerHalfSize * 2, 0.1f, innerHalfSize * 2));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(outerHalfSize * 2, 0.1f, outerHalfSize * 2));
    }
#endif
}

#if UNITY_EDITOR
/// <summary>BoundaryScatter'ın Inspector'ına "Oluştur" ve "Temizle" butonları ekler.</summary>
[CustomEditor(typeof(BoundaryScatter))]
public class BoundaryScatterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // normal alanlar

        var scatter = (BoundaryScatter)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Oluştur", GUILayout.Height(30)))
            scatter.Generate();

        if (GUILayout.Button("Temizle"))
            scatter.Clear();
    }
}
#endif
#endif // UNITY_EDITOR (F-09)
