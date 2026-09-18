using UnityEngine;

namespace SpaceShip.Core
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class CameraFit : MonoBehaviour
    {
        [Header("Tamanho de um ladrilho de fundo (unidades de mundo)")]
        [Tooltip("Largura de um sprite de fundo. 1024 px / 100 PPU = 10.24.")]
        [SerializeField, Min(0.01f)] private float backgroundWidth = 10.24f;

        [Tooltip("Altura de um sprite de fundo. 768 px / 100 PPU = 7.68.")]
        [SerializeField, Min(0.01f)] private float backgroundHeight = 7.68f;

        private Camera targetCamera;
        private float lastAspect = -1f;

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            lastAspect = -1f;
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
                if (targetCamera == null)
                {
                    return;
                }
            }

            if (!targetCamera.orthographic)
            {
                return;
            }

            float aspect = targetCamera.aspect;
            if (aspect <= 0f || Mathf.Approximately(aspect, lastAspect))
            {
                return;
            }

            lastAspect = aspect;

            float limitedByWidth = (backgroundWidth * 0.5f) / aspect;

            float limitedByHeight = backgroundHeight * 0.5f;

            targetCamera.orthographicSize = Mathf.Min(limitedByWidth, limitedByHeight);
        }
    }
}
