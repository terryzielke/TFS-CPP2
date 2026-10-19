using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{

    protected static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    _instance = singletonObject.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            Debug.LogWarning($"Multiple instances of singleton {typeof(T).Name} detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (_instance != this)
        {
            Debug.LogWarning($"Multiple instances of singleton {typeof(T).Name} detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
}
