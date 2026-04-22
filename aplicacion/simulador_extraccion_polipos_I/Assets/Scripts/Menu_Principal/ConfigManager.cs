using UnityEngine;
using System;

public class DatosProcesados
{
    public float insercionFinal;
    public float torsionFinal;
    public float volanteXFinal;
    public float volanteYFinal;

    public bool botonFreeze;
    public bool botonCapture;
    public bool botonZoom;
    public bool botonSuccion;
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
    public float sensTorsion = 1.0f;
    public float sensVolantes = 1.0f;

    [Header("Mapeo de Ejes (Hardware -> Juego)")]
    public string mapInsAdelante = "INS_+";
    public string mapInsAtras = "INS_-";
    public string mapTorDer = "TOR_+";
    public string mapTorIzq = "TOR_-";
    public string mapVolXDer = "E1_+";
    public string mapVolXIzq = "E1_-";
    public string mapVolYArr = "E2_+";
    public string mapVolYAba = "E2_-";

    [Header("Mapeo de Botones (Hardware -> Juego)")]
    public string mapFreeze = "B1";
    public string mapCapture = "B2";
    public string mapZoom = "B3";
    public string mapSuccion = "Su";

    void Awake()
    {
        //cargamos los ajustes guardados, o dejamos los valores por defecto si no hay nada guardado
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
            CargarAjustes();
        }
        else Destroy(gameObject);

