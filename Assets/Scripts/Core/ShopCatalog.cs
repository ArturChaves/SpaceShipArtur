namespace SpaceShip.Core
{
    public enum ShopItemId
    {
        Spread,
        Damage,
        FireRate,

        ExtraLife,
        StartShield,
        LongerSlowMotion,

        EnemySlow,
        EnemyFragile,
        EnemyDisarm
    }

    public struct ShopItem
    {
        public ShopItemId Id;
        public string Category;
        public string Name;
        public string Description;

        public int BasePrice;

        public int PriceStep;

        public int MaxLevel;
    }

    public static class ShopCatalog
    {
        public const int ProjectilesPerSpreadLevel = 1;

        public const float FireRateStepPerLevel = 0.12f;

        public const float EnemySlowStepPerLevel = 0.08f;

        public const float EnemyDisarmStepPerLevel = 0.22f;

        public const float SlowMotionSecondsPerLevel = 1.5f;

        private static readonly ShopItem[] Items =
        {
            new ShopItem
            {
                Id = ShopItemId.Spread, Category = "ARMA", Name = "Leque",
                Description = "+1 projetil por disparo",
                BasePrice = 420, PriceStep = 520, MaxLevel = 4
            },
            new ShopItem
            {
                Id = ShopItemId.Damage, Category = "ARMA", Name = "Carga pesada",
                Description = "+1 de dano por tiro",
                BasePrice = 480, PriceStep = 620, MaxLevel = 4
            },
            new ShopItem
            {
                Id = ShopItemId.FireRate, Category = "ARMA", Name = "Refrigeracao",
                Description = "-12% no intervalo entre tiros",
                BasePrice = 320, PriceStep = 400, MaxLevel = 5
            },
            new ShopItem
            {
                Id = ShopItemId.ExtraLife, Category = "NAVE", Name = "Nave reserva",
                Description = "+1 vida",
                BasePrice = 450, PriceStep = 400, MaxLevel = 3
            },
            new ShopItem
            {
                Id = ShopItemId.StartShield, Category = "NAVE", Name = "Escudo de fabrica",
                Description = "Renasce sempre com escudo",
                BasePrice = 520, PriceStep = 0, MaxLevel = 1
            },
            new ShopItem
            {
                Id = ShopItemId.LongerSlowMotion, Category = "NAVE", Name = "Capacitor temporal",
                Description = "+1,5 s de slow motion",
                BasePrice = 240, PriceStep = 200, MaxLevel = 3
            },
            new ShopItem
            {
                Id = ShopItemId.EnemySlow, Category = "SABOTAGEM", Name = "Lastro gravitacional",
                Description = "-8% na velocidade inimiga",
                BasePrice = 200, PriceStep = 170, MaxLevel = 4
            },
            new ShopItem
            {
                Id = ShopItemId.EnemyFragile, Category = "SABOTAGEM", Name = "Corrosao de casco",
                Description = "-1 de resistencia dos inimigos",
                BasePrice = 380, PriceStep = 340, MaxLevel = 2
            },
            new ShopItem
            {
                Id = ShopItemId.EnemyDisarm, Category = "SABOTAGEM", Name = "Interferencia",
                Description = "+22% no intervalo de tiro inimigo",
                BasePrice = 230, PriceStep = 190, MaxLevel = 3
            },
        };

        public static int Count => Items.Length;

        public static ShopItem At(int index)
        {
            return Items[System.Math.Max(0, System.Math.Min(index, Items.Length - 1))];
        }

        public static int IndexOf(ShopItemId id)
        {
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int PriceFor(ShopItem item, int currentLevel)
        {
            if (currentLevel >= item.MaxLevel)
            {
                return -1;
            }

            return item.BasePrice + item.PriceStep * currentLevel;
        }
    }
}
