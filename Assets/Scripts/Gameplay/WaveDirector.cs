using System;
using SpaceShip.Core;
using UnityEngine;

namespace SpaceShip.Gameplay
{
    [DisallowMultipleComponent]
    public class WaveDirector : MonoBehaviour
    {
        public static WaveDirector Instance { get; private set; }

        [Header("Cota")]
        [Tooltip("Abates necessarios na primeira onda.")]
        [SerializeField, Min(1)] private int baseQuota = 8;

        [Tooltip("Quantos abates a cota cresce por onda.")]
        [SerializeField, Min(0)] private int quotaPerWave = 3;

        [Tooltip("Teto da cota, para as ondas tardias nao virarem maratona.")]
        [SerializeField, Min(1)] private int maximumQuota = 32;

        [Header("Chefe")]
        [Tooltip("De quantas em quantas ondas vem um chefe.")]
        [SerializeField, Min(2)] private int bossEveryWaves = 4;

        [Header("Escalonamento por onda")]
        [Tooltip("Quanto a resistencia dos inimigos cresce por onda, como fracao da base. " +
                 "0.18 = +18% por onda, entao na onda 10 um inimigo aguenta ~2,6x mais.")]
        [SerializeField, Range(0f, 1f)] private float healthGrowthPerWave = 0.18f;

        [Tooltip("De quantas em quantas ondas os inimigos ganham +1 de resistencia fixa. " +
                 "Sem isso, um inimigo de 1 de vida demoraria ondas demais para virar 2.")]
        [SerializeField, Min(1)] private int flatHealthEveryWaves = 3;

        [Tooltip("Quanto a velocidade dos inimigos cresce por onda.")]
        [SerializeField, Range(0f, 0.2f)] private float speedGrowthPerWave = 0.04f;

        [Tooltip("Teto do multiplicador de velocidade, para a onda nao virar impossivel de ler.")]
        [SerializeField, Min(1f)] private float maximumSpeedScale = 1.9f;

        [Header("Pressao")]
        [Tooltip("Quanto o intervalo entre inimigos encolhe por onda.")]
        [SerializeField, Range(0f, 0.2f)] private float intervalDecayPerWave = 0.05f;

        [Tooltip("Piso do multiplicador de intervalo, por mais longe que a partida va.")]
        [SerializeField, Range(0.15f, 1f)] private float minimumIntervalScale = 0.4f;

        [Header("Cena")]
        [SerializeField] private EnemySpawner enemySpawner;

        private int wave;
        private int kills;
        private int quota;
        private bool running;
        private bool bossWave;

        public event Action<int, int, int> ProgressChanged;

        public event Action<int> WaveCleared;

        public int Wave => wave;

        public int Kills => kills;

        public int Quota => quota;

        public bool IsBossWave => bossWave;

        public float HealthScaleForWave => 1f + healthGrowthPerWave * Mathf.Max(0, wave - 1);

        public int FlatHealthForWave => flatHealthEveryWaves <= 0
            ? 0
            : Mathf.Max(0, wave - 1) / flatHealthEveryWaves;

        public float SpeedScaleForWave => Mathf.Min(
            maximumSpeedScale, 1f + speedGrowthPerWave * Mathf.Max(0, wave - 1));

        public int ResolveEnemyHealth(int baseHitPoints)
        {
            RunState run = RunState.Instance;
            int fromRun = run != null
                ? run.ResolveEnemyHealth(baseHitPoints)
                : Mathf.Max(1, baseHitPoints);

            return Mathf.Max(1, Mathf.CeilToInt(fromRun * HealthScaleForWave) + FlatHealthForWave);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void BeginRun()
        {
            wave = 0;
            kills = 0;
            quota = 0;
            running = false;
            bossWave = false;
        }

        public void StartNextWave()
        {
            wave++;
            kills = 0;
            running = true;
            bossWave = bossEveryWaves > 0 && wave % bossEveryWaves == 0;

            if (bossWave)
            {
                quota = 0;

                if (GameManager.Instance == null || !GameManager.Instance.SpawnBoss())
                {
                    bossWave = false;
                    StartNormalWave();
                }
                else if (enemySpawner != null)
                {
                    enemySpawner.SetEscortMode(true);
                    enemySpawner.SetIntervalScale(CurrentIntervalScale());
                    enemySpawner.SetRunning(true);
                }
            }
            else
            {
                StartNormalWave();
            }

            ProgressChanged?.Invoke(wave, kills, quota);
        }

        private void StartNormalWave()
        {
            quota = Mathf.Min(maximumQuota, baseQuota + (wave - 1) * quotaPerWave);

            if (enemySpawner != null)
            {
                enemySpawner.SetEscortMode(false);
                enemySpawner.SetIntervalScale(CurrentIntervalScale());
                enemySpawner.SetRunning(true);
            }
        }

        public float CurrentIntervalScale()
        {
            RunState run = RunState.Instance;
            float fromDifficulty = run != null ? run.Settings.SpawnIntervalScale : 1f;
            float fromWave = 1f - (wave - 1) * intervalDecayPerWave;
            return Mathf.Max(minimumIntervalScale, fromDifficulty * fromWave);
        }

        public void SetSpawning(bool value)
        {
            if (!running)
            {
                return;
            }

            enemySpawner?.SetRunning(value);
        }

        public void ReportKill()
        {
            if (!running || bossWave)
            {
                return;
            }

            kills++;
            ProgressChanged?.Invoke(wave, kills, quota);

            if (kills >= quota)
            {
                Finish();
            }
        }

        public void ReportBossDefeated()
        {
            if (!running || !bossWave)
            {
                return;
            }

            Finish();
        }

        private void Finish()
        {
            running = false;
            enemySpawner?.SetRunning(false);
            WaveCleared?.Invoke(wave);
        }
    }
}
