using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace VonderGames.AiTesting.Gameplay.Pooling
{
    public sealed class SceneEntityPool<T> : IDisposable where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _runtimeRoot;
        private readonly GameObject _storage;
        private readonly ObjectPool<T> _pool;
        private readonly HashSet<T> _active = new HashSet<T>();
        private readonly Action<T> _reset;
        public int ActiveCount => _active.Count;
        public int InactiveCount => _pool.CountInactive;

        public SceneEntityPool(T prefab, Transform runtimeRoot, Action<T> reset, int retainedCapacity)
        {
            _prefab = prefab;
            _runtimeRoot = runtimeRoot;
            _reset = reset;
            _storage = new GameObject(typeof(T).Name + "Pool");
            _storage.transform.SetParent(runtimeRoot, false);
            _storage.SetActive(false);
            _pool = new ObjectPool<T>(Create, actionOnRelease: Store,
                actionOnDestroy: DestroyItem,
                collectionCheck: true, defaultCapacity: retainedCapacity, maxSize: retainedCapacity);
        }

        public T Rent(Vector2 position)
        {
            T item = _pool.Get();
            _active.Add(item);
            item.transform.SetParent(_runtimeRoot, false);
            item.transform.SetPositionAndRotation(position, Quaternion.identity);
            return item;
        }

        public void Return(T item)
        {
            if (!_active.Remove(item)) return;
            _pool.Release(item);
        }

        public void ClearActive()
        {
            // Only restart/teardown allocates a snapshot; returns mutate active membership.
            foreach (T item in new List<T>(_active))
            {
                // Scene unloading can destroy a leased child before its pool owner.
                if (item != null) Return(item);
                else _active.Remove(item);
            }
        }

        public void Dispose()
        {
            ClearActive();
            _pool.Dispose();
            UnityEngine.Object.Destroy(_storage);
        }

        private T Create()
        {
            T item = UnityEngine.Object.Instantiate(_prefab, _storage.transform);
            item.gameObject.SetActive(false);
            return item;
        }

        private void Store(T item)
        {
            _reset(item);
            item.gameObject.SetActive(false);
            item.transform.SetParent(_storage.transform, false);
        }

        private void DestroyItem(T item)
        {
            // The scene may already have destroyed inactive storage during unloading.
            if (item != null) UnityEngine.Object.Destroy(item.gameObject);
        }

    }
}
