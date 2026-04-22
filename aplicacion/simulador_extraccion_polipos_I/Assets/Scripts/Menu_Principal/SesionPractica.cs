using System;
using System.Collections.Generic;

[Serializable]
public class SesionPractica
{
    public string nombreEstudiante;
    public string fecha;
    public string idSesion;

    // 1.1 Seguridad y Navegación (30%)
    public float puntajeSeguridad;
    public int indiceTrauma;
    public float suavidadDesplazamiento;
    public float porcentajeExploracion;
    public bool retiradaSegura;

    // 1.2 Protocolo y Diagnóstico (30%)
    public float puntajeProtocolo;
    public int hallazgosDocumentados;
    public float calidadCapturaPromedio;
    public int aciertosYamada;

    // 1.3 Técnica Quirúrgica (40%)
    public float puntajeTecnica;
    public float estabilidadAbordaje;
    public float tasaExtraccion;
    public bool higieneCampo;

    // Resultados Finales
    public float puntajeTotal;
    public List<string> penalizaciones = new List<string>();
}