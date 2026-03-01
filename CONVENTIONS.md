# ARmonia — Naming & Structure Conventions

Reference document to maintain consistency across the entire project.
**Every new asset, script, or folder must follow these rules.**

---

## 1. Folders

| Rule | Correct | Incorrect |
| --- | --- | --- |
| PascalCase | `Scripts/` | `scripts/` |
| Plural | `Materials/`, `Prefabs/`, `Textures/` | `Material/`, `Prefab/` |
| No spaces | `XR/`, `TextMesh Pro/` N/A (external packages) | `My Folder/` |
| Project root | `_Project/` (with `_` to sort first) | `Project/` |

### `_Project/` folder structure

```text
_Project/
├── Audio/
│   ├── Music/
│   └── SFX/
├── Assets/
│   └── BlockDatabase.asset
├── Fonts/
├── Materials/
│   ├── AR/
│   └── Blocks/
├── Prefabs/
│   ├── AR/
│   ├── Blocks/
│   ├── UI/
│   └── VFX/
├── Scenes/
│   └── Main_AR.unity
├── Scripts/
│   ├── AR/           → ARWorldManager.cs
│   ├── Core/         → GridManager.cs, GridVisualizer.cs, GameAudioService.cs
│   ├── Interaction/  → ARBlockPlacer.cs, ToolManager.cs, DebugRayVisualizer.cs,
│   │                   ToolType.cs
│   ├── UI/           → UIManager.cs, GameOptionsMenu.cs, OrientationManager.cs,
│   │                   ScreenshotService.cs, WorldResetService.cs
│   └── Voxel/        → BlockType.cs, VoxelBlock.cs, BlockDatabase.cs
└── Textures/
    ├── Blocks/
    ├── Icons/
    └── UI/
```

---

## 2. Scene Hierarchy — `Main_AR.unity`

The scene is organized into **2 root GameObjects** that act as logical groups.
Every object is in English and follows a strict prefix convention.

### Full hierarchy

```text
AR System                                  [Empty — groups all 3D/AR objects]
├── AR Session                             [ARSession, ARInputManager]
├── XR Interaction Manager                 [XRInteractionManager]
├── XR Origin (Mobile AR)                  [XROrigin, ARPlaneManager, ARRaycastManager,
│   │                                       ARAnchorManager, ARBlockPlacer, ARWorldManager,
│   │                                       GameAudioService, DebugRayVisualizer,
│   │                                       LineRenderer, AudioSource]
│   └── Camera Offset                      [standard XR child]
│       └── Main Camera                    [Camera, AudioListener, TrackedPoseDriver,
│                                           ARCameraManager, ARCameraBackground]
├── WorldContainer                         [GridManager, GridVisualizer]  (localScale 0.1)
├── ToolManager                            [ToolManager]
└── Directional Light                      [Light, URP AdditionalLightData]

UI System                                  [Empty — groups all UI objects]
├── MainCanvas                             [Canvas, CanvasScaler, GraphicRaycaster,
│   │                                       UIManager, OrientationManager]
│   ├── HUD_Hotbar                         [Image — bottom block-selection bar]
│   │   └── Hotbar_LayoutGroup             [HorizontalLayoutGroup]
│   │       ├── Btn_Dirt       → Txt_Dirt
│   │       ├── Btn_Sand       → Txt_Sand
│   │       ├── Btn_Stone      → Txt_Stone
│   │       ├── Btn_Wood       → Txt_Wood
│   │       ├── Btn_Torch      → Txt_Torch
│   │       └── Btn_None       → Txt_None
│   │
│   ├── HUD_ToolPanel                      [Image — side tool-selection panel]
│   │   └── Tools_LayoutGroup              [VerticalLayoutGroup]
│   │       ├── Btn_Break      → Txt_Break
│   │       ├── Btn_Brush      → Txt_Brush
│   │       └── Btn_Plow       → Txt_Plow
│   │
│   ├── HUD_Selector                       [Image — yellow highlight rect]
│   ├── HUD_MenuBlocker                    [Button+Image — invisible fullscreen close tap]
│   │
│   ├── HUD_OptionsMenu                    [GameOptionsMenu]
│   │   ├── Svc_WorldReset                 [WorldResetService]
│   │   ├── Svc_Screenshot                 [ScreenshotService]
│   │   ├── Btn_Settings       → Txt_Settings
│   │   └── Panel_OptionsDropdown          [VerticalLayoutGroup, ContentSizeFitter] (inactive)
│   │       ├── Btn_Lighting   → Txt_Lighting
│   │       ├── Btn_Photo      → Txt_Photo
│   │       ├── Btn_ClearAll   → Txt_ClearAll
│   │       └── Btn_Exit       → Txt_Exit
│   │
│   └── Popup_ConfirmClearAll              [RectTransform — fullscreen popup root] (inactive)
│       └── Overlay_Background             [Image — dark semi-transparent overlay]
│           └── Panel_ConfirmDialog         [Image — centered dialog card]
│               ├── Txt_ConfirmMessage     [TextMeshPro — question text]
│               └── Dialog_LayoutGroup     [HorizontalLayoutGroup]
│                   ├── Btn_Confirm → Txt_Confirm
│                   └── Btn_Cancel  → Txt_Cancel
│
└── EventSystem                            [InputSystemUIInputModule, EventSystem]
```

