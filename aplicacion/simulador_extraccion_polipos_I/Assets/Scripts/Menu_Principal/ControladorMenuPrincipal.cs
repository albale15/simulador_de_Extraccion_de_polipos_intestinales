using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ControladorMenuPrincipal : MonoBehaviour
{
    [Header("Pantalla de Carga Inicial (Splash)")]
    public GameObject pantallaSplashCarga;
    public Slider barraDeCarga;
    public TextMeshProUGUI txtEstadoCarga;

    [Header("Pantallas del Menú (Telones)")]
    public GameObject pantallaBotonesPrincipales;
    public GameObject pantallaPreInicio;
    public GameObject pantallaConfiguracion;
    public GameObject pantallaHistorial;

    void Start()
    {
        ApagarTodasLasPantallas();
        pantallaSplashCarga.SetActive(true);

        StartCoroutine(RutinaDeCargaReal());
    }

    private IEnumerator RutinaDeCargaReal()
    {
        // --- FASE 1: Inicialización ---
        if (barraDeCarga != null) barraDeCarga.value = 0.1f;
        if (txtEstadoCarga != null) txtEstadoCarga.text = "Iniciando sistema...";
        yield return null;

        // --- FASE 2: Lectura de Disco ---
        if (txtEstadoCarga != null) txtEstadoCarga.text = "Cargando configuraciones y base de datos...";
        yield return new WaitUntil(() => ConfigManager.instancia != null && HistoryManager.instancia != null);
        if (barraDeCarga != null) barraDeCarga.value = 0.4f;

        // --- FASE 3: Búsqueda del Hardware ---
        SerialManager serial = SerialManager.instancia;
        if (serial != null)
        {
            pantallaSplashCarga.SetActive(true);

            // AQUÍ ESTÁ LA SOLUCIÓN: Si volvemos del juego y ya estaba conectado, ¡NO buscamos de nuevo!
            if (serial.estadoActual == SerialManager.EstadoConexion.Conectado)
            {
                if (txtEstadoCarga != null) txtEstadoCarga.text = "Hardware ya conectado. Restaurando sesión...";
                if (barraDeCarga != null) barraDeCarga.value = 0.8f;
                yield return new WaitForSeconds(0.5f); // Pausa estética breve
            }
            else
            {
                // Si no estaba conectado, hacemos el escaneo normal
                if (txtEstadoCarga != null) txtEstadoCarga.text = "Escaneando puertos USB físicos...";
                if (barraDeCarga != null) barraDeCarga.value = 0.6f;

                serial.IniciarBusqueda();

                yield return new WaitUntil(() =>
                    serial.estadoActual == SerialManager.EstadoConexion.Conectado ||
                    serial.estadoActual == SerialManager.EstadoConexion.Error);
            }
        }

        // --- FASE 4: Finalización ---
        if (txtEstadoCarga != null) txtEstadoCarga.text = "¡Sistema Listo!";
        if (barraDeCarga != null) barraDeCarga.value = 1.0f;

        yield return new WaitForSeconds(0.5f);

        pantallaSplashCarga.SetActive(false);
        MostrarPantallaPrincipal();
    }

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