using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Representa UN vagón del tren a efectos de generación procedural de puzzles.
    /// Se coloca como componente en el GameObject contenedor del vagón.
    ///
    /// No sabe nada de pooling ni de mecanismos concretos: solo es "datos + puntos".
    /// TrainLayout es quien decide qué hacer con ellos.
    /// </summary>
    public class WagonSection : MonoBehaviour
    {
        [Header("Identidad")]
        public string wagonId = "Vagon_01";

        [Header("Dificultad de este vagón")]
        [Tooltip("Se compara contra PuzzleMechanismData.difficultyTier al elegir dónde " +
                 "spawnear cada mecanismo. No es una regla estricta: si no hay puntos " +
                 "libres del tier exacto en NINGÚN vagón, TrainLayout cae a cualquier " +
                 "punto libre de cualquier vagón.")]
        public int difficultyTier = 1;

        [Header("Puntos de spawn de ESTE vagón")]
        [Tooltip("Si se deja vacío, se auto-completa en Awake con todos los hijos directos " +
                 "de este objeto. Así solo arrastras Transforms vacíos como hijos del vagón " +
                 "en la jerarquía y no hace falta tocar este array a mano.")]
        [SerializeField] private Transform[] spawnPoints;

        public Transform[] SpawnPoints => spawnPoints;

        private void Awake()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                AutoCollectChildren();
        }

        private void AutoCollectChildren()
        {
            spawnPoints = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                spawnPoints[i] = transform.GetChild(i);
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-completar spawnPoints desde hijos")]
        private void AutoCollectChildrenEditor() => AutoCollectChildren();
#endif
    }
}
