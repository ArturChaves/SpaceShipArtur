using UnityEngine;

namespace SpaceShip.Core
{
    [DisallowMultipleComponent]
    public class PlayArea : MonoBehaviour
    {
        public static PlayArea Instance { get; private set; }

        [Header("Camera de referencia")]
        [Tooltip("Camera ortografica que define a area jogavel. Vazio = Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Header("Margens (unidades de mundo)")]
        [Tooltip("Folga nas laterais para a nave nao encostar exatamente na borda.")]
        [SerializeField, Min(0f)] private float horizontalPadding = 0.45f;

        [Tooltip("Folga no topo, reservada para o HUD.")]
        [SerializeField, Min(0f)] private float topPadding = 0.85f;

        [Tooltip("Folga na base da tela.")]
        [SerializeField, Min(0f)] private float bottomPadding = 0.45f;

        [Header("Fora da tela")]
        [Tooltip("Distancia alem da borda direita em que os inimigos nascem.")]
        [SerializeField, Min(0f)] private float offscreenMargin = 1.5f;

        [Tooltip("Ate onde o tiro do jogador vive, alem da borda direita.\n\n" +
                 "TEM que ser bem menor que a margem de spawn. Quando os dois valores " +
                 "coincidem, os tiros ficam vivos dentro da zona de nascimento e matam " +
                 "o inimigo no instante em que ele nasce - fora da tela.")]
        [SerializeField, Min(0f)] private float shotRangeMargin = 0.3f;

        private Rect cachedBounds;
        private float cachedAspect = -1f;
        private float cachedSize = -1f;
        private Vector3 cachedCameraPosition = Vector3.positiveInfinity;

        public Rect Bounds
        {
            get
            {
                RefreshIfNeeded();
                return cachedBounds;
            }
        }

        public float Left => Bounds.xMin;
        public float Right => Bounds.xMax;
        public float Top => Bounds.yMax;
        public float Bottom => Bounds.yMin;

        public float SpawnX => Right + offscreenMargin;

        public float DespawnLeftX => Left - offscreenMargin;

        public float DespawnRightX => Right + Mathf.Min(shotRangeMargin, offscreenMargin * 0.5f);

        private void Awake()
        {
            Instance = this;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public float RandomY(float verticalInset = 0f)
        {
            Rect rect = Bounds;
            float min = rect.yMin + verticalInset;
            float max = rect.yMax - verticalInset;
            if (min > max)
            {
                return rect.center.y;
            }

            return Random.Range(min, max);
        }

        public Vector3 Clamp(Vector3 position, float halfWidth = 0f, float halfHeight = 0f)
        {
            Rect rect = Bounds;
            position.x = Mathf.Clamp(position.x, rect.xMin + halfWidth, rect.xMax - halfWidth);
            position.y = Mathf.Clamp(position.y, rect.yMin + halfHeight, rect.yMax - halfHeight);
            return position;
        }

        private void RefreshIfNeeded()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    return;
                }
            }

            float aspect = targetCamera.aspect;
            float size = targetCamera.orthographicSize;

            CameraShake shake = CameraShake.Instance;
            Vector3 cameraPosition = shake != null
                ? shake.RestPosition
                : targetCamera.transform.position;

            bool unchanged = Mathf.Approximately(aspect, cachedAspect)
                             && Mathf.Approximately(size, cachedSize)
                             && cameraPosition == cachedCameraPosition;
            if (unchanged)
            {
                return;
            }

            cachedAspect = aspect;
            cachedSize = size;
            cachedCameraPosition = cameraPosition;

            float halfHeight = size;
            float halfWidth = size * aspect;

            float xMin = cameraPosition.x - halfWidth + horizontalPadding;
            float xMax = cameraPosition.x + halfWidth - horizontalPadding;
            float yMin = cameraPosition.y - halfHeight + bottomPadding;
            float yMax = cameraPosition.y + halfHeight - topPadding;

            cachedBounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
