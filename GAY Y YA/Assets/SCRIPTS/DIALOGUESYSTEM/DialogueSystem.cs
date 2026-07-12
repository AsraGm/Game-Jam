using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    private bool isPaused = false;
    public static bool DialogoActivo = false;

    [Header("Referencias UI")]
    [SerializeField] public GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerName;
    [SerializeField] private string nombrePersonaje = "ANON";

    [Header("Configuración de diálogo")]
    [TextArea(3, 6)]
    public string[] lineasDialogo;

    [Header("Intro al cargar la escena")]
    [SerializeField] private bool iniciarDialogoAlInicio = true;
    [SerializeField] private bool soloUnaVez = true;

    private int indiceDialogo = 0;
    private bool dialogoActivo = false;
    private bool introIniciada = false;

    // Referencias al jugador
    private MovementController playerMovement;
    private MouseLook cameraController;

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Buscar player y sus componentes
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<MovementController>();
            cameraController = player.GetComponentInChildren<MouseLook>();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto con tag 'Player' en la escena.");
        }

        if (iniciarDialogoAlInicio && (!soloUnaVez || !introIniciada))
        {
            introIniciada = true;
            IniciarDialogo();
        }
    }

    private void Update()
    {
        if (dialogoActivo && Input.GetKeyDown(KeyCode.E))
        {
            if (isPaused) return;
            MostrarSiguienteLinea();
        }
    }

    public void IniciarDialogo()
    {
        if (dialogoActivo) return;

        if (lineasDialogo == null || lineasDialogo.Length == 0)
        {
            Debug.LogWarning("El NPC no tiene líneas de diálogo asignadas.");
            return;
        }

        indiceDialogo = 0;
        dialogoActivo = true;
        DialogoActivo = true;
        dialoguePanel.SetActive(true);

        // Bloquear controles
        if (playerMovement != null) playerMovement.enabled = false;
        if (cameraController != null) cameraController.enabled = false;

        if (speakerName != null)
            speakerName.text = nombrePersonaje;

        MostrarLineaActual();
    }

    public void IniciarDialogoConLineas(string[] nuevasLineas)
    {
        if (dialogoActivo) return;

        lineasDialogo = nuevasLineas;
        indiceDialogo = 0;
        dialogoActivo = true;
        DialogoActivo = true;

        dialoguePanel.SetActive(true);

        if (playerMovement != null) playerMovement.enabled = false;
        if (cameraController != null) cameraController.enabled = false;

        if (speakerName != null)
            speakerName.text = nombrePersonaje;

        MostrarLineaActual();
    }

    public void PausarDialogo()
    {
        isPaused = true;
    }

    public void ReanudarDialogo()
    {
        isPaused = false;
    }

    private void MostrarLineaActual()
    {
        if (indiceDialogo < lineasDialogo.Length)
        {
            if (dialogueText != null)
                dialogueText.text = lineasDialogo[indiceDialogo];
        }
    }

    void MostrarSiguienteLinea()
    {
        indiceDialogo++;

        if (indiceDialogo < lineasDialogo.Length)
        {
            MostrarLineaActual();
        }
        else
        {
            TerminarDialogo();
        }
    }

    void TerminarDialogo()
    {
        dialogoActivo = false;
        DialogoActivo = false;

        dialoguePanel.SetActive(false);

        //  CRÍTICO: Verificar que los scripts existen antes de reactivar
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            Debug.Log("[Dialogue] PlayerMovement reactivado");
        }

        if (cameraController != null)
        {
            cameraController.enabled = true;
            Debug.Log("[Dialogue] CameraController reactivado");
        }

        //  SIEMPRE desbloquear cursor al terminar diálogo
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Diálogo terminado. Controles restaurados.");
    }

    public void ReproducirDialogoTemporal(string[] lines)
    {
        StopAllCoroutines();
        dialoguePanel.SetActive(true);
        StartCoroutine(RunTemporaryDialogue(lines));
    }

    private IEnumerator RunTemporaryDialogue(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            dialogueText.text = lines[i];
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        }

        dialoguePanel.SetActive(false);
    }
}
