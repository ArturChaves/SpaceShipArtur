using System;
using UnityEngine;

namespace SpaceShip.Core
{
    [DisallowMultipleComponent]
    public class ComboTracker : MonoBehaviour
    {
        public static ComboTracker Instance { get; private set; }

        [Header("Janela")]
        [Tooltip("Segundos sem abater antes da corrente cair.")]
        [SerializeField, Min(0.5f)] private float window = 3.2f;

        [Header("Multiplicador")]
        [Tooltip("Abates necessarios para subir um degrau.")]
        [SerializeField, Min(1)] private int killsPerStep = 4;

        [Tooltip("Quanto o multiplicador sobe por degrau.")]
        [SerializeField, Min(0.05f)] private float stepSize = 0.5f;

        [Tooltip("Teto do multiplicador.")]
        [SerializeField, Min(1f)] private float maximumMultiplier = 5f;

        private int chain;
        private float remaining;

        public event Action<int, float> Changed;

        public int Chain => chain;

        public float Multiplier => Mathf.Min(
            maximumMultiplier, 1f + (chain / killsPerStep) * stepSize);

        public float WindowFraction => window <= 0f ? 0f : Mathf.Clamp01(remaining / window);

        public bool IsActive => chain > 0;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (chain <= 0)
            {
                return;
            }

            remaining -= GameClock.PlayerDelta;
            if (remaining <= 0f)
            {
                Reset();
            }
        }

        public float RegisterKill()
        {
            chain++;
            remaining = window;
            Changed?.Invoke(chain, Multiplier);
            return Multiplier;
        }

        public void Reset()
        {
            if (chain == 0)
            {
                return;
            }

            chain = 0;
            remaining = 0f;
            Changed?.Invoke(chain, Multiplier);
        }
    }
}
