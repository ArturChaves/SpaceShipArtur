using System;
using UnityEngine;

namespace SpaceShip.Core
{
    [DisallowMultipleComponent]
    public class TimeWarp : MonoBehaviour
    {
        public static TimeWarp Instance { get; private set; }

        [Header("Intensidade")]
        [Tooltip("Escala de tempo durante o efeito. 0.35 = mundo a 35% da velocidade.")]
        [SerializeField, Range(0.05f, 1f)] private float slowScale = 0.35f;

        [Tooltip("Segundos (reais) que o efeito dura por ativacao.")]
        [SerializeField, Min(0.1f)] private float duration = 5f;

        [Tooltip("Tempo de transicao ao entrar e ao sair, para nao estalar.")]
        [SerializeField, Min(0f)] private float blendTime = 0.25f;

        [Header("Limites")]
        [Tooltip("Teto de duracao acumulada quando o efeito e reativado ja ativo.")]
        [SerializeField, Min(0.1f)] private float maximumStackedDuration = 12f;

        private float remaining;
        private float currentScale = 1f;
        private float defaultFixedDelta = 0.02f;
        private bool wasActive;
        private bool paused;

        public event Action<bool> ActiveChanged;

        public bool IsActive => remaining > 0f;

        public float Remaining => Mathf.Max(0f, remaining);

        public float Normalized => maximumStackedDuration <= 0f
            ? 0f
            : Mathf.Clamp01(remaining / duration);

        public float Weight
        {
            get
            {
                float span = 1f - slowScale;
                return span <= 0.0001f ? 0f : Mathf.Clamp01((1f - currentScale) / span);
            }
        }

        public float DefaultDuration
        {
            get
            {
                RunState run = RunState.Instance;
                return duration + (run != null ? run.BonusSlowMotionSeconds : 0f);
            }
        }

        public bool IsPaused => paused;

        public void SetPaused(bool value)
        {
            if (paused == value)
            {
                return;
            }

            paused = value;
            GameClock.SetPaused(value);

            if (paused)
            {
                Time.timeScale = 0f;
            }
            else
            {
                ApplyScale(currentScale);
            }
        }

        private void Awake()
        {
            Instance = this;
            defaultFixedDelta = Time.fixedDeltaTime;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            RestoreNormalTime();
        }

        private void OnDisable()
        {
            RestoreNormalTime();
        }

        public void Activate()
        {
            Activate(DefaultDuration);
        }

        public void Activate(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            remaining = Mathf.Min(remaining + seconds, maximumStackedDuration);
        }

        public void Cancel()
        {
            remaining = 0f;
        }

        private void Update()
        {
            if (paused)
            {
                return;
            }

            float realDelta = GameClock.PlayerDelta;

            if (remaining > 0f)
            {
                remaining -= realDelta;
                if (remaining < 0f)
                {
                    remaining = 0f;
                }
            }

            float target = remaining > 0f ? slowScale : 1f;

            if (blendTime > 0f)
            {
                float step = realDelta / blendTime;
                currentScale = Mathf.MoveTowards(currentScale, target, step);
            }
            else
            {
                currentScale = target;
            }

            ApplyScale(currentScale);

            bool active = IsActive;
            if (active != wasActive)
            {
                wasActive = active;
                ActiveChanged?.Invoke(active);
            }
        }

        private void ApplyScale(float scale)
        {
            Time.timeScale = scale;

            Time.fixedDeltaTime = defaultFixedDelta * Mathf.Max(0.0001f, scale);
        }

        private void RestoreNormalTime()
        {
            remaining = 0f;
            currentScale = 1f;
            paused = false;
            GameClock.Reset();
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDelta;
        }
    }
}
