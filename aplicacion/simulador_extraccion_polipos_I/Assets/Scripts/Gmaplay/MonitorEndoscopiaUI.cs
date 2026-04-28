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
    private float puntosPerdidos = 0f;

    void Start()
    {
        panelPausa.SetActive(false);
        pantallaCargaNegra.SetActive(false);

        string nombreDoc = ManejadorPartida.nombreEstudiante != "" ? ManejadorPartida.nombreEstudiante : "NaN";
        txtDatosDoctor.text = $"Dr/a: {nombreDoc}\n\nDificultad: {ObtenerNombreDificultad()}";

        ActualizarTextosBotones();

        btnContinuar.onClick.AddListener(ReanudarJuego);
        btnMenuPrincipal.onClick.AddListener(IrAlMenuPrincipal);
        chkModoComputadora.onValueChanged.AddListener(CambiarModoControl);
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
        // 1. PROFUNDIDAD REAL DESDE EL ODÓMETRO
        int centimetros = Mathf.RoundToInt(endoscopio.distanciaTotalInsertada * 10f);
        txtProfundidad.text = $"Cm: {centimetros}";

        // 2. EL OCTÁGONO (SOLUCIÓN DE INGENIERÍA)
        // En lugar de preguntarle a los huesos 3D que están doblados, usamos el valor matemático puro.
        float giroPuro = endoscopio.torqueGiro;

        txtTorque.text = $"Torque: {Mathf.RoundToInt(endoscopio.torqueGiro)}°";

        // Giramos el gráfico UI en el eje Z (el eje de las pantallas 2D).
        // NOTA: Si ves que gira al revés que tu mano, cámbialo a positivo (giroPuro) en vez de negativo (-giroPuro)
        if (indicadorOctagono != null)
        {
            indicadorOctagono.localRotation = Quaternion.Euler(0, 0, +giroPuro);
        }

        if (herramientas != null)
        {
            ActualizarPanelPolipos();
            if (ManejadorPartida.dificultad == 0 || ManejadorPartida.dificultad == 1)
            {
                float notaFinal = 100f - puntosPerdidos;
                txtNotaEstudiante.text = $"Seguridad y Navegación:\n{notaFinal:F1} / 100\n\nProtocolo y Diagnóstico:\n{notaFinal:F1} / 100\n\nTécnica Quirúrgica:\n{notaFinal:F1} / 100";
            }
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
            if (SerialManager.instancia.estadoActual == SerialManager.EstadoConexion.Error)
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
                if (estaPausado)
                {
                    txtAlertaConexion.text = "<color=green>Conexión estable.</color>";
                    chkModoComputadora.interactable = true;
                }
            }
        }
    }

    private void ActualizarTextosBotones()
    {
        if (ConfigManager.instancia != null)
        {
            ConfigManager cfg = ConfigManager.instancia;
            txtListaBotones.text =
                $"<color=#00FFFF>(Configuración Activa)</color>\n" +
                $"1: Freeze [{cfg.mapFreeze}]\n" +
                $"2: Capture [{cfg.mapCapture}]\n" +
                $"3: Zoom [{cfg.mapZoom}]\n" +
                $"4: Succión [{cfg.mapSuccion}]";
        }
        else
        {
            txtListaBotones.text = "Error: ConfigManager no encontrado.";
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
        Time.timeScale = 1f;
        panelPausa.SetActive(false);
    }

    private void CambiarModoControl(bool usarPc)
    {
        if (endoscopio != null)
        {
            endoscopio.usarControlHardware = !usarPc;
        }
    }

    private void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void RegistrarError(string mensajeFallo, float puntosAQuitar)
    {
        puntosPerdidos += puntosAQuitar;
        txtRespuestas.text += $"\n<color=red>[-] {mensajeFallo}</color>";

        string[] lineas = txtRespuestas.text.Split('\n');
        if (lineas.Length > 5)
        {
            txtRespuestas.text = "Respuestas:" + string.Join("\n", lineas, lineas.Length - 4, 4);
        }
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