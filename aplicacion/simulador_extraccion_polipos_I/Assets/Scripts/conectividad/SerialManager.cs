using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;
using System;

public class SerialManager : MonoBehaviour
{
    [Header("Configuración de Identidad")]
    public string firmaEsperada = "ID:ENDOSCOPIO_V1";

    [Header("Estado del Hardware")]
    public bool estaConectado = false;
    public string puertoActivo = "";
    public string ultimoJsonRecibido = "";

    private SerialPort _puerto;
    private Thread _hiloLectura;
    private bool _ejecutando = false;
    private ConcurrentQueue<string> _colaMensajes = new ConcurrentQueue<string>();

    void Start()
    {
        StartCoroutine(RutinaBusquedaAsincrona());
    }

    // 1. BÚSQUEDA FLUIDA: Escanea sin congelar Unity
    System.Collections.IEnumerator RutinaBusquedaAsincrona()
    {
        while (!estaConectado)
        {
            string[] puertos = SerialPort.GetPortNames();
            foreach (string nombrePuerto in puertos)
            {
                // Entramos al intento de conexión esperando que termine, pero sin congelar
                yield return StartCoroutine(IntentarConexionFluida(nombrePuerto));

                if (estaConectado) yield break; // Si ya conectó, detenemos la búsqueda
            }
            // Espera 1 segundo antes de volver a escanear todos los puertos
            yield return new WaitForSeconds(1f);
        }
    }

    // 2. EL APRETÓN DE MANOS SIN LAG
    System.Collections.IEnumerator IntentarConexionFluida(string nombrePuerto)
    {
        bool conexionExitosa = false;

        try
        {
            _puerto = new SerialPort(nombrePuerto, 115200) { ReadTimeout = 20, WriteTimeout = 20 };
            _puerto.Open();
            _puerto.DiscardInBuffer();
            _puerto.Write("?");
        }
        catch
        {
            if (_puerto != null && _puerto.IsOpen) _puerto.Close();
            yield break; // Salimos de esta corrutina y pasa al siguiente puerto
        }

        // AQUÍ ESTÁ LA MAGIA: Esperamos 0.1 segundos SIN detener a Unity
        yield return new WaitForSecondsRealtime(0.1f);

        try
        {
            if (_puerto != null && _puerto.IsOpen && _puerto.BytesToRead > 0)
            {
                string respuesta = _puerto.ReadExisting();

                if (respuesta.Contains(firmaEsperada))
                {
                    puertoActivo = nombrePuerto;
                    estaConectado = true;
                    _ejecutando = true;

                    // Iniciamos el hilo veloz para leer datos
                    _hiloLectura = new Thread(LecturaDeFondo);
                    _hiloLectura.IsBackground = true;
                    _hiloLectura.Start();
                    conexionExitosa = true;
                }
            }
        }
        catch { }

        // Si no era el endoscopio, cerramos el puerto amablemente
        if (!conexionExitosa)
        {
            if (_puerto != null && _puerto.IsOpen) _puerto.Close();
        }
    }

    // 3. EL HILO SECUNDARIO (Se mantiene igual, funcionaba perfecto)
    void LecturaDeFondo()
    {
        while (_ejecutando && _puerto != null && _puerto.IsOpen)
        {
            try
            {
                if (_puerto.BytesToRead > 0)
                {
                    string dato = _puerto.ReadLine();
                    _colaMensajes.Enqueue(dato);
                }
            }
            catch (TimeoutException) { }
            catch (Exception) { break; }
        }
    }

    // 4. EL DIBUJADO SEGURO
    void Update()
    {
        string mensajeFresco = "";
        while (_colaMensajes.TryDequeue(out string mensaje))
        {
            mensajeFresco = mensaje;
        }

        if (!string.IsNullOrEmpty(mensajeFresco))
        {
            ultimoJsonRecibido = mensajeFresco;
        }
    }

    void OnDisable()
    {
        _ejecutando = false;
        if (_hiloLectura != null && _hiloLectura.IsAlive) _hiloLectura.Join(200);
        if (_puerto != null && _puerto.IsOpen) _puerto.Close();
    }
}