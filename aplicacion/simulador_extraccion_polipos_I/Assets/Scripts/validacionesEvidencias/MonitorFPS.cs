using UnityEngine;

public class MonitorFPS : MonoBehaviour
{
    public static MonitorFPS instancia;

    private float tiempoSiguienteLog = 0.0f;
    private float tasaLog = 2.0f;

    private int fpsMinimo = 9999;
    private int fpsMaximo = 0;
    private int totalFrames = 0;
    private float tiempoTotal = 0f;
    private bool sistemaCalentado = false;

    void Awake()
    {
        if (instancia == null) { instancia = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        tiempoSiguienteLog = Time.unscaledTime + tasaLog;
    }

    void Update()
    {
        // Omitimos todo el procesamiento hasta que pasen 3 segundos de carga
        if (Time.time < 3.0f) return;

        sistemaCalentado = true;

        float frameTime = Time.unscaledDeltaTime;

        // ignorar frames absurdamente largos (típicos de pausas de carga)
        if (frameTime > 0.2f) return;

        totalFrames++;
        tiempoTotal += frameTime;

        int fpsActual = Mathf.RoundToInt(1.0f / frameTime);

        // Actualizamos Mínimo y Máximo
        if (fpsActual < fpsMinimo) fpsMinimo = fpsActual;
        if (fpsActual > fpsMaximo) fpsMaximo = fpsActual;

        // 2. Reporte solo si ya pasamos el tiempo de carga
        if (Time.unscaledTime > tiempoSiguienteLog)
        {
            int fpsPromedio = Mathf.RoundToInt(totalFrames / tiempoTotal);

            Debug.Log($"[FPS_AUDIT] FPS:{fpsActual} | Promedio:{fpsPromedio} | Min:{fpsMinimo} | Max:{fpsMaximo}");

            // Reseteamos el intervalo de tiempo para el próximo log, pero NO reseteamos los totales
            // para que el promedio sea acumulativo de la sesión
            tiempoSiguienteLog = Time.unscaledTime + tasaLog;
        }
    }
}