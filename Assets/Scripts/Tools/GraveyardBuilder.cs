#if UNITY_EDITOR // F-09: araç yalnızca editörde derlenir, build'e girmez
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Editör aracı: Bir doğma noktasının etrafına küçük bir mezarlık kurar.
/// Bu objenin konumu = doğma noktası. "Ön" (+Z) = oyuncunun geleceği yön, orası boş kalır.
/// Mezar taşları arkada sıralar halinde dizilir, arkaya ve yanlara çit çekilir.
/// "Oluştur" butonu ile çalışır, oyun sırasında hiçbir şey yapmaz.
/// </summary>
public class GraveyardBuilder : MonoBehaviour, IEditorOnlyComponent
{
    [Header("Yön")]
    [SerializeField] private Transform faceTarget;           // mezarlık bu noktaya dönük olur (ör. Courtyard)

    [Header("Prefab'lar")]
    [SerializeField] private GameObject[] tombstones;        // mezar taşları
    [SerializeField] private GameObject[] crosses;           // haçlar
    [SerializeField] private GameObject[] decor;             // tabut, kemik, kürek...
    [SerializeField] private GameObject fence;               // tek çit parçası

    [Header("Yerleşim (metre)")]
    [SerializeField] private float width = 8f;               // yan yana genişlik
    [SerializeField] private float depth = 7f;               // arkaya doğru derinlik
    [SerializeField] private float clearFront = 2.5f;        // doğma noktasının arkasında boş kalan şerit
    [SerializeField] private int rows = 3;
    [SerializeField] private int perRow = 4;
    [Range(0f, 1f)]
    [SerializeField] private float crossChance = 0.3f;       // bir yerin haç olma olasılığı
    [SerializeField] private int decorCount = 4;
    [SerializeField] private float fenceSpacing = 1.5f;      // çit parçaları arası mesafe

    [Header("Çeşitlilik")]
    [SerializeField] private float positionJitter = 0.4f;    // sıralardan ne kadar kaysın
    [SerializeField] private float yawJitter = 15f;          // sağa-sola dönme (derece)
    [SerializeField] private float tiltJitter = 8f;          // öne-arkaya / yana yatma (derece)
    [SerializeField] private float objectScale = 2f;         // Graveyard paketi küçük modellenmiş
    [SerializeField] private bool addColliders = true;       // collider'ı olmayan taş ve çitlere ekle
    [SerializeField] private int seed = 1;

#if UNITY_EDITOR
    public void Generate()
    {
        Clear();
        Random.InitState(seed);

        // Önce mezarlığı hedefe çevir (sadece Y ekseninde)
        if (faceTarget != null)
        {
            Vector3 dir = faceTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                Undo.RecordObject(transform, "Graveyard Face");
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        // 1) Mezar taşları ve haçlar: doğma noktasının arkasında sıralar halinde
        float rowStep = (depth - clearFront) / Mathf.Max(1, rows);
        float colStep = width / Mathf.Max(1, perRow);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < perRow; c++)
            {
                bool isCross = crosses != null && crosses.Length > 0 && Random.value < crossChance;
                GameObject[] pool = isCross ? crosses : tombstones;
                if (pool == null || pool.Length == 0) continue;

                float x = -width / 2f + colStep * (c + 0.5f) + Random.Range(-positionJitter, positionJitter);
                float z = -clearFront - rowStep * (r + 0.5f) + Random.Range(-positionJitter, positionJitter);

                // Ön yüz oyuncuya baksın (+Z), biraz rastgele dönük ve yatık
                Quaternion rot = Quaternion.Euler(
                    Random.Range(-tiltJitter, tiltJitter),
                    Random.Range(-yawJitter, yawJitter),
                    Random.Range(-tiltJitter, tiltJitter));

                Place(pool[Random.Range(0, pool.Length)], new Vector3(x, 0f, z), rot, true);
            }
        }

        // 2) Dekor: mezarların arasına serpiştir (ön şeride değil)
        if (decor != null && decor.Length > 0)
        {
            for (int i = 0; i < decorCount; i++)
            {
                Vector3 p = new Vector3(Random.Range(-width / 2f, width / 2f), 0f, Random.Range(-depth, -clearFront * 0.6f));
                Place(decor[Random.Range(0, decor.Length)], p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), false);
            }
        }

        // 3) Çit: arka kenar + iki yan kenar. Ön taraf açık kalır.
        if (fence != null)
        {
            float backZ = -depth - 0.5f;
            float halfW = width / 2f + 0.5f;

            for (float x = -halfW; x <= halfW + 0.01f; x += fenceSpacing)                 // arka
                Place(fence, new Vector3(x, 0f, backZ), Quaternion.identity, true);

            for (float z = backZ + fenceSpacing; z <= -clearFront * 0.5f; z += fenceSpacing) // yanlar
            {
                Place(fence, new Vector3(-halfW, 0f, z), Quaternion.Euler(0f, 90f, 0f), true);
                Place(fence, new Vector3(halfW, 0f, z), Quaternion.Euler(0f, 90f, 0f), true);
            }
        }
    }

    /// <summary>Prefab'ı bu objenin child'ı olarak, yerel konum/açıyla yerleştirir.</summary>
    private void Place(GameObject prefab, Vector3 localPos, Quaternion localRot, bool solid)
    {
        if (prefab == null) return;

        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = localRot;
        obj.transform.localScale = obj.transform.localScale * objectScale;

        // Mermi çarpsın, oyuncu içinden geçmesin
        if (solid && addColliders && obj.GetComponentInChildren<Collider>() == null)
            obj.AddComponent<BoxCollider>();

        GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic);
        Undo.RegisterCreatedObjectUndo(obj, "Graveyard Builder");
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
    }

    /// <summary>Seçiliyken: mavi = mezarlık alanı, yeşil = boş kalacak doğma şeridi.</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(0f, 0f, -depth / 2f), new Vector3(width, 0.1f, depth));
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(0f, 0f, clearFront / 2f - clearFront), new Vector3(width, 0.1f, clearFront));
        Gizmos.DrawRay(Vector3.zero, Vector3.forward * 3f); // ön yön
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(GraveyardBuilder))]
public class GraveyardBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var builder = (GraveyardBuilder)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Oluştur", GUILayout.Height(30)))
            builder.Generate();

        if (GUILayout.Button("Temizle"))
            builder.Clear();
    }
}
#endif
#endif // UNITY_EDITOR (F-09)
