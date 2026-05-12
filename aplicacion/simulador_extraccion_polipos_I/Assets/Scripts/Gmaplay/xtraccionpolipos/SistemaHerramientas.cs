using UnityEngine;
using System.Collections;

public class SistemaHerramientas : MonoBehaviour
{
    [Header("Referencias Anatómicas")]
    public Transform canalDeTrabajo;
    public EndoscopioCurvas endoscopio;
    public Camera camaraPrincipal;

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
    public float anguloToleranciaFoto = 25f;
    public float distanciaExtensionHerramienta = 0.05f;
    public LayerMask capaPolipos;

    [Header("Estado del Sistema")]
    public bool estaCortando = false;
    public bool llevandoPolipo = false;
    public bool estaCongelado = false;

    // Sensor de zona de extracción
    [HideInInspector]
    public bool enZonaExtraccion = false;

    private bool estaEnZoom = false;
    private float fovOriginal;

    private bool enModoSeleccion = false;

    [HideInInspector]
    public int[] yamadasEliminados = new int[4] { 0, 0, 0, 0 };

    public PolipoInteractuable polipoEnMira;
    private Vector3 posInicialPunta;
    private Quaternion rotInicialPunta;
    private MonitorEndoscopiaUI monitorUI;

    // Memoria para Higiene
    public PolipoInteractuable ultimoPolipoCortado;

    // Contadores de movimientos para higiene
    private int movimientosSinSuccionar = 0;
    private bool endoscopioEstabaMoviendo = false;
    private int movimientosEnSeleccion = 0; // NUEVO: Para cancelar el menú si se mueve

    private DatosProcesados datosHardware;
    private bool ultimoF, ultimoC, ultimoZ, ultimoS, ultimoA, ultimoL;
    [Header("Fin de Procedimiento")]
    public bool enModoConfirmarSalida = false;
    public bool EstaEnModoSeleccion() { return enModoSeleccion; }
    public PolipoInteractuable ObtenerPolipoEnMira() { return polipoEnMira; }
    public PolipoInteractuable ObtenerUltimoPolipoCortado() { return ultimoPolipoCortado; }
    void Start()
    {
        if (ManejadorPartida.dificultad == 0)
        {
            anguloTolerancia = 180f;
            anguloToleranciaFoto = 180f;
            // Detiene la ejecución del script aquí
        }
        pinzaDientes.SetActive(false);
        pinzaAsas.SetActive(false);
        monitorUI = FindObjectOfType<MonitorEndoscopiaUI>();

        if (camaraPrincipal == null) camaraPrincipal = Camera.main;
        if (camaraPrincipal != null) fovOriginal = camaraPrincipal.fieldOfView;
    }

    void OnEnable()
    {
        if (ConfigManager.instancia != null)
            ConfigManager.instancia.AlRecibirDatosProcesados += ActualizarDatosHardware;
    }

    void OnDisable()
    {
        if (ConfigManager.instancia != null)
            ConfigManager.instancia.AlRecibirDatosProcesados -= ActualizarDatosHardware;
    }

    private void ActualizarDatosHardware(DatosProcesados datos)
    {
        datosHardware = datos;
    }

    void Update()
    {
        bool btnFreeze = false, btnCapture = false, btnZoom = false, btnSuccion = false, btnAccion = false, btnLimpiado = false;
        bool moviendo = false; // Sensor local de movimiento

        bool modoPC = false;
        if (endoscopio != null)
            modoPC = !endoscopio.usarControlHardware;
        else
            modoPC = true;
        
        if (modoPC)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) { btnFreeze = true; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { btnCapture = true; }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { btnZoom = true; }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { btnSuccion = true; }
            if (Input.GetKeyDown(KeyCode.Alpha5)) { btnAccion = true; }
            if (Input.GetKeyDown(KeyCode.Alpha6)) { btnLimpiado = true; }

            // Verifica si está tocando teclas de movimiento
            moviendo = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
        }
        else
        {
            if (datosHardware != null)
            {
                btnFreeze = (datosHardware.botonFreeze && !ultimoF);
                btnCapture = (datosHardware.botonCapture && !ultimoC);
                btnZoom = (datosHardware.botonZoom && !ultimoZ);
                btnSuccion = (datosHardware.botonSuccion && !ultimoS);
                btnAccion = (datosHardware.botonAccion && !ultimoA);
                btnLimpiado = (datosHardware.botonLimpiado && !ultimoL);

                // Verifica si los valores analógicos del hardware superan el reposo
                moviendo = Mathf.Abs(datosHardware.insercionFinal) > 0.05f || Mathf.Abs(datosHardware.volanteXFinal) > 0.05f || Mathf.Abs(datosHardware.volanteYFinal) > 0.05f;
                ultimoF = datosHardware.botonFreeze;
                ultimoC = datosHardware.botonCapture;
                ultimoZ = datosHardware.botonZoom;
                ultimoS = datosHardware.botonSuccion;
                ultimoA = datosHardware.botonAccion;
                ultimoL = datosHardware.botonLimpiado;
            }
        }

