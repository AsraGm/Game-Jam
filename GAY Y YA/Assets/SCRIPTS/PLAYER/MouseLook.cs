using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ponle este script a la CÁMARA (normalmente hija del GameObject del jugador,
/// el mismo que tiene MovementController y el Rigidbody).
///
/// Gira el CUERPO del jugador en horizontal (para que el movimiento, que usa
/// transform.rotation en MovementController, siga hacia donde miras) y solo
/// la CÁMARA en vertical (arriba/abajo), para no voltear el cuerpo entero
/// al mirar hacia el techo o el piso.
/// </summary>
public class MouseLook : MonoBehaviour
{
    [Header("Sensibilidad")]
    public float mouseSensitivity = 200f;
    private const float SENSIBILIDAD_MIN = 50f;
    private const float SENSIBILIDAD_MAX = 600f;

    [Header("Cuerpo del jugador (el objeto con Rigidbody / MovementController)")]
    [SerializeField] private Transform playerBody;

    [Header("Límites de inclinación vertical (grados)")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private float _pitch; // rotación vertical acumulada de la cámara

    private void Start()
    {
        if (EsEscenaMenu())
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

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Horizontal: rota el CUERPO, no la cámara.
        playerBody.Rotate(Vector3.up * mouseX);

        // Vertical: solo se inclina la cámara, nunca el cuerpo.
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // Recibe valor del slider (0 a 1) y lo mapea al rango real
    public void SetSensibilidad(float valorSlider)
    {
        mouseSensitivity = Mathf.Lerp(SENSIBILIDAD_MIN, SENSIBILIDAD_MAX, valorSlider);
        PlayerPrefs.SetFloat("Sensibilidad", valorSlider);
    }

    public float GetSensibilidadNormalizada()
    {
        return Mathf.InverseLerp(SENSIBILIDAD_MIN, SENSIBILIDAD_MAX, mouseSensitivity);
    }

    private bool EsEscenaMenu()
    {
        string sceneName = SceneManager.GetActiveScene().name.ToUpperInvariant();
        return sceneName.Contains("MENU") || sceneName.Contains("MAIN");
    }
}
