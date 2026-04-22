using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

public class HistoryUI : MonoBehaviour
{
    [Header("Creación de Cursos")]
    public TMP_InputField inputNuevoCurso;
    public GameObject btnVolverRaiz;

    [Header("Buscador")]
    public TMP_InputField inputBusqueda;

    [Header("Lista Dinámica")]
    public Transform contenedorDeLista;
    public GameObject prefabBotonArchivo;

    [Header("Panel de Detalles")]
    public GameObject panelDetalles;
    public TextMeshProUGUI txtDetalles;
    public GameObject btnExportarExcel;

    [Header("Feedback del Sistema")]
    public TextMeshProUGUI txtFeedback;

    [Header("Popup de Eliminación")]
    public GameObject panelConfirmacion;
    public TextMeshProUGUI txtPreguntaConfirmacion;

    private string rutaAEliminarTemporal;
    private bool esCarpetaAEliminarTemporal;

    private List<GameObject> botonesCreados = new List<GameObject>();
    private bool estamosEnRaiz = true;
    private SesionPractica sesionSeleccionada;

    void Start()
    {
        panelDetalles.SetActive(false);
        btnExportarExcel.SetActive(false); // Oculto al iniciar
        panelConfirmacion.SetActive(false);

        if (inputBusqueda != null) inputBusqueda.onValueChanged.AddListener(FiltrarLista);
        IrARaiz();
    }

    public void IrARaiz()
    {
        if (HistoryManager.instancia == null) return;
        estamosEnRaiz = true;
        HistoryManager.instancia.rutaCursoActual = HistoryManager.instancia.rutaBase;

        btnVolverRaiz.SetActive(false);
        panelDetalles.SetActive(false);

        // Apagamos el botón de exportar porque estamos viendo las carpetas
        btnExportarExcel.SetActive(false);

        txtFeedback.text = "Mostrando todos los Cursos.";
        RefrescarLista();
    }

    public void EntrarACurso(string rutaCarpeta)
    {
        estamosEnRaiz = false;
        HistoryManager.instancia.rutaCursoActual = rutaCarpeta;

        btnVolverRaiz.SetActive(true);
        panelDetalles.SetActive(false);

        // Encendemos el botón de exportar porque ya entramos a un curso
        btnExportarExcel.SetActive(true);

        txtFeedback.text = $"Curso: {Path.GetFileName(rutaCarpeta)}";
        RefrescarLista();
    }

    public void BotonCrearNuevoCurso()
    {
        if (!string.IsNullOrEmpty(inputNuevoCurso.text))
        {
            HistoryManager.instancia.CrearCarpetaCurso(inputNuevoCurso.text);
            txtFeedback.text = $"<color=green>Curso '{inputNuevoCurso.text}' creado.</color>";
            inputNuevoCurso.text = "";
            if (estamosEnRaiz) RefrescarLista();
        }
    }

    public void RefrescarLista()
    {
        foreach (GameObject btn in botonesCreados) Destroy(btn);
        botonesCreados.Clear();

        if (estamosEnRaiz)
        {
            string[] carpetas = Directory.GetDirectories(HistoryManager.instancia.rutaBase);
            foreach (string carpeta in carpetas) CrearBotonEnLista(Path.GetFileName(carpeta), carpeta, true);
        }
        else
        {
            string ruta = HistoryManager.instancia.rutaCursoActual;
            string[] archivosJson = Directory.GetFiles(ruta, "*.json");
            List<string> todosLosArchivos = new List<string>(archivosJson);

            foreach (string archivo in todosLosArchivos) CrearBotonEnLista(Path.GetFileName(archivo), archivo, false);
        }

        FiltrarLista(inputBusqueda.text);
    }

    private void CrearBotonEnLista(string textoMostrar, string ruta, bool esCarpeta)
    {
        GameObject nuevoBoton = Instantiate(prefabBotonArchivo, contenedorDeLista);

        string icono = esCarpeta ? "📁 " : "📄 ";
        nuevoBoton.GetComponentInChildren<TextMeshProUGUI>().text = icono + textoMostrar;

        if (esCarpeta)
            nuevoBoton.GetComponent<Button>().onClick.AddListener(() => EntrarACurso(ruta));
        else
            nuevoBoton.GetComponent<Button>().onClick.AddListener(() => ProcesarClicEnArchivo(ruta));

        Transform botonEliminar = nuevoBoton.transform.Find("Btn_Eliminar");
        if (botonEliminar != null)
        {
            botonEliminar.GetComponent<Button>().onClick.AddListener(() => PrepararEliminacion(ruta, esCarpeta));
        }

        botonesCreados.Add(nuevoBoton);
    }

