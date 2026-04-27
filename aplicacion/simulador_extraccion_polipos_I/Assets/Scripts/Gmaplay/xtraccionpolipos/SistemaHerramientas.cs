using UnityEngine;
using System.Collections;

public class SistemaHerramientas : MonoBehaviour
{
    [Header("Referencias Anatómicas")]
    [Tooltip("El objeto vacío que representa el agujero por donde salen las pinzas")]
    public Transform canalDeTrabajo;
    public GameObject pinzaDientes;
    public GameObject pinzaAsas;

    [Header("Configuración de Interacción")]
    public float distanciaAccion = 0.2f;
    public float anguloTolerancia = 35f;
    public LayerMask capaPolipos;

    [Header("Estado del Sistema")]
    public bool estaCortando = false;
    public bool llevandoPolipo = false;
    public int poliposEliminados = 0;

    private PolipoInteractuable polipoEnMira;
    private Vector3 posInicialPunta;
    private Quaternion rotInicialPunta;

    void Start()
    {
        pinzaDientes.SetActive(false);
        pinzaAsas.SetActive(false);

        if (canalDeTrabajo == null)
        {
            Debug.LogError("<color=red>¡ATENCIÓN! Asigna el 'CanalDeTrabajo' en el Inspector.</color>");
            canalDeTrabajo = this.transform;
        }
    }

    void Update()
    {
        // 1. EL LÁSER AHORA SALE DESDE EL CANAL DE TRABAJO (Usando su flecha azul / Forward)
        Vector3 origenRayo = canalDeTrabajo.position;
        Vector3 direccionRayo = canalDeTrabajo.forward;

        // --- DEBUG VISUAL CONSTANTE ---
        if (Physics.Raycast(origenRayo, direccionRayo, out RaycastHit hitDebug, distanciaAccion))
        {
            if (((1 << hitDebug.collider.gameObject.layer) & capaPolipos) != 0)
            {
                Debug.DrawRay(origenRayo, direccionRayo * hitDebug.distance, Color.green);
                if (polipoEnMira == null)
                {
                    Debug.Log($"<color=green>[SCANNER]: Pólipo detectado desde el canal a {hitDebug.distance:F3}m.</color>");
                }
            }
            else
            {
                Debug.DrawRay(origenRayo, direccionRayo * hitDebug.distance, Color.yellow);
            }
        }
        else
        {
            Debug.DrawRay(origenRayo, direccionRayo * distanciaAccion, Color.red);
        }

        // --- LÓGICA DE INTERACCIÓN ---
        if (estaCortando) { VerificarMovimientoProhibido(); return; }

        if (Physics.Raycast(origenRayo, direccionRayo, out RaycastHit hit, distanciaAccion, capaPolipos))
        {
            polipoEnMira = hit.collider.GetComponent<PolipoInteractuable>();

            if (polipoEnMira != null && !polipoEnMira.yaCortado)
            {
                float anguloAtaque = Vector3.Angle(direccionRayo, -hit.normal);

                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2))
                {
                    Debug.Log($"<color=white>--- INTENTO DE CORTE ---</color>\nDistancia: {hit.distance:F3} | Ángulo: {anguloAtaque:F1}°");

                    if (llevandoPolipo)
                    {
                        Debug.LogWarning("Enfermera: Doctor, suelte el pólipo anterior en la salida primero.");
                        return;
                    }

                    if (anguloAtaque > anguloTolerancia)
                    {
                        Debug.LogWarning($"<color=orange>[Fallo]: Ángulo incorrecto ({anguloAtaque:F1}°). Alinee el canal de trabajo con la base del pólipo.</color>");
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

    private void ProcesarCorte(bool esPinza)
    {
        if (esPinza)
        {
            if (polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada1 || polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada2)
                StartCoroutine(RutinaCortePinzaFria());
            else
                Debug.LogError("Enfermera: Pinzas incorrectas para este tamaño.");
        }
        else
        {
            if (polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada3 || polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada4)
                StartCoroutine(RutinaCorteAsaCaliente());
            else
                Debug.LogError("Enfermera: El asa es demasiado grande para este pólipo plano.");
        }
    }

    private IEnumerator RutinaCortePinzaFria()
    {
        IniciarCorteSeguro();
        pinzaDientes.SetActive(true);
        yield return new WaitForSeconds(1.5f);

        if (estaCortando)
        {
            polipoEnMira.SerCortado(canalDeTrabajo); // Se vincula al canal de trabajo
            poliposEliminados++;
        }

        pinzaDientes.SetActive(false);
        estaCortando = false;
    }

    private IEnumerator RutinaCorteAsaCaliente()
    {
        IniciarCorteSeguro();
        pinzaAsas.SetActive(true);
        yield return new WaitForSeconds(2.5f);

        if (estaCortando)
        {
            polipoEnMira.SerCortado(canalDeTrabajo); // El pólipo extraído se succiona al canal de trabajo
            llevandoPolipo = true;
        }

        pinzaAsas.SetActive(false);
        estaCortando = false;
    }

    private void IniciarCorteSeguro()
    {
        estaCortando = true;
        posInicialPunta = transform.position;
        rotInicialPunta = transform.rotation;
    }

    private void VerificarMovimientoProhibido()
    {
        if (Vector3.Distance(transform.position, posInicialPunta) > 0.005f || Quaternion.Angle(transform.rotation, rotInicialPunta) > 3f)
        {
            Debug.LogWarning("<color=red>CORTE ABORTADO: El endoscopio se movió.</color>");
            estaCortando = false;
            pinzaDientes.SetActive(false);
            pinzaAsas.SetActive(false);
        }
    }
}