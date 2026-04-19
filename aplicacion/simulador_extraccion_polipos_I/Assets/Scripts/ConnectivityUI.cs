using UnityEngine;
using TMPro;

public class ConnectivityUI : MonoBehaviour
{
    public SerialManager serial;

    [Header("UI Elements")]
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtJson;
    public GameObject botonReconectar;
    public GameObject panelCargando;

    // Variables privadas para controlar el tiempo
    private float _tiempoConectado = 0f;
    private SerialManager.EstadoConexion _estadoAnterior;

    void Update()
    {
        if (serial == null) return;

        // 1. Detectar si el estado acaba de cambiar para reiniciar el cronómetro
        if (serial.estadoActual != _estadoAnterior)
        {
            _estadoAnterior = serial.estadoActual;
            _tiempoConectado = 0f;
        }

        // 2. Control del panel inicial
        if (panelCargando != null)
        {
            panelCargando.SetActive(serial.estadoActual == SerialManager.EstadoConexion.Iniciando);
        }

        // 3. Máquina de estados visual
        switch (serial.estadoActual)
        {
            case SerialManager.EstadoConexion.Buscando:
                txtEstado.text = "<color=yellow>Buscando control...</color>\n<size=20>" + serial.mensajeInterfaz + "</size>";
                botonReconectar.SetActive(false);
                break;

            case SerialManager.EstadoConexion.Conectado:
                // Sumamos el tiempo que ha pasado desde que se conectó
                _tiempoConectado += Time.deltaTime;

                if (_tiempoConectado <= 5f)
                {
                    // Primeros 5 segundos: Mostramos el éxito
                    txtEstado.text = "Centro de mando: <color=green>CONECTADO</color> (" + serial.puertoActivo + ")";
                }
                else
                {
                    // Después de 5 segundos: El texto desaparece para limpiar la pantalla
                    txtEstado.text = "";
                }

                botonReconectar.SetActive(false);
                break;

            case SerialManager.EstadoConexion.Error:
                // Si el cable se desconecta repentinamente o no lo encuentra, vuelve aquí
                txtEstado.text = "Centro de mando: <color=red>DESCONECTADO</color>\n<size=20>Por favor, revisa el cable USB.</size>";
                botonReconectar.SetActive(true); // Activa el botón de reconectar
                break;
        }

        // (Opcional) Mostrar el JSON solo para pruebas. 
        // Si quieres que esto también desaparezca después de 5 segundos, puedes agregar la condición.
        if (serial.estadoActual == SerialManager.EstadoConexion.Conectado)
        {
            txtJson.text = "RAW JSON:\n" + serial.ultimoJsonRecibido;
        }
        else
        {
            txtJson.text = "";
        }
    }

    public void ClickReconectar()
    {
        serial.IniciarBusqueda();
    }
}