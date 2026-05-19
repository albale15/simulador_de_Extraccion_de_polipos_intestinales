using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instancia;
    [HideInInspector] public bool controlesBloqueados = false;
    void Awake()
    {
        instancia = this;
    }
    public List<PasoTutorial> pasos;
    private int pasoActual = -1;

    [Header("Referencias UI")]
    public GameObject panelInstrucciones;
    public TextMeshProUGUI txtInstrucciones;
    public TutorialHighlight mascara;

    [Header("Botones para Emergencias")]
    [Tooltip("Arrastra aquí el RectTransform del botón Succión del Panel Azul")]
    public RectTransform uiBotonSuccion;
    [Tooltip("Arrastra aquí el RectTransform del botón Lavar Lente del Panel Azul")]
    public RectTransform uiBotonLimpiado;

    [Header("Conexiones Simulator")]
    public EndoscopioCurvas endoscopio;
    public SistemaHerramientas herramientas;
    public MonitorEndoscopiaUI monitorUI;

    private bool esperandoCondicion = false;
    public GameObject objTextoPresionar;

    [Header("Objetos a Desactivar si NO es Tutorial")]
    [Tooltip("Pon aquí los Muros de Tutorial y los Pólipos pre-definidos de la escena.")]
    public List<GameObject> elementosSoloTutorial;

    [HideInInspector] public string accionEsperadaActiva = "";

    // Memoria para saber qué pedía el paso original antes de la emergencia
    private string accionPasoOriginal = "";
    void Start()
    {
        if (ManejadorPartida.dificultad != 0)
        {
            gameObject.SetActive(false);
            return;
        }
        if (elementosSoloTutorial != null)
        {
            foreach (GameObject obj in elementosSoloTutorial)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // Desactivamos el panel de instrucciones por si acaso
        if (panelInstrucciones != null) panelInstrucciones.SetActive(true);
        // El panel indicativo inicial de MonitorEndoscopiaUI se cierra y empezamos
        StartCoroutine(CicloTutorial());
    }
    void Update()
    {
        if (pasoActual < 0 || pasoActual >= pasos.Count) return;

        PasoTutorial p = pasos[pasoActual];

        // Verificamos si existe alguna emergencia dinámica
        bool sangrando = herramientas.ObtenerNivelSangrado() > 0f;
        bool inundado = herramientas.ObtenerLavadosSinSuccionar() > 0;
        bool sucio = herramientas.ObtenerNivelSuciedad() > 0.5f;


        if (sangrando || inundado || herramientas.ObtenerNivelSuciedad() > 0.2f)
        {
            if (panelInstrucciones != null) panelInstrucciones.SetActive(true);
            // SOBREESCRIBIMOS EL TUTORIAL TEMPORALMENTE
            if (sangrando)
            {
                txtInstrucciones.text = "<color=#FF0000>Cuando se hace un corte puede ensuciar un poco el lente con sangre,</color>\nMantén presionado <b>Succión</b> para limpiar el campo visual.";
                mascara.ResaltarElemento(uiBotonSuccion);
                accionEsperadaActiva = "Succion"; // Desbloqueamos este botón en el simulador
                
            }
            else if (inundado)
            {
                txtInstrucciones.text = "<color=#1E90FF>Cuando limpias se usa liquido este se pone en el intestino</color>\nPresiona <b>Succión</b> para aspirar el agua acumulada.";
                mascara.ResaltarElemento(uiBotonSuccion);
                accionEsperadaActiva = "Succion";
            }
            else if (herramientas.ObtenerNivelSuciedad() > 0.2f)
            {
                txtInstrucciones.text = "<color=#FF8C00>Cuando se ensucia el lente</color>\nPresiona <b>Lavar Lente</b> para poder ver con claridad.";
                mascara.ResaltarElemento(uiBotonLimpiado);
                accionEsperadaActiva = "Limpiado"; // Desbloqueamos este botón en el simulador
            }
        }
        else
        {
            if (panelInstrucciones != null)
            {
                bool pasoTieneTexto = !string.IsNullOrEmpty(p.instruccion);
                panelInstrucciones.SetActive(pasoTieneTexto);
            }
            // NO HAY EMERGENCIAS -> RESTAURAMOS EL TUTORIAL NORMAL
            txtInstrucciones.text = p.instruccion;
            if (p.uiAResaltar != null) mascara.ResaltarElemento(p.uiAResaltar);
            else mascara.Desactivar();

            // Restauramos el botón que originalmente pedía este paso
            accionEsperadaActiva = accionPasoOriginal;
        }
    }
    // Función auxiliar para pausar el avance si hay una emergencia
    public bool HayEmergencia()
    {
        if (herramientas == null) return false;
        return herramientas.ObtenerNivelSangrado() > 0f ||
               herramientas.ObtenerLavadosSinSuccionar() > 0 ||
               herramientas.ObtenerNivelSuciedad() > 0.5f;
    }

    IEnumerator CicloTutorial()
    {
        // Esperamos a que el usuario cierre el panel de "DATOS: TUTORIAL" inicial
        yield return new WaitUntil(() => Time.timeScale > 0);

        if (panelInstrucciones != null) panelInstrucciones.SetActive(true);

        while (pasoActual < pasos.Count - 1)
        {
            pasoActual++;
            pasoActual = Mathf.Clamp(pasoActual, 0, pasos.Count - 1);

            PasoTutorial p = pasos[pasoActual];

            // 1. Mostrar UI
            controlesBloqueados = p.bloquearControles;
            if (p.espera == PasoTutorial.TipoEspera.AccionInput)
            {
                accionPasoOriginal = p.accionRequerida; // Guardamos en memoria lo que pedía
            }
            else
            {
                accionPasoOriginal = ""; // Debemos vaciar la memoria para liberar los volantes y la inserción
                accionEsperadaActiva = "";
            }
            if (p.uiAResaltar != null && p.mostrarTemporalmente)
            {
                p.uiAResaltar.gameObject.SetActive(true);
            }

            txtInstrucciones.text = p.instruccion;
            if (p.uiAResaltar != null) mascara.ResaltarElemento(p.uiAResaltar);
            else mascara.Desactivar();

            if (objTextoPresionar != null)
            {
                objTextoPresionar.SetActive(p.mostrarTextoPresionar);
            }

            // 2. Esperar condición
            esperandoCondicion = true;
            Debug.Log("Esperando condición para paso: " + p.nombrePaso);
            yield return EvaluarCondicion(p);
            esperandoCondicion = false;


            if (p.uiAResaltar != null && p.mostrarTemporalmente)
            {
                p.uiAResaltar.gameObject.SetActive(false);
            }

            // 3. Liberar camino si existe collider
            if (p.colliderABloquear != null) p.colliderABloquear.SetActive(false);
        }
        controlesBloqueados = false;
        accionEsperadaActiva = "";
        accionPasoOriginal = "";
        // Fin del Tutorial
        txtInstrucciones.text = "Tutorial Completado. Regrese a la zona de extracción para finalizar.";
        mascara.Desactivar();
        // Le damos 4 segundos al jugador para leer el texto final
        yield return new WaitForSeconds(4f);

        // Apagamos el panel principal
        if (panelInstrucciones != null) panelInstrucciones.SetActive(false);

        // Y por si acaso, aseguramos que el texto parpadeante también se apague
        if (objTextoPresionar != null) objTextoPresionar.SetActive(false);
    }

    IEnumerator EvaluarCondicion(PasoTutorial p)
    {
        bool usandoPC = (endoscopio != null) ? !endoscopio.usarControlHardware : true;
        DatosProcesados hw = ConfigManager.instancia.datosActuales;

        switch (p.espera)
        {
            case PasoTutorial.TipoEspera.Tiempo:
                float t = 0;
                while (t < p.tiempoSegundos)
                {
                    // Si hay emergencia, congelamos el cronómetro de este paso
                    if (!HayEmergencia()) t += Time.deltaTime;
                    yield return null;
                }
                break;

            case PasoTutorial.TipoEspera.AccionInput:
                bool cumplido = false;
                bool requiereSoltarBoton = false;
                while (!cumplido)
                {
                    // SI HAY EMERGENCIA, IGNORAMOS EL INPUT DEL PASO LINEAL 
                    if (HayEmergencia())
                    {
                        yield return null;
                        continue;
                    }
                    hw = ConfigManager.instancia.datosActuales; // Refrescamos datos

                    // Movimientos
                    if (p.accionRequerida == "W") // Inserción
                    {
                        if (usandoPC && Input.GetKey(KeyCode.W)) cumplido = true;
                        if (!usandoPC && hw.insercionFinal > 0.05f) cumplido = true;
                    }
                    else if (p.accionRequerida == "S") // Extracción
                    {
                        if (usandoPC && Input.GetKey(KeyCode.S)) cumplido = true;
                        if (!usandoPC && hw.insercionFinal < -0.05f) cumplido = true;
                    }
                    // Botones
                    else if (p.accionRequerida == "Freeze")
                    {
                        if (usandoPC && Input.GetKeyDown(KeyCode.Alpha1)) cumplido = true;
                        if (!usandoPC && hw.botonFreeze) cumplido = true;
                    }
                    else if (p.accionRequerida == "Capture")
                    {
                        if (usandoPC && Input.GetKeyDown(KeyCode.Alpha2)) cumplido = true;
                        if (!usandoPC && hw.botonCapture) cumplido = true;
                    }
                    else if (p.accionRequerida == "Accion")
                    {
                        if (usandoPC && Input.GetKeyDown(KeyCode.Alpha5)) cumplido = true;
                        if (!usandoPC && hw.botonAccion) cumplido = true;
                    }
                    else if (p.accionRequerida == "Succion")
                    {
                        if (usandoPC && Input.GetKeyDown(KeyCode.Alpha4)) cumplido = true;
                        if (!usandoPC && hw.botonSuccion) cumplido = true;
                    }
                    yield return null;
                }
                if (requiereSoltarBoton && !usandoPC)
                {
                    while (true)
                    {
                        hw = ConfigManager.instancia.datosActuales;

                        // Rompemos este bucle de espera solo cuando el botón esté suelto (!hw.boton...)
                        if (p.accionRequerida == "Freeze" && !hw.botonFreeze) break;
                        if (p.accionRequerida == "Capture" && !hw.botonCapture) break;
                        if (p.accionRequerida == "Accion" && !hw.botonAccion) break;
                        if (p.accionRequerida == "Succion" && !hw.botonSuccion) break;

                        yield return null;
                    }
                }
                break;

            case PasoTutorial.TipoEspera.PolipoEliminado:
                int inicial = herramientas.ObtenerTotalEliminados();
                // Exigimos que cumpla la acción Y que no haya emergencias activas
                yield return new WaitUntil(() => herramientas.ObtenerTotalEliminados() > inicial && !HayEmergencia());
                break;

            case PasoTutorial.TipoEspera.LlegarAZona:
                // Espera a que el endoscopio esté en la zona de extracción (la variable que ya creaste)
                yield return new WaitUntil(() => herramientas.enZonaExtraccion && !HayEmergencia());
                break;
            case PasoTutorial.TipoEspera.PolipoEnMira:
                // Usa estrictamente el trigger de pólipos. 
                // Espera hasta que el colisionador de la cámara detecte uno.
                yield return new WaitUntil(() => herramientas.ObtenerPolipoEnMira() != null && !HayEmergencia());
                break;
        }
    }

    // Función para el Game Over en tutorial
    public void TutorialFallido()
    {
        StopAllCoroutines();
        txtInstrucciones.text = "<color=red>PROCEDIMIENTO FALLIDO. Reintente desde el menú principal.</color>";
        mascara.Desactivar();
    }
}