using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EndoscopioCurvas : MonoBehaviour
{
    [Header("Los Huesos (0=Punta, Último=Base)")]
    public Transform[] huesos;

    [Header("Configuración de Movimiento")]
    public float velocidadInsercion = 0.5f;
    public float velocidadGiro = 80f;

    // --- CAMBIO CLAVE: Memoria Global ---
    private Quaternion[] rotacionesGlobalesIniciales;
    private Quaternion[] olaDeCurvas;

    private float longitudHueso;
    private float distanciaAcumulada = 0f;
    private float rotX, rotY;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rotacionesGlobalesIniciales = new Quaternion[huesos.Length];
        olaDeCurvas = new Quaternion[huesos.Length];

        for (int i = 0; i < huesos.Length; i++)
        {
            // Guardamos la orientación exacta que tienen en el mundo al iniciar, no la local
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

        // --- 1. CONTROL DE LA PUNTA ---
        if (Input.GetKey(KeyCode.UpArrow)) rotX -= velocidadGiro * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow)) rotX += velocidadGiro * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) rotY -= velocidadGiro * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) rotY += velocidadGiro * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -90f, 90f);
        rotY = Mathf.Clamp(rotY, -90f, 90f);

        olaDeCurvas[0] = Quaternion.Euler(rotX, 0, rotY);

        // APLICAMOS LA ROTACIÓN DE FORMA GLOBAL
        huesos[0].rotation = rotacionesGlobalesIniciales[0] * olaDeCurvas[0];
    }

    void FixedUpdate()
    {
        if (huesos.Length < 2) return; // Seguridad extra

        // --- 2. DIRECCIÓN DE EMPUJE FÍSICO (Lógica Pura del Cuello) ---
        float empuje = 0;
        if (Input.GetKey(KeyCode.W)) empuje = 1;
        if (Input.GetKey(KeyCode.S)) empuje = -1;

        if (empuje != 0)
        {
            // DIRECCIÓN ÚNICA: Siempre empujamos en la dirección del Hueso 1 (el cuello)
            Vector3 direccionFinal = huesos[1].up;
            Debug.Log("<color=cyan>Empujando en dirección del Cuello (" + huesos[1].name + ")</color>");

            // EL MURO: Unity se encarga de frenarnos si los Colliders chocan
            float distanciaAvanzada = empuje * velocidadInsercion * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + (direccionFinal * distanciaAvanzada));

            // --- 3. LA OLA DE DEFORMACIÓN ---
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

        // --- 4. APLICAR LA OLA (AHORA ES GLOBAL) ---
        for (int i = 1; i < huesos.Length; i++)
        {
            Quaternion objetivoSuave;
            if (empuje >= 0)
            {
                objetivoSuave = Quaternion.Slerp(olaDeCurvas[i], olaDeCurvas[i - 1], distanciaAcumulada / longitudHueso);
            }
            else
            {
                int indexAtras = Mathf.Min(i + 1, huesos.Length - 1);
                objetivoSuave = Quaternion.Slerp(olaDeCurvas[i], olaDeCurvas[indexAtras], Mathf.Abs(distanciaAcumulada) / longitudHueso);
            }

            // MAGIA AQUÍ: Usamos .rotation en lugar de .localRotation
            // Esto anula el giro en U porque los ángulos ya no se suman entre padres e hijos
            huesos[i].rotation = rotacionesGlobalesIniciales[i] * objetivoSuave;
        }
    }
}