using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class EndoscopioCurvas : MonoBehaviour
{
    [Header("Los Huesos (0=Punta, Último=Base)")]
    public Transform[] huesos;

    [Header("Conexión de Hardware")]
    public bool usarControlHardware = false;
    private DatosProcesados datosHardware;
    private float tiempoUltimoDatoHardware = 0f;

    [Header("Configuración de Controles")]
    public float velocidadInsercion = 0.5f;
    public float velocidadExtraccion = 0.8f;
    
    public float velocidadGiroPunta = 10f;
    public float suavidadGiroHuesos = 15f;

    [Header("Mecánicas Médicas")]

    public float fuerzaArrectar = 5.0f;
    public float umbralBucleAtasco = 140f;
    public float caidaGravedad = 8f;

    [Header("Flexibilidad Dinámica")]
    public float limiteFlexionNormal = 90f;
    public float limiteFlexionRelajada = 160f;

    [Header("Límites de Seguridad (Fatal Errors)")]

    public float tiempoMaximoForzandoBucle = 3f;
    public float tiempoMaximoTiron = 4f;
    public float valorDañoExtraccion = 5f;

    [Header("Visualización del Tubo")]
    public bool dibujarTuboExterior = true;
    public float grosorTubo = 0.012f;

    [Header("Escudo Anti-Túnel")]
    public LayerMask capaIntestino;

    // Estado del juego
    private bool juegoTerminado = false;

    // SENSOR PARA SABER SI EL JUGADOR ESTÁ MOVIENDO ALGO
    private bool controlActivo = false;

    // Contadores internos

    private float tiempoForzandoBucle = 0f;
    private float tiempoExtraccionBrusca = 0f;

    // Cinemática y Odómetro
    private Quaternion[] rotacionesGlobalesIniciales;
    private Quaternion[] olaDeCurvas;
    private float longitudHueso;
    private float distanciaAcumulada = 0f;

    [HideInInspector]
    public float distanciaTotalInsertada = 0f; // El Odómetro real

    public float rotX = 0f, rotZ = 0f;
    private List<Quaternion> historialCurvas = new List<Quaternion>();

    private Rigidbody rb;
    private float empujeFisico = 0f;


    private LineRenderer lr;
    private List<Vector3> rutaTubo = new List<Vector3>();

    // REFERENCIAS PARA RESTRICCIÓN DE FREEZE 
    private SistemaHerramientas herramientas;
    private MonitorEndoscopiaUI monitorUI;
    private bool alertaFreezeDada = false;

    // Banderas para no repetir la misma penalización infinitamente
    private bool penalizadoBucle = false;
    private bool penalizadoTiron = false;
    private int siguienteUmbralSuavidad = 5;

    private int siguienteUmbralTiron = 35;
    private bool atascadoPorBucle = false;
    private float velocidadGiroPuntapc = 20f;
    void OnEnable()
    {
        if (ConfigManager.instancia != null) ConfigManager.instancia.AlRecibirDatosProcesados += ActualizarDatosHardware;
    }

    void OnDisable()
    {
        if (ConfigManager.instancia != null) ConfigManager.instancia.AlRecibirDatosProcesados -= ActualizarDatosHardware;
    }

    private void ActualizarDatosHardware(DatosProcesados nuevosDatos)
    {
        datosHardware = nuevosDatos;
        tiempoUltimoDatoHardware = Time.time;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        herramientas = FindObjectOfType<SistemaHerramientas>();
        monitorUI = FindObjectOfType<MonitorEndoscopiaUI>();

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

    public bool MoviendoControles() { return controlActivo; }

    void Update()
    {
        if (juegoTerminado || huesos.Length == 0) return;

        // GUARDAMOS EL ESTADO ANTERIOR DE LA PUNTA
        float rotXAnterior = rotX;
        float rotZAnterior = rotZ;

        
        bool tocandoFlechas = false;
        bool bloqueadoPorTutorial = (TutorialManager.instancia != null && TutorialManager.instancia.controlesBloqueados);
        bool modoPC = !usarControlHardware;
        
        if (!bloqueadoPorTutorial)
        {
            if (modoPC)
            {
                empujeFisico = 0f;
                velocidadGiroPuntapc = 40f; // En PC, la punta gira el doble de rápido para compensar la falta de sensibilidad analógica.
                if (Input.GetKey(KeyCode.W)) empujeFisico = 0.5f;
                if (Input.GetKey(KeyCode.S)) empujeFisico = -0.3f;

                if (Input.GetKey(KeyCode.UpArrow)) { rotX -= velocidadGiroPuntapc * Time.deltaTime; tocandoFlechas = true; }
                if (Input.GetKey(KeyCode.DownArrow)) { rotX += velocidadGiroPuntapc * Time.deltaTime; tocandoFlechas = true; }
                if (Input.GetKey(KeyCode.LeftArrow)) { rotZ += velocidadGiroPuntapc * Time.deltaTime; tocandoFlechas = true; }
                if (Input.GetKey(KeyCode.RightArrow)) { rotZ -= velocidadGiroPuntapc * Time.deltaTime; tocandoFlechas = true; }


                //if (Input.GetKeyDown(KeyCode.W)) Debug.Log("[PC] Avanzando tubo (W)");
                //if (Input.GetKeyDown(KeyCode.S)) Debug.Log("[PC] Retrayendo tubo (S)");;
                //if (Input.GetKeyDown(KeyCode.UpArrow)) Debug.Log("[PC] Moviendo Punta (Flechas)");
            }
            else
            {
                if (datosHardware != null)
                {
                    // 1. Si el hardware mandó un pulso, lo atrapamos. 
                    // Al no borrarlo arriba, sobrevive todos los frames rápidos hasta que la física lo procese.
                    if (Mathf.Abs(datosHardware.insercionFinal) > 0.01f)
                    {
                        empujeFisico = Mathf.Clamp(datosHardware.insercionFinal, -1f, 1f);
                    }

                    // 2. Si hay silencio total del hardware por 150ms, ENTONCES lo apagamos.
                    if (Time.time - tiempoUltimoDatoHardware > 0.15f)
                    {
                        empujeFisico = 0f;
                        datosHardware.insercionFinal = 0f;
                    }

                    if (Mathf.Abs(datosHardware.volanteYFinal) > 0.001f)
                    {
                        rotX -= datosHardware.volanteYFinal * velocidadGiroPunta;
                        tocandoFlechas = true;
                    }

                    if (Mathf.Abs(datosHardware.volanteXFinal) > 0.001f)
                    {
                        rotZ += datosHardware.volanteXFinal * velocidadGiroPunta;
                        tocandoFlechas = true;
                    }
                }
            }
        }
        else
        {
            empujeFisico = 0f; // Si el tutorial nos bloquea, detenemos la fuerza
        }
        if (TutorialManager.instancia != null && ManejadorPartida.dificultad == 0)
        {
            string filtro = TutorialManager.instancia.accionEsperadaActiva;
            // Si el filtro NO está vacío, significa que el tutorial exige una acción específica
            if (!string.IsNullOrEmpty(filtro))
            {
                // Si exigen empujar ("W"), anulamos el jalón hacia atrás ("S")
                if (filtro != "W" && empujeFisico > 0) empujeFisico = 0f;
                // Si exigen jalar ("S"), anulamos el empuje hacia adelante ("W")
                if (filtro != "S" && empujeFisico < 0) empujeFisico = 0f;

                // Si la acción no es "Flechas", revertimos si intentan mover la punta con los volantes
                if (filtro != "Flechas" && tocandoFlechas)
                {
                    rotX = rotXAnterior;
                    rotZ = rotZAnterior;
                    tocandoFlechas = false;
                }
            }
        }
        if (atascadoPorBucle)
        {
            // 1. Si intenta empujar hacia adelante, anulamos la fuerza
            //if (empujeFisico > 0)
            //{
            //    empujeFisico = 0f;
            //}

            // 2. Si intenta doblar la punta, revertimos la rotación a como estaba en el frame anterior
            if (tocandoFlechas)
            {
                rotX = rotXAnterior;
                rotZ = rotZAnterior;
                tocandoFlechas = false; // Esto apaga el sensor de movimiento de la punta
            }
            controlActivo = (Mathf.Abs(empujeFisico) > 0 || tocandoFlechas);
        }
        controlActivo = (Mathf.Abs(empujeFisico) > 0 || tocandoFlechas);
        if (herramientas != null && herramientas.estaCongelado)
        {
            if (controlActivo)
            {
                if (!alertaFreezeDada && monitorUI != null)
                {
                    monitorUI.RegistrarErrorEstandarizado(MonitorEndoscopiaUI.CategoriaEvaluacion.Seguridad, 2, "Operación a ciegas: Movió el endoscopio mientras la pantalla estaba congelada.");
                    alertaFreezeDada = true;
                }

                empujeFisico = 0f;
                tocandoFlechas = false;
                controlActivo = false;
                return;
            }
            else
            {
                // Si el jugador suelta los controles (deja de moverse), "recargamos" la penalización 
                // para que le vuelva a quitar puntos si vuelve a tocar algo por error.
                alertaFreezeDada = false;
            }
        }
        else
        {
            alertaFreezeDada = false;
        }

        if (empujeFisico > 0 && !tocandoFlechas)
        {
            rotX += caidaGravedad * Time.deltaTime;
        }

        if (empujeFisico < 0)
        {
            // Enderezamos la punta MUCHO más rápido al jalar (12f en vez de 2f)
            // Si el jugador está intentando doblar la punta mientras jala, lo dejamos doblar, pero con resistencia.
            float velocidadRelajacion = tocandoFlechas ? 4f : 12f;

            rotX = Mathf.Lerp(rotX, 0f, Time.deltaTime * velocidadRelajacion);
            rotZ = Mathf.Lerp(rotZ, 0f, Time.deltaTime * velocidadRelajacion);

        }

        float limiteActual = (empujeFisico > 0) ? limiteFlexionNormal : limiteFlexionRelajada;
        rotX = Mathf.Clamp(rotX, -limiteActual, limiteActual);
        rotZ = Mathf.Clamp(rotZ, -limiteActual, limiteActual);

        
    }

    void FixedUpdate()
    {
        if (juegoTerminado || huesos.Length < 2) return;

        if (controlActivo)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Vector3 direccionFinal = huesos[0].up;
        if (empujeFisico < 0 && huesos.Length >= 4)
        {
            int huesoGuia = Mathf.Min(4, huesos.Length - 1);
            direccionFinal = (huesos[0].position - huesos[huesoGuia].position).normalized;
        }

        float velActual = (empujeFisico > 0) ? velocidadInsercion : velocidadExtraccion;
        float distanciaAvanzada = empujeFisico * velActual * Time.fixedDeltaTime;

        bool choqueFrontalActivo = false;

        // 1. CÁLCULO DE LA CURVATURA TOTAL
        float curvaturaCuerpo = 0f;
        for (int i = 0; i < huesos.Length - 1; i++)
        {
            curvaturaCuerpo += Vector3.Angle(huesos[i].up, huesos[i + 1].up);
        }

        // 2. ESCUDO PREDICTIVO
        if (empujeFisico > 0)
        {
            Vector3 origenPunta = huesos[0].position;

            if (Physics.SphereCast(origenPunta, 0.015f, direccionFinal, out RaycastHit paredHit, distanciaAvanzada + 0.015f, capaIntestino))
            {
                float anguloChoque = Vector3.Angle(direccionFinal, -paredHit.normal);

                if (anguloChoque < 35f)
                {
                    distanciaAvanzada = 0f;
                    choqueFrontalActivo = true;
                }
                else
                {
                    distanciaAvanzada = Mathf.Max(0, paredHit.distance - 0.01f);
                }
            }
        }

        // 3. LÓGICA DE FRENO ABSOLUTO POR BUCLE
        float multiplicadorAvance = 1f;

        // Reducimos el umbral. 200° es suficiente para dar curvas sanas. 
        // Si pasa de 200°, significa que el usuario está dando la vuelta (U-Turn).
        float umbralPeligro = 200f;

        // SOLO aplicamos el multiplicador 0 si está empujando. Si está jalando (extrayendo), le dejamos moverse.
        if (empujeFisico > 0)
        {
            if (choqueFrontalActivo) multiplicadorAvance = 0f;
            else if (curvaturaCuerpo > umbralPeligro) multiplicadorAvance = 0f; // FRENO 100% ABSOLUTO
        }

        // El atasco se activa si hay choque frontal o si el tubo está demasiado torcido.
        // Esto bloquea los volantes en tu función Update() para que no sigan girando la cámara.
        atascadoPorBucle = (choqueFrontalActivo && empujeFisico > 0) || (curvaturaCuerpo > umbralPeligro);

        // El daño solo ocurre si empuja mientras está atascado.
        bool bajoEstres = atascadoPorBucle && empujeFisico > 0;

        if (bajoEstres)
        {
            if (multiplicadorAvance < 0.05f)
            {
                tiempoForzandoBucle += Time.fixedDeltaTime;
                if (tiempoForzandoBucle > tiempoMaximoForzandoBucle)
                {
                    string motivo = choqueFrontalActivo ? "PERFORACIÓN INTESTINAL: Forzaste el avance frontalmente contra la mucosa." : "PERFORACIÓN INTESTINAL: Bucle crítico. El endoscopio se volcó sobre sí mismo.";
                    ProcesarGameOver(motivo);
                }
                else
                {
                    int porcentaje = (int)((tiempoForzandoBucle / tiempoMaximoForzandoBucle) * 100);
                    string alerta = choqueFrontalActivo ? "¡CHOQUE FRONTAL!" : "¡ATASCO/VUELTA EN U!";
                    Debug.LogWarning($"<color=orange>{alerta} Daño: {porcentaje}%</color>");

                    if (porcentaje >= siguienteUmbralSuavidad)
                    {
                        if (monitorUI != null) monitorUI.RegistrarErrorEstandarizado(MonitorEndoscopiaUI.CategoriaEvaluacion.Seguridad, 1, $"Suavidad de Desplazamiento: Fuerza excesiva ({porcentaje}% tensión).");
                        siguienteUmbralSuavidad += 5;
                    }
                    if (porcentaje >= 25 && !penalizadoBucle)
                    {
                        if (monitorUI != null) monitorUI.RegistrarErrorEstandarizado(MonitorEndoscopiaUI.CategoriaEvaluacion.Seguridad, 0, "Trauma Tisular: Fuerza excesiva contra la mucosa.");
                        penalizadoBucle = true;
                    }
                }
            }
        }
        else
        {
            tiempoForzandoBucle = Mathf.Max(0, tiempoForzandoBucle - (Time.fixedDeltaTime * 2f));
        }

        if (tiempoForzandoBucle == 0) { penalizadoBucle = false; siguienteUmbralSuavidad = 5; }

        bool bloqueadoPorTutorial = (TutorialManager.instancia != null && TutorialManager.instancia.controlesBloqueados);
        if (monitorUI != null && !bloqueadoPorTutorial) monitorUI.ActualizarEstadoBucle(atascadoPorBucle);

        // 4. LÓGICA DE EXTRACCIÓN BRUSCA (TIRÓN)
        float tasaCuracion = usarControlHardware ? 1.5f : 8f;
        if (empujeFisico < 0)
        {
            if (curvaturaCuerpo > 90f)
            {
                multiplicadorAvance = 1f; // Se permite extraer para desatar nudos
                if (curvaturaCuerpo > 180f)
                {
                    tiempoExtraccionBrusca += Time.fixedDeltaTime * valorDañoExtraccion;
                    if (tiempoExtraccionBrusca > tiempoMaximoTiron)
                        ProcesarGameOver("LACERACIÓN DE MUCOSA: Mantuviste un jalón prolongado sin pausas en una curva cerrada.");
                    else
                    {
                        int dolor = (int)((tiempoExtraccionBrusca / tiempoMaximoTiron) * 100);
                        if (dolor >= siguienteUmbralTiron)
                        {
                            if (monitorUI != null) monitorUI.RegistrarErrorEstandarizado(MonitorEndoscopiaUI.CategoriaEvaluacion.Seguridad, 3, "Seguridad en la Retirada: Tirones bruscos causando laceración.");
                            siguienteUmbralTiron += 20;
                            penalizadoTiron = true;
                        }
                    }
                }
                else tiempoExtraccionBrusca = Mathf.Max(0, tiempoExtraccionBrusca - (Time.fixedDeltaTime * tasaCuracion));
            }
            else tiempoExtraccionBrusca = Mathf.Max(0, tiempoExtraccionBrusca - (Time.fixedDeltaTime * tasaCuracion));
        }
        else tiempoExtraccionBrusca = Mathf.Max(0, tiempoExtraccionBrusca - (Time.fixedDeltaTime * tasaCuracion));

        if (tiempoExtraccionBrusca == 0) { penalizadoTiron = false; siguienteUmbralTiron = 35; }

        if (monitorUI != null)
        {
            int porcentajeBucle = (int)((tiempoForzandoBucle / tiempoMaximoForzandoBucle) * 100);
            int porcentajeTiron = (int)((tiempoExtraccionBrusca / tiempoMaximoTiron) * 100);

            if (porcentajeBucle > 0 || porcentajeTiron > 0)
            {
                if (porcentajeBucle >= porcentajeTiron)
                {
                    string txtPeligro = choqueFrontalActivo ? "¡Choque Frontal!" : "¡Atasco Interno!";
                    monitorUI.MostrarDanio(porcentajeBucle, txtPeligro);
                }
                else monitorUI.MostrarDanio(porcentajeTiron, "¡Fricción alta en retirada!");
            }
            else monitorUI.MostrarDanio(0, "");
        }

        // 5. APLICAR MOVIMIENTO FINAL CON CANDADO ABSOLUTO
        if (empujeFisico != 0)
        {
            distanciaAvanzada *= multiplicadorAvance;

            // --- AQUÍ ESTÁ LA MAGIA ---
            // Si la distancia cayó a 0 (porque estás en bucle o chocado de frente), 
            // CORTAMOS LA FUNCIÓN AQUÍ MISMO con un 'return'.
            // Esto evita que se ejecute el rb.MovePosition y el acumulador Slerp.
            // Consecuencia: Congelación total de la física. Imposible deslizarse o dar la vuelta.
            if (Mathf.Abs(distanciaAvanzada) < 0.0001f && empujeFisico > 0)
            {
                rb.velocity = Vector3.zero; // Aniquilamos cualquier inercia de Unity
                return;
            }

            distanciaTotalInsertada += distanciaAvanzada;
            if (distanciaTotalInsertada < 0) distanciaTotalInsertada = 0f;

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
                    if (direccionMovimiento.magnitude > 0.04f && Vector3.Dot(baseHueso.up, direccionMovimiento.normalized) > 0.2f)
                        rutaTubo.Add(baseHueso.position);
                }
                else if (empujeFisico < 0 && rutaTubo.Count > 1)
                {
                    while (rutaTubo.Count > 1 && Vector3.Distance(baseHueso.position, rutaTubo[rutaTubo.Count - 2]) <= Vector3.Distance(baseHueso.position, rutaTubo[rutaTubo.Count - 1]))
                        rutaTubo.RemoveAt(rutaTubo.Count - 1);
                }
                lr.positionCount = rutaTubo.Count;
                lr.SetPositions(rutaTubo.ToArray());
                lr.SetPosition(rutaTubo.Count - 1, baseHueso.position);
            }
        }
    }

    void ProcesarGameOver(string motivo)
    {
        if (juegoTerminado) return;
        juegoTerminado = true;
        Time.timeScale = 0f;

        Debug.LogError($"<color=red><b>GAME OVER:</b> {motivo}</color>");

        //le mandamos el motivo a la interfaz para que muestre el PopUp
        if (monitorUI != null)
        {
            monitorUI.MostrarGameOver(motivo);
        }
    }

    void LateUpdate()
    {
        if (juegoTerminado || huesos.Length < 2) return;

        // La rotación axial Y multiplicada por el Torque 
        olaDeCurvas[0] = olaDeCurvas[1] * Quaternion.Euler(rotX, 0, rotZ);
        Quaternion curvaCuello = Quaternion.identity;

        for (int i = 1; i < huesos.Length; i++)
        {
            Quaternion curvaSuave = (empujeFisico >= 0)
                ? Quaternion.Slerp(olaDeCurvas[i], olaDeCurvas[i - 1], distanciaAcumulada / longitudHueso)
                : Quaternion.Slerp(olaDeCurvas[i], olaDeCurvas[Mathf.Min(i + 1, huesos.Length - 1)], Mathf.Abs(distanciaAcumulada) / longitudHueso);

            if (i == 1) curvaCuello = curvaSuave;

            Quaternion rotacionObjetivo = rotacionesGlobalesIniciales[i] * curvaSuave;
            huesos[i].rotation = Quaternion.Slerp(huesos[i].rotation, rotacionObjetivo, Time.deltaTime * suavidadGiroHuesos);
        }

        Quaternion curvaPunta = curvaCuello * Quaternion.Euler(rotX, 0, rotZ);
        Quaternion objPunta = rotacionesGlobalesIniciales[0] * curvaPunta;
        huesos[0].rotation = Quaternion.Slerp(huesos[0].rotation, objPunta, Time.deltaTime * suavidadGiroHuesos);
    }
}