using UnityEngine;

namespace SpaceShip.Core
{
    [DisallowMultipleComponent]
    public class MusicPlayer : MonoBehaviour
    {
        public static MusicPlayer Instance { get; private set; }

        [Header("Trilha")]
        [SerializeField] private AudioClip loop;

        [Tooltip("Volume durante a partida.")]
        [SerializeField, Range(0f, 1f)] private float playingVolume = 0.32f;

        [Tooltip("Volume com o jogo parado (pausa, loja, fim de jogo).")]
        [SerializeField, Range(0f, 1f)] private float idleVolume = 0.16f;

        [Header("Slow motion")]
        [Tooltip("Tom da trilha no auge do efeito.")]
        [SerializeField, Range(0.4f, 1f)] private float slowMotionPitch = 0.82f;

        [Tooltip("Velocidade da transicao de volume e tom.")]
        [SerializeField, Min(0.1f)] private float blendSpeed = 3f;

        private AudioSource source;
        private TimeWarp warp;
        private GameManager game;

        private void Awake()
        {
            Instance = this;

            source = gameObject.AddComponent<AudioSource>();
            source.clip = loop;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = idleVolume;
        }

        private void Start()
        {
            warp = TimeWarp.Instance;
            game = GameManager.Instance;

            if (loop != null)
            {
                source.Play();
            }
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
            if (source == null)
            {
                return;
            }

            bool playing = game != null && game.State == GameState.Playing;
            float targetVolume = playing ? playingVolume : idleVolume;

            float weight = warp != null ? warp.Weight : 0f;
            float targetPitch = Mathf.Lerp(1f, slowMotionPitch, weight);

            float step = Time.unscaledDeltaTime * blendSpeed;
            source.volume = Mathf.MoveTowards(source.volume, targetVolume, step);
            source.pitch = Mathf.MoveTowards(source.pitch, targetPitch, step);
        }
    }
}
