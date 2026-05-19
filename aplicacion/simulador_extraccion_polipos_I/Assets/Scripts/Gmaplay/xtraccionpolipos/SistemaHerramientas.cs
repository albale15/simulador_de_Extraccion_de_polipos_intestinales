using UnityEngine;
using System.Collections;

public class SistemaHerramientas : MonoBehaviour
{
    [Header("Referencias Anatómicas")]
    public Transform canalDeTrabajo;
    public EndoscopioCurvas endoscopio;
    public Camera camaraPrincipal;

    [Header("Herramienta: Pinza Biopsia (<5cm)")]
    public GameObject pinzaDientes;
    public Transform pinzaDerecha;
    public Transform pinzaIzquierda;
    public float anguloAperturaPinza = 45f;

    [Header("Herramienta: Asa Diatérmica (>5cm)")]
    public GameObject pinzaAsas;
    public Transform lazoBezier;
    public Vector3 escalaLazoCerrado = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("Efectos Visuales de Herramientas")]
    public LineRenderer cableHerramienta;

    [Header("Configuración de Interacción")]
    public float distanciaAccion = 0.2f;
    public float anguloTolerancia = 35f;
    public float anguloToleranciaFoto = 25f;
    public float distanciaExtensionHerramienta = 0.05f;
    public LayerMask capaPolipos;

    [Header("Efecto de Lente Sucio")]
    public UnityEngine.UI.Image imagenSuciedadLente; // imagen UI con textura de suciedad/gotas
    public LayerMask capaIntestino; // capa de las paredes del intestino
    public float duracionDesvanecimientoLimpieza = 2f; // Tiempo en segundos que tarda en limpiarse del todo
    private Coroutine rutinaLimpiezaActiva = null; // Para asegurar que solo corra una limpieza a la vez
    private bool estaLimpiando = false; // Bandera para bloquear que se ensucie mientras se limpia
    private float nivelSuciedad = 0f;

    [Header("Manejo de Fluidos (Succión)")]
    public ParticleSystem particulasSangrado; // Arrastraremos el Particle System aquí
    private float nivelSangrado = 0f;
    private int lavadosSinSuccionar = 0; // Castiga si el doctor echa mucha agua sin aspirar
    private bool estabaSuccionandoSangre = false;

    private float tiempoLenteSucio = 0f;
    private bool penalizadoPorSuciedad = false;

    private float tiempoSangrandoSinSuccion = 0f;
    private bool penalizadoPorSangrado = false;
    [Header("Estado del Sistema")]
    public bool estaCortando = false;
    public bool llevandoPolipo = false;
    public bool estaCongelado = false;

    // Sensor de zona de extracción
    [HideInInspector]
    public bool enZonaExtraccion = false;

    private bool estaEnZoom = false;
    private float fovOriginal;

    private bool enModoSeleccion = false;

    [HideInInspector]
    public int[] yamadasEliminados = new int[4] { 0, 0, 0, 0 };

    public PolipoInteractuable polipoEnMira;
    private Vector3 posInicialPunta;
    private Quaternion rotInicialPunta;
    private MonitorEndoscopiaUI monitorUI;

    // Memoria para Higiene
    public PolipoInteractuable ultimoPolipoCortado;

    // Contadores de movimientos para higiene
    private int movimientosSinSuccionar = 0;
    private bool endoscopioEstabaMoviendo = false;
    private int movimientosEnSeleccion = 0; // Para cancelar el menú si se mueve

    private DatosProcesados datosHardware;
    private bool ultimoF, ultimoC, ultimoZ, ultimoS, ultimoA, ultimoL;
    [Header("Fin de Procedimiento")]
    public bool enModoConfirmarSalida = false;
    public bool EstaEnModoSeleccion() { return enModoSeleccion; }
    public PolipoInteractuable ObtenerPolipoEnMira() { return polipoEnMira; }
    public PolipoInteractuable ObtenerUltimoPolipoCortado() { return ultimoPolipoCortado; }
    public float ObtenerNivelSangrado() { return nivelSangrado; }
    public float ObtenerNivelSuciedad() { return nivelSuciedad; }
    public int ObtenerLavadosSinSuccionar() { return lavadosSinSuccionar; }


 
    private bool forzarPrimerSangradoTutorial = true;    // Variable para asegurar que el tutorial muestre la emergencia al menos una vez
    private Vector3 escalaOriginalLazoBase;
    void Start()
    {
        if (ManejadorPartida.dificultad == 0)
        {
            anguloTolerancia = 90f; // <--- CÁMBIALO A 2f
            anguloToleranciaFoto = 40f; // <--- CÁMBIALO A 2f (o déjalo más alto si solo quieres castigar la pinza)
            // Detiene la ejecución del script aquí
        }
        if (lazoBezier != null)
        {
            escalaOriginalLazoBase = lazoBezier.localScale; // Guardamos su tamaño real de trabajo
        }
        if (ManejadorPartida.dificultad == 0)
        {
            anguloTolerancia = 180f;
            anguloToleranciaFoto = 180f;
            // Detiene la ejecución del script aquí
        }
        pinzaDientes.SetActive(false);
        pinzaAsas.SetActive(false);
        monitorUI = FindObjectOfType<MonitorEndoscopiaUI>();

        if (camaraPrincipal == null) camaraPrincipal = Camera.main;
        if (camaraPrincipal != null) fovOriginal = camaraPrincipal.fieldOfView;
        if (particulasSangrado != null)
        {
            particulasSangrado.Stop(); // Asegura que no haya sangre al empezar
        }
    }

    void OnEnable()
    {
        if (ConfigManager.instancia != null)
            ConfigManager.instancia.AlRecibirDatosProcesados += ActualizarDatosHardware;
    }

    void OnDisable()
    {
        if (ConfigManager.instancia != null)
            ConfigManager.instancia.AlRecibirDatosProcesados -= ActualizarDatosHardware;
    }

    private void ActualizarDatosHardware(DatosProcesados datos)
    {
        datosHardware = datos;
    }

    void Update()
    {
        bool btnFreeze = false, btnCapture = false, btnZoom = false, btnSuccion = false, btnAccion = false, btnLimpiado = false;
        bool moviendo = false; // Sensor local de movimiento
        bool manteniendoSuccion = false;

        bool modoPC = false;
        if (endoscopio != null)
            modoPC = !endoscopio.usarControlHardware;
        else
            modoPC = true;
        
        if (modoPC)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) { btnFreeze = true; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { btnCapture = true; }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { btnZoom = true; }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { btnSuccion = true; }
            if (Input.GetKeyDown(KeyCode.Alpha5)) { btnAccion = true; }
            if (Input.GetKeyDown(KeyCode.Alpha6)) { btnLimpiado = true; }


            manteniendoSuccion = Input.GetKey(KeyCode.Alpha4); // Detecta si lo mantiene apretado
            // Verifica si está tocando teclas de movimiento
            moviendo = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
        }
        else
        {
            if (datosHardware != null)
            {
                btnFreeze = (datosHardware.botonFreeze && !ultimoF);
                btnCapture = (datosHardware.botonCapture && !ultimoC);
                btnZoom = (datosHardware.botonZoom && !ultimoZ);
                btnSuccion = (datosHardware.botonSuccion && !ultimoS);
                btnAccion = (datosHardware.botonAccion && !ultimoA);
                btnLimpiado = (datosHardware.botonLimpiado && !ultimoL);

                manteniendoSuccion = datosHardware.botonSuccion; // Detecta si el hardware manda señal continua
                // Verifica si los valores analógicos del hardware superan el reposo
                moviendo = Mathf.Abs(datosHardware.insercionFinal) > 0.05f || Mathf.Abs(datosHardware.volanteXFinal) > 0.05f || Mathf.Abs(datosHardware.volanteYFinal) > 0.05f;
                ultimoF = datosHardware.botonFreeze;
                ultimoC = datosHardware.botonCapture;
                ultimoZ = datosHardware.botonZoom;
                ultimoS = datosHardware.botonSuccion;
                ultimoA = datosHardware.botonAccion;
                ultimoL = datosHardware.botonLimpiado;
            }
        }

        Vector3 origenRayo = canalDeTrabajo.position;
        Vector3 direccionRayo = canalDeTrabajo.forward;
        bool bloqueadoPorTutorial = (TutorialManager.instancia != null && TutorialManager.instancia.controlesBloqueados);
        // ENSUCIAR EL LENTE POR ROCE 
        if (moviendo && !bloqueadoPorTutorial && !estaCongelado)
        {
            // Creamos una esfera invisible de 2cm en la punta. Si toca la pared, se ensucia gradualmente.
            if (Physics.CheckSphere(origenRayo, 0.10f, capaIntestino))
            {
                nivelSuciedad += Time.deltaTime * 0.05f; // Ajusta el 0.35f si quieres que se ensucie más rápido o lento
                nivelSuciedad = Mathf.Clamp01(nivelSuciedad); // Mantiene el valor entre 0 (limpio) y 1 (opaco)
            }
        }

        // Actualizamos la opacidad de la imagen de suciedad en la pantalla
        if (imagenSuciedadLente != null)
        {
            Color colorSuciedad = imagenSuciedadLente.color;
            colorSuciedad.a = nivelSuciedad;
            imagenSuciedadLente.color = colorSuciedad;
        }
        if (nivelSuciedad > 0.5f) // Si está sucio a más del 50%
        {
            tiempoLenteSucio += Time.deltaTime;

            // Si pasan 6 segundos y aún no lo hemos penalizado
            if (tiempoLenteSucio > 6f && !penalizadoPorSuciedad)
            {
                EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 9, "Mala higiene visual: El lente estuvo muy obstruido por más de 6 segundos sin irrigación.");
                penalizadoPorSuciedad = true; // Seguro para que no le quite puntos 60 veces por segundo
            }
        }
        else
        {
            // Si el doctor limpia el lente (nivel baja del 50%), reiniciamos todo
            tiempoLenteSucio = 0f;
            penalizadoPorSuciedad = false;
        }

        // EFECTO 3D DE SANGRADO Y SUCCIÓN

        if (nivelSangrado > 0)
        {
            if (manteniendoSuccion && !estaCongelado)
            {
                estabaSuccionandoSangre = true;
                nivelSangrado -= Time.deltaTime * 0.5f; // Tarda 2 segundos en aspirarse
                nivelSangrado = Mathf.Max(0, nivelSangrado);

                // Si ya aspiró toda la sangre, apagamos las partículas
                if (nivelSangrado == 0 && particulasSangrado != null && particulasSangrado.isPlaying)
                {
                    particulasSangrado.Stop();
                    EnviarInfoUI("Hemorragia controlada. Campo visual despejado.", "#00FF00");
                    estabaSuccionandoSangre = false;

                    // Reseteamos los castigos para el próximo pólipo
                    tiempoSangrandoSinSuccion = 0f;
                    penalizadoPorSangrado = false;
                }
            }
            else
            {
                // EL CRONÓMETRO: Si hay sangre y NO está apretando el botón, el tiempo corre
                tiempoSangrandoSinSuccion += Time.deltaTime;

                // Si pasan 5 segundos y no lo hemos castigado aún
                if (tiempoSangrandoSinSuccion >= 5f)
                {
                    EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Seguridad, 1, "Negligencia Quirúrgica: Demoró más de 5 segundos en atender la hemorragia.");
                    tiempoSangrandoSinSuccion = 0f;
                }

                // Castigo por interrupción (Soltó el botón antes de tiempo)
                if (estabaSuccionandoSangre)
                {
                    EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Seguridad, 1, "Manejo de Fluidos: Interrumpió la aspiración antes de controlar totalmente la hemorragia.");
                    estabaSuccionandoSangre = false;
                }
            }
        }
        else
        {
            // Seguridad: Si no hay sangre, los relojes siempre están en cero
            tiempoSangrandoSinSuccion = 0f;
            penalizadoPorSangrado = false;
        }

        // Si la pantalla está llena de partículas de sangre, bloqueamos herramientas y cámara
        if (nivelSangrado > 0.3f && (btnCapture || btnFreeze || btnAccion))
        {
            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 9, "Campo visual obstruido por hemorragia. Mantenga la Succión para limpiar.");
            btnCapture = false;
            btnFreeze = false;
            btnAccion = false;
        }

        // Si la pantalla está llena de partículas de sangre, bloqueamos herramientas y cámara
        if (nivelSangrado > 0.3f && (btnCapture || btnFreeze || btnAccion))
        {
            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 9, "Campo visual obstruido por hemorragia. Mantenga la Succión para limpiar.");
            btnCapture = false;
            btnFreeze = false;
            btnAccion = false;
        }


        if (bloqueadoPorTutorial)
        {
            btnFreeze = false;
            btnCapture = false;
            btnZoom = false;
            btnSuccion = false;
            btnAccion = false;
            btnLimpiado = false;
            moviendo = false;
        }
        if (TutorialManager.instancia != null && ManejadorPartida.dificultad == 0)
        {
            string filtro = TutorialManager.instancia.accionEsperadaActiva;
            if (!string.IsNullOrEmpty(filtro))
            {
                // Apagamos implacablemente cualquier botón que no sea el que el tutorial pidió
                if (filtro != "Freeze") btnFreeze = false;
                if (filtro != "Capture") btnCapture = false;
                if (filtro != "Accion") btnAccion = false;
                if (filtro != "Succion") btnSuccion = false;
                if (filtro != "Zoom") btnZoom = false;
                if (filtro != "Limpiado") btnLimpiado = false;
            }
        }
        // LÓGICA DE HIGIENE: 3 MOVIMIENTOS
        if (ultimoPolipoCortado != null && ultimoPolipoCortado.estadoActual == PolipoInteractuable.EstadoPolipo.CortadoSuelto)
        {
            if (moviendo && !endoscopioEstabaMoviendo)
            {
                movimientosSinSuccionar++;
            }

            if (movimientosSinSuccionar >= 3)
            {
                float distanciaAlResto = Vector3.Distance(origenRayo, ultimoPolipoCortado.transform.position);
                if (distanciaAlResto > 0.3f)
                {
                    EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 9, "Mala higiene visual: Abandonó el área dejando tejido suelto sin recuperar.");
                    ultimoPolipoCortado = null; // Detiene el castigo
                    movimientosSinSuccionar = 0;
                }
            }
        }

        // CANCELAR SELECCIÓN SI SE MUEVE
        if (enModoSeleccion)
        {
            if (moviendo && !endoscopioEstabaMoviendo)
            {
                movimientosEnSeleccion++;
            }

            if (movimientosEnSeleccion > 1) // Si hace más de 1 movimiento, se sale del menú
            {
                EnviarInfoUI("Selección de herramienta cancelada por movimiento del endoscopio.", "#FF8C00");
                ActivarModoSeleccion(false);
            }
        }
        else
        {
            movimientosEnSeleccion = 0; // Se resetea cuando no estamos en el menú
        }

        endoscopioEstabaMoviendo = moviendo;
        // ----------------------------------------------------

        //BLOQUEO DE FREEZE INTACTO QUE TÚ PROGRAMASTE
        if (estaCongelado)
        {
            if (btnAccion || btnZoom || btnSuccion || btnLimpiado)
            {
                EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Seguridad, 2, "Operación a ciegas: Intentó usar herramientas con la imagen congelada.");
            }

            if (btnCapture) EjecutarCapture();
            if (btnFreeze) EjecutarFreeze();

            return;
        }

        if (!enModoSeleccion)
        {
            if (btnZoom && camaraPrincipal != null) EjecutarZoom();
            if (btnFreeze) EjecutarFreeze();
            if (btnCapture) EjecutarCapture();
            if (btnLimpiado) EjecutarLimpiado();

            // --- LÓGICA DE SUCCIÓN MANUAL Y CONTAMINACIÓN ---
            if (btnSuccion)
            {
                if (llevandoPolipo)
                {
                    if (enZonaExtraccion)
                    {
                        SoltarPolipoEnLaboratorio();
                    }
                    else
                    {
                        EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 9, "Contaminación: No puede soltar la muestra dentro del tracto. Llévela hasta la salida.");
                    }
                }
                else if (nivelSangrado > 0f)
                {
                    EnviarInfoUI("Succión activada. Aspirando hemorragia...", "#1E90FF");
                }
                // PRIORIDAD 3: Hay agua de lavado acumulada (Ignora la herramienta)
                else if (lavadosSinSuccionar > 0)
                {
                    lavadosSinSuccionar = 0; // Vaciamos el agua acumulada
                    EnviarInfoUI("Succión activada. Aspirando fluidos de irrigación...", "#1E90FF");
                }
                // PRIORIDAD 4: El campo visual está limpio, se procede a intentar atrapar
                else
                {
                    IntentarAtrapar(origenRayo, direccionRayo);
                }
            }
        }

        if (estaCortando) { VerificarMovimientoProhibido(); return; }

        if (Physics.Raycast(origenRayo, direccionRayo, out RaycastHit hit, distanciaAccion, capaPolipos))
        {
            polipoEnMira = hit.collider.GetComponent<PolipoInteractuable>();

            if (polipoEnMira != null && polipoEnMira.estadoActual == PolipoInteractuable.EstadoPolipo.Intacto)
            {
                float anguloAtaque = Vector3.Angle(direccionRayo, -hit.normal);

                if (!enModoSeleccion)
                {
                    if (btnAccion)
                    {
                        if (!polipoEnMira.fueFotografiado)
                        {
                            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 4, "Omisión de Protocolo: Debe congelar (Freeze) y documentar (Capture) antes de intervenir.");
                            return;
                        }

                        if (llevandoPolipo)
                        {
                            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 8, "Suelte el pólipo anterior en la salida primero.");
                            return;
                        }
                        if (anguloAtaque > anguloTolerancia)
                        {
                            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 8, $"Ángulo incorrecto ({anguloAtaque:F1}°). Alinee el canal de trabajo.");
                            return;
                        }

                        ActivarModoSeleccion(true);
                    }
                }
                else
                {
                    if (btnAccion)
                    {
                        ActivarModoSeleccion(false);
                    }
                    else if (btnFreeze)
                    {
                        ProcesarCorteManual(true);
                    }
                    else if (btnCapture)
                    {
                        ProcesarCorteManual(false);
                    }
                }
            }
        }
        else
        {
            polipoEnMira = null;
            if (enModoSeleccion) ActivarModoSeleccion(false);
        }

        if (enModoConfirmarSalida)
        {
            // Si el médico mueve el endoscopio, cancelamos la salida por precaución
            if (moviendo && !endoscopioEstabaMoviendo)
            {
                if (monitorUI != null) monitorUI.BotonConfirmarNo();
                EnviarInfoUI("Confirmación cancelada por movimiento.", "#FF8C00");
            }
            else if (btnFreeze) // Botón 1 = SÍ (Aceptar)
            {
                if (monitorUI != null) monitorUI.BotonConfirmarSi();
            }
            else if (btnCapture || btnAccion) // Botón 2 o Accion = NO (Cancelar)
            {
                if (monitorUI != null) monitorUI.BotonConfirmarNo();
            }

            endoscopioEstabaMoviendo = moviendo;
            return; // Evita que se activen otras herramientas mientras el popup esté abierto
        }

        // Y para invocarlo cuando esté en la zona:
        if (enZonaExtraccion && btnAccion && polipoEnMira == null && !enModoSeleccion && !llevandoPolipo)
        {
            ActivarModoSalida(true);
            return;
        }

    }
    void LateUpdate()
    {
        if (cableHerramienta != null)
        {
            // Si la Pinza Fría está encendida
            if (pinzaDientes != null && pinzaDientes.activeInHierarchy)
            {
                cableHerramienta.enabled = true;
                cableHerramienta.SetPosition(0, canalDeTrabajo.position); // Inicio en el endoscopio
                cableHerramienta.SetPosition(1, pinzaDientes.transform.position); // Fin en la pinza
            }
            // Si el Asa Caliente está encendida
            else if (pinzaAsas != null && pinzaAsas.activeInHierarchy)
            {
                cableHerramienta.enabled = true;
                cableHerramienta.SetPosition(0, canalDeTrabajo.position);
                cableHerramienta.SetPosition(1, pinzaAsas.transform.position);
            }
            // Si ninguna está activa, apagamos el cable
            else
            {
                cableHerramienta.enabled = false;
            }
        }
    }
    //FUNCIÓN DE LAVADO DE LENTE
    private void EjecutarLimpiado()
    {
        if (nivelSuciedad > 0f)
        {
            // Si ya se estaba limpiando, detenemos esa rutina para empezar una nueva (evita errores)
            if (rutinaLimpiezaActiva != null) StopCoroutine(rutinaLimpiezaActiva);

            // Lanzamos el desvanecimiento progresivo
            rutinaLimpiezaActiva = StartCoroutine(RutinaDesvanecimientoLimpiezaLente());

            EnviarInfoUI("Lavado de lente iniciado...", "#00FFFF");
        }
        else
        {
            EnviarInfoUI("Lavado de lente activado.", "#00FFFF");
        }
        // Penalización por inundar al paciente
        lavadosSinSuccionar++;
        if (lavadosSinSuccionar >= 3)
        {
            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Seguridad, 1, "Acumulación de fluidos: Ha irrigado demasiada agua sin succionar. Riesgo de aspiración.");
        }
    }

    // La Corrutina que hace el trabajo matemático frame a frame
    private IEnumerator RutinaDesvanecimientoLimpiezaLente()
    {
        estaLimpiando = true; // Bloqueamos el ensuciado por roce temporalmente
        float velocidadLimpieza = 1f / duracionDesvanecimientoLimpieza; // Calculamos cuánto quitar por segundo

        // Mientras siga quedando suciedad...
        while (nivelSuciedad > 0f)
        {
            // Reducimos el nivelSuciedad progresivamente basado en el tiempo transcurrido
            nivelSuciedad -= velocidadLimpieza * Time.deltaTime;

            // Nos aseguramos de no bajar de cero
            nivelSuciedad = Mathf.Max(0f, nivelSuciedad);

            // Update() automáticamente leerá este 'nivelSuciedad' actualizado y suavizará la opacidad visual de la imagen en pantalla en el siguiente frame.

            yield return null; // Esperamos al siguiente frame
        }

        // Finalización
        nivelSuciedad = 0f; // Aseguramos el cero absoluto
        EnviarInfoUI("Lavado de lente completado.", "#00FFFF");
        rutinaLimpiezaActiva = null; // Liberamos la referencia de la rutina
        estaLimpiando = false; // Permitimos que se vuelva a ensuciar si choca
    }
    private void ActivarModoSeleccion(bool activar)
    {
        enModoSeleccion = activar;
        if (monitorUI != null) monitorUI.ActualizarTextosBotones(activar);

        if (activar) EnviarInfoUI("Modo Herramientas: Seleccione Pinza de Biopsia (1) o Asa de Polipectomía (2)", "#FFFFFF");
        else EnviarInfoUI("Modo Herramientas Cancelado", "#888888");
    }

    private void ProcesarCorteManual(bool esPinza)
    {
        ActivarModoSeleccion(false);

        if (estaCongelado) EjecutarFreeze();

        if (esPinza)
        {
            EnviarInfoUI("Preparando Pinza de Biopsia...", "#FFFF00");

            if (polipoEnMira.tamanoMilimetros <= 5f)
            {
                StartCoroutine(AnimacionPinzaFria());
            }
            else
            {
                EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 6, $"Error de Criterio: El pólipo mide {polipoEnMira.tamanoMilimetros:F1}mm. Pólipos mayores a 5mm requieren Asa de Polipectomía por riesgo de sangrado.");
            }
        }
        else
        {
            EnviarInfoUI("Preparando Asa de Polipectomía...", "#FF4500");

            if (polipoEnMira.tamanoMilimetros > 5f)
            {
                StartCoroutine(AnimacionAsaCaliente());
            }
            else
            {
                EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 6, $"Error de Criterio: El pólipo es diminuto ({polipoEnMira.tamanoMilimetros:F1}mm). El Asa de Polipectomía resbalará o quemará tejido sano innecesariamente. Use Pinza de Biopsia.");
            }
        }
    }

    private void EjecutarZoom()
    {
        estaEnZoom = !estaEnZoom;
        camaraPrincipal.fieldOfView = estaEnZoom ? (fovOriginal / 1.6f) : fovOriginal;
        EnviarInfoUI(estaEnZoom ? "Zoom Óptico Activado" : "Zoom Óptico Desactivado", "#FF8C00");
    }

    private void EjecutarFreeze()
    {
        estaCongelado = !estaCongelado;
        if (estaCongelado)
        {
            Time.timeScale = 0.0001f;
            EnviarInfoUI("Imagen Congelada (Freeze)", "#00FFFF");
        }
        else
        {
            Time.timeScale = 1f;
            EnviarInfoUI("Imagen Descongelada", "#00FFFF");
        }
    }

    private void EjecutarCapture()
    {
        if (estaCongelado)
        {
            EnviarInfoUI("Fotografía guardada en expediente del paciente.", "#FFD700");
            if (nivelSuciedad > 0.4f)
            {
                // Penaliza el índice 5 (Calidad de Captura)
                EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 5, "Calidad Fotográfica Deficiente: Lente obstruido por suciedad durante la captura.");
                return;
            }

            if (polipoEnMira != null && polipoEnMira.estadoActual == PolipoInteractuable.EstadoPolipo.Intacto)
            {
                Vector3 direccionAlPolipo = (polipoEnMira.transform.position - canalDeTrabajo.position).normalized;
                float anguloCentrado = Vector3.Angle(canalDeTrabajo.forward, direccionAlPolipo);

                if (anguloCentrado > anguloToleranciaFoto)
                {
                    EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 5, $"Calidad Fotográfica Deficiente: Pólipo descentrado ({anguloCentrado:F1}°).");
                }
                else
                {
                    EnviarInfoUI($"Calidad de Foto Óptima (Ángulo: {anguloCentrado:F1}°)", "#32CD32");
                }

                polipoEnMira.fueFotografiado = true;
            }
        }
        else
        {
            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Protocolo, 4, "No se puede capturar foto en movimiento. Congele (Freeze) la imagen primero.");
        }
    }

    //private void IntentarSuccion(Vector3 origen, Vector3 direccion)
    //{
    //    if (Physics.Raycast(origen, direccion, out RaycastHit hit, distanciaAccion * 1.5f, capaPolipos))
    //    {
    //        PolipoInteractuable polipoTocado = hit.collider.GetComponent<PolipoInteractuable>();

    //        if (polipoTocado != null && polipoTocado.estadoActual == PolipoInteractuable.EstadoPolipo.CortadoSuelto)
    //        {
    //            StartCoroutine(RutinaSuccion(polipoTocado));

    //            // Limpia la higiene al succionar
    //            if (ultimoPolipoCortado == polipoTocado)
    //            {
    //                ultimoPolipoCortado = null;
    //                movimientosSinSuccionar = 0;
    //            }
    //        }
    //        else if (polipoTocado != null && polipoTocado.estadoActual == PolipoInteractuable.EstadoPolipo.Intacto)
    //        {
    //            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 9, "Enfermera: No podemos succionar un pólipo que no ha sido cortado.");
    //        }
    //    }
    //}

    //private IEnumerator RutinaSuccion(PolipoInteractuable polipo)
    //{
    //    float tiempo = 0;
    //    Vector3 posInicial = polipo.transform.position;

    //    while (tiempo < 1f)
    //    {
    //        tiempo += Time.unscaledDeltaTime * 5f;
    //        polipo.transform.position = Vector3.Lerp(posInicial, canalDeTrabajo.position, tiempo);
    //        yield return null;
    //    }

    //    polipo.SerSuccionado(canalDeTrabajo);
    //    llevandoPolipo = true;
    //    EnviarInfoUI("Pólipo succionado. Proceda a retirarlo del paciente.", "#1E90FF");
    //}


    private void IntentarAtrapar(Vector3 origen, Vector3 direccion)
    {
        if (Physics.Raycast(origen, direccion, out RaycastHit hit, distanciaAccion * 1.5f, capaPolipos))
        {
            PolipoInteractuable polipoTocado = hit.collider.GetComponent<PolipoInteractuable>();

            if (polipoTocado != null && polipoTocado.estadoActual == PolipoInteractuable.EstadoPolipo.CortadoSuelto)
            {
                StartCoroutine(AnimacionAsaAtrapar(polipoTocado));

                // Limpia la higiene al atraparlo
                if (ultimoPolipoCortado == polipoTocado)
                {
                    ultimoPolipoCortado = null;
                    movimientosSinSuccionar = 0;
                }
            }
            else if (polipoTocado != null && polipoTocado.estadoActual == PolipoInteractuable.EstadoPolipo.Intacto)
            {
                EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 9, "Enfermera: No podemos atrapar un pólipo que no ha sido seccionado.");
            }
        }
    }

    private IEnumerator AnimacionAsaAtrapar(PolipoInteractuable polipo)
    {
        IniciarCorteSeguro();
        pinzaAsas.SetActive(true);

        pinzaAsas.transform.localPosition = Vector3.zero;
        // Calculamos la distancia exacta hasta donde quedó flotando el pólipo suelto
        float distanciaReal = Vector3.Distance(canalDeTrabajo.position, polipo.transform.position);
        Vector3 posExtendida = new Vector3(0, 0, distanciaReal);
        Vector3 escalaOriginalLazo = escalaOriginalLazoBase;

        // 1. Extiende el Asa
        yield return MoverHerramienta(pinzaAsas.transform, Vector3.zero, posExtendida, 0.5f);
        if (!estaCortando) yield break;

        // 2. Trae el pólipo hacia el asa (simulando que el médico lo enlaza)
        float tiempo = 0;
        Vector3 posInicial = polipo.transform.position;
        while (tiempo < 1f && estaCortando)
        {
            tiempo += Time.unscaledDeltaTime * 4f;
            polipo.transform.position = Vector3.Lerp(posInicial, pinzaAsas.transform.position, tiempo);
            yield return null;
        }

        // 3. Cierra el lazo para atraparlo
        yield return EscalarLazo(escalaOriginalLazo, escalaLazoCerrado, 0.5f);
        if (!estaCortando) yield break;

        // 4. Pegamos el pólipo al Asa temporalmente para que viajen juntos
        polipo.transform.SetParent(pinzaAsas.transform, true);

        // 5. Retrae el Asa (el pólipo será arrastrado suavemente por la pantalla)
        yield return MoverHerramienta(pinzaAsas.transform, posExtendida, Vector3.zero, 0.5f);
        if (!estaCortando) yield break;

        // 6. Ahora que ya llegaron al lente, activamos el estado final en el canal de trabajo
        polipo.SerAtrapado(canalDeTrabajo);
        llevandoPolipo = true;
        EnviarInfoUI("Pólipo asegurado con Asa de Polipectomía. Extraiga el endoscopio.", "#1E90FF");

        lazoBezier.localScale = escalaOriginalLazo;
        TerminarCorteSeguro();
    }


    private void SoltarPolipoEnLaboratorio()
    {
        llevandoPolipo = false;
        RefrescarUIBotones();
        int poliposBorrados = 0;

        foreach (Transform hijo in canalDeTrabajo)
        {
            PolipoInteractuable polipo = hijo.GetComponent<PolipoInteractuable>();
            if (polipo != null)
            {
                SumarPolipoEliminado(polipo.tipo);
                Destroy(hijo.gameObject);
                poliposBorrados++;
            }
        }
        EnviarInfoUI($"Extracción Exitosa. {poliposBorrados} muestra(s) depositada(s) en laboratorio.", "#32CD32");
    }

    private IEnumerator AnimacionPinzaFria()
    {
        IniciarCorteSeguro();
        pinzaDientes.SetActive(true);

        pinzaDientes.transform.localPosition = Vector3.zero;
        // Medimos la distancia exacta entre la punta del endoscopio y el pólipo
        float distanciaReal = Vector3.Distance(canalDeTrabajo.position, polipoEnMira.transform.position);
        // Le restamos 0.015f para que la pinza se detenga "mordiendo" la superficie, no el centro
        Vector3 posExtendida = new Vector3(0, 0, distanciaReal - 0.015f);

        yield return MoverHerramienta(pinzaDientes.transform, Vector3.zero, posExtendida, 0.5f);
        if (!estaCortando) yield break;

        yield return RotarPinzas(0, anguloAperturaPinza, 0.3f);
        if (!estaCortando) yield break;

        yield return RotarPinzas(anguloAperturaPinza, 0, 0.2f);
        if (!estaCortando) yield break;

        polipoEnMira.ProcesarCorte();

        bool debeSangrar = (Random.value <= 0.30f);
        if (ManejadorPartida.dificultad == 0 && forzarPrimerSangradoTutorial)
        {
            debeSangrar = true;
            forzarPrimerSangradoTutorial = false; // Desactivamos la trampa para los siguientes cortes
        }

        if (debeSangrar)
        {
            nivelSangrado = 1.0f;
            if (particulasSangrado != null) particulasSangrado.Play(); // Dispara la nube de sangre
            EnviarInfoUI("Hemorragia leve post-corte. Mantenga Succión para aspirar los fluidos.", "#FF0000");
        }

        ultimoPolipoCortado = polipoEnMira;
        movimientosSinSuccionar = 0;

        SumarPolipoEliminado(polipoEnMira.tipo);
        EnviarInfoUI($"Pólipo {polipoEnMira.tipo} extraído con éxito.", "#00FF00");

        yield return MoverHerramienta(pinzaDientes.transform, posExtendida, Vector3.zero, 0.5f);

        TerminarCorteSeguro();
    }

    private IEnumerator AnimacionAsaCaliente()
    {
        IniciarCorteSeguro();
        pinzaAsas.SetActive(true);

        pinzaAsas.transform.localPosition = Vector3.zero;
        // CÁLCULO DE DISTANCIA DINÁMICA
        float distanciaReal = Vector3.Distance(canalDeTrabajo.position, polipoEnMira.transform.position);
        // Le restamos solo 0.005f porque el asa necesita abrazar un poco más adentro que la pinza
        Vector3 posExtendida = new Vector3(0, 0, distanciaReal - 0.15f);
        Vector3 escalaOriginalLazo = escalaOriginalLazoBase;

        yield return MoverHerramienta(pinzaAsas.transform, Vector3.zero, posExtendida, 0.8f);
        if (!estaCortando) yield break;

        yield return EscalarLazo(escalaOriginalLazo, escalaLazoCerrado, 1.0f);
        if (!estaCortando) yield break;

        yield return new WaitForSeconds(1.0f);
        if (!estaCortando) yield break;

        polipoEnMira.ProcesarCorte();
        if (Random.value <= 0.30f)
        {
            nivelSangrado = 1.0f;
            if (particulasSangrado != null) particulasSangrado.Play(); // Dispara la nube de sangre
            EnviarInfoUI("Hemorragia leve post-corte. Mantenga Succión para aspirar los fluidos.", "#FF0000");
        }
        ultimoPolipoCortado = polipoEnMira;
        movimientosSinSuccionar = 0;

        EnviarInfoUI($"Pólipo {polipoEnMira.tipo} seccionado. Proceda con la succión.", "#00FF00");

        yield return MoverHerramienta(pinzaAsas.transform, posExtendida, Vector3.zero, 0.5f);
        lazoBezier.localScale = escalaOriginalLazo;

        TerminarCorteSeguro();
    }

    public void SumarPolipoEliminado(PolipoInteractuable.TipoPolipo tipo)
    {
        switch (tipo)
        {
            case PolipoInteractuable.TipoPolipo.Yamada1: yamadasEliminados[0]++; break;
            case PolipoInteractuable.TipoPolipo.Yamada2: yamadasEliminados[1]++; break;
            case PolipoInteractuable.TipoPolipo.Yamada3: yamadasEliminados[2]++; break;
            case PolipoInteractuable.TipoPolipo.Yamada4: yamadasEliminados[3]++; break;
        }
    }

    public int ObtenerTotalEliminados()
    {
        return yamadasEliminados[0] + yamadasEliminados[1] + yamadasEliminados[2] + yamadasEliminados[3];
    }

    private IEnumerator MoverHerramienta(Transform obj, Vector3 inicio, Vector3 fin, float duracion)
    {
        float tiempo = 0;
        while (tiempo < 1f && estaCortando)
        {
            tiempo += Time.unscaledDeltaTime / duracion;
            obj.localPosition = Vector3.Lerp(inicio, fin, tiempo);
            yield return null;
        }
    }

    private IEnumerator RotarPinzas(float anguloInicio, float anguloFin, float duracion)
    {
        float tiempo = 0;
        while (tiempo < 1f && estaCortando)
        {
            tiempo += Time.unscaledDeltaTime / duracion;
            float anguloActual = Mathf.Lerp(anguloInicio, anguloFin, tiempo);

            pinzaDerecha.localRotation = Quaternion.Euler(anguloActual, 0, 0);
            pinzaIzquierda.localRotation = Quaternion.Euler(-anguloActual, 0, 0);

            yield return null;
        }
    }

    private IEnumerator EscalarLazo(Vector3 inicio, Vector3 fin, float duracion)
    {
        float tiempo = 0;
        while (tiempo < 1f && estaCortando)
        {
            tiempo += Time.unscaledDeltaTime / duracion;
            lazoBezier.localScale = Vector3.Lerp(inicio, fin, tiempo);
            yield return null;
        }
    }

    private void IniciarCorteSeguro()
    {
        estaCortando = true;
        posInicialPunta = transform.position;
        rotInicialPunta = transform.rotation;
    }

    private void TerminarCorteSeguro()
    {
        estaCortando = false;

        pinzaDientes.transform.localPosition = Vector3.zero;
        pinzaAsas.transform.localPosition = Vector3.zero;

        // FIX: Usamos la memoria en lugar de Vector3.one
        if (lazoBezier != null) lazoBezier.localScale = escalaOriginalLazoBase;

        pinzaDerecha.localRotation = Quaternion.identity;
        pinzaIzquierda.localRotation = Quaternion.identity;

        pinzaDientes.SetActive(false);
        pinzaAsas.SetActive(false);
    }

    private void VerificarMovimientoProhibido()
    {
        // ESCUDO ANTI-LAG (Cuello de botella USB)
        // Si el frame tardó más de 50 milisegundos, significa que Unity se congeló
        // enviando el dato de vibración. Ignoramos este frame para evitar que
        // el salto visual cancele el corte de la cirugía.
        if (Time.deltaTime > 0.05f) return;

        if (Vector3.Distance(transform.position, posInicialPunta) > 0.005f || Quaternion.Angle(transform.rotation, rotInicialPunta) > 3f)
        {
            EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion.Tecnica, 7, "Corte Abortado: Pérdida de estabilidad del endoscopio durante intervención.");
            TerminarCorteSeguro();
        }
    }

    private void EnviarErrorUI(MonitorEndoscopiaUI.CategoriaEvaluacion cat, int index, string mensaje)
    {
        Debug.LogWarning(mensaje);
        if (monitorUI != null) monitorUI.RegistrarErrorEstandarizado(cat, index, mensaje);
    }

    private void EnviarInfoUI(string mensaje, string colorHex)
    {
        Debug.Log($"<color={colorHex}>{mensaje}</color>");
        if (monitorUI != null) monitorUI.RegistrarAccionInfo(mensaje, colorHex);
    }
    public void ActivarModoSalida(bool activar)
    {
        enModoConfirmarSalida = activar;
        RefrescarUIBotones();
        if (activar)
        {
            EnviarInfoUI("¿FINALIZAR PROCEDIMIENTO?\n1 (Freeze): SÍ\n2 (Capture): NO", "#FFD700");
            if (monitorUI != null) monitorUI.MostrarPopupConfirmacion(MonitorEndoscopiaUI.TipoConfirmacion.FinalizarProcedimiento);
        }
        else
        {
            if (monitorUI != null) monitorUI.OcultarPopupConfirmacion();
        }
    }
    public void RefrescarUIBotones()
    {
        if (monitorUI != null)
        {
            // Solo habilitamos el texto rojo si está en la zona, NO está seleccionando herramienta,
            // NO está llevando un pólipo y NO está apuntando a un pólipo vivo.
            bool puedeSalir = enZonaExtraccion && !llevandoPolipo && !enModoSeleccion && polipoEnMira == null;

            monitorUI.ActualizarTextosBotones(enModoSeleccion, puedeSalir);
        }
    }

    
}