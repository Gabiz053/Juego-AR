# 🌿 ARmonia: Jardín Zen en Realidad Aumentada

![Unity](https://img.shields.io/badge/Unity-6-black?style=flat-square&logo=unity)
![ARCore](https://img.shields.io/badge/ARCore-XR-blue?style=flat-square)
![Android](https://img.shields.io/badge/Android-Target_S24_Ultra-green?style=flat-square&logo=android)
![URP](https://img.shields.io/badge/Render-URP-red?style=flat-square)

**ARmonia** es un sandbox creativo en Realidad Aumentada (AR) para un solo jugador. Diseñado para dispositivos Android de gama alta (Target: Samsung Galaxy S24 Ultra), el juego permite a los usuarios construir y relajarse creando su propio jardín zen virtual directamente en el suelo de su casa.

Inspirado en el sistema de construcción de *Minecraft Earth*, ARmonia combina la precisión matemática de los voxels con una estética pixel art retro y lo último en tecnología de inmersión AR móvil.

---

## ✨ Características del Juego

### 🛠️ Construcción Voxel Precisa (Snap & Build)
Un motor de construcción por cuadrícula (Grid) donde los bloques nunca se superponen. Al apuntar y tocar, el sistema detecta la cara del bloque existente y coloca el nuevo cubo de 1x1x1 perfectamente alineado, garantizando una experiencia de construcción fluida y satisfactoria.

### 📏 Escala Dinámica: Tu Jardín, Tu Espacio
El mundo virtual se adapta a tu entorno. A través de la interfaz, puedes cambiar la escala del mundo (`WorldContainer`) al instante:
* **Modo Mini:** Construye una maqueta sobre la mesa de tu salón.
* **Modo Mega:** Escala 1:1 para caminar entre los bloques en el suelo de tu habitación o jardín real.

### 🎒 Inventario Clásico (Hotbar) y Herramientas Zen
Una interfaz intuitiva en la parte inferior de la pantalla te da acceso rápido a tus materiales y herramientas:
* **Bloques Base:** Arena, Piedra, Madera, Agua (transparente) y Antorchas (emisivas).
* **Herramienta Destruir:** Elimina cualquier bloque con un toque.
* **Herramienta Arado:** Toca la arena para "peinarla" y cambiar su textura por arena arada, al puro estilo de un jardín zen tradicional.
* **Pincel 3D Mágico:** Arrastra el dedo por el aire para instanciar elementos flotantes. Incluye la opción de activar físicas para que caigan suavemente sobre tu jardín.

### 🪄 Inmersión AR Avanzada
* **Oclusión Real (Depth API):** Tus manos, tus muebles y tus paredes tapan los bloques virtuales. El jardín virtual existe *detrás* de los objetos reales.
* **Iluminación Híbrida:** Alterna entre usar la luz ambiental de tu habitación (AR Light Estimation) o tomar el control del sol rotando físicamente tu dispositivo gracias al giroscopio.
* **Estabilidad Total:** Uso de AR Anchors espaciales para que el mundo se quede clavado en el suelo y no tiemble mientras te mueves a su alrededor.

### 📸 Estética Next-Gen y Modo Social
A pesar de su estética Pixel Art (texturas 16x16 sin compresión), el juego utiliza todo el poder del Universal Render Pipeline (URP):
* **Gráficos Avanzados:** Sombras de contacto entre bloques (SSAO), resplandores en elementos mágicos y fuego (Bloom), y colores cinematográficos (Tonemapping ACES). Todo optimizado sobre OpenGLES3 para evitar sobrecalentamientos.
* **Medidor de Armonía:** El juego evalúa pasivamente la variedad y disposición de tus bloques.
* **Cámara Nativa:** Oculta la UI y captura tu obra de arte para compartirla directamente en tus redes sociales.

---

## 🗺️ Hoja de Ruta del Desarrollo (Roadmap)

A continuación, las fases de desarrollo del proyecto. Marca con una `x` (`[x]`) conforme se vayan completando.

- [ ] **FASE 1: Cimientos AR**
  - Configurar proyecto URP en Android.
  - Instalar AR Foundation, ARCore XR Plugin y ARCore Extensions.
  - Crear la escena AR base y configurar la detección de planos (solo suelo).
- [ ] **FASE 2: El Motor Voxel**
  - Crear Prefabs de los 5 bloques con sus colliders.
  - Programar `GridManager` (matemáticas de snap).
  - Programar `BlockPlacer` (sistema de Raycast a normales).
- [ ] **FASE 3: Fusión AR + Escalas**
  - Adaptar instanciación al ARRaycastManager.
  - Crear el `WorldContainer` y la lógica de escalas (Mini/Mega).
  - Implementar AR Anchor en el primer bloque colocado.
- [ ] **FASE 4: Interfaz e Inventario**
  - Crear Canvas UI (Hotbar estilo Minecraft).
  - Programar `ToolManager` para gestionar el inventario.
  - Activar herramienta de Destrucción.
- [ ] **FASE 5: Herramientas Zen**
  - Implementar Herramienta Arado (cambio de materiales).
  - Implementar Pincel 3D Mágico (instanciación al arrastrar, con/sin Rigidbody).
- [ ] **FASE 6: Iluminación y Oclusión**
  - Implementar Foco Móvil (control de Directional Light por Input.gyro).
  - Configurar AR Light Estimation.
  - Activar AR Occlusion Manager (Depth API).
- [ ] **FASE 7: Pulido y Social**
  - Configurar Audio Manager para SFX.
  - Crear lógica de UI del "Medidor de Armonía".
  - Integrar plugin Native Share para capturas de pantalla.

---

## ⚙️ Especificaciones Técnicas

* **Motor:** Unity 6
* **Render Pipeline:** Universal Render Pipeline (URP)
* **API Gráfica:** OpenGLES3 (Obligatorio)
* **Arquitectura:** Clean Code (PascalCase/camelCase), cero uso de `GetComponent` masivo en `Update`, enfocado en rendimiento móvil.