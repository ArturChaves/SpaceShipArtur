using UnityEngine;

namespace SpaceShip.Core
{
    public static class GameClock
    {
        public static bool Paused { get; private set; }

        public static float PlayerDelta => Paused ? 0f : Time.unscaledDeltaTime;

        public static float WorldDelta => Time.deltaTime;

        internal static void SetPaused(bool value)
        {
            Paused = value;
        }

        internal static void Reset()
        {
            Paused = false;
        }
    }
}
