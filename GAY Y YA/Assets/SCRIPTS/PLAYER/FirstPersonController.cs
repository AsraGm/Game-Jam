using UnityEngine;

public class MovementController : MonoBehaviour
{
    public float crouchSpeed = 3;
    public float walkSpeed = 5;
    public float runSpeed = 7;
    public float velSalto = 5;
    public bool controlActivo = true;
    //public AudioManager audioManager;
    private float ultimoPasoTime;
    [SerializeField] float tiempoEntrePasos = .5f;

    [Header("Crouch / Agacharse (solo cámara)")]
    [Tooltip("Cuánto baja la cámara (en unidades locales) cuando estás agachado.")]
    [SerializeField] private float crouchCameraOffset = 0.6f;
    [Tooltip("Qué tan rápido transiciona la cámara al agacharte/pararte (mayor = más rápido).")]
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [Tooltip("El pivote/transform de la cámara. Tiene que estar asignado para que el agachado se note.")]
    [SerializeField] private Transform cameraHolder;

    public bool IsCrouchingActive { get; private set; }

    private Rigidbody rb;
    private float standingCameraY;

    //Corrutinas corrutinas;

    private void Awake()
    {
        //audioManager = FindObjectOfType<AudioManager>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        //corrutinas = GetComponent<Corrutinas>();

        if (cameraHolder != null)
            standingCameraY = cameraHolder.localPosition.y;
    }

    private void Start()
    {
    }

    private void Update()
    {
        if (controlActivo)
        {
            Jump();
            HandleCrouch();
        }
        if (IsMoving() && Time.time > ultimoPasoTime + tiempoEntrePasos)
        {
            //audioManager.PlaySound("Pasos");
            ultimoPasoTime = Time.time;
        }
    }

    private void FixedUpdate()
    {
        if (controlActivo)
        {
            Move();
        }
    }

    private void Jump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, velSalto, rb.linearVelocity.z);

        }
    }

    private void Move()
    {
        rb.linearVelocity = transform.rotation * new Vector3(HorizontalMove(), rb.linearVelocity.y, VerticalMove());

    }

    private float ActualSpeed()
    {
        return IsRunning() ? runSpeed : IsCrouching() ? crouchSpeed : walkSpeed; // Operador ternario
    }

    public float HorizontalMove()
    {
        return Input.GetAxis("Horizontal") * ActualSpeed();
    }

    public float VerticalMove()
    {
        return Input.GetAxis("Vertical") * ActualSpeed();
    }

    public bool IsMoving()
    {
        if (HorizontalMove() != 0 || VerticalMove() != 0)
        {
            //Debug.Log("Me muevo");
            return true;
        }
        else
        {
            //Debug.Log("No me muevo");
            return false;
        }
    }

    public bool IsRunning()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    private bool IsCrouching()
    {
        return Input.GetKey(KeyCode.LeftControl);
    }

    // ---- Crouch real: solo cámara, sin tocar el collider ----
    // Al soltar Ctrl se para de inmediato, sin chequeos de por medio.

    private void HandleCrouch()
    {
        IsCrouchingActive = IsCrouching();

        if (cameraHolder == null) return;

        float targetY = IsCrouchingActive ? standingCameraY - crouchCameraOffset : standingCameraY;

        Vector3 pos = cameraHolder.localPosition;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * crouchTransitionSpeed);
        cameraHolder.localPosition = pos;
    }
}