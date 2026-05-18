using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text;

public class MonitorEndoscopiaUI : MonoBehaviour
{
    [Header("Conexión con Físicas")]
    public EndoscopioCurvas endoscopio;
    public SistemaHerramientas herramientas;

    [Header("Panel Superior Izquierdo (Datos)")]
    public TextMeshProUGUI txtDatosDoctor;
    public TextMeshProUGUI txtReloj;

    [Header("Panel Medio (Telemetría)")]
    public TextMeshProUGUI txtProfundidad;
    public TextMeshProUGUI txtRespuestas;

    // TEXTO DE DAÑO 
    public TextMeshProUGUI txtDanioPaciente;

    [Header("Panel Azul (Configuración Botones)")]
    public TextMeshProUGUI txtListaBotones;

    [Header("Panel Morado (Polipos y Nota)")]
    public TextMeshProUGUI txtEstadisticasPolipos;
    public TextMeshProUGUI txtNotaEstudiante;

    [Header("Menú de Pausa y Alertas")]
    public GameObject panelPausa;
    public TextMeshProUGUI txtAlertaConexion;
    public Toggle chkModoComputadora;
    public Button btnContinuar;
    public Button btnMenuPrincipal;
    public Button btnReconectarHardware;

    [Header("Pantallas Extra")]
    public GameObject pantallaCargaNegra;
    public GameObject alertaBucleUI;
    private bool estaPausado = false;
    private SerialManager.EstadoConexion estadoHardwareAnterior = SerialManager.EstadoConexion.Iniciando;
    [Header("Guía Contextual")]
    public GameObject panelGuiaContextual;
    public TextMeshProUGUI txtGuiaContextual;
    public enum CategoriaEvaluacion { Seguridad, Protocolo, Tecnica }

    private float notaSeguridad = 100f;
    private float notaProtocolo = 100f;
    private float notaTecnica = 100f;
    [Header("Popups y Fin de Juego")]

    public GameObject panelConfirmarSalida; // Arrastra aquí un Panel oscuro con el texto "¿Desea finalizar la endoscopia?"
    public TextMeshProUGUI txtPreguntaConfirmacion;// Un texto dentro del panel para cambiar la pregunta según el caso
    // PANEL INDICATIVO (GAME OVER / AVISOS) ---
    public GameObject panelIndicativo;
    public TextMeshProUGUI txtTituloIndicativo;
    public TextMeshProUGUI txtTextoIndicativo;
    public Button btnContinuarIndicativo;

    // Variable para recordar si la partida terminó por un error crítico
    private string motivoGameOverCritico = "";
    public enum TipoConfirmacion { Ninguno, FinalizarProcedimiento, SalirAlMenu }
    private TipoConfirmacion contextoConfirmacion = TipoConfirmacion.Ninguno;

    private float profundidadMaximaAlcanzada = 0f;
    private const float META_PROFUNDIDAD = 150f;
    [Header("Reporte de Errores")]
    public GameObject panelResultadosFinales; // El panel que mostrará la nota final
    public TextMeshProUGUI txtResultadosFinales;
    public TextMeshProUGUI txtDetallePenalizaciones; // Arrastra aquí el texto dentro del ScrollView
    public Button btnFinalizarReporte; // Botón para volver al menú desde los resultados

    // Array para guardar cuántos puntos se perdieron por cada índice (0 al 9)
    private float[] puntosPerdidosTally = new float[10];

    // Nombres amigables para el reporte final
    private string[] nombresParametros = new string[] {
        "Trauma Tisular",            // Indice 0
        "Suavidad de Desplazamiento", // Indice 1
        "Exploración Visual",        // Indice 2
        "Seguridad en Retirada",     // Indice 3
        "Documentación de Hallazgos",// Indice 4
        "Calidad de Captura",        // Indice 5
        "Regla de Yamada",           // Indice 6
        "Estabilidad de Abordaje",   // Indice 7
        "Alineación de Canal",       // Indice 8
        "Higiene / Contaminación"    // Indice 9
    };
    public ScrollRect scrollRespuestas;
    void Start()
    {
        panelPausa.SetActive(false);
        if (pantallaCargaNegra != null) pantallaCargaNegra.SetActive(false);
        // Apagamos la alerta al iniciar
        if (alertaBucleUI != null) alertaBucleUI.SetActive(false);

        if (panelConfirmarSalida != null) panelConfirmarSalida.SetActive(false);
        if (panelResultadosFinales != null) panelResultadosFinales.SetActive(false);
        // Configuramos el nuevo panel
        if (panelIndicativo != null) panelIndicativo.SetActive(false);
        if (btnContinuarIndicativo != null)
        {
            btnContinuarIndicativo.onClick.RemoveAllListeners();
            // Al presionar continuar, cierra el panel indicativo y lanza el reporte final
            btnContinuarIndicativo.onClick.AddListener(() => {
                panelIndicativo.SetActive(false);
                if (!string.IsNullOrEmpty(motivoGameOverCritico))
                {
                    FinalizarSimulacion();
                }
                else
                {
                    // Si el motivo estaba vacío, significa que estaba cerrando el mensaje de INICIO
                    ReanudarJuego(); // Usamos tu método existente para devolver el timeScale a 1f
                }
            });
        }
        Time.timeScale = 1f;

        string nombreDoc = ManejadorPartida.nombreEstudiante != "" ? ManejadorPartida.nombreEstudiante : "NaN";
        txtDatosDoctor.text = $"Dr/a: {nombreDoc}\n\nDificultad: {ObtenerNombreDificultad()}";

        ActualizarTextosBotones(false);

        btnContinuar.onClick.RemoveAllListeners();
        btnContinuar.onClick.AddListener(ReanudarJuego);

        // CAMBIO AQUÍ: Ahora el botón del menú abre el popup, no carga la escena directo
        btnMenuPrincipal.onClick.RemoveAllListeners();
        btnMenuPrincipal.onClick.AddListener(() => MostrarPopupConfirmacion(TipoConfirmacion.SalirAlMenu));

        if (btnReconectarHardware != null)
        {
            btnReconectarHardware.onClick.RemoveAllListeners();
            btnReconectarHardware.onClick.AddListener(IntentarReconexionSTM32);
        }

        chkModoComputadora.onValueChanged.RemoveAllListeners();
        chkModoComputadora.onValueChanged.AddListener(CambiarModoControl);

        CambiarModoControl(chkModoComputadora.isOn);

        if (txtDanioPaciente != null) txtDanioPaciente.text = "Daño: 0%";
        if (btnFinalizarReporte != null)
        {
            btnFinalizarReporte.onClick.AddListener(IrAlMenuPrincipal);
        }

        MostrarMensajeDeInicio();
    }

