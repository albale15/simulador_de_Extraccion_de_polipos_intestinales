using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PoliposManager : MonoBehaviour
{
    [Header("Modelos 3D de los Pólipos")]
    public GameObject prefabYamada1;
    public GameObject prefabYamada2;
    public GameObject prefabYamada3;
    public GameObject prefabYamada4;

    [Header("Configuración del Entorno")]
    [Tooltip("El objeto padre que contiene todos los Puntos (Empty GameObjects) flotando en el centro")]
    public Transform contenedorPuntosSpawn;
    [Tooltip("Selecciona aquí la capa 'Intestino' para que el láser solo choque con las paredes")]
    public LayerMask capaIntestino;

    [Header("Configuración del Tutorial")]
    [Tooltip("Arrastra aquí los 5 pólipos que pusiste manualmente en la escena.")]
    public List<PolipoInteractuable> poliposTutorial;


    private List<Transform> puntosDisponibles = new List<Transform>();
    private GameObject[] prefabDiccionario;

    [HideInInspector]
    public List<GameObject> poliposActivos = new List<GameObject>();

    void Start()
    {
        if (ManejadorPartida.dificultad == 0)
        {
            Debug.Log("Dificultad Tutorial: Inicializando pólipos desde el Inspector...");

            // Llamamos a una corrutina para darle a Unity un milisegundo de respiro
            StartCoroutine(InicializarPoliposTutorial());

            return; // Detenemos la ejecución normal
        }

        prefabDiccionario = new GameObject[] { prefabYamada1, prefabYamada2, prefabYamada3, prefabYamada4 };

        foreach (Transform punto in contenedorPuntosSpawn)
        {
            puntosDisponibles.Add(punto);
        }

        StartCoroutine(GenerarPoliposAleatorios());
    }

    private IEnumerator InicializarPoliposTutorial()
    {
        // Esperamos 0.1 segundos para asegurar que los pólipos ya ejecutaron su propio Awake/Start
        yield return new WaitForSecondsRealtime(0.1f);

        if (poliposTutorial != null)
        {
            foreach (PolipoInteractuable polipo in poliposTutorial)
            {
                if (polipo == null) continue; // Por si hay un hueco vacío en la lista

                float tamanoFijo = 4f; // Base pequeña por defecto (<=5mm para Pinza Fría)

                // Si el pólipo que se tiene es Yamada 3 o 4, lo hacemos GRANDE
                if (polipo.tipo == PolipoInteractuable.TipoPolipo.Yamada3 || polipo.tipo == PolipoInteractuable.TipoPolipo.Yamada4)
                {
                    tamanoFijo = 8f; // >5mm para obligar a usar el Asa Diatérmica
                }

                // Inicializamos el pólipo con su tamaño (ahora sí lo aceptará)
                polipo.InicializarTamanoClinico(tamanoFijo);

                // Lo añadimos a la lista de activos
                poliposActivos.Add(polipo.gameObject);
            }

            Debug.Log("<color=cyan>Pólipos del tutorial inicializados con éxito.</color>");
        }
    }

    private IEnumerator GenerarPoliposAleatorios()
    {
        // EL FIX: Usar 'Realtime' ignora si el juego está en pausa (Time.timeScale = 0)
        yield return new WaitForSecondsRealtime(0.1f);

        int totalPedidos = ManejadorPartida.totalPolipos;
        if (puntosDisponibles.Count < totalPedidos)
        {
            Debug.LogWarning($"<color=orange>Hay más pólipos pedidos ({totalPedidos}) que puntos de spawn ({puntosDisponibles.Count}). Limitando...</color>");
            totalPedidos = puntosDisponibles.Count;
        }

        for (int tipoPolipo = 0; tipoPolipo < 4; tipoPolipo++)
        {
            int cantidadPedidaDeEsteTipo = ManejadorPartida.yamada[tipoPolipo];

            for (int i = 0; i < cantidadPedidaDeEsteTipo; i++)
            {
                if (puntosDisponibles.Count == 0) break;

                int indiceAleatorio = Random.Range(0, puntosDisponibles.Count);
                Transform puntoCentro = puntosDisponibles[indiceAleatorio];

                Vector3 direccionAleatoria = Random.onUnitSphere;

                if (Physics.Raycast(puntoCentro.position, direccionAleatoria, out RaycastHit hit, 10f, capaIntestino))
                {
                    Vector3 direccionHaciaElCentro = (puntoCentro.position - hit.point).normalized;
                    Quaternion rotacionCorregida = Quaternion.FromToRotation(Vector3.up, direccionHaciaElCentro);

                    GameObject nuevoPolipo = Instantiate(prefabDiccionario[tipoPolipo], hit.point, rotacionCorregida);
                    nuevoPolipo.transform.position = hit.point;
                    nuevoPolipo.transform.SetParent(contenedorPuntosSpawn, true);


                    // ASIGNACIÓN DE TAMAÑO ALEATORIO COHERENTE
                    PolipoInteractuable componenteInteractuable = nuevoPolipo.GetComponent<PolipoInteractuable>();
                    if (componenteInteractuable != null)
                    {
                        float tamanoAleatorio = 2f; // Base por seguridad

                        // Le damos rangos realistas según la forma que eligieron en el menú
                        switch (tipoPolipo)
                        {
                            case 0: // Yamada 1 (Planos/Diminutos): Mayormente chicos, algunos medianos
                                tamanoAleatorio = Random.Range(2f, 6f);
                                break;
                            case 1: // Yamada 2 (Sésiles): Rango intermedio
                                tamanoAleatorio = Random.Range(2f, 7f);
                                break;
                            case 2: // Yamada 3 (Semi-pediculados): Mayormente grandes
                                tamanoAleatorio = Random.Range(4f, 8f);
                                break;
                            case 3: // Yamada 4 (Pediculados/Grandes): Claramente grandes
                                tamanoAleatorio = Random.Range(4f, 9f);
                                break;
                        }

                        // Enviamos el tamaño al pólipo para que altere su escala visual y su lógica
                        componenteInteractuable.InicializarTamanoClinico(tamanoAleatorio);
                    }


                    poliposActivos.Add(nuevoPolipo);
                    Debug.DrawLine(puntoCentro.position, hit.point, Color.green, 15f);

                    puntosDisponibles.RemoveAt(indiceAleatorio);
                }
                else
                {
                    Debug.DrawRay(puntoCentro.position, direccionAleatoria * 3f, Color.red, 5f);
                    i--;
                }

                // El segundo FIX: No pausar si ya estamos congelados
                if (i % 2 == 0 && Time.timeScale > 0) yield return null;
            }
        }

        Debug.Log($"<color=green>Generación Completa: {poliposActivos.Count} pólipos pegados a las paredes.</color>");
    }
}