using UnityEditor;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform handPoint; // dónde "vive" visualmente la pieza recogida

        [Header("Zona de interacción (reemplaza al raycast)")]
        [Tooltip("SphereCollider en modo Trigger (ver InteractionZone.cs), normalmente " +
                 "hijo de la cámara o del jugador. El radio de esa esfera define el alcance.")]
        [SerializeField] private InteractionZone interactionZone;

        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("Inventario")]
        [SerializeField] private Inventory inventory;

        private void Update()
        {
            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }
        }

        private void TryInteract()
        {
            if (interactionZone == null)
            {
                Debug.LogWarning($"[{name}] PlayerInteractor: falta asignar 'Interaction Zone' en el Inspector.", this);
                return;
            }

            Vector3 origin = playerCamera != null ? playerCamera.transform.position : transform.position;

            // Prioridad 1: ¿hay un mecanismo roto en rango? Reparar el más cercano.
            var mechanism = interactionZone.GetClosestBrokenMechanism(origin);
            if (mechanism != null)
            {
                TryRepairMechanism(mechanism);
                return;
            }

            // Prioridad 2: ¿hay una pieza de repuesto en rango? Recoger la más cercana.
            var part = interactionZone.GetClosestPart(origin);
            if (part != null)
            {
                TryPickupPart(part);
            }
        }

        private void TryRepairMechanism(IPuzzleMechanism mechanism)
        {
            Debug.Log($"[REPAIR] Mecanismo: {mechanism.Data?.displayName}, IsBroken: {mechanism.IsBroken}, Inventario vacío: {inventory == null || inventory.IsEmpty}");

            if (!mechanism.IsBroken)
            {
                return;
            }

            if (inventory == null || inventory.IsEmpty)
            {
                return;
            }

            Debug.Log($"[REPAIR] Pieza en mano - Data: {inventory.CurrentPart.Data}, partId: {inventory.CurrentPart.Data?.partId} | Requerido: {mechanism.Data.requiredPart?.partId}");

            bool repaired = mechanism.TryRepair(inventory.CurrentPart);
            Debug.Log($"[REPAIR] Resultado: {repaired}");

            if (repaired)
            {
                inventory.ConsumeCurrent();
            }
        }

        private void TryPickupPart(ReplacementPart part)
        {
            if (inventory == null || !inventory.IsEmpty) return; // manos ocupadas

            if (!inventory.TryAdd(part)) return;

            Transform target = handPoint != null ? handPoint : transform;
            Vector3 originalWorldScale = part.transform.lossyScale; // escala mundial ANTES de reparentar

            part.transform.SetParent(target, worldPositionStays: false);
            part.transform.localPosition = Vector3.zero;
            part.transform.localRotation = Quaternion.identity;

            // Convertimos la escala mundial que tenía a la escala local que necesita
            // tener AHORA (bajo el nuevo padre) para verse exactamente igual que antes.
            Vector3 parentLossyScale = target.lossyScale;
            part.transform.localScale = new Vector3(
                originalWorldScale.x / parentLossyScale.x,
                originalWorldScale.y / parentLossyScale.y,
                originalWorldScale.z / parentLossyScale.z
            );

            // Si la pieza tiene física, hay que apagarla o va a pelear con el
            // parenting cada FixedUpdate (eso también se puede ver como deformación).
            var rb = part.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            // Se queda ACTIVA (visible en la mano). Apagamos su Collider para
            // que la zona de interacción no la vuelva a detectar como "recogible".
            var col = part.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}