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

        // para asegurar que oscurezca todo menos el botón.
        //transform.SetAsLastSibling();

        // 1. Obtenemos las 4 esquinas reales del botón en la pantalla
        Vector3[] esquinas = new Vector3[4];
        target.GetWorldCorners(esquinas);

        float xMin = esquinas[0].x;
        float xMax = esquinas[2].x;
        float yMin = esquinas[0].y;
        float yMax = esquinas[1].y;

        float anchoPantalla = Screen.width;
        float altoPantalla = Screen.height;

        // 2. Acomodamos los paneles negros para formar el "marco"
        ConfigurarPanel(arriba, 0, yMax, anchoPantalla, altoPantalla - yMax);
        ConfigurarPanel(abajo, 0, 0, anchoPantalla, yMin);
        ConfigurarPanel(izquierda, 0, yMin, xMin, yMax - yMin);
        ConfigurarPanel(derecha, xMax, yMin, anchoPantalla - xMax, yMax - yMin);
    }

    // Esta función ignora cómo hayas dejado el panel en Unity y lo fuerza a ser perfecto
    private void ConfigurarPanel(RectTransform panel, float x, float y, float ancho, float alto)
    {
        if (panel == null) return;

        panel.pivot = Vector2.zero; // Fuerza el pivote a la esquina inferior izquierda
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.zero;
        panel.position = new Vector3(x, y, 0);
        panel.sizeDelta = new Vector2(ancho, alto);
    }

    public void Desactivar()
    {
        gameObject.SetActive(false);
    }
}