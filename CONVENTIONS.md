<!-- markdownlint-disable MD013 MD060 -->

# Convenciones del proyecto ARmonia

Referencia obligatoria para mantener consistencia. **Todo asset, script o carpeta nuevo debe cumplir estas reglas.**

> **Ultima auditoria:** 75 scripts - 2 escenas - 21 prefabs - 6 ScriptableObjects -
> 2 shaders - 8 materiales - 46 clips de audio - 28 texturas - 7 modelos 3D - 5 fuentes

---

## Tabla de contenidos

1. [Nombrado de assets](#1-nombrado-de-assets)
2. [Codigo C#](#2-codigo-c)
3. [Variables y campos C#](#3-variables-y-campos-c)
4. [Jerarquia de escenas](#4-jerarquia-de-escenas)
5. [Patrones de arquitectura](#5-patrones-de-arquitectura)
6. [Rendimiento -- target S24 Ultra](#6-rendimiento----target-s24-ultra)
7. [Estetica URP](#7-estetica-urp)
8. [Reglas generales](#8-reglas-generales)

---

## 1. Nombrado de assets

### 1.1 Carpetas

| Regla | Correcto | Incorrecto |
|-------|----------|------------|
| PascalCase | `Scripts/` | `scripts/` |
| Plural | `Materials/`, `Prefabs/` | `Material/`, `Prefab/` |
| Sin espacios | `XR/` | `My Folder/` |
| Raiz del proyecto | `_Project/` (con `_` para ordenar primero) | `Project/` |
| Carpetas vacias | Confirmar con `.gitkeep` | Dejar vacia (Git la ignora) |

### 1.2 GameObjects

| Prefijo | Uso | Ejemplos |
|---------|-----|----------|
| *(ninguno)* | Objetos estandar de Unity | `AR Session`, `Main Camera`, `EventSystem` |
| `HUD_` | Regiones persistentes de pantalla | `HUD_Hotbar`, `HUD_Harmony`, `HUD_UndoRedo` |
| `Pnl_` | Paneles contenidos en una seccion | `Pnl_OptionsDropdown`, `Pnl_ConfirmDialog` |
| `Popup_` | Modales fullscreen | `Popup_ConfirmClearAll`, `Popup_ScreenshotToast`, `Popup_SaveGarden`, `Popup_BonsaiSelector` |
| `Overlay_` | Fondos oscuros/transparentes | `Overlay_Background` |
| `Img_` | Imagenes y barras de progreso | `Img_BarBackground`, `Img_BarFill`, `Img_Preview` |
| `Btn_` | Botones (contiene hijo `Txt_` o `Icon_`) | `Btn_Sand`, `Btn_Settings`, `Btn_Confirm` |
| `Txt_` | Labels de TextMesh Pro | `Txt_Sand`, `Txt_HarmonyStatus` |
| `Icon_` | Imagenes de icono dentro de botones | `Icon_Undo`, `Icon_Sand`, `Icon_Settings` |
| `Sld_` | Sliders | `Sld_MusicVolume` |
| `*_LayoutGroup` | Objetos con LayoutGroup component | `Hotbar_LayoutGroup`, `Dialog_LayoutGroup` |
| `Svc_` | GameObjects de servicio (sin visual) | `Svc_Audio`, `Svc_Interaction`, `Svc_WorldReset`, `Svc_SaveLoad`, `Svc_BonsaiSession` |
| PascalCase | Singletons / contenedores | `WorldContainer`, `ToolManager`, `MainCanvas` |

**Reglas obligatorias:**

- Cada `Btn_X` debe contener al menos un hijo `Txt_X` o `Icon_X` con el mismo sufijo.
- Todos los nombres en **ingles**. No espanol en la jerarquia.
- Sin espacios en nombres propios. Usar `_` para separar prefijo de nombre.
- Los objetos estandar de Unity mantienen su nombre por defecto (`AR Session`, `Directional Light`, etc.).

### 1.3 Materiales

Prefijo obligatorio: **`M_`**

| Patron | Ejemplo |
|--------|---------|
| `M_{Nombre}` | `M_ARGround.mat` |
| `M_Block{Tipo}` | `M_BlockSand.mat`, `M_BlockStone.mat` |
| `M_{Sistema}{Nombre}` | `M_GridLines.mat` |

**Shader asignado:**

| Material | Shader |
|----------|--------|
| `M_Block*.mat` | `ARmonia/Blocks/VoxelLit` |
| `M_ARGround*.mat` | `ARmonia/AR/ARPlane` |
| `M_GridLines.mat` | Vertex colour (sin textura) |

### 1.4 Prefabs

| Categoria | Prefijo | Ejemplo |
|-----------|---------|---------|
| Bloques voxel | `Voxel_` | `Voxel_Sand.prefab`, `Voxel_Torch.prefab` |
| Piedras decorativas | `Pebble_` | `Pebble_Stone.prefab` |
| Objetos 3D | `Object_` | `Object_Creeper.prefab`, `Object_Frog.prefab` |
| Elementos AR | `AR_` | `AR_Default_Plane.prefab` |
| Efectos visuales | `VFX_` | `VFX_BlockPlace.prefab`, `VFX_BlockBreak.prefab` |
| Elementos UI | `UI_` | `UI_HotbarSlot.prefab`, `UI_GardenListItem.prefab` |

**Componentes obligatorios por tipo de prefab:**

| Tipo | Componentes requeridos |
|------|------------------------|
| Bloque voxel (`Voxel_*`) | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` |
| Bloque arena (`Voxel_Sand`) | Igual que `Voxel_*` + `SandGravity` |
| Piedra (`Pebble_*`) | `ProceduralPebble` + `BlockDestroy` + `BlockSpawn` + `PebbleSupport` + `MeshCollider (convex)` |
| VFX place (`VFX_BlockPlace`) | `VFXBlockPlace` + `ParticleSystem` |
| VFX destroy (`VFX_BlockBreak`) | `VFXBlockDestroy` + `ParticleSystem` |

### 1.5 Texturas y modelos 3D

**Texturas:**

| Tipo | Prefijo | Ejemplo |
|------|---------|---------|
| Albedo / Diffuse | `T_` | `T_BlockSand_D.png` |
| Normal map | `T_` + sufijo `_N` | `T_BlockStone_N.png` |
| Mask (ORM) | `T_` + sufijo `_M` | `T_BlockWood_M.png` |
| UI icons | `Icon_` | `Icon_Destroy.png` |
| UI sprites | `UI_` | `UI_HotbarBg.png` |
| App icon | PascalCase sin prefijo | `ARmoniaIcon.png` |
| Variantes de icono | Sin prefijo, en `Textures/Images/` | `ARmoniaIcon_bw.jpg` |

**Regla pixel-art:** todas las texturas de bloques usan `Filter Mode = Point (no filter)` y `Compression = None` para bordes nitidos.

**Modelos 3D:**

| Regla | Ejemplo |
|-------|---------|
| Prefijo `Model_` + PascalCase | `Model_Glass.glb`, `Model_Stone.glb` |
| Modelos sin nombre propio | `minecraft_creeper_head 1.glb`, `frog_fountain_minecraft_mob.glb` |
| Ubicacion bloques: `_Project/Models/Blocks/` | Meshes de bloques voxel |
| Ubicacion objetos: `_Project/Models/Things/` | Meshes decorativos (Creeper, Frog) |
| Formato `.glb` (glTF Binary) | Unity importa mesh + texturas embebidas |

### 1.6 Audio

| Tipo | Prefijo | Ejemplo |
|------|---------|---------|
| Efectos de sonido | `SFX_` | `SFX_BlockPlace.mp3`, `SFX_MenuClick.mp3` |
| Musica de fondo | `MUS_` o nombre original | `1-13. Wet Hands.mp3` |
| Voz / Narracion | `VO_` | `VO_Tutorial01.mp3` |

**Subcarpetas:**

| Carpeta | Contenido |
|---------|-----------|
| `Audio/Music/` | Pistas de fondo (12 tracks) |
| `Audio/SFX/UI/` | Sonidos de interfaz (8 clips) |
| `Audio/SFX/Voxels/` | Sonidos de bloques y herramientas (26 clips) |

### 1.7 Shaders

| Regla | Ejemplo |
|-------|---------|
| Ruta en menu de shader | `Shader "ARmonia/{Carpeta}/{Nombre}"` |
| Archivo en `_Project/Shaders/` | `ARPlane.shader`, `VoxelLit.shader` |
| Nombre del pass | `Name "ForwardLit"`, `Name "ARPlaneOverlay"` |

**Reglas tecnicas:**

- `CBUFFER_START(UnityPerMaterial)` para compatibilidad con SRP Batcher.
- `#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE` para sombras.
- `#pragma multi_compile _ _SHADOWS_SOFT` para sombras suaves.
- `#pragma multi_compile_fog` si soporta fog.
- `MaterialPropertyBlock` en vez de material instances para propiedades per-object en runtime.

### 1.8 ScriptableObjects

| Regla | Ejemplo |
|-------|---------|
| `CreateAssetMenu` con ruta `ARmonia/` | `[CreateAssetMenu(menuName = "ARmonia/Core/Harmony Config")]` |
| Nombre de archivo = `{Tipo}` o `{Tipo}_{Variante}` | `BlockDatabase.asset`, `WorldModeConfig_Normal.asset` |
| Ubicacion | `_Project/Assets/` |

**ScriptableObjects actuales:**

| Asset | Clase | Campos principales |
|-------|-------|--------------------|
| `BlockDatabase.asset` | `BlockDatabaseSO` | `_entries[]` (BlockType -> prefab) |
| `HarmonyConfig.asset` | `HarmonyConfig` | Pesos, umbrales, gate minimos |
| `WorldModeConfig_Bonsai.asset` | `WorldModeSO` | Scale 0.02, TrackedImage, ImageLibrary -> `ReferenceImageLibrary.asset` |
| `WorldModeConfig_Normal.asset` | `WorldModeSO` | Scale 0.10, ARPlane |
| `WorldModeConfig_Real.asset` | `WorldModeSO` | Scale 0.50, ARPlane |
| `ReferenceImageLibrary.asset` | `XRReferenceImageLibrary` | 2 imagenes: `one` (0.13x0.13m), `qr_prueba` (0.10x0.10m), SpecifySize ON |

### 1.9 Escenas

| Regla | Ejemplo |
|-------|---------|
| PascalCase con contexto | `Main_AR.unity` |
| Pantalla de titulo | `Title_Screen.unity` |
| Escena de test | `Test_GridManager.unity` |

---

## 2. Codigo C\#

### 2.1 Archivos y namespaces

| Regla | Ejemplo |
|-------|---------|
| PascalCase | `GridManager.cs` |
| Nombre de archivo = nombre de clase | `ARBlockPlacer.cs` -> `public class ARBlockPlacer` |
| Namespace sigue ruta de carpeta | `namespace _Project.Scripts.Core` |

**Namespaces del proyecto:**

| Carpeta | Namespace |
|---------|-----------|
| `Scripts/AR/` | `_Project.Scripts.AR` |
| `Scripts/Core/` | `_Project.Scripts.Core` |
| `Scripts/Infrastructure/` | `_Project.Scripts.Infrastructure` |
| `Scripts/Interaction/` | `_Project.Scripts.Interaction` |
| `Scripts/Title/` | `_Project.Scripts.Title` |
| `Scripts/UI/` | `_Project.Scripts.UI` |
| `Scripts/Voxel/` | `_Project.Scripts.Voxel` |

### 2.2 Atributos de clase

Cada MonoBehaviour lleva estos atributos **obligatorios**:

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

### 2.3 Orden de regiones

Cada script sigue este orden de `#region`:

```csharp
#region Constants              // private const, static readonly
#region Inspector              // [SerializeField] con [Header] y [Tooltip]
#region Events                 // public event Action<T>
#region Cached Components      // o "#region State" -- referencias y estado runtime
#region Public API             // Propiedades y metodos publicos
#region Unity Lifecycle        // Awake, OnEnable, Start, Update, LateUpdate, OnDisable, OnDestroy
#region Internals              // Metodos privados auxiliares
#region Validation             // ValidateReferences() llamado desde Start()
```

### 2.4 Reglas de estilo

| Regla | Ejemplo |
|-------|---------|
| Cada `[SerializeField]` lleva `[Tooltip]` | `[Tooltip("Desc.")] [SerializeField] private float _value;` |
| Cada grupo de campos lleva `[Header]` | `[Header("Dependencies")]` |
| XML `<summary>` en toda clase publica | `/// <summary>Gestiona la rejilla.</summary>` |
| Yield cacheados como campo | `private readonly WaitForSeconds _wait = new(0.5f);` |
| No dejar `using` sin usar | Limpiar imports |
| `Debug.Log` en decisiones clave | `Debug.Log($"[ClassName] Action -- context.");` |

### 2.5 Convencion de Debug.Log

Cada servicio y controlador lleva `Debug.Log` estrategicos en puntos clave para facilitar el diagnostico:

| Cuando | Ejemplo |
|--------|---------|
| Inicializacion de servicio | `Debug.Log($"[HarmonyService] Initialized -- score: {_lastScore:F2}.");` |
| Cambio de estado relevante | `Debug.Log($"[ToolManager] Tool changed to {CurrentTool}.");` |
| Evento de juego importante | `Debug.Log("[HarmonyService] *** PERFECT HARMONY REACHED ***");` |
| Toggle ON/OFF | `Debug.Log($"[BrushTool] Brush {(IsBrushActive ? "ON" : "OFF")}.");` |
| Operacion destructiva | `Debug.Log($"[WorldResetService] World reset complete -- destroyed {count} objects.");` |

**Formato:** `[ClassName] Message -- context.`

No meter `Debug.Log` en hot paths (`Update`, loops de coroutine). Unity los stripea automaticamente en builds no-Development.

### 2.6 Convencion de ValidateReferences()

Cada MonoBehaviour tiene un `ValidateReferences()` llamado desde `Start()` para comprobar que las dependencias se resolvieron. **Formato unico:**

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
| Mensaje | `"[Clase] _campo is not assigned."` | `"[Clase] _campo not found!"` |
| Puntuacion | Punto final `.` | Exclamacion `!` |
| Contexto `this` | Siempre `this` como segundo argumento | Sin contexto |
| `OnValidate()` | **Prohibido** en MonoBehaviours | Solo `ScriptableObject` puede usarlo |
| Arrays vacios | `if (_arr == null \|\| _arr.Length == 0)` | Solo `_arr == null` |
| GetComponent | `"[Clase] ComponentType is not assigned."` | `"[Clase] ComponentType not found!"` |

---

## 3. Variables y campos C\#

| Tipo | Convencion | Ejemplo |
|------|------------|---------|
| `[SerializeField]` private | `_camelCase` | `_gridManager`, `_audioService` |
| Private field | `_camelCase` | `_lastScore`, `_knocked` |
| Public property | PascalCase | `GridSize`, `IsWorldAnchored` |
| Constant (`const`) | `UPPER_SNAKE_CASE` | `MIN_FORWARD_SQR_MAG`, `RAY_DURATION` |
| Static readonly | `UPPER_SNAKE_CASE` | `GRID_MATRIX_ID`, `GRID_ENABLED_ID` |
| Local variable | `camelCase` | `snappedPosition`, `halfSize` |
| Method parameter | `camelCase` | `hitPose`, `playerCamera` |
| Public method | PascalCase | `AnchorWorld()`, `GetSnappedPosition()` |
| Private method | PascalCase | `OrientTowardsCamera()`, `Recalculate()` |
| Event (`Action`) | `On` + PascalCase | `OnToolChanged`, `OnHarmonyChanged` |
| Enum values | PascalCase con `_` si compuesto | `Build_Sand`, `Tool_Destroy`, `Bonsai` |
| Boolean properties | `Is`/`Can`/`Has` + PascalCase | `IsGridActive`, `CanUndo`, `IsBuildTool` |

---

## 4. Jerarquia de escenas

### 4.1 Main_AR.unity

La escena se organiza en **2 GameObjects raiz** como grupos logicos.

```text
AR System                                  [Empty -- agrupa objetos 3D/AR]
+-- AR Session                             [ARSession, ARInputManager,
|                                           ARPointCloudManager, AROcclusionManager]
+-- XR Interaction Manager                 [XRInteractionManager]
+-- XR Origin (Mobile AR)                  [XROrigin, ARSession, ARPlaneManager,
|   |                                       ARAnchorManager, ARRaycastManager,
|   |                                       ARTrackedImageManager, ARMeshManager,
|   |                                       ARWorldManager, ARDepthService,
|   |                                       ARPlaneGridAligner, WorldModeBootstrapper]
|   +-- Camera Offset
|   |   +-- Main Camera                    [Camera, AudioListener, TrackedPoseDriver,
|   |                                       ARCameraManager, ARCameraBackground,
|   |                                       UniversalAdditionalCameraData]
|   |       +-- CameraFlashLight           [Light (Spot), AudioSource]
|   +-- Svc_Audio                          [Empty -- agrupa servicios de audio y hapticos]
|   |       GameAudioService, MusicService, HapticService
|   +-- Svc_Interaction                    [Empty -- agrupa input y herramientas]
|   |       TouchInputRouter, ARBlockPlacer, BlockDestroyer,
|   |       BrushTool, PlowTool, DebugRayVisualizer + LineRenderer
|   +-- Svc_GameLogic                      [Empty -- agrupa logica de juego]
|   |       HarmonyService, UndoRedoService
|   +-- Svc_Lighting                       [Empty -- gestion de iluminacion]
|   |       LightingService
|   +-- Svc_BonsaiSession                  [BonsaiSessionController -- solo activo en Bonsai]
+-- WorldContainer                         [GridManager, GridVisualizer]
+-- ToolManager                            [ToolManager]
+-- Directional Light                      [Light (Directional), AudioSource,
                                            UniversalAdditionalLightData]

UI System                                  [Empty -- agrupa objetos UI]
+-- MainCanvas                             [Canvas, CanvasScaler, GraphicRaycaster,
|   |                                       UIManager, OrientationManager,
|   |                                       UIAudioService, AudioSource]
|   +-- HUD_Hotbar                         [Image]
|   |   +-- Hotbar_LayoutGroup             [VerticalLayoutGroup]
|   |       +-- Btn_Sand       -> Icon_Sand, Txt_Sand
|   |       +-- Btn_Glass      -> Icon_Glass, Txt_Glass
|   |       +-- Btn_Stone      -> Icon_Stone, Txt_Stone
|   |       +-- Btn_Wood       -> Icon_Wood, Txt_Wood
|   |       +-- Btn_Torch      -> Icon_Torch, Txt_Torch
|   |       +-- Btn_Grass      -> Icon_Grass, Txt_Grass
|   |       +-- Btn_None       -> Txt_None
|   |
|   +-- HUD_ToolPanel                      [Image]
|   |   +-- Tools_LayoutGroup              [HorizontalLayoutGroup]
|   |       +-- Btn_Break      -> Icon_Break, Txt_Break
|   |       +-- Btn_Hoe        -> Icon_Hoe, Txt_Hoe
|   |       +-- Btn_Brush      -> Icon_Brush, Txt_Brush  [BrushHUD]
|   |
|   +-- HUD_Selector                       [Image -- highlight amarillo]
|   |
|   +-- HUD_Harmony                        [HarmonyHUD]
|   |   +-- Txt_HarmonyStatus             [TMP_Text]
|   |   +-- Txt_Status                    [TMP_Text]
|   |   +-- Img_BarBackground              [Image]
|   |       +-- Img_BarFill               [Image -- fill anchor-driven]
|   |
|   +-- HUD_UndoRedo                       [UndoRedoHUD]
|   |   +-- Undo_LayoutGroup              [VerticalLayoutGroup]
|   |       +-- Btn_Undo       -> Icon_Undo, Txt_Undo
|   |       +-- Btn_Redo       -> Icon_Redo, Txt_Redo
|   |
|   +-- HUD_MenuBlocker                    [Button+Image -- invisible fullscreen, OFF]
|   |
|   +-- HUD_OptionsMenu                    [GameOptionsMenu]
|   |   +-- Svc_WorldReset                 [WorldResetService]
|   |   +-- Svc_Screenshot                 [ScreenshotService]
|   |   +-- Svc_SaveLoad                   [SaveLoadService]
|   |   +-- Btn_Settings       -> Icon_Settings, Txt_Settings
|   |   +-- Pnl_OptionsDropdown            [HorizontalLayoutGroup, ContentSizeFitter]
|   |       +-- Btn_Lighting               [DropdownButtonState] -> Txt_Lighting
|   |       +-- Btn_Depth                  [DropdownButtonState] -> Txt_Depth
|   |       +-- Plane_Controls             [VerticalLayoutGroup]
|   |       |   +-- Btn_Plane              [DropdownButtonState] -> Txt_Plane
|   |       |   +-- Btn_Grid              [DropdownButtonState] -> Txt_Grid
|   |       +-- Btn_Photo      -> Txt_Photo
|   |       +-- Btn_SaveGarden -> Txt_SaveGarden
|   |       +-- Btn_ClearAll   -> Txt_ClearAll
|   |       +-- Btn_Exit       -> Txt_Exit
|   |       +-- Btn_Vibration              [DropdownButtonState] -> Txt_Vibration
|   |       +-- Pnl_MusicVolume            [HorizontalLayoutGroup]
|   |           +-- Txt_MusicVolume        [TMP_Text]
|   |           +-- Sld_MusicVolume        [Slider]
|   |               +-- Background
|   |               +-- Fill Area -> Fill
|   |               +-- Handle Slide Area -> Handle
|   |
|   +-- Popup_ConfirmClearAll              [RectTransform -- OFF]
|   |   +-- Overlay_Background             [Image -- overlay semi-transparente]
|   |       +-- Pnl_ConfirmDialog          [Image -- card centrada]
|   |           +-- Txt_ConfirmMessage     [TMP_Text]
|   |           +-- Dialog_LayoutGroup     [VerticalLayoutGroup]
|   |               +-- Btn_Confirm -> Txt_Confirm
|   |               +-- Btn_Cancel  -> Txt_Cancel
|   |
|   +-- HUD_PerfectHarmony                 [PerfectHarmonyPanel]
|   |   +-- HUD_Particles                  [HarmonyParticles, ParticleSystem]
|   |   +-- Overlay_Background             [Image]
|   |       +-- Pnl_ConfirmDialog          [Image]
|   |           +-- Txt_ConfirmMessage     [TMP_Text]
|   |           +-- Dialog_LayoutGroup     [VerticalLayoutGroup]
|   |               +-- Btn_Continue -> Txt_Continue
|   |
|   +-- HUD_ScreenshotFlash                [Image (blanco) -- flash, OFF]
|   |
|   +-- Popup_ScreenshotToast              [ScreenshotToastPanel -- OFF]
|   |   +-- Pnl_ToastCard                  [HorizontalLayoutGroup]
|   |       +-- Txt_ToastMessage           [TMP_Text]
|   |       +-- Img_Preview                [RawImage -- thumbnail captura]
|   |       +-- Dialog_LayoutGroup         [VerticalLayoutGroup]
|   |           +-- Btn_Continue -> Txt_Continue
|   |
|   +-- Popup_SaveGarden                   [SaveGardenPopup, CanvasGroup -- OFF]
|   |   +-- Overlay_Background             [Image -- overlay semi-transparente]
|   |       +-- Pnl_SaveCard               [Image, VerticalLayoutGroup]
|   |           +-- Txt_SaveTitle           [TMP_Text -- "Guardar Jardin"]
|   |           +-- Inp_GardenName          [TMP_InputField]
|   |           +-- Dialog_LayoutGroup      [HorizontalLayoutGroup]
|   |               +-- Btn_Save   -> Txt_Save
|   |               +-- Btn_Cancel -> Txt_Cancel
|   |
|   +-- Popup_BonsaiSelector               [BonsaiSelectorPopup, CanvasGroup -- OFF]
|       +-- Overlay_Background             [Image -- overlay semi-transparente]
|           +-- Pnl_SelectorCard           [Image, VerticalLayoutGroup]
|               +-- Txt_SelectorTitle      [TMP_Text -- "Selecciona un jardin"]
|               +-- ScrollView_Gardens     [ScrollRect]
|               |   +-- Viewport           [Mask]
|               |       +-- Content_GardenList [VerticalLayoutGroup]
|               +-- Pnl_EmptyState          [OFF por defecto]
|               |   +-- Txt_EmptyMessage   [TMP_Text]
|               |   +-- Btn_BackToMenu     -> Txt_BackToMenu
|               +-- Btn_CloseSelector      -> Txt_CloseSelector
|
+-- EventSystem                            [EventSystem, StandaloneInputModule]
```

### 4.2 Mapa Script -> GameObject (Main_AR)

| Script | GameObject host | RequireComponent |
|--------|-----------------|------------------|
| `ARWorldManager` | XR Origin (Mobile AR) | `ARAnchorManager` |
| `ARDepthService` | XR Origin (Mobile AR) | -- (auto-busca AROcclusionManager) |
| `ARPlaneGridAligner` | XR Origin (Mobile AR) | -- |
| `WorldModeBootstrapper` | XR Origin (Mobile AR) | -- |
| `GameAudioService` | Svc_Audio | -- |
| `MusicService` | Svc_Audio | -- |
| `HapticService` | Svc_Audio | -- |
| `TouchInputRouter` | Svc_Interaction | -- |
| `ARBlockPlacer` | Svc_Interaction | -- |
| `BlockDestroyer` | Svc_Interaction | -- |
| `BrushTool` | Svc_Interaction | -- |
| `PlowTool` | Svc_Interaction | -- |
| `DebugRayVisualizer` | Svc_Interaction | -- |
| `HarmonyService` | Svc_GameLogic | -- |
| `UndoRedoService` | Svc_GameLogic | -- |
| `LightingService` | Svc_Lighting | -- |
| `GridManager` | WorldContainer | -- |
| `GridVisualizer` | WorldContainer | -- |
| `ToolManager` | ToolManager | -- |
| `UIManager` | MainCanvas | -- |
| `OrientationManager` | MainCanvas | -- |
| `UIAudioService` | MainCanvas | `AudioSource` |
| `HarmonyHUD` | HUD_Harmony | -- |
| `UndoRedoHUD` | HUD_UndoRedo | -- |
| `BrushHUD` | Btn_Brush | -- |
| `GameOptionsMenu` | HUD_OptionsMenu | -- |
| `PerfectHarmonyPanel` | HUD_PerfectHarmony | `CanvasGroup` |
| `HarmonyParticles` | HUD_Particles | `ParticleSystem` |
| `WorldResetService` | Svc_WorldReset | -- |
| `ScreenshotService` | Svc_Screenshot | -- |
| `ScreenshotToastPanel` | Popup_ScreenshotToast | `CanvasGroup` |
| `SaveGardenPopup` | Popup_SaveGarden | `CanvasGroup` |
| `BonsaiSelectorPopup` | Popup_BonsaiSelector | `CanvasGroup` |
| `SaveLoadService` | Svc_SaveLoad | -- |
| `BonsaiSessionController` | Svc_BonsaiSession | -- |
| `ButtonPressAnimation` | Cada `Btn_*` | `Button` |
| `DropdownButtonState` | `Btn_Lighting`, `Btn_Depth`, `Btn_Grid`, `Btn_Plane`, `Btn_Vibration` | -- |

### 4.3 Title_Screen.unity

Camara frontal con Face Tracking (filtro de Creeper) y Hand Tracking (MediaPipe, GPU delegate) para seleccionar modo con **pinch click** o **dwell time** (3s fallback). Logo "ARMONIA" con bobbing vertical. Transicion a `Main_AR` via `SceneTransitionService` (fade-to-black).

```text
AR System                                  [Empty -- agrupa objetos AR]
+-- XR Origin (Front Camera)               [XROrigin, InputActionManager,
|   |                                       ARFaceManager (enabled, maxFaces 1),
|   |                                       ARPlaneManager (disabled), ARRaycastManager (disabled),
|   |                                       CreeperFaceFilter, HandTrackingService]
|   +-- Camera Offset
|       +-- Main Camera                    [Camera, AudioListener, TrackedPoseDriver,
|                                           ARCameraManager (User facing), ARCameraBackground,
|                                           UniversalAdditionalCameraData]
+-- XR Interaction Manager                 [XRInteractionManager]
+-- AR Session                             [ARSession, ARInputManager]
+-- Directional Light                      [Light (Directional), UniversalAdditionalLightData]

UI System                                  [Empty -- agrupa objetos UI]
+-- TitleCanvas                            [Canvas (Overlay, order 10), CanvasScaler (1080x2400, match 0.5),
|   |                                       GraphicRaycaster, TitleSceneManager, DwellSelector, AudioSource]
|   +-- HUD_Buttons                        [Image (semi-transparente), VerticalLayoutGroup]
|   |   +-- Txt_GameMode                   [TMP_Text -- "Elige tu modo de juego", size 60]
|   |   +-- Btn_Bonsai                     [Button -> SelectMode(0), Image]
|   |   |   +-- Txt_Bonsai                 [TMP_Text -- "Bonsai", size 50]
|   |   +-- Btn_Normal                     [Button -> SelectMode(1), Image]
|   |   |   +-- Txt_Normal                 [TMP_Text -- "Normal", size 50]
|   |   +-- Btn_Real                       [Button -> SelectMode(2), Image]
|   |       +-- Txt_Real                   [TMP_Text -- "Real", size 50]
|   +-- HUD_Logo                           [Image, TitleLogoAnimator]
|   +-- HandCursor                         [RectTransform, CanvasGroup, HandCursorUI]
|       +-- Img_CursorDot                  [Image -- 100x100 white circle, raycastTarget OFF]
|       +-- Img_DwellProgress              [Image -- 100x100 radial fill ring]
+-- EventSystem                            [EventSystem, InputSystemUIInputModule]

MusicPlayer                                [AudioSource, MusicService]
```

### 4.4 Mapa Script -> GameObject (Title_Screen)

| Script | GameObject host | RequireComponent |
|--------|-----------------|------------------|
| `TitleSceneManager` | TitleCanvas | -- |
| `CreeperFaceFilter` | XR Origin (Front Camera) | -- |
| `HandTrackingService` | XR Origin (Front Camera) | -- |
| `HandCursorUI` | HandCursor | -- |
| `DwellSelector` | TitleCanvas | `AudioSource` |
| `TitleLogoAnimator` | HUD_Logo | -- |
| `MusicService` | MusicPlayer | -- |

### 4.5 Wiring de botones

**Title_Screen:**

| Boton | OnClick destino | Parametro |
|-------|-----------------|-----------|
| `Btn_Bonsai` | `TitleSceneManager.SelectMode(int)` | `0` |
| `Btn_Normal` | `TitleSceneManager.SelectMode(int)` | `1` |
| `Btn_Real` | `TitleSceneManager.SelectMode(int)` | `2` |

**Main_AR -- regla del Brush:**

El `Btn_Brush` es una **excepcion** al patron estandar de botones:

| Boton | OnClick destino | Razon |
|-------|-----------------|-------|
| `Btn_Sand` ... `Btn_Hoe` (salvo Brush) | `UIManager.OnSlotClicked(int)` | Herramientas normales |
| `Btn_Brush` | `BrushTool.ToggleBrush()` -- **llamada directa** | Mode overlay, no pasa por ToolManager |
| `Btn_SaveGarden` | `GameOptionsMenu.SaveGarden()` | Abre popup de guardado |

---

## 5. Patrones de arquitectura

Tabla con ejemplo real de cada patron arquitectonico del proyecto:

| Patron | Uso | Ejemplo real |
|--------|-----|--------------|
| **Service Locator** | Resolucion de dependencias por interfaz sin singletons ni `Find*` | `ServiceLocator.Register<IGameAudioService>(this)` en `Awake`; `ServiceLocator.TryGet<IGameAudioService>(out _audioService)` en consumidor |
| **EventBus** (pub/sub tipado) | Comunicacion cross-sistema sin referencias directas | `EventBus.Publish(new BlockPlacedEvent(cell, id))` -> cualquier suscriptor lo recibe |
| **C# Events** (`event Action<T>`) | Notificacion intra-capa, UI reactiva | `IHarmonyService.OnHarmonyChanged` -> `HarmonyHUD.SetHarmony` |
| **Llamada directa** | Acoplamiento estrecho dentro de la misma capa | `ARBlockPlacer` -> `IGridManager.GetSnappedPosition()` |
| **Inspector** `[SerializeField]` | Inyeccion de dependencias para MonoBehaviours de escena | Toda seccion `#region Inspector` en scripts de escena |
| **Command Pattern** | Undo/Redo | `IUndoableAction` -> `PlaceBlockAction` / `DestroyBlockAction` |
| **Facade** | Simplificar acceso a subsistema | `GridManager` envuelve `GridVisualizer` |
| **ScriptableObject data** | Config compartida sin depender de escena | `BlockDatabaseSO`, `HarmonyConfig`, `WorldModeSO` |
| **Static context** | Dato cross-escena sin singletons | `WorldModeContext.Selected` |
| **Internal static helper** | Logica compartida entre Commands sin estado | `PlaceBlockAction.ArmForImmediate()` -- deshabilita BlockSpawn, habilita Collider + SetReady(). Usado por Redo (place) y Undo (destroy). |
| **ServiceLocator.TryGet en Awake** | Servicios de escena desde prefabs instanciados | `BlockSpawn`, `BlockDestroy`, `SandGravity` resuelven interfaces en `Awake` |
| **Prefab-owns-feedback** | Audio y VFX viven en el prefab, no en el caller | `BlockSpawn` reproduce place sounds/VFX; `BlockDestroy` reproduce break sounds/VFX |
| **VoxelBlock como fuente de audio** | Prefab data-component leido por sibling components | `BlockSpawn` lee `VoxelBlock.PlaceSounds`; `BlockDestroy` lee `VoxelBlock.BreakSounds`. Fallback a campos propios para pebbles. |
| **OnClick directo** | Botones de modo toggle que no son herramientas | `Btn_Brush.OnClick -> BrushTool.ToggleBrush()` |
| **OnClick con int param** | Seleccion indexada desde botones UI | `Btn_Bonsai.OnClick -> TitleSceneManager.SelectMode(0)` |
| **Scene transition** | Carga de escena con fade y dato estatico pre-escrito | `TitleSceneManager` escribe `WorldModeContext.Selected`, luego `SceneTransitionService.TransitionTo("Main_AR")`. `WorldModeBootstrapper` lee en `Awake()` y difiere activacion de AR managers a corrutina que espera `ARSession.state >= SessionInitializing`. Retorno: `GameOptionsMenu.ExitGame()` lanza coroutine `ExitWithARCleanup()` que deshabilita managers AR, espera 1 frame, deinicializa el XR loader (`DeinitializeLoader`) para destruir la sesion nativa de ARCore y eliminar config stale de image tracking, re-inicializa un loader limpio (`InitializeLoaderSync`), y transiciona. |
| **DontDestroyOnLoad + ServiceLocator** | Servicio cross-escena | `SceneTransitionService`: se registra como `ISceneTransitionService` en `Awake`, persiste via `DontDestroyOnLoad`, guard `IsRegistered<T>()` para evitar duplicados. Canvas overlay propio (sort order 999). |
| **Pinch gesture detection** | Deteccion de gesto por distancia de landmarks | `HandTrackingService` mide distancia thumb tip (#4) <-> index tip (#8). Histeresis (enter 0.055, exit 0.08) + debounce (2 frames). |
| **JSON persistence** | Guardado/carga de estado del mundo a disco | `SaveLoadService` serializa `GardenSaveData` via `JsonUtility` a `persistentDataPath/Gardens/`. `ISaveLoadService` registrada en ServiceLocator. |
| **Continuous image tracking** | Seguimiento AR de carta impresa en Bonsai mode | `WorldModeBootstrapper` actualiza pose del WorldContainer cada frame desde `ARTrackedImage.updated`. Sin `ARAnchor` -- pose directa. |
| **Session controller** | Orquestacion de flujo de modo de juego | `BonsaiSessionController` escucha `OnBonsaiImageDetected`, abre popup selector. Self-disable si no es modo Bonsai. |
| **Sand gravity (EventBus + safety poll)** | Gravedad selectiva por tipo de bloque | `SandGravity` (solo `Voxel_Sand`): espera `BlockDestroy.IsReady` + 0.15s, suscripcion a `BlockDestroyedEvent` (trigger primario) + `InvokeRepeating` cada 1s (safety net). Si no hay soporte -> reserva celda en `HashSet` estatico -> cae animado (ease-in). `Physics.SyncTransforms()` tras aterrizar. Desactiva collider/BlockDestroy durante caida. |

### Vibracion haptica -- patron de integracion

`HapticService` vive en `Svc_Audio`. Empieza **desactivado** (el usuario lo activa desde `Btn_Vibration`).

Los scripts llaman `_hapticService?.VibrateX()`. `UIAudioService` incluye vibracion en todos los metodos `Play*()` (excepto `PlayPhoto`, que la maneja `ScreenshotService`).

---

## 6. Rendimiento -- target S24 Ultra

Reglas obligatorias para 60fps estables en AR movil:

| Regla | Por que | Como |
|-------|---------|------|
| **No `GetComponent` en `Update()`** | Presion GC + CPU spike por frame | Cachear en `Awake()` o `Start()` |
| **No `Find()` / `FindObjectOfType()`** | Scan O(n) cada llamada | `[SerializeField]` en Inspector o `ServiceLocator.TryGet<T>()` para prefabs |
| **No allocation en hot paths** | GC stutter en movil | Reutilizar `List<>`, `HashSet<>`, `MaterialPropertyBlock`, `WaitForSeconds` |
| **UI event-driven** | Polling gasta bateria + CPU | `event Action<T>` -> suscribir en `OnEnable`, desuscribir en `OnDisable` |
| **No concatenar `string` en `Update()`** | `StringBuilder` oculto alloc | Interpolated strings solo en `Debug.Log` (stripped en Release) |
| **Cachear `Camera.main`** | Internamente llama `FindObjectWithTag` | Guardar en `Awake()`: `_mainCamera = Camera.main` |
| **Usar `sqrMagnitude`** | Evita `sqrt` por frame | `if (sqrDist <= radius * radius)` |
| **HashSet para celdas pendientes** | Lookup O(1) contra double-tap | `_pendingCells` en `ARBlockPlacer` |
| **Shader IDs estaticos** | `Shader.PropertyToID` es costoso | `static readonly int ID = Shader.PropertyToID("_Name")` |
| **Coroutine yields cacheados** | `new WaitForSeconds` = alloc | `private readonly WaitForSeconds _wait = new(0.5f)` |
| **No `Instantiate`/`Destroy` masivo** | GC spikes | Pool VFX prefabs (pendiente de implementar) |

---

## 7. Estetica URP

| Regla | Configuracion |
|-------|---------------|
| **Texturas pixel-art** | `Filter Mode = Point (no filter)`, `Compression = None` |
| **Sombras suaves** | URP Asset -> Shadows -> Soft Shadows = ON |
| **Resolucion de sombras** | Minimo 1024 para sombras nitidas |
| **Shader de bloques** | `ARmonia/Blocks/VoxelLit` con toon lighting 3 bandas |
| **Shader de suelo AR** | `ARmonia/AR/ARPlane` con arena zen + grid animado |
| **MaterialPropertyBlock** | Usar en vez de material instances para propiedades per-object |
| **Oclusion por profundidad** | `AROcclusionManager` via `ARDepthService` (toggle enabled/disabled, checkbox `_depthOnStart`, default ON) |
| **Pipeline Assets** | `Mobile_RPAsset` para Android, `PC_RPAsset` para Editor |
| **Fuente** | `minecraft_fot_esp` (con caracteres espanoles) via TextMeshPro SDF |

---

## 8. Reglas generales

### Obligatorio

- Todos los nombres en **ingles** -- no espanol en nombres de escena, scripts ni assets.
- Sin espacios en nombres de assets o carpetas.
- Sin caracteres especiales (solo letras, numeros, `_`).
- Cada asset tiene su `.meta` -- **nunca mover ni renombrar assets fuera de Unity Editor**.
- Carpetas vacias confirmadas con `.gitkeep`.
- Bloques en `Prefabs/Blocks/`, VFX en `Prefabs/VFX/`.
- Scripts AR en `Scripts/AR/`, no mezclados con Core o Interaction.
- Cada `[SerializeField]` lleva `[Tooltip]`.
- Cada grupo de `[SerializeField]` lleva `[Header]`.
- Cada MonoBehaviour tiene `ValidateReferences()` llamado en `Start()` -- formato estandarizado (ver seccion 2.6).
- Cachear yield objects de coroutines como campos.
- Usar `#region` siguiendo el orden de la plantilla (seccion 2.3).
- Usar `[DisallowMultipleComponent]` en cada MonoBehaviour.
- Usar `[AddComponentMenu("ARmonia/...")]` en cada MonoBehaviour.
- Cada clase tiene XML `<summary>` doc comment.
- Cada `using` no utilizado se elimina.

### Prohibido

- `GetComponent` en `Update()` -- cachear en `Awake()`.
- `Find()` o `FindObjectOfType()` -- usar `[SerializeField]`.
- Allocations en hot paths (`Update`, loops de coroutine) -- reutilizar buffers.
- Hardcodear magic numbers -- usar `[SerializeField]` o `const`.
- Duplicar referencias de escena en prefabs -- usar `ServiceLocator.TryGet<T>()` en `Awake` o `[SerializeField]` para datos de prefab.
- Dejar `Debug.Log` en release sin condicional (Unity los stripea en builds no-Development, pero mantener limpio).
- `OnValidate()` en MonoBehaviours -- solo `ValidateReferences()` desde `Start()`. Excepcion: `ScriptableObject` puede usar `OnValidate()` para validacion de datos de asset.
- `Debug.LogError` dentro de `ValidateReferences()` -- siempre `Debug.LogWarning` (ver seccion 2.6).

### Mantenimiento de documentacion

Cada vez que se haga un cambio en el proyecto (nuevo script, prefab, carpeta, renombrado, cambio de jerarquia, nuevo paquete, etc.) se deben actualizar **los dos archivos de documentacion**:

- **`README.md`** -- estructura de carpetas, inventario de assets, catalogo de scripts, estado del proyecto, dependencias.
- **`CONVENTIONS.md`** -- jerarquia de escena, mapa Script-GameObject, reglas de nombrado.

**La documentacion desactualizada se considera un bug igual que el codigo roto.**
