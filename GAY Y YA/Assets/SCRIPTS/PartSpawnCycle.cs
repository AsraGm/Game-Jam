using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Spawner simple para jam: llena puntos fijos puestos a mano en la escena,
    /// usando el MISMO patrón que MaintenancePartBase.InstallPart (si había algo
    /// en el punto, se destruye; se instancia el prefab nuevo en su lugar).
    /// Nada de tracking de "puntos libres" ni ScriptableObject de layout: cada
    /// Transform del array ES un slot fijo.
    ///
    /// El catálogo se reparte con un "shuffle bag" (como el generador de piezas
    /// de Tetris): se mezcla la lista completa, se va sacando una por una sin
    /// repetir, y cuando se vacía se vuelve a mezclar. Así nunca sabés qué sigue,
    /// pero tampoco se repite la misma dos veces seguidas, y el ciclo nuevo NO
    /// tiene por qué arrancar en la primera del catálogo.
    /// </summary>
    public class PartSpawnCycle : MonoBehaviour
    {
        [Header("Puntos de spawn (puestos a mano en la escena, arrastra los 4)")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Catálogo de piezas posibles")]
        [SerializeField] private List<ReplacementPartData> catalog = new List<ReplacementPartData>();

        [Header("Timing")]
        [Tooltip("Segundos entre cada spawn individual al llenar los puntos por primera vez.")]
        [SerializeField] private float delayBetweenSpawns = 1.5f;
        [Tooltip("Segundos que tarda en reaparecer una pieza en un punto después de que el jugador la recoge.")]
        [SerializeField] private float respawnDelay = 10f;

        // Instancia actualmente viva en cada punto (o null si el slot está vacío).
        private readonly Dictionary<Transform, ReplacementPart> _installed = new();

        // El "bag" de piezas pendientes por repartir en este ciclo.
        private readonly List<ReplacementPartData> _bag = new();

        private void Start()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError($"[{name}] PartSpawnCycle: no hay spawnPoints asignados en el Inspector.", this);
                return;
            }
            if (catalog == null || catalog.Count == 0)
            {
                Debug.LogError($"[{name}] PartSpawnCycle: el catálogo está vacío.", this);
                return;
            }
            if (catalog.Count < spawnPoints.Length)
            {
                Debug.LogWarning($"[{name}] PartSpawnCycle: el catálogo tiene {catalog.Count} pieza(s) " +
                                  $"pero hay {spawnPoints.Length} puntos. Con menos piezas únicas que puntos, " +
                                  $"vas a ver repetidos en pantalla sí o sí — agregá más entradas al catálogo " +
                                  $"para evitarlo.", this);
            }

            StartCoroutine(FillPointsRoutine());
        }

        private IEnumerator FillPointsRoutine()
        {
            foreach (var point in spawnPoints)
            {
                if (point == null) continue;
                SpawnAt(point);
                yield return new WaitForSeconds(delayBetweenSpawns);
            }
        }

        private void SpawnAt(Transform point)
        {
            ReplacementPartData data = DrawFromBag();
            if (data == null || data.worldPrefab == null)
            {
                Debug.LogWarning($"[{name}] PartSpawnCycle: pieza sin 'World Prefab' asignado ({(data != null ? data.name : "null")}).", this);
                return;
            }

            // Mismo patrón que InstallPart: si había algo en el slot, se destruye antes.
            if (_installed.TryGetValue(point, out var existing) && existing != null)
                Destroy(existing.gameObject);

            GameObject clone = Instantiate(data.worldPrefab, point.position, point.rotation, point);

            if (!clone.TryGetComponent<ReplacementPart>(out var part))
                part = clone.AddComponent<ReplacementPart>();

            part.Initialize(data);
            part.OnPickedUp += _ => HandlePickedUp(point);

            _installed[point] = part;
        }

        private void HandlePickedUp(Transform point)
        {
            _installed[point] = null;
            StartCoroutine(RespawnAfterDelay(point));
        }

        private IEnumerator RespawnAfterDelay(Transform point)
        {
            yield return new WaitForSeconds(respawnDelay);
            SpawnAt(point);
        }

        /// Saca una pieza al azar del bag actual, evitando devolver una que YA
        /// esté instalada en otro punto en este momento (para no ver el mismo
        /// item repetido dos veces en pantalla al mismo tiempo). Si está vacío,
        /// arma uno nuevo mezclado.
        private ReplacementPartData DrawFromBag()
        {
            if (_bag.Count == 0) RefillBag();
            if (_bag.Count == 0) return null;

            for (int i = _bag.Count - 1; i >= 0; i--)
            {
                if (IsCurrentlyInstalled(_bag[i])) continue;

                var picked = _bag[i];
                _bag.RemoveAt(i);
                return picked;
            }

            // Todo lo que queda en el bag ya está instalado en algún punto
            // (catálogo más chico que la cantidad de slots): no hay forma de
            // evitar el repetido, se usa igual el último del bag.
            int last = _bag.Count - 1;
            var fallback = _bag[last];
            _bag.RemoveAt(last);
            return fallback;
        }

        private bool IsCurrentlyInstalled(ReplacementPartData data)
        {
            foreach (var part in _installed.Values)
            {
                if (part != null && part.Data == data) return true;
            }
            return false;
        }

        private void RefillBag()
        {
            _bag.AddRange(catalog);

            // Fisher-Yates shuffle
            for (int i = _bag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
        }
    }
}
