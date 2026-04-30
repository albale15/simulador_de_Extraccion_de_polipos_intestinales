using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

public class SetupUI : MonoBehaviour
{
    [Header("Navegación de Pestañas")]
    public GameObject panelInicioPartida;
    public GameObject panelAvanzado;

    [Header("Inicio - Datos del Alumno y Guardado")]
    public TMP_InputField inputNombre;
    public Toggle toggleGuardar;

    [Tooltip("Agrupa el Dropdown y el Creador de Cursos para ocultarlos fácilmente")]
    public GameObject panelSeleccionRuta;
    public TMP_Dropdown dropCursos;
    public TMP_InputField inputNuevoCurso;

    [Header("Inicio - Dificultad y Pólipos")]
    public TMP_Dropdown dropDificultad;
    public TMP_InputField inputTotalPolipos;
    public Toggle togglePersonalizar;
    public GameObject panelYamadasInputs;
    public TMP_InputField inputY1, inputY2, inputY3, inputY4;

    [Header("Inicio - Ponderación (Sliders)")]
    public Slider sliderSeguridad;
    public Slider sliderProtocolo;
    public Slider sliderTecnica;
    public TextMeshProUGUI txtSeg, txtProt, txtTec;
    public TMP_InputField inputSeg, inputProt, inputTec;
    private bool ajustandoSliders = false;

    [Header("Avanzado - Penalizaciones")]
    public TMP_InputField[] inputsPenalizaciones = new TMP_InputField[10];
    private bool configAvanzadaModificada = false;

    [Header("Feedback de Errores")]
    public TextMeshProUGUI txtFeedbackError;

    [Header("Popups")]
    public GameObject popupAvisoNoGuardado;

    [Header("configurables")]
    public int limitepolipos=10;

    void Start()
    {
        CargarConfiguracionAvanzada();
        RestablecerPesos();

        toggleGuardar.onValueChanged.AddListener(AlCambiarGuardado);
        togglePersonalizar.onValueChanged.AddListener(AlCambiarPersonalizar);

        dropDificultad.onValueChanged.AddListener((int valor) => AlCambiarDificultadODatos());
        inputTotalPolipos.onValueChanged.AddListener((string texto) => AlCambiarDificultadODatos());

        sliderSeguridad.onValueChanged.AddListener((v) => AutoBalancearPesos(0, v));
        sliderProtocolo.onValueChanged.AddListener((v) => AutoBalancearPesos(1, v));
        sliderTecnica.onValueChanged.AddListener((v) => AutoBalancearPesos(2, v));

        inputSeg.onEndEdit.AddListener((val) => AlCambiarInputPeso(0, val));
        inputProt.onEndEdit.AddListener((val) => AlCambiarInputPeso(1, val));
        inputTec.onEndEdit.AddListener((val) => AlCambiarInputPeso(2, val));
        foreach (var input in inputsPenalizaciones)
            input.onValueChanged.AddListener((v) => configAvanzadaModificada = true);

        MostrarPestaña(true);
        AlCambiarGuardado(toggleGuardar.isOn);
        AlCambiarDificultadODatos();
        popupAvisoNoGuardado.SetActive(false);
    }

    // --- NUEVO: SISTEMA DE ESCUCHA ---
    void OnEnable()
    {
        if (HistoryManager.instancia != null)
        {
            // Nos suscribimos al megáfono
            HistoryManager.instancia.AlActualizarDirectorios += ReaccionarACambioDeCarpetas;
            ReaccionarACambioDeCarpetas();
        }
    }

    void OnDisable()
    {
        if (HistoryManager.instancia != null)
        {
            // Dejamos de escuchar si se apaga el panel para no causar errores
            HistoryManager.instancia.AlActualizarDirectorios -= ReaccionarACambioDeCarpetas;
        }
    }

    private void ReaccionarACambioDeCarpetas()
    {
        if (toggleGuardar != null && toggleGuardar.isOn)
        {
            ActualizarListaDeCursos();
        }
    }
    // ---------------------------------

