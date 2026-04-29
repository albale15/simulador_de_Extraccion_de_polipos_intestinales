using UnityEngine;

// Al ser "static", estos datos flotan en la memoria. 
// La escena de Gameplay solo tendrá que preguntar: ManejadorPartida.dificultad
public static class ManejadorPartida
{
    public static string nombreEstudiante = "Estudiante";
    public static bool guardarHistorial = false;
    public static string rutaGuardado = ""; // La que el profesor elija

    public static int dificultad = 1; // 0=Tutorial, 1=Fácil, 2=Normal, 3=Realista
    public static int totalPolipos = 5;
    public static int[] yamada = new int[4]; // Índice 0=Y1, 1=Y2, 2=Y3, 3=Y4

    public static float pesoSeguridad = 30f;
    public static float pesoProtocolo = 30f;
    public static float pesoTecnica = 40f;

    // Array con los 10 parámetros (cuántos puntos quita cada error)
    public static float[] penalizaciones = new float[10] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
}