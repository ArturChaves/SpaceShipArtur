using SpaceShip.Core;
using UnityEngine;

namespace SpaceShip.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class Explosion : MonoBehaviour, IPooled
    {
        [Header("Animacao")]
        [Tooltip("Duracao padrao em segundos, se Play() for chamado sem argumento.")]
        [SerializeField, Min(0.05f)] private float defaultDuration = 0.7f;

        [Tooltip("Escala inicial, como fracao da escala final.")]
        [SerializeField, Min(0f)] private float startScale = 0.35f;

        [Tooltip("Quanto a explosao cresce alem da escala do objeto.")]
        [SerializeField, Min(0.1f)] private float growth = 1.6f;

        [Tooltip("Graus por segundo, para dois estouros seguidos nao ficarem identicos.")]
        [SerializeField] private float spin = 45f;

        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private float duration;
        private float elapsed;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            baseScale = transform.localScale;
            duration = defaultDuration;
        }

        public void OnSpawned()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            elapsed = 0f;
            duration = defaultDuration;
            transform.localScale = baseScale * startScale;
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }

        public void Play(float seconds = -1f)
        {
            duration = seconds > 0f ? seconds : defaultDuration;
            elapsed = 0f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

            float scale = Mathf.Lerp(startScale, growth, t);
            transform.localScale = baseScale * scale;
            transform.Rotate(0f, 0f, spin * Time.deltaTime);

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;

                color.a = 1f - (t * t);
                spriteRenderer.color = color;
            }

            if (t >= 1f)
            {
                Pool.Release(gameObject);
            }
        }
    }
}
