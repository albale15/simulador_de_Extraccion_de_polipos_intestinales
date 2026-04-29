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

    [Header("Conexiones Simulator")]
    public EndoscopioCurvas endoscopio;
    public SistemaHerramientas herramientas;
    public MonitorEndoscopiaUI monitorUI;

    private bool esperandoCondicion = false;
    public GameObject objTextoPresionar;

    void Start()
    {
        if (ManejadorPartida.dificultad != 0)
        {
            gameObject.SetActive(false);
            return;
        }

        // El panel indicativo inicial de MonitorEndoscopiaUI se cierra y empezamos
        StartCoroutine(CicloTutorial());
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
        // Fin del Tutorial
        txtInstrucciones.text = "Tutorial Completado. Regrese a la zona de extracción para finalizar.";
        mascara.Desactivar();
    }

    IEnumerator EvaluarCondicion(PasoTutorial p)
    {
        bool usandoPC = (endoscopio != null) ? !endoscopio.usarControlHardware : true;
        DatosProcesados hw = ConfigManager.instancia.datosActuales;

        switch (p.espera)
        {
            case PasoTutorial.TipoEspera.Tiempo:
                yield return new WaitForSeconds(p.tiempoSegundos);
                break;

            case PasoTutorial.TipoEspera.AccionInput:
                bool cumplido = false;
                while (!cumplido)
                {
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
                break;

            case PasoTutorial.TipoEspera.PolipoEliminado:
                int inicial = herramientas.ObtenerTotalEliminados();
                // Espera hasta que el número de pólipos eliminados suba
                yield return new WaitUntil(() => herramientas.ObtenerTotalEliminados() > inicial);
                break;

            case PasoTutorial.TipoEspera.LlegarAZona:
                // Espera a que el endoscopio esté en la zona de extracción (la variable que ya creaste)
                yield return new WaitUntil(() => herramientas.enZonaExtraccion);
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