using UnityEngine;
public class GroundChecker : MonoBehaviour
{
    [Header("Ground Check")]
    [Tooltip("Transform hijo ubicado a la altura de los pies del personaje.")]
    public Transform groundCheck;

    [Tooltip("Radio de la esfera de chequeo.")]
    public float groundCheckRadius = 0.3f;

    [Tooltip("Qué capas cuentan como 'suelo'.")]
    public LayerMask whatIsGround;

    // Se mantiene público para poder verlo en el Inspector mientras jugás (debug),
    // pero desde código externo se recomienda leer la propiedad IsGrounded.
    public bool grounded;

    // Propiedad pública prolija para que otros scripts (movimiento, animaciones, sonido) la lean
    public bool IsGrounded => grounded;

    private void Awake()
    {
        if (groundCheck == null)
            Debug.LogError($"[{nameof(GroundChecker)}] Asigná el Transform de groundCheck en el inspector.", this);
    }

    private void FixedUpdate()
    {
        CheckGround();
    }

    private void CheckGround()
    {
        if (groundCheck == null) return;
        grounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, whatIsGround);
    }

    // Dibuja el gizmo en el editor para que veas exactamente dónde chequea
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}