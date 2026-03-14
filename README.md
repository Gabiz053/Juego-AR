<!-- markdownlint-disable MD013 MD060 -->

# ARmonia -- Jardin Zen AR estilo Minecraft Earth

![Unity](https://img.shields.io/badge/Unity-6-black?style=flat-square&logo=unity)
![ARCore](https://img.shields.io/badge/ARCore-XR-blue?style=flat-square)
![Android](https://img.shields.io/badge/Android-Target_S24_Ultra-green?style=flat-square&logo=android)
![URP](https://img.shields.io/badge/Render-URP-red?style=flat-square)

Sandbox creativo de Realidad Aumentada para un jugador. Construyes un jardin zen colocando bloques voxel 1x1x1 sobre superficies reales detectadas por la camara trasera. Hay seis tipos de bloque (arena, piedra, madera, cristal, hierba y antorchas) mas piedritas decorativas procedurales. La barra de **Armonia** evalua tu jardin en tiempo real segun variedad, cantidad y decoracion.

La pantalla de inicio usa la **camara frontal**: Face Tracking para poner una cara de Creeper al jugador, y Hand Tracking (MediaPipe) para seleccionar el modo de juego con gestos.

| Dato | Valor |
|------|-------|
| **Hardware de referencia** | Samsung Galaxy S24 Ultra |
| **Motor** | Unity 6 (6000.0.66f2) |
| **Pipeline** | Universal Render Pipeline (URP), OpenGLES3 |
| **AR** | AR Foundation 6.0.6 + ARCore XR Plugin 6.0.6 |
| **Input** | Enhanced Touch (Input System 1.17.0) |
| **Extras** | NativeGallery (screenshots), MediaPipe Unity Plugin 0.16.3 (Hand + Face Tracking) |
| **Bundle ID** | `com.Gabiz.ARmonia` |
| **Version** | 0.3.0 |

---

## Tabla de contenidos

1. [Como abrir el proyecto](#1-como-abrir-el-proyecto)
2. [Estructura de carpetas](#2-estructura-de-carpetas)
3. [Las dos escenas](#3-las-dos-escenas)
4. [Arquitectura](#4-arquitectura)
5. [Gameplay -- herramientas e inventario](#5-gameplay----herramientas-e-inventario)
6. [Flujos detallados](#6-flujos-detallados)
7. [Shaders personalizados](#7-shaders-personalizados)
8. [Inventario de assets](#8-inventario-de-assets)
9. [Catalogo de scripts (69)](#9-catalogo-de-scripts-69)
10. [Estado del proyecto](#10-estado-del-proyecto)
11. [Dependencias de paquetes](#11-dependencias-de-paquetes)

---

## 1. Como abrir el proyecto

1. Instala **Unity 6** (6000.0.66f2 o superior) con los modulos: Android Build Support, AR Foundation, URP.
2. Clona el repositorio:

   ```bash
   git clone https://github.com/Gabiz053/Juego-AR.git
   ```

3. Abre con Unity Hub y selecciona la carpeta raiz `Juego-AR/`.
4. **Escena de inicio:** `Assets/_Project/Scenes/Title_Screen.unity` (build index 0).
5. **Escena de juego:** `Assets/_Project/Scenes/Main_AR.unity` (build index 1).
6. Build target: **Android** (ARCore). Probar en Samsung S24 Ultra o dispositivo compatible con ARCore.
7. Bundle ID: `com.Gabiz.ARmonia`.

---

## 2. Estructura de carpetas

```text
Assets/
+-- _Project/
|   +-- Assets/                  <- ScriptableObjects y fuentes
|   |   +-- Fonts/               <- TTF + SDF assets de TextMeshPro
|   |   +-- BlockDatabase.asset
|   |   +-- HarmonyConfig.asset
|   |   +-- WorldModeConfig_Bonsai.asset
|   |   +-- WorldModeConfig_Normal.asset
|   |   +-- WorldModeConfig_Real.asset
|   |   +-- ReferenceImageLibrary.asset
|   +-- Audio/
|   |   +-- Music/               <- 12 pistas MP3 (C418 Minecraft OST)
|   |   +-- SFX/
|   |       +-- UI/              <- 8 clips (MenuClick, LevelUp, Orb, ToastComplete, ButtonPress, TakePhoto, Guardian Hover, Init Game)
|   |       +-- Voxels/          <- 26 clips (dig, hit, mining, brush, hoe, break)
|   +-- Materials/
|   |   +-- AR/                  <- M_ARGround.mat, M_GridLines.mat
|   |   +-- Blocks/              <- M_BlockDirt/Sand/Stone/Torch/Wood.mat, M_Sand.mat
|   +-- Models/
|   |   +-- Blocks/              <- 5 modelos .glb (Glass, Grass, Stone, Torch, Wood)
|   |   +-- Things/              <- 2 modelos .glb (Creeper head, Frog fountain)
|   +-- Prefabs/
|   |   +-- AR/                  <- AR_Default_Plane, AR_RayInteractor
|   |   +-- Blocks/              <- 11 prefabs activos + carpeta _Deprecated/ (5 obsoletos)
|   |   +-- UI/                  <- (reservada, .gitkeep)
|   |   +-- VFX/                 <- VFX_BlockPlace, VFX_BlockBreak
|   +-- Scenes/
|   |   +-- Title_Screen.unity   <- Pantalla de inicio
|   |   +-- Main_AR.unity        <- Escena principal de juego
|   +-- Scripts/
|   |   +-- AR/                  <- Gestion AR: ancla, planos, profundidad, modos, bonsai (5 scripts)
|   |   +-- Core/                <- Grid, armonia, audio, iluminacion, undo/redo, reset, screenshot, transiciones, guardado, SOs (19 scripts)
|   |   +-- Infrastructure/      <- ServiceLocator, EventBus, GameEvents, enums, interfaces (17 scripts)
|   |   +-- Interaction/         <- Input tactil, herramientas, colocacion/destruccion (7 scripts)
|   |   +-- Title/               <- Pantalla de inicio: face/hand tracking, seleccion modo (6 scripts)
|   |   +-- UI/                  <- HUD, menu, orientacion, popups de guardado/carga (13 scripts)
|   |   +-- Voxel/               <- Bloques, spawn/destroy, piedras procedurales, VFX (8 scripts)
|   +-- Shaders/
|   |   +-- ARPlane.shader       <- Arena zen con grid animado
|   |   +-- VoxelLit.shader      <- Toon-lit para bloques voxel
|   +-- Textures/
|       +-- AR/                  <- T_Sand.png, T_ZenFloor.png
|       +-- Icons/               <- ARmoniaIcon.png, ARmoniaIconBackground.jpg, ARmoniaTitle.png
|       +-- Images/              <- Variantes del icono (ARmoniaIcon.jpg, ARmoniaIcon_bw.jpg)
|       +-- UI/                  <- 21 sprites PNG (Icon_*, UI_*)
+-- Resources/                   <- (vacia, uso interno Unity)
+-- Settings/                    <- URP Pipeline Assets y Volume Profiles
|   +-- Mobile_RPAsset.asset     <- URP Render Pipeline Asset (Android)
|   +-- Mobile_Renderer.asset    <- URP Renderer (Android)
|   +-- PC_RPAsset.asset         <- URP Render Pipeline Asset (Editor)
|   +-- PC_Renderer.asset        <- URP Renderer (Editor)
|   +-- DefaultVolumeProfile.asset
|   +-- SampleSceneProfile.asset
|   +-- UniversalRenderPipelineGlobalSettings.asset
+-- InputSystem_Actions.inputactions
+-- TextMesh Pro/                <- Paquete TMP (importado)
+-- XR/                          <- XR Interaction Toolkit defaults
+-- XRI/                         <- XR Interaction Toolkit settings
```

---

## 3. Las dos escenas

### 3.1 Title_Screen (build index 0)

La pantalla de inicio usa la **camara frontal** para dos cosas a la vez:

- **AR Face Tracking** (`ARFaceManager`): superpone una cabeza de Creeper de Minecraft sobre la cara del jugador via `CreeperFaceFilter`.
- **Hand Tracking** (MediaPipe HandLandmarker, GPU delegate con fallback CPU): muestra un cursor que sigue la punta del dedo indice. El jugador selecciona el modo de juego con un **pinch** (pulgar + indice, instantaneo) o manteniendo el cursor encima de un boton durante 3 segundos (dwell time, fallback).

El logo "ARMONIA" (`HUD_Logo`, Image) flota con un bobbing vertical (`TitleLogoAnimator`). La musica de fondo suena con shuffle y crossfade (`MusicService` en un `MusicPlayer` root separado).

```text
Title_Screen (camara frontal)
  |
  +-- AR Face Tracking (ARFaceManager, maxFaces 1)
  |     +-- CreeperFaceFilter -> instancia prefab Creeper como hijo del ARFace
  |
  +-- Hand Tracking (MediaPipe HandLandmarker, GPU delegate)
  |     +-- HandTrackingService -> landmark #8 (indice) + pinch (#4 <-> #8)
  |           +-- HandCursorUI (cursor con hover scale 1.35x)
  |           +-- DwellSelector (pinch instantaneo O dwell 3s)
  |                 +-- Highlight de botones (escala 1.12x, tint blanco)
  |
  +-- TitleLogoAnimator -> bobbing senoidal (+/-12px, 0.2Hz en escena)
  |
  +-- TitleSceneManager
        +-- Btn_Bonsai -> SelectMode(0) -> WorldModeContext.Selected = Bonsai
        +-- Btn_Normal -> SelectMode(1) -> WorldModeContext.Selected = Normal
        +-- Btn_Real   -> SelectMode(2) -> WorldModeContext.Selected = Real
              +-- SceneTransitionService.TransitionTo("Main_AR")
                    fade-to-black 0.4s -> async load -> fade-in 0.4s
```

**Hand Tracking -- detalle tecnico:** `HandTrackingService` captura frames de `ARCameraManager`, los envia a MediaPipe (IMAGE mode), y extrae los landmarks del indice (#8) y pulgar (#4). Coordenadas de camara frontal: rotacion 270 grados, flip horizontal, `screenX = (1 - normY) * w`, `screenY = (1 - normX) * h`. Smoothing de 0.7. Deteccion de pinch con histeresis (enter 0.055, exit 0.08) y debounce de 2 frames.

### 3.2 Main_AR (build index 1)

La escena principal. Camara trasera con AR Foundation. El jugador construye su jardin zen tocando la pantalla.

Al cargar la escena, `WorldModeBootstrapper` lee `WorldModeContext.Selected` (escrito por la pantalla de inicio) y configura todo: escala del mundo, tipo de ancla (plano AR o imagen trackeada), y activa el manager AR correspondiente. Si se abre `Main_AR` directamente (sin pasar por Title), `_devOverrideMode` actua como fallback.

La activacion de AR managers se difiere a una corrutina en `Start()` que espera a que `ARSession.state >= SessionInitializing` (timeout 5s). Esto evita una race condition al transicionar desde `Title_Screen` (camara frontal) donde ARCore aplicaria la configuracion nativa antes de que la image library estuviera lista.

**Retorno:** `Btn_Exit` en el menu de opciones llama a `GameOptionsMenu.ExitGame()` que usa `SceneTransitionService.TransitionTo("Title_Screen")` con fade-to-black.

### 3.3 Flujo entre escenas

```text
Title_Screen                               Main_AR
+-----------------------+  TransitionTo  +------------------------+
| TitleSceneManager     | ------------> | WorldModeBootstrapper    |
| SelectMode(int)       |  (fade-black) | Awake: lee contexto,    |
| -> WorldModeContext   |               |   aplica escala          |
| -> SceneTransition    |               | Start: corrutina         |
|   Service             |  TransitionTo |   espera ARSession ->    |
+-----------------------+ <------------ |   configura managers     |
                          GameOptions   +------------------------+
                          Menu.ExitGame()
                           (fade-black)
```

`WorldModeContext` es un `static class` que transporta la seleccion de modo entre escenas sin `DontDestroyOnLoad`. Title screen escribe, bootstrapper lee. `SceneTransitionService` es un singleton con `DontDestroyOnLoad` que crea su propio Canvas overlay (sort order 999) con fade-to-black. Usa `Time.unscaledDeltaTime` y bloquea raycasts durante la transicion.

> **Nota:** La jerarquia completa de GameObjects de cada escena esta documentada en `CONVENTIONS.md`, seccion 4.

---

## 4. Arquitectura

### 4.1 WorldContainer

Todo el mundo de bloques cuelga de un unico `Transform` llamado **WorldContainer**.

```text
[ARAnchor]                      <- Creado por ARWorldManager en el primer tap
  +-- WorldContainer            <- localScale controla la escala del mundo entero
        +-- Voxel_Sand(Clone)   <- Bloques hijos, posicion local = posicion en grid
        +-- Voxel_Stone(Clone)
        +-- Pebble_Stone(Clone) <- Decoraciones (posicion libre, sin grid)
        +-- Dynamic_GridVisual  <- Rejilla visual (MeshFilter con lineas procedurales)
```

**Por que esta estructura:**

- **Escala unica:** cambiando `WorldContainer.localScale` con un solo float (`WorldModeSO.WorldContainerScale`) pasas de Bonsai (0.02 = 2cm/bloque) a Normal (0.10) a Real (1.0 = 1m/bloque) sin tocar nada mas.
- **Estabilidad AR:** al parentar WorldContainer a un `ARAnchor`, ARCore compensa el drift de tracking automaticamente.
- **Snap correcto:** `GridManager.GetSnappedPosition()` trabaja en espacio local del contenedor (`Floor + half-cell offset`), asi el snap funciona igual en cualquier escala.
- **Reset limpio:** `WorldResetService` itera los hijos de WorldContainer en reversa, destruyendo solo los que tienen `VoxelBlock` o `ProceduralPebble` sin tocar el grid visual.

### 4.2 Modos de escala del mundo

Configurados por `WorldModeSO` (ScriptableObject, uno por modo) y aplicados por `WorldModeBootstrapper` al inicio:

| Modo | Escala | Tamano bloque | Ancla | MaxBlocks | Uso |
|------|--------|---------------|-------|-----------|-----|
| **Bonsai** | 0.02 | 2 cm | `ARTrackedImageManager` (imagen impresa 20cm) | Configurable | Jardin miniatura sobre carta/poster |
| **Normal** | 0.10 | 10 cm | `ARPlaneManager` (suelo detectado) | 0 (ilimitado) | Escala mesa/suelo -- modo por defecto |
| **Real** | 1.00 | 1 m | `ARPlaneManager` (suelo detectado) | 0 (ilimitado) | Escala Minecraft real |

Para Bonsai se necesita una `XRReferenceImageLibrary` configurada en el `WorldModeSO`. El bootstrapper activa `ARPlaneManager` o `ARTrackedImageManager` segun el modo, desactivando el otro. El modo Bonsai incluye logs detallados de image library assignment y eventos ADDED/UPDATED/REMOVED con nombre, tracking state y posicion.

### 4.3 Patrones de comunicacion

Los sistemas se comunican sin acoplarse entre si. Aqui va un resumen rapido (la tabla completa con ejemplos esta en `CONVENTIONS.md`, seccion 5):

| Patron | Para que | Ejemplo rapido |
|--------|----------|----------------|
| **Service Locator** | Resolver dependencias por interfaz | `ServiceLocator.TryGet<IGameAudioService>(out _audio)` |
| **EventBus** (pub/sub tipado) | Comunicacion cross-sistema | `EventBus.Publish(new BlockPlacedEvent(...))` |
| **C# Events** (`event Action<T>`) | Notificacion intra-capa, UI reactiva | `OnHarmonyChanged` -> `HarmonyHUD.SetHarmony` |
| **Command Pattern** | Undo/Redo | `PlaceBlockAction` / `DestroyBlockAction` |
| **Inspector** `[SerializeField]` | Inyeccion para MonoBehaviours de escena | Toda seccion `#region Inspector` |
| **ServiceLocator.TryGet en Awake** | Prefabs instanciados dinamicamente | `BlockSpawn`, `BlockDestroy`, `SandGravity` |
| **ScriptableObject data** | Config compartida sin depender de escena | `BlockDatabaseSO`, `HarmonyConfig`, `WorldModeSO` |
| **Static context** | Dato cross-escena sin singletons | `WorldModeContext.Selected` |
| **Facade** | Simplificar subsistema | `GridManager` envuelve `GridVisualizer` |
| **Prefab-owns-feedback** | Audio/VFX viven en el prefab, no en el caller | `BlockSpawn` y `BlockDestroy` manejan sus propios efectos |
| **OnClick directo** | Botones modo toggle | `Btn_Brush.OnClick -> BrushTool.ToggleBrush()` |

---

## 5. Gameplay -- herramientas e inventario

### 5.1 Herramientas

| Slot | `ToolType` | Valor | Que hace |
|------|------------|-------|----------|
| Arena | `Build_Sand` | 0 | Bloque 1x1x1 de arena. Minimo 10 para gate de armonia. |
| Cristal | `Build_Glass` | 1 | Bloque translucido. |
| Piedra | `Build_Stone` | 2 | Bloque solido de piedra. |
| Madera | `Build_Wood` | 3 | Bloque de madera. |
| Antorcha | `Build_Torch` | 4 | Bloque antorcha con URP Light component. |
| Hierba | `Build_Grass` | 5 | Bloque verde. Minimo 10 para gate de armonia. |
| Vacio | `Tool_None` | 6 | Mano vacia. El toque no hace nada. |
| Destruir | `Tool_Destroy` | 7 | Raycast fisico -> `BlockDestroy.BreakFromTool()`. Funciona con bloques y piedritas. |
| Pincel | `Tool_Brush` | 8 | **Mode overlay** -- toggle ON/OFF. `Btn_Brush.OnClick` llama directamente a `BrushTool.ToggleBrush()`, no pasa por `UIManager`. Arrastra para placement/destroy continuo cada 0.08s. Compatible con build, destroy y plow. |
| Arado | `Tool_Plow` | 9 | Decorador de piedritas procedurales. Coloca piedras icosaedro con scatter, rotacion y escala aleatoria. |

**Conversion ToolType <-> BlockType:** `ToolManager` castea `(BlockType)(int)CurrentTool` para obtener el prefab de `BlockDatabaseSO`. Los valores 0-5 de `ToolType` coinciden 1:1 con `BlockType`.

> **ADVERTENCIA:** Los valores int de `ToolType` 0-7 y 9 estan baked en los `OnClick` events de los botones en la escena. No se deben cambiar. `Tool_Brush (8)` es excepcion -- su boton llama directamente a `BrushTool.ToggleBrush()`, no usa el valor int.

### 5.2 Sistema de Armonia

La barra de Armonia evalua tu jardin con tres pilares:

| Pilar | Peso | Como se calcula |
|-------|------|-----------------|
| **Variedad** | 0.45 | `distinctTypes / fullVarietyTypeCount (6)` |
| **Decoracion** | 0.35 | `totalPebbles / targetPebbleCount (25)` |
| **Cantidad** | 0.20 | `totalBlocks / targetBlockCount (50)` |

Ademas hay un **gate de minimos** que penaliza proporcionalmente si: Arena < 10 bloques O Hierba < 10 bloques (`gateStrength = 0.85`).

El score final va de 0.0 a 1.0 con un threshold anti-jitter de 0.005. Solo se recalcula cuando algo cambia (nunca en Update). Toda la configuracion esta en `HarmonyConfig.asset`.

**Frases por fase:**

| Rango | Frase |
|-------|-------|
| [0.00 - 0.25) | "Empieza tu jardin" |
| [0.25 - 0.50) | "Anade mas variedad" |
| [0.50 - 0.75) | "Jardin equilibrado" |
| [0.75 - 1.00) | "Gran armonia" |
| 1.00 | "Armonia perfecta!" |

### 5.3 Vibracion haptica

`HapticService` usa el plugin [Vibration](https://github.com/BenoitFreslon/Vibration) de Benoit Freslon. Empieza **desactivado** -- el jugador lo activa desde `Btn_Vibration` en el menu.

| Preset | Metodo | Duracion | Uso |
|--------|--------|----------|-----|
| Light (Pop) | `VibrateLight()` | ~50 ms | UI taps, colocar bloque, foto |
| Medium (Peek) | `VibrateMedium()` | ~100 ms | Destruir bloque |
| Heavy (Nope) | `VibrateHeavy()` | Patron triple | Fases altas de armonia, armonia perfecta |

`UIAudioService` integra vibracion en todos los metodos `Play*()` (excepto `PlayPhoto`, que la maneja `ScreenshotService`).

---

## 6. Flujos detallados

Diagramas paso a paso de cada sistema. Sirven para entender el codigo o depurar problemas.

### 6.1 Del dedo al bloque (flujo principal)

```text
Touch (Enhanced Touch API, TouchPhase.Began)
  |
  v
TouchInputRouter.Update()      <- Ignora toques sobre UI (IsPointerOverGameObject)
  |
  +-- BrushTool.IsBrushActive?
  |   +-- Si -> BrushTool.Update() se come el touch
  |            +-- TouchPhase.Moved/Stationary cada _strokeCooldown (0.08s)
  |            +-- Si ToolManager.IsBuildTool -> ARBlockPlacer.TryPlaceBlock()
  |            +-- Si Tool_Destroy -> BlockDestroyer.TryDestroyBlock()
  |            +-- Si Tool_Plow -> PlowTool.PlacePebbleAtScreen()
  |
  +-- ToolManager.IsBuildTool? -> ARBlockPlacer.TryPlaceBlock()
  |     |
  |     +-- Physics.Raycast(_voxelLayerMask)  -> Hit bloque existente -> stacking
  |     |     +-- hitPoint + hitNormal * gridSize -> nueva posicion local
  |     |
  |     +-- ARRaycastManager.Raycast(TrackableType.PlaneWithinPolygon)
  |           +-- Hit plano AR -> primer bloque
  |                 |
  |                 +-- Si !IsWorldAnchored:
  |                      ARWorldManager.AnchorWorld(hitPose, camera)
  |                        +-- Posiciona WorldContainer en hitPoint
  |                        +-- Orienta forward hacia el jugador (solo XZ)
  |                        +-- Crea ARAnchor (prefab o manual)
  |                        +-- Parenta WorldContainer al anchor
  |                        +-- GridManager.ActivateGrid(camera)
  |                              +-- GridVisualizer.Activate()
  |
  |  ProcessAndPlace(rawLocalPosition)
  |     +-- GridManager.GetSnappedPosition()   <- Floor(pos/gridSize)*gridSize + half
  |     +-- Validaciones:
  |     |   +-- Distancia maxima (_maxBuildDistance)
  |     |   +-- Distancia minima (_minPlaceDistance)
  |     |   +-- Overlap check (Physics.CheckBox con _overlapTolerance)
  |     |   +-- _pendingCells.Contains() -- celda ya reservada durante animacion
  |     +-- _pendingCells.Add(snappedPos)      <- Reserva celda
  |     +-- Instantiate(prefab, WorldContainer)
  |     +-- BlockSpawn.Play(camera, onComplete)
  |     |     +-- Fase 1 (80%): vuelo local desde camara, scale 0 -> peakScale (1.15)
  |     |     +-- Fase 2 (20%): settle peakScale -> 1.0
  |     |     +-- Re-enable collider + BlockDestroy.SetReady()
  |     |     +-- PlayPlaceFeedback(): VFX + audio (lee de VoxelBlock.PlaceSounds)
  |     |     +-- onComplete -> _pendingCells.Remove()
  |     +-- UndoRedoService.Record(new PlaceBlockAction(...))
  |     +-- EventBus.Publish(new BlockPlacedEvent(blockType))
  |
  +-- ToolManager.CurrentTool == Tool_Destroy? -> BlockDestroyer.TryDestroyBlock()
        +-- Physics.Raycast(_voxelLayerMask | _pebbleLayerMask)
        +-- UndoRedoService.Record(new DestroyBlockAction(...))
        +-- BlockDestroy.BreakFromTool(hitNormal)
        |     +-- Audio: VoxelBlock.BreakSounds (bloques) o _breakSounds (pebbles)
        |     +-- VFX: _breakVfxPrefab [SerializeField] en el prefab
        |     +-- Unparent del WorldContainer
        |     +-- AddComponent<Rigidbody>() + AddForce(kickDir * knockForce)
        |     +-- AddTorque(random * knockForce * 3)
        |     +-- WaitForSeconds(_destroyDelay = 0.12s)
        |     +-- Shrink to zero (_shrinkDuration = 0.18s)
        |     +-- Destroy(gameObject)
        +-- EventBus.Publish(new BlockDestroyedEvent(blockType))
```

### 6.2 Activacion del Brush

```text
Btn_Brush.OnClick -> BrushTool.ToggleBrush()     <- Llamada DIRECTA, no por ToolManager
  |
  +-- IsBrushActive = !IsBrushActive
  +-- event OnBrushToggled(bool)
  |     +-- BrushHUD.RefreshVisual() -> dim/restore boton
  +-- UIAudioService.PlayToggle()

El Brush es un MODE OVERLAY, no una herramienta normal.
No pasa por UIManager ni ToolManager.
Btn_Brush.OnClick apunta directamente a BrushTool.ToggleBrush().
```

### 6.3 Proximidad knock (auto-destruccion por camara)

```text
BlockDestroy.Update()    <- Solo si _ready == true (post-spawn)
  |
  +-- _toolManager.CurrentTool != Tool_Destroy? -> return (sin efecto)
  |
  +-- sqrDistance(camera, block) <= _knockRadius^2 (0.18m)?
       +-- StartCoroutine(KnockRoutine())  <- Mismo flujo que BreakFromTool
          pero direccion = away from camera + Vector3.up * 0.6

La proximidad knock SOLO se activa cuando el jugador tiene
la herramienta de pico (Tool_Destroy) seleccionada.
```

### 6.4 Decorador de piedritas (PlowTool)

```text
PlowTool.Update()   (solo cuando ToolManager.CurrentTool == Tool_Plow)
  |
  +-- Touch.Began -> TryPlacePebble(screenPos)
  |     +-- Physics.Raycast(_voxelLayerMask) -> sobre tapa/cara de un bloque
  |     +-- ARRaycastManager.Raycast -> sobre suelo AR
  |     +-- Validaciones: distancia min/max
  |
  +-- PlaceAt(worldPoint, surfaceNormal, onARPlane)
        +-- Random prefab de _pebblePrefabs[]
        +-- Scatter: Random.insideUnitCircle * _scatterRadius
        |     proyectado en tangente/bitangente de surfaceNormal
        +-- Rotacion: Quaternion.FromToRotation(up, normal) * AngleAxis(random 0-360)
        +-- Escala: Random.Range(_scaleMin, _scaleMax) * prefab.localScale
        +-- PebbleSupport.Configure(onARPlane, _voxelLayerMask, surfaceNormal)
        |     +-- Si onARPlane -> nunca auto-break
        |     +-- Si onBlock -> InvokeRepeating(Poll, 0.35s)
        |           +-- Raycast hacia -surfaceNormal, _checkDistance 0.20m
        |           +-- Si no hay soporte -> BlockDestroy.BreakFromTool(-supportDir)
        +-- BlockSpawn.Play(camera, onComplete)
        |     +-- PlayPlaceFeedback(): audio (lee de BlockSpawn._placeSounds)
        |     +-- onComplete -> BlockDestroy.SetReady() + PebbleSupport.Arm()
        +-- EventBus.Publish(new PebblePlacedEvent())

BrushTool + Tool_Plow activos:
  -> PlowTool.PlacePebbleAtScreen() cada _brushCooldown (0.06s)
```

### 6.5 Armonia -> UI -> Armonia Perfecta

```text
HarmonyService.Recalculate()     <- Solo cuando algo cambia, nunca en Update()
  |
  +-- ScoreVariety()     -> distinctTypes / fullVarietyTypeCount (6)
  |                        Peso: 0.45
  +-- ScoreDecoration()  -> totalPebbles / targetPebbleCount (25)
  |                        Peso: 0.35
  +-- ScoreQuantity()    -> totalBlocks / targetBlockCount (50)
  |                        Peso: 0.20
  +-- ScoreMinimumGate() -> penaliza proporcionalmente si:
  |                        Sand < minSandBlocks (10) OR Grass < minGrassBlocks (10)
  |                        gateStrength = 0.85
  |
  v
  float score [0..1]  (threshold anti-jitter de 0.005)
  |
  +-- event OnHarmonyChanged(score)
  |     +-- HarmonyHUD.SetHarmony(score)
  |           +-- Anima anchorMax.x de _fillRect (coroutine)
  |           +-- Color: gradiente _colourLow(rojo) -> _colourMid(amarillo) -> _colourHigh(verde)
  |           +-- Frase por fase (ver tabla en seccion 5.2)
  |           +-- Pop animation (scale 1.0 -> 1.20 -> 1.0 en 0.45s)
  |           +-- Shake animation (offset +/-7px en 0.28s)
  |           +-- UIAudioService.PlayHarmonyPhase(1..4)
  |
  +-- event OnPerfectHarmony (solo una vez por sesion)
        +-- PerfectHarmonyPanel
              +-- CanvasGroup fade in (0.6s, SmoothStep)
              +-- Btn_Continue fade in simultaneo
              +-- HarmonyParticles.Play()
              |     +-- Burst: 120 particulas x 3 repeticiones (1.1s entre cada una)
              |     +-- Posicion: 1.2m frente a la camara AR
              |     +-- Colores: gradiente dorado -> pastel -> lavanda -> blanco
              |     +-- Ambient: 5 particulas/s continuas en radio 0.7m
              +-- UIAudioService.PlayConfirm()
              +-- Btn_Continue -> fade out (0.35s)
```

### 6.6 Undo/Redo

```text
UndoRedoService (Stack<IUndoableAction>, cap = 20 configurable)
  |
  +-- Record(PlaceBlockAction)     <- ARBlockPlacer tras cada colocacion
  +-- Record(DestroyBlockAction)   <- BlockDestroyer antes de cada destruccion
  |     +-- Cada Record() limpia la pila de Redo
  |     +-- Si undoStack > _maxHistory -> TrimBottom() (O(n), solo al cap)
  |
  +-- Undo()
  |   +-- PlaceBlockAction.Undo()   -> Destroy(instance)
  |   +-- DestroyBlockAction.Undo() -> Instantiate + arm
  |
  +-- Redo()
  |   +-- PlaceBlockAction.Redo()   -> Instantiate + arm
  |   +-- DestroyBlockAction.Redo() -> Destroy(restoredInstance)
  |
  +-- event OnStackChanged(canUndo, canRedo)
  |     +-- UndoRedoHUD.RefreshState()
  |           +-- Button.interactable = canUndo/canRedo
  |           +-- Icon alpha: _alphaEnabled(1.0) / _alphaDisabled(0.35)
  |
  +-- Tras cada undo/redo:
       EventBus.Publish(new UndoPerformedEvent/RedoPerformedEvent())
            -> HarmonyService.HandleUndoRedo() -> RebuildCounters() + Recalculate()
```

`PlaceBlockAction.ArmForImmediate()` es un metodo estatico compartido entre ambos commands: deshabilita `BlockSpawn`, habilita `Collider` + `BlockDestroy.SetReady()`. Lo usa `Redo` (place) y `Undo` (destroy).

### 6.7 World Reset

```text
GameOptionsMenu.RequestClearAll() -> Popup_ConfirmClearAll visible
  |
  +-- Btn_Confirm -> GameOptionsMenu.ConfirmClearAll()
  |     +-- WorldResetService.ResetWorld()
  |           +-- DestroyAllBlocks()
  |           |     +-- Itera WorldContainer.children en reversa
  |           |        Solo destruye si tiene VoxelBlock o ProceduralPebble
  |           +-- ARWorldManager.ResetAnchor()
  |           |     +-- Destruye ARAnchor, un-parent WorldContainer
  |           +-- GridManager.DeactivateGrid()
  |           |     +-- GridVisualizer.Deactivate() -> Destroy mesh
  |           +-- UndoRedoService.Clear()
  |           +-- EventBus.Publish(new WorldResetEvent())
  |           |     -> HarmonyService.HandleWorldReset()
  |           |     +-- RebuildCounters() (todo a 0) + Recalculate()
  |           |     +-- OnWorldReset?.Invoke()
  |           |     +-- _perfectFired = false (permite re-trigger)
  |           +-- event OnWorldReset
  |                 +-- PerfectHarmonyPanel.HandleWorldReset()
  |                 |     +-- StopAmbient() + hide panel
  |                 +-- HarmonyHUD desfreeze
  |
  +-- Btn_Cancel -> GameOptionsMenu.CancelClearAll()
        +-- Oculta popup, UIAudioService.PlayCancel()
```

### 6.8 Menu de opciones

```text
Btn_Settings -> GameOptionsMenu.ToggleMenu()
  +-- Pnl_OptionsDropdown.SetActive(toggle)
  +-- HUD_MenuBlocker.SetActive(toggle)    <- cierra al tocar fuera
  +-- UIAudioService.PlayMenuOpen()

Botones dentro del dropdown:
  +-- Btn_Lighting -> ToggleLighting()
  |     +-- LightingService.ToggleLighting()
  |           +-- CameraFlashLight (SpotLight) ON/OFF
  |           +-- Directional Light OFF/ON (si _disableGlobalOnFocus)
  |           +-- event OnLightingToggled -> DropdownButtonState actualiza color
  +-- Btn_Depth -> ToggleDepth()
  |     +-- ARDepthService.ToggleDepth()
  |           +-- AROcclusionManager.enabled = true/false
  |           +-- event OnDepthToggled -> DropdownButtonState actualiza color
  +-- Btn_Grid -> ToggleGrid()
  |     +-- ARPlaneGridAligner.SetGrid(bool) -> MaterialPropertyBlock _GridEnabled
  +-- Btn_Plane -> TogglePlaneVisual()
  |     +-- ARPlaneGridAligner.SetVisual(bool) -> MeshRenderer.enabled en planos
  +-- Btn_Vibration -> ToggleVibration()
  |     +-- HapticService.ToggleHaptics()
  |           +-- IsEnabled = !IsEnabled (default OFF)
  |           +-- event OnHapticsToggled -> DropdownButtonState actualiza color
  +-- Sld_MusicVolume -> OnMusicVolumeChanged(0-100)
  |     +-- MusicService.SetVolume(0-1)
  +-- Btn_Photo -> TakePhoto()
  |     +-- ScreenshotService.Capture() (ver flujo 6.10)
  +-- Btn_ClearAll -> RequestClearAll() (ver flujo 6.7)
  +-- Btn_Exit -> ExitGame()
        +-- SceneTransitionService.TransitionTo("Title_Screen")
```

### 6.9 Orientacion

```text
OrientationManager.Update()
  |
  +-- Screen.width > Screen.height? -- cambio?
       +-- Landscape:
       |   +-- Oculta HUD_Hotbar, HUD_ToolPanel, HUD_Selector
       |   +-- Guarda _previousTool
       |   +-- ToolManager.SelectToolByIndex(Tool_None)
       |
       +-- Portrait:
           +-- Muestra HUD_Hotbar, HUD_ToolPanel, HUD_Selector
           +-- WaitForEndOfFrame -> Restaura _previousTool
```

### 6.10 Screenshot

```text
ScreenshotService.Capture()    <- Con debounce (_isCapturing)
  +-- Canvas.enabled = false
  +-- WaitForEndOfFrame
  +-- Texture2D.ReadPixels -> Apply
  +-- Canvas.enabled = true
  +-- UIAudioService.PlayPhoto() (shutter sound)
  +-- Flash overlay (GameObject ON -> alpha 1 -> 0 -> OFF)
  +-- HapticService.VibrateLight() (pop tactil)
  +-- NativeGallery.SaveImageToGallery() (Android/iOS)
  |   +-- Editor fallback: File.WriteAllBytes(persistentDataPath)
  +-- ScreenshotToastPanel.Show(texture)
  |   +-- GameObject ON -> RawImage = thumbnail
  |   +-- CanvasGroup fade in (0.3s SmoothStep)
  |   +-- Btn_Continue -> fade out -> ReleaseTexture -> GameObject OFF
  +-- event OnScreenshotCaptured -> CloseMenuDelayed (1 frame) -> ToggleMenu()
```

### 6.11 Gravedad de arena (SandGravity)

```text
SandGravity (solo en Voxel_Sand)
  |
  +-- Espera BlockDestroy.IsReady + 0.15s
  +-- Suscripcion a EventBus<BlockDestroyedEvent> (trigger primario)
  +-- InvokeRepeating cada 1s (safety poll)
  |
  +-- Comprobacion: hay soporte debajo? (bloque o plano AR)
       +-- Si -> nada (sigue comprobando)
       +-- No -> reserva celda destino en HashSet estatico
             +-- Cae animado (ease-in, velocidad configurable)
             +-- Desactiva collider + BlockDestroy durante caida
             +-- Tras aterrizar, libera reserva + Physics.SyncTransforms()
             +-- Reinicia comprobacion
```

---

## 7. Shaders personalizados

### 7.1 `ARmonia/AR/ARPlane` (ARPlane.shader)

Shader HLSL para URP que renderiza el suelo AR como arena zen estilizada.

| Propiedad | Detalle |
|-----------|---------|
| **5 tonos de arena** | `_Sand0` a `_Sand4`, distribuidos por hash |
| **Grid superpuesto** | Lineas menores cada `_CellSize` (0.1m), mayores cada `_MajorEvery` celdas (10) |
| **Toggle runtime** | `_GridEnabled` via `MaterialPropertyBlock` |
| **Animacion** | Pulse sinusoidal en lineas menores, shimmer en mayores |
| **`_GridMatrix`** | Matriz `worldToLocal` del WorldContainer, inyectada por `ARPlaneGridAligner` para alinear grid shader con grid de voxels |
| **Sombras** | Recibe sombras del directional light (atenuacion 0.55) |
| **Render** | `Transparent`, `ZWrite Off`, `Cull Off`, `Blend SrcAlpha OneMinusSrcAlpha` |

### 7.2 `ARmonia/Blocks/VoxelLit` (VoxelLit.shader)

Shader HLSL para URP que ilumina los bloques voxel con estetica Minecraft.

| Propiedad | Detalle |
|-----------|---------|
| **Albedo point-sampled** | Texturas pixel-art sin filtrado bilineal |
| **Toon lighting 3 bandas** | `_BandLight` (0.6), `_BandMid` (0.25), `_MidScale` (0.55), `_ShadowScale` (0.25) |
| **Vertex colour AO** | Canal R del vertex colour como oclusion ambiental (`_AmbientOcclusion` = 0.6) |
| **Emision** | `_EmissionColor` + `_EmissionIntensity` para antorchas |
| **Shadow strength** | `_ShadowStrength` (0.55) mezclada con URP shadow attenuation |
| **Ambient** | Spherical Harmonics (URP SH) |
| **Fog** | URP fog support |
| **Passes extra** | `ShadowCaster` y `DepthOnly` via UsePass de URP/Lit |

---

## 8. Inventario de assets

### 8.1 ScriptableObjects (6 assets)

| Asset | Clase | Ubicacion | Descripcion |
|-------|-------|-----------|-------------|
| `BlockDatabase.asset` | `BlockDatabaseSO` | `Assets/` | Mapea cada `BlockType` a su prefab. Lookup O(1) con lazy dictionary. |
| `HarmonyConfig.asset` | `HarmonyConfig` | `Assets/` | Pesos de los 3 pilares (variedad 0.45, decoracion 0.35, cantidad 0.20), umbrales y gate de minimos. |
| `WorldModeConfig_Bonsai.asset` | `WorldModeSO` | `Assets/` | Escala 0.02, ancla por tracked image, `ImagePhysicalWidth` 0.20m. |
| `WorldModeConfig_Normal.asset` | `WorldModeSO` | `Assets/` | Escala 0.10, ancla por AR plane. Modo por defecto. |
| `WorldModeConfig_Real.asset` | `WorldModeSO` | `Assets/` | Escala 1.00, ancla por AR plane. Escala Minecraft real. |
| `ReferenceImageLibrary.asset` | `XRReferenceImageLibrary` | `Assets/` | 2 imagenes: `one` (0.13x0.13m), `qr_prueba` (0.10x0.10m). SpecifySize ON. Usada por modo Bonsai. |

### 8.2 Prefabs (21 archivos)

**Bloques voxel activos (6):**

| Prefab | Componentes | Shader |
|--------|-------------|--------|
| `Voxel_Sand` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `SandGravity` + `BoxCollider` + `MeshRenderer` | `ARmonia/Blocks/VoxelLit` |
| `Voxel_Glass` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` | `ARmonia/Blocks/VoxelLit` |
| `Voxel_Stone` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` | `ARmonia/Blocks/VoxelLit` |
| `Voxel_Wood` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` | `ARmonia/Blocks/VoxelLit` |
| `Voxel_Grass` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` | `ARmonia/Blocks/VoxelLit` |
| `Voxel_Torch` | `VoxelBlock` + `BlockDestroy` + `BlockSpawn` + `BoxCollider` + `MeshRenderer` + `Light (URP)` | `ARmonia/Blocks/VoxelLit` |

**Piedritas activas (3):**

| Prefab | Componentes |
|--------|-------------|
| `Pebble_Stone` | `ProceduralPebble` + `BlockDestroy` + `BlockSpawn` + `PebbleSupport` + `MeshCollider (convex)` |
| `Pebble_Stone1` | Variante de `Pebble_Stone` |
| `Pebble_Stone2` | Variante de `Pebble_Stone` |

**Objetos decorativos (2):**

| Prefab | Uso |
|--------|-----|
| `Object_Creeper` | Cabeza de Creeper para el face filter en Title_Screen (`CreeperFaceFilter`). |
| `Object_Frog` | Rana decorativa (Minecraft mob). |

**Bloques obsoletos** (`_Deprecated/`, 5): `Voxel_Dirt`, `Voxel_Sand`, `Voxel_Stone`, `Voxel_Torch`, `Voxel_Wood` -- versiones anteriores conservadas como respaldo.

**VFX (2):**

| Prefab | Componente | Descripcion |
|--------|------------|-------------|
| `VFX_BlockPlace` | `VFXBlockPlace` + `ParticleSystem` | Burst de particulas + scale pop al colocar. Auto-destruccion a 0.8s. |
| `VFX_BlockBreak` | `VFXBlockDestroy` + `ParticleSystem` | Burst de cubitos con gravedad al destruir. Auto-destruccion a 1.0s. |

**AR (2):** `AR_Default_Plane.prefab`, `AR_RayInteractor.prefab`.

**UI (1):**

| Prefab | Componentes | Descripcion |
|--------|-------------|-------------|
| `UI_GardenListItem` | `Button` + `Image` + `ButtonPressAnimation` | Boton dinamico para lista de jardines guardados en `BonsaiSelectorPopup`. Hijo `Txt_GardenName` (TMP_Text). |

### 8.3 Materiales (8)

| Material | Shader | Uso |
|----------|--------|-----|
| `M_ARGround.mat` | `ARmonia/AR/ARPlane` | Suelo AR: arena zen con grid superpuesto, sombras, pulse y shimmer. |
| `M_GridLines.mat` | Vertex colour | Lineas del `GridVisualizer` (mesh procedural). |
| `M_BlockDirt.mat` | `ARmonia/Blocks/VoxelLit` | Bloque tierra (obsoleto, reemplazado por Sand). |
| `M_BlockSand.mat` | `ARmonia/Blocks/VoxelLit` | Bloque arena. |
| `M_BlockStone.mat` | `ARmonia/Blocks/VoxelLit` | Bloque piedra. |
| `M_BlockTorch.mat` | `ARmonia/Blocks/VoxelLit` | Bloque antorcha (con emision). |
| `M_BlockWood.mat` | `ARmonia/Blocks/VoxelLit` | Bloque madera. |
| `M_Sand.mat` | (variante) | Material auxiliar arena. |

Texturas auxiliares en `Textures/AR/`: `T_Sand.png` (patron arena), `T_ZenFloor.png` (textura generada).

### 8.4 Audio (46 archivos)

**Musica (12 tracks MP3):** `Door`, `Subwoofer Lullaby`, `Living Mice`, `Minecraft`, `Oxygene`, `Equinoxe`, `Mice on Venus`, `Dry Hands`, `Wet Hands`, `Sweden`, `Alpha`, `Moog City 2`.

**SFX UI (8 clips):** `SFX_ButtonPress`, `SFX_LevelUp`, `SFX_MenuClick`, `SFX_Orb`, `SFX_ToastComplete`, `SFX_TakePhoto`, `SFX_Guardian Hover`, `SFX_Init Game`.

**SFX Voxels (26 clips):**

| Categoria | Clips |
|-----------|-------|
| Arena | `Sand_hit4`, `Sand_mining2`, `Sand_mining4`, `Sand_mining5` |
| Piedra | `Stone_dig1-4`, `Stone_hit2` |
| Madera | `Wood_dig1-3` |
| Cristal | `Glass_dig1-3` |
| Hierba | `Grass_dig2-4` |
| Pinceles | `Brushing_generic2-3` |
| Arado | `Hoe_till1-4` |
| General | `SFX_BlockPlace`, `Random_break` |

### 8.5 Modelos 3D (7 archivos .glb)

**Bloques (5):** `Model_Glass`, `Model_Grass`, `Model_Stone`, `Model_Torch`, `Model_Wood` en `Models/Blocks/`.

**Objetos (2):** `minecraft_creeper_head 1.glb`, `frog_fountain_minecraft_mob.glb` en `Models/Things/`.

Unity importa estos `.glb` como meshes con texturas embebidas.

### 8.6 Texturas e iconos (28 archivos)

| Carpeta | Contenido |
|---------|-----------|
| `Textures/AR/` (2) | `T_Sand.png`, `T_ZenFloor.png` |
| `Textures/Icons/` (3) | `ARmoniaIcon.png`, `ARmoniaIconBackground.jpg`, `ARmoniaTitle.png` |
| `Textures/Images/` (2) | `ARmoniaIcon.jpg`, `ARmoniaIcon_bw.jpg` |
| `Textures/UI/` (21) | `Icon_Barrier`, `Icon_Brush`, `Icon_Cobblestone`, `Icon_CommandBlock`, `Icon_DiamondPickaxe`, `Icon_Glass`, `Icon_GoldenHoe`, `Icon_GrassBlock`, `Icon_Light`, `Icon_OakPlanks`, `Icon_OakWood`, `Icon_Sand`, `Icon_Stick`, `Icon_Stone`, `Icon_Sword`, `Icon_Torch`, `UI_Background`, `UI_Book`, `UI_DemoBg`, `UI_Gui`, `UI_Icons` |

### 8.7 Fuentes (5 archivos)

| Archivo | Tipo | Uso |
|---------|------|-----|
| `Minecraft.ttf` | TrueType | Fuente base pixel Minecraft. |
| `Minecraft SDF.asset` | TMP SDF Font Asset | Version SDF para TextMeshPro. |
| `minecraft_fot_esp.ttf` | TrueType | Variante con caracteres espanoles (n, acentos). |
| `minecraft_fot_esp SDF.asset` | TMP SDF Font Asset | Version SDF con soporte espanol. |
| `Text StyleSheet.asset` | TMP Style Sheet | Hoja de estilos de TextMeshPro. |

---

## 9. Catalogo de scripts (75)

### AR (5 scripts) -- `_Project.Scripts.AR`

| Script | Responsabilidad |
|--------|-----------------|
| `ARDepthService` | Toggle runtime de `AROcclusionManager` (enabled/disabled). Checkbox `_depthOnStart` controla estado inicial (default ON). Evento `OnDepthToggled`. |
| `ARPlaneGridAligner` | Inyecta `WorldContainer.worldToLocalMatrix` en cada plano AR como `_GridMatrix` via `MaterialPropertyBlock`. Controla `_GridEnabled` y `MeshRenderer.enabled` de los planos. |
| `ARWorldManager` | Crea `ARAnchor` en el primer hit, orienta WorldContainer.forward hacia el jugador (solo XZ, con fallback si forward ~ up), parenta WorldContainer, activa `GridManager.ActivateGrid()`. `ResetAnchor()` destruye el anchor y libera WorldContainer. |
| `BonsaiSessionController` | Controlador de flujo para modo Bonsai. Escucha `WorldModeBootstrapper.OnBonsaiImageDetected` y abre `BonsaiSelectorPopup`. Se auto-desactiva si `WorldModeContext.Selected != Bonsai`. |
| `WorldModeBootstrapper` | Lee `WorldModeContext.Selected`; si es `None` (cold start / Editor) usa `_devOverrideMode`. Busca `WorldModeSO` en `_modeConfigs[]`, aplica `WorldContainer.localScale` en `Awake()`. La activacion de AR managers se difiere a corrutina en `Start()` que espera `ARSession.state >= SessionInitializing` (timeout 5s). En modo Bonsai: seguimiento continuo de `ARTrackedImage` sin `ARAnchor`, dispara `OnBonsaiImageDetected` en primera deteccion. |

### Core (19 scripts) -- `_Project.Scripts.Core`

| Script | Tipo | Responsabilidad |
|--------|------|-----------------|
| `BlockDatabaseSO` | ScriptableObject | Array de `BlockEntry` (type + prefab). Lazy `Dictionary<BlockType,GameObject>` para O(1). `OnValidate` editor-only para detectar duplicados. |
| `HarmonyConfig` | ScriptableObject | `varietyWeight` (0.45), `decorationWeight` (0.35), `quantityWeight` (0.20), `fullVarietyTypeCount` (6), `targetBlockCount` (50), `targetPebbleCount` (25), `minSandBlocks` (10), `minGrassBlocks` (10), `gateStrength` (0.85). |
| `WorldModeSO` | ScriptableObject | `Mode`, `DisplayName`, `WorldContainerScale`, `AnchorType` (enum anidado: `ARPlane`/`TrackedImage`), `ImageLibrary` (XRReferenceImageLibrary para Bonsai), `ImagePhysicalWidth`, `MaxBlocks`. |
| `GardenSaveData` | Class (structs) | Estructuras serializables para persistencia: `VoxelSaveData` (blockType + position), `PebbleSaveData` (prefabIndex + transform), `GardenSaveData` (gardenName, createdAt, voxels[], pebbles[]). JsonUtility compatible. |
| `SaveLoadService` | MonoBehaviour | Implementa `ISaveLoadService`. Serializa WorldContainer a JSON en `persistentDataPath/Gardens/`. `SaveCurrentGarden()` itera hijos, `ApplyGarden()` limpia mundo, instancia via `BlockDatabaseSO`, arma con `ArmForImmediate()`, limpia undo, rescanea armonia. |
| `GridManager` | MonoBehaviour | Dueno de `_gridSize` (1.0). `GetSnappedPosition()`: `Floor(pos/size)*size + half`. Facade de `GridVisualizer`: `ActivateGrid()` / `DeactivateGrid()`. |
| `GridVisualizer` | MonoBehaviour | Crea un `GameObject` hijo con `MeshFilter` + `MeshRenderer`. Genera mesh de lineas con fade radial (alpha proporcional a 1 - sqrDist/sqrRadius). Solo reconstruye al cambiar celda central. Buffers reutilizados para zero-GC. |
| `HarmonyService` | MonoBehaviour | Evalua armonia. `Dictionary<BlockType,int>` para conteos. Eventos: `OnHarmonyChanged(float)`, `OnPerfectHarmony`, `OnWorldReset`. Suscrito via EventBus a: `BlockPlacedEvent`, `BlockDestroyedEvent`, `PebblePlacedEvent`, `PebbleDestroyedEvent`, `WorldResetEvent`, `UndoPerformedEvent`, `RedoPerformedEvent`. Handlers privados (`Handle*`) actualizan conteos y llaman `Recalculate()`. `RebuildCounters()` escanea O(n) el WorldContainer tras undo/redo/reset. |
| `GameAudioService` | MonoBehaviour | One-shot SFX con pitch variation (+/-0.15). Anti-repeticion en arrays. `AudioSource` asignado via Inspector. |
| `MusicService` | MonoBehaviour | Shuffle Fisher-Yates, crossfade entre tracks (2s), volume slider. `AudioSource` dedicado. Evento `OnVolumeChanged`. |
| `UIAudioService` | MonoBehaviour | 7 pools de clips + 4 clips individuales para fases armonia. Pitch variation +/-0.05. Vibracion haptica integrada en todos los `Play*()`. `[RequireComponent(AudioSource)]`. |
| `LightingService` | MonoBehaviour | Toggle entre modo Global (Directional Light ON, Spot OFF) y Focus (Directional OFF, Spot ON, con rango/intensidad/angulo configurables). Evento `OnLightingToggled(bool)`. Configurable `_disableGlobalOnFocus`. |
| `HapticService` | MonoBehaviour | Wrapper del plugin Vibration. 3 presets: `VibrateLight()` (~50ms), `VibrateMedium()` (~100ms), `VibrateHeavy()` (triple-tap). Toggle ON/OFF, default OFF. Evento `OnHapticsToggled(bool)`. Lazy `Init()` del plugin nativo. |
| `ScreenshotService` | MonoBehaviour | `Capture()` con debounce. Oculta canvas, `ReadPixels`, guarda a galeria via `NativeGallery` (fallback `persistentDataPath` en Editor). Flash visual, audio, haptic, toast de confirmacion. Evento `OnScreenshotCaptured(path)`. |
| `WorldResetService` | MonoBehaviour | `ResetWorld()`: destroy blocks (reversa, solo `VoxelBlock`/`ProceduralPebble`), reset anchor, deactivate grid, clear undo, reset harmony. Evento `OnWorldReset`. |
| `SceneTransitionService` | MonoBehaviour | Singleton (`DontDestroyOnLoad`). Crea Canvas overlay propio (sort order 999) con Image negra + CanvasGroup. `TransitionTo(sceneName)`: fade-to-black 0.4s -> `LoadSceneAsync` -> fade-in 0.4s. Usa `Time.unscaledDeltaTime`. Bloquea raycasts durante transicion. |
| `UndoRedoService` | MonoBehaviour | `Stack<IUndoableAction>` con cap 20. `Record()`, `Undo()`, `Redo()`, `Clear()`. Evento `OnStackChanged(canUndo, canRedo)`. |
| `PlaceBlockAction` | Class | Command: `Undo()` -> `Destroy(instance)`. `Redo()` -> `Instantiate` + `ArmForImmediate()`. Metodo estatico compartido `ArmForImmediate()`: deshabilita `BlockSpawn`, habilita `Collider` + `BlockDestroy.SetReady()`. |
| `DestroyBlockAction` | Class | Command: `Undo()` -> `Instantiate` + `PlaceBlockAction.ArmForImmediate()`. `Redo()` -> `Destroy(restoredInstance)`. Creado por `BlockDestroyer` (tap) y `BlockDestroy` (proximity knock). |

### Infrastructure (17 scripts) -- `_Project.Scripts.Infrastructure`

| Script | Tipo | Responsabilidad |
|--------|------|-----------------|
| `ServiceLocator` | Static class | Registro/resolucion de servicios por interfaz. `Register<T>()`, `Unregister<T>()`, `TryGet<T>()`, `IsRegistered<T>()`. |
| `EventBus` | Static class | Pub/sub tipado cross-sistema. `Subscribe<T>()`, `Unsubscribe<T>()`, `Publish<T>()`. |
| `GameEvents` | Structs | 9 structs de eventos del juego (e.g. `BlockPlacedEvent`, `BlockDestroyedEvent`, `UndoPerformedEvent`). |
| `BlockType` | Enum | `Sand(0)`, `Glass(1)`, `Stone(2)`, `Wood(3)`, `Torch(4)`, `Grass(5)`. |
| `ToolType` | Enum | `Build_Sand(0)` a `Build_Grass(5)`, `Tool_None(6)`, `Tool_Destroy(7)`, `Tool_Brush(8)`, `Tool_Plow(9)`. |
| `WorldMode` | Enum | `None(-1)` (sentinel), `Bonsai(0)`, `Normal(1)`, `Real(2)`. |
| `WorldModeContext` | Static class | `Selected` (WorldMode, default `None`). Canal cross-escena sin DontDestroyOnLoad. |
| `IGameAudioService` | Interface | Contrato para `GameAudioService`. |
| `IGridManager` | Interface | Contrato para `GridManager`. |
| `IHapticService` | Interface | Contrato para `HapticService`. |
| `IHarmonyService` | Interface | Contrato para `HarmonyService`. |
| `ISaveLoadService` | Interface | Contrato para `SaveLoadService`. `SaveCurrentGarden()`, `GetSavedGardensList()`, `LoadGarden()`, `ApplyGarden()`, `DeleteGarden()`. |
| `ISceneTransitionService` | Interface | Contrato para `SceneTransitionService`. |
| `IToolManager` | Interface | Contrato para `ToolManager`. |
| `IUIAudioService` | Interface | Contrato para `UIAudioService`. |
| `IUndoRedoService` | Interface | Contrato para `UndoRedoService`. |
| `IUndoableAction` | Interface | Contrato `Undo()`, `Redo()`. |

### Interaction (7 scripts) -- `_Project.Scripts.Interaction`

| Script | Responsabilidad |
|--------|-----------------|
| `ToolManager` | `CurrentTool` (default `Build_Sand`), `IsBuildTool` (rango 0-5), `SelectToolByIndex(int)`, `GetCurrentBlockPrefab()`, `GetBlockPrefab(BlockType)`. Evento `OnToolChanged`. |
| `TouchInputRouter` | Punto de entrada de input tactil. Captura `Touch.Began`, filtra toques sobre UI, cede al `BrushTool` si activo, despacha a `ARBlockPlacer` o `BlockDestroyer` segun herramienta. |
| `ARBlockPlacer` | Solo colocacion de bloques. `TryPlaceBlock()` resuelve root block via `GetComponentInParent<VoxelBlock>` para stacking correcto. `ProcessAndPlace()`: snap, validacion, `Instantiate`. `_pendingCells` HashSet contra double-tap. Sin audio ni VFX (el prefab los gestiona via `BlockSpawn`). Registra `PlaceBlockAction`. |
| `BlockDestroyer` | Solo destruccion de bloques y piedritas. `TryDestroyBlock()` con physics raycast (voxel + pebble layers). Registra `DestroyBlockAction`. Sin audio ni VFX (el prefab los gestiona via `BlockDestroy`). Publica `BlockDestroyedEvent` via `EventBus`. |
| `BrushTool` | Toggle `IsBrushActive`. `Btn_Brush.OnClick` llama directamente a `ToggleBrush()` (mode overlay, no pasa por ToolManager). En Update si activo: consume `Touch.activeTouches`, llama `ARBlockPlacer`/`BlockDestroyer`/`PlowTool` cada `_strokeCooldown` (0.08s). Evento `OnBrushToggled(bool)`. |
| `PlowTool` | Decorador de piedritas. Raycast propio (voxel + AR). `PlaceAt()`: scatter, normal alignment, random scale/rotation, `PebbleSupport.Configure()`, `BlockSpawn.Play()`. Publica `PebblePlacedEvent` via `EventBus`. No registra undo. |
| `DebugRayVisualizer` | Dibuja rayo de 0.1s desde camara en cada tap. Toggle `_enabled`. `LineRenderer` asignado via Inspector. Development-only. |

### Title (6 scripts) -- `_Project.Scripts.Title`

| Script | Responsabilidad |
|--------|-----------------|
| `TitleSceneManager` | `SelectMode(int)`: escribe `WorldModeContext.Selected` y transiciona a `Main_AR` via `SceneTransitionService.TransitionTo()`. Wiring: `Btn_Bonsai -> 0`, `Btn_Normal -> 1`, `Btn_Real -> 2`. Fuerza `Screen.orientation = Portrait`. |
| `CreeperFaceFilter` | Suscribe a `ARFaceManager.trackablesChanged`, instancia prefab `Object_Creeper` como hijo del `ARFace`. Offset `(0, -0.1, 0.13)`, rotacion y escala `(0.12, 0.12, 0.12)` configurables en Inspector. Cleanup en `OnDestroy`. |
| `HandTrackingService` | Inicializa MediaPipe HandLandmarker (GPU delegate con fallback CPU, IMAGE mode). Captura frames de `ARCameraManager`, extrae landmark #8 (indice) con smoothing (0.7), rotacion 270 grados para front camera portrait. Pinch: distancia thumb tip (#4) <-> index tip (#8) con histeresis (enter 0.055, exit 0.08) y debounce (2 frames). Eventos: `OnFingertipScreenPosition`, `OnHandDetected`, `OnHandLost`, `OnPinchDetected`. |
| `HandCursorUI` | Cursor UI que sigue la punta del indice. `RectTransformUtility.ScreenPointToLocalPointInRectangle` para conversion a canvas. Fade in/out via `CanvasGroup`. Escala gradual del dot al hover (1x -> 1.35x, speed 1.5/s). Dot se oculta durante dwell progress (solo se ve ring radial). |
| `DwellSelector` | Solapamiento cursor con `RectTransform` de botones via `RectTransformUtility.RectangleContainsScreenPoint`. Dos modos: **pinch click** (instantaneo via `OnPinchDetected`) y **dwell timer** (3s en Inspector, fallback). Highlight: escala 1.12x + tint blanco. Drives `HandCursorUI.SetDwellProgress()` y `SetHovering()`. `[RequireComponent(AudioSource)]`. |
| `TitleLogoAnimator` | Bobbing vertical del logo "ARMONIA" (`HUD_Logo`, Image) con oscilacion senoidal. Amplitud (+/-12px) y frecuencia configurables (default 0.6Hz, en escena 0.2Hz). Opera sobre `RectTransform.anchoredPosition`. |

### UI (13 scripts) -- `_Project.Scripts.UI`

| Script | Responsabilidad |
|--------|-----------------|
| `UIManager` | Selector highlight (`_selectorRect`) que sigue al slot activo. `_slotRects[]` indexado por valor int de `ToolType`. `OnSlotClicked(int)` delega a `ToolManager.SelectToolByIndex()`. |
| `HarmonyHUD` | Barra fill animada (`_fillRect.anchorMax.x`), gradiente tricolor, 5 frases por fase, pop/shake. Flag `_frozen` para post-perfect. Vibracion haptica escalada por fase. |
| `HarmonyParticles` | `[RequireComponent(ParticleSystem)]`. Burst 120 particulas x 3 repeticiones. Ambient 5/s continuas. Colores: dorado, melocoton, lavanda, blanco. |
| `PerfectHarmonyPanel` | `[RequireComponent(CanvasGroup)]`. Fade in/out con SmoothStep. Suscrito a `HarmonyService.OnPerfectHarmony` y `OnWorldReset`. |
| `ScreenshotToastPanel` | `[RequireComponent(CanvasGroup)]`. Toast de confirmacion post-captura. `Show(Texture2D)` -> fade in -> `Btn_Continue` -> fade out. |
| `UndoRedoHUD` | Botones `_undoButton`/`_redoButton`. Alpha enabled/disabled (1.0/0.35). |
| `BrushHUD` | Suscrito a `BrushTool.OnBrushToggled`. Dim/restore de `Image.color`. |
| `GameOptionsMenu` | Controlador UI del dropdown de opciones. 5 toggles (iluminacion, profundidad, grid, plano, vibracion), slider de musica, foto, guardar jardin, reset con dialogo de confirmacion, salir. `SaveGarden()` abre `SaveGardenPopup`. `ExitGame()` transiciona a `Title_Screen` via `SceneTransitionService`. |
| `SaveGardenPopup` | `[RequireComponent(CanvasGroup)]`. Modal con `TMP_InputField` para nombre de jardin. `Show()` -> fade in. `OnSave()` valida nombre no vacio, delega a `ISaveLoadService.SaveCurrentGarden()`. |
| `BonsaiSelectorPopup` | `[RequireComponent(CanvasGroup)]`. Modal para seleccion de jardin en modo Bonsai. Lista dinamica de botones o estado vacio con "Volver al Menu". Instancia `UI_GardenListItem` por cada jardin guardado. |
| `OrientationManager` | Detecta portrait/landscape. Oculta hotbar/toolpanel en landscape. Fuerza `Tool_None`. Restaura tool en portrait. |
| `ButtonPressAnimation` | `[RequireComponent(Button)]`. `IPointerDownHandler` + `IPointerUpHandler`. Squeeze scale-down/up automatico. |
| `DropdownButtonState` | Dim/restore de `Image.color` para toggles ON/OFF. `SetState(bool)`. |

### Voxel (8 scripts) -- `_Project.Scripts.Voxel`

| Script | Responsabilidad |
|--------|-----------------|
| `VoxelBlock` | Ficha de identidad del bloque: `_blockType`, `_placeSounds[]`, `_breakSounds[]`. |
| `BlockSpawn` | Animacion fly-in + feedback de colocacion. Deshabilita `Collider` y `BlockDestroy` durante vuelo. Auto-localiza `GameAudioService`, `HapticService` via ServiceLocator. |
| `BlockDestroy` | Proximidad knock + `BreakFromTool`. `KnockRoutine`: impulso/torque + shrink + `Destroy`. Auto-localiza servicios via ServiceLocator. |
| `SandGravity` | Gravedad para arena: suscripcion a `BlockDestroyedEvent` (trigger primario) + safety poll cada 1s. Reserva de celda destino via `HashSet` estatico para stacking correcto. Caida animada (ease-in, velocidad configurable). `Physics.SyncTransforms()` tras aterrizar. Solo en `Voxel_Sand`. |
| `ProceduralPebble` | `[RequireComponent(MeshFilter, MeshRenderer, MeshCollider)]`. Genera mesh icosaedro jittered con semilla aleatoria. |
| `PebbleSupport` | Poll periodico. Raycast hacia `-_supportDir`. Si no hay apoyo -> `BlockDestroy.BreakFromTool()`. |
| `VFXBlockPlace` | ParticleSystem burst + scale pop. Auto-destroy a 0.8s. |
| `VFXBlockDestroy` | ParticleSystem burst de cubitos con gravedad. Auto-destroy a 1.0s. |

---

## 10. Estado del proyecto

### Lo que funciona

- **AR Foundation:** deteccion de planos, ancla espacial, oclusion por profundidad (toggle), alineacion de grid al shader del plano.
- **Construccion voxel:** tap para colocar, stacking por caras, snap a grid, reserva de celda contra double-tap.
- **6 tipos de bloque:** Sand, Glass, Stone, Wood, Torch, Grass con prefabs, sonidos y VFX diferenciados.
- **Gravedad de arena:** los bloques Sand caen si no tienen soporte debajo (bloque o plano AR); evento `BlockDestroyedEvent` como trigger primario + safety poll cada 1s, reserva de celda destino para stacking correcto, animacion ease-in, `Physics.SyncTransforms()` tras aterrizar.
- **Herramienta Destruir:** raycast fisico, impulso con Rigidbody, tumble, shrink, VFX.
- **Pincel Rapido:** toggle ON/OFF, placement/destroy continuo, cooldown 0.08s.
- **Decorador de Piedritas:** piedras procedurales icosaedro, rotacion/escala/scatter aleatorio, soporte con auto-destruccion.
- **Grid visual:** mesh procedural de lineas con fade radial, zero-GC.
- **Sistema de Armonia:** 3 pilares + gate de minimos, 100% event-driven.
- **HarmonyHUD:** barra animada, gradiente tricolor, frases por fase, pop/shake.
- **Panel Armonia Perfecta:** fade, particulas procedurales multicolor, boton Continuar.
- **Undo/Redo:** patron Command, stack con cap de 20, HUD con botones atenuados.
- **Menu opciones:** 5 toggles (iluminacion, profundidad, grid, plano visual, vibracion), slider de musica, foto, guardar jardin, reset con confirmacion, salir.
- **Audio:** `GameAudioService` (SFX con pitch variation), `UIAudioService` (7 pools + 4 fases armonia + haptic integrado), `MusicService` (shuffle, crossfade, slider).
- **Vibracion haptica:** `HapticService` via plugin Vibration. 3 presets. Toggle ON/OFF (default OFF).
- **Screenshot:** captura sin UI visible, guardado en galeria, flash visual, toast de confirmacion.
- **Guardado de jardin:** `SaveLoadService` serializa bloques y piedras del WorldContainer a JSON en `persistentDataPath/Gardens/`. Popup con `TMP_InputField` para nombre. Carga restaura mundo con `ArmForImmediate`, limpia undo, rescanea armonia.
- **Pantalla de inicio:** Camara frontal, Face Tracking con Creeper, 3 botones de modo. Logo con bobbing. Retorno via `Btn_Exit`.
- **Hand Tracking:** Cursor de indice via MediaPipe (GPU delegate). Seleccion por **pinch click** o **dwell time** (3s). Highlight de botones al hover.
- **Transiciones de escena:** `SceneTransitionService` singleton. Fade-to-black 0.4s -> async load -> fade-in 0.4s.
- **Seleccion de modo:** `TitleSceneManager.SelectMode(int)` -> `WorldModeContext` -> `SceneTransitionService` -> `WorldModeBootstrapper` con activacion diferida.

### Parcial

- **Modo Bonsai:** `XRReferenceImageLibrary` configurada con imagenes `one` (0.13m) y `qr_prueba` (0.10m). Activacion diferida de `ARTrackedImageManager`. Seguimiento continuo de imagen (WorldContainer sigue la carta). `BonsaiSessionController` abre `BonsaiSelectorPopup` al detectar imagen. Selector muestra jardines guardados o estado vacio con boton de vuelta al menu. Funcional en dispositivo, pendiente de testeo exhaustivo.

### Trabajo a futuro

| Feature | Detalle |
|---------|---------|
| Tutorial / Onboarding | No hay guia para jugadores nuevos. |
| Logros / Progresion | No hay sistema mas alla de la barra de armonia. |
| Luz dinamica de antorchas | El prefab Torch tiene un `Light` component URP pero no emite. |
| Agua / Bloques animados | No hay shaders animados ni bloque de agua. |
| Sonido ambiente adaptativo | No hay sonidos de naturaleza que cambien con el jardin. |
| Multijugador / Compartir | No hay networking ni exportacion del jardin. |

---

## 11. Dependencias de paquetes

| Paquete | Version | Uso |
|---------|---------|-----|
| `com.unity.xr.arfoundation` | 6.0.6 | AR Foundation: sesion, planos, anclas, raycast, oclusion, imagenes, caras. |
| `com.unity.xr.arcore` | 6.0.6 | ARCore XR Plugin para Android. |
| `com.unity.xr.interaction.toolkit` | 3.0.10 | XR Interaction Toolkit (XR Origin, Ray Interactor). |
| `com.unity.inputsystem` | 1.17.0 | Enhanced Touch API (input tactil). |
| `com.unity.render-pipelines.universal` | 17.0.4 | Universal Render Pipeline. |
| `com.unity.ugui` | 2.0.0 | UI Canvas, Button, Image, Slider. |
| `com.unity.cloud.gltfast` | (git) | Importador glTF para modelos `.glb` de bloques. |
| `com.unity.timeline` | 1.8.10 | Timeline (no utilizado activamente). |
| `com.unity.visualscripting` | 1.9.7 | Visual Scripting (no utilizado activamente). |
| `com.unity.ai.navigation` | 2.0.11 | AI Navigation (no utilizado activamente). |
| `com.yasirkula.nativegallery` | (git) | NativeGallery: guarda screenshots en la galeria del dispositivo. |
| `com.benoitfreslon.vibration` | (git) | Vibration: respuestas hapticas nativas. |
| `com.github.homuler.mediapipe` | 0.16.3 (embedded) | MediaPipe Unity Plugin. Hand Landmark Detection (GPU delegate) + Face Tracking para Title_Screen. Modelo: `hand_landmarker.bytes` en StreamingAssets. |
