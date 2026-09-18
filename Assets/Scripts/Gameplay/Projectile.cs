using SpaceShip.Core;
using UnityEngine;

namespace SpaceShip.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour, IPooled
    {
        [Header("Balistica")]
        [Tooltip("Velocidade em unidades de mundo por segundo.")]
        [SerializeField, Min(0.1f)] private float speed = 12f;

        [Tooltip("Dano aplicado ao alvo atingido.")]
        [SerializeField, Min(1)] private int damage = 1;

        [Tooltip("Segundos de vida maxima, para o tiro nunca vazar caso escape da area.")]
        [SerializeField, Min(0.1f)] private float maximumLifetime = 6f;

        [Header("Efeito")]
        [Tooltip("Explosao pequena mostrada ao acertar. Opcional.")]
        [SerializeField] private Explosion impactEffect;

        [Tooltip("Se diferente de None, este projetil NAO mata a nave: aplica este " +
                 "efeito negativo por alguns segundos. E o tiro disruptor.")]
        [SerializeField] private PlayerDebuff debuffOnHit = PlayerDebuff.None;

        [Tooltip("Duracao do efeito negativo, em segundos.")]
        [SerializeField, Min(0.1f)] private float debuffSeconds = 2f;

        [Tooltip("Sorteia o efeito a cada disparo em vez de usar sempre o mesmo. " +
                 "Um prefab so atende aos tres efeitos.")]
        [SerializeField] private bool randomizeDebuff;

        private Faction owner = Faction.Player;
        private Vector2 direction = Vector2.right;
        private float age;

        private int baseDamage;
        private float baseSpeed;
        private bool baselineCaptured;

        private void Awake()
        {
            CaptureBaseline();
        }

        private void CaptureBaseline()
        {
            if (baselineCaptured)
            {
                return;
            }

            baseDamage = damage;
            baseSpeed = speed;
            baselineCaptured = true;
        }

        public void OnSpawned()
        {
            CaptureBaseline();

            age = 0f;
            damage = baseDamage;
            speed = baseSpeed;
            owner = Faction.Player;
            direction = Vector2.right;

            if (randomizeDebuff && debuffOnHit != PlayerDebuff.None)
            {
                int roll = Random.Range(0, 3);
                debuffOnHit = roll == 0 ? PlayerDebuff.Jam
                    : roll == 1 ? PlayerDebuff.Drag
                    : PlayerDebuff.Scramble;
            }
        }

        public Faction Owner => owner;

        public void Launch(Faction shooter, Vector2 travelDirection, float travelSpeed = -1f,
                           int damageOverride = -1)
        {
            owner = shooter;

            if (damageOverride > 0)
            {
                damage = damageOverride;
            }
            direction = travelDirection.sqrMagnitude < 0.0001f
                ? Vector2.right
                : travelDirection.normalized;

            if (travelSpeed > 0f)
            {
                speed = travelSpeed;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private float Delta => owner == Faction.Player
            ? GameClock.PlayerDelta
            : GameClock.WorldDelta;

        private void Update()
        {
            float delta = Delta;

            age += delta;
            if (age >= maximumLifetime)
            {
                Pool.Release(gameObject);
                return;
            }

            transform.position += (Vector3)(direction * (speed * delta));

            if (IsOutsidePlayArea())
            {
                Pool.Release(gameObject);
            }
        }

        private bool IsOutsidePlayArea()
        {
            PlayArea area = PlayArea.Instance;
            if (area == null)
            {
                return false;
            }

            Vector3 position = transform.position;
            return position.x < area.DespawnLeftX
                   || position.x > area.DespawnRightX
                   || position.y < area.Bottom - 2f
                   || position.y > area.Top + 2f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target == null || target.Side == owner)
            {
                return;
            }

            if (debuffOnHit != PlayerDebuff.None && target is PlayerShip ship)
            {
                ship.ApplyDebuff(debuffOnHit, debuffSeconds);
            }
            else
            {
                target.TakeHit(damage);
            }

            if (impactEffect != null)
            {
                Pool.Spawn(impactEffect, transform.position, Quaternion.identity).Play(0.45f);
            }

            Pool.Release(gameObject);
        }
    }
}
