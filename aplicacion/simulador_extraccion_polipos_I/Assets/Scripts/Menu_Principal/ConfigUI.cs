using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfigUI : MonoBehaviour
{
    [Header("Conexiones Directas")]
    public SerialManager serial;
    public ConfigManager config;

    [Header("Sliders de Sensibilidad")]
    public Slider sliderSensIns;
    public Slider sliderSensTor;
    public Slider sliderSensVol;

    [Header("Textos de Sensibilidad")]
    public TextMeshProUGUI txtValIns;
    public TextMeshProUGUI txtValTor;
    public TextMeshProUGUI txtValVol;

    [Header("Textos de Mapeo: Volantes")]
    public TextMeshProUGUI txtMapVolXIzq;
    public TextMeshProUGUI txtMapVolXDer;
    public TextMeshProUGUI txtMapVolYArr;
    public TextMeshProUGUI txtMapVolYAba;

    [Header("Textos de Mapeo: Tubo")]
    public TextMeshProUGUI txtMapInsAdelante;
    public TextMeshProUGUI txtMapInsAtras;
    public TextMeshProUGUI txtMapTorDer;
    public TextMeshProUGUI txtMapTorIzq;

    [Header("Textos de Mapeo: Botones")]
    public TextMeshProUGUI txtMapFreeze;
    public TextMeshProUGUI txtMapCapture;
    public TextMeshProUGUI txtMapZoom;
    public TextMeshProUGUI txtMapSuccion;

    [Header("Feedback al Usuario")]
    public TextMeshProUGUI txtFeedback;

    private string accionEsperando = "";
    private bool esperandoEje = false;
    private TextMeshProUGUI textoBotonActivo;

    void Awake()
    {
        // Ignoramos los cadáveres del Inspector.
        serial = SerialManager.instancia;
        config = ConfigManager.instancia;
    }

    void Start()
    {
        if (serial == null) serial = SerialManager.instancia;
        if (config == null) config = ConfigManager.instancia;
        ActualizarUI();

    }

    void OnEnable()
    {
        // Doble validación al entrar a esta pestaña
        if (serial == null) serial = SerialManager.instancia;
        if (config == null) config = ConfigManager.instancia;

        ActualizarUI();

        if (serial != null)
            serial.AlRecibirNuevosDatos += EscucharParaMapear;
    }

    void OnDisable()
    {
        if (serial != null)
            serial.AlRecibirNuevosDatos -= EscucharParaMapear;

        accionEsperando = "";
    }

    private void ActualizarUI()
    {
        if (config == null) return;

        if (sliderSensIns) sliderSensIns.SetValueWithoutNotify(config.sensInsercion);
        if (sliderSensTor) sliderSensTor.SetValueWithoutNotify(config.sensTorsion);
        if (sliderSensVol) sliderSensVol.SetValueWithoutNotify(config.sensVolantes);

        if (txtMapVolXIzq) txtMapVolXIzq.text = config.mapVolXIzq;
        if (txtMapVolXDer) txtMapVolXDer.text = config.mapVolXDer;
        if (txtMapVolYArr) txtMapVolYArr.text = config.mapVolYArr;
        if (txtMapVolYAba) txtMapVolYAba.text = config.mapVolYAba;
        if (txtMapInsAdelante) txtMapInsAdelante.text = config.mapInsAdelante;
        if (txtMapInsAtras) txtMapInsAtras.text = config.mapInsAtras;
        if (txtMapTorDer) txtMapTorDer.text = config.mapTorDer;
        if (txtMapTorIzq) txtMapTorIzq.text = config.mapTorIzq;
        if (txtMapFreeze) txtMapFreeze.text = config.mapFreeze;
        if (txtMapCapture) txtMapCapture.text = config.mapCapture;
        if (txtMapZoom) txtMapZoom.text = config.mapZoom;
        if (txtMapSuccion) txtMapSuccion.text = config.mapSuccion;

        txtFeedback.text = "Modifica los valores y presiona 'Guardar' para aplicar.";
        ActualizarTextosSensibilidad();
    }

    public void CambiarSensibilidadInsercion(float val)
    {
        if (config == null) return;
        config.sensInsercion = val;
        ActualizarTextosSensibilidad();
    }
    public void CambiarSensibilidadTorsion(float val)
    {
        if (config == null) return;
        config.sensTorsion = val;
        ActualizarTextosSensibilidad();
    }
    public void CambiarSensibilidadVolantes(float val)
    {
        if (config == null) return;
        config.sensVolantes = val;
        ActualizarTextosSensibilidad();
    }

    private void ActualizarTextosSensibilidad()
    {
        if (config == null) return;
        if (txtValIns) txtValIns.text = config.sensInsercion.ToString("F1") + "x";
        if (txtValTor) txtValTor.text = config.sensTorsion.ToString("F1") + "x";
        if (txtValVol) txtValVol.text = config.sensVolantes.ToString("F1") + "x";
    }

    public void MapearVolXIzq() { PrepararMapeo("VolXIzq", true, txtMapVolXIzq); }
    public void MapearVolXDer() { PrepararMapeo("VolXDer", true, txtMapVolXDer); }
    public void MapearVolYArr() { PrepararMapeo("VolYArr", true, txtMapVolYArr); }
    public void MapearVolYAba() { PrepararMapeo("VolYAba", true, txtMapVolYAba); }
    public void MapearInsAdelante() { PrepararMapeo("InsAde", true, txtMapInsAdelante); }
    public void MapearInsAtras() { PrepararMapeo("InsAtr", true, txtMapInsAtras); }
    public void MapearTorDer() { PrepararMapeo("TorDer", true, txtMapTorDer); }
    public void MapearTorIzq() { PrepararMapeo("TorIzq", true, txtMapTorIzq); }
    public void MapearFreeze() { PrepararMapeo("Freeze", false, txtMapFreeze); }
    public void MapearCapture() { PrepararMapeo("Capture", false, txtMapCapture); }
    public void MapearZoom() { PrepararMapeo("Zoom", false, txtMapZoom); }
    public void MapearSuccion() { PrepararMapeo("Succion", false, txtMapSuccion); }

    private void PrepararMapeo(string accion, bool requiereEje, TextMeshProUGUI textoUI)
    {
        accionEsperando = accion;
        esperandoEje = requiereEje;
        textoBotonActivo = textoUI;
        textoBotonActivo.text = "<color=black>Moviendo...</color>";
        txtFeedback.text = requiereEje ? "Gira o empuja el control físico..." : "Presiona el botón físico...";
    }

    private void EscucharParaMapear(DatosHardware d)
    {
        if (accionEsperando == "") return;

        string inputDetectado = DetectarInput(d, esperandoEje);

        if (inputDetectado != "")
        {
            AsignarMapeoEnMemoria(accionEsperando, inputDetectado);
            textoBotonActivo.text = inputDetectado;
            txtFeedback.text = "<color=green>Asignado temporalmente. ¡No olvides Guardar!</color>";
            accionEsperando = "";
        }
        else if (DetectarInput(d, !esperandoEje) != "")
        {
            txtFeedback.text = "<color=red>Movimiento Inválido. Usa el tipo de control correcto.</color>";
        }
    }

    private string DetectarInput(DatosHardware d, bool buscarEje)
    {
        if (buscarEje)
        {
            if (d.insercion > 0) return "INS_+";
            if (d.insercion < 0) return "INS_-";
            if (d.torsion > 0) return "TOR_+";
            if (d.torsion < 0) return "TOR_-";
            if (d.volante1 > 0) return "E1_+";
            if (d.volante1 < 0) return "E1_-";
            if (d.volante2 > 0) return "E2_+";
            if (d.volante2 < 0) return "E2_-";
        }
        else
        {
            if (d.boton1 == 1) return "B1";
            if (d.boton2 == 1) return "B2";
            if (d.boton3 == 1) return "B3";
            if (d.boton4 == 1) return "B4";
            if (d.botonSuccion == 1) return "Su";
        }
        return "";
    }

    private void AsignarMapeoEnMemoria(string accion, string inputDetectado)
    {
        if (config == null) return;

        switch (accion)
        {
            case "VolXIzq": config.mapVolXIzq = inputDetectado; break;
            case "VolXDer": config.mapVolXDer = inputDetectado; break;
            case "VolYArr": config.mapVolYArr = inputDetectado; break;
            case "VolYAba": config.mapVolYAba = inputDetectado; break;
            case "InsAde": config.mapInsAdelante = inputDetectado; break;
            case "InsAtr": config.mapInsAtras = inputDetectado; break;
            case "TorDer": config.mapTorDer = inputDetectado; break;
            case "TorIzq": config.mapTorIzq = inputDetectado; break;
            case "Freeze": config.mapFreeze = inputDetectado; break;
            case "Capture": config.mapCapture = inputDetectado; break;
            case "Zoom": config.mapZoom = inputDetectado; break;
            case "Succion": config.mapSuccion = inputDetectado; break;
        }
    }

    public void BotonGuardarCambios()
    {
        if (config != null)
        {
            config.GuardarAjustes();
            txtFeedback.text = "<color=green><b>¡Configuración guardada exitosamente!</b></color>";
        }
    }

    public void BotonRestablecer()
    {
        if (config != null)
        {
            config.RestablecerValores();
            ActualizarUI();
            txtFeedback.text = "<color=white>Valores restablecidos a fábrica.</color>";
        }
    }

    public void ProbarVibracionSTM()
    {
        if (serial != null && serial.estadoActual == SerialManager.EstadoConexion.Conectado)
        {
            serial.EnviarDato("V1:1000\n");
            txtFeedback.text = "<color=green>Probando vibrador...</color>";
        }
        else
        {
            txtFeedback.text = "<color=red>Error: Hardware no conectado.</color>";
        }
    }
}