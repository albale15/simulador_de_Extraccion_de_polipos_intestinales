using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfigUI : MonoBehaviour
{
    public SerialManager serial;

    [Header("Controles UI")]
    public Slider sliderSensIns;
    public Toggle toggleInvIns;
    public TextMeshProUGUI txtValorSens;

    void Start()
    {
        // Inicializar la UI con lo que ya está guardado
        if (sliderSensIns != null) sliderSensIns.value = ConfigManager.instancia.sensInsercion;
        if (toggleInvIns != null) toggleInvIns.isOn = ConfigManager.instancia.invertirInsercion;
    }

    public void ActualizarSensibilidad(float valor)
    {
        ConfigManager.instancia.sensInsercion = valor;
        if (txtValorSens != null) txtValorSens.text = valor.ToString("F2");
    }

    public void ActualizarInversion(bool valor)
    {
        ConfigManager.instancia.invertirInsercion = valor;
    }

    // BOTÓN: Probar Vibración
    public void ProbarVibracion()
    {
        // Usamos la nueva máquina de estados para verificar la conexión
        if (serial != null && serial.estadoActual == SerialManager.EstadoConexion.Conectado)
        {
            // Ahora este método sí existe en el SerialManager
            serial.EnviarDato("V1:100\n");
            Debug.Log("Enviando pulso de vibración de prueba a la STM32...");
        }
        else
        {
            Debug.LogWarning("No se puede vibrar: El endoscopio no está conectado.");
        }
    }
}