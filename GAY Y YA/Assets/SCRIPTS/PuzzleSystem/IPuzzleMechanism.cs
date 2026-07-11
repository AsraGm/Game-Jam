using System;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Contrato que debe cumplir CUALQUIER mecanismo de mantenimiento/puzzle.
    /// El sistema central (MaintenanceManager) solo conoce esta interfaz,
    /// nunca las clases concretas. Así se pueden agregar mecanismos nuevos
    /// sin tocar el código del manager ni del generador procedural.
    /// </summary>
    public interface IPuzzleMechanism
    {
        bool IsBroken { get; }
        PuzzleMechanismData Data { get; }

        /// Inicializa el mecanismo con su configuración (llamado al spawnear/poolear).
        void Initialize(PuzzleMechanismData data);

        /// Intenta reparar usando la pieza de repuesto encontrada por el jugador.
        /// Devuelve true si la pieza era compatible y se reparó.
        bool TryRepair(ReplacementPart part);

        /// Fuerza que el mecanismo falle (llamado por el timer de desgaste).
        void BreakDown();

        event Action<IPuzzleMechanism> OnBroken;
        event Action<IPuzzleMechanism> OnRepaired;
    }
}
