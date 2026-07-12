using UnityEngine;
using UnityEngine.UI;

namespace TrainMechanic.Puzzles
{
    public class JourneyProgressUI : MonoBehaviour
    {
        [Header("Fuente de progreso")]
        [SerializeField] private StationJourneyManager journey;

        [Header("Barra de progreso (opcional)")]
        [Tooltip("Image en modo 'Filled' (Horizontal), Fill Method de izquierda a derecha. " +
                 "Si se deja vacía, simplemente no se actualiza ninguna barra.")]
        [SerializeField] private Image fillBar;

        [Header("Ícono del tren sobre el riel (opcional)")]
        [Tooltip("RectTransform del ícono/sprite del tren que se desliza entre los dos puntos.")]
        [SerializeField] private RectTransform trainIcon;
        [Tooltip("Posición (anchoredPosition) del ícono cuando el progreso es 0 (estación de salida).")]
        [SerializeField] private RectTransform trackStartPoint;
        [Tooltip("Posición (anchoredPosition) del ícono cuando el progreso es 1 (próxima estación).")]
        [SerializeField] private RectTransform trackEndPoint;

        [Header("Paneles de resultado")]
        [Tooltip("Se activa cuando explota la máquina (Game Over).")]
        [SerializeField] private GameObject gameOverPanel;
        [Tooltip("Opcional: se activa cuando se llega a la estación sin explotar.")]
        [SerializeField] private GameObject arrivedPanel;

        private void OnEnable()
        {
            if (journey == null)
            {
                Debug.LogWarning($"[{name}] JourneyProgressUI: falta asignar 'Journey' en el Inspector. " +
                                  "Esta UI se queda congelada tal cual la ves ahora.", this);
                return;
            }

            // Error de configuración MUY común: arrastrar el mismo panel a los dos
            // campos (o dejar 'Arrived Panel' vacío y que alguien lo complete después
            // copiando el de Game Over). Si esto pasa, ganar y perder se ven IGUAL en
            // pantalla — lo avisamos fuerte por consola en vez de dejar que parezca
            // "el código no distingue game over de llegada".
            if (gameOverPanel != null && gameOverPanel == arrivedPanel)
            {
                Debug.LogError($"[{name}] JourneyProgressUI: 'Game Over Panel' y 'Arrived Panel' " +
                                "apuntan al MISMO GameObject en el Inspector. Por eso ganar y perder " +
                                "muestran la misma pantalla — necesitás dos paneles distintos, uno " +
                                "para cada campo.", this);
            }

            journey.OnProgressChanged += HandleProgressChanged;
            journey.OnGameOver += HandleGameOver;
            journey.OnArrived += HandleArrived;

            // Pinta el estado actual al activarse (por si esta UI se activa después
            // de que el viaje ya arrancó, ej. al volver de un menú de pausa).
            HandleProgressChanged(journey.Progress01);
            if (gameOverPanel != null) gameOverPanel.SetActive(journey.IsGameOver);
            if (arrivedPanel != null) arrivedPanel.SetActive(journey.HasArrived);
        }

        private void OnDisable()
        {
            if (journey == null) return;
            journey.OnProgressChanged -= HandleProgressChanged;
            journey.OnGameOver -= HandleGameOver;
            journey.OnArrived -= HandleArrived;
        }

        private void HandleProgressChanged(float progress01)
        {
            if (fillBar != null)
                fillBar.fillAmount = progress01;

            if (trainIcon != null && trackStartPoint != null && trackEndPoint != null)
            {
                trainIcon.anchoredPosition = Vector2.Lerp(
                    trackStartPoint.anchoredPosition,
                    trackEndPoint.anchoredPosition,
                    progress01);
            }
        }

        private void HandleGameOver()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }

        private void HandleArrived()
        {
            if (arrivedPanel != null) arrivedPanel.SetActive(true);
        }
    }
}