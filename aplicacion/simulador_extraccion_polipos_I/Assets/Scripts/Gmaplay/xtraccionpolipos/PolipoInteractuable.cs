using UnityEngine;

public class PolipoInteractuable : MonoBehaviour
{
    public enum TipoPolipo { Yamada1, Yamada2, Yamada3, Yamada4 }
    public enum EstadoPolipo { Intacto, CortadoSuelto, Capturado }

    [Header("Configuración del Pólipo")]
    public TipoPolipo tipo;
    public EstadoPolipo estadoActual = EstadoPolipo.Intacto;
    // Tamaño real asignado por el Manager 
    [HideInInspector]
    public float tamanoMilimetros;

    // Bandera para protocolo médico
    [HideInInspector]
    public bool fueFotografiado = false;
    // Se llama cuando la pinza o asa terminan de cortar

    // Ajusta la escala real del objeto en Unity según el tamaño médico asignado
    public void InicializarTamanoClinico(float milimetros)
    {
        tamanoMilimetros = milimetros;

        // Regla de tres o factor de escala visual para que el estudiante note la diferencia en el monitor.
        // Si 5mm es la escala base (1,1,1), un pólipo de 12mm será proporcionalmente más grande.
        float factorEscalaVisual = milimetros / 5f;
        transform.localScale = Vector3.one * factorEscalaVisual;
    }

    public void ProcesarCorte()
    {
        if (tamanoMilimetros <= 5f)
        {
            // Las pinzas frías lo extirpan/destruyen directamente
            Debug.Log($"<color=cyan>[Pólipo] {tipo} de {tamanoMilimetros:F1}mm removido con pinza fría.</color>");
            gameObject.SetActive(false);
        }
        else
        {
            // El asa lo corta, pero lo deja suelto para la succión
            estadoActual = EstadoPolipo.CortadoSuelto;
            Debug.Log($"<color=yellow>[Pólipo] {tipo} de {tamanoMilimetros:F1}mm cortado con asa. Esperando succión...</color>");

            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null) rend.material.color = Color.gray;
        }
    }

    // Se llama cuando atrapamos el pólipo con el asa (Botón 4)
    public void SerAtrapado(Transform canalDeTrabajo)
    {
        estadoActual = EstadoPolipo.Capturado;
        Debug.Log($"<color=green>[Pólipo] {tipo} atrapado. ¡Llévalo a la salida!</color>");

        transform.SetParent(canalDeTrabajo);
        // Lo ponemos un poquito más afuera (0.04f) para que se vea colgado en el asa
        transform.localPosition = new Vector3(0, 0.03f, 0.2f);
        //transform.localScale *= 0.7f; // No lo encogemos tanto para que sea visible
    }
}