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
    public GameObject panelCargando;

    private float _tiempoConectado = 0f;
    private SerialManager.EstadoConexion _estadoAnterior;

    void Awake()
    {
        // contra Condición de Carrera
        if (serial == null) serial = FindObjectOfType<SerialManager>();
        if (config == null) config = FindObjectOfType<ConfigManager>();
    }

    // ESCUCHAMOS CONFIG MANAGER 
    void OnEnable()
    {
        if (config != null)
        {
            // Nos suscribimos a los datos ya limpios, mapeados y multiplicados
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

    // Usamos DatosProcesados en lugar de DatosHardware
    private void EscucharDatosLimpios(DatosProcesados datos)
    {
        string feedback = "<b>Monitor de Acciones Mapeadas:</b>\n";

        // solo preguntamos si el valor es mayor o menor a cero
        if (datos.volanteXFinal > 0) feedback += "Volante X: Girando Derecha \n";
        else if (datos.volanteXFinal < 0) feedback += "Volante X: Girando Izquierda \n";

        if (datos.volanteYFinal > 0) feedback += "Volante Y: Girando Arriba \n";
        else if (datos.volanteYFinal < 0) feedback += "Volante Y: Girando Abajo \n";

        if (datos.insercionFinal > 0) feedback += "Tubo: Insertando \n";
        else if (datos.insercionFinal < 0) feedback += "Tubo: Retrayendo \n";

        if (datos.torsionFinal > 0) feedback += "Torque: Girando Derecha \n";
        else if (datos.torsionFinal < 0) feedback += "Torque: Girando Izquierda \n";

        // Los botones ahora son las ACCIONES, no el hardware físico
        if (datos.botonFreeze) feedback += "<color=#00FFFF>Acción: Freeze activada</color>\n";
        if (datos.botonCapture) feedback += "<color=#FFD700>Acción: Capture activada</color>\n";
        if (datos.botonZoom) feedback += "<color=#FF8C00>Acción: Zoom activado</color>\n";
        if (datos.botonSuccion) feedback += "<color=#1E90FF>Acción: Succión activada</color>\n";

        txtFeedbackAcciones.text = feedback;
    }

    // GESTIÓN DE LA UI VISUAL 
    void Update()
    {
        if (serial == null) return;

        if (serial.estadoActual != _estadoAnterior)
        {
            _estadoAnterior = serial.estadoActual;
            _tiempoConectado = 0f;
        }

        if (panelCargando != null) panelCargando.SetActive(serial.estadoActual == SerialManager.EstadoConexion.Iniciando);

        switch (serial.estadoActual)
        {
            case SerialManager.EstadoConexion.Buscando:
                txtEstado.text = "<color=yellow>Buscando control...</color>\n<size=20>" + serial.mensajeInterfaz + "</size>";
                botonReconectar.SetActive(false);
                if (txtFeedbackAcciones.text != "") txtFeedbackAcciones.text = "";
                break;

            case SerialManager.EstadoConexion.Conectado:
                _tiempoConectado += Time.deltaTime;
                if (_tiempoConectado <= 5f) txtEstado.text = "Centro de mando: <color=green>CONECTADO</color> (" + serial.puertoActivo + ")";
                else txtEstado.text = "";
                botonReconectar.SetActive(false);
                break;

            case SerialManager.EstadoConexion.Error:
                txtEstado.text = "Centro de mando: <color=red>DESCONECTADO</color>\n<size=20>Por favor, revisa el cable USB.</size>";
                botonReconectar.SetActive(true);
                if (txtFeedbackAcciones.text != "") txtFeedbackAcciones.text = "";
                break;
        }
    }

    public void ClickReconectar()
    {
        serial.IniciarBusqueda();
    }
}