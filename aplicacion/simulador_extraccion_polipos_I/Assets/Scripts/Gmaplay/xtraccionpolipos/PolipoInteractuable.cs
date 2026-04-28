using UnityEngine;

public class PolipoInteractuable : MonoBehaviour
{
    public enum TipoPolipo { Yamada1, Yamada2, Yamada3, Yamada4 }
    public enum EstadoPolipo { Intacto, CortadoSuelto, Capturado } // NUEVOS ESTADOS

    [Header("Configuración del Pólipo")]
    public TipoPolipo tipo;
    public EstadoPolipo estadoActual = EstadoPolipo.Intacto;
    // Bandera para protocolo médico
    [HideInInspector]
    public bool fueFotografiado = false;
    // Se llama cuando la pinza o asa terminan de cortar
    public void ProcesarCorte()
    {
        if (tipo == TipoPolipo.Yamada1 || tipo == TipoPolipo.Yamada2)
        {
            // Las pinzas frías lo destruyen directamente
            Debug.Log($"<color=cyan>[Pólipo] {tipo} destruido por pinza fría.</color>");
            gameObject.SetActive(false);
        }
        else
        {
            // El asa lo corta, pero lo deja suelto para la succión
            estadoActual = EstadoPolipo.CortadoSuelto;
            Debug.Log($"<color=yellow>[Pólipo] {tipo} cortado. Esperando succión...</color>");

            // Opcional: Cambiarle el color un poco para que el jugador sepa que ya está cortado quemado
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null) rend.material.color = Color.gray;
        }
    }

    // Se llama cuando presionamos el botón de succión (Tecla 5)
    public void SerSuccionado(Transform canalDeTrabajo)
    {
        estadoActual = EstadoPolipo.Capturado;
        Debug.Log($"<color=green>[Pólipo] {tipo} succionado. ¡Llévalo a la salida!</color>");

        transform.SetParent(canalDeTrabajo);
        // Lo ponemos justo en la "boca" del canal de trabajo
        transform.localPosition = new Vector3(0, 0, 0.02f);
        transform.localScale *= 0.5f;
    }
}