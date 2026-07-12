using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    public class StationJourneyManager : MonoBehaviour
    {
        [Header("Duración del viaje")]
        [Tooltip("Segundos que tarda el viaje completo (estación A -> estación B) si no pasa nada raro.")]
        [SerializeField] private float journeyDurationSeconds = 180f;

        [Header("Condición de derrota")]
        [Tooltip("El manager que ya tiene la lista de mecanismos rotos actuales.")]
        [SerializeField] private MaintenanceManager maintenanceManager;
        [Tooltip("Si en algún momento hay ESTA cantidad de mecanismos rotos a la vez (o más), " +
                 "'explota la máquina': Game Over inmediato, sin importar cuánto faltaba para llegar.")]
        [SerializeField] private int maxSimultaneousBroken = 3;

        private float _elapsed;

        /// 0 al arrancar el viaje, 1 al llegar a la próxima estación. Se congela si
        /// hay Game Over o si ya se llegó.
        public float Progress01 { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool HasArrived { get; private set; }

        /// Segundos reales jugados hasta ahora (o hasta que terminó la partida,
        /// si ya hubo Game Over o Llegada). Pensado para las stats de fin de
        /// partida (RunResultStatsUI).
        public float ElapsedSeconds => _elapsed;

        /// Progreso actualizado cada frame (para animar barra + ícono del tren).
        public event Action<float> OnProgressChanged;
        /// Se dispara UNA vez, cuando explota la máquina (demasiados mecanismos rotos a la vez).
        public event Action OnGameOver;
        /// Se dispara UNA vez, cuando el tren llega a la próxima estación sin explotar.
        public event Action OnArrived;

        private void OnEnable()
        {
            if (maintenanceManager == null)
            {
                Debug.LogWarning($"[{name}] StationJourneyManager: falta asignar 'Maintenance Manager' " +
                                  "en el Inspector. Sin esto, NUNCA va a disparar Game Over por mecanismos " +
                                  "rotos, el viaje solo va a terminar por tiempo.", this);
                return;
            }

            maintenanceManager.OnBrokenListChanged += HandleBrokenListChanged;
        }

        private void OnDisable()
        {
            if (maintenanceManager != null)
                maintenanceManager.OnBrokenListChanged -= HandleBrokenListChanged;
        }

        private void Update()
        {
            if (IsGameOver || HasArrived) return;
            if (DialogueSystem.DialogoActivo) return; // no contar tiempo mientras hay diálogo

            _elapsed += Time.deltaTime;
            Progress01 = Mathf.Clamp01(_elapsed / journeyDurationSeconds);
            OnProgressChanged?.Invoke(Progress01);

            if (Progress01 >= 1f)
            {
                HasArrived = true;
                Debug.Log($"[{name}] StationJourneyManager: LLEGADA — el tren llegó a la próxima estación.", this);
                FreezeGame();
                OnArrived?.Invoke();
            }
        }

        /// Detiene TODO lo que dependa de Time.timeScale: movimiento del jugador
        /// (MovementController usa Rigidbody + FixedUpdate, que se frena solo con
        /// timeScale = 0), rotación de cámara (MouseLook multiplica por
        /// Time.deltaTime), y cualquier coroutine con WaitForSeconds (como los
        /// timers de desgaste/respawn). Además desbloquea el cursor para que se
        /// pueda clickear el panel de resultado.
        private void FreezeGame()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            // Seguro: si esta instancia se destruye mientras el juego seguía
            // congelado (ej. al recargar la escena desde el botón de "reintentar"
            // del panel de Game Over), no queremos arrastrar timeScale = 0 al
            // nivel siguiente.
            Time.timeScale = 1f;
        }

        private void HandleBrokenListChanged(IReadOnlyList<IPuzzleMechanism> broken)
        {
            if (IsGameOver || HasArrived) return;
            if (broken.Count < maxSimultaneousBroken) return;

            IsGameOver = true;
            Debug.Log($"[{name}] StationJourneyManager: GAME OVER — mecanismos rotos simultáneos: " +
                      $"{broken.Count}/{maxSimultaneousBroken}.", this);
            FreezeGame();
            OnGameOver?.Invoke();
        }

#if UNITY_EDITOR
        [ContextMenu("TEST: Forzar Game Over ahora")]
        private void ForceGameOverForTesting()
        {
            if (IsGameOver || HasArrived) return;
            IsGameOver = true;
            FreezeGame();
            OnGameOver?.Invoke();
        }

        [ContextMenu("TEST: Forzar llegada ahora")]
        private void ForceArrivalForTesting()
        {
            if (IsGameOver || HasArrived) return;
            Progress01 = 1f;
            HasArrived = true;
            FreezeGame();
            OnProgressChanged?.Invoke(Progress01);
            OnArrived?.Invoke();
        }
#endif
    }
}