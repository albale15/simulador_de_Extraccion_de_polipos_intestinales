using UnityEngine;
using System.IO;
using System;
using System.Text;

public class HistoryManager : MonoBehaviour
{
    public static HistoryManager instancia;

    [HideInInspector] public string rutaBase;
    [HideInInspector] public string rutaCursoActual;

    // EL MEGÁFONO: Avisará a toda la UI cuando haya cambios en las carpetas
    public event Action AlActualizarDirectorios;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);

            rutaBase = Path.Combine(Application.persistentDataPath, "Historiales_Endoscopia");
            if (!Directory.Exists(rutaBase)) Directory.CreateDirectory(rutaBase);

            rutaCursoActual = rutaBase;
        }
        else Destroy(gameObject);
    }

    public void CrearCarpetaCurso(string nombreCurso)
    {
        string nuevaRuta = Path.Combine(rutaBase, nombreCurso);
        if (!Directory.Exists(nuevaRuta))
        {
            Directory.CreateDirectory(nuevaRuta);
            Debug.Log("Curso creado en: " + nuevaRuta);

            // GRITAMOS: ¡Nueva carpeta creada!
            AlActualizarDirectorios?.Invoke();
        }
    }

    public void GuardarSesion(SesionPractica sesion)
    {
        string json = JsonUtility.ToJson(sesion, true);
        string nombreArchivo = $"{sesion.nombreEstudiante}_{DateTime.Now:yyyyMMdd_HHmm}.json";
        if (!string.IsNullOrEmpty(ManejadorPartida.rutaGuardado))
        {
            rutaCursoActual = ManejadorPartida.rutaGuardado;
        }
        File.WriteAllText(Path.Combine(rutaCursoActual, nombreArchivo), json);
    }

    public SesionPractica CargarArchivoEspecifico(string rutaCompleta)
    {
        try
        {
            string contenido = File.ReadAllText(rutaCompleta);
            SesionPractica datos = JsonUtility.FromJson<SesionPractica>(contenido);
            if (datos == null || string.IsNullOrEmpty(datos.idSesion)) return null;
            return datos;
        }
        catch { return null; }
    }

    public void ExportarCursoAExcel()
    {
        string[] archivosJson = Directory.GetFiles(rutaCursoActual, "*.json");

        if (archivosJson.Length == 0) return;

        StringBuilder csv = new StringBuilder();

        csv.AppendLine("Nombre,Fecha,Puntaje Total,Traumas,Yamada Correcto,Puntos Perdidos");

        foreach (string ruta in archivosJson)
        {
            SesionPractica sesion = CargarArchivoEspecifico(ruta);
            if (sesion != null)
            {
                string detallesPenalizaciones = string.Join(" | ", sesion.penalizaciones);
                string yamada = sesion.aciertosYamada > 0 ? "Si" : "No";

                csv.AppendLine($"{sesion.nombreEstudiante},{sesion.fecha},{sesion.puntajeTotal},{sesion.indiceTrauma},{yamada},{detallesPenalizaciones}");
            }
        }

        string nombreCurso = new DirectoryInfo(rutaCursoActual).Name;

        string rutaExport = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Reporte_Curso_{nombreCurso}.csv");
        File.WriteAllText(rutaExport, csv.ToString(), Encoding.UTF8);
        Debug.Log("Exportado a Excel en: " + rutaExport);
    }

    public void EliminarElemento(string ruta, bool esCarpeta)
    {
        try
        {
            if (esCarpeta)
            {
                if (Directory.Exists(ruta)) Directory.Delete(ruta, true);
                Debug.Log("Carpeta eliminada: " + ruta);

                // GRITAMOS: ¡Carpeta eliminada!
                AlActualizarDirectorios?.Invoke();
            }
            else
            {
                if (File.Exists(ruta)) File.Delete(ruta);
                Debug.Log("Archivo eliminado: " + ruta);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error al eliminar: " + e.Message);
        }
    }
}