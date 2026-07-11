using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    public class PartSpawnCycle : MonoBehaviour
    {
        [Header("Puntos de spawn (puestos a mano en la escena, arrastra los 4)")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Catálogo de piezas posibles")]
        [SerializeField] private List<ReplacementPartData> catalog = new List<ReplacementPartData>();

        [Header("Timing (valores AL EMPEZAR el viaje, progreso = 0)")]
        [Tooltip("Segundos entre cada spawn individual al llenar los puntos por primera vez.")]
        [SerializeField] private float delayBetweenSpawns = 1.5f;
        [Tooltip("Segundos que tarda en reaparecer una pieza en un punto después de que el jugador la recoge.")]
        [SerializeField] private float respawnDelay = 10f;

        [Header("Dificultad progresiva (según StationJourneyManager)")]
        [Tooltip("Si se deja vacío, la dificultad queda fija en los valores 'al empezar' de " +
                 "arriba durante todo el viaje (delay constante, todos los puntos siempre activos).")]
        [SerializeField] private StationJourneyManager journey;
        [Tooltip("Delay entre spawns cuando el progreso llega a 1 (justo antes de llegar a la estación). " +
                 "Tiene que ser MAYOR a 'Delay Between Spawns' de arriba para que cueste más con el tiempo.")]
        [SerializeField] private float delayBetweenSpawnsAtEnd = 4f;
        [Tooltip("Respawn delay cuando el progreso llega a 1. Igual que arriba, mayor = más difícil.")]
        [SerializeField] private float respawnDelayAtEnd = 30f;
        [Tooltip("Cuántos de los puntos de spawn siguen activos cuando el progreso llega a 1. " +
                 "Los que se desactivan son siempre los MÁS CERCANOS (ver 'Distance Reference " +
                 "Point'); los que sobreviven son los más lejos, así al jugador le cuesta más " +
                 "llegar a tiempo.")]
        [SerializeField] private int minActivePointsAtEnd = 2;
        [Tooltip("Punto desde el cual se mide qué tan 'lejos' está cada spawnPoint (ej. la cabina " +
                 "del jugador, o el vagón de arranque). Sin esto no hay forma de saber cuáles son " +
                 "los puntos más cercanos para ir apagándolos primero — la dificultad de posición " +
                 "no se aplica, solo delay y cantidad de puntos.")]
        [SerializeField] private Transform distanceReferencePoint;

        [Header("Debug")]
        [Tooltip("Muestra en pantalla (arriba a la izquierda) los valores actuales de dificultad " +
                 "mientras jugás/testeás: progreso, delay, respawn, y cuántos puntos siguen activos. " +
                 "Apagalo antes del build final del jam (OnGUI gasta rendimiento).")]
        [SerializeField] private bool showDifficultyDebugHud = true;

        // Instancia actualmente viva en cada punto (o null si el slot está vacío).
        private readonly Dictionary<Transform, ReplacementPart> _installed = new();

        // El "bag" de piezas pendientes por repartir en este ciclo.
        private readonly List<ReplacementPartData> _bag = new();

        // spawnPoints ordenados por distancia a distanceReferencePoint, DESCENDENTE
        // (el más lejos primero). Tomar los primeros N de esta lista = quedarse
        // siempre con los N puntos más lejos, sin importar cuánto se achique N.
        private readonly List<Transform> _sortedByDistanceDescending = new();

        // Puntos que la dificultad actual permite usar para spawnear/respawnear.
        // Se recalcula cada vez que cambia el progreso del viaje.
        private readonly HashSet<Transform> _activePoints = new();

        // Valores de timing YA ajustados por la dificultad actual (interpolados
        // entre los "al empezar" y los "AtEnd" según journey.Progress01).
        private float _currentDelayBetweenSpawns;
        private float _currentRespawnDelay;

        // Guardado solo para mostrarlo en el HUD de debug (ver OnGUI).
        private float _lastKnownProgress01;

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

            BuildDistanceOrder();

            if (journey != null)
            {
                journey.OnProgressChanged += HandleProgressChanged;
                HandleProgressChanged(journey.Progress01); // valores iniciales (normalmente progreso = 0)
            }
            else
            {
                // Sin viaje asignado: dificultad fija en los valores "al empezar", todos los
                // puntos siempre activos. Comportamiento idéntico al de antes de este cambio.
                _currentDelayBetweenSpawns = delayBetweenSpawns;
                _currentRespawnDelay = respawnDelay;
                _activePoints.Clear();
                foreach (var p in spawnPoints)
                    if (p != null) _activePoints.Add(p);
            }

            StartCoroutine(FillPointsRoutine());
        }

        private void OnDestroy()
        {
            if (journey != null)
                journey.OnProgressChanged -= HandleProgressChanged;
        }

        /// Arma _sortedByDistanceDescending UNA vez al arrancar: los spawnPoints
        /// ordenados del más lejos al más cerca de distanceReferencePoint. Es la
        /// lista fija de la que después se van tomando "los primeros N" a medida
        /// que la dificultad reduce cuántos puntos quedan activos.
        private void BuildDistanceOrder()
        {
            _sortedByDistanceDescending.Clear();
            _sortedByDistanceDescending.AddRange(spawnPoints);

            if (distanceReferencePoint == null)
            {
                Debug.LogWarning($"[{name}] PartSpawnCycle: no asignaste 'Distance Reference Point'. " +
                                  "La dificultad no va a poder ir alejando los puntos activos — solo " +
                                  "va a afectar el delay y la cantidad de puntos, en el orden en que " +
                                  "los arrastraste en el Inspector.", this);
                return;
            }

            Vector3 refPos = distanceReferencePoint.position;
            _sortedByDistanceDescending.Sort((a, b) =>
            {
                float distA = a != null ? (a.position - refPos).sqrMagnitude : -1f;
                float distB = b != null ? (b.position - refPos).sqrMagnitude : -1f;
                return distB.CompareTo(distA); // descendente: el más lejos va primero
            });
        }

        /// Se llama cada vez que StationJourneyManager avisa que cambió el progreso.
        /// Recalcula, con una interpolación lineal simple, el timing actual y cuáles
        /// puntos siguen activos.
        private void HandleProgressChanged(float progress01)
        {
            _lastKnownProgress01 = progress01;

            _currentDelayBetweenSpawns = Mathf.Lerp(delayBetweenSpawns, delayBetweenSpawnsAtEnd, progress01);
            _currentRespawnDelay = Mathf.Lerp(respawnDelay, respawnDelayAtEnd, progress01);

            int clampedMin = Mathf.Min(minActivePointsAtEnd, spawnPoints.Length);
            int activeCount = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(spawnPoints.Length, clampedMin, progress01)),
                clampedMin,
                spawnPoints.Length);

            _activePoints.Clear();
            for (int i = 0; i < activeCount && i < _sortedByDistanceDescending.Count; i++)
            {
                var p = _sortedByDistanceDescending[i];
                if (p != null) _activePoints.Add(p);
            }
        }

        // HUD de texto simple arriba a la izquierda de la pantalla, SOLO para
        // testear/afinar números durante el jam. OnGUI corre en Modo Inmediato
        // (sin batching), por eso es un toggle apagable — no lo dejes prendido
        // en el build que entregues.
        private void OnGUI()
        {
            if (!showDifficultyDebugHud) return;

            GUI.Label(new Rect(10, 10, 420, 20),
                $"Progreso del viaje: {_lastKnownProgress01 * 100f:0}%" +
                (journey == null ? "  (SIN journey asignado — dificultad fija)" : ""));

            GUI.Label(new Rect(10, 30, 420, 20),
                $"Delay entre spawns actual: {_currentDelayBetweenSpawns:0.0}s " +
                $"(inicio {delayBetweenSpawns:0.0}s -> final {delayBetweenSpawnsAtEnd:0.0}s)");

            GUI.Label(new Rect(10, 50, 420, 20),
                $"Respawn delay actual: {_currentRespawnDelay:0.0}s " +
                $"(inicio {respawnDelay:0.0}s -> final {respawnDelayAtEnd:0.0}s)");

            GUI.Label(new Rect(10, 70, 420, 20),
                $"Puntos activos ahora: {_activePoints.Count} / {spawnPoints.Length} " +
                $"(mínimo al final: {minActivePointsAtEnd})" +
                (distanceReferencePoint == null ? "  (SIN Distance Reference Point — no aleja items)" : ""));
        }

        private IEnumerator FillPointsRoutine()
        {
            foreach (var point in spawnPoints)
            {
                if (point == null) continue;
                if (!_activePoints.Contains(point)) continue; // desactivado por dificultad desde el arranque
                SpawnAt(point);
                yield return new WaitForSeconds(_currentDelayBetweenSpawns);
            }
        }

        private void SpawnAt(Transform point)
        {
            ReplacementPartData data = DrawFromBag();
            GameObject prefab = data != null ? data.GetRandomWorldPrefab() : null;
            if (data == null || prefab == null)
            {
                Debug.LogWarning($"[{name}] PartSpawnCycle: pieza sin ningún 'World Prefab' asignado ({(data != null ? data.name : "null")}).", this);
                return;
            }

            // Mismo patrón que InstallPart: si había algo en el slot, se destruye antes.
            if (_installed.TryGetValue(point, out var existing) && existing != null)
                Destroy(existing.gameObject);

            GameObject clone = Instantiate(prefab, point.position, point.rotation, point);

            if (!clone.TryGetComponent<ReplacementPart>(out var part))
                part = clone.AddComponent<ReplacementPart>();

            // Le pasamos también 'prefab' (la variante puntual ya elegida) para que
            // al reparar se instale ESTA MISMA, no una nueva tirada al azar.
            part.Initialize(data, prefab);
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
            yield return new WaitForSeconds(_currentRespawnDelay);

            // La dificultad pudo haber apagado este punto MIENTRAS esperábamos
            // (el viaje siguió avanzando durante el respawnDelay). Si ya no está
            // activo, se queda vacío para siempre — es justo el efecto buscado:
            // con el tiempo, los puntos más cercanos dejan de reponerse.
            if (!_activePoints.Contains(point)) yield break;

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