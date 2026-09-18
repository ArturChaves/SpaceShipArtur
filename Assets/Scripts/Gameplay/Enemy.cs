using SpaceShip.Core;
using UnityEngine;

namespace SpaceShip.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class Enemy : MonoBehaviour, IDamageable
    {
        [Header("Identidade")]
        [Tooltip("Define pontuacao e padrao de movimento.")]
        [SerializeField] private EnemyKind kind = EnemyKind.Drone;

        [Tooltip("Tiros necessarios para destruir.")]
        [SerializeField, Min(1)] private int hitPoints = 1;

        [Header("Movimento")]
        [Tooltip("Velocidade horizontal em unidades por segundo (sempre para a esquerda).")]
        [SerializeField, Min(0.1f)] private float speed = 3.2f;

        [Tooltip("Altura da onda, para o tipo Stinger. Em unidades de mundo.")]
        [SerializeField, Min(0f)] private float waveAmplitude = 1.1f;

        [Tooltip("Frequencia da onda, para o tipo Stinger. Em ciclos por segundo.")]
        [SerializeField, Min(0f)] private float waveFrequency = 1.3f;

        [Header("Sentinela")]
        [Tooltip("Distancia da borda direita em que a sentinela estaciona. " +
                 "Sorteada na faixa para duas nao pararem uma em cima da outra.")]
        [SerializeField] private Vector2 sentryHoldInset = new Vector2(0.6f, 2.8f);

        [Tooltip("Segundos parada atirando antes de voltar a avancar.\n\n" +
                 "Ela nao fica para sempre de proposito: numa luta de chefe, torres " +
                 "imortais acumulariam ate a tela virar impossivel de ler.")]
        [SerializeField, Min(0.5f)] private float sentryHoldSeconds = 7f;

        [Tooltip("Altura do balanco enquanto esta parada, so para nao parecer congelada.")]
        [SerializeField, Min(0f)] private float sentryBobAmplitude = 0.22f;

        [Header("Tiro")]
        [Tooltip("Prefab do tiro inimigo. Vazio = este tipo nao atira.")]
        [SerializeField] private Projectile missilePrefab;

        [Tooltip("Segundos entre tiros.")]
        [SerializeField, Min(0.2f)] private float fireInterval = 2.2f;

        [Tooltip("Velocidade do tiro inimigo.")]
        [SerializeField, Min(0.5f)] private float missileSpeed = 6f;

        [Tooltip("Tiro disruptor: nao mata, aplica um efeito negativo na nave. " +
                 "Vazio = este inimigo nunca dispara disruptor.")]
        [SerializeField] private Projectile disruptorPrefab;

        [Header("Efeito")]
        [Tooltip("Explosao ao ser destruido.")]
        [SerializeField] private Explosion explosionPrefab;

        [Tooltip("Dano que causa ao colidir com a nave do jogador.")]
        [SerializeField, Min(1)] private int ramDamage = 1;

        [Tooltip("Quanto tremor a morte deste inimigo provoca na camera, de 0 a 1.")]
        [SerializeField, Range(0f, 1f)] private float deathTrauma = 0.2f;

        [Tooltip("Cor do clarao ao levar um tiro sem morrer.")]
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.55f, 0.45f, 1f);

        [Tooltip("Duracao do clarao de dano, em segundos reais.")]
        [SerializeField, Min(0.01f)] private float damageFlashDuration = 0.09f;

        private SpriteRenderer spriteRenderer;
        private Color baseColor = Color.white;
        private float flashTimer;
        private float baseY;
        private float wavePhase;
        private float fireTimer;
        private bool destroyed;
        private float sentryHoldX;
        private float sentryTimer;
        private bool sentrySettled;

        public Faction Side => Faction.Enemy;

        public EnemyKind Kind => kind;

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }

            RunState run = RunState.Instance;
            if (run != null)
            {
                speed *= run.EnemySpeedScale;
                fireInterval *= run.EnemyFireIntervalScale;
            }

            WaveDirector waves = WaveDirector.Instance;
            if (waves != null)
            {
                speed *= waves.SpeedScaleForWave;
                hitPoints = waves.ResolveEnemyHealth(hitPoints);
            }
            else if (run != null)
            {
                hitPoints = run.ResolveEnemyHealth(hitPoints);
            }

            baseY = transform.position.y;

            wavePhase = Random.Range(0f, Mathf.PI * 2f);
            sentryTimer = sentryHoldSeconds;

            fireTimer = Random.Range(fireInterval * 0.35f, fireInterval);
        }

        private void Start()
        {
            PlayArea area = PlayArea.Instance;
            sentryHoldX = area != null
                ? area.Right - Random.Range(sentryHoldInset.x, sentryHoldInset.y)
                : 2f;
        }

        public void SetBaseline(float y)
        {
            baseY = y;
            Vector3 position = transform.position;
            position.y = y;
            transform.position = position;
        }

        private void Update()
        {
            if (destroyed)
            {
                return;
            }

            float delta = Time.deltaTime;

            TickDamageFlash();
            MoveStep(delta);
            TryFire(delta);
            DespawnIfPastLeftEdge();
        }

        private void MoveStep(float delta)
        {
            Vector3 position = transform.position;

            switch (kind)
            {
                case EnemyKind.Stinger:

                    position.x -= speed * delta;
                    wavePhase += delta * waveFrequency * Mathf.PI * 2f;
                    position.y = baseY + Mathf.Sin(wavePhase) * waveAmplitude;
                    break;

                case EnemyKind.Hulk:

                    position.x -= speed * delta;
                    position.y = Mathf.MoveTowards(position.y, TrackedPlayerY(), delta * 0.7f);
                    break;

                case EnemyKind.Sentry:
                    position = SentryStep(position, delta);
                    break;

                case EnemyKind.Drone:
                case EnemyKind.Racer:
                default:

                    position.x -= speed * delta;
                    break;
            }

            transform.position = position;
        }

        private Vector3 SentryStep(Vector3 position, float delta)
        {
            if (!sentrySettled)
            {
                position.x -= speed * delta;
                if (position.x <= sentryHoldX)
                {
                    position.x = sentryHoldX;
                    sentrySettled = true;
                }

                return position;
            }

            sentryTimer -= delta;
            if (sentryTimer <= 0f)
            {
                position.x -= speed * delta;
            }

            wavePhase += delta * 0.6f * Mathf.PI * 2f;
            position.y = baseY + Mathf.Sin(wavePhase) * sentryBobAmplitude;
            return position;
        }

        private float TrackedPlayerY()
        {
            GameManager game = GameManager.Instance;
            PlayerShip player = game != null ? game.ActivePlayer : null;
            return player != null ? player.transform.position.y : baseY;
        }

        private void TryFire(float delta)
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

            fireTimer = fireInterval;

            PlayArea area = PlayArea.Instance;
            if (area != null && transform.position.x > area.Right)
            {
                return;
            }

            float offset = spriteRenderer != null && spriteRenderer.sprite != null
                ? spriteRenderer.sprite.bounds.extents.x * Mathf.Abs(transform.lossyScale.x) + 0.08f
                : 0.3f;

            Vector3 origin = transform.position + new Vector3(-offset, 0f, 0f);

            Projectile prefab = PickMissilePrefab();
            Projectile shot = Pool.Spawn(prefab, origin, Quaternion.identity);
            shot.Launch(Faction.Enemy, Vector2.left, missileSpeed);

            SfxLibrary.Instance?.PlayEnemyShot();
        }

        private Projectile PickMissilePrefab()
        {
            RunState run = RunState.Instance;
            if (disruptorPrefab == null || run == null)
            {
                return missilePrefab;
            }

            return Random.value < run.Settings.DisruptorChance ? disruptorPrefab : missilePrefab;
        }

        private void DespawnIfPastLeftEdge()
        {
            PlayArea area = PlayArea.Instance;
            if (area == null)
            {
                return;
            }

            if (transform.position.x < area.DespawnLeftX)
            {
                Destroy(gameObject);
            }
        }

        public bool TakeHit(int damage)
        {
            if (destroyed)
            {
                return false;
            }

            hitPoints -= Mathf.Max(1, damage);

            if (hitPoints > 0)
            {
                FlashDamage();
                SfxLibrary.Instance?.PlayEnemyHit();
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
            flashTimer = damageFlashDuration;
        }

        private void TickDamageFlash()
        {
            if (flashTimer <= 0f)
            {
                return;
            }

            flashTimer -= GameClock.PlayerDelta;
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
                Pool.Spawn(explosionPrefab, transform.position, Quaternion.identity).Play(0.6f);
            }

            SfxLibrary.Instance?.PlayEnemyExplosion();
            CameraShake.Instance?.AddTrauma(deathTrauma);

            GameManager.Instance?.ReportEnemyDestroyed(kind);
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (destroyed)
            {
                return;
            }

            PlayerShip player = other.GetComponentInParent<PlayerShip>();
            if (player == null)
            {
                return;
            }

            if (player.TakeHit(ramDamage))
            {
                Die();
            }
        }
    }
}
