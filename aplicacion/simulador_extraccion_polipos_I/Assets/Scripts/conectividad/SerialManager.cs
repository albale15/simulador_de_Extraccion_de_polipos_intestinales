using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;
using System;


// 1. EL CONTENEDOR DE DATOS TRADUCIDOS
public class DatosHardware
{
    // Orden exacto de los botones
    public int botonLimpiado; // "Lim"
    public int botonSuccion;  // "Su"
    public int boton1;        // "B1"
    public int boton2;        // "B2"
    public int boton3;        // "B3"
    public int boton4;        // "B4"
    public int volante1, volante2; // E1 y E2
    public int insercion; // "INS"
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


    // VARIABLES TEST DE LATENCIA
    private System.Diagnostics.Stopwatch cronometroLatencia = new System.Diagnostics.Stopwatch();
    private int pingsCompletados = 0;
    private long sumaRTT = 0;
    private bool pruebaLatenciaActiva = false;




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
        //Debug.Log($"<color=yellow>[Serial] IniciarBusqueda invocado. Estado actual: {estadoActual}</color>");

        if (estadoActual == EstadoConexion.Buscando)
        {
            Debug.LogWarning("<color=orange>[Serial] ABORTADO: El sistema cree que ya está buscando. El botón no hará nada.</color>");
            return;
        }

        estadoActual = EstadoConexion.Buscando;
        mensajeInterfaz = "Iniciando escaneo de puertos...";

        //Debug.Log("<color=yellow>[Serial] Estado cambiado a Buscando. Disparando el Hilo Secundario...</color>");

        _hiloBusqueda = new Thread(RutinaBusquedaEnFondo) { IsBackground = true };
        _hiloBusqueda.Start();
    }

    void RutinaBusquedaEnFondo()
    {
        //Debug.Log("<color=magenta>[Hilo] --- HILO INICIADO --- Durmiendo 1.5s para no saturar USB...</color>");
        Thread.Sleep(1500);

        string[] puertos = SerialPort.GetPortNames();
        //Debug.Log($"<color=magenta>[Hilo] Escaneo completado. Se encontraron {puertos.Length} puertos conectados a la PC.</color>");

        if (puertos.Length == 0)
        {
            estadoActual = EstadoConexion.Error;
            mensajeInterfaz = "No se detectaron puertos USB.";
            Debug.Log("<color=magenta>[Hilo] Fin de rutina: No hay puertos. Estado -> Error.</color>");
            return;
        }

        foreach (string nombrePuerto in puertos)
        {
            mensajeInterfaz = "Verificando " + nombrePuerto + "...";
            //Debug.Log($"<color=magenta>[Hilo] Intentando handshake con {nombrePuerto}...</color>");

            if (IntentarConexionFondo(nombrePuerto))
            {
                puertoActivo = nombrePuerto;
                mensajeInterfaz = "Sistema listo en " + nombrePuerto;
                estadoActual = EstadoConexion.Conectado;

                _ejecutando = true;
                _hiloLectura = new Thread(LecturaDeFondo) { IsBackground = true };
                _hiloLectura.Start();

                Debug.Log($"<color=green><b>[Hilo] ¡ÉXITO! Endoscopio detectado en {nombrePuerto}. Hilo de búsqueda cerrado.</b></color>");
                return;
            }
            else
            {
                Debug.Log($"<color=magenta>[Hilo] {nombrePuerto} rechazó la conexión o no es el endoscopio.</color>");
            }
        }

        estadoActual = EstadoConexion.Error;
        mensajeInterfaz = "Endoscopio no encontrado.";
        Debug.Log("<color=magenta>[Hilo] Fin de rutina: Se revisaron todos los puertos pero no hubo firma válida. Estado -> Error.</color>");
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
        // Presiona L en el teclado de 
        if (Input.GetKeyDown(KeyCode.L) && !pruebaLatenciaActiva && estadoActual == EstadoConexion.Conectado)
        {
            Debug.Log("<color=yellow>Iniciando Test de Latencia (20 Pings)...</color>");
            pruebaLatenciaActiva = true;
            pingsCompletados = 0;
            sumaRTT = 0;
            DispararPing();
        }

        string mensajeFresco = "";
        bool llegoPong = false;

        // Leemos TODOS los mensajes atrapados. Si vemos el PONG, levantamos la bandera.
        while (_colaMensajes.TryDequeue(out string mensaje))
        {
            if (mensaje.Contains("PONG"))
            {
                llegoPong = true;
            }
            else
            {
                mensajeFresco = mensaje; // Guardamos solo si son datos del endoscopio
            }
        }

        // RECEPTOR DEL TEST DE LATENCIA 
        if (pruebaLatenciaActiva && llegoPong)
        {
            cronometroLatencia.Stop();
            long rttActual = cronometroLatencia.ElapsedMilliseconds;
            sumaRTT += rttActual;
            pingsCompletados++;

            Debug.Log($"Ping {pingsCompletados}/20 -> RTT: {rttActual} ms");

            if (pingsCompletados < 20)
            {
                DispararPing(); // Dispara el siguiente
            }
            else
            {
                long rttPromedio = sumaRTT / 20;
                long latenciaPromedio = rttPromedio / 2;
                Debug.Log($"<color=green><b>=== RESULTADO FINAL DE LATENCIA ===\nRTT Promedio: {rttPromedio} ms\nLatencia (1-Vía): {latenciaPromedio} ms\n================================</b></color>");
                pruebaLatenciaActiva = false;
            }
            return; // Cortamos el frame aquí para evitar procesar ruido
        }

        // FLUJO NORMAL DE JUEGO
        if (!string.IsNullOrEmpty(mensajeFresco))
        {
            ultimoMensajeCrudo = mensajeFresco;
            TraducirDatos(mensajeFresco);
            AlRecibirNuevosDatos?.Invoke(datosActuales);
        }
    }

    private void DispararPing()
    {
        cronometroLatencia.Restart(); // Pone a 0 y arranca
        EnviarDato("P"); //
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
                        case "Lim": datosActuales.botonLimpiado = valor; break;
                        case "Su": datosActuales.botonSuccion = valor; break;
                        case "B1": datosActuales.boton1 = valor; break;
                        case "B2": datosActuales.boton2 = valor; break;
                        case "B3": datosActuales.boton3 = valor; break;
                        case "B4": datosActuales.boton4 = valor; break;
                        case "E1": datosActuales.volante1 = valor; break;
                        case "E2": datosActuales.volante2 = valor; break;
                        case "INS": datosActuales.insercion = valor; break;
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