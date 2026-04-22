using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;
using System;

// 1. EL CONTENEDOR DE DATOS TRADUCIDOS
public class DatosHardware
{
    public int boton1, boton2, boton3, boton4, botonSuccion;
    public int volante1, volante2; // E1 y E2
    public int insercion, torsion; // INS y TOR
}

public class SerialManager : MonoBehaviour
{

    // entrada al puerto serial y traductor de datos para el endoscopio
    public static SerialManager instancia;

    public enum EstadoConexion { Iniciando, Buscando, Conectado, Error }

    [Header("Estado del Hardware")]
    public EstadoConexion estadoActual = EstadoConexion.Iniciando;
    public string mensajeInterfaz = "Cargando componentes...";
    public string puertoActivo = "";

    // Aquí guardaremos los datos ya traducidos y listos para usar
    public DatosHardware datosActuales = new DatosHardware();
    public string ultimoMensajeCrudo = ""; // Solo para depuración

    [Header("Configuración")]
    public string firmaEsperada = "ID:ENDOSCOPIO_V1";

    private SerialPort _puerto;
    private Thread _hiloBusqueda;
    private Thread _hiloLectura;
    private bool _ejecutando = false;
    private ConcurrentQueue<string> _colaMensajes = new ConcurrentQueue<string>();
    // CREAMOS EL EVENTO (control de emisiones)
    public event Action<DatosHardware> AlRecibirNuevosDatos;
    void Awake()
    {
        // Hacer que este objeto sobreviva al cambiar de escenas
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        //Invoke(nameof(IniciarBusqueda), 2.5f);
    }

    public void IniciarBusqueda()
    {
        if (estadoActual == EstadoConexion.Buscando) return;
        _hiloBusqueda = new Thread(RutinaBusquedaEnFondo) { IsBackground = true };
        _hiloBusqueda.Start();
    }

    void RutinaBusquedaEnFondo()
    {
        estadoActual = EstadoConexion.Buscando;
        mensajeInterfaz = "Iniciando escaneo de puertos...";
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

                _ejecutando = true;
                _hiloLectura = new Thread(LecturaDeFondo) { IsBackground = true };
                _hiloLectura.Start();
                return;
            }
        }

        estadoActual = EstadoConexion.Error;
        mensajeInterfaz = "Endoscopio no encontrado.";
    }

    bool IntentarConexionFondo(string nombrePuerto)
    {
        try
        {
            _puerto = new SerialPort(nombrePuerto, 115200) { ReadTimeout = 50, WriteTimeout = 50 };
            _puerto.Open();
            _puerto.DiscardInBuffer();
            _puerto.Write("?");
        }
        catch
        {
            CerrarPuerto();
            return false;
        }

        Thread.Sleep(150);

        try
        {
            if (_puerto != null && _puerto.IsOpen && _puerto.BytesToRead > 0)
            {
                string respuesta = _puerto.ReadExisting();
                if (respuesta.Contains(firmaEsperada)) return true;
            }
        }
        catch { }

        CerrarPuerto();
        return false;
    }

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

    void Update()
    {
        string mensajeFresco = "";
        // El Manager sí debe revisar el buzón en Update (es muy ligero)
        while (_colaMensajes.TryDequeue(out string mensaje)) mensajeFresco = mensaje;

        if (!string.IsNullOrEmpty(mensajeFresco))
        {
            ultimoMensajeCrudo = mensajeFresco;
            TraducirDatos(mensajeFresco);

            // ESTILO SOCKET.IO:
            // Si alguien está escuchando, le enviamos los datos ya traducidos.
            AlRecibirNuevosDatos?.Invoke(datosActuales);
        }
    }

    //EL MOTOR DE TRADUCCIÓN
    private void TraducirDatos(string mensajeCrudo)
    {
        try
        {
            // Cortamos el mensaje en pedazos usando los espacios
            string[] partes = mensajeCrudo.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string parte in partes)
            {
                // Cortamos cada pedazo por los dos puntos (ej: "B1:1" -> ["B1", "1"])
                string[] claveValor = parte.Split(':');
                if (claveValor.Length == 2)
                {
                    string clave = claveValor[0];
                    int valor = int.Parse(claveValor[1]);

                    // Asignamos el valor a la variable correcta de Unity
                    switch (clave)
                    {
                        case "B1": datosActuales.boton1 = valor; break;
                        case "B2": datosActuales.boton2 = valor; break;
                        case "B3": datosActuales.boton3 = valor; break;
                        case "B4": datosActuales.boton4 = valor; break;
                        case "Su": datosActuales.botonSuccion = valor; break;
                        case "E1": datosActuales.volante1 = valor; break;
                        case "E2": datosActuales.volante2 = valor; break;
                        case "INS": datosActuales.insercion = valor; break;
                        case "TOR": datosActuales.torsion = valor; break;
                    }
                }
            }
        }
        catch { /* Si llega un texto a medias o basura, lo ignoramos para no crashear */ }
    }
    //enviado de datos a la STM32 (ej: para activar vibración )
    public void EnviarDato(string mensaje)
    {
        if (estadoActual == EstadoConexion.Conectado && _puerto != null && _puerto.IsOpen)
        {
            try { _puerto.Write(mensaje); }
            catch (Exception e) { Debug.LogWarning("Error al enviar: " + e.Message); }
        }
    }
    //cierre de seguridad para evitar que el puerto quede abierto o los hilos sigan corriendo al cerrar la aplicación
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
    //cierre completo de hilos y puerto
    void CerrarTodo()
    {
        _ejecutando = false;
        if (_hiloLectura != null && _hiloLectura.IsAlive && Thread.CurrentThread != _hiloLectura)
        {
            _hiloLectura.Join(200);
        }
        CerrarPuerto();
    }

    void OnDestroy() => CerrarTodo();
    void OnApplicationQuit() => CerrarTodo();
}