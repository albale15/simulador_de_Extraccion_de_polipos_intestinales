using UnityEngine;
using System;

public class DatosProcesados
{
    public float insercionFinal;
    public float volanteXFinal;
    public float volanteYFinal;

    public bool botonFreeze;
    public bool botonCapture;
    public bool botonZoom;
    public bool botonAccion;
    public bool botonSuccion;
    public bool botonLimpiado;
}

// Obligamos a este script a iniciar primero
[DefaultExecutionOrder(-50)]
public class ConfigManager : MonoBehaviour
{
    public static ConfigManager instancia;
    public event Action<DatosProcesados> AlRecibirDatosProcesados;

    [Header("Conexión Directa")]
    public SerialManager serial; // EL CABLE DIRECTO

    [Header("Sensibilidades")]
    public float sensInsercion = 1.0f;
    public float sensVolantes = 1.0f;

    [Header("Mapeo de Ejes (Hardware -> Juego)")]
    public string mapInsAdelante = "INS_+";
    public string mapInsAtras = "INS_-";

    public string mapVolXDer = "E1_+";
    public string mapVolXIzq = "E1_-";
    public string mapVolYArr = "E2_+";
    public string mapVolYAba = "E2_-";

    [Header("Mapeo de Botones (Hardware -> Juego)")]
    public string mapFreeze = "B1";
    public string mapCapture = "B2";
    public string mapZoom = "B3";
    public string mapAccion = "B4";
    public string mapSuccion = "Su";
    public string mapLimpiado = "Lim";

