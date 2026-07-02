using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElevatorGame.Core
{
    /// <summary>
    /// A Service Locator that holds references to all active managers.
    /// Eliminates the need for singletons and tight coupling.
    /// </summary>
    public class DependencyManager : MonoBehaviour
    {
        private static DependencyManager _instance;
        public static DependencyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("DependencyManager");
                    _instance = go.AddComponent<DependencyManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private readonly Dictionary<Type, MonoBehaviour> _services = new Dictionary<Type, MonoBehaviour>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Register<T>(T service) where T : MonoBehaviour
        {
            Type type = typeof(T);
            if (!_services.ContainsKey(type))
            {
                _services[type] = service;
            }
            else
            {
                Debug.LogWarning($"[DependencyManager] Service of type {type} is already registered.");
            }
        }

        public void Unregister<T>() where T : MonoBehaviour
        {
            Type type = typeof(T);
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
            }
        }

        public T Resolve<T>() where T : MonoBehaviour
        {
            Type type = typeof(T);
            if (_services.TryGetValue(type, out MonoBehaviour service))
            {
                return service as T;
            }

            Debug.LogError($"[DependencyManager] Service of type {type} is not registered!");
            return null;
        }
    }
}
