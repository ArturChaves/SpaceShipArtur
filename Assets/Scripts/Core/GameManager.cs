using System;
using SpaceShip.Gameplay;
using UnityEngine;

namespace SpaceShip.Core
{
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private const string HighScoreKeyPrefix = "SpaceShip.HighScore.";

        [Header("Regras")]
        [Tooltip("Segundos de pausa entre a nave explodir e voltar a jogar.")]
        [SerializeField, Min(0f)] private float respawnDelay = 1.4f;

        [Header("Slow motion por pontuacao")]
        [Tooltip("A cada tantos pontos o slow motion dispara sozinho. " +
                 "Este e o gatilho 'quando uma determinada pontuacao for alcancada' do exercicio.")]
        [SerializeField, Min(1)] private int slowMotionScoreInterval = 500;

        [Header("Cena")]
        [Tooltip("Prefab da nave do jogador.")]
        [SerializeField] private PlayerShip playerPrefab;

        [Tooltip("Onde a nave nasce. Vazio = a esquerda da area jogavel.")]
        [SerializeField] private Transform playerSpawnPoint;

        [Tooltip("Prefab do chefe. Vazio = as ondas de chefe viram ondas comuns.")]
        [SerializeField] private Boss bossPrefab;

        [Tooltip("Quem cria os power-ups.")]
        [SerializeField] private PowerUpSpawner powerUpSpawner;

        [Tooltip("Quem organiza as ondas.")]
        [SerializeField] private WaveDirector waveDirector;

        private int score;
        private int lives;
        private int nextSlowMotionScore;
        private int appliedBonusLives;
        private float respawnTimer;
        private PlayerShip activePlayer;
        private Boss activeBoss;
        private RunState run;

        public event Action<int> ScoreChanged;
        public event Action<int> LivesChanged;
        public event Action<GameState> StateChanged;

        public event Action<string> Announced;

        public event Action<Boss> BossSpawned;

        public event Action<PlayerShip> PlayerSpawned;

        public int Score => score;
        public int Lives => lives;
        public int HighScore { get; private set; }
        public GameState State { get; private set; } = GameState.Ready;

        public PlayerShip ActivePlayer => activePlayer;

        public bool BossActive => activeBoss != null;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            run = RunState.Instance;
            if (run != null)
            {
                run.UpgradesChanged += HandleUpgradesChanged;
            }

            if (waveDirector != null)
            {
                waveDirector.WaveCleared += HandleWaveCleared;
            }

