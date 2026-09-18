using System;
using SpaceShip.Core;
using UnityEngine;

namespace SpaceShip.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class PlayerShip : MonoBehaviour, IDamageable
    {
        [Header("Movimento")]
        [Tooltip("Velocidade em unidades de mundo por segundo.")]
        [SerializeField, Min(0.1f)] private float speed = 6.5f;

        [Tooltip("Segundos para chegar na velocidade cheia. Tira a rigidez do controle.")]
        [SerializeField, Min(0f)] private float acceleration = 14f;

        [Tooltip("Quanto o nariz inclina ao subir ou descer, em graus.")]
        [SerializeField] private float tiltAngle = 14f;

        [Header("Tiro")]
        [Tooltip("Prefab do tiro da nave.")]
        [SerializeField] private Projectile missilePrefab;

        [Tooltip("De onde o tiro sai. Vazio = um pouco a frente do centro.")]
        [SerializeField] private Transform muzzle;

        [Tooltip("Segundos entre dois tiros.")]
        [SerializeField, Min(0.02f)] private float fireCooldown = 0.16f;

        [Tooltip("Velocidade do tiro em unidades por segundo.")]
        [SerializeField, Min(1f)] private float missileSpeed = 14f;

        [Header("Dano")]
        [Tooltip("Segundos de invulnerabilidade ao nascer, para nao morrer no spawn.")]
        [SerializeField, Min(0f)] private float spawnInvulnerability = 1.6f;

        [Tooltip("Explosao mostrada quando a nave e destruida.")]
        [SerializeField] private Explosion explosionPrefab;

        [Header("Power-ups")]
        [Tooltip("Duracao do tiro triplo, em segundos.")]
        [SerializeField, Min(0f)] private float tripleShotDuration = 10f;

        [Tooltip("Abertura do leque do tiro triplo, em graus.")]
        [SerializeField, Min(0f)] private float tripleShotSpread = 12f;

        [Tooltip("Duracao da cadencia acelerada, em segundos.")]
        [SerializeField, Min(0f)] private float rapidFireDuration = 10f;

        [Tooltip("Multiplicador do intervalo entre tiros com a cadencia ativa.")]
        [SerializeField, Range(0.1f, 1f)] private float rapidFireCooldownScale = 0.45f;

        [Tooltip("Bolha desenhada em volta da nave enquanto o escudo estiver de pe.")]
        [SerializeField] private Sprite shieldSprite;

        [Tooltip("Invulnerabilidade concedida ao quebrar o escudo, para nao morrer em seguida.")]
        [SerializeField, Min(0f)] private float shieldBreakInvulnerability = 0.9f;

        [Tooltip("Clarao curto na boca do canhao a cada disparo. Opcional.")]
        [SerializeField] private Explosion muzzleFlashPrefab;

        [Header("Efeitos negativos")]
        [Tooltip("Quanto o efeito Drag reduz da velocidade, de 0 a 1.")]
        [SerializeField, Range(0f, 0.9f)] private float dragSlowFactor = 0.45f;

        private SpriteRenderer spriteRenderer;
        private Vector2 velocity;
        private float cooldownTimer;
        private float invulnerableTimer;
        private bool destroyed;
        private float halfWidth = 0.32f;
        private float halfHeight = 0.15f;

        private float tripleShotTimer;
        private float rapidFireTimer;
        private bool hasShield;
        private SpriteRenderer shieldRenderer;
        private PlayerDebuff debuff = PlayerDebuff.None;
        private float debuffTimer;

        public event Action<PlayerDebuff, float> DebuffChanged;

        public PlayerDebuff ActiveDebuff => debuff;

        public float DebuffRemaining => Mathf.Max(0f, debuffTimer);

        public event Action PowerUpsChanged;

        public bool HasShield => hasShield;

        public float TripleShotRemaining => Mathf.Max(0f, tripleShotTimer);

        public float RapidFireRemaining => Mathf.Max(0f, rapidFireTimer);

        public Faction Side => Faction.Player;

        public bool IsInvulnerable => invulnerableTimer > 0f;

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                Vector3 extents = spriteRenderer.sprite.bounds.extents;
                Vector3 scale = transform.lossyScale;
                halfWidth = extents.x * Mathf.Abs(scale.x);
                halfHeight = extents.y * Mathf.Abs(scale.y);
            }

            invulnerableTimer = spawnInvulnerability;
        }

        private void Update()
        {
            if (destroyed)
            {
                return;
            }

            GameManager game = GameManager.Instance;
            if (game != null && game.State != GameState.Playing)
            {
                return;
            }

            float delta = GameClock.PlayerDelta;

            TickInvulnerability(delta);
            TickPowerUps(delta);
            TickDebuff(delta);
            Move(delta);
            TryFire(delta);
        }

        public void ApplyDebuff(PlayerDebuff kind, float seconds)
        {
            if (destroyed || kind == PlayerDebuff.None || IsInvulnerable)
            {
                return;
            }

            if (hasShield)
            {
                hasShield = false;
                UpdateShieldVisual();
                invulnerableTimer = shieldBreakInvulnerability;
                SfxLibrary.Instance?.PlayEnemyHit();
                PowerUpsChanged?.Invoke();
                return;
            }

            RunState run = RunState.Instance;
            if (kind == PlayerDebuff.Scramble && (run == null || !run.Settings.AllowScramble))
            {
                kind = PlayerDebuff.Jam;
            }

            debuff = kind;
            debuffTimer = Mathf.Max(debuffTimer, seconds);

            SfxLibrary.Instance?.PlayEnemyHit();
            CameraShake.Instance?.AddTrauma(0.25f);
            DebuffChanged?.Invoke(debuff, debuffTimer);
        }

        private void TickDebuff(float delta)
        {
            if (debuff == PlayerDebuff.None)
            {
                return;
            }

            debuffTimer -= delta;
            if (debuffTimer <= 0f)
            {
                debuffTimer = 0f;
                debuff = PlayerDebuff.None;
                DebuffChanged?.Invoke(debuff, 0f);
            }
        }

        public void ApplyPowerUp(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.TripleShot:
                    tripleShotTimer += tripleShotDuration;
                    break;

                case PowerUpKind.RapidFire:
                    rapidFireTimer += rapidFireDuration;
                    break;

                case PowerUpKind.Shield:
                    hasShield = true;
                    UpdateShieldVisual();
                    break;

                case PowerUpKind.SlowMotion:

                    break;
            }

            PowerUpsChanged?.Invoke();
        }

        private void TickPowerUps(float delta)
        {
            bool changed = false;

            if (tripleShotTimer > 0f)
            {
                tripleShotTimer -= delta;
                if (tripleShotTimer <= 0f)
                {
                    tripleShotTimer = 0f;
                    changed = true;
                }
            }

            if (rapidFireTimer > 0f)
            {
                rapidFireTimer -= delta;
                if (rapidFireTimer <= 0f)
                {
                    rapidFireTimer = 0f;
                    changed = true;
                }
            }

            if (changed)
            {
                PowerUpsChanged?.Invoke();
            }

            if (shieldRenderer != null && hasShield)
            {
                float pulse = 0.78f + Mathf.Sin(Time.unscaledTime * 3.4f) * 0.12f;
                Color color = shieldRenderer.color;
                color.a = pulse;
                shieldRenderer.color = color;
            }
        }

        private void UpdateShieldVisual()
        {
            if (shieldRenderer == null)
            {
                if (!hasShield || shieldSprite == null)
                {
                    return;
                }

                GameObject holder = new GameObject("Shield");
                holder.transform.SetParent(transform, false);

                shieldRenderer = holder.AddComponent<SpriteRenderer>();
                shieldRenderer.sprite = shieldSprite;

                shieldRenderer.sortingOrder = 21;
            }

            shieldRenderer.enabled = hasShield;
        }

        private void TickInvulnerability(float delta)
        {
            if (invulnerableTimer <= 0f)
            {
                return;
            }

            invulnerableTimer -= delta;

            if (spriteRenderer == null)
            {
                return;
            }

            bool visible = invulnerableTimer <= 0f
                           || Mathf.Repeat(invulnerableTimer, 0.16f) > 0.08f;
            Color color = spriteRenderer.color;
            color.a = visible ? 1f : 0.3f;
            spriteRenderer.color = color;
        }

        private void Move(float delta)
        {
            Vector2 input = PlayerInputReader.Move;

            if (debuff == PlayerDebuff.Scramble)
            {
                input.x = -input.x;
            }

            float effectiveSpeed = debuff == PlayerDebuff.Drag
                ? speed * (1f - dragSlowFactor)
                : speed;

            Vector2 target = input * effectiveSpeed;
            velocity = acceleration <= 0f
                ? target
                : Vector2.MoveTowards(velocity, target, acceleration * delta);

            Vector3 position = transform.position + (Vector3)(velocity * delta);

            PlayArea area = PlayArea.Instance;
            transform.position = area != null
                ? area.Clamp(position, halfWidth, halfHeight)
                : position;

            float tilt = effectiveSpeed <= 0f ? 0f : (velocity.y / effectiveSpeed) * tiltAngle;
            transform.rotation = Quaternion.Euler(0f, 0f, tilt);
        }

        private void TryFire(float delta)
        {
            cooldownTimer -= delta;

            if (debuff == PlayerDebuff.Jam)
            {
                return;
            }

            if (!PlayerInputReader.FireHeld || cooldownTimer > 0f || missilePrefab == null)
            {
                return;
            }

            RunState run = RunState.Instance;
            float shopScale = run != null ? run.FireRateScale : 1f;
            cooldownTimer = (rapidFireTimer > 0f
                ? fireCooldown * rapidFireCooldownScale
                : fireCooldown) * shopScale;

            Vector3 origin = muzzle != null
                ? muzzle.position
                : transform.position + new Vector3(halfWidth + 0.08f, 0f, 0f);

            int extra = (tripleShotTimer > 0f ? 2 : 0)
                        + (run != null ? run.ExtraProjectiles : 0);
            int barrels = 1 + extra;

            int damage = 1 + (run != null ? run.BonusDamage : 0);

            if (barrels <= 1)
            {
                FireOne(origin, 0f, damage);
            }
            else
            {
                float half = tripleShotSpread * (barrels - 1) * 0.5f;
                for (int i = 0; i < barrels; i++)
                {
                    FireOne(origin, half - tripleShotSpread * i, damage);
                }
            }

            if (muzzleFlashPrefab != null)
            {
                Pool.Spawn(muzzleFlashPrefab, origin, Quaternion.identity).Play(0.09f);
            }

            SfxLibrary.Instance?.PlayPlayerShot();
        }

        private void FireOne(Vector3 origin, float angleDegrees, int damage)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            Projectile shot = Pool.Spawn(missilePrefab, origin, Quaternion.identity);
            shot.Launch(Faction.Player, direction, missileSpeed, damage);
        }

        public bool TakeHit(int damage)
        {
            if (destroyed || IsInvulnerable)
            {
                return false;
            }

            if (hasShield)
            {
                hasShield = false;
                UpdateShieldVisual();
                invulnerableTimer = shieldBreakInvulnerability;

                SfxLibrary.Instance?.PlayEnemyHit();
                CameraShake.Instance?.AddTrauma(0.35f);
                PowerUpsChanged?.Invoke();
                return false;
            }

            destroyed = true;
            ComboTracker.Instance?.Reset();

            if (explosionPrefab != null)
            {
                Pool.Spawn(explosionPrefab, transform.position, Quaternion.identity).Play(0.9f);
            }

            SfxLibrary.Instance?.PlayPlayerExplosion();
            CameraShake.Instance?.AddTrauma(0.8f);
            GameManager.Instance?.ReportPlayerDestroyed();
            Destroy(gameObject);
            return true;
        }
    }
}