        Vector3 origenRayo = canalDeTrabajo.position;
        Vector3 direccionRayo = canalDeTrabajo.forward;
        bool bloqueadoPorTutorial = (TutorialManager.instancia != null && TutorialManager.instancia.controlesBloqueados);
        if (bloqueadoPorTutorial)
        {
            btnFreeze = false;
            btnCapture = false;
            btnZoom = false;
            btnSuccion = false;
            btnAccion = false;
            btnLimpiado = false;
            moviendo = false;
        }
        if (TutorialManager.instancia != null && ManejadorPartida.dificultad == 0)
        {
            string filtro = TutorialManager.instancia.accionEsperadaActiva;
            if (!string.IsNullOrEmpty(filtro))
            {
                // Apagamos implacablemente cualquier botón que no sea el que el tutorial pidió
                if (filtro != "Freeze") btnFreeze = false;
                if (filtro != "Capture") btnCapture = false;
                if (filtro != "Accion") btnAccion = false;
                if (filtro != "Succion") btnSuccion = false;
                if (filtro != "Zoom") btnZoom = false;
                if (filtro != "Limpiado") btnLimpiado = false;
            }
        }
        // LÓGICA DE HIGIENE: 3 MOVIMIENTOS
        if (ultimoPolipoCortado != null && ultimoPolipoCortado.estadoActual == PolipoInteractuable.EstadoPolipo.CortadoSuelto)
        {
            if (moviendo && !endoscopioEstabaMoviendo)
            {
                movimientosSinSuccionar++;
            }

            if (movimientosSinSuccionar >= 3)
            {
                float distanciaAlResto = Vector3.Distance(origenRayo, ultimoPolipoCortado.transform.position);
                if (distanciaAlResto > 0.3f)
                {
                    EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 9, "Mala higiene visual: Abandonó el área dejando tejido suelto sin succionar.");
                    ultimoPolipoCortado = null; // Detiene el castigo
                    movimientosSinSuccionar = 0;
                }
            }
        }

        // CANCELAR SELECCIÓN SI SE MUEVE
        if (enModoSeleccion)
        {
            if (moviendo && !endoscopioEstabaMoviendo)
            {
                movimientosEnSeleccion++;
            }

            if (movimientosEnSeleccion > 1) // Si hace más de 1 movimiento, se sale del menú
            {
                EnviarInfoUI("Selección de herramienta cancelada por movimiento del endoscopio.", "#FF8C00");
                ActivarModoSeleccion(false);
            }
        }
        else
        {
            movimientosEnSeleccion = 0; // Se resetea cuando no estamos en el menú
        }

        endoscopioEstabaMoviendo = moviendo;
        // ----------------------------------------------------

        //BLOQUEO DE FREEZE INTACTO QUE TÚ PROGRAMASTE
        if (estaCongelado)
        {
            if (btnAccion || btnZoom || btnSuccion)
            {
                EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Seguridad, 2, "Operación a ciegas: Intentó usar herramientas con la imagen congelada.");
            }

            if (btnCapture) EjecutarCapture();
            if (btnFreeze) EjecutarFreeze();
            if (btnLimpiado) EjecutarLimpiado();
            return;
        }

        if (!enModoSeleccion)
        {
            if (btnZoom && camaraPrincipal != null) EjecutarZoom();
            if (btnFreeze) EjecutarFreeze();
            if (btnCapture) EjecutarCapture();
            if (btnLimpiado) EjecutarLimpiado();

            // --- LÓGICA DE SUCCIÓN MANUAL Y CONTAMINACIÓN ---
            if (btnSuccion)
            {
                if (llevandoPolipo)
                {
                    if (enZonaExtraccion)
                    {
                        SoltarPolipoEnLaboratorio();
                    }
                    else
                    {
                        // Solo penalizamos, pero NO cambiamos llevandoPolipo a false 
                        EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 9, "Contaminación: Intentó apagar la succión dentro del paciente. Manténgala encendida hasta salir.");
                    }
                }
                else
                {
                    IntentarSuccion(origenRayo, direccionRayo);
                }
            }
        }

        if (estaCortando) { VerificarMovimientoProhibido(); return; }

        if (Physics.Raycast(origenRayo, direccionRayo, out RaycastHit hit, distanciaAccion, capaPolipos))
        {
            polipoEnMira = hit.collider.GetComponent<PolipoInteractuable>();

            if (polipoEnMira != null && polipoEnMira.estadoActual == PolipoInteractuable.EstadoPolipo.Intacto)
            {
                float anguloAtaque = Vector3.Angle(direccionRayo, -hit.normal);

                if (!enModoSeleccion)
                {
                    if (btnAccion)
                    {
                        if (!polipoEnMira.fueFotografiado)
                        {
                            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 4, "Omisión de Protocolo: Debe congelar (Freeze) y documentar (Capture) antes de intervenir.");
                            return;
                        }

                        if (llevandoPolipo)
                        {
                            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 8, "Suelte el pólipo anterior en la salida primero.");
                            return;
                        }
                        if (anguloAtaque > anguloTolerancia)
                        {
                            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 8, $"Ángulo incorrecto ({anguloAtaque:F1}°). Alinee el canal de trabajo.");
                            return;
                        }

                        ActivarModoSeleccion(true);
                    }
                }
                else
                {
                    if (btnAccion)
                    {
                        ActivarModoSeleccion(false);
                    }
                    else if (btnFreeze)
                    {
                        ProcesarCorteManual(true);
                    }
                    else if (btnCapture)
                    {
                        ProcesarCorteManual(false);
                    }
                }
            }
        }
        else
        {
            polipoEnMira = null;
            if (enModoSeleccion) ActivarModoSeleccion(false);
        }

        if (enModoConfirmarSalida)
        {
            // Si el médico mueve el endoscopio, cancelamos la salida por precaución
            if (moviendo && !endoscopioEstabaMoviendo)
            {
                if (monitorUI != null) monitorUI.BotonConfirmarNo();
                EnviarInfoUI("Confirmación cancelada por movimiento.", "#FF8C00");
            }
            else if (btnFreeze) // Botón 1 = SÍ (Aceptar)
            {
                if (monitorUI != null) monitorUI.BotonConfirmarSi();
            }
            else if (btnCapture || btnAccion) // Botón 2 o Accion = NO (Cancelar)
            {
                if (monitorUI != null) monitorUI.BotonConfirmarNo();
            }

            endoscopioEstabaMoviendo = moviendo;
            return; // Evita que se activen otras herramientas mientras el popup esté abierto
        }

        // Y para invocarlo cuando esté en la zona:
        if (enZonaExtraccion && btnAccion && polipoEnMira == null && !enModoSeleccion && !llevandoPolipo)
        {
            ActivarModoSalida(true);
            return;
        }

    }

    //FUNCIÓN DE LAVADO DE LENTE
    private void EjecutarLimpiado()
    {
        EnviarInfoUI("Lavado de lente activado. Campo visual irrigado.", "#00FFFF");

    }

    private void ActivarModoSeleccion(bool activar)
    {
        enModoSeleccion = activar;
        if (monitorUI != null) monitorUI.ActualizarTextosBotones(activar);

        if (activar) EnviarInfoUI("Modo Herramientas: Seleccione Pinza (1) o Asa (2)", "#FFFFFF");
        else EnviarInfoUI("Modo Herramientas Cancelado", "#888888");
    }

    private void ProcesarCorteManual(bool esPinza)
    {
        ActivarModoSeleccion(false);

        if (estaCongelado) EjecutarFreeze();

        if (esPinza)
        {
            EnviarInfoUI("Preparando Pinza de Biopsia...", "#FFFF00");
            if (polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada1 || polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada2)
            {
                StartCoroutine(AnimacionPinzaFria());
            }
            else
            {
                EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 6, "Regla de Yamada violada: Se requiere Asa Diatérmica para pólipos grandes/pediculados.");
            }
        }
        else
        {
            EnviarInfoUI("Preparando Asa Diatérmica...", "#FF4500");
            if (polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada3 || polipoEnMira.tipo == PolipoInteractuable.TipoPolipo.Yamada4)
            {
                StartCoroutine(AnimacionAsaCaliente());
            }
            else
            {
                EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 6, "Regla de Yamada violada: El asa es excesiva y riesgosa para pólipos planos.");
            }
        }
    }

    private void EjecutarZoom()
    {
        estaEnZoom = !estaEnZoom;
        camaraPrincipal.fieldOfView = estaEnZoom ? (fovOriginal / 1.6f) : fovOriginal;
        EnviarInfoUI(estaEnZoom ? "Zoom Óptico Activado" : "Zoom Óptico Desactivado", "#FF8C00");
    }

    private void EjecutarFreeze()
    {
        estaCongelado = !estaCongelado;
        if (estaCongelado)
        {
            Time.timeScale = 0.0001f;
            EnviarInfoUI("Imagen Congelada (Freeze)", "#00FFFF");
        }
        else
        {
            Time.timeScale = 1f;
            EnviarInfoUI("Imagen Descongelada", "#00FFFF");
        }
    }

    private void EjecutarCapture()
    {
        if (estaCongelado)
        {
            EnviarInfoUI("Fotografía guardada en expediente del paciente.", "#FFD700");

            if (polipoEnMira != null && polipoEnMira.estadoActual == PolipoInteractuable.EstadoPolipo.Intacto)
            {
                Vector3 direccionAlPolipo = (polipoEnMira.transform.position - canalDeTrabajo.position).normalized;
                float anguloCentrado = Vector3.Angle(canalDeTrabajo.forward, direccionAlPolipo);

                if (anguloCentrado > anguloToleranciaFoto)
                {
                    EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 5, $"Calidad Fotográfica Deficiente: Pólipo descentrado ({anguloCentrado:F1}°).");
                }
                else
                {
                    EnviarInfoUI($"Calidad de Foto Óptima (Ángulo: {anguloCentrado:F1}°)", "#32CD32");
                }

                polipoEnMira.fueFotografiado = true;
            }
        }
        else
        {
            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 4, "No se puede capturar foto en movimiento. Congele (Freeze) la imagen primero.");
        }
    }

    private void IntentarSuccion(Vector3 origen, Vector3 direccion)
    {
        if (Physics.Raycast(origen, direccion, out RaycastHit hit, distanciaAccion * 1.5f, capaPolipos))
        {
            PolipoInteractuable polipoTocado = hit.collider.GetComponent<PolipoInteractuable>();

            if (polipoTocado != null && polipoTocado.estadoActual == PolipoInteractuable.EstadoPolipo.CortadoSuelto)
            {
                StartCoroutine(RutinaSuccion(polipoTocado));

                // Limpia la higiene al succionar
                if (ultimoPolipoCortado == polipoTocado)
                {
                    ultimoPolipoCortado = null;
                    movimientosSinSuccionar = 0;
                }
            }
            else if (polipoTocado != null && polipoTocado.estadoActual == PolipoInteractuable.EstadoPolipo.Intacto)
            {
                EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 9, "Enfermera: No podemos succionar un pólipo que no ha sido cortado.");
            }
        }
    }

    private IEnumerator RutinaSuccion(PolipoInteractuable polipo)
    {
        float tiempo = 0;
        Vector3 posInicial = polipo.transform.position;

        while (tiempo < 1f)
        {
            tiempo += Time.unscaledDeltaTime * 5f;
            polipo.transform.position = Vector3.Lerp(posInicial, canalDeTrabajo.position, tiempo);
            yield return null;
        }

        polipo.SerSuccionado(canalDeTrabajo);
        llevandoPolipo = true;
        EnviarInfoUI("Pólipo succionado. Proceda a retirarlo del paciente.", "#1E90FF");
    }

    private void SoltarPolipoEnLaboratorio()
    {
        llevandoPolipo = false;
        RefrescarUIBotones();
        int poliposBorrados = 0;

        foreach (Transform hijo in canalDeTrabajo)
        {
            PolipoInteractuable polipo = hijo.GetComponent<PolipoInteractuable>();
            if (polipo != null)
            {
                SumarPolipoEliminado(polipo.tipo);
                Destroy(hijo.gameObject);
                poliposBorrados++;
            }
        }
        EnviarInfoUI($"Extracción Exitosa. {poliposBorrados} muestra(s) depositada(s) en laboratorio.", "#32CD32");
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

        ultimoPolipoCortado = polipoEnMira;
        movimientosSinSuccionar = 0;

        SumarPolipoEliminado(polipoEnMira.tipo);
        EnviarInfoUI($"Pólipo {polipoEnMira.tipo} extraído con éxito.", "#00FF00");

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

        ultimoPolipoCortado = polipoEnMira;
        movimientosSinSuccionar = 0;

        EnviarInfoUI($"Pólipo {polipoEnMira.tipo} seccionado. Proceda con la succión.", "#00FF00");

        yield return MoverHerramienta(pinzaAsas.transform, posExtendida, Vector3.zero, 0.5f);
        lazoBezier.localScale = escalaOriginalLazo;

        TerminarCorteSeguro();
    }

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

    public int ObtenerTotalEliminados()
    {
        return yamadasEliminados[0] + yamadasEliminados[1] + yamadasEliminados[2] + yamadasEliminados[3];
    }

    private IEnumerator MoverHerramienta(Transform obj, Vector3 inicio, Vector3 fin, float duracion)
    {
        float tiempo = 0;
        while (tiempo < 1f)
        {
            tiempo += Time.unscaledDeltaTime / duracion;
            obj.localPosition = Vector3.Lerp(inicio, fin, tiempo);
            yield return null;
        }
    }

    private IEnumerator RotarPinzas(float anguloInicio, float anguloFin, float duracion)
    {
        float tiempo = 0;
        while (tiempo < 1f)
        {
            tiempo += Time.unscaledDeltaTime / duracion;
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
            tiempo += Time.unscaledDeltaTime / duracion;
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
            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 7, "Corte Abortado: Pérdida de estabilidad del endoscopio durante intervención.");
            TerminarCorteSeguro();
            if (lazoBezier != null) lazoBezier.localScale = Vector3.one;
        }
    }

    private void EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion cat, int index, string mensaje)
    {
        Debug.LogWarning(mensaje);
        if (monitorUI != null) monitorUI.RegistrarErrorEstandarizado(cat, index, mensaje);
    }

    private void EnviarInfoUI(string mensaje, string colorHex)
    {
        Debug.Log($"<color={colorHex}>{mensaje}</color>");
        if (monitorUI != null) monitorUI.RegistrarAccionInfo(mensaje, colorHex);
    }
    public void ActivarModoSalida(bool activar)
    {
        enModoConfirmarSalida = activar;
        RefrescarUIBotones();
        if (activar)
        {
            EnviarInfoUI("¿FINALIZAR PROCEDIMIENTO?\n1 (Freeze): SÍ\n2 (Capture): NO", "#FFD700");
            if (monitorUI != null) monitorUI.MostrarPopupConfirmacion(MonitorEndoscopiaUI.TipoConfirmacion.FinalizarProcedimiento);
        }
        else
        {
            if (monitorUI != null) monitorUI.OcultarPopupConfirmacion();
        }
    }
    public void RefrescarUIBotones()
    {
        if (monitorUI != null)
        {
            // Solo habilitamos el texto rojo si está en la zona, NO está seleccionando herramienta,
            // NO está llevando un pólipo y NO está apuntando a un pólipo vivo.
            bool puedeSalir = enZonaExtraccion && !llevandoPolipo && !enModoSeleccion && polipoEnMira == null;

            monitorUI.ActualizarTextosBotones(enModoSeleccion, puedeSalir);
        }
    }
    
}