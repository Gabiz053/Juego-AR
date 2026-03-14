# Convenciones del proyecto ARmonia
# Convenciones del proyecto ARmonia

Documento de referencia para mantener consistencia en todo el proyecto.
**Cada nuevo asset, script o carpeta debe seguir estas reglas.**

> **Última auditoría:** 60 scripts · 2 escenas · 17 prefabs · 6 ScriptableObjects ·
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
│   ├── Title_Screen.unity    ← Pantalla de inicio (selección de modo, face tracking)
│   └── Main_AR.unity         ← Escena principal de juego
├── Scripts/
│   ├── AR/                  ← Gestión AR: ancla, planos, profundidad, modos
│   ├── Core/                ← Grid, armonía, audio, iluminación, undo/redo, reset, screenshot, datos de modo, transiciones de escena
│   ├── Infrastructure/      ← ServiceLocator, EventBus, GameEvents, domain enums (BlockType, ToolType), service interfaces (IGameAudioService, IHapticService, etc.), IUndoableAction — arquitectura base
│   ├── Interaction/         ← Input táctil, herramientas, colocación/destrucción, debug ray
│   ├── Title/               ← Pantalla de inicio: face tracking, hand tracking, selección de modo, animación de logo
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
│   ├── Svc_Audio                          [Empty — agrupa servicios de audio y hápticos]
│   │       GameAudioService + AudioSource (SFX)
│   │       MusicService + AudioSource (Music)
│   │       HapticService
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
│   │       ├── Btn_Vibration              [DropdownButtonState] → Txt_Vibration
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
│   ├── HUD_ScreenshotFlash                [Image (blanco), CanvasGroup — flash de captura, desactivado por defecto]
│   │
│   ├── Popup_ScreenshotToast              [ScreenshotToastPanel, CanvasGroup — desactivado por defecto]
│   │   └── Panel_ToastCard               [Image — card centrada]
│   │       ├── Img_Preview                [RawImage — thumbnail de la captura]
│   │       ├── Txt_ToastMessage           [TMP_Text — "📷 Foto guardada"]
│   │       └── Btn_Accept     → Txt_Accept
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
| `HapticService` | Svc_Audio | — |
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
| `ScreenshotToastPanel` | Popup_ScreenshotToast | `CanvasGroup` |
| `ButtonPressAnimation` | Cada `Btn_*` | `Button` |
| `DropdownButtonState` | `Btn_Lighting`, `Btn_Depth`, `Btn_Grid`, `Btn_Plane`, `Btn_Vibration` | — |

### `Title_Screen.unity` — jerarquía completa

La escena de inicio utiliza la cámara frontal con Face Tracking para el filtro
de Creeper y Hand Tracking (MediaPipe, GPU delegate) para seleccionar el modo de
juego con el dedo índice mediante **pinch click** (gesto de pellizco instantáneo)
o **dwell time** (1s como fallback). El logo "ARMONIA" flota con bobbing vertical.
La transición a `Main_AR` usa `SceneTransitionService` (fade-to-black).

