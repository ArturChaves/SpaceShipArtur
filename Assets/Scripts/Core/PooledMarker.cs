using UnityEngine;

namespace SpaceShip.Core
{
    [DisallowMultipleComponent]
    public class PooledMarker : MonoBehaviour
    {
        public Component Prefab { get; internal set; }
    }
}
