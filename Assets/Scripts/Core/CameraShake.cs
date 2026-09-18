using UnityEngine;

namespace SpaceShip.Core
{
    [DisallowMultipleComponent]
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Header("Intensidade")]
        [Tooltip("Deslocamento maximo, em unidades de mundo, com o trauma no talo.")]
        [SerializeField, Min(0f)] private float maximumOffset = 0.32f;

        [Tooltip("Rotacao maxima, em graus, com o trauma no talo.")]
        [SerializeField, Min(0f)] private float maximumRoll = 1.6f;

        [Tooltip("Quanto trauma se perde por segundo.")]
        [SerializeField, Min(0.1f)] private float decayPerSecond = 2.2f;

        [Tooltip("Velocidade da leitura do ruido. Maior = tremor mais nervoso.")]
        [SerializeField, Min(1f)] private float frequency = 24f;

        private Vector3 restPosition;
        private Quaternion restRotation;
        private float trauma;
        private float seed;

        public Vector3 RestPosition => restPosition;

        private void Awake()
        {
            Instance = this;
            restPosition = transform.position;
            restRotation = transform.rotation;
            seed = Random.Range(0f, 100f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + Mathf.Max(0f, amount));
        }

        private void LateUpdate()
        {
            if (trauma <= 0f)
            {
                return;
            }

            trauma = Mathf.Max(0f, trauma - decayPerSecond * Time.unscaledDeltaTime);

            float shake = trauma * trauma;
            float t = Time.unscaledTime * frequency;

            float offsetX = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f;
            float offsetY = (Mathf.PerlinNoise(seed + 17f, t) - 0.5f) * 2f;
            float roll = (Mathf.PerlinNoise(seed + 43f, t) - 0.5f) * 2f;

            transform.position = restPosition
                                 + new Vector3(offsetX, offsetY, 0f) * (maximumOffset * shake);
            transform.rotation = restRotation
                                 * Quaternion.Euler(0f, 0f, roll * maximumRoll * shake);

            if (trauma <= 0f)
            {
                transform.position = restPosition;
                transform.rotation = restRotation;
            }
        }
    }
}
