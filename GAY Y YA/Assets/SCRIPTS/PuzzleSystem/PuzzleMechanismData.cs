using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Datos de configuración de UN TIPO de mecanismo/puzzle.
    /// Esto es lo que va en la "lista de prefabs" del catálogo.
    /// Crear uno nuevo: click derecho en Project > Create > TrainMechanic > Puzzle Mechanism Data
    /// </summary>
    [CreateAssetMenu(fileName = "New Puzzle Mechanism", menuName = "TrainMechanic/Puzzle Mechanism Data")]
    public class PuzzleMechanismData : ScriptableObject
    {
        [Header("Identidad")]
        public string mechanismId; // ej: "valve_01", "fusebox_01"
        public string displayName;

        [Header("Prefab del mecanismo (debe tener un componente que implemente IPuzzleMechanism)")]
        public GameObject mechanismPrefab;

        [Header("Pieza de repuesto compatible")]
        public ReplacementPartData requiredPart;

        [Header("Duración antes de fallar (segundos), rango para variedad")]
        public float minDurability = 30f;
        public float maxDurability = 90f;

        [Header("Peso para selección procedural (mayor = más probable que aparezca)")]
        [Min(0.01f)] public float spawnWeight = 1f;

        public float GetRandomDurability() => Random.Range(minDurability, maxDurability);
    }
}
