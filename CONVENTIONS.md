# Convenciones del proyecto ARmonia

Documento de referencia para mantener consistencia en todo el proyecto.
**Cada nuevo asset, script o carpeta debe seguir estas reglas.**

> **Última auditoría:** 49 scripts · 1 escena · 16 prefabs · 5 ScriptableObjects ·
> 2 shaders · 8 materiales · 40 clips de audio · 25 texturas/modelos · 5 fuentes

---

## Tabla de contenidos

1. [Carpetas](#1-carpetas)
2. [Jerarquía de escena](#2-jerarquía-de-escena)
3. [Scripts C#](#3-scripts-c)
4. [Variables y campos C#](#4-variables-y-campos-c)
5. [Materiales](#5-materiales)
6. [Prefabs](#6-prefabs)
7. [Texturas y modelos 3D](#7-texturas)
8. [Audio](#8-audio)
9. [Escenas](#9-escenas)
10. [Shaders](#10-shaders)
11. [ScriptableObjects](#11-scriptableobjects)
12. [Rendimiento — target S24 Ultra](#12-rendimiento--target-s24-ultra)
13. [Estética URP](#13-estética-urp)
14. [Patrones de comunicación](#14-patrones-de-comunicación)
15. [Reglas generales](#15-reglas-generales)

---

## 1. Carpetas

| Regla | Correcto | Incorrecto |
|-------|----------|------------|
| PascalCase | `Scripts/` | `scripts/` |
| Plural | `Materials/`, `Prefabs/`, `Textures/` | `Material/`, `Prefab/` |
| Sin espacios | `XR/` | `My Folder/` |
| Raíz del proyecto | `_Project/` (con `_` para ordenar primero) | `Project/` |
| Carpetas vacías | Confirmar con `.gitkeep` | Dejar vacía (Git la ignora) |

### Estructura de `_Project/`

```text
_Project/
├── Assets/
│   ├── Fonts/               ← TTF + SDF assets de TextMeshPro
│   ├── BlockDatabase.asset
│   ├── HarmonyConfig.asset
│   └── WorldModeConfig_*.asset
├── Audio/
│   ├── Music/               ← Pistas de fondo (.mp3)
│   └── SFX/
│       ├── UI/              ← Sonidos de interfaz (SFX_*)
│       └── Voxels/          ← Sonidos de bloques y herramientas
├── Materials/
│   ├── AR/                  ← Materiales de planos AR y grid
│   └── Blocks/              ← Materiales de bloques voxel
├── Models/
│   └── Blocks/              ← Modelos .glb importados (Model_Glass, Model_Grass, etc.)
├── Prefabs/
│   ├── AR/                  ← Planos AR, interactores XR
│   ├── Blocks/              ← Bloques voxel y piedritas (+ _Deprecated/)
│   ├── UI/                  ← (reservada para prefabs de UI)
│   └── VFX/                 ← Efectos de partículas
├── Scenes/
│   └── Main_AR.unity
├── Scripts/
│   ├── AR/                  ← Gestión AR: ancla, planos, profundidad, modos
│   ├── Core/                ← Grid, armonía, audio, iluminación, undo/redo, reset, screenshot, datos de modo
│   ├── Interaction/         ← Input táctil, herramientas, colocación/destrucción, debug ray
│   ├── UI/                  ← HUD, menú, orientación, servicios UI
│   └── Voxel/               ← Bloques, spawn/destroy, piedras procedurales, VFX
├── Shaders/                 ← Shaders HLSL personalizados (URP)
└── Textures/
    ├── AR/                  ← Texturas de suelo AR (T_Sand, T_ZenFloor)
    ├── Icons/               ← Icono de app
    └── UI/                  ← Sprites PNG para hotbar y menú (Icon_*, UI_*)
```

---

## 2. Jerarquía de escena

### `Main_AR.unity` — jerarquía completa

La escena se organiza en **2 GameObjects raíz** que actúan como grupos lógicos.

```text
AR System                                  [Empty — agrupa objetos 3D/AR]
├── AR Session                             [ARSession, ARInputManager]
├── XR Interaction Manager                 [XRInteractionManager]
├── XR Origin (Mobile AR)                  [XROrigin, ARPlaneManager, ARRaycastManager,
│   │                                       ARAnchorManager, ARTrackedImageManager,
│   │                                       ARWorldManager, ARDepthService,
│   │                                       ARPlaneGridAligner, WorldModeBootstrapper]
│   ├── Camera Offset
│   │   └── Main Camera                    [Camera, AudioListener, TrackedPoseDriver,
│   │                                       ARCameraManager, ARCameraBackground,
│   │                                       AROcclusionManager]
│   │       └── CameraFlashLight           [Light (Spot) — linterna de foco]
│   ├── Svc_Audio                          [Empty — agrupa servicios de audio]
│   │       GameAudioService + AudioSource (SFX)
│   │       MusicService + AudioSource (Music)
│   ├── Svc_Interaction                    [Empty — agrupa input y herramientas]
│   │       TouchInputRouter
│   │       ARBlockPlacer
│   │       BlockDestroyer
│   │       BrushTool
│   │       PlowTool
│   │       DebugRayVisualizer + LineRenderer
│   ├── Svc_GameLogic                      [Empty — agrupa lógica de juego]
│   │       HarmonyService
│   │       UndoRedoService
│   └── Svc_Lighting                       [Empty — gestión de iluminación]
│           LightingService
├── WorldContainer                         [GridManager, GridVisualizer]
├── ToolManager                            [ToolManager]
└── Directional Light                      [Light (Directional), URP AdditionalLightData]

UI System                                  [Empty — agrupa objetos UI]
├── MainCanvas                             [Canvas, CanvasScaler, GraphicRaycaster,
│   │                                       UIManager, OrientationManager,
│   │                                       UIAudioService, AudioSource]
│   ├── HUD_Hotbar                         [Image]
│   │   └── Hotbar_LayoutGroup             [HorizontalLayoutGroup]
│   │       ├── Btn_Sand       → Icon_Sand, Txt_Sand
│   │       ├── Btn_Glass      → Icon_Glass, Txt_Glass
│   │       ├── Btn_Stone      → Icon_Stone, Txt_Stone
│   │       ├── Btn_Wood       → Icon_Wood, Txt_Wood
│   │       ├── Btn_Torch      → Icon_Torch, Txt_Torch
│   │       ├── Btn_Grass      → Icon_Grass, Txt_Grass
│   │       └── Btn_None       → Txt_None
│   │
│   ├── HUD_ToolPanel                      [Image]
│   │   └── Tools_LayoutGroup              [VerticalLayoutGroup]
│   │       ├── Btn_Break      → Icon_Break, Txt_Break
│   │       ├── Btn_Brush      → Icon_Brush, Txt_Brush  [BrushHUD]
│   │       └── Btn_Hoe        → Icon_Hoe, Txt_Hoe
│   │
│   ├── HUD_Selector                       [Image — highlight amarillo]
│   │
│   ├── HUD_Harmony                        [HarmonyHUD]
│   │   ├── Img_BarBackground              [Image]
│   │   │   └── Img_BarFill               [Image — fill anchor-driven]
│   │   └── Txt_HarmonyStatus             [TMP_Text]
│   │
│   ├── HUD_UndoRedo                       [UndoRedoHUD]
│   │   └── Undo_LayoutGroup              [HorizontalLayoutGroup]
│   │       ├── Btn_Undo       → Icon_Undo, Txt_Undo
│   │       └── Btn_Redo       → Icon_Redo, Txt_Redo
│   │
│   ├── HUD_MenuBlocker                    [Button+Image — invisible fullscreen]
│   │
│   ├── HUD_OptionsMenu                    [GameOptionsMenu]
│   │   ├── Svc_WorldReset                 [WorldResetService]
│   │   ├── Svc_Screenshot                 [ScreenshotService]
│   │   ├── Btn_Settings       → Icon_Settings, Txt_Settings
│   │   └── Panel_OptionsDropdown          [VerticalLayoutGroup, ContentSizeFitter]
│   │       ├── Btn_Lighting               [DropdownButtonState] → Txt_Lighting
│   │       ├── Btn_Depth                  [DropdownButtonState] → Txt_Depth
│   │       ├── Btn_Grid                   [DropdownButtonState] → Txt_Grid
│   │       ├── Btn_Plane                  [DropdownButtonState] → Txt_Plane
│   │       ├── Panel_MusicVolume
│   │       │   ├── Sld_MusicVolume        [Slider]
│   │       │   │   ├── Background
│   │       │   │   ├── Fill Area
│   │       │   │   └── Handle Slide Area → Handle
│   │       │   └── Txt_MusicVolume        [TMP_Text]
│   │       ├── Btn_Photo      → Txt_Photo
│   │       ├── Btn_ClearAll   → Txt_ClearAll
│   │       └── Btn_Exit       → Txt_Exit
│   │
│   ├── HUD_PerfectHarmony                 [PerfectHarmonyPanel, CanvasGroup]
│   │   ├── Txt_Status                     [TMP_Text — título]
│   │   ├── HUD_Particles                  [HarmonyParticles, ParticleSystem]
│   │   └── Btn_Continue       → Txt_Continue
│   │
│   ├── Plane_Controls                     [contenedor de controles extra]
│   │
│   └── Popup_ConfirmClearAll              [RectTransform]
│       └── Overlay_Background             [Image — overlay semi-transparente]
│           └── Panel_ConfirmDialog        [Image — card centrada]
│               ├── Txt_ConfirmMessage     [TMP_Text × 2]
│               └── Dialog_LayoutGroup     [HorizontalLayoutGroup]
│                   ├── Btn_Confirm → Txt_Confirm
│                   └── Btn_Cancel  → Txt_Cancel
│
└── EventSystem                            [InputSystemUIInputModule, EventSystem]
```

### Mapa Script → GameObject

| Script | GameObject host | RequireComponent |
|--------|----------------|------------------|
| `ARWorldManager` | XR Origin (Mobile AR) | `ARAnchorManager` |
| `ARDepthService` | XR Origin (Mobile AR) | — |
| `ARPlaneGridAligner` | XR Origin (Mobile AR) | — |
| `WorldModeBootstrapper` | XR Origin (Mobile AR) | — |
| `GameAudioService` | Svc_Audio | — |
| `MusicService` | Svc_Audio | — |
| `TouchInputRouter` | Svc_Interaction | — |
| `ARBlockPlacer` | Svc_Interaction | — |
| `BlockDestroyer` | Svc_Interaction | — |
| `BrushTool` | Svc_Interaction | — |
| `PlowTool` | Svc_Interaction | — |
| `DebugRayVisualizer` | Svc_Interaction | — |
| `HarmonyService` | Svc_GameLogic | — |
| `UndoRedoService` | Svc_GameLogic | — |
| `LightingService` | Svc_Lighting | — |
| `GridManager` | WorldContainer | — |
| `GridVisualizer` | WorldContainer | — |
| `ToolManager` | ToolManager | — |
| `UIManager` | MainCanvas | — |
| `OrientationManager` | MainCanvas | — |
| `UIAudioService` | MainCanvas | `AudioSource` |
| `HarmonyHUD` | HUD_Harmony | — |
| `UndoRedoHUD` | HUD_UndoRedo | — |
| `BrushHUD` | Btn_Brush | — |
| `GameOptionsMenu` | HUD_OptionsMenu | — |
| `PerfectHarmonyPanel` | HUD_PerfectHarmony | `CanvasGroup` |
| `HarmonyParticles` | HUD_Particles | `ParticleSystem` |
| `WorldResetService` | Svc_WorldReset | — |
| `ScreenshotService` | Svc_Screenshot | — |
| `ButtonPressAnimation` | Cada `Btn_*` | `Button` |
| `DropdownButtonState` | `Btn_Lighting`, `Btn_Depth`, `Btn_Grid`, `Btn_Plane` | — |

### Reglas de nombrado de GameObjects

| Prefijo | Uso | Ejemplos |
|---------|-----|----------|
| *(ninguno)* | Objetos estándar de Unity | `AR Session`, `Main Camera`, `EventSystem` |
| `HUD_` | Regiones persistentes de pantalla | `HUD_Hotbar`, `HUD_Harmony`, `HUD_UndoRedo` |
| `Panel_` | Paneles contenidos en una sección | `Panel_OptionsDropdown`, `Panel_ConfirmDialog`, `Panel_MusicVolume` |
| `Popup_` | Modales fullscreen | `Popup_ConfirmClearAll` |
| `Overlay_` | Fondos oscuros/transparentes | `Overlay_Background` |
| `Bar_` / `Img_` | Componentes de barras de progreso | `Img_BarBackground`, `Img_BarFill` |
| `Btn_` | Botones (contiene un hijo `Txt_` o `Icon_`) | `Btn_Sand`, `Btn_Settings`, `Btn_Confirm` |
| `Txt_` | Labels de TextMeshPro | `Txt_Sand`, `Txt_HarmonyStatus`, `Txt_ConfirmMessage` |
| `Icon_` | Imágenes de icono dentro de botones | `Icon_Undo`, `Icon_Sand`, `Icon_Settings` |
| `Sld_` | Sliders | `Sld_MusicVolume` |
| `*_LayoutGroup` | Objetos con LayoutGroup component | `Hotbar_LayoutGroup`, `Dialog_LayoutGroup` |
| `Svc_` | GameObjects de servicio (sin visual) | `Svc_Screenshot`, `Svc_WorldReset`, `Svc_Audio`, `Svc_Interaction` |
| PascalCase | Singletons / contenedores | `WorldContainer`, `ToolManager`, `MainCanvas` |

**Reglas obligatorias:**
- Cada `Btn_X` debe contener al menos un hijo `Txt_X` o `Icon_X` con el mismo sufijo.
- Todos los nombres en **inglés**. No español en la jerarquía.
- Sin espacios en nombres propios. Usar `_` para separar prefijo de nombre.
- Los objetos estándar de Unity mantienen su nombre por defecto (`AR Session`, `Directional Light`, etc.).

### Wiring de botones — regla del Brush

El `Btn_Brush` es una **excepción** al patrón estándar de botones:

| Botón | OnClick destino | Razón |
|-------|----------------|-------|
| `Btn_Sand` … `Btn_Hoe` (salvo Brush) | `UIManager.OnSlotClicked(int)` | Son herramientas normales |
| `Btn_Brush` | `BrushTool.ToggleBrush()` — **llamada directa** | El Brush es un mode overlay, no una herramienta. No pasa por ToolManager. |

---

## 3. Scripts C\#

### Nombrado de archivos

| Regla | Ejemplo |
|-------|---------|
| PascalCase | `GridManager.cs` |
| Nombre de archivo = nombre de clase exacto | `ARBlockPlacer.cs` → `public class ARBlockPlacer` |
| Sin prefijos/sufijos extra | `BlockPlacer.cs` **incorrecto** si la clase es `ARBlockPlacer` |
| Namespace sigue la ruta de carpeta | `namespace _Project.Scripts.Core` |

### Namespaces por carpeta

| Carpeta | Namespace |
|---------|-----------|
| `Scripts/AR/` | `_Project.Scripts.AR` |
| `Scripts/Core/` | `_Project.Scripts.Core` |
| `Scripts/Interaction/` | `_Project.Scripts.Interaction` |
| `Scripts/UI/` | `_Project.Scripts.UI` |
| `Scripts/Voxel/` | `_Project.Scripts.Voxel` |

### Atributos obligatorios de clase

```csharp
[DisallowMultipleComponent]
[AddComponentMenu("ARmonia/{Carpeta}/{NombreClase}")]
public class MiScript : MonoBehaviour { }
```

Ejemplo real:

```csharp
[DisallowMultipleComponent]
[AddComponentMenu("ARmonia/Core/Grid Manager")]
public class GridManager : MonoBehaviour { }
```

### Orden de regiones dentro de cada MonoBehaviour

Cada script sigue este orden de `#region`:

```csharp
#region Constants              // private const, static readonly
#region Inspector              // [SerializeField] con [Header] y [Tooltip]
#region Events                 // public event Action<T>
#region Cached Components      // o "#region State" — referencias y estado runtime
#region Public API             // Propiedades y métodos públicos
#region Unity Lifecycle        // Awake, OnEnable, Start, Update, LateUpdate, OnDisable, OnDestroy
#region Internals              // Métodos privados auxiliares
#region Validation             // ValidateReferences() llamado desde Start()
```

### Reglas de estilo C\#

| Regla | Ejemplo |
|-------|---------|
| Cada `[SerializeField]` lleva `[Tooltip]` | `[Tooltip("Descripción clara.")] [SerializeField] private float _value;` |
| Cada grupo de campos lleva `[Header]` | `[Header("Dependencies")]` |
| `ValidateReferences()` en `Start()` | `private void Start() { ValidateReferences(); }` |
| XML `<summary>` en toda clase pública | `/// <summary>Gestiona la rejilla de construcción.</summary>` |
| Yield cacheados como campo | `private readonly WaitForSeconds _wait = new WaitForSeconds(0.5f);` |
| No dejar `using` sin usar | Limpiar imports |

---

## 4. Variables y campos C\#

| Tipo | Convención | Ejemplo |
|------|-----------|---------|
| `[SerializeField]` private | `_camelCase` | `_gridManager`, `_audioService` |
| Private field (sin serialize) | `_camelCase` | `_lastScore`, `_knocked`, `_isCapturing` |
| Public property | PascalCase | `GridSize`, `IsWorldAnchored`, `CurrentScore` |
| Constant (`const`) | `UPPER_SNAKE_CASE` | `MIN_FORWARD_SQR_MAG`, `RAY_DURATION` |
| Static readonly | `UPPER_SNAKE_CASE` | `GRID_MATRIX_ID`, `GRID_ENABLED_ID` |
| Local variable | `camelCase` | `snappedPosition`, `halfSize`, `elapsed` |
| Method parameter | `camelCase` | `hitPose`, `playerCamera`, `enable` |
| Public method | PascalCase | `AnchorWorld()`, `GetSnappedPosition()` |
| Private method | PascalCase | `OrientTowardsCamera()`, `Recalculate()` |
| Event (`Action`) | `On` + PascalCase | `OnToolChanged`, `OnHarmonyChanged`, `OnPerfectHarmony` |
| Enum values | PascalCase con `_` si compuesto | `Build_Sand`, `Tool_Destroy`, `Bonsai` |
| Boolean properties | `Is`/`Can`/`Has` + PascalCase | `IsGridActive`, `CanUndo`, `IsBuildTool` |

---

## 5. Materiales

**Prefijo obligatorio: `M_`**

| Patrón | Ejemplo |
|--------|---------|
| `M_{Nombre}` | `M_ARGround.mat` |
| `M_Block{Tipo}` | `M_BlockSand.mat`, `M_BlockStone.mat` |
| `M_{Sistema}{Nombre}` | `M_GridLines.mat` |

**Shader asignado obligatorio:**

| Material de bloque | Shader |
|-------------------|--------|
| `M_Block*.mat` | `ARmonia/Blocks/VoxelLit` |
| `M_ARGround*.mat` | `ARmonia/AR/ARPlane` |
| `M_GridLines.mat` | Vertex colour (sin textura) |

---

## 6. Prefabs

**Prefijo por categoría:**

| Categoría | Prefijo | Ejemplo |
|-----------|---------|---------|
| Bloques voxel | `Voxel_` | `Voxel_Sand.prefab`, `Voxel_Torch.prefab` |
| Piedras decorativas | `Pebble_` | `Pebble_Stone.prefab` |
| Elementos AR | `AR_` | `AR_Default_Plane.prefab`, `AR_RayInteractor.prefab` |
| Efectos visuales | `VFX_` | `VFX_BlockPlace.prefab`, `VFX_BlockBreak.prefab` |
| Elementos UI | `UI_` | `UI_HotbarSlot.prefab` |

**Componentes obligatorios por tipo de prefab:**

| Tipo | Componentes requeridos |
|------|----------------------|
| Bloque voxel (`Voxel_*`) | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` |
| Piedra (`Pebble_*`) | `ProceduralPebble` + `BlockDestroy` + `BlockSpawn` + `PebbleSupport` + `MeshCollider (convex)` |
| VFX place (`VFX_BlockPlace`) | `VFXBlockPlace` + `ParticleSystem` |
| VFX destroy (`VFX_BlockBreak`) | `VFXBlockDestroy` + `ParticleSystem` |

---

## 7. Texturas

**Prefijo por tipo:**

| Tipo | Prefijo | Ejemplo |
|------|---------|---------|
| Albedo / Diffuse | `T_` | `T_BlockSand_D.png` |
| Normal map | `T_` + sufijo `_N` | `T_BlockStone_N.png` |
| Mask (ORM) | `T_` + sufijo `_M` | `T_BlockWood_M.png` |
| UI icons | `Icon_` | `Icon_Destroy.png` |
| UI sprites | `UI_` | `UI_HotbarBg.png` |
| App icon | PascalCase sin prefijo | `ARmoniaIcon.png` |

**Regla pixel-art:** Todas las texturas de bloques usan
`Filter Mode = Point (no filter)` y `Compression = None` para mantener bordes
nítidos de pixel.

### Modelos 3D

| Regla | Ejemplo |
|-------|---------|
| Prefijo `Model_` + PascalCase | `Model_Glass.glb`, `Model_Stone.glb` |
| Ubicación: `_Project/Models/Blocks/` | No mezclar con texturas |
| Formato `.glb` (glTF Binary) | Unity importa mesh + texturas embebidas |

---

## 8. Audio

| Tipo | Prefijo | Ejemplo |
|------|---------|---------|
| Efectos de sonido | `SFX_` | `SFX_BlockPlace.mp3`, `SFX_MenuClick.mp3` |
| Música de fondo | `MUS_` o nombre original | `1-13. Wet Hands.mp3` |
| Voz / Narración | `VO_` | `VO_Tutorial01.mp3` |

**Organización de subcarpetas:**

| Carpeta | Contenido |
|---------|-----------|
| `Audio/Music/` | Pistas de fondo (12 tracks) |
| `Audio/SFX/UI/` | Sonidos de interfaz (5 clips) |
| `Audio/SFX/Voxels/` | Sonidos de bloques y herramientas (23 clips) |

---

## 9. Escenas

| Regla | Ejemplo |
|-------|---------|
| PascalCase con contexto | `Main_AR.unity` |
| Pantalla de menú | `Menu_Main.unity` |
| Pantalla de título | `Title_FaceTrack.unity` |
| Pantalla de carga | `Loading.unity` |
| Escena de test | `Test_GridManager.unity` |

---

## 10. Shaders

| Regla | Ejemplo |
|-------|---------|
| Ruta en menú de shader | `Shader "ARmonia/{Carpeta}/{Nombre}"` |
| Archivo en `_Project/Shaders/` | `ARPlane.shader`, `VoxelLit.shader` |
| Nombre del pass | `Name "ForwardLit"`, `Name "ARPlaneOverlay"` |

**Shaders actuales:**

| Shader | Ruta | Descripción |
|--------|------|-------------|
| `ARmonia/AR/ARPlane` | `ARPlane.shader` | Arena zen con grid animado, 5 tonos, pulse/shimmer, sombras, transparente. |
| `ARmonia/Blocks/VoxelLit` | `VoxelLit.shader` | Toon-lit 3 bandas, point-sampled, vertex AO, emisión, fog, shadow caster + depth. |

**Reglas para shaders:**
- Usar `CBUFFER_START(UnityPerMaterial)` para compatibilidad con SRP Batcher.
- Incluir `#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE`.
- Incluir `#pragma multi_compile _ _SHADOWS_SOFT`.
- Incluir `#pragma multi_compile_fog` si el shader soporta fog.
- Usar `MaterialPropertyBlock` en vez de material instances para propiedades per-object en runtime.

---

## 11. ScriptableObjects

| Regla | Ejemplo |
|-------|---------|
| `CreateAssetMenu` con ruta `ARmonia/` | `[CreateAssetMenu(menuName = "ARmonia/Core/Harmony Config")]` |
| Nombre de archivo = `{Tipo}` o `{Tipo}_{Variante}` | `BlockDatabase.asset`, `WorldModeConfig_Normal.asset` |
| Ubicación | `_Project/Assets/` |

**ScriptableObjects actuales:**

| Asset | Clase | Campos principales |
|-------|-------|--------------------|
| `BlockDatabase.asset` | `BlockDatabase` | `_entries[]` (BlockType → prefab) |
| `HarmonyConfig.asset` | `HarmonyConfig` | Pesos, umbrales, gate mínimos |
| `WorldModeConfig_Bonsai.asset` | `WorldModeSO` | Scale 0.02, TrackedImage |
| `WorldModeConfig_Normal.asset` | `WorldModeSO` | Scale 0.10, ARPlane |
| `WorldModeConfig_Real.asset` | `WorldModeSO` | Scale 1.00, ARPlane |

---

## 12. Rendimiento — target S24 Ultra

Estas reglas son obligatorias para mantener 60fps estables en AR móvil:

| Regla | Por qué | Cómo |
|-------|---------|------|
| **No `GetComponent` en `Update()`** | Presión GC + CPU spike por frame | Cachear en `Awake()` o `Start()` |
| **No `Find()` / `FindObjectOfType()`** | Scan O(n) cada llamada | Usar `[SerializeField]` en Inspector |
| **No allocation en hot paths** | GC stutter en móvil | Reutilizar `List<>`, `HashSet<>`, `MaterialPropertyBlock`, `WaitForSeconds` |
| **UI event-driven** | Polling gasta batería + CPU | Usar `event Action<T>` → suscribir en `OnEnable`, desuscribir en `OnDisable` |
| **No concatenar `string` en `Update()`** | `StringBuilder` oculto alloc | Usar interpolated strings solo en `Debug.Log` (stripped en Release) |
| **Cachear `Camera.main`** | Internamente llama `FindObjectWithTag` | Guardar en `Awake()`: `_mainCamera = Camera.main` |
| **Usar `sqrMagnitude`** | Evita `sqrt` por frame | `if (sqrDist <= radius * radius)` |
| **HashSet para celdas pendientes** | Lookup O(1) contra double-tap | `_pendingCells` en `ARBlockPlacer` |
| **Shader IDs estáticos** | `Shader.PropertyToID` es costoso | `static readonly int ID = Shader.PropertyToID("_Name")` |
| **Coroutine yields cacheados** | `new WaitForSeconds` = alloc | `private readonly WaitForSeconds _wait = new WaitForSeconds(0.5f)` |
| **No `Instantiate`/`Destroy` masivo** | GC spikes | Pool VFX prefabs (pendiente de implementar) |

---

## 13. Estética URP

| Regla | Configuración |
|-------|---------------|
| **Texturas pixel-art** | `Filter Mode = Point (no filter)`, `Compression = None` |
| **Sombras suaves** | URP Asset → Shadows → Soft Shadows = ON |
| **Resolución de sombras** | Mínimo 1024 para sombras nítidas |
| **Shader de bloques** | `ARmonia/Blocks/VoxelLit` con toon lighting 3 bandas |
| **Shader de suelo AR** | `ARmonia/AR/ARPlane` con arena zen + grid animado |
| **MaterialPropertyBlock** | Usar en vez de material instances para propiedades per-object |
| **Oclusión por profundidad** | `AROcclusionManager` via `ARDepthService` (default OFF) |
| **Pipeline Assets** | `Mobile_RPAsset` para Android, `PC_RPAsset` para Editor |
| **Fuente** | `minecraft_fot_esp` (con caracteres españoles) vía TextMeshPro SDF |

---

## 14. Patrones de comunicación

| Patrón | Uso | Ejemplo real |
|--------|-----|-------------|
| **C# Events (`event Action<T>`)** | Notificación cross-sistema, UI reactiva | `HarmonyService.OnHarmonyChanged` → `HarmonyHUD.SetHarmony` |
| **Llamada directa** | Acoplamiento estrecho dentro de la misma capa | `ARBlockPlacer` → `GridManager.GetSnappedPosition()` |
| **Inspector `[SerializeField]`** | Inyección de dependencias para todos los MonoBehaviours | Toda sección `#region Inspector` |
| **Command Pattern** | Undo/Redo | `IUndoableAction` → `PlaceBlockAction` / `DestroyBlockAction` |
| **Facade Pattern** | Simplificar acceso a subsistema | `GridManager` envuelve `GridVisualizer` |
| **ScriptableObject data** | Configuración compartida sin dependencia de escena | `BlockDatabase`, `HarmonyConfig`, `WorldModeSO` |
| **Static context** | Dato cross-escena sin singletons | `WorldModeContext.Selected` |
| **Auto-locate en Awake** | Componentes del mismo GO o jerarquía cercana | `PerfectHarmonyPanel` auto-localiza `CanvasGroup`, `HarmonyParticles`, `UIAudioService` |
| **InjectSharedRefs** | Inyección post-instantiate para evitar duplicar refs en prefabs | `BlockDestroy.InjectSharedRefs(vfx, audio)` |
| **OnClick directo** | Botones de modo toggle que no son herramientas | `Btn_Brush.OnClick → BrushTool.ToggleBrush()` |

**Patrones prohibidos:**
| Prohibido | Razón |
|-----------|-------|
| Singleton MonoBehaviour (`Instance` pattern) | Acoplamiento global, difícil de testear |
| `SendMessage()` / `BroadcastMessage()` | Lento, sin type-safety, sin refactoring |
| Tags para lógica | Usar `GetComponent<T>()` en vez de `CompareTag()` |
| `static` mutable en MonoBehaviours | Solo permitido en `WorldModeContext` (static class pura) |
| Rutar toggles de modo por `ToolManager` | `BrushTool` es un mode overlay — su botón llama directamente a `ToggleBrush()` |

---

## 15. Reglas generales

### Obligatorio

- Todos los nombres en **inglés** — no español en nombres de escena, scripts ni assets.
- Sin espacios en nombres de assets o carpetas.
- Sin caracteres especiales (solo letras, números, `_`).
- Cada asset tiene su `.meta` — **nunca mover ni renombrar assets fuera de Unity Editor**.
- Carpetas vacías confirmadas con `.gitkeep`.
- Bloques en `Prefabs/Blocks/`, VFX en `Prefabs/VFX/`.
- Scripts AR en `Scripts/AR/`, no mezclados con Core o Interaction.
- Cada `[SerializeField]` lleva `[Tooltip]`.
- Cada grupo de `[SerializeField]` lleva `[Header]`.
- Cada MonoBehaviour tiene `ValidateReferences()` llamado en `Start()`.
- Cachear yield objects de coroutines como campos.
- Usar `#region` siguiendo el orden de la plantilla (Sección 3).
- Usar `[DisallowMultipleComponent]` en cada MonoBehaviour.
- Usar `[AddComponentMenu("ARmonia/...")]` en cada MonoBehaviour.
- Cada clase tiene XML `<summary>` doc comment.
- Cada `using` no utilizado se elimina.

### Prohibido

- `GetComponent` en `Update()` — cachear en `Awake()`.
- `Find()` o `FindObjectOfType()` — usar `[SerializeField]`.
- Allocations en hot paths (`Update`, loops de coroutine) — reutilizar buffers.
- Hardcodear magic numbers — usar `[SerializeField]` o `const`.
- Duplicar referencias de prefabs en múltiples scripts — usar `InjectSharedRefs()`.
- Dejar `Debug.Log` en release sin condicional (Unity los stripea automáticamente en builds no-Development, pero mantener limpio).

### Mantenimiento de documentación

Cada vez que se realice un cambio en el proyecto (nuevo script, nuevo prefab,
nueva carpeta, renombrado de asset, cambio de jerarquía de escena, nuevo
paquete, etc.) se deben actualizar **los dos archivos de documentación** para
que sigan reflejando el estado real del proyecto:

- **`README.md`** — actualizar la sección correspondiente: estructura de
  carpetas, inventario de assets, lista de scripts, estado del proyecto,
  dependencias o cualquier otra sección afectada por el cambio.
- **`CONVENTIONS.md`** — actualizar la sección correspondiente: jerarquía de
  escena, mapa Script-GameObject, reglas de nombrado o cualquier otra sección
  afectada por el cambio.

**La documentación desactualizada se considera un bug igual que el código roto.**
