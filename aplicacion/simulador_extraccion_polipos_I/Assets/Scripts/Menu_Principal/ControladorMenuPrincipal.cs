using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ControladorMenuPrincipal : MonoBehaviour
{
    [Header("Pantalla de Carga Inicial (Splash)")]
    public GameObject pantallaSplashCarga;
    public Slider barraDeCarga;
    public TextMeshProUGUI txtEstadoCarga; // Para mostrar qué está haciendo ("Leyendo Disco...", "Buscando USB...")

    [Header("Pantallas del Menú (Telones)")]
    public GameObject pantallaBotonesPrincipales;
    public GameObject pantallaPreInicio;
    public GameObject pantallaConfiguracion;
    public GameObject pantallaHistorial;

    void Start()
    {
        ApagarTodasLasPantallas();
        pantallaSplashCarga.SetActive(true);

        // Iniciamos la carga basada en EVENTOS REALES, no en tiempo.
        StartCoroutine(RutinaDeCargaReal());
    }

    private IEnumerator RutinaDeCargaReal()
    {
        // --- FASE 1: Inicialización de la Interfaz ---
        if (barraDeCarga != null) barraDeCarga.value = 0.1f;
        if (txtEstadoCarga != null) txtEstadoCarga.text = "Iniciando sistema...";
        yield return null; // Pausamos 1 frame para asegurar que Unity dibuje la UI sin congelarse

        // --- FASE 2: Lectura de Disco (Archivos de Guardado) ---
        if (txtEstadoCarga != null) txtEstadoCarga.text = "Cargando configuraciones y base de datos...";
        // Esperamos a que los cerebros inmortales existan en la memoria RAM
        yield return new WaitUntil(() => ConfigManager.instancia != null && HistoryManager.instancia != null);
        if (barraDeCarga != null) barraDeCarga.value = 0.4f;

        // --- FASE 3: Búsqueda del Hardware (Lo más pesado) ---
        SerialManager serial = SerialManager.instancia;
        if (serial != null)
        {
            if (txtEstadoCarga != null) txtEstadoCarga.text = "Escaneando puertos USB físicos...";
            if (barraDeCarga != null) barraDeCarga.value = 0.6f;

            // Le damos la orden manual de encender el hilo secundario
            serial.IniciarBusqueda();

            // AQUÍ ESTÁ LA MAGIA DE OPTIMIZACIÓN:
            // La pantalla se quedará congelada en 60% HASTA que el SerialManager haya terminado de escanear.
            // En una PC Gamer esto tomará 1.5 segundos. En una laptop vieja tomará 4 segundos. No habrá crasheos.
            yield return new WaitUntil(() =>
                serial.estadoActual == SerialManager.EstadoConexion.Conectado ||
                serial.estadoActual == SerialManager.EstadoConexion.Error);
        }

        // --- FASE 4: Finalización ---
        if (txtEstadoCarga != null) txtEstadoCarga.text = "¡Sistema Listo!";
        if (barraDeCarga != null) barraDeCarga.value = 1.0f;

        // Pausa estética de medio segundo para que el humano logre ver que la barra llegó al 100%
        yield return new WaitForSeconds(0.5f);

        pantallaSplashCarga.SetActive(false);
        MostrarPantallaPrincipal();
    }

    // --- FUNCIONES PARA ABRIR PANTALLAS ---
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

    private void ApagarTodasLasPantallas()
    {
        if (pantallaBotonesPrincipales) pantallaBotonesPrincipales.SetActive(false);
        if (pantallaPreInicio) pantallaPreInicio.SetActive(false);
        if (pantallaConfiguracion) pantallaConfiguracion.SetActive(false);
        if (pantallaHistorial) pantallaHistorial.SetActive(false);
    }

    public void SalirDeLaAplicacion()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}