    public DatosProcesados datosActuales = new DatosProcesados();
    private float acumuladorVolX = 0f;
    private float acumuladorVolY = 0f;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
            CargarAjustes();
        }
        else Destroy(gameObject);

        if (serial == null) serial = FindObjectOfType<SerialManager>();
    }

    void OnEnable()
    {
        if (serial != null)
            serial.AlRecibirNuevosDatos += ProcesarHardware;
    }

    void OnDisable()
    {
        if (serial != null)
            serial.AlRecibirNuevosDatos -= ProcesarHardware;
    }

    private void ProcesarHardware(DatosHardware d)
    {
        // Inserción se queda normal
        datosActuales.insercionFinal += (ObtenerEje(mapInsAdelante, d) - ObtenerEje(mapInsAtras, d)) * sensInsercion;
        // Usamos += para sumar todos los micro-giros si llegan muy rápido en un solo frame
        datosActuales.volanteXFinal += (ObtenerEje(mapVolXDer, d) - ObtenerEje(mapVolXIzq, d)) * sensVolantes;
        datosActuales.volanteYFinal += (ObtenerEje(mapVolYArr, d) - ObtenerEje(mapVolYAba, d)) * sensVolantes;

        datosActuales.botonFreeze = ObtenerBoton(mapFreeze, d);
        datosActuales.botonCapture = ObtenerBoton(mapCapture, d);
        datosActuales.botonZoom = ObtenerBoton(mapZoom, d);
        datosActuales.botonAccion = ObtenerBoton(mapAccion, d);
        datosActuales.botonSuccion = ObtenerBoton(mapSuccion, d);
        datosActuales.botonLimpiado = ObtenerBoton(mapLimpiado, d);

        AlRecibirDatosProcesados?.Invoke(datosActuales);
    }

    void LateUpdate()
    {
        if (datosActuales != null)
        {
            // Limpiamos LOS VOLANTES cada frame. Si paras la mano, esto cae a cero INSTANTÁNEAMENTE.
            datosActuales.insercionFinal = 0f;
            datosActuales.volanteXFinal = 0f;
            datosActuales.volanteYFinal = 0f;

            datosActuales.botonFreeze = false;
            datosActuales.botonCapture = false;
            datosActuales.botonZoom = false;
            datosActuales.botonAccion = false;
            datosActuales.botonSuccion = false;
            datosActuales.botonLimpiado = false;
        }
    }

    private float ObtenerEje(string bind, DatosHardware d)
    {
        if (bind == "INS_+" && d.insercion > 0) return d.insercion;
        if (bind == "INS_-" && d.insercion < 0) return Mathf.Abs(d.insercion);
        if (bind == "E1_+" && d.volante1 > 0) return d.volante1;
        if (bind == "E1_-" && d.volante1 < 0) return Mathf.Abs(d.volante1);
        if (bind == "E2_+" && d.volante2 > 0) return d.volante2;
        if (bind == "E2_-" && d.volante2 < 0) return Mathf.Abs(d.volante2);
        return 0f;
    }

    private bool ObtenerBoton(string bind, DatosHardware d)
    {
        if (bind == "B1") return d.boton1 == 1;
        if (bind == "B2") return d.boton2 == 1;
        if (bind == "B3") return d.boton3 == 1;
        if (bind == "B4") return d.boton4 == 1;
        if (bind == "Su") return d.botonSuccion == 1;
        if (bind == "Lim") return d.botonLimpiado == 1;
        return false;
    }

    public void GuardarAjustes()
    {
        PlayerPrefs.SetFloat("S_Ins", sensInsercion);
        PlayerPrefs.SetFloat("S_Vol", sensVolantes);
        PlayerPrefs.SetString("M_InsA", mapInsAdelante);
        PlayerPrefs.SetString("M_InsB", mapInsAtras);
        PlayerPrefs.SetString("M_VxD", mapVolXDer);
        PlayerPrefs.SetString("M_VxI", mapVolXIzq);
        PlayerPrefs.SetString("M_VyA", mapVolYArr);
        PlayerPrefs.SetString("M_VyB", mapVolYAba);
        PlayerPrefs.SetString("M_BtnF", mapFreeze);
        PlayerPrefs.SetString("M_BtnC", mapCapture);
        PlayerPrefs.SetString("M_BtnZ", mapZoom);
        PlayerPrefs.SetString("M_BtnA", mapAccion);
        PlayerPrefs.SetString("M_BtnS", mapSuccion);
        PlayerPrefs.SetString("M_BtnL", mapLimpiado);
        PlayerPrefs.Save();
    }

    void CargarAjustes()
    {
        sensInsercion = PlayerPrefs.GetFloat("S_Ins", 1.0f);
        sensVolantes = PlayerPrefs.GetFloat("S_Vol", 1.0f);
        mapInsAdelante = PlayerPrefs.GetString("M_InsA", "INS_+");
        mapInsAtras = PlayerPrefs.GetString("M_InsB", "INS_-");
        mapVolXDer = PlayerPrefs.GetString("M_VxD", "E1_+");
        mapVolXIzq = PlayerPrefs.GetString("M_VxI", "E1_-");
        mapVolYArr = PlayerPrefs.GetString("M_VyA", "E2_+");
        mapVolYAba = PlayerPrefs.GetString("M_VyB", "E2_-");
        mapFreeze = PlayerPrefs.GetString("M_BtnF", "B1");
        mapCapture = PlayerPrefs.GetString("M_BtnC", "B2");
        mapZoom = PlayerPrefs.GetString("M_BtnZ", "B3");
        mapAccion = PlayerPrefs.GetString("M_BtnA", "B4");
        mapSuccion = PlayerPrefs.GetString("M_BtnS", "Su");
        mapLimpiado = PlayerPrefs.GetString("M_BtnL", "Lim");
    }

    public void RestablecerValores()
    {
        sensInsercion = 1.0f; sensVolantes = 1.0f;
        mapInsAdelante = "INS_+"; mapInsAtras = "INS_-";
        mapVolXDer = "E1_+"; mapVolXIzq = "E1_-";
        mapVolYArr = "E2_+"; mapVolYAba = "E2_-";
        mapFreeze = "B1"; mapCapture = "B2"; mapZoom = "B4"; mapAccion = "B3"; mapSuccion = "Su";
        mapLimpiado = "Lim";
        GuardarAjustes();
    }
}