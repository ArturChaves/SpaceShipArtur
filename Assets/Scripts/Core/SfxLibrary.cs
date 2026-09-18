using UnityEngine;

namespace SpaceShip.Core
{
    [DisallowMultipleComponent]
    public class SfxLibrary : MonoBehaviour
    {
        public static SfxLibrary Instance { get; private set; }

        [Header("Nave do jogador")]
        [SerializeField] private AudioClip playerShot;
        [SerializeField] private AudioClip playerExplosion;

        [Header("Inimigos")]
        [SerializeField] private AudioClip enemyShot;
        [SerializeField] private AudioClip enemyHit;
        [SerializeField] private AudioClip enemyExplosion;

        [Header("Chefe")]
        [SerializeField] private AudioClip bossWarning;
        [SerializeField] private AudioClip bossHit;

        [Header("Eventos")]
        [SerializeField] private AudioClip pickup;
        [SerializeField] private AudioClip slowMotionStart;
        [SerializeField] private AudioClip slowMotionEnd;
        [SerializeField] private AudioClip gameOver;

        [Header("Mixagem")]
        [Tooltip("Volume geral aplicado a todos os efeitos.")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;

        [Tooltip("Quantas fontes de audio simultaneas. Mais fontes = menos cortes.")]
        [SerializeField, Range(2, 24)] private int voices = 10;

        [Tooltip("Quanto o tom dos sons do MUNDO cai durante o slow motion. " +
                 "Os sons da nave nao sao afetados, pela mesma razao que ela nao desacelera.")]
        [SerializeField, Range(0.4f, 1f)] private float worldPitchUnderSlowMotion = 0.78f;

        private AudioSource[] sources;
        private int nextVoice;
        private TimeWarp warp;

        private void Awake()
        {
            Instance = this;

            sources = new AudioSource[Mathf.Max(2, voices)];
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;

                source.spatialBlend = 0f;
                sources[i] = source;
            }
        }

        private void Start()
        {
            warp = TimeWarp.Instance;
            if (warp != null)
            {
                warp.ActiveChanged += HandleSlowMotionChanged;
            }
        }

        private void OnDestroy()
        {
            if (warp != null)
            {
                warp.ActiveChanged -= HandleSlowMotionChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private float WorldPitch => warp != null && warp.IsActive
            ? worldPitchUnderSlowMotion
            : 1f;

        public void PlayPlayerShot() { Play(playerShot, 0.45f, 0.07f, 1f); }
        public void PlayPlayerExplosion() { Play(playerExplosion, 1f, 0.03f, 1f); }

        public void PlayEnemyShot() { Play(enemyShot, 0.40f, 0.09f, WorldPitch); }
        public void PlayEnemyHit() { Play(enemyHit, 0.45f, 0.12f, WorldPitch); }
        public void PlayEnemyExplosion() { Play(enemyExplosion, 0.75f, 0.10f, WorldPitch); }

        public void PlayBossWarning() { Play(bossWarning, 0.85f, 0f, 1f); }
        public void PlayBossHit() { Play(bossHit, 0.55f, 0.08f, WorldPitch); }

        public void PlayPickup() { Play(pickup, 0.80f, 0.03f, 1f); }
        public void PlayGameOver() { Play(gameOver, 0.90f, 0f, 1f); }

        private void HandleSlowMotionChanged(bool active)
        {
            Play(active ? slowMotionStart : slowMotionEnd, 0.85f, 0f, 1f);
        }

        private void Play(AudioClip clip, float volume, float pitchJitter, float basePitch)
        {
            if (clip == null || sources == null || sources.Length == 0)
            {
                return;
            }

            AudioSource source = sources[nextVoice];
            nextVoice = (nextVoice + 1) % sources.Length;

            source.pitch = basePitch * (1f + Random.Range(-pitchJitter, pitchJitter));
            source.PlayOneShot(clip, Mathf.Clamp01(volume * masterVolume));
        }
    }
}
