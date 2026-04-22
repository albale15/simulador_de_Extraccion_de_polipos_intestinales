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
        if (serial == null) serial = FindObjectOfType<SerialManager>();
        if (config == null) config = FindObjectOfType<ConfigManager>();
    }
    // Doble seguridad para cargar la UI 
    void Start()
    {
        // El Start se ejecuta al final de todo el proceso de encendido.
        // Aquí estamos 100% seguros de que el ConfigManager ya leyó el disco duro.
        ActualizarUI();
    }
    void OnEnable()
    {
        // El OnEnable se ejecuta cada vez que entramos a esta pantalla. Por si el usuario hizo cambios en otra parte del juego, o simplemente para refrescar la UI.
        ActualizarUI();
        if (serial != null)
            serial.AlRecibirNuevosDatos += EscucharParaMapear;
    }

    void OnDisable()
    {
        // Al salir de esta pantalla, nos "desuscribimos" del evento para liberar RAM y evitar errores.
        if (serial != null)
            serial.AlRecibirNuevosDatos -= EscucharParaMapear;

        accionEsperando = "";
    }
    // Esta función toma los valores actuales del ConfigManager y los muestra en la UI. Se llama al iniciar y cada vez que entramos a esta pantalla.
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

    // EVENTOS DE LOS SLIDERS DE SENSIBILIDAD
    public void CambiarSensibilidadInsercion(float val)
    {
        if (config == null) return;
        config.sensInsercion = val;
        ActualizarTextosSensibilidad();
        // NOTA: Ya no guardamos aquí. Solo actualizamos la variable en memoria.
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
    // Esta función actualiza los textos que muestran el valor numérico de la sensibilidad, cada vez que se cambia un slider o se carga la pantalla.
    private void ActualizarTextosSensibilidad()
    {
        if (config == null) return;
        if (txtValIns) txtValIns.text = config.sensInsercion.ToString("F1") + "x";
        if (txtValTor) txtValTor.text = config.sensTorsion.ToString("F1") + "x";
        if (txtValVol) txtValVol.text = config.sensVolantes.ToString("F1") + "x";
    }

    // EVENTOS DE LOS BOTONES DE MAPEO
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

    // Cada vez que se presiona un botón de mapeo, esta función prepara el sistema para escuchar el siguiente input del hardware y asignarlo a la acción correspondiente.
    private void PrepararMapeo(string accion, bool requiereEje, TextMeshProUGUI textoUI)
    {
        accionEsperando = accion;
        esperandoEje = requiereEje;
        textoBotonActivo = textoUI;
        textoBotonActivo.text = "<color=black>Moviendo...</color>";
        txtFeedback.text = requiereEje ? "Gira o empuja el control físico..." : "Presiona el botón físico...";
    }
    // Esta función se ejecuta cada vez que el SerialManager recibe datos nuevos del hardware, pero SOLO asignará un nuevo mapeo si estamos en modo "esperando un input". De lo contrario, solo ignora los datos.
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
    // Esta función analiza los datos del hardware para detectar cuál fue el último control que se movió o botón que se presionó, y devuelve un string con el formato adecuado para asignar al mapeo (por ejemplo, "INS_+" o "B1"). Si no se detecta ningún movimiento relevante, devuelve una cadena vacía.
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
    // Esta función asigna el nuevo mapeo detectado a la variable correspondiente del ConfigManager, pero SOLO en memoria. El cambio no se guarda en el disco duro hasta que el usuario presione el botón "Guardar".
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

    // BOTÓN EXCLUSIVO DE GUARDADO
    public void BotonGuardarCambios()
    {
        if (config != null)
        {
            config.GuardarAjustes();
            txtFeedback.text = "<color=green><b>¡Configuración guardada exitosamente!</b></color>";
        }
    }
    // BOTÓN EXCLUSIVO DE RESTABLECER
    public void BotonRestablecer()
    {
        if (config != null)
        {
            config.RestablecerValores();
            ActualizarUI();
            txtFeedback.text = "<color=white>Valores restablecidos a fábrica.</color>";
        }
    }
    // BOTÓN DE PRUEBA DE VIBRACIÓN (solo para el STM, que tiene un comando específico para eso)
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