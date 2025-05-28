using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

namespace LichLord
{
    public interface IGlobalService
    {
        void Initialize();
        void Tick();
        void Deinitialize();
    }

    [System.Serializable]
    public static class Global
    {
        // PUBLIC MEMBERS

        [SerializeField]
        public static GlobalSettings Settings;
        [SerializeField]
        public static GlobalTables Tables;
        [SerializeField]
        public static Networking Networking;
        /*
            [SerializeField]
            public static RuntimeSettings RuntimeSettings;
            [SerializeField]
            public static PlayerService PlayerService;

            [SerializeField]
            public static MultiplayManager MultiplayManager;
        */


        // PRIVATE MEMBERS

        private static bool _isInitialized = false;
public static bool IsInitialized { get { return _isInitialized; } }

public static Action OnInitialized;

public static void SetInitialized(bool value)
{
    _isInitialized = value;
    OnInitialized?.Invoke();
}

private static List<IGlobalService> _globalServices = new List<IGlobalService>(16);

// PUBLIC METHODS

public static void Quit()
{
    Deinitialize();

#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}

// PRIVATE METHODS

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void InitializeSubSystem()
{
    /*
    if (Application.isBatchMode == true)
    {
        UnityEngine.AudioListener.volume = 0.0f;
        PlayerLoopUtility.RemovePlayerLoopSystems(typeof(PostLateUpdate.UpdateAudio));
    }

#if UNITY_EDITOR
    if (Application.isPlaying == false)
        return;
#endif
    if (PlayerLoopUtility.HasPlayerLoopSystem(typeof(Global)) == false)
    {
        PlayerLoopUtility.AddPlayerLoopSystem(typeof(Global), typeof(Update.ScriptRunBehaviourUpdate), BeforeUpdate, AfterUpdate);
    }
    */
        Application.quitting -= OnApplicationQuit;
            Application.quitting += OnApplicationQuit;

            //_isInitialized = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            Initialize();

            // You can pause network services here
            /*
            if (ApplicationSettings.IsBatchServer == true)
            {
                Application.targetFrameRate = TickRate.Resolve(NetworkProjectConfig.Global.Simulation.TickRateSelection).Server;
            }

            if (ApplicationSettings.HasFrameRate == true)
            {
                Application.targetFrameRate = ApplicationSettings.FrameRate;
            }
            */
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAfterSceneLoad()
        {
            // You can unpause network services here
        }

        private static void Initialize()
        {
            /*
            if (GlobalInitializer.Instance == null)
            {
                GameObject go = new GameObject("GlobalInitializer");
                GlobalInitializer init = go.AddComponent<GlobalInitializer>();
                init.InitializeGlobals();
            }
            else
            {
                GlobalInitializer.Instance.InitializeGlobals();
            }
            */
        }

        private static void Deinitialize()
        {
            if (_isInitialized == false)
                return;

            for (int i = _globalServices.Count - 1; i >= 0; i--)
            {
                var service = _globalServices[i];
                if (service != null)
                {
                    service.Deinitialize();
                }
            }

            _isInitialized = false;
        }

        private static void OnApplicationQuit()
        {
            Deinitialize();
        }

        private static void BeforeUpdate()
        {
            for (int i = 0; i < _globalServices.Count; i++)
            {
                _globalServices[i].Tick();
            }
        }

        private static void AfterUpdate()
        {
            if (Application.isPlaying == false)
            {
                // PlayerLoopUtility.RemovePlayerLoopSystems(typeof(Global));
            }
        }

        public static void PrepareGlobalServices()
        {
              /*
            //PlayerService = new PlayerService();

            //_globalServices.Add(PlayerService);
          
            for (int i = 0; i < _globalServices.Count; i++)
            {
                _globalServices[i].Initialize();
            }
              */
        }

        public static T CreateStaticObject<T>() where T : Component
        {
            GameObject gameObject = new GameObject(typeof(T).Name);
            UnityEngine.Object.DontDestroyOnLoad(gameObject);

            return gameObject.AddComponent<T>();
        }
    }
}
