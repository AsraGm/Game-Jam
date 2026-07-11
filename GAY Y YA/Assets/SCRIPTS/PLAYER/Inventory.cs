using System;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Inventario del jugador: por ahora, UNA sola pieza a la vez.
    /// Se separa de PlayerInteractor para que la UI de estado (u otros
    /// sistemas) puedan suscribirse a OnPartChanged sin acoplarse al
    /// código de raycast/interacción.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        public ReplacementPart CurrentPart { get; private set; }

        /// Se dispara cuando cambia lo que el jugador trae en mano.
        /// null significa "manos vacías". Útil para UI sin Update().
        public event Action<ReplacementPart> OnPartChanged;

        public bool IsEmpty => CurrentPart == null;

        /// Devuelve true si se pudo guardar (manos vacías).
        public bool TryAdd(ReplacementPart part)
        {
            if (!IsEmpty || part == null) return false;

            CurrentPart = part;
            OnPartChanged?.Invoke(CurrentPart);
            return true;
        }

        /// Usa (gasta) la pieza actual, ej. al reparar con éxito.
        public void ConsumeCurrent()
        {
            if (CurrentPart == null) return;

            Destroy(CurrentPart.gameObject);
            CurrentPart = null;
            OnPartChanged?.Invoke(null);
        }
    }
}
