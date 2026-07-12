using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Arma un resumen de la partida (nivel, tiempo jugado, si se llegó o no,
    /// piezas usadas y piezas recogidas) y lo escribe en un TMP_Text cuando
    /// termina el viaje (Game Over o Llegada).
    ///
    /// Es independiente de JourneyProgressUI: solo necesita las referencias de
    /// abajo, y maneja su propio panel de resultado (resultsRoot) en vez de vivir
    /// adentro de los paneles de esa otra UI.
    ///
    /// IMPORTANTE sobre dónde vive este script:
    /// 'OnGameOver' y 'OnArrived' se disparan UNA sola vez cada uno (ver
    /// StationJourneyManager). Este script tiene que vivir en un GameObject
    /// SIEMPRE ACTIVO en la escena (ej. el mismo que JourneyProgressUI, o un
    /// "Results Controller" vacío aparte) — NO adentro de 'gameOverPanel' ni de
    /// 'arrivedPanel' de JourneyProgressUI. Si estuviera metido en uno de esos
    /// dos, solo se enteraría del resultado que prende A ESE panel (ej. viviendo
    /// dentro de gameOverPanel, jamás correría cuando se gana, porque ese objeto
    /// nunca se activa en ese caso).
    ///
    /// En cambio, este script prende su PROPIO panel (resultsRoot) cuando
    /// corresponda, sea cual sea el resultado — así funciona igual para
    /// Game Over y para Llegada. Como red de seguridad extra, OnEnable() también
    /// revisa si el viaje YA terminó al activarse y llama a HandleResult() a
    /// mano si es así (por si en algún setup particular este script SÍ termina
    /// dependiendo de que algo externo lo active).
    /// </summary>
    public class RunResultStatsUI : MonoBehaviour
    {
        [Header("Fuentes de datos")]
        [SerializeField] private StationJourneyManager journey;
        [SerializeField] private MaintenanceManager maintenanceManager;
        [SerializeField] private Inventory inventory;

        [Header("Panel raíz de resultado (recomendado)")]
        [Tooltip("Contenedor de stats")]
        [SerializeField] private GameObject resultsRoot;

        [Header("Dónde se escribe el resultado")]
        [SerializeField] private TMP_Text statsText;

        [Header("Paneles separados para Ganaste / Perdiste (opcional)")]
        [Tooltip("Se activa SOLO cuando el tren llega a la estación (ganaste)")]
        [SerializeField] private GameObject winOnlyPanel;
        [Tooltip("Se activa SOLO cuando explota la máquina (Game Over)")]
        [SerializeField] private GameObject loseOnlyPanel;

        [Tooltip("Nombre 'lindo' del nivel para mostrar en vez del nombre técnico de la")]
        [SerializeField] private string levelDisplayName;

        private readonly StringBuilder _sb = new StringBuilder();

        private void Awake()
        {
            // Red de seguridad: por si en el Editor alguien lo dejó activado a
            // mano mientras probaba. El panel de resultado no debería verse
            // hasta que HandleResult() lo prenda.
            if (resultsRoot != null) resultsRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (journey == null)
            {
                Debug.LogWarning($"[{name}] RunResultStatsUI: falta asignar 'Journey' en el Inspector.", this);
                return;
            }

            journey.OnGameOver += HandleResult;
            journey.OnArrived += HandleResult;

            // Nos ponemos al día: si el viaje ya terminó ANTES de que este objeto
            // se activara (típico si vive dentro de un panel que arranca apagado),
            // el evento de 'una sola vez' correspondiente ya pasó y nos lo perdimos.
            // Corremos el resultado a mano para no quedarnos con texto/paneles
            // desactualizados (vacíos o con el estado equivocado).
            if (journey.IsGameOver || journey.HasArrived)
                HandleResult();
        }

        private void OnDisable()
        {
            if (journey == null) return;
            journey.OnGameOver -= HandleResult;
            journey.OnArrived -= HandleResult;
        }

        private void HandleResult()
        {
            // Este método solo se llama cuando el viaje YA terminó (por evento o
            // por el 'ponerse al día' de OnEnable), así que HasArrived ya refleja
            // el resultado final de forma confiable.
            bool won = journey.HasArrived;

            if (resultsRoot != null) resultsRoot.SetActive(true);
            if (winOnlyPanel != null) winOnlyPanel.SetActive(won);
            if (loseOnlyPanel != null) loseOnlyPanel.SetActive(!won);

            if (statsText == null)
            {
                Debug.LogWarning($"[{name}] RunResultStatsUI: falta asignar 'Stats Text' en el Inspector.", this);
                return;
            }

            string levelName = !string.IsNullOrEmpty(levelDisplayName)
                ? levelDisplayName
                : SceneManager.GetActiveScene().name;

            int minutes = Mathf.FloorToInt(journey.ElapsedSeconds / 60f);
            int seconds = Mathf.FloorToInt(journey.ElapsedSeconds % 60f);

            // Ambos son opcionales: si no asignaste MaintenanceManager/Inventory acá,
            // simplemente no se muestran esas líneas en vez de romper todo.
            int? repaired = maintenanceManager != null ? maintenanceManager.TotalRepairsCount : null;
            int? collected = inventory != null ? inventory.TotalPartsCollected : null;

            _sb.Clear();
            _sb.AppendLine($"Level: {levelName}");
            _sb.AppendLine($"Time: {minutes:00}:{seconds:00}");
            _sb.AppendLine($"¿Arrived?: {(won ? "Sí" : "No")}");

            if (repaired.HasValue)
                _sb.AppendLine($"Used Pieces: {repaired.Value}");

            if (collected.HasValue)
                _sb.Append($"Pieces Collected: {collected.Value}");

            statsText.text = _sb.ToString().TrimEnd();
        }
    }
}