using UnityEngine;

namespace SpaceShip.Presentation
{
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class ThrusterAnimator : MonoBehaviour
    {
        [Tooltip("Quadros na ordem, normalmente Ship01 a Ship04.")]
        [SerializeField] private Sprite[] frames;

        [Tooltip("Quadros por segundo.")]
        [SerializeField, Min(1f)] private float framesPerSecond = 14f;

        private SpriteRenderer spriteRenderer;
        private float timer;
        private int index;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            timer = 0f;
            index = 0;
            ApplyCurrentFrame();
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2)
            {
                return;
            }

            timer += Time.unscaledDeltaTime;
            float step = 1f / framesPerSecond;
            if (timer < step)
            {
                return;
            }

            while (timer >= step)
            {
                timer -= step;
                index = (index + 1) % frames.Length;
            }

            ApplyCurrentFrame();
        }

        private void ApplyCurrentFrame()
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0)
            {
                return;
            }

            Sprite frame = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            if (frame != null)
            {
                spriteRenderer.sprite = frame;
            }
        }
    }
}
