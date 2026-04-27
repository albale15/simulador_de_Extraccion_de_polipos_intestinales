using UnityEngine;

public class PolipoInteractuable : MonoBehaviour
{
    public enum TipoPolipo { Yamada1, Yamada2, Yamada3, Yamada4 }

    [Header("Configuración del Pólipo")]
    public TipoPolipo tipo;
    public bool yaCortado = false;

    public void SerCortado(Transform puntaEndoscopio)
    {
        yaCortado = true;
        Debug.Log($"<color=cyan>[Pólipo] Iniciando proceso de corte para un {tipo}.</color>");

        if (tipo == TipoPolipo.Yamada1 || tipo == TipoPolipo.Yamada2)
        {
            Debug.Log($"<color=cyan>[Pólipo] {tipo} destruido/ocultado exitosamente.</color>");
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"<color=cyan>[Pólipo] {tipo} emparentado a la cámara. ¡Listo para extraer!</color>");
            transform.SetParent(puntaEndoscopio);
            transform.localPosition = new Vector3(0, 0, 0.05f);
            transform.localScale *= 0.5f;
        }
    }
}