    public void MostrarPestaña(bool esInicio)
    {
        panelInicioPartida.SetActive(esInicio);
        panelAvanzado.SetActive(!esInicio);
    }

    private void AlCambiarGuardado(bool guardar)
    {
        if (panelSeleccionRuta != null) panelSeleccionRuta.SetActive(guardar);

        if (guardar)
        {
            ActualizarListaDeCursos();
        }
    }

    public void ActualizarListaDeCursos()
    {
        if (HistoryManager.instancia == null || dropCursos == null) return;

        dropCursos.ClearOptions();
        List<string> nombresCursos = new List<string>();

        if (Directory.Exists(HistoryManager.instancia.rutaBase))
        {
            string[] carpetas = Directory.GetDirectories(HistoryManager.instancia.rutaBase);
            foreach (string carpeta in carpetas)
            {
                nombresCursos.Add(Path.GetFileName(carpeta));
            }
        }

        if (nombresCursos.Count == 0) nombresCursos.Add("Sin cursos disponibles");
        dropCursos.AddOptions(nombresCursos);
    }

    public void BotonCrearCursoDesdePreparacion()
    {
        if (HistoryManager.instancia != null && !string.IsNullOrEmpty(inputNuevoCurso.text))
        {
            HistoryManager.instancia.CrearCarpetaCurso(inputNuevoCurso.text);
            inputNuevoCurso.text = "";
            // Ya no llamamos a actualizar la lista a mano, el evento del Manager lo hace solo.
            dropCursos.value = dropCursos.options.Count - 1;
        }
    }

    private void AlCambiarPersonalizar(bool personalizado)
    {
        foreach (var input in new[] { inputY1, inputY2, inputY3, inputY4 }) input.interactable = personalizado;
        if (!personalizado) AlCambiarDificultadODatos();
    }

    private void AlCambiarDificultadODatos()
    {
        int dif = dropDificultad.value;
        int total;
        int.TryParse(inputTotalPolipos.text, out total);
        if (total < 1)
        {
            total = 1;
        }
        else if (total > limitepolipos)
        {
            total = limitepolipos;
            // Al cambiar el texto aquí, la UI se actualiza instantáneamente
            inputTotalPolipos.text = "10";
        }
        if (dif == 0) // Si es Tutorial
        {
            if (toggleGuardar != null)
            {
                toggleGuardar.isOn = false;
                toggleGuardar.interactable = false;
            }
        }
        else // Si es cualquier otra dificultad
        {
            if (toggleGuardar != null)
            {
                toggleGuardar.interactable = true;
            }
        }

        if (dif == 0)
        {
            inputTotalPolipos.text = "5";
            inputTotalPolipos.interactable = false;
            togglePersonalizar.isOn = false;
            togglePersonalizar.interactable = false;
            AsignarYamadas(2, 1, 1, 1);
            return;
        }

        inputTotalPolipos.interactable = true;
        togglePersonalizar.interactable = true;

        if (!togglePersonalizar.isOn)
        {
            int y1 = 0, y2 = 0, y3 = 0, y4 = 0;
            switch (dif)
            {
                case 1:
                    y1 = Mathf.RoundToInt(total * 0.80f); y2 = Mathf.RoundToInt(total * 0.10f);
                    y3 = Mathf.RoundToInt(total * 0.05f); y4 = Mathf.Max(0, total - (y1 + y2 + y3));
                    break;
                case 2:
                    y1 = Mathf.RoundToInt(total * 0.50f); y2 = Mathf.RoundToInt(total * 0.30f);
                    y3 = Mathf.RoundToInt(total * 0.15f); y4 = Mathf.Max(0, total - (y1 + y2 + y3));
                    break;
                case 3:
                    y1 = Mathf.RoundToInt(total * 0.40f); y2 = Mathf.RoundToInt(total * 0.40f);
                    y3 = Mathf.RoundToInt(total * 0.10f); y4 = Mathf.Max(0, total - (y1 + y2 + y3));
                    break;
            }
            AsignarYamadas(y1, y2, y3, y4);
        }
    }

