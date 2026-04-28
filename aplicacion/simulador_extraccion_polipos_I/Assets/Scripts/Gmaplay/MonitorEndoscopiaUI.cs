using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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

    void Start()
    {
        panelPausa.SetActive(false);
        pantallaCargaNegra.SetActive(false);
        Time.timeScale = 1f;

        string nombreDoc = ManejadorPartida.nombreEstudiante != "" ? ManejadorPartida.nombreEstudiante : "NaN";
        txtDatosDoctor.text = $"Dr/a: {nombreDoc}\n\nDificultad: {ObtenerNombreDificultad()}";

        ActualizarTextosBotones(false);

        btnContinuar.onClick.RemoveAllListeners();
        btnContinuar.onClick.AddListener(ReanudarJuego);

        btnMenuPrincipal.onClick.RemoveAllListeners();
        btnMenuPrincipal.onClick.AddListener(IrAlMenuPrincipal);

        chkModoComputadora.onValueChanged.RemoveAllListeners();
        chkModoComputadora.onValueChanged.AddListener(CambiarModoControl);

        CambiarModoControl(chkModoComputadora.isOn);

        // Inicializar texto de daño
        if (txtDanioPaciente != null) txtDanioPaciente.text = "Daño: 0%";
    }

    void Update()
    {
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

    public void ActualizarTextosBotones(bool enModoSeleccion)
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
                txtListaBotones.text =
                    $"<color=#00FFFF>(Configuración Activa)</color>\n" +
                    $"1: Freeze [{cfg.mapFreeze}]\n" +
                    $"2: Capture [{cfg.mapCapture}]\n" +
                    $"3: Zoom [{cfg.mapZoom}]\n" +
                    $"4: Succión [{cfg.mapSuccion}]\n" +
                    $"5: Herramientas [{cfg.mapAccion}]";
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

        switch (categoria)
        {
            case CategoriaEvaluacion.Seguridad: notaSeguridad -= puntosAQuitar; break;
            case CategoriaEvaluacion.Protocolo: notaProtocolo -= puntosAQuitar; break;
            case CategoriaEvaluacion.Tecnica: notaTecnica -= puntosAQuitar; break;
        }
        ImprimirMensajeConsola($"[-] {mensajeFallo} (-{puntosAQuitar} pts)", "red");
    }

    private void ImprimirMensajeConsola(string lineaCompleta, string color)
    {
        txtRespuestas.text += $"\n<color={color}>{lineaCompleta}</color>";
        string[] lineas = txtRespuestas.text.Split('\n');
        if (lineas.Length > 5)
            txtRespuestas.text = "Respuestas:" + string.Join("\n", lineas, lineas.Length - 4, 4);
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
}