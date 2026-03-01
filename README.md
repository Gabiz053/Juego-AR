# 🌿 ARmonia — Jardín Zen en Realidad Aumentada

![Unity](https://img.shields.io/badge/Unity-6-black?style=flat-square&logo=unity)
![ARCore](https://img.shields.io/badge/ARCore-XR-blue?style=flat-square)
![Android](https://img.shields.io/badge/Android-Target_S24_Ultra-green?style=flat-square&logo=android)
![URP](https://img.shields.io/badge/Render-URP-red?style=flat-square)

**ARmonia** es un sandbox creativo en Realidad Aumentada (AR) para un solo jugador.
Diseñado para dispositivos Android de gama alta (Target: Samsung Galaxy S24 Ultra),
el juego permite a los usuarios construir y relajarse creando su propio jardín zen
virtual directamente en el suelo de su casa.

Inspirado en el sistema de construcción de *Minecraft Earth*, ARmonia combina la
precisión matemática de los voxels con una estética pixel art retro y lo último en
tecnología de inmersión AR móvil.

---

## ✨ Características Implementadas

### 🛠️ Construcción Voxel Precisa (Snap & Build)

Un motor de construcción por cuadrícula (Grid) donde los bloques nunca se superponen.
Al apuntar y tocar, el sistema detecta la cara del bloque existente y coloca el nuevo
cubo perfectamente alineado al grid, garantizando una experiencia de construcción
fluida y satisfactoria. La colocación funciona tanto sobre planos AR detectados
(primer bloque) como apilando sobre bloques existentes (physics raycast).

### 📏 Escala Dinámica — Tu Jardín, Tu Espacio

El mundo virtual se adapta a tu entorno. El `WorldContainer` se puede escalar al
instante:

* **Modo Mini:** Construye una maqueta sobre la mesa de tu salón.
* **Modo Mega:** Escala 1:1 para caminar entre los bloques en el suelo de tu
  habitación o jardín real.

### 🎒 Inventario Clásico (Hotbar) y Herramientas

Una interfaz intuitiva en la parte inferior de la pantalla te da acceso rápido a
tus materiales y herramientas:

* **5 Bloques:** Tierra (Dirt), Arena (Sand), Piedra (Stone), Madera (Wood) y
  Antorcha (Torch).
* **Herramienta Destruir:** Elimina cualquier bloque con un solo toque.
* **Herramienta Pincel (Brush):** Slot reservado para el pincel 3D (v2 — planned).
* **Herramienta Arado (Plow):** Slot reservado para peinar arena (v2 — planned).
* **Tool None:** Modo mano vacía — el toque no coloca ni destruye nada.

### 🪄 Inmersión AR

* **Estabilidad Total:** AR Anchors espaciales fijan el mundo al suelo; no tiembla
  al caminar alrededor.
* **Grid Visual Radial:** Un halo de líneas con fade radial sigue al jugador para
  visualizar la cuadrícula de construcción.
* **Iluminación Manual:** Botón para encender/apagar la luz direccional de la escena.

### 📸 Captura y Menú de Opciones

* **Cámara Nativa:** Oculta la UI y captura una screenshot con nombre con timestamp
  a `Application.persistentDataPath`.
* **Borrar Todo:** Popup de confirmación que destruye todos los bloques, resetea el
  AR anchor y desactiva el grid.
* **Orientación Automática:** En landscape se ocultan los controles de construcción
  y se fuerza Tool None; al volver a portrait se restaura la herramienta anterior.
* **Audio Service:** Servicio centralizado de SFX con variación de pitch aleatoria.

---

## 🏗️ Arquitectura del Código

El proyecto sigue una arquitectura modular organizada en **5 carpetas** con
**15 scripts** (12 MonoBehaviours, 2 enums, 1 ScriptableObject):

### Voxel (`_Project.Scripts.Voxel`)

* **`BlockType`** — Enum con los 5 tipos de bloque (Dirt, Sand, Stone, Wood, Torch).
* **`VoxelBlock`** — Componente en cada prefab con tipo, sonido de colocación y
  sonido de destrucción.
* **`BlockDatabase`** — ScriptableObject que mapea cada `BlockType` a su prefab
  con lookup O(1) lazy dictionary.

### Interaction (`_Project.Scripts.Interaction`)

* **`ToolType`** — Enum con las 9 herramientas (5 build + 4 utility). Los valores
  int están baked en los OnClick de los botones de la escena.
* **`ToolManager`** — Gestiona la herramienta seleccionada. Evento `OnToolChanged`.
  Convierte `ToolType` → `BlockType` → prefab via `BlockDatabase`.
* **`ARBlockPlacer`** — Sistema de interacción touch. AR plane raycast para el
  primer bloque, physics raycast para apilar. Valida distancia, overlap y posición
  de cámara.
* **`DebugRayVisualizer`** — Dibuja un rayo de debug temporal desde la cámara en
  cada tap usando `LineRenderer` y coroutine.

### Core (`_Project.Scripts.Core`)

* **`GridManager`** — Facade del grid: posee `GridSize`, `GetSnappedPosition()`,
  y delega visualización a `GridVisualizer`.
* **`GridVisualizer`** — Genera un mesh de líneas dinámico con fade radial,
  zero-GC buffers, y solo reconstruye cuando cambia la celda central.
* **`GameAudioService`** — Servicio de audio one-shot con variación de pitch.
  `[RequireComponent(AudioSource)]`.

### AR (`_Project.Scripts.AR`)

* **`ARWorldManager`** — Crea un `ARAnchor` en la primera colocación, orienta el
  `WorldContainer` hacia el jugador, y activa el grid. Método `ResetAnchor()`.

### UI (`_Project.Scripts.UI`)

* **`UIManager`** — Selector highlight amarillo que sigue al slot activo.
  Suscrito a `ToolManager.OnToolChanged`.
* **`GameOptionsMenu`** — Controlador UI puro del dropdown de opciones. Delega a
  `WorldResetService` y `ScreenshotService`.
* **`OrientationManager`** — Detecta portrait/landscape y oculta/muestra la UI
  de construcción.
* **`ScreenshotService`** — Captura con ocultación de canvas y debounce.
* **`WorldResetService`** — Destruye bloques (solo `VoxelBlock`), resetea anchor,
  desactiva grid.

### Mapa de GameObjects ↔ Scripts

| GameObject | Scripts |
| --- | --- |
| XR Origin (Mobile AR) | `ARBlockPlacer`, `ARWorldManager`, `GameAudioService`, `DebugRayVisualizer` |
| WorldContainer | `GridManager`, `GridVisualizer` |
| ToolManager | `ToolManager` |
| MainCanvas | `UIManager`, `OrientationManager` |
| HUD_OptionsMenu | `GameOptionsMenu` |
| Svc_Screenshot | `ScreenshotService` |
| Svc_WorldReset | `WorldResetService` |

---

## 🗺️ Hoja de Ruta del Desarrollo (Roadmap)

* [x] **FASE 1 — Cimientos AR**
  * Configurar proyecto URP en Android.
  * Instalar AR Foundation, ARCore XR Plugin y ARCore Extensions.
  * Crear la escena AR base y configurar la detección de planos (solo suelo).
* [x] **FASE 2 — El Motor Voxel**
  * Crear prefabs de los 5 bloques con sus colliders y `VoxelBlock`.
  * Programar `GridManager` + `GridVisualizer` (snap math + radial grid mesh).
  * Programar `ARBlockPlacer` (AR raycast + physics raycast + placement validation).
* [x] **FASE 3 — Fusión AR + Escalas**
  * Adaptar instanciación al `ARRaycastManager`.
  * Crear el `WorldContainer` y la lógica de escalas dinámicas.
  * Implementar `ARWorldManager` con AR Anchor en el primer bloque colocado.
* [x] **FASE 4 — Interfaz e Inventario**
  * Crear Canvas UI (Hotbar de 6 bloques + 3 herramientas).
  * Programar `ToolManager` + `UIManager` + `BlockDatabase`.
  * Activar herramienta de Destrucción vía `ARBlockPlacer`.
* [ ] **FASE 5 — Herramientas Zen** *(planned)*
  * Implementar Herramienta Arado (cambio de materiales al tocar arena).
  * Implementar Pincel 3D Mágico (instanciación al arrastrar, con/sin Rigidbody).
* [ ] **FASE 6 — Iluminación y Oclusión** *(planned)*
  * Implementar foco móvil (control de Directional Light por giroscopio).
  * Configurar AR Light Estimation.
  * Activar AR Occlusion Manager (Depth API).
* [x] **FASE 7 — Pulido**
  * Configurar `GameAudioService` para SFX con pitch variation.
  * `ScreenshotService` — captura con canvas hiding.
  * `WorldResetService` — clear all con confirmación.
  * `OrientationManager` — portrait/landscape responsive.
  * `GameOptionsMenu` — dropdown con lighting toggle, photo, clear all, exit.

---

## ⚙️ Especificaciones Técnicas

* **Motor:** Unity 6
* **Render Pipeline:** Universal Render Pipeline (URP)
* **API Gráfica:** OpenGLES3
* **Plataforma:** Android (Target: Samsung Galaxy S24 Ultra)
* **AR SDK:** AR Foundation + ARCore XR Plugin
* **Input:** Enhanced Touch (Input System)
* **UI:** TextMeshPro, Canvas + GraphicRaycaster + InputSystemUIInputModule
* **Arquitectura:** Clean Code modular, 5 namespaces, servicios delegados,
  cero `GetComponent` en `Update`, cero `Find()`/`FindObjectOfType()`,
  todo cacheado en `Awake()`, todo serializable con `[Tooltip]`
