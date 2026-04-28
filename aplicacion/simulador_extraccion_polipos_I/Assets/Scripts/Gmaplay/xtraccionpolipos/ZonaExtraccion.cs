using UnityEngine;

public class ZonaExtraccion : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        SistemaHerramientas herramientas = other.GetComponentInParent<SistemaHerramientas>();
        if (herramientas != null)
        {
            herramientas.enZonaExtraccion = true;
            Debug.Log("<color=white>[Zona Extracción]: Punta en posición. Apague la succión (Botón 4) para depositar la muestra.</color>");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        SistemaHerramientas herramientas = other.GetComponentInParent<SistemaHerramientas>();
        if (herramientas != null)
        {
            herramientas.enZonaExtraccion = false;
        }
    }
}