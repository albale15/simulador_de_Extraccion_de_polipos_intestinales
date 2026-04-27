using UnityEngine;

public class ZonaExtraccion : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. Verificamos si lo que chocó tiene el script de herramientas (nuestra cámara)
        SistemaHerramientas herramientas = other.GetComponent<SistemaHerramientas>();

        if (herramientas != null)
        {
            Debug.Log("<color=white>[Zona Extracción]: Punta del endoscopio detectada en la salida.</color>");

            // 2. ¿Lleva un pólipo colgado?
            if (herramientas.llevandoPolipo)
            {
                herramientas.llevandoPolipo = false;
                herramientas.poliposEliminados++;

                int poliposBorrados = 0;

                // 3. Limpiar los hijos (destruir la muestra)
                foreach (Transform hijo in herramientas.transform)
                {
                    PolipoInteractuable polipo = hijo.GetComponent<PolipoInteractuable>();
                    if (polipo != null)
                    {
                        Destroy(hijo.gameObject);
                        poliposBorrados++;
                    }
                }

                Debug.Log($"<color=green>[Éxito]: ¡Pólipo depositado en el laboratorio! Se eliminaron {poliposBorrados} modelos de la punta.</color>");
            }
            else
            {
                Debug.Log("<color=grey>[Zona Extracción]: El endoscopio entró/salió pero no traía muestras.</color>");
            }
        }
    }
}