namespace SpaceShip.Core
{
    public struct DifficultySettings
    {
        public string Label;

        public int StartingLives;

        public float EnemySpeedScale;

        public float EnemyHealthScale;

        public float SpawnIntervalScale;

        public float EnemyFireIntervalScale;

        public float ScoreScale;

        public float DisruptorChance;

        public bool AllowScramble;
    }

    public static class Difficulty
    {
        public const int Count = 4;

        public static DifficultySettings SettingsFor(DifficultyLevel level)
        {
            switch (level)
            {
                case DifficultyLevel.Facil:
                    return new DifficultySettings
                    {
                        Label = "FACIL",
                        StartingLives = 5,
                        EnemySpeedScale = 0.85f,
                        EnemyHealthScale = 1f,
                        SpawnIntervalScale = 1.35f,
                        EnemyFireIntervalScale = 1.4f,
                        ScoreScale = 0.7f,
                        DisruptorChance = 0f,
                        AllowScramble = false
                    };

                case DifficultyLevel.Dificil:
                    return new DifficultySettings
                    {
                        Label = "DIFICIL",
                        StartingLives = 2,
                        EnemySpeedScale = 1.2f,
                        EnemyHealthScale = 1.35f,
                        SpawnIntervalScale = 0.76f,
                        EnemyFireIntervalScale = 0.78f,
                        ScoreScale = 1.5f,
                        DisruptorChance = 0.3f,
                        AllowScramble = true
                    };

                case DifficultyLevel.Insano:
                    return new DifficultySettings
                    {
                        Label = "INSANO",
                        StartingLives = 1,
                        EnemySpeedScale = 1.42f,
                        EnemyHealthScale = 1.7f,
                        SpawnIntervalScale = 0.58f,
                        EnemyFireIntervalScale = 0.6f,
                        ScoreScale = 2.2f,
                        DisruptorChance = 0.45f,
                        AllowScramble = true
                    };

                case DifficultyLevel.Normal:
                default:
                    return new DifficultySettings
                    {
                        Label = "NORMAL",
                        StartingLives = 3,
                        EnemySpeedScale = 1f,
                        EnemyHealthScale = 1f,
                        SpawnIntervalScale = 1f,
                        EnemyFireIntervalScale = 1f,
                        ScoreScale = 1f,
                        DisruptorChance = 0.16f,
                        AllowScramble = false
                    };
            }
        }

        public static DifficultyLevel Cycle(DifficultyLevel level, int step)
        {
            int index = ((int)level + step) % Count;
            if (index < 0)
            {
                index += Count;
            }

            return (DifficultyLevel)index;
        }
    }
}