    public void FiltrarLista(string textoEscrito)
    {
        textoEscrito = textoEscrito.ToLower();
        foreach (GameObject boton in botonesCreados)
        {
            string textoBoton = boton.GetComponentInChildren<TextMeshProUGUI>().text.ToLower();
            boton.SetActive(string.IsNullOrEmpty(textoEscrito) || textoBoton.Contains(textoEscrito));
        }
    }

    private void PrepararEliminacion(string ruta, bool esCarpeta)
    {
        rutaAEliminarTemporal = ruta;
        esCarpetaAEliminarTemporal = esCarpeta;

        string nombreElemento = Path.GetFileName(ruta);
        string tipo = esCarpeta ? "el curso completo" : "el registro de";

        txtPreguntaConfirmacion.text = $"¿Estás seguro de que deseas eliminar {tipo} <b>'{nombreElemento}'</b>?\n<size=16><color=red>Esta acción no se puede deshacer.</color></size>";
        panelConfirmacion.SetActive(true);
    }

    public void ConfirmarEliminacion()
    {
        HistoryManager.instancia.EliminarElemento(rutaAEliminarTemporal, esCarpetaAEliminarTemporal);
        panelConfirmacion.SetActive(false);
        txtFeedback.text = "<color=orange>Elemento eliminado del sistema.</color>";

        if (!esCarpetaAEliminarTemporal && sesionSeleccionada != null && rutaAEliminarTemporal.Contains(sesionSeleccionada.nombreEstudiante))
        {
            panelDetalles.SetActive(false);
        }

        RefrescarLista();
    }

    public void CancelarEliminacion()
    {
        rutaAEliminarTemporal = "";
        panelConfirmacion.SetActive(false);
    }

    private void ProcesarClicEnArchivo(string rutaDelArchivo)
    {
        SesionPractica resultado = HistoryManager.instancia.CargarArchivoEspecifico(rutaDelArchivo);

        if (resultado != null)
        {
            sesionSeleccionada = resultado;
            MostrarInformacion(resultado);
            txtFeedback.text = $"<color=green>Cargado: {resultado.nombreEstudiante}</color>";
        }
        else
        {
            panelDetalles.SetActive(false);
            sesionSeleccionada = null;
            txtFeedback.text = "<color=red><b>ARCHIVO NO VÁLIDO</b></color>";
        }
    }

    private void MostrarInformacion(SesionPractica sesion)
    {
        panelDetalles.SetActive(true);
        txtDetalles.text =
            $"<b>ESTUDIANTE: {sesion.nombreEstudiante}</b>\n" +
            $"--------------------------------\n" +
            $"<b>NOTA FINAL: <color=yellow>{sesion.puntajeTotal} / 100</color></b>\n\n" +
            $"<b>Seguridad (30%):</b> {sesion.puntajeSeguridad} pts\n" +
            $"<b>Protocolo (30%):</b> {sesion.puntajeProtocolo} pts\n" +
            $"<b>Técnica Quirúrgica (40%):</b> {sesion.puntajeTecnica} pts\n\n" +
            $"<b>PENALIZACIONES:</b>\n<color=#FF7777>{string.Join("\n", sesion.penalizaciones)}</color>";
    }

    // --- LA CORRECCIÓN: BOTÓN AHORA EXPORTA TODO ---
    public void BotonExportarExcel()
    {
        if (HistoryManager.instancia != null)
        {
            HistoryManager.instancia.ExportarCursoAExcel();
            string nombreCurso = new DirectoryInfo(HistoryManager.instancia.rutaCursoActual).Name;
            txtFeedback.text = $"<color=green>Reporte del curso '{nombreCurso}' guardado en el Escritorio.</color>";
        }
    }
}