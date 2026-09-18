using SpaceShip.Core;
using UnityEngine;

namespace SpaceShip.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class PowerUp : MonoBehaviour
    {
        [Header("Identidade")]
        [Tooltip("O que este item concede ao ser coletado.")]
        [SerializeField] private PowerUpKind kind = PowerUpKind.SlowMotion;

        [Header("Movimento")]
        [Tooltip("Velocidade com que deriva para a esquerda.")]
        [SerializeField, Min(0f)] private float driftSpeed = 1.8f;

        [Tooltip("Altura do bobbing vertical.")]
        [SerializeField, Min(0f)] private float bobAmplitude = 0.25f;

        [Tooltip("Frequencia do bobbing, em ciclos por segundo.")]
        [SerializeField, Min(0f)] private float bobFrequency = 0.8f;

        [Tooltip("Graus por segundo de rotacao, so para chamar atencao.")]
        [SerializeField] private float spin = 90f;

        [Header("Efeito")]
        [Tooltip("Segundos de slow motion concedidos. 0 = usa a duracao padrao do TimeWarp.")]
        [SerializeField, Min(0f)] private float slowMotionSeconds = 0f;

        [Tooltip("Pontos ganhos ao coletar.")]
        [SerializeField, Min(0)] private int pickupScore = 15;

        [Tooltip("Efeito visual mostrado ao coletar.")]
        [SerializeField] private Explosion pickupEffect;

        private float baseY;
        private float phase;
        private bool collected;

        private void Awake()
        {
            baseY = transform.position.y;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            float delta = Time.deltaTime;

            phase += delta * bobFrequency * Mathf.PI * 2f;

            Vector3 position = transform.position;
            position.x -= driftSpeed * delta;
            position.y = baseY + Mathf.Sin(phase) * bobAmplitude;
            transform.position = position;

            transform.Rotate(0f, 0f, spin * delta);

            PlayArea area = PlayArea.Instance;
            if (area != null && position.x < area.DespawnLeftX)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected)
            {
                return;
            }

            PlayerShip player = other.GetComponentInParent<PlayerShip>();
            if (player == null)
            {
                return;
            }

            collected = true;

            SfxLibrary.Instance?.PlayPickup();

            GameManager manager = GameManager.Instance;
            manager?.AwardBonus(pickupScore);

            if (kind == PowerUpKind.SlowMotion)
            {
                if (slowMotionSeconds > 0f)
                {
                    TimeWarp.Instance?.Activate(slowMotionSeconds);
                    manager?.Announce("SLOW MOTION");
                }
                else
                {
                    manager?.TriggerSlowMotion("SLOW MOTION");
                }
            }
            else
            {
                player.ApplyPowerUp(kind);
                manager?.Announce(LabelFor(kind));
            }

            if (pickupEffect != null)
            {
                Pool.Spawn(pickupEffect, transform.position, Quaternion.identity).Play(0.5f);
            }

            Destroy(gameObject);
        }

        private static string LabelFor(PowerUpKind value)
        {
            switch (value)
            {
                case PowerUpKind.TripleShot:
                    return "TIRO TRIPLO";
                case PowerUpKind.RapidFire:
                    return "CADENCIA MAXIMA";
                case PowerUpKind.Shield:
                    return "ESCUDO";
                default:
                    return "SLOW MOTION";
            }
        }
    }
}
