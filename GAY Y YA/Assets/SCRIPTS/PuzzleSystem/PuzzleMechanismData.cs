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

        [Header("Dificultad (para elegir el vagón al spawnear)")]
        [Tooltip("TrainLayout intenta ubicar este mecanismo en un WagonSection con el " +
                 "mismo difficultyTier. Si ningún vagón libre coincide, cae a cualquier " +
                 "punto libre igual. 1 = fácil, mayor número = más difícil.")]
        public int difficultyTier = 1;

        public float GetRandomDurability() => Random.Range(minDurability, maxDurability);
    }
}
