using UnityEngine;
using System.Collections;

public class SistemaHerramientas : MonoBehaviour
{
    [Header("Referencias Anatómicas")]
    public Transform canalDeTrabajo;

    [Header("Herramienta: Pinza Biopsia (Yamada 1 y 2)")]
    public GameObject pinzaDientes;
    public Transform pinzaDerecha;
    public Transform pinzaIzquierda;
    public float anguloAperturaPinza = 45f;

    [Header("Herramienta: Asa Diatérmica (Yamada 3 y 4)")]
    public GameObject pinzaAsas;
    public Transform lazoBezier;
    public Vector3 escalaLazoCerrado = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("Configuración de Interacción")]
    public float distanciaAccion = 0.2f;
    public float anguloTolerancia = 35f;
    public float distanciaExtensionHerramienta = 0.05f;
    public LayerMask capaPolipos;

    [Header("Estado del Sistema")]
    public bool estaCortando = false;
    public bool llevandoPolipo = false;

    // --- NUEVO: Array para contar cada Yamada por separado ---
    // Indice 0 = Y1, Indice 1 = Y2, Indice 2 = Y3, Indice 3 = Y4
    [HideInInspector]
    public int[] yamadasEliminados = new int[4] { 0, 0, 0, 0 };

    private PolipoInteractuable polipoEnMira;
    private Vector3 posInicialPunta;
    private Quaternion rotInicialPunta;

    // Referencia opcional para enviar mensajes de error directo a la UI
    private MonitorEndoscopiaUI monitorUI;

    void Start()
    {
        pinzaDientes.SetActive(false);
        pinzaAsas.SetActive(false);
        monitorUI = FindObjectOfType<MonitorEndoscopiaUI>();
    }

    void Update()
    {
        Vector3 origenRayo = canalDeTrabajo.position;
        Vector3 direccionRayo = canalDeTrabajo.forward;

        if (Input.GetKeyDown(KeyCode.Alpha5)) IntentarSuccion(origenRayo, direccionRayo);

        if (estaCortando) { VerificarMovimientoProhibido(); return; }

        if (Physics.Raycast(origenRayo, direccionRayo, out RaycastHit hit, distanciaAccion, capaPolipos))
        {
            polipoEnMira = hit.collider.GetComponent<PolipoInteractuable>();

            if (polipoEnMira != null && polipoEnMira.estadoActual == PolipoInteractuable.EstadoPolipo.Intacto)
            {
                float anguloAtaque = Vector3.Angle(direccionRayo, -hit.normal);

                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2))
                {
                    if (llevandoPolipo)
                    {
                        EnviarErrorUI("Enfermera: Doctor, suelte el pólipo anterior en la salida primero.", 2f);
                        return;
                    }
                    if (anguloAtaque > anguloTolerancia)
                    {
                        EnviarErrorUI($"Ángulo incorrecto ({anguloAtaque:F1}°). Alinee el canal de trabajo.", 1f);
                        return;
                    }

                    if (Input.GetKeyDown(KeyCode.Alpha1)) ProcesarCorte(true);
                    else ProcesarCorte(false);
                }
            }
        }
        else
        {
            polipoEnMira = null;
        }
    }

    private void IntentarSuccion(Vector3 origen, Vector3 direccion)
    {
        if (llevandoPolipo) return;

        if (Physics.Raycast(origen, direccion, out RaycastHit hit, distanciaAccion * 1.5f, capaPolipos))
        {
            PolipoInteractuable polipoTocado = hit.collider.GetComponent<PolipoInteractuable>();

            if (polipoTocado != null && polipoTocado.estadoActual == PolipoInteractuable.EstadoPolipo.CortadoSuelto)
            {
                StartCoroutine(RutinaSuccion(polipoTocado));
            }
            else if (polipoTocado != null && polipoTocado.estadoActual == PolipoInteractuable.EstadoPolipo.Intacto)
            {
                EnviarErrorUI("Enfermera: No podemos succionar un pólipo que no ha sido cortado.", 1f);
            }
        }
    }

    private IEnumerator RutinaSuccion(PolipoInteractuable polipo)
    {
        float tiempo = 0;
        Vector3 posInicial = polipo.transform.position;

        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime * 5f;
            polipo.transform.position = Vector3.Lerp(posInicial, canalDeTrabajo.position, tiempo);
            yield return null;
        }

        polipo.SerSuccionado(canalDeTrabajo);
        llevandoPolipo = true;
    }

    private void ProcesarCorte(bool esPinza)
    {
        if (esPinza)
        {
            if (polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada1 || polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada2)
                StartCoroutine(AnimacionPinzaFria());
            else
                EnviarErrorUI("Enfermera: Pinzas incorrectas para este tamaño.", 3f);
        }
        else
        {
            if (polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada3 || polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada4)
                StartCoroutine(AnimacionAsaCaliente());
            else
                EnviarErrorUI("Enfermera: El asa es demasiado grande para este pólipo plano.", 3f);
        }
    }

    private IEnumerator AnimacionPinzaFria()
    {
        IniciarCorteSeguro();
        pinzaDientes.SetActive(true);

        pinzaDientes.transform.localPosition = Vector3.zero;
        Vector3 posExtendida = new Vector3(0, 0, distanciaExtensionHerramienta);

        yield return MoverHerramienta(pinzaDientes.transform, Vector3.zero, posExtendida, 0.5f);
        if (!estaCortando) yield break;

        yield return RotarPinzas(0, anguloAperturaPinza, 0.3f);
        if (!estaCortando) yield break;

        yield return RotarPinzas(anguloAperturaPinza, 0, 0.2f);
        if (!estaCortando) yield break;

        polipoEnMira.ProcesarCorte();

        // Sumamos directamente porque los pólipos pequeños desaparecen al instante
        SumarPolipoEliminado(polipoEnMira.tipo);

        yield return MoverHerramienta(pinzaDientes.transform, posExtendida, Vector3.zero, 0.5f);

        TerminarCorteSeguro();
    }

    private IEnumerator AnimacionAsaCaliente()
    {
        IniciarCorteSeguro();
        pinzaAsas.SetActive(true);

        pinzaAsas.transform.localPosition = Vector3.zero;
        Vector3 posExtendida = new Vector3(0, 0, distanciaExtensionHerramienta);
        Vector3 escalaOriginalLazo = lazoBezier.localScale;

        yield return MoverHerramienta(pinzaAsas.transform, Vector3.zero, posExtendida, 0.8f);
        if (!estaCortando) yield break;

        yield return EscalarLazo(escalaOriginalLazo, escalaLazoCerrado, 1.0f);
        if (!estaCortando) yield break;

        yield return new WaitForSeconds(1.0f);
        if (!estaCortando) yield break;

        polipoEnMira.ProcesarCorte();

        yield return MoverHerramienta(pinzaAsas.transform, posExtendida, Vector3.zero, 0.5f);
        lazoBezier.localScale = escalaOriginalLazo;

        TerminarCorteSeguro();
    }

    // NUEVA FUNCIÓN PARA LLEVAR EL CONTEO
    public void SumarPolipoEliminado(PolipoInteractuable.TipoPolipo tipo)
    {
        switch (tipo)
        {
            case PolipoInteractuable.TipoPolipo.Yamada1: yamadasEliminados[0]++; break;
            case PolipoInteractuable.TipoPolipo.Yamada2: yamadasEliminados[1]++; break;
            case PolipoInteractuable.TipoPolipo.Yamada3: yamadasEliminados[2]++; break;
            case PolipoInteractuable.TipoPolipo.Yamada4: yamadasEliminados[3]++; break;
        }
    }

    // NUEVA FUNCIÓN PARA TOTALES
    public int ObtenerTotalEliminados()
    {
        return yamadasEliminados[0] + yamadasEliminados[1] + yamadasEliminados[2] + yamadasEliminados[3];
    }

    private IEnumerator MoverHerramienta(Transform obj, Vector3 inicio, Vector3 fin, float duracion)
    {
        float tiempo = 0;
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime / duracion;
            obj.localPosition = Vector3.Lerp(inicio, fin, tiempo);
            yield return null;
        }
    }

    private IEnumerator RotarPinzas(float anguloInicio, float anguloFin, float duracion)
    {
        float tiempo = 0;
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime / duracion;
            float anguloActual = Mathf.Lerp(anguloInicio, anguloFin, tiempo);

            pinzaDerecha.localRotation = Quaternion.Euler(anguloActual, 0, 0);
            pinzaIzquierda.localRotation = Quaternion.Euler(-anguloActual, 0, 0);

            yield return null;
        }
    }

    private IEnumerator EscalarLazo(Vector3 inicio, Vector3 fin, float duracion)
    {
        float tiempo = 0;
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime / duracion;
            lazoBezier.localScale = Vector3.Lerp(inicio, fin, tiempo);
            yield return null;
        }
    }

    private void IniciarCorteSeguro()
    {
        estaCortando = true;
        posInicialPunta = transform.position;
        rotInicialPunta = transform.rotation;
    }

    private void TerminarCorteSeguro()
    {
        estaCortando = false;
        pinzaDientes.SetActive(false);
        pinzaAsas.SetActive(false);
    }

    private void VerificarMovimientoProhibido()
    {
        if (Vector3.Distance(transform.position, posInicialPunta) > 0.005f || Quaternion.Angle(transform.rotation, rotInicialPunta) > 3f)
        {
            EnviarErrorUI("CORTE ABORTADO: El endoscopio se movió.", 5f);
            TerminarCorteSeguro();
            if (lazoBezier != null) lazoBezier.localScale = Vector3.one;
        }
    }

    private void EnviarErrorUI(string mensaje, float puntosRestados)
    {
        Debug.LogWarning(mensaje);
        if (monitorUI != null) monitorUI.RegistrarError(mensaje, puntosRestados);
    }
}