### Component-to-GameObject map

| Script (MonoBehaviour) | Host GameObject | RequireComponent |
| --- | --- | --- |
| `ARBlockPlacer` | XR Origin (Mobile AR) | `ARRaycastManager` |
| `ARWorldManager` | XR Origin (Mobile AR) | `ARAnchorManager` |
| `GameAudioService` | XR Origin (Mobile AR) | `AudioSource` |
| `DebugRayVisualizer` | XR Origin (Mobile AR) | `LineRenderer` |
| `GridManager` | WorldContainer | — |
| `GridVisualizer` | WorldContainer | — |
| `ToolManager` | ToolManager | — |
| `UIManager` | MainCanvas | — |
| `OrientationManager` | MainCanvas | — |
| `GameOptionsMenu` | HUD_OptionsMenu | — |
| `ScreenshotService` | Svc_Screenshot | — |
| `WorldResetService` | Svc_WorldReset | — |

### Scene GameObject naming rules

| Prefix | Used for | Examples |
| --- | --- | --- |
| *(none)* | Unity standard objects | `AR Session`, `Main Camera`, `EventSystem` |
| `HUD_` | Persistent on-screen UI regions | `HUD_Hotbar`, `HUD_Selector`, `HUD_MenuBlocker` |
| `Panel_` | Contained UI panels/cards | `Panel_OptionsDropdown`, `Panel_ConfirmDialog` |
| `Popup_` | Fullscreen modal popup roots | `Popup_ConfirmClearAll` |
| `Overlay_` | Dark/transparent background layers | `Overlay_Background` |
| `Btn_` | Buttons (each containing a Txt_ child) | `Btn_Dirt`, `Btn_Settings`, `Btn_Confirm` |
| `Txt_` | TextMeshPro text labels (child of Btn_ or standalone) | `Txt_Dirt`, `Txt_ConfirmMessage` |
| `*_LayoutGroup` | Objects with Layout Group components | `Hotbar_LayoutGroup`, `Tools_LayoutGroup`, `Dialog_LayoutGroup` |
| `Svc_` | Service-only GameObjects (no visuals) | `Svc_Screenshot`, `Svc_WorldReset` |
| PascalCase | Manager singletons / containers | `WorldContainer`, `ToolManager`, `MainCanvas` |

> **Rule**: Every `Btn_X` button must contain exactly one `Txt_X` child with the same suffix.
> **Rule**: All names are in English. No Spanish.
> **Rule**: No spaces in custom names. Use `_` (underscores) to separate prefix from name.
> **Rule**: Unity-standard objects keep their default names (`AR Session`, `Directional Light`, etc.).

---

## 3. C# Scripts

| Rule | Example |
| --- | --- |
| PascalCase | `GridManager.cs` |
| **Filename must match class name exactly** | `ARBlockPlacer.cs` → `public class ARBlockPlacer` |
| No extra prefixes/suffixes | `BlockPlacer.cs` ❌ if the class is `ARBlockPlacer` |
| Namespace follows folder path | `_Project.Scripts.Core`, `_Project.Scripts.UI` |

### Namespaces by folder

| Folder | Namespace | Scripts |
| --- | --- | --- |
| `Scripts/AR/` | `_Project.Scripts.AR` | `ARWorldManager` |
| `Scripts/Core/` | `_Project.Scripts.Core` | `GridManager`, `GridVisualizer`, `GameAudioService` |
| `Scripts/Interaction/` | `_Project.Scripts.Interaction` | `ToolType`, `ToolManager`, `ARBlockPlacer`, `DebugRayVisualizer` |
| `Scripts/UI/` | `_Project.Scripts.UI` | `UIManager`, `GameOptionsMenu`, `OrientationManager`, `ScreenshotService`, `WorldResetService` |
| `Scripts/Voxel/` | `_Project.Scripts.Voxel` | `BlockType`, `VoxelBlock`, `BlockDatabase` |

