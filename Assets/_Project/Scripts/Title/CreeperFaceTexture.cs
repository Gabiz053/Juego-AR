// ------------------------------------------------------------
//  CreeperFaceTexture.cs  -  _Project.Scripts.Title
//  Generates the iconic Creeper face texture procedurally.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Title
{
    /// <summary>
    /// Generates a Creeper face texture procedurally at runtime.<br/>
    /// The texture maps onto ARFace UVs:
    /// <list type="bullet">
    ///   <item>Green semi-transparent base (the Creeper skin colour)</item>
    ///   <item>Dark pixel-art eyes and mouth in the centre</item>
    ///   <item>Fully transparent everywhere else so the camera feed shows through</item>
    /// </list>
    /// Attach to the same <c>GameObject</c> as <see cref="CreeperFaceFilter"/>
    /// and assign the generated material via <see cref="GeneratedMaterial"/>.<br/>
    /// Only needed for <b>Mode B</b> (flat overlay).  If using the Creeper head
    /// prefab (Mode A), this script is not required.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Title/Creeper Face Texture")]
    public class CreeperFaceTexture : MonoBehaviour
    {
        #region Inspector -----------------------------------------

        [Header("Texture Settings")]
        [Tooltip("Texture resolution (square). 256 is plenty for pixel-art.")]
        [SerializeField] private int _resolution = 256;

        [Header("Creeper Colours")]
        [Tooltip("Green skin overlay colour.")]
        [SerializeField] private Color _skinColor = new Color(0.33f, 0.62f, 0.18f, 0.65f);

        [Tooltip("Dark colour for eyes and mouth pixels.")]
        [SerializeField] private Color _featureColor = new Color(0.05f, 0.05f, 0.05f, 0.90f);

        [Header("Material")]
        [Tooltip("Shader used for the generated material.")]
        [SerializeField] private Shader _creeperShader;

        [Header("Auto-wire")]
        [Tooltip("If true, automatically assigns the generated material to the sibling CreeperFaceFilter.")]
        [SerializeField] private bool _autoWireFilter = true;

        #endregion

        #region State ---------------------------------------------

        private Texture2D _texture;
        private Material  _material;

        #endregion

        #region Public API ----------------------------------------

        /// <summary>The generated material ready to assign to a MeshRenderer.</summary>
        public Material GeneratedMaterial => _material;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake()
        {
            _texture  = GenerateCreeperTexture();
            _material = CreateMaterial(_texture);

            if (_autoWireFilter)
                WireFilter();

            Debug.Log($"[CreeperFaceTexture] Texture generated ({_resolution}x{_resolution}).");
        }

        private void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
            if (_material != null) Destroy(_material);
        }

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Generates a square texture with the Creeper face pattern.
        /// The pattern is drawn on a 16×16 conceptual grid scaled up to
        /// <see cref="_resolution"/>.
        /// </summary>
        private Texture2D GenerateCreeperTexture()
        {
            var tex = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp
            };

            int cellSize = _resolution / 16;
            var pixels   = new Color[_resolution * _resolution];

            // Fill with transparent green skin.
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = _skinColor;

            // Creeper face features on 16×16 grid.
            // Eyes (two 2×2 blocks with 1-cell offset between top/bottom half)
            FillCell(pixels, cellSize, 4,  3, _featureColor);
            FillCell(pixels, cellSize, 4,  4, _featureColor);
            FillCell(pixels, cellSize, 5,  3, _featureColor);
            FillCell(pixels, cellSize, 5,  4, _featureColor);
            FillCell(pixels, cellSize, 6,  4, _featureColor);
            FillCell(pixels, cellSize, 6,  5, _featureColor);
            FillCell(pixels, cellSize, 7,  4, _featureColor);
            FillCell(pixels, cellSize, 7,  5, _featureColor);

            FillCell(pixels, cellSize, 4, 11, _featureColor);
            FillCell(pixels, cellSize, 4, 12, _featureColor);
            FillCell(pixels, cellSize, 5, 11, _featureColor);
            FillCell(pixels, cellSize, 5, 12, _featureColor);
            FillCell(pixels, cellSize, 6, 10, _featureColor);
            FillCell(pixels, cellSize, 6, 11, _featureColor);
            FillCell(pixels, cellSize, 7, 10, _featureColor);
            FillCell(pixels, cellSize, 7, 11, _featureColor);

            // Mouth (frown shape)
            FillCell(pixels, cellSize, 8,  7, _featureColor);
            FillCell(pixels, cellSize, 8,  8, _featureColor);
            FillCell(pixels, cellSize, 9,  6, _featureColor);
            FillCell(pixels, cellSize, 9,  7, _featureColor);
            FillCell(pixels, cellSize, 9,  8, _featureColor);
            FillCell(pixels, cellSize, 9,  9, _featureColor);
            FillCell(pixels, cellSize, 10, 6, _featureColor);
            FillCell(pixels, cellSize, 10, 7, _featureColor);
            FillCell(pixels, cellSize, 10, 8, _featureColor);
            FillCell(pixels, cellSize, 10, 9, _featureColor);
            FillCell(pixels, cellSize, 11, 5, _featureColor);
            FillCell(pixels, cellSize, 11, 6, _featureColor);
            FillCell(pixels, cellSize, 11, 9, _featureColor);
            FillCell(pixels, cellSize, 11, 10, _featureColor);

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Fills a single cell on the 16×16 conceptual grid.
        /// Row 0 = top of the texture (UV y=1), row 15 = bottom.
        /// </summary>
        private void FillCell(Color[] pixels, int cellSize, int row, int col, Color color)
        {
            int baseY = (_resolution - 1) - (row * cellSize);
            int baseX = col * cellSize;

            for (int dy = 0; dy < cellSize; dy++)
            {
                for (int dx = 0; dx < cellSize; dx++)
                {
                    int px = baseX + dx;
                    int py = baseY - dy;
                    if (px >= 0 && px < _resolution && py >= 0 && py < _resolution)
                        pixels[py * _resolution + px] = color;
                }
            }
        }

        /// <summary>Creates a material instance using the CreeperFace shader.</summary>
        private Material CreateMaterial(Texture2D texture)
        {
            Shader shader = _creeperShader;

            if (shader == null)
                shader = Shader.Find("ARmonia/AR/CreeperFace");

            if (shader == null)
            {
                Debug.LogError("[CreeperFaceTexture] CreeperFace shader not found! Using URP/Unlit fallback.");
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            var mat = new Material(shader);
            mat.mainTexture = texture;
            return mat;
        }

        /// <summary>
        /// Auto-assigns the generated material to the sibling
        /// <see cref="CreeperFaceFilter"/> component via its public API.
        /// </summary>
        private void WireFilter()
        {
            var filter = GetComponent<CreeperFaceFilter>();
            if (filter == null)
                filter = GetComponentInParent<CreeperFaceFilter>();

            if (filter == null)
            {
                Debug.LogWarning("[CreeperFaceTexture] No CreeperFaceFilter found to auto-wire.", this);
                return;
            }

            filter.SetMaterial(_material);
            Debug.Log("[CreeperFaceTexture] Auto-wired material to CreeperFaceFilter.");
        }

        #endregion
    }
}
