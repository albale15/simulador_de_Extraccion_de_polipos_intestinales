using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    // --- EL MÉTODO BLINDADO PARA ATRAPAR SINGLETONS ---
    // --- MÉTODO BLINDADO DE CONEXIÓN ---
    private void ConectarSistemas()
    {
        // 1. Forzamos a usar el Singleton Inmortal (ignorando cualquier clon muerto del inspector)
        if (SerialManager.instancia != null)
        {
            serial = SerialManager.instancia;
        }
        else if (serial == null)
        {
            serial = FindObjectOfType<SerialManager>();
        }

        // 2. Lo mismo para el ConfigManager
        if (ConfigManager.instancia != null)
        {
            config = ConfigManager.instancia;
        }
        else if (config == null)
        {
            config = FindObjectOfType<ConfigManager>();
        }
    }

    void Awake()
    {
        ConectarSistemas();
    }

    void Start()
    {
        ConectarSistemas();
        if (botonReconectar != null)
        {
            Button btn = botonReconectar.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ClickReconectar);
            }
        }
        if (serial != null)
        {
            // Forzamos la actualización visual la primera vez que se carga
            _estadoAnterior = SerialManager.EstadoConexion.Iniciando;
            ActualizarPanelVisual(serial.estadoActual);
        }
    }

    void OnEnable()
    {
        ConectarSistemas();

        if (config != null)
        {
            // SEGURIDAD PARA SINGLETONS:
            // Desuscribimos antes de suscribir. Esto evita escuchar los datos dobles.
            config.AlRecibirDatosProcesados -= EscucharDatosLimpios;
            config.AlRecibirDatosProcesados += EscucharDatosLimpios;
        }

        if (serial != null)
        {
            // Forzamos actualizar visualmente si hubo un cambio mientras el menú estaba cerrado
            ActualizarPanelVisual(serial.estadoActual);
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
        if (txtFeedbackAcciones == null) return;

        string feedback = "<b>Monitor de Acciones Mapeadas:</b>\n";

        if (datos.volanteXFinal > 0) feedback += "Volante X: Girando Derecha \n";
        else if (datos.volanteXFinal < 0) feedback += "Volante X: Girando Izquierda \n";

        if (datos.volanteYFinal > 0) feedback += "Volante Y: Girando Arriba \n";
        else if (datos.volanteYFinal < 0) feedback += "Volante Y: Girando Abajo \n";

        if (datos.insercionFinal > 0) feedback += "Tubo: Insertando \n";
        else if (datos.insercionFinal < 0) feedback += "Tubo: Retrayendo \n";


        if (datos.botonFreeze) feedback += "<color=#00FFFF>Acción: Freeze activada</color>\n";
        if (datos.botonCapture) feedback += "<color=#FFD700>Acción: Capture activada</color>\n";
        if (datos.botonZoom) feedback += "<color=#FF8C00>Acción: Zoom activado</color>\n";
        if (datos.botonSuccion) feedback += "<color=#1E90FF>Acción: Succión activada</color>\n";

        if (datos.botonAccion) feedback += "<color=#FF69B4>Acción: Botón 5 (Acción) activado</color>\n";
        if (datos.botonLimpiado) feedback += "<color=#32CD32>Acción: Limpiado de lente activado</color>\n";
        txtFeedbackAcciones.text = feedback;
    }

    void Update()
    {
        if (serial == null) return;

        // Si el estado cambió desde la última vez, actualizamos el panel
        if (serial.estadoActual != _estadoAnterior)
        {
            _estadoAnterior = serial.estadoActual;
            _tiempoConectado = 0f;
            ActualizarPanelVisual(serial.estadoActual);
        }

        // FEEDBACK DE "BUSCANDO
        // El SerialManager actualiza su variable "mensajeInterfaz" constantemente.
        // Aquí le decimos a la UI que lea esa actualización frame a frame si está en modo "Buscando".
        if (serial.estadoActual == SerialManager.EstadoConexion.Buscando && txtEstado != null)
        {
            txtEstado.text = "<color=yellow>Buscando control...</color>\n<size=20>" + serial.mensajeInterfaz + "</size>";
        }

        // Lógica para borrar el mensaje "Conectado" después de 5 segundos
        if (serial.estadoActual == SerialManager.EstadoConexion.Conectado)
        {
            _tiempoConectado += Time.deltaTime;
            if (_tiempoConectado > 5f && txtEstado != null && txtEstado.text != "")
            {
                txtEstado.text = "";
            }
        }
    }

    private void ActualizarPanelVisual(SerialManager.EstadoConexion estado)
    {
        if (txtEstado == null || botonReconectar == null) return;

        switch (estado)
        {
            case SerialManager.EstadoConexion.Buscando:
                txtEstado.text = "<color=yellow>Buscando control...</color>\n<size=20>" + serial.mensajeInterfaz + "</size>";
                botonReconectar.SetActive(false);
                if (txtFeedbackAcciones != null && txtFeedbackAcciones.text != "") txtFeedbackAcciones.text = "";
                break;

            case SerialManager.EstadoConexion.Conectado:
                txtEstado.text = "Centro de mando: <color=green>CONECTADO</color> (" + serial.puertoActivo + ")";
                botonReconectar.SetActive(false);
                break;

            case SerialManager.EstadoConexion.Error:
                txtEstado.text = "Centro de mando: <color=red>DESCONECTADO</color>\n<size=20>Por favor, revisa el cable USB.</size>";
                botonReconectar.SetActive(true);
                if (txtFeedbackAcciones != null && txtFeedbackAcciones.text != "") txtFeedbackAcciones.text = "";
                break;

            case SerialManager.EstadoConexion.Iniciando:
                txtEstado.text = "";
                botonReconectar.SetActive(false);
                break;
        }
    }

    public void ClickReconectar()
    {
        Debug.Log("<color=cyan>[UI] Clic detectado en Botón Reconectar.</color>");

        if (serial != null)
        {
            Debug.Log("<color=cyan>[UI] SerialManager encontrado. Enviando orden de búsqueda...</color>");
            botonReconectar.SetActive(false);
            serial.IniciarBusqueda();
        }
        else
        {
            Debug.LogError("<color=red>[UI] ERROR FATAL: 'serial' es NULO. La UI perdió la conexión con el mánager.</color>");
        }
    }
}