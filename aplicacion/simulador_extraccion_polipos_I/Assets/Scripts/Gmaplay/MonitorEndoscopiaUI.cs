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
    public TextMeshProUGUI txtTorque;
    public TextMeshProUGUI txtRespuestas;
    public RectTransform indicadorOctagono;

    // --- NUEVO: TEXTO DE DAÑO ---
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

    [Header("Pantallas Extra")]
    public GameObject pantallaCargaNegra;

    private bool estaPausado = false;

    public enum CategoriaEvaluacion { Seguridad, Protocolo, Tecnica }

    private float notaSeguridad = 100f;
    private float notaProtocolo = 100f;
    private float notaTecnica = 100f;
    [Header("Popups y Fin de Juego")]

    public GameObject panelConfirmarSalida; // Arrastra aquí un Panel oscuro con el texto "¿Desea finalizar la endoscopia?"
    public TextMeshProUGUI txtPreguntaConfirmacion;// Un texto dentro del panel para cambiar la pregunta según el caso

    public enum TipoConfirmacion { Ninguno, FinalizarProcedimiento, SalirAlMenu }
    private TipoConfirmacion contextoConfirmacion = TipoConfirmacion.Ninguno;

    private float profundidadMaximaAlcanzada = 0f;
    private const float META_PROFUNDIDAD = 400f;
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
        pantallaCargaNegra.SetActive(false);
        if (panelConfirmarSalida != null) panelConfirmarSalida.SetActive(false);
        if (panelResultadosFinales != null) panelResultadosFinales.SetActive(false);

        Time.timeScale = 1f;

        string nombreDoc = ManejadorPartida.nombreEstudiante != "" ? ManejadorPartida.nombreEstudiante : "NaN";
        txtDatosDoctor.text = $"Dr/a: {nombreDoc}\n\nDificultad: {ObtenerNombreDificultad()}";

        ActualizarTextosBotones(false);

        btnContinuar.onClick.RemoveAllListeners();
        btnContinuar.onClick.AddListener(ReanudarJuego);

        // CAMBIO AQUÍ: Ahora el botón del menú abre el popup, no carga la escena directo
        btnMenuPrincipal.onClick.RemoveAllListeners();
        btnMenuPrincipal.onClick.AddListener(() => MostrarPopupConfirmacion(TipoConfirmacion.SalirAlMenu));

        chkModoComputadora.onValueChanged.RemoveAllListeners();
        chkModoComputadora.onValueChanged.AddListener(CambiarModoControl);

        CambiarModoControl(chkModoComputadora.isOn);

        if (txtDanioPaciente != null) txtDanioPaciente.text = "Daño: 0%";
        if (btnFinalizarReporte != null)
        {
            btnFinalizarReporte.onClick.AddListener(IrAlMenuPrincipal);
        }
    }

    void Update()
    {
        float profundidadActualCm = endoscopio.distanciaTotalInsertada * 10f;
        txtReloj.text = DateTime.Now.ToString("dd/MM/yyyy\nHH:mm:ss");

        if (!estaPausado && endoscopio != null)
        {
            ActualizarTelemetria();
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
        int centimetros = Mathf.RoundToInt(endoscopio.distanciaTotalInsertada * 10f);
        txtProfundidad.text = $"Cm: {centimetros}";

        float giroPuro = endoscopio.torqueGiro;
        txtTorque.text = $"Torque: {Mathf.RoundToInt(giroPuro)}°";

        if (indicadorOctagono != null)
        {
            indicadorOctagono.localRotation = Quaternion.Euler(0, 0, +giroPuro);
        }

        if (herramientas != null)
        {
            ActualizarPanelPolipos();

            if (ManejadorPartida.dificultad == 0 || ManejadorPartida.dificultad == 1)
            {
                txtNotaEstudiante.text =
                    $"Seguridad y Nav:\n<color={(notaSeguridad > 60 ? "white" : "red")}>{notaSeguridad:F1} / 100</color>\n\n" +
                    $"Protocolo y Diag:\n<color={(notaProtocolo > 60 ? "white" : "red")}>{notaProtocolo:F1} / 100</color>\n\n" +
                    $"Técnica Quirúrgica:\n<color={(notaTecnica > 60 ? "white" : "red")}>{notaTecnica:F1} / 100</color>";
            }
            else
            {
                txtNotaEstudiante.text = "Evaluación en curso...";
            }
        }
    }

    // --- NUEVA FUNCIÓN PARA ACTUALIZAR DAÑO ---
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
                $"Pólipos Totales: {totalExtraidos} / {tTotal}\n\n" +
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
        if (SerialManager.instancia != null)
        {
            if (SerialManager.instancia.estadoActual == SerialManager.EstadoConexion.Error && !chkModoComputadora.isOn)
            {
                if (!estaPausado)
                {
                    PausarJuego();
                    txtAlertaConexion.text = "<color=red> ERROR: CONEXIÓN STM32 PERDIDA</color>\nPor favor, revise el cable USB.";
                    chkModoComputadora.isOn = true;
                    chkModoComputadora.interactable = false;
                }
            }
            else if (SerialManager.instancia.estadoActual == SerialManager.EstadoConexion.Conectado)
            {
                if (estaPausado && !chkModoComputadora.isOn)
                {
                    txtAlertaConexion.text = "<color=green>Conexión estable.</color>";
                    chkModoComputadora.interactable = true;
                }
            }
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
                    $"1: Pinza Biopsia [{cfg.mapFreeze}]\n" +
                    $"2: Asa Diatérmica [{cfg.mapCapture}]\n" +
                    $"<color=#888888>3: ---</color>\n" +
                    $"<color=#888888>4: ---</color>\n" +
                    $"5: Cancelar [{cfg.mapAccion}]";
            }
            else
            {
                string textoBoton5 = habilitarSalida
                ? $"<color=red><b>5: Finalizar Endoscopia [{cfg.mapAccion}]</b></color>"
                : $"5: Herramientas [{cfg.mapAccion}]";
                txtListaBotones.text =
                    $"<color=#00FFFF>(Configuración Activa)</color>\n" +
                    $"1: Freeze [{cfg.mapFreeze}]\n" +
                    $"2: Capture [{cfg.mapCapture}]\n" +
                    $"3: Zoom [{cfg.mapZoom}]\n" +
                    $"4: Succión [{cfg.mapSuccion}]\n" +
                    textoBoton5; ;
            }
        }
    }

    public void PausarJuego()
    {
        estaPausado = true;
        Time.timeScale = 0f;
        panelPausa.SetActive(true);
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
        ImprimirMensajeConsola($"[-] {mensajeFallo} (-{puntosAQuitar} pts)", "red");
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

        // Condición B: Profundidad insuficiente (Meta 400cm)
        if (profundidadMaximaAlcanzada < META_PROFUNDIDAD)
        {
            float distanciaFaltante = META_PROFUNDIDAD - profundidadMaximaAlcanzada;
            int bloquesDeDiez = Mathf.CeilToInt(distanciaFaltante / 10f);
            puntosPerdidosExploracion += (bloquesDeDiez * penalizacionBase);
            ImprimirMensajeConsola($"[-] Exploración incompleta: Faltaron {distanciaFaltante:F1}cm para cubrir el tracto.", "red");
        }

        puntosPerdidosTally[2] += puntosPerdidosExploracion;
        notaSeguridad -= puntosPerdidosExploracion;

        // ====================================================================
        // --- NUEVO: PENALIZACIÓN POR INACCIÓN (Protocolo y Técnica) ---
        // ====================================================================
        if (poliposRestantes > 0 && ManejadorPartida.totalPolipos > 0)
        {
            // Calculamos el valor de cada pólipo (Ej: 100 / 5 = 20 puntos por pólipo)
            float penalizacionPorInaccion = (100f / ManejadorPartida.totalPolipos) * poliposRestantes;

            notaProtocolo -= penalizacionPorInaccion;
            notaTecnica -= penalizacionPorInaccion;

            ImprimirMensajeConsola($"[-] Ausencia de Procedimiento: -{penalizacionPorInaccion:F1} pts en Protocolo y Técnica por pólipos ignorados.", "orange");
        }

        // --- NUEVO: ABANDONO PREMATURO (Seguridad a 0) ---
        // Si no avanzó ni el 20% (80cm) y no sacó nada, reprueba Seguridad automáticamente
        if (profundidadMaximaAlcanzada < 80f && herramientas.ObtenerTotalEliminados() == 0)
        {
            notaSeguridad = 0f;
            ImprimirMensajeConsola("[-] ABANDONO CRÍTICO: Procedimiento abortado sin exploración inicial. Seguridad anulada.", "red");
        }
        // ====================================================================

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
            sb.AppendLine("<color=#DEFF9A><b>DESGLOSE DE PENALIZACIONES:</b></color>\n");

            // Imprimimos el array de 10 parámetros
            for (int i = 0; i < puntosPerdidosTally.Length; i++)
            {
                if (puntosPerdidosTally[i] > 0)
                {
                    sb.AppendLine($"• {nombresParametros[i]}: <color=red>-{puntosPerdidosTally[i]:F1} pts</color>");
                }
            }

            // Agregamos el texto extra si hubo penalización por inacción
            if (poliposRestantes > 0 && ManejadorPartida.totalPolipos > 0)
            {
                float penalizacionPorInaccion = (100f / ManejadorPartida.totalPolipos) * poliposRestantes;
                sb.AppendLine($"\n• <color=orange>Inacción Quirúrgica (Prot/Tec): -{penalizacionPorInaccion:F1} pts</color>");
            }

            if (profundidadMaximaAlcanzada < 80f && herramientas.ObtenerTotalEliminados() == 0)
            {
                sb.AppendLine($"• <color=red>Abandono Prematuro: Seguridad reducida a 0%</color>");
            }

            if (sb.Length < 60) sb.AppendLine("<color=green>Excelente: No se registraron penalizaciones.</color>");

            txtDetallePenalizaciones.text = sb.ToString();
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
}