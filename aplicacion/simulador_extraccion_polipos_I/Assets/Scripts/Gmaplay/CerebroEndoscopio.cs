using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EndoscopioCurvas : MonoBehaviour
{
    [Header("Los Huesos (0=Punta, Último=Base)")]
    public Transform[] huesos;

    [Header("Configuración de Controles")]
    public float velocidadInsercion = 0.5f;   // W / S
    public float velocidadTorque = 100f;      // A / D
    public float velocidadGiroPunta = 80f;    // Flechas
    public float suavidadGiroHuesos = 15f;    // Amortiguador visual

    private Quaternion[] rotacionesGlobalesIniciales;
    private Quaternion[] olaDeCurvas;

    private float longitudHueso;
    private float distanciaAcumulada = 0f;

    private float rotX = 0f;
    private float rotZ = 0f;
    private float torqueGiro = 0f;

    private Rigidbody rb;
    private float empujeFisico = 0f;

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
        {
            longitudHueso = Vector3.Distance(huesos[0].position, huesos[1].position);
        }
    }

    void Update()
    {
        if (huesos.Length == 0) return;

        empujeFisico = 0;
        if (Input.GetKey(KeyCode.W)) empujeFisico = 1;
        if (Input.GetKey(KeyCode.S)) empujeFisico = -1;

        if (Input.GetKey(KeyCode.UpArrow)) rotX -= velocidadGiroPunta * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) rotX += velocidadGiroPunta * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftArrow)) rotZ += velocidadGiroPunta * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow)) rotZ -= velocidadGiroPunta * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -90f, 90f);
        rotZ = Mathf.Clamp(rotZ, -90f, 90f);

        if (Input.GetKey(KeyCode.A)) torqueGiro -= velocidadTorque * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) torqueGiro += velocidadTorque * Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (huesos.Length < 2 || empujeFisico == 0) return;

        Vector3 direccionFinal = huesos[1].up;
        float distanciaAvanzada = empujeFisico * velocidadInsercion * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + (direccionFinal * distanciaAvanzada));

        distanciaAcumulada += distanciaAvanzada;

        if (distanciaAcumulada >= longitudHueso)
        {
            for (int i = huesos.Length - 1; i > 0; i--)
                olaDeCurvas[i] = olaDeCurvas[i - 1];
            distanciaAcumulada -= longitudHueso;
        }
        else if (distanciaAcumulada <= -longitudHueso)
        {
            for (int i = 0; i < huesos.Length - 1; i++)
                olaDeCurvas[i] = olaDeCurvas[i + 1];
            olaDeCurvas[huesos.Length - 1] = Quaternion.identity;
            distanciaAcumulada += longitudHueso;
        }
    }

    void LateUpdate()
    {
        if (huesos.Length < 2) return;

        olaDeCurvas[0] = olaDeCurvas[1] * Quaternion.Euler(rotX, 0, rotZ);
        Quaternion curvaCuello = Quaternion.identity;

        for (int i = 1; i < huesos.Length; i++)
        {
            Quaternion curvaSuave;

            if (empujeFisico >= 0)
            {
                curvaSuave = Quaternion.Slerp(olaDeCurvas[i], olaDeCurvas[i - 1], distanciaAcumulada / longitudHueso);
            }
            else
            {
                int indexAtras = Mathf.Min(i + 1, huesos.Length - 1);
                curvaSuave = Quaternion.Slerp(olaDeCurvas[i], olaDeCurvas[indexAtras], Mathf.Abs(distanciaAcumulada) / longitudHueso);
            }

            if (i == 1) curvaCuello = curvaSuave;

            // --- CORRECCIÓN DEL TORQUE AQUÍ ---
            // Multiplicamos la curva primero, y el Torque al final. Así el giro en Y ocurre sobre el eje ya doblado.
            Quaternion rotacionObjetivoGlobal = rotacionesGlobalesIniciales[i] * curvaSuave * Quaternion.Euler(0, torqueGiro, 0);
            huesos[i].rotation = Quaternion.Slerp(huesos[i].rotation, rotacionObjetivoGlobal, Time.deltaTime * suavidadGiroHuesos);
        }

        // --- CORRECCIÓN DE LA PUNTA AQUÍ ---
        Quaternion curvaPunta = curvaCuello * Quaternion.Euler(rotX, 0, rotZ);
        Quaternion objetivoPuntaGlobal = rotacionesGlobalesIniciales[0] * curvaPunta * Quaternion.Euler(0, torqueGiro, 0);

        huesos[0].rotation = Quaternion.Slerp(huesos[0].rotation, objetivoPuntaGlobal, Time.deltaTime * suavidadGiroHuesos);
    }
}