        // Si se nos olvida arrastrarlo en el Inspector, lo busca automáticamente
        if (serial == null) serial = FindObjectOfType<SerialManager>();
    }

    void OnEnable()
    {
        // Nos suscribimos al evento del SerialManager para recibir los datos crudos y procesarlos
        if (serial != null)
            serial.AlRecibirNuevosDatos += ProcesarHardware;
    }

    void OnDisable()
    {
        // Nos desuscribimos para evitar errores y liberar RAM
        if (serial != null)
            serial.AlRecibirNuevosDatos -= ProcesarHardware;
    }
    // Esta función se ejecutará cada vez que el SerialManager reciba datos nuevos del hardware
    private void ProcesarHardware(DatosHardware d)
    {
        DatosProcesados limpios = new DatosProcesados();

        limpios.insercionFinal = (ObtenerEje(mapInsAdelante, d) - ObtenerEje(mapInsAtras, d)) * sensInsercion;
        limpios.torsionFinal = (ObtenerEje(mapTorDer, d) - ObtenerEje(mapTorIzq, d)) * sensTorsion;
        limpios.volanteXFinal = (ObtenerEje(mapVolXDer, d) - ObtenerEje(mapVolXIzq, d)) * sensVolantes;
        limpios.volanteYFinal = (ObtenerEje(mapVolYArr, d) - ObtenerEje(mapVolYAba, d)) * sensVolantes;

        limpios.botonFreeze = ObtenerBoton(mapFreeze, d);
        limpios.botonCapture = ObtenerBoton(mapCapture, d);
        limpios.botonZoom = ObtenerBoton(mapZoom, d);
        limpios.botonSuccion = ObtenerBoton(mapSuccion, d);
        // Una vez que tenemos los datos limpios y listos, los enviamos a través de nuestro propio evento para que cualquier otro script pueda usarlos sin preocuparse por el hardware
        AlRecibirDatosProcesados?.Invoke(limpios);
    }
    // Esta función toma el nombre del bind (por ejemplo, "INS_+") y devuelve el valor correspondiente del hardware, aplicando las condiciones de dirección (positivo/negativo) según el caso.
    private float ObtenerEje(string bind, DatosHardware d)
    {
        if (bind == "INS_+" && d.insercion > 0) return d.insercion;
        if (bind == "INS_-" && d.insercion < 0) return Mathf.Abs(d.insercion);
        if (bind == "TOR_+" && d.torsion > 0) return d.torsion;
        if (bind == "TOR_-" && d.torsion < 0) return Mathf.Abs(d.torsion);
        if (bind == "E1_+" && d.volante1 > 0) return d.volante1;
        if (bind == "E1_-" && d.volante1 < 0) return Mathf.Abs(d.volante1);
        if (bind == "E2_+" && d.volante2 > 0) return d.volante2;
        if (bind == "E2_-" && d.volante2 < 0) return Mathf.Abs(d.volante2);
        return 0f;
    }
    // Esta función toma el nombre del bind de botón (por ejemplo, "B1") y devuelve true si ese botón está presionado en los datos actuales, o false si no lo está.
    private bool ObtenerBoton(string bind, DatosHardware d)
    {
        if (bind == "B1") return d.boton1 == 1;
        if (bind == "B2") return d.boton2 == 1;
        if (bind == "B3") return d.boton3 == 1;
        if (bind == "B4") return d.boton4 == 1;
        if (bind == "Su") return d.botonSuccion == 1;
        return false;
    }
    // Esta función se puede llamar desde el menú de configuración para guardar los ajustes actuales en PlayerPrefs, lo que permite que persistan entre sesiones.
    public void GuardarAjustes()
    {
        PlayerPrefs.SetFloat("S_Ins", sensInsercion);
        PlayerPrefs.SetFloat("S_Tor", sensTorsion);
        PlayerPrefs.SetFloat("S_Vol", sensVolantes);

        PlayerPrefs.SetString("M_InsA", mapInsAdelante);
        PlayerPrefs.SetString("M_InsB", mapInsAtras);
        PlayerPrefs.SetString("M_TorD", mapTorDer);
        PlayerPrefs.SetString("M_TorI", mapTorIzq);
        PlayerPrefs.SetString("M_VxD", mapVolXDer);
        PlayerPrefs.SetString("M_VxI", mapVolXIzq);
        PlayerPrefs.SetString("M_VyA", mapVolYArr);
        PlayerPrefs.SetString("M_VyB", mapVolYAba);

        PlayerPrefs.SetString("M_BtnF", mapFreeze);
        PlayerPrefs.SetString("M_BtnC", mapCapture);
        PlayerPrefs.SetString("M_BtnZ", mapZoom);
        PlayerPrefs.SetString("M_BtnS", mapSuccion);
        PlayerPrefs.Save();
    }
    // Esta función se llama al iniciar el juego para cargar los ajustes guardados previamente, o usar los valores por defecto si no hay nada guardado.
    void CargarAjustes()
    {
        sensInsercion = PlayerPrefs.GetFloat("S_Ins", 1.0f);
        sensTorsion = PlayerPrefs.GetFloat("S_Tor", 1.0f);
        sensVolantes = PlayerPrefs.GetFloat("S_Vol", 1.0f);

        mapInsAdelante = PlayerPrefs.GetString("M_InsA", "INS_+");
        mapInsAtras = PlayerPrefs.GetString("M_InsB", "INS_-");
        mapTorDer = PlayerPrefs.GetString("M_TorD", "TOR_+");
        mapTorIzq = PlayerPrefs.GetString("M_TorI", "TOR_-");
        mapVolXDer = PlayerPrefs.GetString("M_VxD", "E1_+");
        mapVolXIzq = PlayerPrefs.GetString("M_VxI", "E1_-");
        mapVolYArr = PlayerPrefs.GetString("M_VyA", "E2_+");
        mapVolYAba = PlayerPrefs.GetString("M_VyB", "E2_-");

        mapFreeze = PlayerPrefs.GetString("M_BtnF", "B1");
        mapCapture = PlayerPrefs.GetString("M_BtnC", "B2");
        mapZoom = PlayerPrefs.GetString("M_BtnZ", "B3");
        mapSuccion = PlayerPrefs.GetString("M_BtnS", "Su");
    }
    // Esta función se puede llamar desde el menú de configuración para restablecer todos los ajustes a sus valores predeterminados de fábrica, y luego guardarlos.
    public void RestablecerValores()
    {
        sensInsercion = 1.0f; sensTorsion = 1.0f; sensVolantes = 1.0f;
        mapInsAdelante = "INS_+"; mapInsAtras = "INS_-";
        mapTorDer = "TOR_+"; mapTorIzq = "TOR_-";
        mapVolXDer = "E1_+"; mapVolXIzq = "E1_-";
        mapVolYArr = "E2_+"; mapVolYAba = "E2_-";
        mapFreeze = "B1"; mapCapture = "B2"; mapZoom = "B3"; mapSuccion = "Su";
        GuardarAjustes();
    }
}