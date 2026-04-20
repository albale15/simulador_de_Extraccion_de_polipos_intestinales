using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    public static ConfigManager instancia;

    [Header("Ajustes de Inserción")]
    public float sensInsercion = 1.0f;
    public bool invertirInsercion = false;

    [Header("Ajustes de Torsión (Torque)")]
    public float sensTorsion = 1.0f;
    public bool invertirTorsion = false;

    [Header("Ajustes de Volantes (Cámara)")]
    public float sensVolantes = 1.0f;
    public bool invertirX = false;
    public bool invertirY = false;

    void Awake()
    {
        // Patron Singleton: Solo puede existir un ConfigManager
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
            CargarAjustes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GuardarAjustes()
    {
        PlayerPrefs.SetFloat("SensIns", sensInsercion);
        PlayerPrefs.SetInt("InvIns", invertirInsercion ? 1 : 0);
        PlayerPrefs.SetFloat("SensTor", sensTorsion);
        PlayerPrefs.SetInt("InvTor", invertirTorsion ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Configuración guardada.");
    }

    void CargarAjustes()
    {
        sensInsercion = PlayerPrefs.GetFloat("SensIns", 1.0f);
        invertirInsercion = PlayerPrefs.GetInt("InvIns", 0) == 1;
        sensTorsion = PlayerPrefs.GetFloat("SensTor", 1.0f);
        invertirTorsion = PlayerPrefs.GetInt("InvTor", 0) == 1;
    }

    public void RestablecerValores()
    {
        sensInsercion = 1.0f;
        invertirInsercion = false;
        sensTorsion = 1.0f;
        invertirTorsion = false;
        GuardarAjustes();
    }
}