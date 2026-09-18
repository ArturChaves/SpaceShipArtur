using System.Collections.Generic;
using UnityEngine;

namespace SpaceShip.Core
{
    [DisallowMultipleComponent]
    public class Pool : MonoBehaviour
    {
        public static Pool Instance { get; private set; }

        private readonly Dictionary<Component, Stack<GameObject>> idle =
            new Dictionary<Component, Stack<GameObject>>();

        private Transform parkingLot;

        private void Awake()
        {
            Instance = this;

            GameObject holder = new GameObject("Pool (inativos)");
            holder.transform.SetParent(transform, false);
            parkingLot = holder.transform;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation)
            where T : Component
        {
            if (prefab == null)
            {
                return null;
            }

            Pool pool = Instance;
            if (pool == null)
            {
                return Object.Instantiate(prefab, position, rotation);
            }

            return pool.SpawnInternal(prefab, position, rotation);
        }

        private T SpawnInternal<T>(T prefab, Vector3 position, Quaternion rotation)
            where T : Component
        {
            GameObject instance = null;
            if (idle.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                while (stack.Count > 0 && instance == null)
                {
                    instance = stack.Pop();
                }
            }

            if (instance == null)
            {
                T created = Object.Instantiate(prefab, position, rotation);
                created.gameObject.AddComponent<PooledMarker>().Prefab = prefab;
                NotifySpawned(created.gameObject);
                return created;
            }

            instance.transform.SetParent(null, false);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.SetActive(true);

            NotifySpawned(instance);
            return instance.GetComponent<T>();
        }

        private static void NotifySpawned(GameObject instance)
        {
            IPooled[] pooled = instance.GetComponents<IPooled>();
            for (int i = 0; i < pooled.Length; i++)
            {
                pooled[i].OnSpawned();
            }
        }

        public static void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            Pool pool = Instance;
            PooledMarker marker = instance.GetComponent<PooledMarker>();

            if (pool == null || marker == null || marker.Prefab == null)
            {
                Object.Destroy(instance);
                return;
            }

            pool.ReleaseInternal(instance, marker.Prefab);
        }

        private void ReleaseInternal(GameObject instance, Component key)
        {
            if (!instance.activeSelf)
            {
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(parkingLot, false);

            if (!idle.TryGetValue(key, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                idle[key] = stack;
            }

            stack.Push(instance);
        }
    }
}