    private void AsignarYamadas(int y1, int y2, int y3, int y4)
    {
        inputY1.text = y1.ToString(); inputY2.text = y2.ToString();
        inputY3.text = y3.ToString(); inputY4.text = y4.ToString();
    }

    private void AutoBalancearPesos(int indiceCambiado, float nuevoValor)
    {
        if (ajustandoSliders) return;
        ajustandoSliders = true;

        Slider[] sliders = { sliderSeguridad, sliderProtocolo, sliderTecnica };
        TMP_InputField[] inputs = { inputSeg, inputProt, inputTec };

        float restante = 100f - nuevoValor;
        int idA = (indiceCambiado + 1) % 3;
        int idB = (indiceCambiado + 2) % 3;

        float sumaActualOtros = sliders[idA].value + sliders[idB].value;

        if (sumaActualOtros > 0.01f)
        {
            sliders[idA].value = Mathf.Round((sliders[idA].value / sumaActualOtros) * restante);
            sliders[idB].value = 100f - nuevoValor - sliders[idA].value;
        }
        else
        {
            sliders[idA].value = Mathf.Round(restante / 2f);
            sliders[idB].value = restante - sliders[idA].value;
        }

        // --- CAMBIO: ESCRIBIMOS EN LOS INPUTS EN VEZ DE LOS TXT ---
        for (int i = 0; i < 3; i++)
        {
            if (inputs[i] != null) inputs[i].text = sliders[i].value.ToString("0");
        }

        ajustandoSliders = false;
    }
    // --- NUEVA FUNCIÓN: DEL INPUT AL SLIDER ---
    private void AlCambiarInputPeso(int indice, string valorTxt)
    {
        if (ajustandoSliders) return;

        float nuevoValor;
        // Intentamos convertir lo que escribió a un número real
        if (float.TryParse(valorTxt, out nuevoValor))
        {
            // Limitamos que no escriban números negativos o mayores a 100
            nuevoValor = Mathf.Clamp(nuevoValor, 0f, 100f);

            // Movemos el slider correspondiente. 
            // ¡La magia es que esto disparará automáticamente la función AutoBalancearPesos!
            if (indice == 0) sliderSeguridad.value = nuevoValor;
            else if (indice == 1) sliderProtocolo.value = nuevoValor;
            else if (indice == 2) sliderTecnica.value = nuevoValor;
        }
        else
        {
            // Si el profesor borró todo o escribió letras por accidente, restauramos los textos
            TMP_InputField[] inputs = { inputSeg, inputProt, inputTec };
            Slider[] sliders = { sliderSeguridad, sliderProtocolo, sliderTecnica };
            if (inputs[indice] != null) inputs[indice].text = sliders[indice].value.ToString("0");
        }
    }
    public void RestablecerPesos()
    {
        ajustandoSliders = true;
        sliderSeguridad.value = 30f; sliderProtocolo.value = 30f; sliderTecnica.value = 40f;

        // --- CAMBIO: ESCRIBIMOS EN LOS INPUTS ---
        if (inputSeg != null) inputSeg.text = "30";
        if (inputProt != null) inputProt.text = "30";
        if (inputTec != null) inputTec.text = "40";

        ajustandoSliders = false;
    }

    public void GuardarConfiguracionAvanzada()
    {
        for (int i = 0; i < 10; i++)
        {
            float valor;
            if (inputsPenalizaciones[i] != null && float.TryParse(inputsPenalizaciones[i].text, out valor))
                PlayerPrefs.SetFloat("Penalizacion_" + i, valor);
        }
        PlayerPrefs.Save();
        configAvanzadaModificada = false;
        Debug.Log("Configuración Avanzada Guardada en Disco");
    }

