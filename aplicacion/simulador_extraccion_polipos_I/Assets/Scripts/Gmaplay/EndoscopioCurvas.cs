using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class EndoscopioCurvas : MonoBehaviour
{
    [Header("Los Huesos (0=Punta, Último=Base)")]
    public Transform[] huesos;

    [Header("Conexión de Hardware")]
    [Tooltip("Activa para ignorar el teclado y usar el control físico (STM32)")]
    public bool usarControlHardware = false;
    private DatosProcesados datosHardware;
    private float tiempoUltimoDatoHardware = 0f;

    [Header("Configuración de Controles")]
    public float velocidadInsercion = 0.5f;
    [Tooltip("Velocidad al retroceder (tecla S)")]
    public float velocidadExtraccion = 0.8f;
    public float velocidadTorque = 100f;
    public float velocidadGiroPunta = 80f;
    public float suavidadGiroHuesos = 15f;

    [Header("Mecánicas Médicas")]
    public float fuerzaRigidezTorque = 1.5f;
    public float fuerzaArrectar = 5.0f;
    public float umbralBucleAtasco = 140f;
    [Tooltip("Grados por segundo que la punta cae por gravedad al avanzar si no hay torque")]
    public float caidaGravedad = 8f;

    [Header("Flexibilidad Dinámica")]
    [Tooltip("Límite de doblez al empujar (W)")]
    public float limiteFlexionNormal = 90f;
    [Tooltip("Límite máximo al jalar o estar quieto (Retroflexión)")]
    public float limiteFlexionRelajada = 160f;

    [Header("Límites de Seguridad (Fatal Errors)")]
    public float maxTorquePermitido = 540f;
    public int maxIntentosTorque = 3;
    public float tiempoMaximoTorque = 2f;
    public float tiempoMaximoForzandoBucle = 3f;
    [Tooltip("Segundos jalando bruscamente en una curva antes de desgarrar el tejido")]
    public float tiempoMaximoTiron = 4f;

    [Header("Visualización del Tubo")]
    public bool dibujarTuboExterior = true;
    public float grosorTubo = 0.012f;

    // Estado del juego
    private bool juegoTerminado = false;

    // Contadores internos
    private int contadorAvisoRoturaTorque = 0;
    private bool teclaPresionadaEnLimite = false;
    private float tiempoForzandoTorque = 0f;
    private float tiempoForzandoBucle = 0f;
    private float tiempoExtraccionBrusca = 0f;

    // Cinemática
    private Quaternion[] rotacionesGlobalesIniciales;
    private Quaternion[] olaDeCurvas;
    private float longitudHueso;
    private float distanciaAcumulada = 0f;
    private float rotX = 0f, rotZ = 0f, torqueGiro = 0f;
    private List<Quaternion> historialCurvas = new List<Quaternion>();

    private Rigidbody rb;
    private float empujeFisico = 0f;
    private float inputTorqueActivo = 0f;

    // LineRenderer
    private LineRenderer lr;
    private List<Vector3> rutaTubo = new List<Vector3>();

    // ==========================================
    // --- ENCHUFE CON EL HARDWARE ---
    // ==========================================
    void OnEnable()
    {
        if (ConfigManager.instancia != null)
        {
            ConfigManager.instancia.AlRecibirDatosProcesados += ActualizarDatosHardware;
        }
    }

    void OnDisable()
    {
        if (ConfigManager.instancia != null)
        {
            ConfigManager.instancia.AlRecibirDatosProcesados -= ActualizarDatosHardware;
        }
    }

    private void ActualizarDatosHardware(DatosProcesados nuevosDatos)
    {
        datosHardware = nuevosDatos;
        tiempoUltimoDatoHardware = Time.time;
    }
    // ==========================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        rotacionesGlobalesIniciales = new Quaternion[huesos.Length];
        olaDeCurvas = new Quaternion[huesos.Length];

        for (int i = 0; i < huesos.Length; i++)
        {
            rotacionesGlobalesIniciales[i] = huesos[i].rotation;
            olaDeCurvas[i] = Quaternion.identity;
        }

        if (huesos.Length > 1)
            longitudHueso = Vector3.Distance(huesos[0].position, huesos[1].position);

        if (dibujarTuboExterior)
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.startWidth = grosorTubo;
            lr.endWidth = grosorTubo;
            lr.positionCount = 0;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.1f, 0.1f, 0.1f);
            lr.endColor = new Color(0.1f, 0.1f, 0.1f);
            rutaTubo.Add(huesos[huesos.Length - 1].position);
        }
    }

    void Update()
    {
        if (juegoTerminado || huesos.Length == 0) return;

        empujeFisico = 0f;
        inputTorqueActivo = 0f;
        bool tocandoFlechas = false;

        if (!usarControlHardware || datosHardware == null)
        {
            // --- 1. LÓGICA PC (Teclado) ---
            if (Input.GetKey(KeyCode.W)) empujeFisico = 1f;
            if (Input.GetKey(KeyCode.S)) empujeFisico = -1f;

            if (Input.GetKey(KeyCode.UpArrow)) { rotX -= velocidadGiroPunta * Time.deltaTime; tocandoFlechas = true; }
            if (Input.GetKey(KeyCode.DownArrow)) { rotX += velocidadGiroPunta * Time.deltaTime; tocandoFlechas = true; }
            if (Input.GetKey(KeyCode.LeftArrow)) { rotZ += velocidadGiroPunta * Time.deltaTime; tocandoFlechas = true; }
            if (Input.GetKey(KeyCode.RightArrow)) { rotZ -= velocidadGiroPunta * Time.deltaTime; tocandoFlechas = true; }

            if (Input.GetKey(KeyCode.A)) inputTorqueActivo = -1f;
            if (Input.GetKey(KeyCode.D)) inputTorqueActivo = 1f;
        }
        else
        {
            // --- 2. LÓGICA ENDOSCOPIO FÍSICO (Hardware) ---
            if (Time.time - tiempoUltimoDatoHardware > 0.15f)
            {
                datosHardware.insercionFinal = 0f;
                datosHardware.torsionFinal = 0f;
                datosHardware.volanteXFinal = 0f;
                datosHardware.volanteYFinal = 0f;
            }

            // [NUEVO] FILTRO DE ZONA MUERTA PARA LOS SENSORES
            empujeFisico = Mathf.Clamp(datosHardware.insercionFinal, -1f, 1f);
            if (Mathf.Abs(empujeFisico) < 0.05f) empujeFisico = 0f;

            inputTorqueActivo = Mathf.Clamp(datosHardware.torsionFinal, -1f, 1f);
            if (Mathf.Abs(inputTorqueActivo) < 0.05f) inputTorqueActivo = 0f;

            if (Mathf.Abs(datosHardware.volanteYFinal) > 0.05f)
            {
                rotX -= datosHardware.volanteYFinal * velocidadGiroPunta * Time.deltaTime;
                tocandoFlechas = true;
            }

            if (Mathf.Abs(datosHardware.volanteXFinal) > 0.05f)
            {
                rotZ += datosHardware.volanteXFinal * velocidadGiroPunta * Time.deltaTime;
                tocandoFlechas = true;
            }
        }

        // --- CAÍDA NATURAL POR GRAVEDAD ---
        if (empujeFisico > 0 && !tocandoFlechas)
        {
            float factorFlexibilidad = 1f - Mathf.Clamp01(Mathf.Abs(torqueGiro) / 40f);
            rotX += caidaGravedad * factorFlexibilidad * Time.deltaTime;
        }

        if (empujeFisico < 0 && !tocandoFlechas)
        {
            rotX = Mathf.Lerp(rotX, 0f, Time.deltaTime * 2f);
            rotZ = Mathf.Lerp(rotZ, 0f, Time.deltaTime * 2f);
        }

        // --- LÍMITES DE FLEXIÓN DINÁMICOS ---
        float limiteActual = (empujeFisico > 0) ? limiteFlexionNormal : limiteFlexionRelajada;
        rotX = Mathf.Clamp(rotX, -limiteActual, limiteActual);
        rotZ = Mathf.Clamp(rotZ, -limiteActual, limiteActual);

        // --- FATIGA POR TORQUE ---
        if (inputTorqueActivo != 0)
        {
            float nuevoTorque = torqueGiro + (inputTorqueActivo * velocidadTorque * Time.deltaTime);
            string estado = Mathf.Abs(nuevoTorque) < 20f ? "<color=green>FLEXIBLE</color>" : "<color=orange>RÍGIDO</color>";

            if (Mathf.Abs(nuevoTorque) > maxTorquePermitido)
            {
                if (!teclaPresionadaEnLimite)
                {
                    contadorAvisoRoturaTorque++;
                    teclaPresionadaEnLimite = true;
                    if (contadorAvisoRoturaTorque >= maxIntentosTorque)
                        ProcesarGameOver("Rompiste la fibra óptica por forzar el límite de torque.");
                    else
                        Debug.LogWarning($"<color=orange>CUIDADO: Forzando Torque. Toques: ({contadorAvisoRoturaTorque}/{maxIntentosTorque}). Suelta y gira al otro lado para relajar.</color>");
                }

                tiempoForzandoTorque += Time.deltaTime;
                if (tiempoForzandoTorque >= tiempoMaximoTorque)
                    ProcesarGameOver("Rompiste la fibra óptica por mantener tensión extrema.");

                torqueGiro = Mathf.Clamp(nuevoTorque, -maxTorquePermitido, maxTorquePermitido);
            }
            else
            {
                torqueGiro = nuevoTorque;
                teclaPresionadaEnLimite = false;
                tiempoForzandoTorque = 0f;
            }
        }
        else
        {
            teclaPresionadaEnLimite = false;
            tiempoForzandoTorque = 0f;
        }

        if (Mathf.Abs(torqueGiro) < maxTorquePermitido - 10f) contadorAvisoRoturaTorque = 0;
    }

    void FixedUpdate()
    {
        if (juegoTerminado || huesos.Length < 2) return;

        float anguloBucle = Vector3.Angle(huesos[huesos.Length - 1].up, huesos[0].up);
        float multiplicadorAvance = 1f;

        if (anguloBucle > umbralBucleAtasco && empujeFisico > 0)
        {
            multiplicadorAvance = Mathf.Clamp01(1f - ((anguloBucle - umbralBucleAtasco) / 40f));
            if (multiplicadorAvance < 0.05f)
            {
                tiempoForzandoBucle += Time.fixedDeltaTime;
                if (tiempoForzandoBucle > tiempoMaximoForzandoBucle)
                    ProcesarGameOver("PERFORACIÓN INTESTINAL: Forzaste el avance durante un bucle.");
                else
                {
                    int porcentaje = (int)((tiempoForzandoBucle / tiempoMaximoForzandoBucle) * 100);
                    Debug.LogWarning($"<color=orange>¡ATASCO! Ángulo ({anguloBucle}°). USA S + A/D PARA ARRECTAR. Daño: {porcentaje}%</color>");
                }
            }
        }
        else
        {
            if (empujeFisico < 0) tiempoForzandoBucle = 0f;
            else tiempoForzandoBucle = Mathf.Max(0, tiempoForzandoBucle - (Time.fixedDeltaTime * 2f));
        }

        if (empujeFisico < 0)
        {
            float curvaturaCuerpo = 0f;
            for (int i = 1; i < olaDeCurvas.Length; i++)
            {
                curvaturaCuerpo += Quaternion.Angle(Quaternion.identity, olaDeCurvas[i]);
            }

            if (curvaturaCuerpo > 60f)
            {
                float factorFriccion = Mathf.Clamp01((curvaturaCuerpo - 60f) / 100f);
                multiplicadorAvance = 1f - (factorFriccion * 0.8f);

                if (curvaturaCuerpo > 120f)
                {
                    tiempoExtraccionBrusca += Time.fixedDeltaTime;
                    if (tiempoExtraccionBrusca > tiempoMaximoTiron)
                        ProcesarGameOver("LACERACIÓN DE MUCOSA: Mantuviste un jalón prolongado sin pausas en una curva cerrada.");
                    else
                    {
                        int dolor = (int)((tiempoExtraccionBrusca / tiempoMaximoTiron) * 100);
                        Debug.LogWarning($"<color=orange>¡PACIENTE CON DOLOR! Fricción alta. Daño tisular: {dolor}% (Suelta S un momento para relajar)</color>");
                    }
                }
                else
                {
                    tiempoExtraccionBrusca = Mathf.Max(0, tiempoExtraccionBrusca - (Time.fixedDeltaTime * 4f));
                }
            }
            else
            {
                tiempoExtraccionBrusca = Mathf.Max(0, tiempoExtraccionBrusca - (Time.fixedDeltaTime * 4f));
            }
        }
        else
        {
            tiempoExtraccionBrusca = Mathf.Max(0, tiempoExtraccionBrusca - (Time.fixedDeltaTime * 4f));
        }

        Vector3 direccionHaciaPunta = huesos[0].position - huesos[huesos.Length - 1].position;
        float productoPuntoPlano = Vector3.Dot(huesos[huesos.Length - 1].up, direccionHaciaPunta.normalized);

        if (productoPuntoPlano < -0.1f && anguloBucle > 90f)
        {
            ProcesarGameOver("AUTO-INTERSECCIÓN FATAL: El endoscopio cruzó su propio plano de entrada.");
        }

        if (dibujarTuboExterior && rutaTubo.Count > 10 && !juegoTerminado)
        {
            int puntosCuerpo = Mathf.CeilToInt(((huesos.Length - 1) * longitudHueso) / 0.04f) + 5;
            if (rutaTubo.Count > puntosCuerpo)
            {
                for (int i = 0; i < rutaTubo.Count - puntosCuerpo; i++)
                {
                    if (Vector3.Distance(huesos[0].position, rutaTubo[i]) < 0.04f && empujeFisico > 0)
                    {
                        ProcesarGameOver("AUTO-INTERSECCIÓN: El endoscopio se anudó sobre sí mismo y chocó con su cable.");
                        break;
                    }
                }
            }
        }

        // --- MOVIMIENTO FÍSICO Y LÓGICA DE TENSIÓN ---
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

            Vector3 direccionFinal = huesos[1].up;
            float velActual = (empujeFisico > 0) ? velocidadInsercion : velocidadExtraccion;
            float distanciaAvanzada = empujeFisico * (velActual * multiplicadorAvance) * Time.fixedDeltaTime;

            rb.MovePosition(rb.position + (direccionFinal * distanciaAvanzada));

            distanciaAcumulada += distanciaAvanzada;

            if (distanciaAcumulada >= longitudHueso)
            {
                historialCurvas.Add(olaDeCurvas[huesos.Length - 1]);
                for (int i = huesos.Length - 1; i > 0; i--) olaDeCurvas[i] = olaDeCurvas[i - 1];
                distanciaAcumulada -= longitudHueso;
            }
            else if (distanciaAcumulada <= -longitudHueso)
            {
                for (int i = 0; i < huesos.Length - 1; i++) olaDeCurvas[i] = olaDeCurvas[i + 1];
                if (historialCurvas.Count > 0)
                {
                    olaDeCurvas[huesos.Length - 1] = historialCurvas[historialCurvas.Count - 1];
                    historialCurvas.RemoveAt(historialCurvas.Count - 1);
                }
                else olaDeCurvas[huesos.Length - 1] = Quaternion.identity;
                distanciaAcumulada += longitudHueso;
            }

            if (dibujarTuboExterior)
            {
                Transform baseHueso = huesos[huesos.Length - 1];
                if (empujeFisico > 0)
                {
                    Vector3 ultimoPunto = rutaTubo[rutaTubo.Count - 1];
                    Vector3 direccionMovimiento = baseHueso.position - ultimoPunto;

                    if (direccionMovimiento.magnitude > 0.04f)
                    {
                        if (Vector3.Dot(baseHueso.up, direccionMovimiento.normalized) > 0.2f)
                        {
                            rutaTubo.Add(baseHueso.position);
                        }
                    }
                }
                else if (empujeFisico < 0 && rutaTubo.Count > 1)
                {
                    while (rutaTubo.Count > 1 && Vector3.Distance(baseHueso.position, rutaTubo[rutaTubo.Count - 2]) <= Vector3.Distance(baseHueso.position, rutaTubo[rutaTubo.Count - 1]))
                    {
                        rutaTubo.RemoveAt(rutaTubo.Count - 1);
                    }
                }
                lr.positionCount = rutaTubo.Count;
                lr.SetPositions(rutaTubo.ToArray());
                lr.SetPosition(rutaTubo.Count - 1, baseHueso.position);
            }
        }
        else
        {
            // [NUEVO] ANCLA MAGNÉTICA: Si no hay empuje intencional, matamos la velocidad física
            // Esto anula cualquier deslizamiento por culpa del material resbaladizo del intestino
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void ProcesarGameOver(string motivo)
    {
        if (juegoTerminado) return;

        juegoTerminado = true;
        Time.timeScale = 0f;

        Debug.LogError($"<color=red><b>GAME OVER:</b> {motivo}</color>");
    }

    void LateUpdate()
    {
        if (juegoTerminado || huesos.Length < 2) return;

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