            HighScore = LoadHighScore();
            lives = CurrentStartingLives();
            EnterState(GameState.Ready);
        }

        private void OnDestroy()
        {
            if (run != null)
            {
                run.UpgradesChanged -= HandleUpgradesChanged;
            }

            if (waveDirector != null)
            {
                waveDirector.WaveCleared -= HandleWaveCleared;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            switch (State)
            {
                case GameState.Ready:
                    TickDifficultySelection();
                    if (PlayerInputReader.StartPressed)
                    {
                        BeginRun();
                    }
                    break;

                case GameState.GameOver:
                    if (PlayerInputReader.StartPressed)
                    {
                        BeginRun();
                    }
                    break;

                case GameState.Respawning:

                    respawnTimer -= GameClock.PlayerDelta;
                    if (respawnTimer <= 0f)
                    {
                        SpawnPlayer();
                        EnterState(GameState.Playing);
                    }
                    break;

                case GameState.Playing:
                    if (PlayerInputReader.PausePressed)
                    {
                        EnterState(GameState.Paused);
                        break;
                    }

                    if (PlayerInputReader.SlowMotionPressed)
                    {
                        TriggerSlowMotion("SLOW MOTION");
                    }
                    break;

                case GameState.Paused:
                    if (PlayerInputReader.PausePressed || PlayerInputReader.StartPressed)
                    {
                        EnterState(GameState.Playing);
                    }
                    break;

                case GameState.Shopping:

                    break;
            }
        }

        private void TickDifficultySelection()
        {
            if (run == null)
            {
                return;
            }

            int step = PlayerInputReader.MenuStep;
            if (step == 0)
            {
                return;
            }

            run.SetDifficulty(Difficulty.Cycle(run.Level, step));
            lives = CurrentStartingLives();
            HighScore = LoadHighScore();

            LivesChanged?.Invoke(lives);
            ScoreChanged?.Invoke(score);
        }

        public void BeginRun()
        {
            score = 0;
            nextSlowMotionScore = slowMotionScoreInterval;
            appliedBonusLives = 0;

            run?.BeginRun();
            lives = CurrentStartingLives();

            ScoreChanged?.Invoke(score);
            LivesChanged?.Invoke(lives);

            TimeWarp.Instance?.Cancel();
            ComboTracker.Instance?.Reset();
            ClearTransientObjects();
            SpawnPlayer();

            waveDirector?.BeginRun();
            EnterState(GameState.Playing);
            waveDirector?.StartNextWave();
        }

        private int CurrentStartingLives()
        {
            int fromDifficulty = run != null ? run.Settings.StartingLives : 3;
            int fromShop = run != null ? run.BonusLives : 0;
            return Mathf.Max(1, fromDifficulty + fromShop);
        }

        public void ReportEnemyDestroyed(EnemyKind kind)
        {
            int basePoints = ScoreTable.PointsFor(kind);
            AwardPoints(basePoints);
            waveDirector?.ReportKill();
        }

        public void AwardPoints(int basePoints)
        {
            if (basePoints <= 0 || State != GameState.Playing)
            {
                return;
            }

            float multiplier = ComboTracker.Instance != null
                ? ComboTracker.Instance.RegisterKill()
                : 1f;
            float difficultyScale = run != null ? run.Settings.ScoreScale : 1f;

            int awarded = Mathf.Max(1, Mathf.RoundToInt(basePoints * multiplier * difficultyScale));
            AddScore(awarded);
            run?.AddCredits(awarded);
        }

        public void AwardBonus(int basePoints)
        {
            if (basePoints <= 0 || State != GameState.Playing)
            {
                return;
            }

            float difficultyScale = run != null ? run.Settings.ScoreScale : 1f;
            int awarded = Mathf.Max(1, Mathf.RoundToInt(basePoints * difficultyScale));

            AddScore(awarded);
            run?.AddCredits(awarded);
        }

        public void AddScore(int amount)
        {
            if (amount <= 0 || State != GameState.Playing)
            {
                return;
            }

            score += amount;
            if (score > HighScore)
            {
                HighScore = score;
                PlayerPrefs.SetInt(HighScoreKey(), HighScore);
            }

            ScoreChanged?.Invoke(score);

            while (score >= nextSlowMotionScore)
            {
                nextSlowMotionScore += slowMotionScoreInterval;
                TriggerSlowMotion("BONUS " + score.ToString() + " PTS");
            }
        }

        public void TriggerSlowMotion(string reason)
        {
            TimeWarp warp = TimeWarp.Instance;
            if (warp == null)
            {
                return;
            }

            warp.Activate();
            Announce(reason);
        }

        public void Announce(string message)
        {
            Announced?.Invoke(message);
        }

        public bool SpawnBoss()
        {
            if (bossPrefab == null)
            {
                return false;
            }

            PlayArea area = PlayArea.Instance;
            Vector3 position = area != null
                ? new Vector3(area.SpawnX + 1.5f, area.Bounds.center.y, 0f)
                : new Vector3(8f, 0f, 0f);

            activeBoss = Instantiate(bossPrefab, position, Quaternion.identity);
            activeBoss.name = "Boss";
            activeBoss.Defeated += HandleBossDefeated;

            SfxLibrary.Instance?.PlayBossWarning();
            Announce("PERIGO");
            BossSpawned?.Invoke(activeBoss);
            return true;
        }

        private void HandleBossDefeated()
        {
            if (activeBoss != null)
            {
                activeBoss.Defeated -= HandleBossDefeated;
                activeBoss = null;
            }

            waveDirector?.ReportBossDefeated();
        }

        private void HandleWaveCleared(int wave)
        {
            if (State != GameState.Playing)
            {
                return;
            }

            TimeWarp.Instance?.Cancel();
            Announce("ONDA " + wave.ToString() + " LIMPA");
            EnterState(GameState.Shopping);
        }

        public void CloseShop()
        {
            if (State != GameState.Shopping)
            {
                return;
            }

            EnterState(GameState.Playing);
            waveDirector?.StartNextWave();
        }

        private void HandleUpgradesChanged()
        {
            if (run == null)
            {
                return;
            }

            int bonus = run.BonusLives;
            if (bonus > appliedBonusLives)
            {
                lives += bonus - appliedBonusLives;
                appliedBonusLives = bonus;
                LivesChanged?.Invoke(lives);
            }
        }

        public void ReportPlayerDestroyed()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            activePlayer = null;

            TimeWarp.Instance?.Cancel();
            ComboTracker.Instance?.Reset();

            lives = Mathf.Max(0, lives - 1);
            LivesChanged?.Invoke(lives);

            if (lives <= 0)
            {
                SfxLibrary.Instance?.PlayGameOver();
                EnterState(GameState.GameOver);
                return;
            }

            respawnTimer = respawnDelay;
            EnterState(GameState.Respawning);
        }

        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[GameManager] playerPrefab nao foi atribuido no inspector.", this);
                return;
            }

            if (activePlayer != null)
            {
                Destroy(activePlayer.gameObject);
            }

            Vector3 position = ResolveSpawnPosition();
            activePlayer = Instantiate(playerPrefab, position, Quaternion.identity);
            activePlayer.name = "Player";

            if (run != null && run.StartWithShield)
            {
                activePlayer.ApplyPowerUp(PowerUpKind.Shield);
            }

            PlayerSpawned?.Invoke(activePlayer);
        }

        private Vector3 ResolveSpawnPosition()
        {
            if (playerSpawnPoint != null)
            {
                return playerSpawnPoint.position;
            }

            PlayArea area = PlayArea.Instance;
            if (area == null)
            {
                return new Vector3(-3.5f, 0f, 0f);
            }

            Rect bounds = area.Bounds;
            return new Vector3(bounds.xMin + bounds.width * 0.15f, bounds.center.y, 0f);
        }

        private void ClearTransientObjects()
        {
            foreach (Enemy enemy in FindObjectsByType<Enemy>())
            {
                Destroy(enemy.gameObject);
            }

            foreach (Projectile projectile in FindObjectsByType<Projectile>())
            {
                Pool.Release(projectile.gameObject);
            }

            foreach (PowerUp powerUp in FindObjectsByType<PowerUp>())
            {
                Destroy(powerUp.gameObject);
            }

            foreach (Boss boss in FindObjectsByType<Boss>())
            {
                boss.Defeated -= HandleBossDefeated;
                Destroy(boss.gameObject);
            }

            activeBoss = null;
        }

        private void EnterState(GameState next)
        {
            State = next;

            bool frozen = next == GameState.Paused || next == GameState.Shopping;
            TimeWarp.Instance?.SetPaused(frozen);

            bool spawning = next == GameState.Playing;
            waveDirector?.SetSpawning(spawning);
            if (powerUpSpawner != null)
            {
                powerUpSpawner.SetRunning(spawning);
            }

            if (next == GameState.GameOver || next == GameState.Ready)
            {
                TimeWarp.Instance?.Cancel();
            }

            StateChanged?.Invoke(next);
        }

        private string HighScoreKey()
        {
            DifficultyLevel level = run != null ? run.Level : DifficultyLevel.Normal;
            return HighScoreKeyPrefix + level.ToString();
        }

        private int LoadHighScore()
        {
            return PlayerPrefs.GetInt(HighScoreKey(), 0);
        }
    }
}
