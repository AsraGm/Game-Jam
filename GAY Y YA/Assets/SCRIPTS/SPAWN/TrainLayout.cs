using System.Collections.Generic;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Organiza los spawnPoints del tren POR VAGÓN. Reemplaza al
    /// "Transform[] spawnPoints" plano que tenía antes MaintenanceManager.
    ///
    /// Responsabilidad única: dado un tier de dificultad, devolver un punto de
    /// spawn LIBRE (preferentemente de un vagón con ese mismo tier). No sabe
    /// nada de pooling, ScriptableObjects de mecanismos, ni de instanciar nada
    /// — eso lo sigue haciendo MaintenanceManager.
    /// </summary>
    public class TrainLayout : MonoBehaviour
    {
        [Tooltip("Si se deja vacío, se auto-completa en Awake con todos los WagonSection " +
                 "hijos de este objeto. Arma un GameObject por vagón (cada uno con su " +
                 "propio WagonSection) como hijos de este 'Train'.")]
        [SerializeField] private WagonSection[] wagons;

        // Puntos ya ocupados por un mecanismo activo. HashSet: O(1) para
        // "¿está libre?", cero recorridos extra por spawn.
        private readonly HashSet<Transform> _occupiedPoints = new();

        private void Awake()
        {
            if (wagons == null || wagons.Length == 0)
                wagons = GetComponentsInChildren<WagonSection>();

            // TEMPORAL — bórralo cuando ya no lo necesites para debuggear.
            Debug.Log($"[TrainLayout] Vagones encontrados: {wagons.Length}. Puntos de spawn totales: {TotalPointCount}");
        }

        /// Cantidad total de puntos de spawn en todo el tren (todos los vagones).
        /// Útil para decidir cuántos puzzles generar al arrancar el nivel.
        public int TotalPointCount
        {
            get
            {
                int total = 0;
                foreach (var wagon in wagons) total += wagon.SpawnPoints.Length;
                return total;
            }
        }

        /// Intenta conseguir un punto libre para un mecanismo del tier dado.
        /// Prioridad: vagón con tier exacto -> cualquier vagón con punto libre.
        /// Devuelve false si el tren entero está lleno (no queda ni un punto libre).
        public bool TryGetSpawnPoint(int difficultyTier, out Transform point)
        {
            point = FindFreePointInTier(difficultyTier);
            if (point != null)
            {
                _occupiedPoints.Add(point);
                return true;
            }

            // Fallback: cualquier vagón, cualquier tier, mientras haya un punto libre.
            point = FindFreePointInTier(tier: null);
            if (point != null)
            {
                _occupiedPoints.Add(point);
                return true;
            }

            return false;
        }

        /// Igual que TryGetSpawnPoint, pero sin preferencia de tier: cualquier
        /// punto libre de cualquier vagón sirve. Pensado para spawnear piezas de
        /// repuesto (ReplacementPartSpawner), que no necesitan matching por dificultad.
        public bool TryGetAnyFreePoint(out Transform point)
        {
            point = FindFreePointInTier(null);
            if (point != null)
            {
                _occupiedPoints.Add(point);
                return true;
            }
            return false;
        }

        /// Libera un punto (ej. cuando su mecanismo vuelve al pool). Sin esto,
        /// un vagón quedaría "lleno" para siempre apenas se recicla un mecanismo.
        public void ReleasePoint(Transform point)
        {
            _occupiedPoints.Remove(point);
        }

        private Transform FindFreePointInTier(int? tier)
        {
            foreach (var wagon in wagons)
            {
                if (tier.HasValue && wagon.difficultyTier != tier.Value) continue;

                foreach (var p in wagon.SpawnPoints)
                {
                    if (!_occupiedPoints.Contains(p))
                        return p;
                }
            }
            return null;
        }
    }
}