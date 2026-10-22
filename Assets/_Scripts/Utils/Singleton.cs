using System;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static bool IsInitialized => _instance != null;
    protected static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();
                if (_instance == null)
                    throw new InvalidOperationException($"No instance of singleton {typeof(T).Name} found in the scene. Make sure to add one to the scene.");

            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Multiple instances of singleton {typeof(T).Name} detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        if (_instance == null)
        {
            Destroy(gameObject);
            throw new InvalidCastException($"Failed to cast {typeof(T).Name} to Singleton<{typeof(T).Name}>.");
        }

        DontDestroyOnLoad(gameObject);
    }
}
