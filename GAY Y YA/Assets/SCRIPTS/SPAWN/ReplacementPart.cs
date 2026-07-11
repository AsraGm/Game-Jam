using System;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Instancia en el mundo de una pieza de repuesto que el jugador puede recoger
    /// y usar para reparar un mecanismo compatible.
    /// </summary>
    public class ReplacementPart : MonoBehaviour
    {
        [Tooltip("Si esta pieza la colocas a mano en la escena, asígnala aquí. " +
                 "Si la genera PartSpawnCycle por código, se puede dejar vacía " +
                 "y se asigna sola vía Initialize().")]
        [SerializeField] private ReplacementPartData data;

        public ReplacementPartData Data => data;

        /// Se dispara cuando el jugador la recoge (PlayerInteractor.TryPickupPart).
        /// PartSpawnCycle se suscribe a esto para saber cuándo volver a llenar ese slot.
        public event Action<ReplacementPart> OnPickedUp;

        public void Initialize(ReplacementPartData newData)
        {
            data = newData;
        }

        /// Llamado por PlayerInteractor justo después de guardarla en el inventario.
        public void NotifyPickedUp()
        {
            OnPickedUp?.Invoke(this);
        }

        public bool IsCompatibleWith(PuzzleMechanismData mechanismData)
        {
            return mechanismData.requiredPart != null &&
                   Data != null &&
                   mechanismData.requiredPart.partId == Data.partId;
        }
    }
}
