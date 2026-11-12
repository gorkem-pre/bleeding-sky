#if UNITY_EDITOR // F-09: araç yalnızca editörde derlenir, build'e girmez
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Editör aracı: Arenaya siper (kaya, sütun, yıkık duvar, kasa) serpiştirir.
/// Kurallar: avluya ve mezarlıklara yaklaşmaz, avludan arenaya bakan görüş hattını boş bırakır,
/// siperler arasında en az belirli bir mesafe bırakır, collider'ı olmayana collider ekler.
/// Bu obje arenanın merkezine konur (0,0,0). "Oluştur" butonu ile çalışır.
/// </summary>
public class CoverScatter : MonoBehaviour, IEditorOnlyComponent
{
    [System.Serializable]
    public class CoverType
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;      // ne sıklıkla seçilsin
        public float minScale = 1f;
        public float maxScale = 1.5f;
        public float sink = 0f;                  // ne kadar toprağa gömülsün (metre, ör. yıkık duvar için 0.5)
        public float maxTilt = 0f;               // yana yatma (derece, ör. yıkık duvar için 10)
    }

    [Header("Siper çeşitleri")]
    [SerializeField] private CoverType[] types;

    [Header("Alan")]
    [SerializeField] private float halfSize = 36f;           // oynanabilir alanın yarısı (sınır duvarlarından biraz içeride)
    [SerializeField] private int count = 8;
    [SerializeField] private float minSpacing = 10f;         // iki siper arası en az mesafe

    [Header("Uzak durulacak yerler")]
    [SerializeField] private Transform courtyard;
    [SerializeField] private float courtyardRadius = 12f;
    [SerializeField] private Transform[] graveyards;
    [SerializeField] private float graveyardRadius = 9f;
    [SerializeField] private float sightlineWidth = 8f;      // avludan merkeze uzanan, boş kalacak koridorun genişliği

    [Header("Diğer")]
    [SerializeField] private bool addColliders = true;
    [SerializeField] private int seed = 1;

#if UNITY_EDITOR
    public void Generate()
    {
        Clear();
        Random.InitState(seed);

        var placed = new System.Collections.Generic.List<Vector3>();
        int attempts = 0;

        while (placed.Count < count && attempts < count * 200)
        {
            attempts++;

            Vector3 p = transform.position + new Vector3(
                Random.Range(-halfSize, halfSize), 0f, Random.Range(-halfSize, halfSize));

            if (!IsValid(p, placed)) continue;

            CoverType type = PickType();
            if (type == null || type.prefab == null) continue;

            var obj = (GameObject)PrefabUtility.InstantiatePrefab(type.prefab, transform);
            obj.transform.position = p + Vector3.down * type.sink;
            obj.transform.rotation = Quaternion.Euler(
                Random.Range(-type.maxTilt, type.maxTilt),
                Random.Range(0f, 360f),
                Random.Range(-type.maxTilt, type.maxTilt));
            obj.transform.localScale = obj.transform.localScale * Random.Range(type.minScale, type.maxScale);

            if (addColliders && obj.GetComponentInChildren<Collider>() == null)
                obj.AddComponent<BoxCollider>();

            GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);
            Undo.RegisterCreatedObjectUndo(obj, "Cover Scatter");
            placed.Add(p);
        }

        if (placed.Count < count)
            Debug.LogWarning($"CoverScatter: {count} siperden sadece {placed.Count} tanesi yerleşebildi. Min Spacing'i ya da yasak bölgeleri küçült.");
    }

    private bool IsValid(Vector3 p, System.Collections.Generic.List<Vector3> placed)
    {
        // Diğer siperlerden yeterince uzak mı?
        foreach (Vector3 q in placed)
            if (Flat(p - q).magnitude < minSpacing) return false;

        if (courtyard != null)
        {
            // Avluya çok yakın mı?
            if (Flat(p - courtyard.position).magnitude < courtyardRadius) return false;

            // Avludan arena merkezine uzanan görüş hattının üstünde mi?
            Vector3 a = Flat(courtyard.position);
            Vector3 b = Flat(transform.position);
            if (DistanceToSegment(Flat(p), a, b) < sightlineWidth / 2f) return false;
        }

        // Mezarlıklara çok yakın mı?
        if (graveyards != null)
            foreach (Transform g in graveyards)
                if (g != null && Flat(p - g.position).magnitude < graveyardRadius) return false;

        return true;
    }

    private CoverType PickType()
    {
        if (types == null || types.Length == 0) return null;
        float total = 0f;
        foreach (var t in types) total += t.weight;
        float r = Random.value * total;
        foreach (var t in types)
        {
            r -= t.weight;
            if (r <= 0f) return t;
        }
        return types[types.Length - 1];
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        return (p - (a + ab * t)).magnitude;
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
    }

    /// <summary>Seçiliyken: sarı = alan, kırmızı = yasak bölgeler, turuncu = görüş hattı.</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(halfSize * 2f, 0.1f, halfSize * 2f));

        Gizmos.color = Color.red;
        if (courtyard != null) DrawCircle(courtyard.position, courtyardRadius);
        if (graveyards != null)
            foreach (Transform g in graveyards)
                if (g != null) DrawCircle(g.position, graveyardRadius);

        if (courtyard != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(Flat(courtyard.position), Flat(transform.position));
        }
    }

    private static void DrawCircle(Vector3 c, float r)
    {
        const int seg = 32;
        Vector3 prev = c + new Vector3(r, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            Vector3 next = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(CoverScatter))]
public class CoverScatterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var scatter = (CoverScatter)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Oluştur", GUILayout.Height(30)))
            scatter.Generate();

        if (GUILayout.Button("Temizle"))
            scatter.Clear();
    }
}
#endif
#endif // UNITY_EDITOR (F-09)
