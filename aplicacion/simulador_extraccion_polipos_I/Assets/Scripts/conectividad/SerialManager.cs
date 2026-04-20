using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;
using System;

public class SerialManager : MonoBehaviour
{
    public enum EstadoConexion { Iniciando, Buscando, Conectado, Error }

    [Header("Estado del Hardware")]
    public EstadoConexion estadoActual = EstadoConexion.Iniciando;
    public string mensajeInterfaz = "Cargando componentes...";
    public string puertoActivo = "";
    public string ultimoJsonRecibido = "";

    [Header("Configuración")]
    public string firmaEsperada = "ID:ENDOSCOPIO_V1";

    private SerialPort _puerto;
    // Ahora tenemos DOS hilos secundarios. Uno busca, otro lee.
    private Thread _hiloBusqueda;
    private Thread _hiloLectura;
    private bool _ejecutando = false;
    private ConcurrentQueue<string> _colaMensajes = new ConcurrentQueue<string>();

    void Start()
    {
        Invoke(nameof(IniciarBusqueda), 2.5f);
    }

    public void IniciarBusqueda()
    {
        if (estadoActual == EstadoConexion.Buscando) return;

        // LA MAGIA OCURRE AQUÍ: 
        // Despachamos la tarea pesada a un hilo completamente separado de Unity.
        _hiloBusqueda = new Thread(RutinaBusquedaEnFondo);
        _hiloBusqueda.IsBackground = true;
        _hiloBusqueda.Start();
    }

    // --- ESTA FUNCIÓN CORRE FUERA DE UNITY ---
    void RutinaBusquedaEnFondo()
    {
        estadoActual = EstadoConexion.Buscando;
        mensajeInterfaz = "Iniciando escaneo de puertos...";

        // El Cooldown para Windows ahora usa Thread.Sleep 
        // Como estamos fuera de Unity, esto NO congela tu pantalla.
        Thread.Sleep(1500);

        string[] puertos = SerialPort.GetPortNames();

        if (puertos.Length == 0)
        {
            estadoActual = EstadoConexion.Error;
            mensajeInterfaz = "No se detectaron puertos USB.";
            return;
        }

        foreach (string nombrePuerto in puertos)
        {
            mensajeInterfaz = "Verificando " + nombrePuerto + "...";

            if (IntentarConexionFondo(nombrePuerto))
            {
                puertoActivo = nombrePuerto;
                mensajeInterfaz = "Sistema listo en " + nombrePuerto;
                estadoActual = EstadoConexion.Conectado;

                // Si conectó con éxito, iniciamos el hilo de lectura continua
                _ejecutando = true;
                _hiloLectura = new Thread(LecturaDeFondo) { IsBackground = true };
                _hiloLectura.Start();
                return; // Terminamos la búsqueda
            }
        }

        // Si terminó de revisar todos los puertos y no conectó
        estadoActual = EstadoConexion.Error;
        mensajeInterfaz = "Endoscopio no encontrado.";
    }

    bool IntentarConexionFondo(string nombrePuerto)
    {
        try
        {
            _puerto = new SerialPort(nombrePuerto, 115200) { ReadTimeout = 50, WriteTimeout = 50 };

            // EL ASESINO DEL LAG: Esta línea ya no congela Unity
            _puerto.Open();

            _puerto.DiscardInBuffer();
            _puerto.Write("?");
        }
        catch
        {
            CerrarPuerto();
            return false;
        }

        Thread.Sleep(150); // Pausa física para que la STM32 responda

        try
        {
            if (_puerto != null && _puerto.IsOpen && _puerto.BytesToRead > 0)
            {
                string respuesta = _puerto.ReadExisting();
                if (respuesta.Contains(firmaEsperada))
                {
                    return true; // ¡Conectado!
                }
            }
        }
        catch { }

        CerrarPuerto();
        return false;
    }

    // --- EL HILO DE LECTURA (Se mantiene igual) ---
    void LecturaDeFondo()
    {
        while (_ejecutando && _puerto != null && _puerto.IsOpen)
        {
            try
            {
                string dato = _puerto.ReadLine();
                _colaMensajes.Enqueue(dato);
            }
            catch (System.IO.IOException)
            {
                estadoActual = EstadoConexion.Error;
                mensajeInterfaz = "¡CONEXIÓN PERDIDA! El cable se desconectó.";
                CerrarTodo();
                break;
            }
            catch (TimeoutException) { }
            catch (Exception) { break; }
        }
    }

    // --- HILO PRINCIPAL DE UNITY (Solo actualiza datos visuales) ---
    void Update()
    {
        string mensajeFresco = "";
        while (_colaMensajes.TryDequeue(out string mensaje)) mensajeFresco = mensaje;
        if (!string.IsNullOrEmpty(mensajeFresco)) ultimoJsonRecibido = mensajeFresco;
    }

    void CerrarPuerto()
    {
        if (_puerto != null)
        {
            try
            {
                if (_puerto.IsOpen) _puerto.Close();
                _puerto.Dispose();
            }
            catch { }
            finally { _puerto = null; }
        }
    }

    void CerrarTodo()
    {
        _ejecutando = false;
        // Evitamos que el hilo intente matarse a sí mismo y cause un deadlock
        if (_hiloLectura != null && _hiloLectura.IsAlive && Thread.CurrentThread != _hiloLectura)
        {
            _hiloLectura.Join(200);
        }
        CerrarPuerto();
    }
    // funcion para enviar datos a la stm32
    public void EnviarDato(string mensaje)
    {
        // Solo enviamos si la máquina de estados dice que estamos conectados
        if (estadoActual == EstadoConexion.Conectado && _puerto != null && _puerto.IsOpen)
        {
            try
            {
                _puerto.Write(mensaje);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error al enviar dato al hardware: " + e.Message);
            }
        }
    }

    void OnDestroy() => CerrarTodo();
    void OnApplicationQuit() => CerrarTodo();
}