```text
AR System                                  [Empty — agrupa objetos AR]
├── XR Origin (Front Camera)               [XROrigin, XRInputModalityManager,
│   │                                       ARFaceManager (maxFaces 1), CreeperFaceFilter,
│   │                                       HandTrackingService,
│   │                                       ARPlaneManager (disabled), ARRaycastManager (disabled)]
│   └── Camera Offset
│       └── Main Camera                    [Camera, AudioListener, TrackedPoseDriver,
│                                           ARCameraManager (User facing), ARCameraBackground,
│                                           UniversalAdditionalCameraData]
├── XR Interaction Manager                 [XRInteractionManager]
├── AR Session                             [ARSession, ARInputManager]
└── Directional Light                      [Light (Directional), UniversalAdditionalLightData]

UI System                                  [Empty — agrupa objetos UI]
├── TitleCanvas                            [Canvas (Overlay, order 10), CanvasScaler (1080×2400, match 0.5),
│   │                                       GraphicRaycaster, TitleSceneManager, DwellSelector]
│   ├── Txt_Title                          [TMP_Text — "ARMONIA", font: minecraft_fot_esp SDF, size 72, bold, blanco,
│   │                                       TitleLogoAnimator]
│   ├── Btn_Bonsai                         [Button → SelectMode(0), Image]
│   │   └── Txt_Bonsai                     [TMP_Text — "Bonsai", font: Minecraft SDF, size 60, gris oscuro]
│   ├── Btn_Normal                         [Button → SelectMode(1), Image]
│   │   └── Txt_Normal                     [TMP_Text — "Normal", font: Minecraft SDF, size 60, gris oscuro]
│   ├── Btn_Real                           [Button → SelectMode(2), Image]
│   │   └── Txt_Real                       [TMP_Text — "Real", font: Minecraft SDF, size 60, gris oscuro]
│   └── HandCursor                         [RectTransform, CanvasGroup, HandCursorUI]
│       ├── Img_CursorDot                  [Image — 40×40 white circle, raycastTarget OFF]
│       └── Img_DwellProgress              [Image — 60×60 radial fill ring, Fill Method Radial360]
└── EventSystem                            [InputSystemUIInputModule, EventSystem]
```

### Mapa Script → GameObject (Title_Screen)

| Script | GameObject host | RequireComponent |
|--------|----------------|------------------|
| `TitleSceneManager` | TitleCanvas | — |
| `CreeperFaceFilter` | XR Origin (Front Camera) | — |
| `HandTrackingService` | XR Origin (Front Camera) | — |
| `HandCursorUI` | HandCursor | — |
| `DwellSelector` | TitleCanvas | — |
| `TitleLogoAnimator` | Txt_Title | — |

### Wiring de botones — Title_Screen

| Botón | OnClick destino | Parámetro |
|-------|----------------|-----------|
| `Btn_Bonsai` | `TitleSceneManager.SelectMode(int)` | `0` |
| `Btn_Normal` | `TitleSceneManager.SelectMode(int)` | `1` |
| `Btn_Real` | `TitleSceneManager.SelectMode(int)` | `2` |

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
| `Txt_` | Labels de TextMesh Pro | `Txt_Sand`, `Txt_HarmonyStatus`, `Txt_ConfirmMessage` |
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
| `Scripts/Infrastructure/` | `_Project.Scripts.Infrastructure` |
| `Scripts/Interaction/` | `_Project.Scripts.Interaction` |
| `Scripts/Title/` | `_Project.Scripts.Title` |
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
| Formato de `ValidateReferences()` | Siempre `Debug.LogWarning("[Clase] _campo is not assigned.", this);` — ver subsección abajo |
| XML `<summary>` en toda clase pública | `/// <summary>Gestiona la rejilla de construcción.</summary>` |
| Yield cacheados como campo | `private readonly WaitForSeconds _wait = new WaitForSeconds(0.5f);` |
| No dejar `using` sin usar | Limpiar imports |
| `Debug.Log` en decisiones clave | `Debug.Log($"[ClassName] Action -- context.");` |

### Convención de Debug.Log

Cada servicio y controlador incluye `Debug.Log` **estratégicos** en puntos de
decisión importantes para facilitar el diagnóstico en consola:

| Cuándo | Ejemplo |
|--------|---------|
| Inicialización de servicio | `Debug.Log($"[HarmonyService] Initialized -- score: {_lastScore:F2}.");` |
| Cambio de estado relevante | `Debug.Log($"[ToolManager] Tool changed to {CurrentTool}.");` |
| Evento de juego importante | `Debug.Log("[HarmonyService] *** PERFECT HARMONY REACHED ***");` |
| Toggle ON/OFF | `Debug.Log($"[BrushTool] Brush {(IsBrushActive ? "ON" : "OFF")}.");` |
| Operación destructiva | `Debug.Log($"[WorldResetService] World reset complete -- destroyed {count} objects.");` |

