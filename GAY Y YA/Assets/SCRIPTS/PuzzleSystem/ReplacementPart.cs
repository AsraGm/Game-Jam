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
                 "Si la genera un spawner por código, se puede dejar vacía y se " +
                 "asigna sola vía Initialize().")]
        [SerializeField] private ReplacementPartData data;

        public ReplacementPartData Data => data;

        public void Initialize(ReplacementPartData newData)
        {
            data = newData;
        }

        public bool IsCompatibleWith(PuzzleMechanismData mechanismData)
        {
            return mechanismData.requiredPart != null &&
                   Data != null &&
                   mechanismData.requiredPart.partId == Data.partId;
        }
    }
}
