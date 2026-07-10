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

    private Rigidbody rb;

    //Corrutinas corrutinas;

    private void Awake()
    {
        //audioManager = FindObjectOfType<AudioManager>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        //corrutinas = GetComponent<Corrutinas>();
    }

    private void Start()
    {
    }

    private void Update()
    {
        if (controlActivo)
        {
            Jump();
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

}