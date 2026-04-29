using UnityEngine;
using System;

[System.Serializable]
public class PasoTutorial
{
    public string nombrePaso;
    [TextArea] public string instruccion;
    public RectTransform uiAResaltar; // Si es null, no resalta nada

    public enum TipoEspera { Tiempo, AccionInput, LlegarAZona, PolipoEliminado, PolipoEnMira }
    public TipoEspera espera;

    public float tiempoSegundos; // Para TipoEspera.Tiempo
    public string accionRequerida; // "W", "S", "B1", "Accion", etc.
    public GameObject colliderABloquear; // El muro que se apaga al terminar este paso
    [Tooltip("Si se marca, el endoscopio y los botones no responderán durante este paso.")]
    public bool bloquearControles;
    [Tooltip("Si se marca, enciende el objeto UI que le indica al usuario que presione un botón.")]
    public bool mostrarTextoPresionar;
    [Tooltip("Si se marca, el objeto en 'Ui A Resaltar' se activará al empezar el paso y se ocultará al terminar.")]
    public bool mostrarTemporalmente;
}