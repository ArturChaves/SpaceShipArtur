using System;
using SpaceShip.Core;
using UnityEngine;

namespace SpaceShip.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class Boss : MonoBehaviour, IDamageable
    {
        private enum Phase
        {
            Entering,

            Fighting
        }

        [Header("Resistencia")]
        [Tooltip("Tiros necessarios para destruir o chefe.")]
        [SerializeField, Min(1)] private int maximumHitPoints = 60;

        [Tooltip("Pontos concedidos ao ser destruido.")]
        [SerializeField, Min(0)] private int scoreValue = 400;

        [Header("Movimento")]
        [Tooltip("Velocidade de entrada, vindo da direita.")]
        [SerializeField, Min(0.1f)] private float entrySpeed = 3.2f;

        [Tooltip("Distancia da borda direita em que o chefe estaciona.")]
        [SerializeField, Min(0f)] private float holdDistanceFromRight = 1.3f;

        [Tooltip("Altura da patrulha vertical, em unidades.")]
        [SerializeField, Min(0f)] private float patrolAmplitude = 1.7f;

        [Tooltip("Frequencia da patrulha, em ciclos por segundo.")]
        [SerializeField, Min(0.01f)] private float patrolFrequency = 0.22f;

        [Header("Armamento")]
        [Tooltip("Prefab do tiro do chefe.")]
        [SerializeField] private Projectile missilePrefab;

        [Tooltip("Segundos entre rajadas com a vida cheia.")]
        [SerializeField, Min(0.2f)] private float fireIntervalHealthy = 2.1f;

        [Tooltip("Segundos entre rajadas com a vida no fim. A luta acelera.")]
        [SerializeField, Min(0.15f)] private float fireIntervalWounded = 0.95f;

        [Tooltip("Quantos projeteis por rajada.")]
        [SerializeField, Min(1)] private int shotsPerVolley = 3;

        [Tooltip("Abertura total do leque da rajada, em graus.")]
        [SerializeField, Min(0f)] private float volleySpread = 34f;

        [Tooltip("Velocidade dos projeteis do chefe.")]
        [SerializeField, Min(0.5f)] private float missileSpeed = 5.5f;

        [Tooltip("Tiro disruptor do chefe: nao mata, aplica efeito negativo. Opcional.")]
        [SerializeField] private Projectile disruptorPrefab;

        [Header("Efeitos")]
        [Tooltip("Explosao mostrada ao morrer.")]
        [SerializeField] private Explosion explosionPrefab;

        [Tooltip("Power-up deixado como recompensa. Opcional.")]
        [SerializeField] private PowerUp rewardPrefab;

        [Tooltip("Dano causado ao colidir com a nave.")]
        [SerializeField, Min(1)] private int ramDamage = 1;

        [Tooltip("Cor do clarao ao levar dano.")]
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.72f, 0.60f, 1f);

        private SpriteRenderer spriteRenderer;
        private Color baseColor = Color.white;
        private float flashTimer;
        private Phase phase = Phase.Entering;
        private int hitPoints;
        private float patrolPhase;
        private float holdX;
        private float centerY;
        private float fireTimer;
        private bool destroyed;

        public event Action<float> HealthChanged;

        public event Action Defeated;

        public Faction Side => Faction.Enemy;

        public float HealthFraction => maximumHitPoints <= 0
            ? 0f
            : Mathf.Clamp01((float)hitPoints / maximumHitPoints);

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }

            WaveDirector waves = WaveDirector.Instance;
            if (waves != null)
            {
                maximumHitPoints = waves.ResolveEnemyHealth(maximumHitPoints);
            }
            else
            {
                RunState run = RunState.Instance;
                if (run != null)
                {
                    maximumHitPoints = Mathf.CeilToInt(maximumHitPoints * run.EnemyHealthScale);
                }
            }

            hitPoints = maximumHitPoints;
            patrolPhase = Mathf.PI * 0.5f;
        }

        private void Start()
        {
            PlayArea area = PlayArea.Instance;
            if (area != null)
            {
                holdX = area.Right - holdDistanceFromRight;
                centerY = area.Bounds.center.y;
            }
            else
            {
                holdX = 3f;
                centerY = 0f;
            }

            fireTimer = fireIntervalHealthy;
            HealthChanged?.Invoke(HealthFraction);
        }

        private void Update()
        {
            if (destroyed)
            {
                return;
            }

            float delta = Time.deltaTime;

            TickDamageFlash();

            if (phase == Phase.Entering)
            {
                TickEntry(delta);
                return;
            }

            TickPatrol(delta);
            TickFire(delta);
        }

        private void TickEntry(float delta)
        {
            Vector3 position = transform.position;
            position.x -= entrySpeed * delta;
            position.y = Mathf.MoveTowards(position.y, centerY, delta * 1.2f);

            if (position.x <= holdX)
            {
                position.x = holdX;
                phase = Phase.Fighting;
            }

            transform.position = position;
        }

        private void TickPatrol(float delta)
        {
            patrolPhase += delta * patrolFrequency * Mathf.PI * 2f;

            Vector3 position = transform.position;
            position.x = holdX;
            position.y = centerY + Mathf.Sin(patrolPhase) * patrolAmplitude;
            transform.position = position;
        }

        private void TickFire(float delta)
        {
            if (missilePrefab == null)
            {
                return;
            }

            fireTimer -= delta;
            if (fireTimer > 0f)
            {
                return;
            }

            fireTimer = Mathf.Lerp(fireIntervalWounded, fireIntervalHealthy, HealthFraction);
            FireVolley();
        }

        private void FireVolley()
        {
            float offset = spriteRenderer != null && spriteRenderer.sprite != null
                ? spriteRenderer.sprite.bounds.extents.x * Mathf.Abs(transform.lossyScale.x)
                : 0.6f;
            Vector3 origin = transform.position + new Vector3(-offset, 0f, 0f);

            RunState run = RunState.Instance;
            int shots = Mathf.Max(1, shotsPerVolley);
            for (int i = 0; i < shots; i++)
            {
                float t = shots == 1 ? 0.5f : i / (float)(shots - 1);
                float angle = 180f + Mathf.Lerp(-volleySpread * 0.5f, volleySpread * 0.5f, t);
                float radians = angle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

                Projectile prefab = missilePrefab;
                if (disruptorPrefab != null && run != null
                    && UnityEngine.Random.value < run.Settings.DisruptorChance)
                {
                    prefab = disruptorPrefab;
                }

                Projectile shot = Pool.Spawn(prefab, origin, Quaternion.identity);
                shot.Launch(Faction.Enemy, direction, missileSpeed);
            }

            SfxLibrary.Instance?.PlayEnemyShot();
        }

        public bool TakeHit(int damage)
        {
            if (destroyed)
            {
                return false;
            }

            hitPoints -= Mathf.Max(1, damage);
            HealthChanged?.Invoke(HealthFraction);

            if (hitPoints > 0)
            {
                FlashDamage();
                SfxLibrary.Instance?.PlayBossHit();
                return false;
            }

            Die();
            return true;
        }

        private void FlashDamage()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = damageFlashColor;
            flashTimer = 0.07f;
        }

        private void TickDamageFlash()
        {
            if (flashTimer <= 0f)
            {
                return;
            }

            flashTimer -= Time.unscaledDeltaTime;
            if (flashTimer <= 0f && spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }
        }

        private void Die()
        {
            destroyed = true;

            if (explosionPrefab != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 at = transform.position + new Vector3(
                        UnityEngine.Random.Range(-0.7f, 0.7f),
                        UnityEngine.Random.Range(-0.5f, 0.5f), 0f);
                    Pool.Spawn(explosionPrefab, at, Quaternion.identity).Play(1.1f);
                }
            }

            if (rewardPrefab != null)
            {
                Instantiate(rewardPrefab, transform.position, Quaternion.identity);
            }

            SfxLibrary.Instance?.PlayPlayerExplosion();
            CameraShake.Instance?.AddTrauma(1f);

            GameManager.Instance?.AwardPoints(scoreValue);

            Defeated?.Invoke();
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (destroyed)
            {
                return;
            }

            PlayerShip player = other.GetComponentInParent<PlayerShip>();
            if (player != null)
            {
                player.TakeHit(ramDamage);
            }
        }
    }
}
