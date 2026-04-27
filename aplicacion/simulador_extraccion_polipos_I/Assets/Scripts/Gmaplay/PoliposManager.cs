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


    private List<Transform> puntosDisponibles = new List<Transform>();
    private GameObject[] prefabDiccionario;

    [HideInInspector]
    public List<GameObject> poliposActivos = new List<GameObject>();

    void Start()
    {

        prefabDiccionario = new GameObject[] { prefabYamada1, prefabYamada2, prefabYamada3, prefabYamada4 };

        foreach (Transform punto in contenedorPuntosSpawn)
        {
            puntosDisponibles.Add(punto);
        }

        StartCoroutine(GenerarPoliposAleatorios());
    }

    private IEnumerator GenerarPoliposAleatorios()
    {
        yield return new WaitForSeconds(0.1f);

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

                // Disparamos el rayo láser de hasta 10 metros, PERO SOLO a la capa del Intestino
                if (Physics.Raycast(puntoCentro.position, direccionAleatoria, out RaycastHit hit, 10f, capaIntestino))
                {
                    // 1. Calculamos que la "cabeza" mire al centro del tubo (el spawn flotante)
                    Vector3 direccionHaciaElCentro = (puntoCentro.position - hit.point).normalized;
                    Quaternion rotacionCorregida = Quaternion.FromToRotation(Vector3.up, direccionHaciaElCentro);

                    // 2. Instanciamos el pólipo en el impacto con coordenadas de mundo
                    GameObject nuevoPolipo = Instantiate(prefabDiccionario[tipoPolipo], hit.point, rotacionCorregida);

                    // 3. FORZAMOS la posición de mundo (Mata cualquier bug de desplazamiento del modelo FBX)
                    nuevoPolipo.transform.position = hit.point;

                    // 4. Lo emparentamos para mantener limpia la jerarquía
                    nuevoPolipo.transform.SetParent(contenedorPuntosSpawn, true);

                    poliposActivos.Add(nuevoPolipo);

                    // Dibuja una línea verde en la ventana "Scene" de Unity para que veas el éxito
                    Debug.DrawLine(puntoCentro.position, hit.point, Color.green, 15f);

                    // Quitamos el punto de la lista para que no crezcan dos pólipos desde el mismo origen
                    puntosDisponibles.RemoveAt(indiceAleatorio);
                }
                else
                {
                    // Si el láser falló (apuntó al vacío), dibuja línea roja y vuelve a intentar el mismo pólipo
                    Debug.DrawRay(puntoCentro.position, direccionAleatoria * 3f, Color.red, 5f);
                    i--;
                }

                // Pausamos brevemente para que no se congele el juego
                if (i % 2 == 0) yield return null;
            }
        }

        Debug.Log($"<color=green>Generación Completa: {poliposActivos.Count} pólipos pegados a las paredes.</color>");
    }
}