using UnityEngine;

public class TutorialHighlight : MonoBehaviour
{
    public RectTransform arriba, abajo, izquierda, derecha;

    public void ResaltarElemento(RectTransform target)
    {
        if (target == null)
        {
            Desactivar();
            return;
        }

        gameObject.SetActive(true);

        // Obtenemos las propiedades de nuestro propio contenedor
        RectTransform miRect = GetComponent<RectTransform>();

        // 1. Obtenemos las 4 esquinas del botón en el espacio del MUNDO
        Vector3[] esquinasWorld = new Vector3[4];
        target.GetWorldCorners(esquinasWorld);

        // 2. Convertimos esas esquinas al espacio LOCAL de esta máscara
        Vector3 esquinaInfIzq = miRect.InverseTransformPoint(esquinasWorld[0]);
        Vector3 esquinaSupDer = miRect.InverseTransformPoint(esquinasWorld[2]);

        float targetXMin = esquinaInfIzq.x;
        float targetXMax = esquinaSupDer.x;
        float targetYMin = esquinaInfIzq.y;
        float targetYMax = esquinaSupDer.y;

        // --- EL TRUCO DEL "TELÓN INFINITO" ---
        // En lugar de leer los bordes de la pantalla (que fallan al cambiar el ratio a 16:10),
        // forzamos a los paneles a estirarse 10,000 unidades en todas las direcciones.
        // Así cubrimos el 100% de cualquier monitor, sin importar su tamaño o forma.
        float oversize = 10000f;
        float screenXMin = -oversize;
        float screenXMax = oversize;
        float screenYMin = -oversize;
        float screenYMax = oversize;

        // 4. Acomodamos los 4 paneles oscuros formando el marco perfecto
        // Panel Arriba
        ConfigurarPanelLocal(arriba, screenXMin, targetYMax, screenXMax - screenXMin, screenYMax - targetYMax);
        // Panel Abajo
        ConfigurarPanelLocal(abajo, screenXMin, screenYMin, screenXMax - screenXMin, targetYMin - screenYMin);
        // Panel Izquierda
        ConfigurarPanelLocal(izquierda, screenXMin, targetYMin, targetXMin - screenXMin, targetYMax - targetYMin);
        // Panel Derecha
        ConfigurarPanelLocal(derecha, targetXMax, targetYMin, screenXMax - targetXMax, targetYMax - targetYMin);
    }

    private void ConfigurarPanelLocal(RectTransform panel, float xLocal, float yLocal, float ancho, float alto)
    {
        if (panel == null) return;

        // Centralizamos los anclajes para que el tamaño (sizeDelta) sea absoluto
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);

        // Ponemos el pivote en la esquina inferior izquierda para posicionar fácil
        panel.pivot = Vector2.zero;

        // Usamos localPosition (coordenadas relativas al Canvas) en lugar de position (coordenadas del mundo)
        panel.localPosition = new Vector3(xLocal, yLocal, 0);

        // Aplicamos el tamaño calculado
        panel.sizeDelta = new Vector2(ancho, alto);
    }

    public void Desactivar()
    {
        gameObject.SetActive(false);
    }
}