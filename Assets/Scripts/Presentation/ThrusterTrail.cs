using SpaceShip.Core;
using UnityEngine;

namespace SpaceShip.Presentation
{
    [DisallowMultipleComponent]
    public class ThrusterTrail : MonoBehaviour
    {
        [Tooltip("Faisca deixada para tras. Uma Explosion curta, que encolhe em vez de crescer.")]
        [SerializeField] private Gameplay.Explosion sparkPrefab;

        [Tooltip("Segundos entre faiscas.")]
        [SerializeField, Min(0.01f)] private float interval = 0.045f;

        [Tooltip("Quanto tempo cada faisca dura.")]
        [SerializeField, Min(0.05f)] private float sparkLife = 0.3f;

        [Tooltip("Deslocamento da faisca em relacao ao centro da nave, em unidades locais.")]
        [SerializeField] private Vector2 offset = new Vector2(-0.34f, 0f);

        private float timer;

        private void Update()
        {
            if (sparkPrefab == null)
            {
                return;
            }

            timer -= GameClock.PlayerDelta;
            if (timer > 0f)
            {
                return;
            }

            timer = interval;

            float scale = Mathf.Abs(transform.lossyScale.x);
            Vector3 local = new Vector3(offset.x * scale, offset.y * scale, 0f);
            Vector3 position = transform.position + transform.rotation * local;

            Pool.Spawn(sparkPrefab, position, Quaternion.identity).Play(sparkLife);
        }
    }
}
