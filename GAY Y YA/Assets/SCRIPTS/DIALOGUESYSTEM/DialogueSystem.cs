using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    // ESTRUCTURA NUEVA: Agrupa los datos de cada línea individual de conversación
    [System.Serializable]
    public struct LineaDeConversacion
    {
        public string nombrePersonaje; // Permite cambiar el nombre por línea (ej: "???", "Player", "Npc")
        public Sprite retratoPersonaje; // El sprite específico para esta línea (expresiones)
        [TextArea(3, 6)] public string texto; // La frase que dirá
    }

    private bool isPaused = false;
    public static bool DialogoActivo = false;

    [Header("Referencias UI")]
    [SerializeField] public GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerName;
    [SerializeField] private Image speakerImageComponent; // El componente de la UI (Image)

    [Header("Configuración de diálogo")]
    // MODIFICADO: Ahora es una lista de nuestra estructura personalizada
    public LineaDeConversacion[] lineasDialogo;

    [Header("Intro al cargar la escena")]
    [SerializeField] private bool iniciarDialogoAlInicio = true;
    [SerializeField] private bool soloUnaVez = true;

    private int indiceDialogo = 0;
    private bool dialogoActivo = false;
    private bool introIniciada = false;
    private float _timeScaleAntes = 1f;

    // Referencias al jugador
    private MovementController playerMovement;
    private MouseLook cameraController;

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

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
        _timeScaleAntes = Time.timeScale;
        Time.timeScale = 0f;

        if (playerMovement != null) playerMovement.enabled = false;
        if (cameraController != null) cameraController.enabled = false;

        MostrarLineaActual();
    }

    // MODIFICADO: Adaptado para recibir el nuevo formato de líneas si se llama externamente
    public void IniciarDialogoConLineas(LineaDeConversacion[] nuevasLineas)
    {
        if (dialogoActivo) return;

        lineasDialogo = nuevasLineas;
        indiceDialogo = 0;
        dialogoActivo = true;
        DialogoActivo = true;

        dialoguePanel.SetActive(true);

        if (playerMovement != null) playerMovement.enabled = false;
        if (cameraController != null) cameraController.enabled = false;

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

    // MODIFICADO: Ahora extrae el texto, el nombre y el sprite de la línea actual
    private void MostrarLineaActual()
    {
        if (indiceDialogo < lineasDialogo.Length)
        {
            LineaDeConversacion lineaActual = lineasDialogo[indiceDialogo];

            // 1. Actualizar Texto de la frase
            if (dialogueText != null)
                dialogueText.text = lineaActual.texto;

            // 2. Actualizar Nombre del personaje de esta línea
            if (speakerName != null)
                speakerName.text = !string.IsNullOrEmpty(lineaActual.nombrePersonaje) ? lineaActual.nombrePersonaje : "ANON";

            // 3. Actualizar Retrato dinámico de esta línea
            if (speakerImageComponent != null)
            {
                if (lineaActual.retratoPersonaje != null)
                {
                    speakerImageComponent.gameObject.SetActive(true);
                    speakerImageComponent.sprite = lineaActual.retratoPersonaje;
                }
                else
                {
                    // Si decides dejar el sprite vacío en alguna línea, el cuadro de la UI se oculta automáticamente
                    speakerImageComponent.gameObject.SetActive(false);
                }
            }
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
        Time.timeScale = _timeScaleAntes;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Diálogo terminado. Controles restaurados.");
    }

    // MODIFICADO: Adaptado para el diálogo temporal utilizando la nueva estructura
    public void ReproducirDialogoTemporal(LineaDeConversacion[] lines)
    {
        StopAllCoroutines();
        dialoguePanel.SetActive(true);
        StartCoroutine(RunTemporaryDialogue(lines));
    }

    private IEnumerator RunTemporaryDialogue(LineaDeConversacion[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            LineaDeConversacion lineaActual = lines[i];

            if (dialogueText != null) dialogueText.text = lineaActual.texto;
            if (speakerName != null) speakerName.text = lineaActual.nombrePersonaje;

            if (speakerImageComponent != null)
            {
                if (lineaActual.retratoPersonaje != null)
                {
                    speakerImageComponent.gameObject.SetActive(true);
                    speakerImageComponent.sprite = lineaActual.retratoPersonaje;
                }
                else
                {
                    speakerImageComponent.gameObject.SetActive(false);
                }
            }

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        }

        dialoguePanel.SetActive(false);
    }
}