**Formato obligatorio:** `[ClassName] Message -- context.`
**No añadir** `Debug.Log` en hot paths (`Update`, loops de coroutine) ni en
métodos llamados cada frame.  Unity los stripea automáticamente en builds no-Development.

### Convención de ValidateReferences()

Cada MonoBehaviour tiene un método `ValidateReferences()` llamado desde `Start()`.
Este método valida que todas las dependencias (campos, servicios, componentes) se
hayan resuelto correctamente.  **Formato único obligatorio:**

```csharp
#region Validation ----------------------------------------

private void ValidateReferences()
{
    if (_fieldName == null)
        Debug.LogWarning("[ClassName] _fieldName is not assigned.", this);
}

#endregion
```

**Reglas:**

| Regla | Correcto | Incorrecto |
|-------|----------|------------|
| Nivel de log | `Debug.LogWarning` | `Debug.LogError` |
| Nombre del campo | `_fieldName` (nombre real del campo privado) | `FieldName`, `"No Collider found"` |
| Mensaje | `"[Clase] _campo is not assigned."` | `"[Clase] _campo not found!"`, `"[Clase] _campo is not assigned!"` |
| Puntuación | Punto final `.` | Exclamación `!` |
| Contexto `this` | Siempre `this` como segundo argumento | Sin contexto |
| `OnValidate()` | **Prohibido** — no usar `#if UNITY_EDITOR` + `OnValidate` | `#if UNITY_EDITOR private void OnValidate() ...` |
| Arrays vacíos | `if (_arr == null \|\| _arr.Length == 0)` | Solo `_arr == null` |
| Componentes vía GetComponent | `"[Clase] ComponentType is not assigned."` | `"[Clase] ComponentType not found!"` |

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
| Bloque arena (`Voxel_Sand`) | Igual que `Voxel_*` + `SandGravity` (gravedad post-spawn) |
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

### Vibración háptica