    void Update()
    {
        float profundidadActualCm = endoscopio.distanciaTotalInsertada * 3.3f;
        txtReloj.text = DateTime.Now.ToString("dd/MM/yyyy\nHH:mm:ss");

        if (!estaPausado && endoscopio != null)
        {
            ActualizarTelemetria();
            //Si estamos en Tutorial (0) o Fácil (1), actualizamos la guía
            if (ManejadorPartida.dificultad == 0 || ManejadorPartida.dificultad == 1)
            {
                ActualizarGuiaContextual();
            }
            else if (panelGuiaContextual != null && panelGuiaContextual.activeSelf)
            {
                // En Normal o Realista, apagamos el panel
                panelGuiaContextual.SetActive(false);
            }
        }

        VigilarHardware();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (estaPausado) ReanudarJuego();
            else PausarJuego();
        }
        if (profundidadActualCm > profundidadMaximaAlcanzada)
        {
            profundidadMaximaAlcanzada = profundidadActualCm;
        }
    }

    private void ActualizarTelemetria()
    {
        int centimetros = Mathf.RoundToInt(endoscopio.distanciaTotalInsertada * 3.75f);
        txtProfundidad.text = $"Cm: {centimetros}";


        if (herramientas != null)
        {
            ActualizarPanelPolipos();

            // Forzamos al panel azul a actualizarse en tiempo real
            bool puedeSalir = herramientas.enZonaExtraccion && !herramientas.llevandoPolipo && !herramientas.EstaEnModoSeleccion() && herramientas.ObtenerPolipoEnMira() == null;
            ActualizarTextosBotones(herramientas.EstaEnModoSeleccion(), puedeSalir);

            if (ManejadorPartida.dificultad == 0 || ManejadorPartida.dificultad == 1)
            {
                txtNotaEstudiante.text =
                    $"Seguridad y Nav:\n<color={(notaSeguridad > 60 ? "white" : "red")}>{notaSeguridad:F1} / 100</color>\n\n" +
                    $"Protocolo y Diag:\n<color={(notaProtocolo > 60 ? "white" : "red")}>{notaProtocolo:F1} / 100</color>\n\n" +
                    $"Técnica Endoscópica:\n<color={(notaTecnica > 60 ? "white" : "red")}>{notaTecnica:F1} / 100</color>";
            }
            else
            {
                txtNotaEstudiante.text = "Evaluación en curso...";
            }
        }
    }

    // --- FUNCIÓN PARA ACTUALIZAR DAÑO ---
    public void MostrarDanio(int porcentaje, string mensaje)
    {
        if (txtDanioPaciente != null)
        {
            if (porcentaje > 0)
                txtDanioPaciente.text = $"<color=white> {porcentaje}%</color>";
            else
                txtDanioPaciente.text = "Daño: 0%";
        }
    }

    private void ActualizarPanelPolipos()
    {
        int totalExtraidos = herramientas.ObtenerTotalEliminados();
        int totalPedidos = ManejadorPartida.totalPolipos > 0 ? ManejadorPartida.totalPolipos : 0;
        string tTotal = totalPedidos > 0 ? totalPedidos.ToString() : "NaN";

        string y1Req = ManejadorPartida.yamada != null && ManejadorPartida.yamada.Length > 0 ? ManejadorPartida.yamada[0].ToString() : "NaN";
        string y2Req = ManejadorPartida.yamada != null && ManejadorPartida.yamada.Length > 1 ? ManejadorPartida.yamada[1].ToString() : "NaN";
        string y3Req = ManejadorPartida.yamada != null && ManejadorPartida.yamada.Length > 2 ? ManejadorPartida.yamada[2].ToString() : "NaN";
        string y4Req = ManejadorPartida.yamada != null && ManejadorPartida.yamada.Length > 3 ? ManejadorPartida.yamada[3].ToString() : "NaN";

        if (ManejadorPartida.dificultad == 0 || ManejadorPartida.dificultad == 1)
        {
            txtEstadisticasPolipos.text =
                $"Pólipos Totales:\n {totalExtraidos} / {tTotal}\n\n" +
                $"Y1: {herramientas.yamadasEliminados[0]} / {y1Req}\n" +
                $"Y2: {herramientas.yamadasEliminados[1]} / {y2Req}\n" +
                $"Y3: {herramientas.yamadasEliminados[2]} / {y3Req}\n" +
                $"Y4: {herramientas.yamadasEliminados[3]} / {y4Req}";
        }
        else if (ManejadorPartida.dificultad == 3)
        {
            int restantes = totalPedidos - totalExtraidos;
            string txtRestantes = restantes > 0 ? restantes.ToString() : "NaN";

            txtEstadisticasPolipos.text =
                $"Pólipos:\n" +
                $"Extraídos: {totalExtraidos}\n" +
                $"Restantes: {txtRestantes}";
        }
        else
        {
            txtEstadisticasPolipos.text =
                $"Pólipos:\n" +
                $"Extraídos: {totalExtraidos}\n";
        }
    }

    private void VigilarHardware()
    {
        if (SerialManager.instancia == null) return;

        SerialManager.EstadoConexion estadoActual = SerialManager.instancia.estadoActual;

        // Salto automático al menú de pausa

        if (estadoActual != estadoHardwareAnterior)
        {
            // Guardamos la memoria ANTES de accionar la pausa para evitar bucles de código
            SerialManager.EstadoConexion estadoViejo = estadoHardwareAnterior;
            estadoHardwareAnterior = estadoActual;

            if (estadoActual == SerialManager.EstadoConexion.Error && estadoViejo == SerialManager.EstadoConexion.Conectado)
            {
                if (!estaPausado)
                {
                    PausarJuego();
                }
            }
            // EL HARDWARE SE ACABA DE CONECTAR
            else if (estadoActual == SerialManager.EstadoConexion.Conectado)
            {
                // Apagamos el modo PC automáticamente para darle prioridad al hardware
                chkModoComputadora.isOn = false;
            }
        }


        //ACTUALIZACION VISUAL Y BLOQUEOS (Se ejecuta siempre)

        switch (estadoActual)
        {
            case SerialManager.EstadoConexion.Buscando:
                if (txtAlertaConexion != null)
                    txtAlertaConexion.text = $"<color=yellow>Buscando Hardware: {SerialManager.instancia.mensajeInterfaz}</color>";

                // Forzamos modo PC y lo bloqueamos
                if (!chkModoComputadora.isOn) chkModoComputadora.isOn = true;
                chkModoComputadora.interactable = false;
                break;

            case SerialManager.EstadoConexion.Error:
                if (txtAlertaConexion != null)
                    txtAlertaConexion.text = "<color=red>¡ERROR DE CONEXIÓN!</color>\nEl cable USB se ha desconectado.";

                if (!chkModoComputadora.isOn) chkModoComputadora.isOn = true;
                chkModoComputadora.interactable = false;
                break;

            case SerialManager.EstadoConexion.Conectado:
                if (txtAlertaConexion != null)
                    txtAlertaConexion.text = $"<color=green>Hardware STM32 Conectado ({SerialManager.instancia.puertoActivo})</color>\nModo PC desactivado automáticamente.";

                chkModoComputadora.interactable = true;
                break;

            case SerialManager.EstadoConexion.Iniciando:
            default:
                if (txtAlertaConexion != null)
                    txtAlertaConexion.text = "<color=white>Esperando conexión de dispositivo...</color>";

                if (!chkModoComputadora.isOn) chkModoComputadora.isOn = true;
                chkModoComputadora.interactable = false;
                break;
        }
    }


    public void ActualizarTextosBotones(bool enModoSeleccion, bool habilitarSalida = false)
    {
        if (ConfigManager.instancia != null)
        {
            ConfigManager cfg = ConfigManager.instancia;
            if (enModoSeleccion)
            {
                txtListaBotones.text =
                    $"<color=#FFD700>(Elija Herramienta)</color>\n" +
                    $"Pinza Biopsia [{cfg.mapFreeze}]\n" +
                    $"Asa de Polipectomía [{cfg.mapCapture}]\n" +
                    $"<color=#888888>---</color>\n" +
                    $"<color=#888888>---</color>\n" +
                    $"Cancelar [{cfg.mapAccion}]";
            }
            else
            {
                string textoAccionFinal = habilitarSalida
                ? $"<color=red><b>Finalizar Procedimiento [{cfg.mapAccion}]</b></color>"
                : $"Menú Herramientas [{cfg.mapAccion}]";

                // Texto dinámico para el botón 4
                string textoSuccion = "Succión"; // Nombre por defecto

                if (herramientas != null)
                {
                    // 1. Si el canal está tapado por un pólipo
                    if (herramientas.llevandoPolipo)
                    {
                        textoSuccion = "<color=#32CD32>Soltar Pólipo</color>";
                    }
                    // 2. Si hay hemorragia
                    else if (herramientas.ObtenerNivelSangrado() > 0f)
                    {
                        textoSuccion = "<color=#FF0000>Aspirar Sangre</color>";
                    }
                    // 3. Si hay agua de lavado
                    else if (herramientas.ObtenerLavadosSinSuccionar() > 0)
                    {
                        textoSuccion = "<color=#1E90FF>Aspirar Agua</color>";
                    }
                    // 4. Si hay un pólipo cortado esperando
                    else if (
                        (herramientas.ObtenerUltimoPolipoCortado() != null && herramientas.ObtenerUltimoPolipoCortado().estadoActual == PolipoInteractuable.EstadoPolipo.CortadoSuelto) ||
                        (herramientas.ObtenerPolipoEnMira() != null && herramientas.ObtenerPolipoEnMira().estadoActual == PolipoInteractuable.EstadoPolipo.CortadoSuelto)
                    )
                    {
                        textoSuccion = "<color=#FFD700>Atrapar Pólipo</color>";
                    }
                }

                txtListaBotones.text =
                    $"<color=#00FFFF>(Controles)</color>\n" +
                    $"Freeze [{cfg.mapFreeze}]\n" +
                    $"Capture [{cfg.mapCapture}]\n" +
                    $"Zoom [{cfg.mapZoom}]\n" +
                    $"Lavar Lente [{cfg.mapLimpiado}]\n" +
                    $"{textoSuccion} [{cfg.mapSuccion}]\n" +
                    textoAccionFinal;
            }
        }
    }

    public void PausarJuego()
    {
        estaPausado = true;
        Time.timeScale = 0f;
        panelPausa.SetActive(true);
        VigilarHardware();
    }

    public void ReanudarJuego()
    {
        estaPausado = false;
        if (herramientas != null && herramientas.estaCongelado) Time.timeScale = 0.0001f;
        else Time.timeScale = 1f;
        panelPausa.SetActive(false);
    }

    private void CambiarModoControl(bool usarPc)
    {
        if (endoscopio != null) endoscopio.usarControlHardware = !usarPc;
    }

    private void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void RegistrarErrorEstandarizado(CategoriaEvaluacion categoria, int indiceParametro, string mensajeFallo)
    {
        float puntosAQuitar = 1f;
        if (ManejadorPartida.penalizaciones != null && indiceParametro >= 0 && indiceParametro < ManejadorPartida.penalizaciones.Length)
            puntosAQuitar = ManejadorPartida.penalizaciones[indiceParametro];
        // Guardamos cuánto se perdió en este parámetro específico para el reporte final
        if (indiceParametro >= 0 && indiceParametro < puntosPerdidosTally.Length)
        {
            puntosPerdidosTally[indiceParametro] += puntosAQuitar;
        }
        switch (categoria)
        {
            case CategoriaEvaluacion.Seguridad:
                notaSeguridad = Mathf.Max(0, notaSeguridad - puntosAQuitar);
                break;
            case CategoriaEvaluacion.Protocolo:
                notaProtocolo = Mathf.Max(0, notaProtocolo - puntosAQuitar);
                break;
            case CategoriaEvaluacion.Tecnica:
                notaTecnica = Mathf.Max(0, notaTecnica - puntosAQuitar);
                break;
        }
        if (ManejadorPartida.dificultad != 3)
        {
            ImprimirMensajeConsola($"[-] {mensajeFallo} (-{puntosAQuitar} pts)", "red");
        }
        if (SerialManager.instancia != null && SerialManager.instancia.estadoActual == SerialManager.EstadoConexion.Conectado)
        {
            // HILO SECUNDARIO Y ANTI-COLAPSO DE BUFFER
            // Ejecutamos la comunicación USB por fuera del hilo principal de Unity
            // para evitar que el juego se congele y las físicas se bugueen.
            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    SerialManager.instancia.EnviarDato("V1:1000\n");

                    // Le damos 50 milisegundos a la placa STM32 para "masticar" el V1
                    // antes de lanzarle el V2, evitando que se asfixie el buffer.
                    System.Threading.Thread.Sleep(50);

                    SerialManager.instancia.EnviarDato("V2:1000\n");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Error en hilo de vibración: " + e.Message);
                }
            });
        }
    }

    private void ImprimirMensajeConsola(string lineaCompleta, string color)
    {
        // 1. Simplemente agregamos la nueva línea de texto
        txtRespuestas.text += $"\n<color={color}>{lineaCompleta}</color>";

        // 2. (Opcional) Llamamos a la corrutina de auto-scroll que hicimos antes
        // Si no implementaste la corrutina aún, puedes borrar este 'if'
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ForzarScrollHaciaAbajo());
        }
    }

    public void RegistrarAccionInfo(string mensajeInfo, string colorHex = "#00FFFF")
    {
        ImprimirMensajeConsola($"[+] {mensajeInfo}", colorHex);
    }

    private string ObtenerNombreDificultad()
    {
        switch (ManejadorPartida.dificultad)
        {
            case 0: return "Tutorial";
            case 1: return "Fácil";
            case 2: return "Normal";
            case 3: return "Realista";
            default: return "NaN";
        }
    }
    public void MostrarPopupSalida(bool mostrar)
    {
        if (panelConfirmarSalida != null)
            panelConfirmarSalida.SetActive(mostrar);
    }

    public void FinalizarSimulacion()
    {
        Time.timeScale = 0f;
        if (panelConfirmarSalida != null) panelConfirmarSalida.SetActive(false);
        if (panelResultadosFinales != null) panelResultadosFinales.SetActive(true);

        // --- CÁLCULO DE PENALIZACIÓN POR EXPLORACIÓN (ÍNDICE 2) ---
        float puntosPerdidosExploracion = 0f;
        float penalizacionBase = ManejadorPartida.penalizaciones[2];
        int poliposRestantes = ManejadorPartida.totalPolipos - herramientas.ObtenerTotalEliminados();

        // Condición A: Pólipos faltantes
        if (poliposRestantes > 0)
        {
            puntosPerdidosExploracion += (poliposRestantes * penalizacionBase);
            ImprimirMensajeConsola($"[-] Omisión: {poliposRestantes} pólipos no detectados.", "red");
        }

        // Condición B: Profundidad insuficiente (Meta 150cm)
        if (profundidadMaximaAlcanzada < META_PROFUNDIDAD)
        {
            float distanciaFaltante = META_PROFUNDIDAD - profundidadMaximaAlcanzada;
            int bloquesDeDiez = Mathf.CeilToInt(distanciaFaltante / 10f);
            puntosPerdidosExploracion += (bloquesDeDiez * penalizacionBase);
            ImprimirMensajeConsola($"[-] Exploración incompleta: Faltaron {distanciaFaltante:F1}cm para cubrir el tracto.", "red");
        }

        puntosPerdidosTally[2] += puntosPerdidosExploracion;
        notaSeguridad -= puntosPerdidosExploracion;

        // PENALIZACIÓN POR INACCIÓN (Protocolo y Técnica) ---

        if (poliposRestantes > 0 && ManejadorPartida.totalPolipos > 0)
        {
            // Calculamos el valor de cada pólipo (Ej: 100 / 5 = 20 puntos por pólipo)
            float penalizacionPorInaccion = (100f / ManejadorPartida.totalPolipos) * poliposRestantes;

            notaProtocolo -= penalizacionPorInaccion;
            notaTecnica -= penalizacionPorInaccion;

            ImprimirMensajeConsola($"[-] Ausencia de Procedimiento: -{penalizacionPorInaccion:F1} pts en Protocolo y Técnica por pólipos ignorados.", "orange");
        }

        // ABANDONO PREMATURO (Seguridad a 0)
        // Si no avanzó ni el 20% (20cm) y no sacó nada, reprueba Seguridad automáticamente
        if (profundidadMaximaAlcanzada < 30f && herramientas.ObtenerTotalEliminados() == 0)
        {
            notaSeguridad = 0f;
            ImprimirMensajeConsola("[-] ABANDONO CRÍTICO: Procedimiento abortado sin exploración inicial. Seguridad anulada.", "red");
        }
        // ====================================================================
        //Logica Game Over por negligencia médica grave (si Seguridad cae por debajo de 40)
        bool esGameOver = !string.IsNullOrEmpty(motivoGameOverCritico);
        if (esGameOver)
        {
            // Restamos 50 puntos fijos a cada categoría principal
            notaSeguridad -= 50f;
            notaProtocolo -= 50f;
            notaTecnica -= 50f;
        }
        // --- CIERRE DE NOTAS ---
        notaSeguridad = Mathf.Max(0, notaSeguridad);
        notaProtocolo = Mathf.Max(0, notaProtocolo);
        notaTecnica = Mathf.Max(0, notaTecnica);

        float notaFinal = ((notaSeguridad * ManejadorPartida.pesoSeguridad) +
                           (notaProtocolo * ManejadorPartida.pesoProtocolo) +
                           (notaTecnica * ManejadorPartida.pesoTecnica)) / 100f;

        // --- REPORTE FINAL ---
        if (txtResultadosFinales != null)
        {
            txtResultadosFinales.text =
                $"<b>REPORTE FINAL DE ENDOSCOPIA</b>\n" +
                $"<b>CALIFICACIÓN TOTAL: {notaFinal:F1} / 100</b>\n" +
                $"Estudiante: {ManejadorPartida.nombreEstudiante}\n" +
                $"Profundidad Máxima: {profundidadMaximaAlcanzada:F1} / {META_PROFUNDIDAD} cm\n" +
                $"Pólipos Extraídos: {herramientas.ObtenerTotalEliminados()} / {ManejadorPartida.totalPolipos}\n\n" +
                $"Seguridad y Nav: {notaSeguridad:F1}%\n" +
                $"Protocolo y Diag: {notaProtocolo:F1}%\n" +
                $"Técnica Quirúrgica: {notaTecnica:F1}%\n\n";
        }

        if (txtDetallePenalizaciones != null)
        {
            StringBuilder sb = new StringBuilder();
            System.Collections.Generic.List<string> listaPenalizacionesJson = new System.Collections.Generic.List<string>();
            if (esGameOver)
            {
                sb.AppendLine($"<color=red><b>¡NEGLIGENCIA MÉDICA GRAVE!</b></color>");
                sb.AppendLine($"<color=orange>{motivoGameOverCritico}</color>");
                sb.AppendLine($"<color=red><b>CASTIGO: -50% EN TODAS LAS CATEGORÍAS</b></color>\n");
                listaPenalizacionesJson.Add($"EVENTO CRÍTICO: {motivoGameOverCritico} (-50% general)");
            }
            sb.AppendLine("<color=#DEFF9A><b>DESGLOSE DE PENALIZACIONES:</b></color>\n");

            // Imprimimos el array de 10 parámetros
            for (int i = 0; i < puntosPerdidosTally.Length; i++)
            {
                if (puntosPerdidosTally[i] > 0)
                {
                    sb.AppendLine($"• {nombresParametros[i]}: <color=red>-{puntosPerdidosTally[i]:F1} pts</color>");
                    listaPenalizacionesJson.Add($"-{puntosPerdidosTally[i]:F1} pts: Falla en {nombresParametros[i]}.");
                }
            }

            // Agregamos el texto extra si hubo penalización por inacción
            if (poliposRestantes > 0 && ManejadorPartida.totalPolipos > 0)
            {
                float penalizacionPorInaccion = (100f / ManejadorPartida.totalPolipos) * poliposRestantes;
                sb.AppendLine($"\n• <color=orange>Inacción Quirúrgica (Prot/Tec): -{penalizacionPorInaccion:F1} pts</color>");
                listaPenalizacionesJson.Add($"-{penalizacionPorInaccion:F1} pts: Inacción Quirúrgica ({poliposRestantes} pólipos ignorados).");
            }

            if (profundidadMaximaAlcanzada < 80f && herramientas.ObtenerTotalEliminados() == 0)
            {
                sb.AppendLine($"• <color=red>Abandono Prematuro: Seguridad reducida a 0%</color>");
                listaPenalizacionesJson.Add("-100 pts: Abandono Prematuro de la intervención.");
            }

            if (sb.Length < 60) sb.AppendLine("<color=green>Excelente: No se registraron penalizaciones.</color>");

            txtDetallePenalizaciones.text = sb.ToString();

            Debug.Log($"<color=cyan>[Sistema] Reporte de penalizaciones generado con éxito.</color>");
            // GUARDADO DE ARCHIVO JSON 
            if (ManejadorPartida.guardarHistorial && HistoryManager.instancia != null)
            {
                Debug.Log($"<color=cyan>[Sistema] Iniciando proceso de guardado de sesión...</color>");
                SesionPractica sesionGuardado = new SesionPractica();

                // Datos de Identificación
                sesionGuardado.nombreEstudiante = ManejadorPartida.nombreEstudiante;
                sesionGuardado.fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                // Creamos un ID tipo "JP_20260422_1430"
                string iniciales = ManejadorPartida.nombreEstudiante.Length > 2 ? ManejadorPartida.nombreEstudiante.Substring(0, 2).ToUpper() : "XX";
                sesionGuardado.idSesion = $"{iniciales}_{DateTime.Now:yyyyMMdd_HHmm}";

                // Notas por Categoría
                sesionGuardado.puntajeSeguridad = notaSeguridad;
                sesionGuardado.puntajeProtocolo = notaProtocolo;
                sesionGuardado.puntajeTecnica = notaTecnica;
                sesionGuardado.puntajeTotal = notaFinal;

                // Desglose Específico (Casteando y adaptando a tus variables)
                sesionGuardado.indiceTrauma = (int)puntosPerdidosTally[0]; // Casteo explícito a int para Trauma Tisular
                sesionGuardado.suavidadDesplazamiento = 100f - puntosPerdidosTally[1];
                sesionGuardado.porcentajeExploracion = Mathf.Clamp((profundidadMaximaAlcanzada / META_PROFUNDIDAD) * 100f, 0, 100f);
                sesionGuardado.retiradaSegura = (puntosPerdidosTally[3] == 0);

                sesionGuardado.hallazgosDocumentados = herramientas.ObtenerTotalEliminados();
                sesionGuardado.calidadCapturaPromedio = 100f - puntosPerdidosTally[5];
                sesionGuardado.aciertosYamada = (puntosPerdidosTally[6] == 0) ? 1 : 0;

                sesionGuardado.estabilidadAbordaje = 100f - puntosPerdidosTally[7];
                sesionGuardado.tasaExtraccion = ManejadorPartida.totalPolipos > 0 ? ((float)herramientas.ObtenerTotalEliminados() / ManejadorPartida.totalPolipos) * 100f : 100f;
                sesionGuardado.higieneCampo = (puntosPerdidosTally[9] == 0);

                // Insertamos la lista de textos rojos que fuimos armando arriba
                sesionGuardado.penalizaciones = listaPenalizacionesJson;

                // ¡A GUARDAR SE HA DICHO!
                HistoryManager.instancia.GuardarSesion(sesionGuardado);
                Debug.Log($"<color=cyan>[Sistema] Archivo guardado correctamente para la sesión: {sesionGuardado.idSesion}</color>");
            }
        }
    }

    public void MostrarPopupConfirmacion(TipoConfirmacion tipo)
    {
        contextoConfirmacion = tipo;
        if (panelConfirmarSalida != null) panelConfirmarSalida.SetActive(true);

        // Cambiamos el texto para que el usuario entienda qué está aceptando
        if (txtPreguntaConfirmacion != null)
        {
            if (tipo == TipoConfirmacion.FinalizarProcedimiento)
                txtPreguntaConfirmacion.text = "¿Está seguro que desea finalizar la endoscopia y ver su calificación?";
            else if (tipo == TipoConfirmacion.SalirAlMenu)
                txtPreguntaConfirmacion.text = "¿Volver al menú principal? Se perderá el progreso de esta sesión.";
        }
    }

    public void OcultarPopupConfirmacion()
    {
        contextoConfirmacion = TipoConfirmacion.Ninguno;
        if (panelConfirmarSalida != null) panelConfirmarSalida.SetActive(false);
    }
    public void BotonConfirmarSi()
    {
        if (contextoConfirmacion == TipoConfirmacion.FinalizarProcedimiento)
        {
            FinalizarSimulacion(); // Calcula nota y muestra resultados
        }
        else if (contextoConfirmacion == TipoConfirmacion.SalirAlMenu)
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        OcultarPopupConfirmacion();

        // Le avisamos al hardware que ya se cerró el menú
        if (herramientas != null) herramientas.ActivarModoSalida(false);
    }

    public void BotonConfirmarNo()
    {
        OcultarPopupConfirmacion();
        if (herramientas != null) herramientas.ActivarModoSalida(false);
    }
    private System.Collections.IEnumerator ForzarScrollHaciaAbajo()
    {
        // Esperamos a que termine el frame actual para que el Canvas recalcule el tamaño del texto
        yield return new WaitForEndOfFrame();

        if (scrollRespuestas != null)
        {
            // 0f significa "hasta abajo", 1f significaría "hasta arriba"
            scrollRespuestas.verticalNormalizedPosition = 0f;
        }
    }
    public void MostrarGameOver(string motivo)
    {
        motivoGameOverCritico = motivo; // Guardamos el motivo para el reporte

        if (panelIndicativo != null)
        {
            txtTituloIndicativo.text = "Simulación Finalizada";
            txtTextoIndicativo.text = $"<color=red><b>EVENTO CRÍTICO:</b></color>\n{motivo}";
            panelIndicativo.SetActive(true);
        }
        else
        {
            // Si olvidaste asignar el panel, pasa directo a los resultados
            FinalizarSimulacion();
        }
    }
    public void MostrarMensajeDeInicio()
    {
        if (panelIndicativo != null)
        {
            // Pausamos el tiempo para que no empiece a correr el reloj ni la física
            Time.timeScale = 0f;
            estaPausado = true; // Usamos tu variable existente para bloquear controles

            string titulo = "";
            string texto = "";

            switch (ManejadorPartida.dificultad)
            {
                case 0: // Tutorial
                    titulo = "DATOS: TUTORIAL";
                    texto = "Práctica rápida guiada con 5 pólipos predefinidos para enseñar los controles y cómo resolver el procedimiento.\n\nSiga las instrucciones en pantalla.";
                    break;
                case 1: // Fácil
                    titulo = "DATOS: FÁCIL";
                    texto = "Sistema guiado que muestra el puntaje en tiempo real. Indica qué acciones tomar (ej. mantenerse quieto) y da feedback visual si se comete un error.";
                    break;
                case 2: // Normal
                    titulo = "DATOS: NORMAL";
                    texto = "Retira las guías paso a paso; el sistema solo indicará al usuario cuando pierda puntos. Evalúe con cuidado.";
                    break;
                case 3: // Realista
                    titulo = "DATOS: REALISTA";
                    texto = "<color=red>Emulación libre sin guías visuales ni elementos de apoyo.</color>\n\nSolo contará con respuestas hápticas de daño al paciente. Si retira el endoscopio sin efectuar acciones, finalizará con 0 puntos.";
                    break;
                default:
                    titulo = "DATOS: DESCONOCIDO";
                    texto = "Proceda con precaución.";
                    break;
            }
            string recomendaciones = "\n\n<color=#B0B0B0><i><b>Recomendaciones Clínicas:</b>\n" +
                                    "• Controle la fuerza de inserción; la pared intestinal es delicada.\n" +
                                    "• Mantenga una distancia prudente para enfocar y operar los pólipos.\n" +
                                    "• Si el endoscopio se atasca, no fuerce el avance. Jale lentamente mientras aplica torsión para arrectar el tubo.</i></color>";
            txtTituloIndicativo.text = titulo;
            txtTextoIndicativo.text = texto + recomendaciones;

            panelIndicativo.SetActive(true);
        }
    }
    private void ActualizarGuiaContextual()
    {
        if (panelGuiaContextual == null || txtGuiaContextual == null || herramientas == null) return;

        panelGuiaContextual.SetActive(true);
        string mensajeGuia = "";

        int poliposRestantes = ManejadorPartida.totalPolipos - herramientas.ObtenerTotalEliminados();

        string btnFreeze = "Teclado 1";
        string btnCapture = "Teclado 2";
        string btnSuccion = "Teclado 4";
        string btnAccion = "Teclado 5";
        string btnLimpiado = "Teclado 6";

        bool usandoHardware = (endoscopio != null && endoscopio.usarControlHardware);

        if (usandoHardware && ConfigManager.instancia != null)
        {
            btnFreeze = ConfigManager.instancia.mapFreeze;
            btnCapture = ConfigManager.instancia.mapCapture;
            btnSuccion = ConfigManager.instancia.mapSuccion;
            btnAccion = ConfigManager.instancia.mapAccion;
            btnLimpiado = ConfigManager.instancia.mapLimpiado;
        }
        // 1. CONDICIÓN DE VICTORIA (Ya no hay pólipos)
        if (poliposRestantes <= 0)
        {
            if (herramientas.enZonaExtraccion)
                mensajeGuia = $"<color=#32CD32>¡Excelente! Has terminado. Presiona Acción <color=yellow>[{btnAccion}]</color> para salir y ver resultados.</color>";
            else
                mensajeGuia = "¡Todos los pólipos extraídos! Vuelve a la zona de extracción al inicio del tracto y presiona salir.";
        }
        // 2. sangre del corte (Bloquea visión, máxima prioridad)
        else if (herramientas.ObtenerNivelSangrado() > 0f)
        {
            mensajeGuia = $"<color=#FF0000>Sangre de corte.</color> Mantenga presionado Succión <color=yellow>[{btnSuccion}]</color> para limpiar el campo visual.";
        }
        // 3. EXTRACCIÓN (Llevando pólipo en la punta)
        else if (herramientas.llevandoPolipo)
        {
            if (herramientas.enZonaExtraccion)
                mensajeGuia = $"¡Llegaste a la salida! Usa Soltar Pólipo <color=yellow>[{btnSuccion}]</color> para depositar la muestra en el laboratorio.";
            else
                mensajeGuia = "Saca el pólipo atrapado extrayendo el tubo hacia la zona de inicio. Cuidado con los tirones.";
        }
        // 4. EXCESO DE AGUA (Peligro de aspiración del paciente)
        else if (herramientas.ObtenerLavadosSinSuccionar() > 0)
        {
            mensajeGuia = $"Fluidos acumulados por lavado. Presione Succión <color=yellow>[{btnSuccion}]</color> para aspirar el agua.";
        }
        // 5. LENTE SUCIO (Peligro de mala captura)
        else if (herramientas.ObtenerNivelSuciedad() > 0.2f)
        {
            mensajeGuia = $"<color=#FF8C00>Lente obstruido.</color> Presione Lavar Lente <color=yellow>[{btnLimpiado}]</color> para irrigar la óptica.";
        }
        // 6. ATRAPAR (Cortó un pólipo grande y está suelto)
        else if (
            (herramientas.ObtenerUltimoPolipoCortado() != null && herramientas.ObtenerUltimoPolipoCortado().estadoActual == PolipoInteractuable.EstadoPolipo.CortadoSuelto) ||
            (herramientas.ObtenerPolipoEnMira() != null && herramientas.ObtenerPolipoEnMira().estadoActual == PolipoInteractuable.EstadoPolipo.CortadoSuelto)
        )
        {
            mensajeGuia = $"El pólipo está suelto. Apunta hacia él y usa Atrapar Pólipo <color=yellow>[{btnSuccion}]</color> para asegurarlo con el asa.";
        }
        // 7. MENÚ DE HERRAMIENTAS ABIERTO
        else if (herramientas.EstaEnModoSeleccion())
        {
            PolipoInteractuable polipo = herramientas.ObtenerPolipoEnMira();
            if (polipo != null)
            {
                // GUÍA BASADA EN TAMAÑO REAL
                if (polipo.tamanoMilimetros <= 5f)
                    mensajeGuia = $"Pólipo diminuto ({polipo.tamanoMilimetros:F1}mm). Usa Pinza de Biopsia <color=yellow>[{btnFreeze}]</color>.";
                else
                    mensajeGuia = $"Pólipo grande ({polipo.tamanoMilimetros:F1}mm). Usa Asa de Polipectomía <color=yellow>[{btnCapture}]</color>.";
            }
        }
        // 8. PÓLIPO EN LA MIRA (Protocolo Médico)
        else if (herramientas.ObtenerPolipoEnMira() != null)
        {
            PolipoInteractuable polipo = herramientas.ObtenerPolipoEnMira();

            if (!polipo.fueFotografiado)
            {
                if (!herramientas.estaCongelado)
                    mensajeGuia = $"¡Pólipo detectado! Sigue el protocolo:\n1. Congela la imagen <color=yellow>[{btnFreeze}]</color>";
                else
                    mensajeGuia = $"Imagen congelada.\n2. Toma la fotografía <color=yellow>[{btnCapture}]</color> para documentarlo.";
            }
            else
            {
                if (herramientas.estaCongelado)
                    mensajeGuia = $"Fotografía guardada.\n1. Descongela la imagen <color=yellow>[{btnFreeze}]</color> para volver a moverte.";
                else
                    mensajeGuia = $"Protocolo visual completo.\nPresiona el Botón de Acción <color=yellow>[{btnAccion}]</color> para elegir herramienta.";
            }
        }
        // 9. CONGELADO POR ERROR
        else if (herramientas.estaCongelado)
        {
            mensajeGuia = $"La pantalla está congelada. Presiona Freeze <color=yellow>[{btnFreeze}]</color> para seguir moviéndote.";
        }
        // 10. ZONA DE EXTRACCIÓN (Buscando)
        else if (herramientas.enZonaExtraccion)
        {
            mensajeGuia = "Aún no hemos acabado. Inserta el endoscopio y busca más pólipos.";
        }
        // 11. EXPLORACIÓN NORMAL
        else
        {
            if (!endoscopio.MoviendoControles())
                mensajeGuia = "Busca un pólipo. Muévete insertando el tubo en la ranura de inserción.";
            else
                mensajeGuia = "Explorando... Revisa bien detrás de los pliegues intestinales usando el torque.";
        }

        txtGuiaContextual.text = mensajeGuia;
    }
    // Método para encender/apagar el letrero de atasco
    public void ActualizarEstadoBucle(bool estaAtascado)
    {
        if (alertaBucleUI != null)
        {
            // Solo lo mostramos si está atascado y la dificultad NO es Realista (3)
            if (estaAtascado && ManejadorPartida.dificultad != 3)
            {
                alertaBucleUI.SetActive(true);
            }
            else
            {
                alertaBucleUI.SetActive(false);
            }
        }
    }
    private void IntentarReconexionSTM32()
    {
        Debug.Log("Intentando reconexión con STM32...");
        if (SerialManager.instancia != null)
        {
            // Cambiamos el texto para dar feedback inmediato
            if (txtAlertaConexion != null)
                txtAlertaConexion.text = "<color=yellow>Buscando hardware STM32...</color>\nPor favor espere.";

            // Llamamos al hilo de búsqueda que ya tienes programado
            SerialManager.instancia.IniciarBusqueda();
        }
        else
        {
            if (txtAlertaConexion != null)
                txtAlertaConexion.text = "<color=red>Error Interno: SerialManager no encontrado.</color>";
        }
    }
}