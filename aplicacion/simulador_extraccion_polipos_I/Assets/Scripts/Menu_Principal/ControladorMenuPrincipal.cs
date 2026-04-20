using UnityEngine;
using UnityEngine.UI; // Para poder manejar botones 

public class ControladorMenuPrincipal : MonoBehaviour
{
    [Header("Pantallas del Menú (Telones)")]
    public GameObject pantallaBotonesPrincipales;
    public GameObject pantallaPreInicio;
    public GameObject pantallaConfiguracion;
    public GameObject pantallaHistorial;

    void Start()
    {
        // Al iniciar la aplicación, nos aseguramos de que solo el menú base esté visible
        MostrarPantallaPrincipal();
    }

    // --- FUNCIONES PARA ABRIR PANTALLAS ---
    // Estas funciones las conectaremos a los clics de los botones en Unity

    public void MostrarPantallaPrincipal()
    {
        ApagarTodasLasPantallas();
        pantallaBotonesPrincipales.SetActive(true);
    }

    public void MostrarPantallaPreInicio()
    {
        ApagarTodasLasPantallas();
        pantallaPreInicio.SetActive(true);
    }

    public void MostrarPantallaConfiguracion()
    {
        ApagarTodasLasPantallas();
        pantallaConfiguracion.SetActive(true);
    }

    public void MostrarPantallaHistorial()
    {
        ApagarTodasLasPantallas();
        pantallaHistorial.SetActive(true);
    }

    // Función interna: "Baja todos los telones" antes de abrir uno nuevo
    // Es más seguro apagarlos todos que intentar adivinar cuál estaba abierto
    private void ApagarTodasLasPantallas()
    {
        pantallaBotonesPrincipales.SetActive(false);
        pantallaPreInicio.SetActive(false);
        pantallaConfiguracion.SetActive(false);
        pantallaHistorial.SetActive(false);
    }

    // --- FUNCIÓN PARA SALIR ---
    public void SalirDeLaAplicacion()
    {
        Debug.Log("Cerrando el simulador...");

        // Cierra la app cuando ya está compilada (exe o linux)
        Application.Quit();

        // Este código es solo para que se vea que funciona dentro del Editor de Unity
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}