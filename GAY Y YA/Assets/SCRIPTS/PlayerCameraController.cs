using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private float mouseSensitivity = 300f;

    // Rango de sensibilidad normalizada (ajusta a tu gusto)
    private const float SENSIBILIDAD_MIN = 50f;
    private const float SENSIBILIDAD_MAX = 600f;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -75f, 75f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
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
}
