using System;
using UnityEngine;

namespace SpaceShip.Core
{
    [DisallowMultipleComponent]
    public class RunState : MonoBehaviour
    {
        public static RunState Instance { get; private set; }

        private const string DifficultyKey = "SpaceShip.Difficulty";

        private readonly int[] levels = new int[ShopCatalog.Count];

        public event Action<int> CreditsChanged;

        public event Action UpgradesChanged;

        public event Action<DifficultyLevel> DifficultyChanged;

        public int Credits { get; private set; }

        public DifficultyLevel Level { get; private set; } = DifficultyLevel.Normal;

        public DifficultySettings Settings => Difficulty.SettingsFor(Level);

        private void Awake()
        {
            Instance = this;
            Level = (DifficultyLevel)Mathf.Clamp(
                PlayerPrefs.GetInt(DifficultyKey, (int)DifficultyLevel.Normal),
                0, Difficulty.Count - 1);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetDifficulty(DifficultyLevel level)
        {
            if (Level == level)
            {
                return;
            }

            Level = level;
            PlayerPrefs.SetInt(DifficultyKey, (int)level);
            DifficultyChanged?.Invoke(level);
        }

        public void BeginRun()
        {
            Credits = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = 0;
            }

            CreditsChanged?.Invoke(Credits);
            UpgradesChanged?.Invoke();
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Credits += amount;
            CreditsChanged?.Invoke(Credits);
        }

        public int LevelOf(ShopItemId id)
        {
            int index = ShopCatalog.IndexOf(id);
            return index < 0 ? 0 : levels[index];
        }

        public int LevelAt(int index)
        {
            return index < 0 || index >= levels.Length ? 0 : levels[index];
        }

        public bool TryPurchase(int index)
        {
            if (index < 0 || index >= levels.Length)
            {
                return false;
            }

            ShopItem item = ShopCatalog.At(index);
            int price = ShopCatalog.PriceFor(item, levels[index]);
            if (price < 0 || price > Credits)
            {
                return false;
            }

            Credits -= price;
            levels[index]++;

            CreditsChanged?.Invoke(Credits);
            UpgradesChanged?.Invoke();
            return true;
        }

        public int ExtraProjectiles =>
            LevelOf(ShopItemId.Spread) * ShopCatalog.ProjectilesPerSpreadLevel;

        public int BonusDamage => LevelOf(ShopItemId.Damage);

        public float FireRateScale => Mathf.Max(
            0.25f, 1f - LevelOf(ShopItemId.FireRate) * ShopCatalog.FireRateStepPerLevel);

        public int BonusLives => LevelOf(ShopItemId.ExtraLife);

        public bool StartWithShield => LevelOf(ShopItemId.StartShield) > 0;

        public float BonusSlowMotionSeconds =>
            LevelOf(ShopItemId.LongerSlowMotion) * ShopCatalog.SlowMotionSecondsPerLevel;

        public float EnemySpeedScale => Mathf.Max(
            0.35f,
            Settings.EnemySpeedScale
            - LevelOf(ShopItemId.EnemySlow) * ShopCatalog.EnemySlowStepPerLevel);

        public float EnemyHealthScale => Settings.EnemyHealthScale;

        public int EnemyHealthPenalty => LevelOf(ShopItemId.EnemyFragile);

        public float EnemyFireIntervalScale =>
            Settings.EnemyFireIntervalScale
            * (1f + LevelOf(ShopItemId.EnemyDisarm) * ShopCatalog.EnemyDisarmStepPerLevel);

        public int ResolveEnemyHealth(int baseHitPoints)
        {
            int scaled = Mathf.CeilToInt(baseHitPoints * EnemyHealthScale);
            return Mathf.Max(1, scaled - EnemyHealthPenalty);
        }
    }
}