    private void CargarConfiguracionAvanzada()
    {
        for (int i = 0; i < 10; i++)
        {
            float valorGuardado = PlayerPrefs.GetFloat("Penalizacion_" + i, 1.0f);
            if (inputsPenalizaciones.Length > i && inputsPenalizaciones[i] != null)
                inputsPenalizaciones[i].text = valorGuardado.ToString();
        }
        configAvanzadaModificada = false;
    }

    public void RestablecerAvanzados()
    {
        for (int i = 0; i < 10; i++)
            if (inputsPenalizaciones.Length > i && inputsPenalizaciones[i] != null)
                inputsPenalizaciones[i].text = "1";

        GuardarConfiguracionAvanzada();
    }

    public void BotonEmpezar()
    {
        // 1. Limpiamos cualquier error previo
        if (txtFeedbackError != null) txtFeedbackError.text = "";

        // 2. VALIDACIÓN: Nombre vacío
        if (string.IsNullOrWhiteSpace(inputNombre.text))
        {
            if (txtFeedbackError != null) txtFeedbackError.text = "<color=red>Error: Ingrese el nombre del estudiante para poder iniciar.</color>";
            return; // Detiene el código, no inicia la partida
        }

        // 3. VALIDACIÓN: Suma exacta de pólipos
        int total, y1, y2, y3, y4;
        int.TryParse(inputTotalPolipos.text, out total);
        int.TryParse(inputY1.text, out y1);
        int.TryParse(inputY2.text, out y2);
        int.TryParse(inputY3.text, out y3);
        int.TryParse(inputY4.text, out y4);

        int sumaYamadas = y1 + y2 + y3 + y4;

        if (sumaYamadas != total)
        {
            if (txtFeedbackError != null)
            {
                txtFeedbackError.text = $"<color=red>Error: La suma de los Yamada ({sumaYamadas}) debe coincidir exactamente con el Total ({total}).</color>";
            }
            return; // Detiene el código, no inicia la partida
        }

        // 4. Si todo es correcto, permitimos el inicio
        if (configAvanzadaModificada)
        {
            popupAvisoNoGuardado.SetActive(true);
        }
        else
        {
            EjecutarInicio();
        }
    }

    public void ConfirmarInicioConCambiosTemporales()
    {
        popupAvisoNoGuardado.SetActive(false);
        EjecutarInicio();
    }

    public void CancelarInicio()
    {
        popupAvisoNoGuardado.SetActive(false);
    }

    private void EjecutarInicio()
    {
        ManejadorPartida.nombreEstudiante = string.IsNullOrEmpty(inputNombre.text) ? "Estudiante" : inputNombre.text;
        ManejadorPartida.guardarHistorial = toggleGuardar.isOn;

        if (toggleGuardar.isOn && dropCursos.options.Count > 0 && dropCursos.options[0].text != "Sin cursos disponibles")
        {
            string nombreCursoSeleccionado = dropCursos.options[dropCursos.value].text;
            ManejadorPartida.rutaGuardado = Path.Combine(HistoryManager.instancia.rutaBase, nombreCursoSeleccionado);
        }
        else
        {
            ManejadorPartida.rutaGuardado = "";
        }

        ManejadorPartida.dificultad = dropDificultad.value;
        int.TryParse(inputTotalPolipos.text, out ManejadorPartida.totalPolipos);

        int.TryParse(inputY1.text, out ManejadorPartida.yamada[0]);
        int.TryParse(inputY2.text, out ManejadorPartida.yamada[1]);
        int.TryParse(inputY3.text, out ManejadorPartida.yamada[2]);
        int.TryParse(inputY4.text, out ManejadorPartida.yamada[3]);

        ManejadorPartida.pesoSeguridad = sliderSeguridad.value;
        ManejadorPartida.pesoProtocolo = sliderProtocolo.value;
        ManejadorPartida.pesoTecnica = sliderTecnica.value;

        for (int i = 0; i < 10; i++)
        {
            if (inputsPenalizaciones[i] != null)
                float.TryParse(inputsPenalizaciones[i].text, out ManejadorPartida.penalizaciones[i]);
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("EscenaGameplay");
    }
}