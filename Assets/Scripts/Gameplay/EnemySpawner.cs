using SpaceShip.Core;
using UnityEngine;

namespace SpaceShip.Gameplay
{
    [DisallowMultipleComponent]
    public class EnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        private struct Entry
        {
            [Tooltip("Prefab da nave inimiga.")]
            public Enemy prefab;

            [Tooltip("Peso relativo no sorteio. Zero = nao aparece.")]
            [Min(0f)] public float weight;

            [Tooltip("Pontuacao minima para este tipo comecar a aparecer.")]
            [Min(0)] public int unlockScore;
        }

        [Header("Catalogo")]
        [SerializeField] private Entry[] entries;

        [Tooltip("Catalogo usado nas ondas de chefe. Sao as escoltas: sentinelas que " +
                 "param atirando e corredores rapidos.\n\n" +
                 "Antes a onda de chefe nao tinha inimigo nenhum, o que deixava as " +
                 "sabotagens compradas na loja sem efeito justamente na luta mais dura.")]
        [SerializeField] private Entry[] escortEntries;

        [Tooltip("Multiplicador do intervalo no modo escolta. Maior que 1 = escoltas " +
                 "mais espacadas, para o chefe continuar sendo a atracao principal.")]
        [SerializeField, Min(0.2f)] private float escortIntervalScale = 1.8f;

        [Header("Ritmo")]
        [Tooltip("Segundos entre naves no comeco da partida.")]
        [SerializeField, Min(0.1f)] private float startingInterval = 1.5f;

        [Tooltip("Menor intervalo possivel, por mais longa que a partida fique.")]
        [SerializeField, Min(0.05f)] private float minimumInterval = 0.45f;

        [Tooltip("Quanto o intervalo encolhe por segundo de partida.")]
        [SerializeField, Min(0f)] private float intervalDecayPerSecond = 0.012f;

        [Tooltip("Variacao aleatoria aplicada ao intervalo, para o ritmo nao ficar mecanico.")]
        [SerializeField, Range(0f, 0.9f)] private float intervalJitter = 0.3f;

        [Header("Formacoes")]
        [Tooltip("Chance de nascer uma fila de naves iguais em vez de uma sozinha.")]
        [SerializeField, Range(0f, 1f)] private float squadChance = 0.22f;

        [Tooltip("Quantas naves saem em uma fila.")]
        [SerializeField, Min(2)] private int squadSize = 3;

        [Tooltip("Espacamento horizontal entre as naves de uma fila.")]
        [SerializeField, Min(0.2f)] private float squadSpacing = 1.1f;

        [Header("Posicionamento")]
        [Tooltip("Folga vertical para a nave nao nascer colada no topo ou na base.")]
        [SerializeField, Min(0f)] private float verticalInset = 0.6f;

        private bool running;
        private float timer;
        private float elapsed;
        private float intervalScale = 1f;
        private bool escortMode;

        public void SetEscortMode(bool value)
        {
            escortMode = value;
        }

        public void SetIntervalScale(float value)
        {
            intervalScale = Mathf.Max(0.05f, value);
        }

        public void SetRunning(bool value)
        {
            running = value;

            if (value)
            {
                elapsed = 0f;
                timer = startingInterval;
            }
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            float delta = Time.deltaTime;

            elapsed += delta;
            timer -= delta;

            if (timer > 0f)
            {
                return;
            }

            SpawnWave();

            float escortScale = escortMode ? escortIntervalScale : 1f;
            float interval = Mathf.Max(minimumInterval * intervalScale,
                                       (startingInterval - elapsed * intervalDecayPerSecond)
                                       * intervalScale) * escortScale;
            timer = interval * Random.Range(1f - intervalJitter, 1f + intervalJitter);
        }

        private void SpawnWave()
        {
            Enemy prefab = PickPrefab();
            if (prefab == null)
            {
                return;
            }

            PlayArea area = PlayArea.Instance;
            if (area == null)
            {
                return;
            }

            float y = area.RandomY(verticalInset);
            float x = area.SpawnX;

            bool marches = prefab.Kind == EnemyKind.Drone || prefab.Kind == EnemyKind.Racer;
            bool squad = marches && Random.value < squadChance;
            int count = squad ? squadSize : 1;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = new Vector3(x + i * squadSpacing, y, 0f);
                Enemy enemy = Instantiate(prefab, position, Quaternion.identity);
                enemy.SetBaseline(y);
            }
        }

        private Enemy PickPrefab()
        {
            Entry[] catalog = escortMode && escortEntries != null && escortEntries.Length > 0
                ? escortEntries
                : entries;

            if (catalog == null || catalog.Length == 0)
            {
                return null;
            }

            return PickFrom(catalog);
        }

        private Enemy PickFrom(Entry[] entries)
        {
            int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;

            float total = 0f;
            for (int i = 0; i < entries.Length; i++)
            {
                if (IsAvailable(entries[i], score))
                {
                    total += entries[i].weight;
                }
            }

            if (total <= 0f)
            {
                return null;
            }

            float roll = Random.Range(0f, total);
            for (int i = 0; i < entries.Length; i++)
            {
                if (!IsAvailable(entries[i], score))
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
                if (IsAvailable(entries[i], score))
                {
                    return entries[i].prefab;
                }
            }

            return null;
        }

        private static bool IsAvailable(Entry entry, int score)
        {
            return entry.prefab != null && entry.weight > 0f && score >= entry.unlockScore;
        }
    }
}
