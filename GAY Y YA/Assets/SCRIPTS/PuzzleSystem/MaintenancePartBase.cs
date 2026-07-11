using System;
using System.Collections;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    [DisallowMultipleComponent]
    public abstract class MaintenancePartBase : MonoBehaviour, IPuzzleMechanism
    {
        [Header("Outline (shader properties _Scale y _Color)")]
        [Tooltip("Renderer cuyo material tiene las properties _Scale y _Color del outline. " +
                 "Si se deja vacío, simplemente no hay outline para este mecanismo. " +
                 "IMPORTANTE: el shader necesita tener una property _Color (tipo Color), " +
                 "igual que ya tiene _Scale, si todavía no la tiene hay que agregarla.")]
        [SerializeField] private Renderer outlineRenderer;

        [Header("Colores del outline por estado")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color warningColor = Color.red;

        [Header("Aviso antes de romperse")]
        [Tooltip("Segundos antes de la falla en los que el outline pasa de verde a rojo, " +
                 "como aviso de que está por romperse. Si es mayor que la durabilidad total, " +
                 "avisa apenas se repara.")]
        [SerializeField] private float warningLeadTime = 5f;

        [Header("Visual intercambiable (\"pon cualquier cosa\")")]
        [Tooltip("Punto (Transform vacío) donde vive la pieza actualmente instalada, como " +
                 "hijo. Al reparar, se destruye lo que había ahí y se instancia el prefab " +
                 "completo de la nueva pieza en su lugar. Mismo patrón que 'Hand Point' en " +
                 "PlayerInteractor, pero del lado del mecanismo.")]
        [SerializeField] private Transform partAttachPoint;
        [Tooltip("Opcional: prefab que se instala en 'Part Attach Point' al inicializarse " +
                 "(o al reciclarse del pool), representando la pieza rota/original antes de " +
                 "cualquier reparación. Si se deja vacío, el punto arranca sin nada instalado.")]
        [SerializeField] private GameObject initialPartPrefab;

        [Header("Mecanismo fijo (puesto a mano en el tren)")]
        [Tooltip("Asigna acá el PuzzleMechanismData de ESTE mecanismo si está puesto a mano " +
                 "en la escena (los mecanismos ya NO los genera ni poolea un manager, son " +
                 "fijos en su lugar). Se auto-inicializa solo en OnEnable. Dejalo vacío " +
                 "únicamente si vas a llamar Initialize() vos mismo desde otro script.")]
        [SerializeField] private PuzzleMechanismData fixedData;

        // La pieza actualmente instalada como hijo de partAttachPoint (la original, o la
        // última con la que se reparó). Se destruye/reemplaza entera en cada reparación,
        // así que no hace falta cachear mesh/material como antes.
        private GameObject _installedPartVisual;

        // _Scale del shader: 0.9 = invisible. Roto/aviso = pulsa entre 1.04 y 1.1.
        private static readonly int ScalePropertyId = Shader.PropertyToID("_Scale");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private const float ScaleHidden = 0.9f;
        private const float ScaleMin = 1.04f;
        private const float ScaleMax = 1.1f;
        [SerializeField] private float pulseSpeed = 7f; // ciclos por segundo aprox, ajusta a gusto

        private MaterialPropertyBlock _propBlock;
        private Coroutine _outlineRoutine;

        public bool IsBroken { get; private set; }
        public PuzzleMechanismData Data { get; private set; }

        public event Action<IPuzzleMechanism> OnBroken;
        public event Action<IPuzzleMechanism> OnRepaired;

        public virtual void Initialize(PuzzleMechanismData data)
        {
            Data = data;
            IsBroken = false;

            CancelInvoke(nameof(BreakDown));
            CancelInvoke(nameof(EnterWarningState));

            // Vuelve a la pieza "original" (rota) para este nuevo ciclo. Si venía
            // reciclada del pool con otra pieza instalada de un ciclo anterior, esto
            // la destruye y pone la inicial en su lugar (o queda vacío si no hay una).
            InstallPart(initialPartPrefab);

            SetOutlineColor(healthyColor);
            StartOutline(); // ahora corre SIEMPRE (verde sano / rojo aviso o roto), no solo al romperse.

            ScheduleNextFailure();
        }

        private void ScheduleNextFailure()
        {
            float durability = Data.GetRandomDurability();
            float warningAt = durability - warningLeadTime;

            if (warningAt > 0f)
                Invoke(nameof(EnterWarningState), warningAt);
            else
                EnterWarningState(); // dura menos que el aviso: avisa de una

            Invoke(nameof(BreakDown), durability);
        }

        /// Aviso visual de que el mecanismo está por romperse (todavía no rompió).
        private void EnterWarningState()
        {
            if (IsBroken) return; // ya se rompió, no hace falta el aviso
            SetOutlineColor(warningColor);
        }

        public void BreakDown()
        {
            if (IsBroken) return;
            IsBroken = true;

            CancelInvoke(nameof(EnterWarningState)); // por si se rompe antes del aviso programado
            SetOutlineColor(warningColor); // se queda en rojo mientras está rota

            OnBrokenVisual();
            OnBroken?.Invoke(this);
        }

        public bool TryRepair(ReplacementPart part)
        {
            if (!IsBroken) return false;
            if (part == null || !part.IsCompatibleWith(Data)) return false;

            IsBroken = false;

            SwapInstalledPart(part);
            OnRepairedVisual();
            SetOutlineColor(healthyColor);

            OnRepaired?.Invoke(this);
            ScheduleNextFailure(); // vuelve a degradarse: así se mantiene el loop de mantenimiento
            return true;
        }

        /// Instala físicamente la pieza usada para reparar: destruye lo que había en
        /// partAttachPoint y pone el prefab COMPLETO de la nueva pieza en su lugar.
        /// Esto es lo que permite el chiste de "pon cualquier cosa": el mismo prefab
        /// que el jugador recogió en el mundo (worldPrefab) queda instalado tal cual,
        /// con todos sus materiales/hijos/lo que sea, no solo un mesh suelto.
        private void SwapInstalledPart(ReplacementPart part)
        {
            if (part == null || part.Data == null || part.Data.worldPrefab == null)
            {
                Debug.LogWarning($"[{name}] SwapInstalledPart: la pieza usada no tiene 'World Prefab' asignado en su ReplacementPartData.");
                return;
            }

            InstallPart(part.Data.worldPrefab);
            Debug.Log($"[{name}] Pieza instalada: {part.Data.displayName}");
        }

        /// Destruye la pieza actualmente instalada (si hay) e instancia el prefab dado
        /// como hijo de partAttachPoint. prefab puede ser null (deja el punto vacío).
        private void InstallPart(GameObject prefab)
        {
            if (partAttachPoint == null)
            {
                if (prefab != null)
                    Debug.LogWarning($"[{name}] InstallPart: falta asignar 'Part Attach Point' en el Inspector de este mecanismo.");
                return;
            }

            if (_installedPartVisual != null)
            {
                Destroy(_installedPartVisual);
                _installedPartVisual = null;
            }

            if (prefab == null) return;

            _installedPartVisual = Instantiate(prefab, partAttachPoint);
            _installedPartVisual.transform.localPosition = Vector3.zero;
            _installedPartVisual.transform.localRotation = Quaternion.identity;

            // El prefab puede traer Collider/Rigidbody propios (porque también sirve
            // como pieza recogible suelta en el mundo). Ya instalado, no queremos que
            // estorbe físicamente ni que la zona de interacción lo detecte de nuevo.
            var col = _installedPartVisual.GetComponentInChildren<Collider>();
            if (col != null) col.enabled = false;

            var rb = _installedPartVisual.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
        }

        /// Hook para efectos visuales/sonido cuando se rompe. Override en cada mecanismo hijo.
        protected abstract void OnBrokenVisual();

        /// Hook para efectos visuales/sonido cuando se repara. Override en cada mecanismo hijo.
        protected abstract void OnRepairedVisual();

        private void StartOutline()
        {
            if (outlineRenderer == null) return;
            if (_outlineRoutine != null) StopCoroutine(_outlineRoutine);
            _outlineRoutine = StartCoroutine(PulseOutline());
        }

        private void StopOutline()
        {
            if (_outlineRoutine != null)
            {
                StopCoroutine(_outlineRoutine);
                _outlineRoutine = null;
            }
            SetScale(ScaleHidden);
        }

        // Corre todo el tiempo que el mecanismo está activo (sano, en aviso o roto).
        // Solo cambia el color entre estados; el pulso de escala es siempre el mismo.
        private IEnumerator PulseOutline()
        {
            while (true)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
                float scale = Mathf.Lerp(ScaleMin, ScaleMax, t);
                SetScale(scale);
                yield return null;
            }
        }

        private void SetScale(float value)
        {
            if (outlineRenderer == null) return;

            // MaterialPropertyBlock: cambia la property visualmente sin crear
            // una instancia de material nueva por objeto (cero GC extra,
            // no rompe el instancing/batching de Unity).
            _propBlock ??= new MaterialPropertyBlock();
            outlineRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(ScalePropertyId, value);
            outlineRenderer.SetPropertyBlock(_propBlock);
        }

        private void SetOutlineColor(Color color)
        {
            if (outlineRenderer == null) return;

            _propBlock ??= new MaterialPropertyBlock();
            outlineRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(ColorPropertyId, color);
            outlineRenderer.SetPropertyBlock(_propBlock);
        }

        protected virtual void OnEnable()
        {
            // Si este mecanismo está puesto a mano en la escena (no generado por
            // un manager), se inicializa solo con su propia data la primera vez.
            if (fixedData != null && Data == null)
                Initialize(fixedData);
        }

        protected virtual void OnDisable()
        {
            // limpieza al volver al pool: evita callbacks fantasma sobre un objeto desactivado
            CancelInvoke(nameof(BreakDown));
            CancelInvoke(nameof(EnterWarningState));
            StopOutline();
        }

#if UNITY_EDITOR
        [ContextMenu("TEST: Forzar ruptura ahora")]
        private void ForceBreakNowForTesting() => BreakDown();
#endif
    }
}