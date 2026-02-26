using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
// ESTOS DOS SON CLAVES PARA EL NUEVO SISTEMA
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(ARRaycastManager))]
[RequireComponent(typeof(LineRenderer))] // Esto añade el componente de línea automáticamente
public class ARBlockPlacer : MonoBehaviour
{
    [Header("Configuración AR")]
    public GameObject objectToPlace; // Tu Cubo

    private ARRaycastManager raycastManager;
    private LineRenderer lineRenderer;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        lineRenderer = GetComponent<LineRenderer>();

        // Configuración visual del Rayo (Línea Roja)
        ConfigurarLineaVisual();
    }

    // OBLIGATORIO: Activar el soporte táctil mejorado al iniciar
    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    // OBLIGATORIO: Desactivarlo al cerrar para no gastar memoria
    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        // Usamos el nuevo sistema 'Touch.activeTouches'
        if (Touch.activeTouches.Count > 0)
        {
            // Cogemos el primer dedo que toca la pantalla
            Touch touch = Touch.activeTouches[0];

            // Solo actuamos justo en el momento de tocar (Began)
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // 1. DIBUJAR EL RAYO VISUAL (DEBUG)
                StartCoroutine(DibujarRayo(touch.screenPosition));

                // 2. LÓGICA AR (Detectar plano)
                if (raycastManager.Raycast(touch.screenPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;
                    Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
                }
            }
        }
    }

    // Corrutina para dibujar una línea roja fugaz
    IEnumerator DibujarRayo(Vector2 screenPos)
    {
        lineRenderer.enabled = true;

        // Origen: La cámara
        Vector3 startPos = Camera.main.transform.position;

        // Destino: Un punto lejano hacia donde tocaste
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        Vector3 endPos = ray.origin + (ray.direction * 2.0f); // 2 metros de largo

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // Esperar 0.1 segundos y apagar la línea
        yield return new WaitForSeconds(0.1f);

        lineRenderer.enabled = false;
    }

    // Configuración automática para que la línea se vea bien sin tocar nada en el editor
    void ConfigurarLineaVisual()
    {
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Material básico blanco
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.enabled = false; // Empieza apagada
    }
}