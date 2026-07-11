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

        /// Cuántas piezas se recogieron en total durante la partida (acumulado,
        /// no baja cuando se consume una). Pensado para las stats de fin de
        /// partida (RunResultStatsUI) — típicamente va a ser igual o un poco
        /// mayor a MaintenanceManager.TotalRepairsCount (si el jugador terminó
        /// la partida con una pieza en la mano sin llegar a usarla).
        public int TotalPartsCollected { get; private set; }

        /// Se dispara cuando cambia lo que el jugador trae en mano.
        /// null significa "manos vacías". Útil para UI sin Update().
        public event Action<ReplacementPart> OnPartChanged;

        public bool IsEmpty => CurrentPart == null;

        /// Devuelve true si se pudo guardar (manos vacías).
        public bool TryAdd(ReplacementPart part)
        {
            if (!IsEmpty || part == null) return false;

            CurrentPart = part;
            TotalPartsCollected++;
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