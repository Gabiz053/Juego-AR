![Unity](https://img.shields.io/badge/Unity-6-black?style=flat-square&logo=unity)
![ARCore](https://img.shields.io/badge/ARCore-XR-blue?style=flat-square)
![Android](https://img.shields.io/badge/Android-Target_S24_Ultra-green?style=flat-square&logo=android)
![URP](https://img.shields.io/badge/Render-URP-red?style=flat-square)

# ARmonía — Jardín Zen AR estilo Minecraft Earth

Sandbox creativo de Realidad Aumentada para un solo jugador. Construyes un jardín
zen con bloques voxel 1×1×1 sobre superficies reales detectadas por la cámara
trasera del móvil. Colocas arena, piedra, madera, cristal, hierba, antorchas y
piedritas decorativas procedurales. Una barra de **Armonía** evalúa en tiempo real
si tu jardín está equilibrado en variedad, cantidad y decoración.

- **Hardware de referencia:** Samsung Galaxy S24 Ultra.
- **Motor:** Unity 6 (2022.3 LTS+).
- **Pipeline:** Universal Render Pipeline (URP), OpenGLES3.
- **AR:** AR Foundation 6.0.6 + ARCore XR Plugin 6.0.6.
- **Input:** Enhanced Touch (Input System 1.17.0).
- **Paquete extra:** MediaPipe Unity Plugin 0.16.3 (local package, preparado para
  Hand/Face Tracking de la futura pantalla de inicio).
- **Bundle ID:** `com.Gabiz.ARmonia`
- **Versión:** 0.2.0

---

## Tabla de contenidos

1. [Estructura de carpetas](#estructura-de-carpetas)
2. [Inventario completo de assets](#inventario-completo-de-assets)
3. [Arquitectura del WorldContainer](#arquitectura-del-worldcontainer)
4. [Modos de escala del mundo](#modos-de-escala-del-mundo)
5. [Mapa de comunicación entre sistemas](#mapa-de-comunicación-entre-sistemas)
6. [Inventario y herramientas](#inventario-y-herramientas)
7. [Shaders personalizados](#shaders-personalizados)
8. [Pantalla de inicio (planificada)](#pantalla-de-inicio-planificada)
9. [Lista completa de scripts (49)](#lista-completa-de-scripts-49)
10. [Estado del proyecto](#estado-del-proyecto)
11. [Dependencias de paquetes](#dependencias-de-paquetes)
12. [Cómo abrir el proyecto](#cómo-abrir-el-proyecto)

---

## Estructura de carpetas

```text
Assets/
├── _Project/
│   ├── Assets/                  ← ScriptableObjects y fuentes
│   │   ├── Fonts/               ← TTF + SDF assets de TextMeshPro
│   │   ├── BlockDatabase.asset
│   │   ├── HarmonyConfig.asset
│   │   ├── WorldModeConfig_Bonsai.asset
│   │   ├── WorldModeConfig_Normal.asset
│   │   └── WorldModeConfig_Real.asset
│   ├── Audio/
│   │   ├── Music/               ← 12 pistas MP3 (C418 Minecraft OST)
│   │   └── SFX/
│   │       ├── UI/              ← 5 clips (SFX_MenuClick, SFX_LevelUp, SFX_Orb, SFX_ToastComplete, SFX_ButtonPress)
│   │       └── Voxels/          ← 23 clips (dig, hit, mining, brush, hoe, break)
│   ├── Materials/
│   │   ├── AR/                  ← M_ARGround.mat, M_GridLines.mat
│   │   └── Blocks/              ← M_BlockDirt/Sand/Stone/Torch/Wood.mat, M_Sand.mat
│   ├── Models/
│   │   └── Blocks/              ← 5 modelos .glb (Model_Glass, Model_Grass, Model_Stone, Model_Torch, Model_Wood)
│   ├── Prefabs/
│   │   ├── AR/                  ← AR_Default_Plane.prefab, AR_RayInteractor.prefab
│   │   ├── Blocks/              ← 7 prefabs activos + carpeta _Deprecated/ (5 obsoletos)
│   │   ├── UI/                  ← (vacía, .gitkeep)
│   │   └── VFX/                 ← VFX_BlockPlace.prefab, VFX_BlockBreak.prefab
│   ├── Scenes/
│   │   └── Main_AR.unity        ← Escena principal (única escena)
│   ├── Scripts/
│   │   ├── AR/                  ← 4 scripts
│   │   ├── Core/                ← 16 scripts (incluye enums, interfaces, statics, servicios)
│   │   ├── Interaction/         ← 9 scripts
│   │   ├── UI/                  ← 11 scripts
│   │   └── Voxel/               ← 9 scripts
│   ├── Shaders/
│   │   ├── ARPlane.shader       ← Shader HLSL arena zen con grid animado
│   │   └── VoxelLit.shader      ← Shader HLSL toon-lit para bloques voxel
│   └── Textures/
│       ├── AR/                  ← T_Sand.png, T_ZenFloor.png
│       ├── Icons/               ← ARmoniaIcon.png, ARmoniaIconBackground.jpg
│       └── UI/                  ← 18 sprites PNG (Icon_Sand, Icon_Stone, UI_Background, etc.)
├── Resources/                   ← (vacía, uso interno Unity)
├── Settings/                    ← URP Pipeline Assets y Volume Profiles
│   ├── Mobile_RPAsset.asset     ← URP Render Pipeline Asset (Android)
│   ├── Mobile_Renderer.asset    ← URP Renderer (Android)
│   ├── PC_RPAsset.asset         ← URP Render Pipeline Asset (Editor)
│   ├── PC_Renderer.asset        ← URP Renderer (Editor)
│   ├── DefaultVolumeProfile.asset
│   ├── SampleSceneProfile.asset
│   └── UniversalRenderPipelineGlobalSettings.asset
├── InputSystem_Actions.inputactions
├── TextMesh Pro/                ← Paquete TMP (importado)
├── XR/                          ← XR Interaction Toolkit defaults
└── XRI/                         ← XR Interaction Toolkit settings
```

---

## Inventario completo de assets

### ScriptableObjects (5 assets)

| Asset | Tipo | Ubicación | Descripción |
|-------|------|-----------|-------------|
| `BlockDatabase.asset` | `BlockDatabase` | `Assets/` | Mapea cada `BlockType` a su prefab. Lookup O(1) con lazy dictionary. |
| `HarmonyConfig.asset` | `HarmonyConfig` | `Assets/` | Pesos de los 3 pilares (variedad 0.45, decoración 0.35, cantidad 0.20), umbrales y gate de mínimos. |
| `WorldModeConfig_Bonsai.asset` | `WorldModeSO` | `Assets/` | Escala 0.02, ancla por tracked image, `ImagePhysicalWidth` 0.20m. |
| `WorldModeConfig_Normal.asset` | `WorldModeSO` | `Assets/` | Escala 0.10, ancla por AR plane. Modo por defecto. |
| `WorldModeConfig_Real.asset` | `WorldModeSO` | `Assets/` | Escala 1.00, ancla por AR plane. Escala Minecraft real. |

### Prefabs (16 archivos, 7+5+2+2)

**Bloques activos (7):**

| Prefab | Componentes | Shader |
|--------|-------------|--------|
| `Voxel_Sand.prefab` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` | `ARmonia/Blocks/VoxelLit` |
| `Voxel_Glass.prefab` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` | `ARmonia/Blocks/VoxelLit` |
| `Voxel_Stone.prefab` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` | `ARmonia/Blocks/VoxelLit` |
| `Voxel_Wood.prefab` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` | `ARmonia/Blocks/VoxelLit` |
| `Voxel_Grass.prefab` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` | `ARmonia/Blocks/VoxelLit` |
| `Voxel_Torch.prefab` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` + `Light (URP)` | `ARmonia/Blocks/VoxelLit` |
| `Pebble_Stone.prefab` | `ProceduralPebble` + `BlockDestroy` + `BlockSpawn` + `PebbleSupport` + `MeshCollider (convex)` | material asignado en Inspector |

**Bloques obsoletos (`_Deprecated/`, 5):** `Voxel_Dirt`, `Voxel_Sand`, `Voxel_Stone`, `Voxel_Torch`, `Voxel_Wood` — versiones anteriores conservadas como respaldo.

**VFX (2):**

| Prefab | Componente | Descripción |
|--------|------------|-------------|
| `VFX_BlockPlace.prefab` | `VFXBlockPlace` + `ParticleSystem` | Burst de partículas + scale pop al colocar un bloque. Auto-destrucción a los 0.8s. |
| `VFX_BlockBreak.prefab` | `VFXBlockDestroy` + `ParticleSystem` | Burst de cubitos con gravedad al destruir. Auto-destrucción a 1.0s. |

**AR (2):** `AR_Default_Plane.prefab`, `AR_RayInteractor.prefab`.

### Materiales (8 archivos)

| Material | Shader | Uso |
|----------|--------|-----|
| `M_ARGround.mat` | `ARmonia/AR/ARPlane` | Suelo AR: arena zen con grid superpuesto, sombras, pulse y shimmer. |
| `M_GridLines.mat` | Vertex colour shader | Líneas del `GridVisualizer` (mesh procedural). |
| `M_BlockDirt.mat` | `ARmonia/Blocks/VoxelLit` | Bloque tierra (obsoleto, reemplazado por Sand). |
| `M_BlockSand.mat` | `ARmonia/Blocks/VoxelLit` | Bloque arena. |
| `M_BlockStone.mat` | `ARmonia/Blocks/VoxelLit` | Bloque piedra. |
| `M_BlockTorch.mat` | `ARmonia/Blocks/VoxelLit` | Bloque antorcha (con emisión). |
| `M_BlockWood.mat` | `ARmonia/Blocks/VoxelLit` | Bloque madera. |
| `M_Sand.mat` | (variante) | Material auxiliar arena. |

Texturas auxiliares en `Textures/AR/`: `T_Sand.png` (patrón arena), `T_ZenFloor.png` (textura generada).

### Audio (40 archivos)

**Música (12 tracks MP3):** `Door`, `Subwoofer Lullaby`, `Living Mice`, `Minecraft`,
`Oxygène`, `Équinoxe`, `Mice on Venus`, `Dry Hands`, `Wet Hands`, `Sweden`,
`Alpha`, `Moog City 2`.

**SFX UI (5 clips):** `SFX_LevelUp.mp3`, `SFX_MenuClick.mp3`,
`SFX_ToastComplete.mp3`, `SFX_Orb.mp3`, `SFX_ButtonPress.mp3`.

**SFX Voxels (23 clips):**

| Categoría | Clips |
|-----------|-------|
| Arena | `Sand_hit4`, `Sand_mining2`, `Sand_mining4`, `Sand_mining5` |
| Piedra | `Stone_dig1–4`, `Stone_hit2` |
| Madera | `Wood_dig1–3` |
| Cristal | `Glass_dig1–3` |
| Hierba | `Grass_dig2–4` |
| Pinceles | `Brushing_generic2–3` |
| Arado | `Hoe_till1–4` |
| General | `SFX_BlockPlace`, `Random_break` |

### Modelos 3D (5 archivos)

**`Models/Blocks/` (5 archivos `.glb`):** `Model_Glass.glb`,
`Model_Grass.glb`, `Model_Stone.glb`, `Model_Torch.glb`, `Model_Wood.glb`.

> **Nota:** Unity importa estos `.glb` como meshes con texturas embebidas.

### Texturas e iconos (22 archivos)

**Texturas AR (`Textures/AR/`, 2):** `T_Sand.png`, `T_ZenFloor.png`.

**Iconos app (`Textures/Icons/`, 2):** `ARmoniaIcon.png`, `ARmoniaIconBackground.jpg`.

**Sprites UI (`Textures/UI/`, 18 PNG):** `Icon_Barrier`, `Icon_Brush`,
`Icon_Cobblestone`, `Icon_CommandBlock`, `Icon_DiamondPickaxe`, `Icon_Glass`,
`Icon_GoldenHoe`, `Icon_GrassBlock`, `Icon_Light`, `Icon_OakPlanks`,
`Icon_OakWood`, `Icon_Sand`, `Icon_Stone`, `Icon_Torch`,
`UI_Background`, `UI_Book`, `UI_DemoBg`, `UI_Gui`, `UI_Icons`.

### Fuentes (5 archivos)

| Archivo | Tipo | Uso |
|---------|------|-----|
| `Minecraft.ttf` | TrueType | Fuente base pixel Minecraft. |
| `Minecraft SDF.asset` | TMP SDF Font Asset | Versión SDF para TextMeshPro. |
| `minecraft_fot_esp.ttf` | TrueType | Variante con caracteres españoles (ñ, acentos). |
| `minecraft_fot_esp SDF.asset` | TMP SDF Font Asset | Versión SDF con soporte español. |
| `Text StyleSheet.asset` | TMP Style Sheet | Hoja de estilos de TextMeshPro. |

### Shaders (2 archivos)

Ver sección [Shaders personalizados](#shaders-personalizados).

---

## Arquitectura del WorldContainer

Todo el mundo de bloques cuelga de un único `Transform` llamado **WorldContainer**.

```text
[ARAnchor]                      ← Creado por ARWorldManager en el primer tap
  └── WorldContainer            ← localScale controla la escala del mundo entero
        ├── Voxel_Sand(Clone)   ← Bloques hijos, posición local = posición en grid
        ├── Voxel_Stone(Clone)
        ├── Pebble_Stone(Clone) ← Decoraciones (posición libre, sin grid)
        └── Dynamic_GridVisual  ← Rejilla visual (MeshFilter con líneas procedurales)
```

**¿Por qué esta estructura?**

1. **Escalabilidad:** Cambiando `WorldContainer.localScale` con un solo float
   (`WorldModeSO.WorldContainerScale`) pasamos de modo Bonsái (0.02 = 2cm/bloque) a
   Normal (0.10 = 10cm/bloque) a Real (1.0 = 1m/bloque) sin tocar nada más.
2. **Estabilidad AR:** Al parentar WorldContainer a un `ARAnchor`, ARCore compensa
   el drift de tracking automáticamente.
3. **Snap correcto:** `GridManager.GetSnappedPosition()` trabaja en **espacio local**
   del contenedor (`Floor + half-cell offset`), así el snap funciona igual en
   cualquier escala.
4. **Reset limpio:** `WorldResetService` itera los hijos de WorldContainer en
   reversa, destruyendo solo los que tienen `VoxelBlock` o `ProceduralPebble` sin
   tocar el grid visual ni otros objetos de infraestructura.

---

## Modos de escala del mundo

Configurados por `WorldModeSO` (ScriptableObject, uno por modo) y aplicados al
inicio de la escena por `WorldModeBootstrapper`:

| Modo | Escala | Ancla | Uso | MaxBlocks |
|------|--------|-------|-----|-----------|
| **Bonsái** | 0.02 (2cm/bloque) | `ARTrackedImageManager` (imagen impresa de 20cm) | Jardín miniatura sobre una carta/poster | Configurable |
| **Normal** | 0.10 (10cm/bloque) | `ARPlaneManager` (suelo detectado) | Escala mesa/suelo — modo por defecto | 0 (ilimitado) |
| **Real** | 1.00 (1m/bloque) | `ARPlaneManager` (suelo detectado) | Escala Minecraft real, caminas entre bloques | 0 (ilimitado) |

`WorldModeContext` es un `static class` que transporta la selección de modo entre
escenas sin `DontDestroyOnLoad`. Mientras no exista la pantalla de inicio,
`WorldModeBootstrapper._devOverrideMode` lo fuerza desde el Inspector.

El bootstrapper activa `ARPlaneManager` o `ARTrackedImageManager` según el modo
seleccionado, desactivando el otro. Para Bonsái necesita una
`XRReferenceImageLibrary` configurada en el `WorldModeSO`.

---

## Mapa de comunicación entre sistemas

### Flujo de activación del Brush

```text
Btn_Brush.OnClick → BrushTool.ToggleBrush()     ← llamada DIRECTA, no pasa por ToolManager
  │
  ├─ IsBrushActive = !IsBrushActive
  ├─ event OnBrushToggled(bool)
  │     └─ BrushHUD.RefreshVisual() → dim/restore botón
  └─ UIAudioService.PlayToggle()

Nota: el Brush es un MODE OVERLAY, no una herramienta normal.
      No pasa por UIManager ni ToolManager.
      Btn_Brush.OnClick apunta directamente a BrushTool.ToggleBrush().
```

### Flujo principal: del dedo al bloque

```text
Touch (Enhanced Touch API, TouchPhase.Began)
  │
  ▼
TouchInputRouter.Update()      ← Ignora toques sobre UI (IsPointerOverGameObject)
  │
  ├─ BrushTool.IsBrushActive?
  │   └─ Sí → BrushTool.Update() se come el touch
  │            ├─ TouchPhase.Moved/Stationary cada _strokeCooldown (0.08s)
  │            ├─ Si ToolManager.IsBuildTool → ARBlockPlacer.TryPlaceBlock()
  │            ├─ Si Tool_Destroy → BlockDestroyer.TryDestroyBlock()
  │            └─ Si Tool_Plow → PlowTool.PlacePebbleAtScreen()
  │
  ├─ ToolManager.IsBuildTool? → ARBlockPlacer.TryPlaceBlock()
  │     │
  │     ├─ Physics.Raycast(_voxelLayerMask)  → Hit bloque existente → stacking
  │     │     └─ hitPoint + hitNormal * gridSize → nueva posición local
  │     │
  │     └─ ARRaycastManager.Raycast(TrackableType.PlaneWithinPolygon)
  │           └─ Hit plano AR → primer bloque
  │                 │
  │                 └─ Si !IsWorldAnchored:
  │                      ARWorldManager.AnchorWorld(hitPose, camera)
  │                        ├─ Posiciona WorldContainer en hitPoint
  │                        ├─ Orienta forward hacia el jugador (solo XZ)
  │                        ├─ Crea ARAnchor (prefab o manual)
  │                        ├─ Parenta WorldContainer al anchor
  │                        └─ GridManager.ActivateGrid(camera)
  │                              └─ GridVisualizer.Activate()
  │
  │  ProcessAndPlace(rawLocalPosition)
  │     ├─ GridManager.GetSnappedPosition()   ← Floor(pos/gridSize)*gridSize + half
  │     ├─ Validaciones:
  │     │   ├─ Distancia máxima (_maxBuildDistance)
  │     │   ├─ Distancia mínima (_minPlaceDistance)
  │     │   ├─ Overlap check (Physics.CheckBox con _overlapTolerance)
  │     │   └─ _pendingCells.Contains() — celda ya reservada durante animación
  │     ├─ _pendingCells.Add(snappedPos)      ← Reserva celda
  │     ├─ Instantiate(prefab, WorldContainer)
  │     ├─ BlockDestroy.InjectSharedRefs(_breakVfxPrefab, _audioService)
  │     ├─ BlockSpawn.Play(camera, onComplete)
  │     │     ├─ Fase 1 (80%): vuelo local desde cámara, scale 0→peakScale (1.15)
  │     │     ├─ Fase 2 (20%): settle peakScale→1.0
  │     │     ├─ Re-enable collider + BlockDestroy
  │     │     └─ onComplete → _pendingCells.Remove()
  │     ├─ VFX_BlockPlace instantiated en posición
  │     ├─ GameAudioService.PlayOneShot(voxelBlock.PlaceSounds)
  │     ├─ UndoRedoService.Record(new PlaceBlockAction(...))
  │     └─ HarmonyService.NotifyBlockPlaced(blockType)
  │
  └─ ToolManager.CurrentTool == Tool_Destroy? → BlockDestroyer.TryDestroyBlock()
        ├─ Physics.Raycast(_voxelLayerMask | _pebbleLayerMask)
        ├─ UndoRedoService.Record(new DestroyBlockAction(...))
        ├─ BlockDestroy.BreakFromTool(hitNormal)
        │     ├─ Audio: VoxelBlock.BreakSounds o inyectados por PlowTool
        │     ├─ VFX_BlockBreak instantiated
        │     ├─ Unparent del WorldContainer
        │     ├─ AddComponent<Rigidbody>() + AddForce(kickDir * knockForce)
        │     ├─ AddTorque(random * knockForce * 3)
        │     ├─ WaitForSeconds(_destroyDelay = 0.12s)
        │     ├─ Shrink to zero (_shrinkDuration = 0.18s)
        │     └─ Destroy(gameObject)
        └─ HarmonyService.NotifyBlockDestroyed(blockType)
```

### Proximidad knock (auto-destrucción por cámara)

```text
BlockDestroy.Update()    ← Solo si _ready == true (post-spawn)
  │
  └─ sqrDistance(camera, block) <= _knockRadius² (0.18m)?
       └─ StartCoroutine(KnockRoutine())  ← Mismo flujo que BreakFromTool
          pero dirección = away from camera + Vector3.up * 0.6
```

### Flujo de Armonía → UI

```text
HarmonyService.Recalculate()     ← Solo cuando algo cambia, nunca en Update()
  │
  ├─ ScoreVariety()     → distinctTypes / fullVarietyTypeCount (6)
  │                        Peso: 0.45
  ├─ ScoreDecoration()  → totalPebbles / targetPebbleCount (25)
  │                        Peso: 0.35
  ├─ ScoreQuantity()    → totalBlocks / targetBlockCount (50)
  │                        Peso: 0.20
  ├─ ScoreMinimumGate() → penaliza proporcionalmente si:
  │                        Sand < minSandBlocks (10) OR Grass < minGrassBlocks (10)
  │                        gateStrength = 0.85
  │
  ▼
  float score [0..1]  (threshold anti-jitter de 0.005)
  │
  ├─ event OnHarmonyChanged(score)
  │     └─ HarmonyHUD.SetHarmony(score)
  │           ├─ Anima anchorMax.x de _fillRect (coroutine)
  │           ├─ Color: gradiente _colourLow(rojo) → _colourMid(amarillo) → _colourHigh(verde)
  │           ├─ Frase por fase:
  │           │   ├─ [0.00–0.25) "Empieza tu jardín"
  │           │   ├─ [0.25–0.50) "Añade más variedad"
  │           │   ├─ [0.50–0.75) "Jardín equilibrado"
  │           │   ├─ [0.75–1.00) "Gran armonía ✦"
  │           │   └─ [1.00]      "¡Armonía perfecta! ✨"
  │           ├─ Pop animation (scale 1.0→1.20→1.0 en 0.45s)
  │           ├─ Shake animation (offset ±7px en 0.28s)
  │           └─ UIAudioService.PlayHarmonyPhase(1..4)
  │
  └─ event OnPerfectHarmony (solo una vez por sesión)
        └─ PerfectHarmonyPanel
              ├─ CanvasGroup fade in (0.6s, SmoothStep)
              ├─ Btn_Continue fade in simultáneo
              ├─ HarmonyParticles.Play()
              │     ├─ Burst: 120 partículas × 3 repeticiones (1.1s entre cada una)
              │     ├─ Posición: 1.2m frente a la cámara AR
              │     ├─ Colores: gradiente dorado → pastel → lavanda → blanco
              │     └─ Ambient: 5 partículas/s continuas en radio 0.7m
              ├─ UIAudioService.PlayConfirm()
              └─ Btn_Continue → fade out (0.35s)
```

### Flujo de Undo/Redo

```text
UndoRedoService (Stack<IUndoableAction>, cap = 20 configurable)
  │
  ├─ Record(PlaceBlockAction)     ← ARBlockPlacer tras cada colocación
  ├─ Record(DestroyBlockAction)   ← BlockDestroyer antes de cada destrucción
  │     └─ Cada Record() limpia la pila de Redo
  │     └─ Si undoStack > _maxHistory → TrimBottom() (O(n), solo al cap)
  │
  ├─ Undo()
  │   ├─ PlaceBlockAction.Undo()   → Destroy(instance)
  │   └─ DestroyBlockAction.Undo() → Instantiate + SetLocalPosition + arm BlockDestroy
  │
  ├─ Redo()
  │   ├─ PlaceBlockAction.Redo()   → Instantiate + arm
  │   └─ DestroyBlockAction.Redo() → Destroy(restoredInstance)
  │
  ├─ event OnStackChanged(canUndo, canRedo)
  │     └─ UndoRedoHUD.RefreshState()
  │           ├─ Button.interactable = canUndo/canRedo
  │           └─ Icon alpha: _alphaEnabled(1.0) / _alphaDisabled(0.35)
  │
  └─ Tras cada undo/redo:
       HarmonyService.NotifyUndoRedo() → RebuildCounters() + Recalculate()
```

### Flujo del Decorador de Piedritas (PlowTool)

```text
PlowTool.Update()   (solo cuando ToolManager.CurrentTool == Tool_Plow)
  │
  ├─ Touch.Began → TryPlacePebble(screenPos)
  │     ├─ Physics.Raycast(_voxelLayerMask) → sobre tapa/cara de un bloque
  │     └─ ARRaycastManager.Raycast → sobre suelo AR
  │     └─ Validaciones: distancia mín/máx
  │
  └─ PlaceAt(worldPoint, surfaceNormal, onARPlane)
        ├─ Random prefab de _pebblePrefabs[]
        ├─ Scatter: Random.insideUnitCircle * _scatterRadius
        │     proyectado en tangente/bitangente de surfaceNormal
        ├─ Rotación: Quaternion.FromToRotation(up, normal) * AngleAxis(random 0–360)
        ├─ Escala: Random.Range(_scaleMin, _scaleMax) * prefab.localScale
        ├─ PebbleSupport.Configure(onARPlane, _voxelLayerMask, surfaceNormal)
        │     └─ Si onARPlane → nunca auto-break
        │     └─ Si onBlock → InvokeRepeating(Poll, 0.35s)
        │           └─ Raycast hacia -surfaceNormal, _checkDistance 0.20m
        │           └─ Si no hay soporte → BlockDestroy.BreakFromTool(-supportDir)
        ├─ BlockDestroy.InjectSharedRefs(_breakVfxPrefab, _audioService, _breakSounds)
        ├─ BlockSpawn.Play(camera, onComplete)
        │     └─ onComplete → BlockDestroy.SetReady() + PebbleSupport.Arm()
        ├─ GameAudioService.PlayOneShot(_placeSounds)
        └─ HarmonyService.NotifyPebblePlaced()

BrushTool + Tool_Plow activos:
  → PlowTool.PlacePebbleAtScreen() cada _brushCooldown (0.06s)
```

### Flujo de World Reset

```text
GameOptionsMenu.RequestClearAll() → Popup_ConfirmClearAll visible
  │
  ├─ Btn_Confirm → GameOptionsMenu.ConfirmClearAll()
  │     └─ WorldResetService.ResetWorld()
  │           ├─ DestroyAllBlocks()
  │           │     └─ Itera WorldContainer.children en reversa
  │           │        Solo destruye si tiene VoxelBlock o ProceduralPebble
  │           ├─ ARWorldManager.ResetAnchor()
  │           │     └─ Destruye ARAnchor, un-parent WorldContainer
  │           ├─ GridManager.DeactivateGrid()
  │           │     └─ GridVisualizer.Deactivate() → Destroy mesh
  │           ├─ UndoRedoService.Clear()
  │           ├─ HarmonyService.NotifyWorldReset()
  │           │     └─ RebuildCounters() (→ todo a 0) + Recalculate()
  │           │     └─ OnWorldReset?.Invoke()
  │           │     └─ _perfectFired = false (permite re-trigger)
  │           └─ event OnWorldReset
  │                 ├─ PerfectHarmonyPanel.HandleWorldReset()
  │                 │     └─ StopAmbient() + hide panel
  │                 └─ HarmonyHUD desfreeze
  │
  └─ Btn_Cancel → GameOptionsMenu.CancelClearAll()
        └─ Oculta popup, UIAudioService.PlayCancel()
```

### Flujo del Menú de Opciones

```text
Btn_Settings → GameOptionsMenu.ToggleMenu()
  ├─ Panel_OptionsDropdown.SetActive(toggle)
  ├─ HUD_MenuBlocker.SetActive(toggle)    ← cierra al tocar fuera
  └─ UIAudioService.PlayMenuOpen()

Botones dentro del dropdown:
  ├─ Btn_Lighting → ToggleLighting()
  │     └─ LightingService.ToggleLighting()
  │           ├─ CameraFlashLight (SpotLight) ON/OFF
  │           ├─ Directional Light OFF/ON (si _disableGlobalOnFocus)
  │           └─ event OnLightingToggled → DropdownButtonState actualiza color
  ├─ Btn_Depth → ToggleDepth()
  │     └─ ARDepthService.ToggleDepth()
  │           ├─ AROcclusionManager.requestedEnvironmentDepthMode
  │           └─ AROcclusionManager.requestedHumanDepthMode
  ├─ Btn_Grid → ToggleGrid()
  │     └─ ARPlaneGridAligner.SetGrid(bool) → MaterialPropertyBlock _GridEnabled
  ├─ Btn_Plane → TogglePlaneVisual()
  │     └─ ARPlaneGridAligner.SetVisual(bool) → MeshRenderer.enabled en planos
  ├─ Sld_MusicVolume → OnMusicVolumeChanged(0–100)
  │     └─ MusicService.SetVolume(0–1)
  ├─ Btn_Photo → TakePhoto()
  │     └─ ScreenshotService.Capture()
  │           ├─ Canvas.enabled = false
  │           ├─ WaitForEndOfFrame
  │           ├─ ScreenCapture.CaptureScreenshot("ARmonia_yyyyMMdd_HHmmss.png")
  │           ├─ Canvas.enabled = true
  │           └─ event OnScreenshotCaptured → ToggleMenu() (cierra menú)
  ├─ Btn_ClearAll → RequestClearAll() (ver flujo de reset)
  └─ Btn_Exit → Application.Quit()
```

### Flujo de Orientación

```text
OrientationManager.Update()
  │
  └─ Screen.width > Screen.height? → cambió?
       ├─ Landscape:
       │   ├─ Oculta HUD_Hotbar, HUD_ToolPanel, HUD_Selector
       │   ├─ Guarda _previousTool
       │   └─ ToolManager.SelectToolByIndex(Tool_None)
       │
       └─ Portrait:
           ├─ Muestra HUD_Hotbar, HUD_ToolPanel, HUD_Selector
           └─ WaitForEndOfFrame → Restaura _previousTool
```

---

## Inventario y herramientas

| Slot | `ToolType` | Valor int | Qué hace |
|------|-----------|-----------|----------|
| Arena | `Build_Sand` | 0 | Bloque 1×1×1 de arena. Mínimo 10 para gate de armonía. |
| Cristal | `Build_Glass` | 1 | Bloque translúcido. |
| Piedra | `Build_Stone` | 2 | Bloque sólido de piedra. |
| Madera | `Build_Wood` | 3 | Bloque de madera. |
| Antorcha | `Build_Torch` | 4 | Bloque antorcha con URP Light component. |
| Hierba | `Build_Grass` | 5 | Bloque verde. Mínimo 10 para gate de armonía. |
| Vacío | `Tool_None` | 6 | Mano vacía. Toque no hace nada. |
| Destruir | `Tool_Destroy` | 7 | Raycast físico → `BlockDestroy.BreakFromTool()`. Funciona sobre bloques y piedritas. |
| Pincel | `Tool_Brush` | 8 | **Mode overlay** — toggle ON/OFF. `Btn_Brush.OnClick` llama directamente a `BrushTool.ToggleBrush()`, no pasa por `UIManager`. Arrastra para placement/destroy continuo cada 0.08s. Compatible con build, destroy y plow. |

**Conversión:** `ToolManager` castea `(BlockType)(int)CurrentTool` para obtener el
prefab de `BlockDatabase`. Los valores 0-5 de `ToolType` coinciden 1:1 con `BlockType`.

**ADVERTENCIA:** Los valores int de `ToolType` 0–7 y 9 están baked en los `OnClick` events
de los botones de la escena. No se deben cambiar. `Tool_Brush (8)` es excepción — su botón
llama directamente a `BrushTool.ToggleBrush()`, no usa el valor int.

---

## Shaders personalizados

### `ARmonia/AR/ARPlane` (`ARPlane.shader`)

Shader HLSL para URP que renderiza el suelo AR como arena zen estilizada.

- **5 tonos de arena** configurables (`_Sand0` a `_Sand4`) con distribución por hash.
- **Grid superpuesto:** líneas menores cada `_CellSize` (0.1m) y mayores cada
  `_MajorEvery` celdas (10). Toggle runtime via `_GridEnabled` (MaterialPropertyBlock).
- **Animación:** pulse sinusoidal en líneas menores, shimmer en mayores.
- **`_GridMatrix`:** matriz `worldToLocal` del WorldContainer inyectada por
  `ARPlaneGridAligner` para que el grid del shader se alinee con la rejilla de voxels.
- **Sombras:** Recibe sombras del directional light (atenuación 0.55).
- **Render:** `Transparent`, `ZWrite Off`, `Cull Off`, `Blend SrcAlpha OneMinusSrcAlpha`.

### `ARmonia/Blocks/VoxelLit` (`VoxelLit.shader`)

Shader HLSL para URP que ilumina los bloques voxel con estética Minecraft.

- **Albedo point-sampled:** texturas pixel-art sin filtrado bilineal.
- **Toon lighting 3 bandas:** `_BandLight` (0.6), `_BandMid` (0.25), con
  `_MidScale` (0.55) y `_ShadowScale` (0.25).
- **Vertex colour AO:** canal R del vertex colour como oclusión ambiental
  (`_AmbientOcclusion` = 0.6).
- **Emisión:** `_EmissionColor` + `_EmissionIntensity` para antorchas.
- **Shadow strength:** `_ShadowStrength` (0.55) mezclada con URP shadow attenuation.
- **Ambient:** Spherical Harmonics (URP SH).
- **Fog:** URP fog support.
- **Passes extra:** `ShadowCaster` y `DepthOnly` vía UsePass de URP/Lit.

---

## Pantalla de inicio (planificada — aún no implementada)

Según el GDD:

1. **Cámara frontal** con Face Tracking → máscara de cara de Creeper sobre la cara
   del jugador.
2. **Hand Tracking** (MediaPipe, ya instalado como paquete local) → el dedo índice
   controla un cursor con forma de antorcha.
3. La antorcha se posa sobre botones y tras un **Dwell Time de 2 segundos** se
   activa la opción (sin tocar la pantalla).
4. Botones: selección de modo (Bonsái / Normal / Real) y "Jugar".
5. Al pulsar "Jugar", escribe `WorldModeContext.Selected = modo` y carga `Main_AR`.

**Estado actual:** MediaPipe Unity Plugin 0.16.3 está instalado como paquete local
en `Packages/com.github.homuler.mediapipe/`, pero no existe ninguna escena de
inicio, ni script de Face Tracking, ni Hand Tracking, ni Dwell Time. Solo existe
`Main_AR.unity`.

---

## Lista completa de scripts (49)

### AR (4 scripts) — `_Project.Scripts.AR`

| Script | Líneas | Responsabilidad |
|--------|--------|----------------|
| `ARDepthService` | ~140 | Toggle runtime de `AROcclusionManager`. Modos: `EnvironmentDepthMode.Fastest`, `HumanSegmentationDepthMode.Fastest`. Evento `OnDepthToggled`. Default OFF. |
| `ARPlaneGridAligner` | ~100 | Inyecta `WorldContainer.worldToLocalMatrix` en cada plano AR como `_GridMatrix` via `MaterialPropertyBlock`. Controla `_GridEnabled` y `MeshRenderer.enabled` de los planos. |
| `ARWorldManager` | ~140 | Crea `ARAnchor` en el primer hit, orienta WorldContainer.forward hacia el jugador (solo XZ, con fallback si forward ≈ up), parenta WorldContainer, activa `GridManager.ActivateGrid()`. `ResetAnchor()` destruye el anchor y libera WorldContainer. |
| `WorldModeBootstrapper` | ~160 | Lee `WorldModeContext.Selected`, busca el `WorldModeSO` correspondiente en `_modeConfigs[]`, aplica `WorldContainer.localScale`, habilita `ARPlaneManager` o `ARTrackedImageManager` según `AnchorType`, suscribe a `trackablesChanged` para auto-anclar. Campo `_devOverrideMode` para testing sin title screen. |

### Core (16 scripts) — `_Project.Scripts.Core`

| Script | Tipo | Responsabilidad |
|--------|------|----------------|
| `GridManager` | MonoBehaviour | Dueño de `_gridSize` (1.0). `GetSnappedPosition()`: `Floor(pos/size)*size + half`. Facade de `GridVisualizer`: `ActivateGrid()` / `DeactivateGrid()`. |
| `GridVisualizer` | MonoBehaviour | Crea un `GameObject` hijo con `MeshFilter` + `MeshRenderer`. Genera mesh de líneas con fade radial (alpha ∝ 1 - sqrDist/sqrRadius). Solo reconstruye al cambiar celda central. Buffers reutilizados (`List<Vector3>`, etc.) para zero-GC. |
| `HarmonyConfig` | ScriptableObject | `varietyWeight` (0.45), `decorationWeight` (0.35), `quantityWeight` (0.20), `fullVarietyTypeCount` (6), `targetBlockCount` (50), `targetPebbleCount` (25), `minSandBlocks` (10), `minGrassBlocks` (10), `gateStrength` (0.85). |
| `HarmonyService` | MonoBehaviour | Evalúa armonía. `Dictionary<BlockType,int>` para conteos. Eventos: `OnHarmonyChanged(float)`, `OnPerfectHarmony`, `OnWorldReset`. `NotifyBlockPlaced/Destroyed`, `NotifyPebblePlaced/Destroyed`, `NotifyWorldReset`, `NotifyUndoRedo` (→ `RebuildCounters` O(n) scan). |
| `GameAudioService` | MonoBehaviour | One-shot SFX con pitch variation (±0.15). Anti-repetición en arrays. `AudioSource` asignado via Inspector. |
| `MusicService` | MonoBehaviour | Shuffle Fisher-Yates, crossfade entre tracks (2s), volume slider. `AudioSource` dedicado (asignado en Inspector, separado de `GameAudioService`). Evento `OnVolumeChanged`. |
| `LightingService` | MonoBehaviour | Toggle entre modo Global (Directional Light ON, Spot OFF) y Focus (Directional OFF, Spot ON). Evento `OnLightingToggled(bool)`. Configurable `_disableGlobalOnFocus`. |
| `ScreenshotService` | MonoBehaviour | `Capture()` con debounce (`_isCapturing`). Oculta `_canvasToHide`, `WaitForEndOfFrame`, `ScreenCapture.CaptureScreenshot`. Evento `OnScreenshotCaptured(fileName)`. |
| `WorldResetService` | MonoBehaviour | `ResetWorld()`: destroy blocks (reversa, solo `VoxelBlock`/`ProceduralPebble`), reset anchor, deactivate grid, clear undo, reset harmony. Evento `OnWorldReset`. |
| `IUndoableAction` | Interface | Contrato `Undo()`, `Redo()`. |
| `PlaceBlockAction` | Class | Command: `Undo()` → `Destroy(instance)`. `Redo()` → `Instantiate` + posición local + callback. |
| `DestroyBlockAction` | Class | Command: `Undo()` → `Instantiate` + arm. `Redo()` → `Destroy(restoredInstance)`. |
| `UndoRedoService` | MonoBehaviour | `Stack<IUndoableAction>` × 2 con cap (`_maxHistory`, default 20). `Record()` limpia redo. `TrimBottom()` O(n) solo al cap. Evento `OnStackChanged(canUndo, canRedo)`. Tras cada undo/redo → `HarmonyService.NotifyUndoRedo()`. |
| `WorldMode` | Enum | `Bonsai = 0`, `Normal = 1`, `Real = 2`. |
| `WorldModeContext` | Static class | `static WorldMode Selected = Normal`. Cross-escena sin MonoBehaviour. |
| `WorldModeSO` | ScriptableObject | `Mode`, `DisplayName`, `WorldContainerScale`, `AnchorType` (enum: `ARPlane`/`TrackedImage`), `ImageLibrary`, `ImagePhysicalWidth`, `MaxBlocks`. |

### Interaction (9 scripts) — `_Project.Scripts.Interaction`

| Script | Tipo | Responsabilidad |
|--------|------|----------------|
| `ToolType` | Enum | 10 valores: `Build_Sand(0)` a `Build_Grass(5)`, `Tool_None(6)`, `Tool_Destroy(7)`, `Tool_Brush(8)`, `Tool_Plow(9)`. |
| `ToolManager` | MonoBehaviour | `CurrentTool` (default `Build_Sand`), `IsBuildTool` (rango 0–5), `SelectToolByIndex(int)`, `GetCurrentBlockPrefab()`, `GetBlockPrefab(BlockType)`. Evento `OnToolChanged`. |
| `TouchInputRouter` | MonoBehaviour | Punto de entrada de input táctil. Captura `Touch.Began`, filtra toques sobre UI, cede al `BrushTool` si activo, despacha a `ARBlockPlacer` o `BlockDestroyer` según herramienta. |
| `ARBlockPlacer` | MonoBehaviour | Solo colocación de bloques. `TryPlaceBlock()`, `ProcessAndPlace()`. `_pendingCells` HashSet contra double-tap. `ARRaycastManager` asignado via Inspector. Refs: ToolManager, GridManager, ARWorldManager, WorldContainer, GameAudioService, UndoRedoService, HarmonyService. |
| `BlockDestroyer` | MonoBehaviour | Solo destrucción de bloques y piedritas. `TryDestroyBlock()` con physics raycast (voxel + pebble layers). Registra `DestroyBlockAction` en `UndoRedoService`. Notifica `HarmonyService`. |
| `BrushTool` | MonoBehaviour | Toggle `IsBrushActive`. `Btn_Brush.OnClick` llama directamente a `ToggleBrush()` (no pasa por ToolManager — es un mode overlay). En Update si activo: consume `Touch.activeTouches`, llama `ARBlockPlacer`/`BlockDestroyer`/`PlowTool` cada `_strokeCooldown` (0.08s). Evento `OnBrushToggled(bool)`. |
| `PlowTool` | MonoBehaviour | Decorador de piedritas. Raycast propio (voxel + AR). `PlaceAt()`: scatter, normal alignment, random scale/rotation, `PebbleSupport.Configure()`, `BlockSpawn.Play()`, `HarmonyService.NotifyPebblePlaced()`. `PlacePebbleAtScreen()` para uso desde BrushTool. |
| `DebugRayVisualizer` | MonoBehaviour | Dibuja rayo de 0.1s desde cámara en cada tap. Toggle `_enabled`. `LineRenderer` asignado via Inspector. |

### UI (11 scripts) — `_Project.Scripts.UI`

| Script | Tipo | Responsabilidad |
|--------|------|----------------|
| `UIManager` | MonoBehaviour | Selector highlight (`_selectorRect`) que sigue al slot activo. `_slotRects[]` indexado por valor int de `ToolType`. `OnSlotClicked(int)` delega a `ToolManager.SelectToolByIndex()`. Delay de 0.1s para que los Layout Groups se asienten antes de posicionar. |
| `HarmonyHUD` | MonoBehaviour | Barra fill animada (`_fillRect.anchorMax.x`), gradiente tricolor, 5 frases por fase, pop/shake, esquinas redondeadas procedurales. Flag `_frozen` para post-perfect. |
| `HarmonyParticles` | MonoBehaviour | `[RequireComponent(ParticleSystem)]`. Configura ParticleSystem proceduralmente en `Awake`. Burst 120 particulas por 3 repeticiones. Ambient 5/s continuas. Colores: dorado, melocoton, lavanda, blanco. Se posiciona frente a `Camera.main`. |
| `PerfectHarmonyPanel` | MonoBehaviour | `[RequireComponent(CanvasGroup)]`. Auto-localiza `HarmonyParticles`, `UIAudioService`. Fade in/out con SmoothStep. Suscrito a `HarmonyService.OnPerfectHarmony` y `OnWorldReset`. |
| `UndoRedoHUD` | MonoBehaviour | Botones `_undoButton`/`_redoButton` con iconos. Suscrito a `UndoRedoService.OnStackChanged`. Alpha enabled/disabled (1.0/0.35). `OnUndoPressed()`/`OnRedoPressed()`. |
| `BrushHUD` | MonoBehaviour | Suscrito a `BrushTool.OnBrushToggled`. Dim/restore de `Image.color` con `_dimFactor` (0.45) cuando brush está OFF/ON. Mismo patrón que `UndoRedoHUD`. |
| `GameOptionsMenu` | MonoBehaviour | Controlador UI del dropdown de opciones. Panels: `_optionsPanel`, `_blockerPanel`, `_confirmPopup`. Delega a `LightingService`, `ARDepthService`, `ARPlaneGridAligner`, `MusicService`, `WorldResetService`, `ScreenshotService`. Slider de música (0–100). |
| `OrientationManager` | MonoBehaviour | Detecta portrait/landscape en `Update()` (`Screen.width > Screen.height`). Oculta hotbar, tool panel, selector en landscape. Fuerza `Tool_None`. Restaura `_previousTool` en portrait con `WaitForEndOfFrame`. |
| `UIAudioService` | MonoBehaviour | `[RequireComponent(AudioSource)]`. 7 pools de clips: click, toggle, menuOpen, confirm, cancel, slotSelect, photo. 4 clips individuales para fases de armonia. Pitch variation ±0.05. Anti-repeticion por pool. |
| `ButtonPressAnimation` | MonoBehaviour | `[RequireComponent(Button)]`. `IPointerDownHandler` + `IPointerUpHandler`. Squeeze scale-down y scale-up automatico en cada boton. |
| `DropdownButtonState` | MonoBehaviour | Dim/restore de `Image.color` para toggles ON/OFF en el dropdown de opciones. `_dimFactor` configurable. `SetState(bool)`. Usado por `Btn_Lighting`, `Btn_Depth`, `Btn_Grid`, `Btn_Plane`. |

### Voxel (9 scripts) — `_Project.Scripts.Voxel`

| Script | Tipo | Responsabilidad |
|--------|------|----------------|
| `BlockType` | Enum | `Sand(0)`, `Glass(1)`, `Stone(2)`, `Wood(3)`, `Torch(4)`, `Grass(5)`. |
| `BlockDatabase` | ScriptableObject | Array de `BlockEntry` (type + prefab). Lazy `Dictionary<BlockType,GameObject>` para O(1). `GetPrefab()`, `TryGetPrefab()`, `Count`. |
| `VoxelBlock` | MonoBehaviour | `_blockType`, `_placeSounds[]`, `_breakSounds[]`. Properties de solo lectura. |
| `BlockSpawn` | MonoBehaviour | Coroutine en espacio local. Fase 1 (80%): vuelo ease-out cubico, scale de 0 a 1.15. Fase 2 (20%): settle de 1.15 a 1.0. Deshabilita `Collider` y `BlockDestroy` durante vuelo. `_cameraLocalOffset` = (0, -0.12, 0.15). |
| `BlockDestroy` | MonoBehaviour | Proximidad knock (`_knockRadius` 0.18m, solo post-`SetReady()`). `BreakFromTool(hitNormal)`: unparent, `Rigidbody` + impulso + torque, VFX, audio, tumble (`_destroyDelay` 0.12s), shrink (`_shrinkDuration` 0.18s), `Destroy`. `InjectSharedRefs()` para VFX y audio sin duplicar en prefabs. |
| `ProceduralPebble` | MonoBehaviour | `[RequireComponent(MeshFilter, MeshRenderer, MeshCollider)]`. Genera mesh icosaedro jittered en `Awake`. Flat shading (verts duplicados por triangulo). Base plana (Y menor que 0 se pone a Y=0). Box-projection UV. Seed 0 = random. |
| `PebbleSupport` | MonoBehaviour | Poll periodico (`InvokeRepeating`, `_checkInterval` 0.35s). Raycast hacia `-_supportDir` (`_checkDistance` 0.20m, solo `_voxelMask`). Si no hay apoyo, llama a `BlockDestroy.BreakFromTool()`. Si `_onARPlane`, nunca auto-break. |
| `VFXBlockPlace` | MonoBehaviour | ParticleSystem burst (10 a 16 particulas) + scale pop (1.18x en 0.06s, luego 1.0 en 0.14s). Auto-destroy a 0.8s. Configura todo proceduralmente en `Start()`. |
| `VFXBlockDestroy` | MonoBehaviour | ParticleSystem burst de cubitos (`Cube.fbx`) con gravedad 2.0, tumble rotacional, fade alpha. Auto-destroy a 1.0s. Configura todo proceduralmente en `Start()`. |

---

## Estado del proyecto

### Funcionalidades completas

- **AR Foundation:** deteccion de planos, ancla espacial, oclusion por profundidad (toggle), alineacion de grid al shader del plano.
- **Construccion voxel:** tap para colocar, stacking por caras, snap a grid, reserva de celda contra double-tap.
- **6 tipos de bloque:** Sand, Glass, Stone, Wood, Torch, Grass con prefabs, sonidos y VFX diferenciados.
- **Herramienta Destruir:** raycast fisico, impulso con Rigidbody, tumble, shrink, VFX. Funciona sobre bloques y piedritas.
- **Pincel Rapido:** toggle ON/OFF, placement/destroy continuo, cooldown 0.08s, compatible con todos los modos.
- **Decorador de Piedritas:** piedras procedurales icosaedro, rotacion/escala/scatter aleatorio, alineacion a normal, soporte con auto-destruccion.
- **Grid visual:** mesh procedural de lineas con fade radial, zero-GC, regenera solo al cambiar celda.
- **Sistema de Armonia:** 3 pilares + gate de minimos, 100% event-driven, zero polling.
- **HarmonyHUD:** barra animada, gradiente tricolor, frases por fase, pop/shake, esquinas redondeadas procedurales.
- **Panel Armonia Perfecta:** fade, particulas procedurales multicolor, boton Continuar.
- **Undo/Redo:** patron Command, stack con cap de 20, HUD con botones atenuados.
- **Menu opciones:** 7 toggles/acciones (iluminacion, profundidad, grid, plano visual, musica, foto, reset).
- **Audio:** `GameAudioService` (SFX con pitch variation), `UIAudioService` (7 pools + 4 fases armonia), `MusicService` (shuffle, crossfade, slider).
- **Screenshot:** captura sin UI visible, timestamp en nombre de archivo.
- **Orientacion:** oculta hotbar en landscape, restaura en portrait.
- **Modos de escala:** Bonsai/Normal/Real con bootstrapper.
- **Proximidad knock:** auto-destruccion si la camara entra en 0.18m del bloque.
- **2 shaders HLSL personalizados:** arena zen con grid animado y voxel toon-lit con AO.
- **Animacion de botones:** squeeze automatico en cada `Btn_*`.
- **Botones de estado:** dim visual para toggles ON/OFF.

### Funcionalidades a medias

- **Modo Bonsai:** Codigo del bootstrapper completo, pero no hay `XRReferenceImageLibrary` configurada. Falta testing real.
- **Pebble Undo/Redo:** Bloques voxel tienen undo/redo completo. Las piedritas del `PlowTool` **no** se registran en `UndoRedoService`.

### Funcionalidades no implementadas

| Feature | Detalle |
|---------|---------|
| Escena de inicio | Camara frontal, Face Tracking (mascara Creeper), Hand Tracking (cursor antorcha), Dwell Time 2s, seleccion de modo. MediaPipe instalado pero sin scripts. |
| Guardado/Carga | No hay serializacion. Al cerrar la app se pierde el jardin. |
| Tutorial / Onboarding | No hay guia para jugadores nuevos. |
| Logros / Progresion | No hay sistema mas alla de la barra de armonia. |
| Luz dinamica de antorchas | El prefab Torch tiene un `Light` component de URP pero no esta configurado como punto de luz emitiendo. |
| Agua / Bloques animados | No hay shaders animados ni bloque de agua. |
| Sonido ambiente adaptativo | No hay sonidos de naturaleza que cambien con el jardin. |
| Multijugador / Compartir | No hay networking ni exportacion del jardin. |

---

## Dependencias de paquetes

| Paquete | Version | Uso |
|---------|---------|-----|
| `com.unity.xr.arfoundation` | 6.0.6 | AR Foundation: sesion, planos, anclas, raycast, oclusion, imagenes. |
| `com.unity.xr.arcore` | 6.0.6 | ARCore XR Plugin para Android. |
| `com.unity.xr.interaction.toolkit` | 3.0.10 | XR Interaction Toolkit (XR Origin, Ray Interactor). |
| `com.unity.inputsystem` | 1.17.0 | Enhanced Touch API (input táctil). |
| `com.unity.render-pipelines.universal` | 17.0.4 | Universal Render Pipeline. |
| `com.unity.ugui` | 2.0.0 | UI Canvas, Button, Image, Slider. |
| `com.unity.cloud.gltfast` | (git) | Importador glTF para modelos `.glb` de bloques. |
| `com.unity.timeline` | 1.8.10 | Timeline (no utilizado activamente). |
| `com.unity.visualscripting` | 1.9.7 | Visual Scripting (no utilizado activamente). |
| `com.unity.ai.navigation` | 2.0.9 | AI Navigation (no utilizado activamente). |
| `com.github.homuler.mediapipe` | 0.16.3 (local) | MediaPipe Unity Plugin. Preparado para Hand/Face Tracking de la pantalla de inicio futura. |

---

## Como abrir el proyecto

1. **Unity 6** (2022.3 LTS o superior) con modulos: Android Build Support, AR
   Foundation, URP.
2. Clonar el repositorio:

   ```bash
   git clone https://github.com/Gabiz053/Juego-AR.git
   ```

3. Abrir con Unity Hub y seleccionar la carpeta raiz `Juego-AR/`.
4. Escena principal: `Assets/_Project/Scenes/Main_AR.unity`.
5. Build target: **Android** (ARCore). Probar en Samsung S24 Ultra o dispositivo
   compatible con ARCore.
6. Bundle ID: `com.Gabiz.ARmonia`.

