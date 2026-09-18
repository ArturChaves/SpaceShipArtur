using SpaceShip.Core;
using UnityEngine;

namespace SpaceShip.Gameplay
{
    [DisallowMultipleComponent]
    public class PowerUpSpawner : MonoBehaviour
    {
        [System.Serializable]
        private struct Entry
        {
            [Tooltip("Prefab do power-up.")]
            public PowerUp prefab;

            [Tooltip("Peso relativo no sorteio. Zero = nunca aparece.")]
            [Min(0f)] public float weight;
        }

        [Header("Itens")]
        [Tooltip("Catalogo sorteado por peso. O slow motion tem peso maior porque e " +
                 "o power-up que o exercicio pede; os outros tres sao acrescimos.")]
        [SerializeField] private Entry[] entries;

        [Header("Ritmo")]
        [Tooltip("Menor espera entre dois itens, em segundos.")]
        [SerializeField, Min(1f)] private float minimumInterval = 11f;

        [Tooltip("Maior espera entre dois itens, em segundos.")]
        [SerializeField, Min(1f)] private float maximumInterval = 20f;

        [Tooltip("Espera antes do primeiro item da partida.")]
        [SerializeField, Min(0f)] private float firstDelay = 8f;

        [Header("Posicionamento")]
        [Tooltip("Folga vertical para o item nao nascer colado nas bordas.")]
        [SerializeField, Min(0f)] private float verticalInset = 0.8f;

        private bool running;
        private float timer;

        public void SetRunning(bool value)
        {
            running = value;

            if (value)
            {
                timer = firstDelay;
            }
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            Spawn();
            timer = Random.Range(minimumInterval, Mathf.Max(minimumInterval, maximumInterval));
        }

        private void Spawn()
        {
            PlayArea area = PlayArea.Instance;
            if (area == null)
            {
                return;
            }

            PowerUp prefab = PickPrefab();
            if (prefab == null)
            {
                return;
            }

            Vector3 position = new Vector3(area.SpawnX, area.RandomY(verticalInset), 0f);
            Instantiate(prefab, position, Quaternion.identity);
        }

        private PowerUp PickPrefab()
        {
            if (entries == null || entries.Length == 0)
            {
                return null;
            }

            float total = 0f;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].prefab != null)
                {
                    total += Mathf.Max(0f, entries[i].weight);
                }
            }

            if (total <= 0f)
            {
                return null;
            }

            float roll = Random.Range(0f, total);
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].prefab == null || entries[i].weight <= 0f)
                {
                    continue;
                }

                roll -= entries[i].weight;
                if (roll <= 0f)
                {
                    return entries[i].prefab;
                }
            }

            for (int i = entries.Length - 1; i >= 0; i--)
            {
                if (entries[i].prefab != null && entries[i].weight > 0f)
                {
                    return entries[i].prefab;
                }
            }

            return null;
        }
    }
}
