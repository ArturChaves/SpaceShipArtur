namespace SpaceShip.Core
{
    public static class ScoreTable
    {
        public const int DronePoints = 10;
        public const int StingerPoints = 25;
        public const int HulkPoints = 50;

        public const int SentryPoints = 35;

        public const int RacerPoints = 15;

        public static int PointsFor(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Drone:
                    return DronePoints;
                case EnemyKind.Stinger:
                    return StingerPoints;
                case EnemyKind.Hulk:
                    return HulkPoints;
                case EnemyKind.Sentry:
                    return SentryPoints;
                case EnemyKind.Racer:
                    return RacerPoints;
                default:
                    return 0;
            }
        }
    }
}