### Script structure template

Every MonoBehaviour follows this region order:

1. `#region Constants` — `private const` and `static readonly` values
2. `#region Inspector` — `[SerializeField]` fields with `[Header]` and `[Tooltip]`
3. `#region Events` — `public event Action<T>` declarations
4. `#region Cached Components` — private references populated in `Awake()`
5. `#region Public API` — properties and methods callable by other systems
6. `#region Unity Lifecycle` — `Awake()`, `OnEnable()`, `Start()`, `Update()`, `OnDisable()`
7. `#region Internals` — private helper methods
8. `#region Validation` — `ValidateReferences()` called from `Start()`

### Required class attributes

```csharp
[DisallowMultipleComponent]
[AddComponentMenu("ARmonia/{Folder}/{ClassName}")]
public class MyScript : MonoBehaviour { }
```

---

## 4. Materials

**Required prefix: `M_`**

| Pattern | Example |
| --- | --- |
| `M_{Name}` | `M_ARGround.mat` |
| `M_Block{Type}` | `M_BlockDirt.mat`, `M_BlockSand.mat` |
| `M_{System}{Name}` | `M_GridLines.mat` |

---

## 5. Prefabs

**Prefix by category:**

| Category | Prefix | Example |
| --- | --- | --- |
| Voxel blocks | `Voxel_` | `Voxel_Dirt.prefab`, `Voxel_Torch.prefab` |
| AR elements | `AR` + PascalCase | `ARDefaultPlane.prefab`, `RayInteractor.prefab` |
| Visual effects | `VFX_` | `VFX_BlockPlace.prefab` |
| UI elements | `UI_` | `UI_HotbarSlot.prefab` |
| Characters / NPCs | `CH_` | `CH_Player.prefab` |

---

## 6. Textures

**Prefix by type:**

| Type | Prefix | Example |
| --- | --- | --- |
| Albedo / Diffuse | `T_` | `T_BlockDirt_D.png` |
| Normal map | `T_` + suffix `_N` | `T_BlockStone_N.png` |
| Mask (ORM) | `T_` + suffix `_M` | `T_BlockWood_M.png` |
| UI icons | `Icon_` | `Icon_Destroy.png` |
| UI sprites | `UI_` | `UI_HotbarBg.png` |
| App icon | PascalCase without prefix | `ARmoniaIcon.png` |

---

## 7. Audio

| Type | Prefix | Example |
| --- | --- | --- |
| Sound effects | `SFX_` | `SFX_BlockPlace.mp3`, `SFX_BlockBreak.mp3` |
| Background music | `MUS_` | `MUS_ZenAmbient.mp3` |
| Voice / Narration | `VO_` | `VO_Tutorial01.mp3` |

---

## 8. Scenes

| Rule | Example |
| --- | --- |
| PascalCase with context | `Main_AR.unity` |
| Menu screens | `Menu_Main.unity` |
| Loading screens | `Loading.unity` |
| Test scenes | `Test_GridManager.unity` |

---

## 9. C# Variables & Fields

| Type | Convention | Example |
| --- | --- | --- |
| `[SerializeField]` private | `_camelCase` | `_gridManager` |
| Public property | PascalCase | `GridSize`, `IsWorldAnchored` |
| Constant | `UPPER_SNAKE_CASE` | `MAX_BUILD_DISTANCE` |
| Local variable | `camelCase` | `snappedPosition` |
| Method parameter | `camelCase` | `hitPose`, `playerCamera` |
| Event | `On` + PascalCase | `OnToolChanged` |

---

## 10. General Rules

* ✅ **All names in English** — no Spanish in scene objects, scripts, or asset names
* ✅ No spaces in any asset or folder name
* ✅ No special characters (only letters, numbers, `_`)
* ✅ Every asset has a matching `.meta` — **never move or rename assets outside Unity Editor**
* ✅ Empty folders confirmed with `.gitkeep`
* ✅ All block prefabs in `Prefabs/Blocks/`, all VFX in `Prefabs/VFX/`
* ✅ AR scripts in `Scripts/AR/`, not mixed with Core
* ✅ Every `[SerializeField]` must have a `[Tooltip]` explaining its purpose
* ✅ Every MonoBehaviour must have `ValidateReferences()` called in `Start()`
* ✅ Cache coroutine yield objects (`WaitForSeconds`, `WaitForEndOfFrame`) as fields
* ❌ Never use `GetComponent` in `Update()` — always cache in `Awake()`
* ❌ Never use `Find()` or `FindObjectOfType()` — use `[SerializeField]`
* ❌ Never allocate in hot paths (`Update`, coroutine loops) — reuse buffers
