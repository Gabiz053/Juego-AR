// ??????????????????????????????????????????????
//  ProceduralPebble.cs  ·  _Project.Scripts.Voxel
//  Procedural mesh generation only — no game logic.
//  Generates a low-poly stone from a jittered icosphere.
// ??????????????????????????????????????????????

using UnityEngine;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Generates a convex low-poly pebble mesh at runtime from a jittered icosahedron.<br/>
    /// Each instance produces a unique shape via a random seed.<br/>
    /// The lower hemisphere is clamped to Y = 0 so the stone sits flush on flat surfaces.<br/>
    /// UVs use box-projection so any tiled stone material maps cleanly.<br/>
    /// This component is <b>pure mesh generation</b> — all game logic (placement,
    /// support, destruction) is handled by <see cref="PebbleSupport"/> and
    /// <see cref="BlockDestroy"/>.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Procedural Pebble")]
    public class ProceduralPebble : MonoBehaviour
    {
        #region Inspector ?????????????????????????????????????

        [Header("Shape")]
        [Tooltip("Overall size (width, height, depth) in metres before PlowTool scale variance.")]
        [SerializeField] private Vector3 _size = new Vector3(0.18f, 0.11f, 0.15f);

        [Tooltip("Per-vertex jitter as a fraction of the smallest half-extent (0–0.5).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _jitterFraction = 0.28f;

        [Tooltip("Mesh seed. 0 = new random shape every instantiation.")]
        [SerializeField] private int _seed = 0;

        #endregion

        #region Unity Lifecycle ????????????????????????????????

        private void Awake() => BuildMesh();

        #endregion

        #region Mesh Generation ????????????????????????????????

        private void BuildMesh()
        {
            var mf   = GetComponent<MeshFilter>();
            var mc   = GetComponent<MeshCollider>();
            int seed = _seed == 0 ? Random.Range(1, 99999) : _seed;

            Mesh mesh  = GenerateMesh(_size, _jitterFraction, seed);
            mesh.name  = "PebbleMesh";
            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
            mc.convex     = true;
        }

        // ????????????????????????????????????????????????????
        //  Icosahedron data (12 verts, 20 tris on unit sphere)
        // ????????????????????????????????????????????????????

        private static readonly float Phi = (1f + Mathf.Sqrt(5f)) * 0.5f;

        private static Vector3[] IcoVerts()
        {
            float n = 1f / Mathf.Sqrt(1f + Phi * Phi);
            float p = Phi * n;
            return new[]
            {
                new Vector3(-n,  p,  0), new Vector3( n,  p,  0),
                new Vector3(-n, -p,  0), new Vector3( n, -p,  0),
                new Vector3( 0, -n,  p), new Vector3( 0,  n,  p),
                new Vector3( 0, -n, -p), new Vector3( 0,  n, -p),
                new Vector3( p,  0, -n), new Vector3( p,  0,  n),
                new Vector3(-p,  0, -n), new Vector3(-p,  0,  n),
            };
        }

        private static readonly int[] IcoTris =
        {
             0,11, 5,  0, 5, 1,  0, 1, 7,  0, 7,10,  0,10,11,
             1, 5, 9,  5,11, 4, 11,10, 2, 10, 7, 6,  7, 1, 8,
             3, 9, 4,  3, 4, 2,  3, 2, 6,  3, 6, 8,  3, 8, 9,
             4, 9, 5,  2, 4,11,  6, 2,10,  8, 6, 7,  9, 8, 1,
        };

        private static Mesh GenerateMesh(Vector3 size, float jitterFraction, int seed)
        {
            var     rng  = new System.Random(seed);
            var     icoV = IcoVerts();
            float   jit  = Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f * jitterFraction;

            // Jitter each unit-sphere vertex, renormalise, scale, flatten bottom.
            var shaped = new Vector3[icoV.Length];
            for (int i = 0; i < icoV.Length; i++)
            {
                Vector3 v = icoV[i];
                v.x += (float)(rng.NextDouble() * 2 - 1) * jit / (size.x * 0.5f);
                v.y += (float)(rng.NextDouble() * 2 - 1) * jit / (size.y * 0.5f);
                v.z += (float)(rng.NextDouble() * 2 - 1) * jit / (size.z * 0.5f);
                v    = v.normalized;
                v    = Vector3.Scale(v, size * 0.5f);
                if (v.y < 0f) v.y = 0f;   // flat base
                shaped[i] = v;
            }

            // Duplicate vertices per triangle ? hard-edge flat shading.
            int triCount = IcoTris.Length / 3;
            var fVerts   = new Vector3[triCount * 3];
            var fUVs     = new Vector2[triCount * 3];
            var fTris    = new int    [triCount * 3];

            for (int t = 0; t < triCount; t++)
            {
                int vi = t * 3;
                int a  = IcoTris[vi], b = IcoTris[vi + 1], c = IcoTris[vi + 2];
                fVerts[vi]     = shaped[a]; fVerts[vi + 1] = shaped[b]; fVerts[vi + 2] = shaped[c];
                fUVs  [vi]     = BoxUV(shaped[a], size);
                fUVs  [vi + 1] = BoxUV(shaped[b], size);
                fUVs  [vi + 2] = BoxUV(shaped[c], size);
                fTris [vi]     = vi; fTris[vi + 1] = vi + 1; fTris[vi + 2] = vi + 2;
            }

            var mesh = new Mesh();
            mesh.vertices  = fVerts;
            mesh.uv        = fUVs;
            mesh.triangles = fTris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Box-projection UV: picks the two non-dominant axes for (U, V).
        private static Vector2 BoxUV(Vector3 v, Vector3 size)
        {
            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
            float hx = size.x * 0.5f,  hy = size.y * 0.5f,  hz = size.z * 0.5f;
            if (ax >= ay && ax >= az) return new Vector2(v.z / hz * 0.5f + 0.5f, v.y / hy * 0.5f + 0.5f);
            if (ay >= ax && ay >= az) return new Vector2(v.x / hx * 0.5f + 0.5f, v.z / hz * 0.5f + 0.5f);
            return                           new Vector2(v.x / hx * 0.5f + 0.5f, v.y / hy * 0.5f + 0.5f);
        }

        #endregion
    }
}
