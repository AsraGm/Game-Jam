using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PausaManager : MonoBehaviour
{
    // Agregar al inicio de PauseManager
    public static int PrioridadActual = 0;
    // 0 = nada, 1 = grimorio, 2 = pausa, 3 = panel final

    public static bool JuegoPausado = false;

    [Header("Referencias")]
    [SerializeField] private GameObject menuPausa;
    [SerializeField] private MovementController playerMovement;
    [SerializeField] private MouseLook playerCameraController;

    [SerializeField] private DialogueSystem dialogueSystem;

    [Header("Submen�s del men� de pausa")]
    [SerializeField] private GameObject botonesPrincipales;  // ? grupo con Reanudar, Config, Salir
    [SerializeField] private GameObject panelConfiguracion;  // ? slider + regresar

    [Header("Sensibilidad")]
    [SerializeField] private Slider sliderSensibilidad;
    [SerializeField] private TextMeshProUGUI textoSensibilidad;

    private bool dialogoActivoAntesDePausa = false;

    private void Awake()
    {
        // Reset al cargar la escena
        PrioridadActual = 0;
        JuegoPausado = false;
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;
        int buildIndex = currentScene.buildIndex;

        Debug.Log("Active Scene Name: " + sceneName);
        Debug.Log("Active Scene Index: " + buildIndex);
    }

    private void Start()
    {
        if (menuPausa != null) menuPausa.SetActive(false);

        ActualizarCursorSegunEstado();

        // Inicializar slider
        if (sliderSensibilidad != null && playerCameraController != null)
        {
            float valorGuardado = PlayerPrefs.GetFloat("Sensibilidad", 0.5f);
            sliderSensibilidad.value = valorGuardado;
            playerCameraController.SetSensibilidad(valorGuardado);
            ActualizarTextoSensibilidad(valorGuardado);

            sliderSensibilidad.onValueChanged.AddListener((val) =>
            {
                playerCameraController.SetSensibilidad(val);
                ActualizarTextoSensibilidad(val);
            });
        }
    }

    void Update()
    {
        if (EsEscenaMenu())
            return;

        if (DialogueSystem.DialogoActivo && !JuegoPausado)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (JuegoPausado)
                ReanudarJuego();
            else
                PausarJuego();
        }
    }

    public void PausarJuego()
    {
        if (PrioridadActual > 2) return; // panel final tiene prioridad

        PrioridadActual = 2;

        JuegoPausado = true;
        Time.timeScale = 0f;

        if (menuPausa != null) menuPausa.SetActive(true);

        // Siempre mostrar botones principales al pausar
        if (botonesPrincipales != null) botonesPrincipales.SetActive(true);
        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);

        if (dialogueSystem != null)
        {
            dialogoActivoAntesDePausa = dialogueSystem.dialoguePanel.activeSelf;
            if (dialogoActivoAntesDePausa)
                dialogueSystem.dialoguePanel.SetActive(false);
        }

        if (playerMovement != null) playerMovement.enabled = false;
        if (playerCameraController != null) playerCameraController.enabled = false;

        ActualizarCursorSegunEstado();
    }

    public void ReanudarJuego()
    {
        PrioridadActual = 0;
        JuegoPausado = false;
        Time.timeScale = 1f;

        if (menuPausa != null) menuPausa.SetActive(false);

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerCameraController != null) playerCameraController.enabled = true;

        if (dialogueSystem != null && dialogoActivoAntesDePausa)
        {
            dialogueSystem.dialoguePanel.SetActive(true);
            dialogoActivoAntesDePausa = false;
        }

        ActualizarCursorSegunEstado();
    }

    private bool EsEscenaMenu()
    {
        string sceneName = SceneManager.GetActiveScene().name.ToUpperInvariant();
        return sceneName.Contains("MENU") || sceneName.Contains("MAIN");
    }

    private void ActualizarCursorSegunEstado()
    {
        if (EsEscenaMenu() || JuegoPausado)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ?? CONFIGURACI�N ?????????????????????????????????

    public void AbrirConfiguracion()
    {
        if (botonesPrincipales != null) botonesPrincipales.SetActive(false);
        if (panelConfiguracion != null) panelConfiguracion.SetActive(true);
    }

    public void RegresarDesdeConfiguracion()
    {
        if (botonesPrincipales != null) botonesPrincipales.SetActive(true);
        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);
    }

    private void ActualizarTextoSensibilidad(float valorSlider)
    {
        if (textoSensibilidad != null)
        {
            int porcentaje = Mathf.RoundToInt(valorSlider * 100f);
            textoSensibilidad.text = $"Sensibilidad: {porcentaje}%";
        }
    }

    public void SalirAlMenuPrincipal()
    {
        // Reset de variables est�ticas ANTES de cambiar escena
        PrioridadActual = 0;
        JuegoPausado = false;
        Time.timeScale = 1f;
        ActualizarCursorSegunEstado();
        SceneManager.LoadScene("MAIN MENU");
    }
}