**Plugin:** [Vibration](https://github.com/BenoitFreslon/Vibration) (Benoit Freslon) — paquete UPM.

**Servicio central:** `HapticService` en `Svc_Audio`.  
Empieza **desactivado** — el usuario lo activa desde `Btn_Vibration` en el dropdown.

| Preset | Método | Duración | Uso |
|--------|--------|----------|-----|
| Light (Pop) | `VibrateLight()` | ≈ 50 ms | UI taps, colocar bloque, foto |
| Medium (Peek) | `VibrateMedium()` | ≈ 100 ms | Destruir bloque |
| Heavy (Nope) | `VibrateHeavy()` | Patrón triple | Fases altas de armonía, armonía perfecta |

**Patrón de integración:** los scripts llaman `_hapticService?.VibrateX()`.
`UIAudioService` incluye vibración en todos los métodos `Play*()` (excepto `PlayPhoto`, que la maneja `ScreenshotService`).

---

## 9. Escenas

| Regla | Ejemplo |
|-------|---------|
| PascalCase con contexto | `Main_AR.unity` |
| Pantalla de título | `Title_Screen.unity` |
| Pantalla de menú | `Menu_Main.unity` |
| Pantalla de carga | `Loading.unity` |
| Escena de test | `Test_GridManager.unity` |

### Escenas actuales

| Escena | Build Index | Descripción |
|--------|-------------|-------------|
| `Title_Screen.unity` | 0 | Pantalla de inicio: cámara frontal, Face Tracking + Creeper, Hand Tracking + pinch click, selección de modo (Bonsai/Normal/Real), logo animado. |
| `Main_AR.unity` | 1 | Escena principal de juego: cámara trasera, AR planes/images, construcción voxel. |

### Flujo entre escenas

```text
Title_Screen                               Main_AR
┌─────────────────────┐  TransitionTo  ┌──────────────────────┐
│ TitleSceneManager   │ ─────────────→ │ WorldModeBootstrapper │
│ SelectMode(int)     │  (fade-black)  │ Awake: lee contexto,  │
│ → WorldModeContext  │                │   aplica escala       │
│ → SceneTransition   │                │ Start: corrutina      │
│   Service           │  TransitionTo  │   espera ARSession →  │
└─────────────────────┘ ←──────────── │   configura managers  │
                         GameOptions   └──────────────────────┘
                         Menu.ExitGame()
                          (fade-black)
```

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
| `WorldModeConfig_Bonsai.asset` | `WorldModeSO` | Scale 0.02, TrackedImage, ImageLibrary → `ReferenceImageLibrary.asset` |
| `WorldModeConfig_Normal.asset` | `WorldModeSO` | Scale 0.10, ARPlane |
| `WorldModeConfig_Real.asset` | `WorldModeSO` | Scale 1.00, ARPlane |
| `ReferenceImageLibrary.asset` | `XRReferenceImageLibrary` | 2 imágenes: `one` (0.13×0.13m), `qr_prueba` (0.10×0.10m), ambas con SpecifySize ON |

---

## 12. Rendimiento — target S24 Ultra

Estas reglas son obligatorias para mantener 60fps estables en AR móvil:

| Regla | Por qué | Cómo |
|-------|---------|------|
| **No `GetComponent` en `Update()`** | Presión GC + CPU spike por frame | Cachear en `Awake()` o `Start()` |
| **No `Find()` / `FindObjectOfType()`** | Scan O(n) cada llamada | Usar `[SerializeField]` en Inspector o `ServiceLocator.TryGet<T>()` para prefabs |
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
| **Oclusión por profundidad** | `AROcclusionManager` via `ARDepthService` (default OFF, modo Best al activar) |
| **Pipeline Assets** | `Mobile_RPAsset` para Android, `PC_RPAsset` para Editor |
| **Fuente** | `minecraft_fot_esp` (con caracteres españoles) vía TextMeshPro SDF |

---

## 14. Patrones de comunicación

| Patrón | Uso | Ejemplo real |
|--------|-----|-------------|
| **Service Locator** | Resolución de dependencias por interfaz sin singletons ni `Find*` | `ServiceLocator.Register<IGameAudioService>(this)` en `Awake`; `ServiceLocator.TryGet<IGameAudioService>(out _audioService)` en consumidor |
| **EventBus (Pub/Sub tipado)** | Comunicación cross-sistema sin referencias directas | `EventBus.Publish(new BlockPlacedEvent(cell, id))` → cualquier suscriptor recibe el evento sin conocer al publisher |
| **C# Events (`event Action<T>`)** | Notificación intra-capa, UI reactiva | `IHarmonyService.OnHarmonyChanged` → `HarmonyHUD.SetHarmony` |
| **Llamada directa** | Acoplamiento estrecho dentro de la misma capa | `ARBlockPlacer` → `IGridManager.GetSnappedPosition()` |
| **Inspector `[SerializeField]`** | Inyección de dependencias para MonoBehaviours de escena | Toda sección `#region Inspector` en scripts de escena |
| **Command Pattern** | Undo/Redo | `IUndoableAction` → `PlaceBlockAction` / `DestroyBlockAction` |
| **Facade Pattern** | Simplificar acceso a subsistema | `GridManager` envuelve `GridVisualizer` |
| **ScriptableObject data** | Configuración compartida sin dependencia de escena | `BlockDatabase`, `HarmonyConfig`, `WorldModeSO` |
| **Static context** | Dato cross-escena sin singletons | `WorldModeContext.Selected` |
| **Internal static helper** | Lógica compartida entre Commands sin estado | `PlaceBlockAction.ArmForImmediate()` — deshabilita BlockSpawn, habilita Collider + BlockDestroy.SetReady(). Usado por `Redo` (place) y `Undo` (destroy). |
| **ServiceLocator.TryGet en Awake** | Servicios de escena desde prefabs instanciados dinámicamente | `BlockSpawn`, `BlockDestroy`, `SandGravity` resuelven interfaces (`IGameAudioService`, `IHapticService`, `IToolManager`, etc.) via `ServiceLocator.TryGet<T>()` en `Awake` |
| **Prefab-owns-feedback** | Audio y VFX viven en el prefab, no en el caller | `BlockSpawn` reproduce place sounds/VFX; `BlockDestroy` reproduce break sounds/VFX. Callers no tocan audio ni VFX. |
| **VoxelBlock como fuente de audio** | Prefab data-component leído por sibling components | `BlockSpawn` lee `VoxelBlock.PlaceSounds`; `BlockDestroy` lee `VoxelBlock.BreakSounds`. Fallback a campos propios para pebbles (sin `VoxelBlock`). |
| **OnClick directo** | Botones de modo toggle que no son herramientas | `Btn_Brush.OnClick → BrushTool.ToggleBrush()` |
| **OnClick con int param** | Selección indexada desde botones UI | `Btn_Bonsai.OnClick → TitleSceneManager.SelectMode(0)` |
| **Scene transition** | Carga de escena con fade y dato estático pre-escrito | `TitleSceneManager` escribe `WorldModeContext.Selected`, luego `ServiceLocator.TryGet<ISceneTransitionService>().TransitionTo("Main_AR")` (fade-to-black → async load → fade-in). `WorldModeBootstrapper` lee el modo en `Awake()` y difiere la activación de AR managers a una corrutina en `Start()` que espera `ARSession.state >= SessionInitializing` para evitar race conditions. `GameOptionsMenu.ExitGame()` transiciona a `Title_Screen` via ServiceLocator. |
| **DontDestroyOnLoad con ServiceLocator** | Servicio cross-escena registrado en ServiceLocator | `SceneTransitionService`: se registra como `ISceneTransitionService` en `Awake`, persiste entre escenas via `DontDestroyOnLoad`, guard `ServiceLocator.IsRegistered<T>()` para evitar duplicados. Canvas overlay propio (sort order 999). |
| **Pinch gesture detection** | Detección de gesto por distancia de landmarks | `HandTrackingService` mide distancia entre thumb tip (#4) e index tip (#8). Histéresis (enter 0.055, exit 0.08) + debounce (2 frames). `DwellSelector` escucha `OnPinchDetected` para selección instantánea de botón. |
| **Sand gravity (poll continuo)** | Gravedad selectiva por tipo de bloque | `SandGravity` (solo en `Voxel_Sand`): espera `BlockDestroy.IsReady` + 0.15s, luego `InvokeRepeating` cada 0.15s. Si no hay soporte → cae animado (ease-in) hasta grid válido. Tras aterrizar reinicia el poll para reaccionar a bloques destruidos. Desactiva collider/`BlockDestroy` durante caída (mismo patrón que `BlockSpawn`). |

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
- Cada MonoBehaviour tiene `ValidateReferences()` llamado en `Start()` — formato estandarizado (ver Sección 3).
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
- Duplicar referencias de escena en prefabs — usar `FindAnyObjectByType<T>()` en `Awake` o `[SerializeField]` para datos de prefab.
- Dejar `Debug.Log` en release sin condicional (Unity los stripea automáticamente en builds no-Development, pero mantener limpio).
- `OnValidate()` en MonoBehaviours — usar únicamente `ValidateReferences()` llamado desde `Start()`. Excepción: `ScriptableObject` puede usar `OnValidate()` para validar datos de asset (e.g. entradas duplicadas).
- `Debug.LogError` dentro de `ValidateReferences()` — siempre usar `Debug.LogWarning` (ver convención en Sección 3).

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

