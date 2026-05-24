using UnityEngine;

public class ValidadorHardware : MonoBehaviour
{
    [Header("Prueba de Botones (100 Tiros)")]
    public int conteoLimpieza = 0;
    public int conteoSuccion = 0;
    public int conteoBoton1 = 0;
    public int conteoBoton2 = 0;
    public int conteoBoton3 = 0;
    public int conteoBoton4 = 0;

    [Header("Prueba de Encoders (Clics)")]
    public int conteoInsercionAdelante = 0;
    public int conteoInsercionAtras = 0;

    // Memoria para detectar el cambio de estado (flanco de subida)
    private DatosHardware memoriaAnterior = new DatosHardware();

    void Start()
    {
        // Nos suscribimos al evento (Como el socket.on)
        if (SerialManager.instancia != null)
        {
            SerialManager.instancia.AlRecibirNuevosDatos += Convalidacion;
        }
        else
        {
            Debug.LogError("No se encontró el SerialManager en la escena.");
        }
    }

    private void Convalidacion(DatosHardware datosNuevos)
    {
        // LÓGICA DE LOS 6 BOTONES (Solo cuenta cuando pasa de 0 a 1)

        // Botón 1: Limpieza
        if (datosNuevos.botonLimpiado == 1 && memoriaAnterior.botonLimpiado == 0)
        {
            conteoLimpieza++;
            Debug.Log($"[TESTING] Botón Limpieza detectado. Pulso N° {conteoLimpieza}");
        }

        // Botón 2: Succión
        if (datosNuevos.botonSuccion == 1 && memoriaAnterior.botonSuccion == 0)
        {
            conteoSuccion++;
            Debug.Log($"[TESTING] Botón Succión detectado. Pulso N° {conteoSuccion}");
        }

        // Botón 3: B1
        if (datosNuevos.boton1 == 1 && memoriaAnterior.boton1 == 0)
        {
            conteoBoton1++;
            Debug.Log($"[TESTING] Botón B1 detectado. Pulso N° {conteoBoton1}");
        }

        // Botón 4: B2
        if (datosNuevos.boton2 == 1 && memoriaAnterior.boton2 == 0)
        {
            conteoBoton2++;
            Debug.Log($"[TESTING] Botón B2 detectado. Pulso N° {conteoBoton2}");
        }

        // Botón 5: B3
        if (datosNuevos.boton3 == 1 && memoriaAnterior.boton3 == 0)
        {
            conteoBoton3++;
            Debug.Log($"[TESTING] Botón B3 detectado. Pulso N° {conteoBoton3}");
        }

        // Botón 6: B4
        if (datosNuevos.boton4 == 1 && memoriaAnterior.boton4 == 0)
        {
            conteoBoton4++;
            Debug.Log($"[TESTING] Botón B4 detectado. Pulso N° {conteoBoton4}");
        }

        //LÓGICA DEL ENCODER DE INSERCIÓN
        if (datosNuevos.insercion > memoriaAnterior.insercion)
        {
            conteoInsercionAdelante++;
            Debug.Log($"[TESTING] Encoder Adelante (+). Clic N° {conteoInsercionAdelante}");
        }
        else if (datosNuevos.insercion < memoriaAnterior.insercion)
        {
            conteoInsercionAtras++;
            Debug.Log($"[TESTING] Encoder Atrás (-). Clic N° {conteoInsercionAtras}");
        }

        // ACTUALIZAR LA MEMORIA
        memoriaAnterior.botonLimpiado = datosNuevos.botonLimpiado;
        memoriaAnterior.botonSuccion = datosNuevos.botonSuccion;
        memoriaAnterior.boton1 = datosNuevos.boton1;
        memoriaAnterior.boton2 = datosNuevos.boton2;
        memoriaAnterior.boton3 = datosNuevos.boton3;
        memoriaAnterior.boton4 = datosNuevos.boton4;
        memoriaAnterior.insercion = datosNuevos.insercion;
    }

    void OnDestroy()
    {
        // Cancelamos la suscripción al cerrar (Como el socket.off)
        if (SerialManager.instancia != null)
        {
            SerialManager.instancia.AlRecibirNuevosDatos -= Convalidacion;
        }
    }
}