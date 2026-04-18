using UnityEngine;
using TMPro;

public class ConnectivityUI : MonoBehaviour
{
    public SerialManager serial;

    [Header("UI Elements")]
    public TextMeshProUGUI txtEstado; // Texto 1: "Conectado"
    public TextMeshProUGUI txtJson;   // Texto 2: El JSON crudo

    void Update()
    {
        if (serial == null) return;

        // Actualizar Texto 1 (Estado)
        if (serial.estaConectado)
        {
            txtEstado.text = "Centro de mando:: <color=green>CONECTADO</color> (" + serial.puertoActivo + ")";
        }
        else
        {
            txtEstado.text = "Centro de mando:: <color=yellow>BUSCANDO CONTROL...</color>";
        }

        // Actualizar Texto 2 (Monitor JSON)
        txtJson.text = "RAW JSON:\n" + serial.ultimoJsonRecibido;
    }
}