using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EndoscopioCurvas : MonoBehaviour
{
    [Header("Los Huesos (0=Punta, Último=Base)")]
    public Transform[] huesos;

    [Header("Configuración de Controles")]
    public float velocidadInsercion = 0.5f;
    public float velocidadTorque = 100f;
    public float velocidadGiroPunta = 80f;
    public float suavidadGiroHuesos = 15f;

    [Header("Mecánicas Médicas (Simulación)")]
    public float fuerzaRigidezTorque = 1.5f;
    public float fuerzaArrectar = 5.0f;
    public float umbralBucleAtasco = 140f;

    [Header("Límites de Seguridad (Fatal Errors)")]
    public float maxTorquePermitido = 540f;
    [Tooltip("Máximo de toques forzando el límite antes de romper la fibra")]
    public int maxIntentosTorque = 3;
    [Tooltip("Segundos manteniendo el torque al máximo antes de romper la fibra")]
    public float tiempoMaximoTorque = 2f;
    [Tooltip("Segundos manteniendo W contra un bucle antes de perforar al paciente")]
    public float tiempoMaximoForzandoBucle = 3f;

    // Contadores internos
    private int contadorAvisoRoturaTorque = 0;
    private bool teclaPresionadaEnLimite = false;
    private float tiempoForzandoTorque = 0f;
    private float tiempoForzandoBucle = 0f;

    private Quaternion[] rotacionesGlobalesIniciales;
    private Quaternion[] olaDeCurvas;

    private float longitudHueso;
    private float distanciaAcumulada = 0f;
    private float rotX = 0f, rotZ = 0f, torqueGiro = 0f;

    private Rigidbody rb;
    private float empujeFisico = 0f;
    private float inputTorqueActivo = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rotacionesGlobalesIniciales = new Quaternion[huesos.Length];
        olaDeCurvas = new Quaternion[huesos.Length];

        for (int i = 0; i < huesos.Length; i++)
        {
            rotacionesGlobalesIniciales[i] = huesos[i].rotation;
            olaDeCurvas[i] = Quaternion.identity;
        }

        if (huesos.Length > 1)
            longitudHueso = Vector3.Distance(huesos[0].position, huesos[1].position);
    }

    void Update()
    {
        if (huesos.Length == 0) return;

        // --- LECTURA DE INPUTS ---
        empujeFisico = 0;
        if (Input.GetKey(KeyCode.W)) empujeFisico = 1;
        if (Input.GetKey(KeyCode.S)) empujeFisico = -1;

        if (Input.GetKey(KeyCode.UpArrow)) rotX -= velocidadGiroPunta * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) rotX += velocidadGiroPunta * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftArrow)) rotZ += velocidadGiroPunta * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow)) rotZ -= velocidadGiroPunta * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -90f, 90f);
        rotZ = Mathf.Clamp(rotZ, -90f, 90f);

        // --- LÓGICA DE FATIGA POR TORQUE (EL EQUIPO) ---
        inputTorqueActivo = 0f;
        if (Input.GetKey(KeyCode.A)) inputTorqueActivo = -1f;
        if (Input.GetKey(KeyCode.D)) inputTorqueActivo = 1f;

        if (inputTorqueActivo != 0)
        {
            float nuevoTorque = torqueGiro + (inputTorqueActivo * velocidadTorque * Time.deltaTime);

            // 1. Verificamos si tocamos el límite
            if (Mathf.Abs(nuevoTorque) > maxTorquePermitido)
            {
                // Regla de los 3 toques
                if (!teclaPresionadaEnLimite)
                {
                    contadorAvisoRoturaTorque++;
                    teclaPresionadaEnLimite = true;

                    if (contadorAvisoRoturaTorque < maxIntentosTorque)
                        Debug.LogWarning($"<color=orange>CUIDADO: Forzando Torque. Toques: ({contadorAvisoRoturaTorque}/{maxIntentosTorque}).</color>");
                    else
                        Debug.LogError("<color=red>¡FATAL ERROR! Rompiste la fibra óptica por forzar el límite repetidamente.</color>");
                }

                // Regla de mantener apretado por 2 segundos
                tiempoForzandoTorque += Time.deltaTime;
                if (tiempoForzandoTorque >= tiempoMaximoTorque)
                {
                    Debug.LogError("<color=red>¡FATAL ERROR! Rompiste la fibra óptica por mantener tensión extrema.</color>");
                }

                torqueGiro = Mathf.Clamp(nuevoTorque, -maxTorquePermitido, maxTorquePermitido);
            }
            else
            {
                // Si estamos girando pero aún no llegamos al límite
                torqueGiro = nuevoTorque;
                teclaPresionadaEnLimite = false;
                tiempoForzandoTorque = 0f; // Resetea el timer de "mantener apretado"
            }
        }
        else
        {
            teclaPresionadaEnLimite = false;
            tiempoForzandoTorque = 0f;
        }

        // 2. Si el usuario se aleja del límite (Ej. gira al lado contrario), perdonamos los errores
        if (Mathf.Abs(torqueGiro) < maxTorquePermitido - 10f)
        {
            contadorAvisoRoturaTorque = 0;
        }
    }

    void FixedUpdate()
    {
        if (huesos.Length < 2) return;

        // --- DETECTOR DE BUCLES Y PERFORACIÓN (EL PACIENTE) ---
        float anguloBucle = Vector3.Angle(huesos[huesos.Length - 1].up, huesos[0].up);
        float multiplicadorAvance = 1f;

        if (anguloBucle > umbralBucleAtasco && empujeFisico > 0)
        {
            multiplicadorAvance = Mathf.Clamp01(1f - ((anguloBucle - umbralBucleAtasco) / 40f));

            if (multiplicadorAvance < 0.05f)
            {
                tiempoForzandoBucle += Time.fixedDeltaTime;

                if (tiempoForzandoBucle > tiempoMaximoForzandoBucle)
                {
                    Debug.LogError("<color=red>¡FATAL ERROR: PERFORACIÓN INTESTINAL! Mantuviste W en un atasco.</color>");
                }
                else
                {
                    int porcentajeDanio = (int)((tiempoForzandoBucle / tiempoMaximoForzandoBucle) * 100);
                    Debug.LogWarning($"<color=orange>ATASCO: Ángulo ({anguloBucle}°). Daño al paciente: {porcentajeDanio}%</color>");
                }
            }
        }
        else
        {
            // Regla de descanso: Si jala (S) el contador baja a 0 instantáneamente. Si solo suelta (W), baja poco a poco.
            if (empujeFisico < 0)
                tiempoForzandoBucle = 0f;
            else
                tiempoForzandoBucle = Mathf.Max(0, tiempoForzandoBucle - (Time.fixedDeltaTime * 2f));
        }

        // --- LÓGICA DE TENSIÓN Y ARRECTAR ---
        if (empujeFisico != 0)
        {
            float nivelDeTension = Mathf.Abs(torqueGiro);
            float rigidezPorTension = nivelDeTension * 0.01f * fuerzaRigidezTorque;
            float fuerzaFinalDeEnderezado = (empujeFisico > 0) ? rigidezPorTension : (inputTorqueActivo != 0 ? fuerzaArrectar : 0f);

            if (fuerzaFinalDeEnderezado > 0)
            {
                for (int i = 1; i < olaDeCurvas.Length; i++)
                    olaDeCurvas[i] = Quaternion.Slerp(olaDeCurvas[i], Quaternion.identity, Time.fixedDeltaTime * fuerzaFinalDeEnderezado);
            }
        }

        // --- MOVIMIENTO FÍSICO ---
        if (empujeFisico != 0)
        {
            Vector3 direccionFinal = huesos[1].up;
            float distanciaAvanzada = empujeFisico * (velocidadInsercion * multiplicadorAvance) * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + (direccionFinal * distanciaAvanzada));

            distanciaAcumulada += distanciaAvanzada;

            if (distanciaAcumulada >= longitudHueso)
            {
                for (int i = huesos.Length - 1; i > 0; i--) olaDeCurvas[i] = olaDeCurvas[i - 1];
                distanciaAcumulada -= longitudHueso;
            }
            else if (distanciaAcumulada <= -longitudHueso)
            {
                for (int i = 0; i < huesos.Length - 1; i++) olaDeCurvas[i] = olaDeCurvas[i + 1];
                olaDeCurvas[huesos.Length - 1] = Quaternion.identity;
                distanciaAcumulada += longitudHueso;
            }
        }
    }

    void LateUpdate()
    {
        if (huesos.Length < 2) return;

        olaDeCurvas[0] = olaDeCurvas[1] * Quaternion.Euler(rotX, 0, rotZ);
        Quaternion curvaCuello = Quaternion.identity;

        for (int i = 1; i < huesos.Length; i++)
        {
            Quaternion curvaSuave = (empujeFisico >= 0)
                ? Quaternion.Slerp(olaDeCurvas[i], olaDeCurvas[i - 1], distanciaAcumulada / longitudHueso)
                : Quaternion.Slerp(olaDeCurvas[i], olaDeCurvas[Mathf.Min(i + 1, huesos.Length - 1)], Mathf.Abs(distanciaAcumulada) / longitudHueso);

            if (i == 1) curvaCuello = curvaSuave;

            Quaternion rotacionObjetivo = rotacionesGlobalesIniciales[i] * curvaSuave * Quaternion.Euler(0, torqueGiro, 0);
            huesos[i].rotation = Quaternion.Slerp(huesos[i].rotation, rotacionObjetivo, Time.deltaTime * suavidadGiroHuesos);
        }

        Quaternion curvaPunta = curvaCuello * Quaternion.Euler(rotX, 0, rotZ);
        Quaternion objPunta = rotacionesGlobalesIniciales[0] * curvaPunta * Quaternion.Euler(0, torqueGiro, 0);
        huesos[0].rotation = Quaternion.Slerp(huesos[0].rotation, objPunta, Time.deltaTime * suavidadGiroHuesos);
    }
}