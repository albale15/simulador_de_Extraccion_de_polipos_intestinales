using UnityEngine;
using TMPro;

public class ConnectivityUI : MonoBehaviour
{
    [Header("Conexiones Directas")]
    public SerialManager serial;
    public ConfigManager config;

    [Header("UI Elements")]
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtFeedbackAcciones;
    public GameObject botonReconectar;

    private float _tiempoConectado = 0f;
    private SerialManager.EstadoConexion _estadoAnterior = SerialManager.EstadoConexion.Iniciando;

    void Awake()
    {
        // LA CURA AL "MISSING": Ignoramos al inspector y cazamos a los inmortales a la fuerza.
        serial = SerialManager.instancia;
        config = ConfigManager.instancia;
    }

    void Start()
    {
        // Si el Awake falló porque se ejecutaron al revés, hacemos un re-intento seguro.
        if (serial == null) serial = SerialManager.instancia;
        if (config == null) config = ConfigManager.instancia;

        if (serial != null)
        {
            _estadoAnterior = SerialManager.EstadoConexion.Iniciando;
            ActualizarPanelVisual(serial.estadoActual);
        }
    }

    void OnEnable()
    {
        // Tercer blindaje por si la UI se enciende tarde
        if (config == null) config = ConfigManager.instancia;

        if (config != null)
        {
            config.AlRecibirDatosProcesados += EscucharDatosLimpios;
        }
    }

    void OnDisable()
    {
        if (config != null)
        {
            config.AlRecibirDatosProcesados -= EscucharDatosLimpios;
        }
    }

    private void EscucharDatosLimpios(DatosProcesados datos)
    {
        string feedback = "<b>Monitor de Acciones Mapeadas:</b>\n";

        if (datos.volanteXFinal > 0) feedback += "Volante X: Girando Derecha \n";
        else if (datos.volanteXFinal < 0) feedback += "Volante X: Girando Izquierda \n";

        if (datos.volanteYFinal > 0) feedback += "Volante Y: Girando Arriba \n";
        else if (datos.volanteYFinal < 0) feedback += "Volante Y: Girando Abajo \n";

        if (datos.insercionFinal > 0) feedback += "Tubo: Insertando \n";
        else if (datos.insercionFinal < 0) feedback += "Tubo: Retrayendo \n";

        if (datos.torsionFinal > 0) feedback += "Torque: Girando Derecha \n";
        else if (datos.torsionFinal < 0) feedback += "Torque: Girando Izquierda \n";

        if (datos.botonFreeze) feedback += "<color=#00FFFF>Acción: Freeze activada</color>\n";
        if (datos.botonCapture) feedback += "<color=#FFD700>Acción: Capture activada</color>\n";
        if (datos.botonZoom) feedback += "<color=#FF8C00>Acción: Zoom activado</color>\n";
        if (datos.botonSuccion) feedback += "<color=#1E90FF>Acción: Succión activada</color>\n";

        txtFeedbackAcciones.text = feedback;
    }

    void Update()
    {
        if (serial == null) return;

        if (serial.estadoActual != _estadoAnterior)
        {
            _estadoAnterior = serial.estadoActual;
            _tiempoConectado = 0f;
            ActualizarPanelVisual(serial.estadoActual);
        }

        if (serial.estadoActual == SerialManager.EstadoConexion.Conectado)
        {
            _tiempoConectado += Time.deltaTime;
            if (_tiempoConectado > 5f && txtEstado.text != "")
            {
                txtEstado.text = "";
            }
        }
    }

    private void ActualizarPanelVisual(SerialManager.EstadoConexion estado)
    {
        switch (estado)
        {
            case SerialManager.EstadoConexion.Buscando:
                txtEstado.text = "<color=yellow>Buscando control...</color>\n<size=20>" + serial.mensajeInterfaz + "</size>";
                botonReconectar.SetActive(false);
                if (txtFeedbackAcciones.text != "") txtFeedbackAcciones.text = "";
                break;

            case SerialManager.EstadoConexion.Conectado:
                txtEstado.text = "Centro de mando: <color=green>CONECTADO</color> (" + serial.puertoActivo + ")";
                botonReconectar.SetActive(false);
                break;

            case SerialManager.EstadoConexion.Error:
                txtEstado.text = "Centro de mando: <color=red>DESCONECTADO</color>\n<size=20>Por favor, revisa el cable USB.</size>";
                botonReconectar.SetActive(true);
                if (txtFeedbackAcciones.text != "") txtFeedbackAcciones.text = "";
                break;

            case SerialManager.EstadoConexion.Iniciando:
                txtEstado.text = "";
                botonReconectar.SetActive(false);
                break;
        }
    }

    public void ClickReconectar()
    {
        if (serial != null) serial.IniciarBusqueda();
    }
}