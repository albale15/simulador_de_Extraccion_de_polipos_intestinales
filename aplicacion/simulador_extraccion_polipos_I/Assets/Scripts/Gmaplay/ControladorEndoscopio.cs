using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ControladorEndoscopio : MonoBehaviour
{
    [Header("Modo de Control")]
    [Tooltip("Activa esto para manejar con el teclado. Desactiva para usar hardware STM32.")]
    public bool controlPorPC = true;

    [Header("Velocidades")]
    public float velocidadInsercion = 3f;   // Metros por segundo
    public float velocidadFlexion = 60f;    // Grados por segundo (Volantes)
    public float velocidadTorque = 90f;     // Grados por segundo (Giro del tubo)

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Si el control por PC está apagado, este script no hace nada (espera al hardware)
        if (!controlPorPC) return;

        ManejarMovimiento();
    }

    void Update()
    {
        if (!controlPorPC) return;

        ManejarBotones();
    }

    private void ManejarMovimiento()
    {
        // 1. LECTURA DE INPUTS
        float inputInsercion = 0f;
        float inputTorque = 0f;
        float inputFlexionX = 0f; // Arriba/Abajo
        float inputFlexionY = 0f; // Izquierda/Derecha

        // --- ENCODER INSERCIÓN (Flechas Arriba/Abajo) ---
        if (Input.GetKey(KeyCode.UpArrow)) inputInsercion = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) inputInsercion = -1f;

        // --- ENCODER TORQUE (Flechas Izquierda/Derecha) ---
        if (Input.GetKey(KeyCode.LeftArrow)) inputTorque = 1f;
        if (Input.GetKey(KeyCode.RightArrow)) inputTorque = -1f;

        // --- VOLANTE VERTICAL (W / S) ---
        if (Input.GetKey(KeyCode.W)) inputFlexionX = -1f; // Apuntar hacia arriba
        if (Input.GetKey(KeyCode.S)) inputFlexionX = 1f;  // Apuntar hacia abajo

        // --- VOLANTE HORIZONTAL (A / D) ---
        if (Input.GetKey(KeyCode.A)) inputFlexionY = -1f; // Apuntar izquierda
        if (Input.GetKey(KeyCode.D)) inputFlexionY = 1f;  // Apuntar derecha


        // 2. APLICAR FÍSICAS (Inserción)
        // Calculamos hacia dónde es "adelante" en este momento y empujamos.
        Vector3 movimiento = transform.forward * inputInsercion * velocidadInsercion * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movimiento);


        // 3. APLICAR ROTACIONES (Flexión y Torque)
        // Juntamos todos los giros en un solo cálculo matemático
        Vector3 rotacionEuler = new Vector3(
            inputFlexionX * velocidadFlexion,
            inputFlexionY * velocidadFlexion,
            inputTorque * velocidadTorque
        ) * Time.fixedDeltaTime;

        // Giramos el endoscopio usando su propia rotación local
        Quaternion giroAdicional = Quaternion.Euler(rotacionEuler);
        rb.MoveRotation(rb.rotation * giroAdicional);
    }

    private void ManejarBotones()
    {
        // Aquí conectaremos las funciones de cortar, agua, foto, etc. más adelante.
        if (Input.GetKeyDown(KeyCode.Alpha1)) Debug.Log("Botón 1 pulsado: Freeze / Foto");
        if (Input.GetKeyDown(KeyCode.Alpha2)) Debug.Log("Botón 2 pulsado: Herramienta Acción");
        if (Input.GetKeyDown(KeyCode.Alpha3)) Debug.Log("Botón 3 pulsado: Zoom");
        if (Input.GetKeyDown(KeyCode.Alpha4)) Debug.Log("Botón 4 pulsado: Extra");
        if (Input.GetKeyDown(KeyCode.Alpha5)) Debug.Log("Botón 5 pulsado: Succión");
    }
}