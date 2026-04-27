using UnityEngine;

public class ZonaExtraccion : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. Verificamos si lo que chocó tiene el script de herramientas (nuestra cámara/canal de trabajo)
        // Ojo: sometimes the collider is in a parent/child, so we use GetComponentInParent to be safe.
        SistemaHerramientas herramientas = other.GetComponentInParent<SistemaHerramientas>();

        if (herramientas != null)
        {
            // 2. ¿Lleva un pólipo colgado?
            if (herramientas.llevandoPolipo)
            {
                herramientas.llevandoPolipo = false;

                int poliposBorrados = 0;

                // 3. Limpiar los hijos del canal de trabajo
                foreach (Transform hijo in herramientas.canalDeTrabajo)
                {
                    PolipoInteractuable polipo = hijo.GetComponent<PolipoInteractuable>();
                    if (polipo != null)
                    {
                        // Sumamos este pólipo específico al contador de la herramienta
                        herramientas.SumarPolipoEliminado(polipo.tipo);

                        Destroy(hijo.gameObject);
                        poliposBorrados++;
                    }
                }

                Debug.Log($"<color=green>[Éxito]: ¡Pólipo depositado en el laboratorio! Se eliminaron {poliposBorrados} modelos de la punta.</color>");
            }
        }
    }
}