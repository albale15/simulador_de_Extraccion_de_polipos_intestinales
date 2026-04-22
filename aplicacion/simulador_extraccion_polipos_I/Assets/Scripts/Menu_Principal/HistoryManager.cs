using UnityEngine;
using System.IO;
using System;
using System.Text;

public class HistoryManager : MonoBehaviour
{
    public static HistoryManager instancia;

    [HideInInspector] public string rutaBase;
    [HideInInspector] public string rutaCursoActual;

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
        }
    }

    public void GuardarSesion(SesionPractica sesion)
    {
        string json = JsonUtility.ToJson(sesion, true);
        string nombreArchivo = $"{sesion.nombreEstudiante}_{DateTime.Now:yyyyMMdd_HHmm}.json";
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

    // --- LA CORRECCIÓN: EXPORTAR CARPETA COMPLETA ---
    public void ExportarCursoAExcel()
    {
        // 1. Buscamos TODOS los archivos json en la carpeta del curso actual
        string[] archivosJson = Directory.GetFiles(rutaCursoActual, "*.json");

        if (archivosJson.Length == 0) return; // Si la carpeta está vacía, no hace nada

        StringBuilder csv = new StringBuilder();

        // Fila 1: Títulos de las columnas
        csv.AppendLine("Nombre,Fecha,Puntaje Total,Traumas,Yamada Correcto,Puntos Perdidos");

        // 2. Bucle: Por cada archivo encontrado, extraemos los datos y los sumamos a la lista
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

        // 3. Obtenemos el nombre de la carpeta actual para nombrar el Excel
        string nombreCurso = new DirectoryInfo(rutaCursoActual).Name;

        // 4. Lo guardamos en el Escritorio
        string rutaExport = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Reporte_Curso_{nombreCurso}.csv");
        File.WriteAllText(rutaExport, csv.ToString(), Encoding.UTF8);
        Debug.Log("Exportado a Excel en: " + rutaExport);
    }

    // SISTEMA DE ELIMINACIÓN FÍSICA 
    public void EliminarElemento(string ruta, bool esCarpeta)
    {
        try
        {
            if (esCarpeta)
            {
                if (Directory.Exists(ruta)) Directory.Delete(ruta, true);
                Debug.Log("Carpeta eliminada: " + ruta);
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