using System;

using UnityEngine;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// The base of BetterLegacy's managers.
    /// </summary>
    /// <typeparam name="T">Type of the manager.</typeparam>
    public abstract class BaseManager<T, TSettings> : MonoBehaviour where T : BaseManager<T, TSettings> where TSettings : ManagerSettings, new()
    {
        #region Init

        /// <summary>
        /// The <typeparamref name="T"/> global instance reference.
        /// </summary>
        public static T inst;

        /// <summary>
        /// Settings of the manager based on <typeparamref name="TSettings"/>.
        /// </summary>
        public static TSettings managerSettings = new TSettings();

        /// <summary>
        /// Initilializes the <typeparamref name="T"/>.
        /// </summary>
        public static void Init()
        {
            if (inst)
            {
                CoreHelper.Log($"{Name} is already initialized.");
                return;
            }

            if (managerSettings == null)
            {
                CoreHelper.LogError($"Could not initialize {Name} due to the settings being null.");
                return;
            }

            inst = managerSettings.IsComponent ? managerSettings.Parent.gameObject.AddComponent<T>() : Creator.NewGameObject(Name, managerSettings.Parent).AddComponent<T>();
        }

        #endregion

        #region Internal

        void Awake()
        {
            if (!inst)
                inst = this as T;
            if (init)
            {
                LogWarning("The Manager was already initialized.");
                return;
            }

            init = true;
            OnInit();
        }

        void Start() => OnManagerStart();

        void OnEnable() => OnActiveChanged(true);
        void OnDisable() => OnActiveChanged(false);

        void Update() => OnTick();

        void OnDestroy()
        {
            inst = null;
            OnManagerDestroyed();
        }

        void OnApplicationQuit() => OnAppExit();

        readonly static Type type = typeof(T);

        bool init;

        #endregion

        #region Values

        /// <summary>
        /// The name of the manager.
        /// </summary>
        public static string Name => type?.Name ?? throw new NullReferenceException($"{nameof(type)} was null and could not have the name obtained from it.");

        /// <summary>
        /// Debug name of the manager.
        /// </summary>
        public string ClassName => managerSettings?.ClassName;

        #endregion

        #region Functions

        /// <summary>
        /// Logs a message from the manager.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public static void Log(string message)
        {
            if (managerSettings != null && !string.IsNullOrEmpty(managerSettings.ClassName))
            {
                Debug.Log($"{managerSettings.ClassName}{message}");
                return;
            }
            CoreHelper.Log(message);
        }

        /// <summary>
        /// Logs a message from the manager.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public static void LogWarning(string message)
        {
            if (managerSettings != null && !string.IsNullOrEmpty(managerSettings.ClassName))
            {
                Debug.LogWarning($"{managerSettings.ClassName}{message}");
                return;
            }
            CoreHelper.LogWarning(message);
        }

        /// <summary>
        /// Logs a message from the manager.
        /// </summary>
        /// <param name="message">Message to log.</param>
        public static void LogError(string message)
        {
            if (managerSettings != null && !string.IsNullOrEmpty(managerSettings.ClassName))
            {
                Debug.LogError($"{managerSettings.ClassName}{message}");
                return;
            }
            CoreHelper.LogError(message);
        }

        /// <summary>
        /// Runs on initialization.
        /// </summary>
        public virtual void OnInit() { }

        /// <summary>
        /// Runs on manager start.
        /// </summary>
        public virtual void OnManagerStart() { }

        /// <summary>
        /// Runs when the active state of the manager is changed.
        /// </summary>
        /// <param name="active">Active state of the manager.</param>
        public virtual void OnActiveChanged(bool active) { }

        /// <summary>
        /// Runs per frame.
        /// </summary>
        public virtual void OnTick() { }

        /// <summary>
        /// Runs when the manager is destroyed.
        /// </summary>
        public virtual void OnManagerDestroyed() { }

        /// <summary>
        /// Runs when the application is closed.
        /// </summary>
        public virtual void OnAppExit() { }

        /// <summary>
        /// Destroys the manager.
        /// </summary>
        public void Kill() => CoreHelper.Destroy(managerSettings != null && managerSettings.IsComponent ? this : gameObject);

        #endregion
    }

    /// <summary>
    /// Example to demonstrate the BaseManager usage.
    /// </summary>
    public class TestManager : BaseManager<TestManager, TestManagerSettings>
    {
        public override void OnInit()
        {
            CoreHelper.Log($"{nameof(OnInit)}");
        }

        public override void OnTick()
        {
            if (number == 1)
                CoreHelper.Log($"{nameof(OnTick)}");
        }

        public int number = 0;
    }

    public class TestManagerSettings : ManagerSettings
    {
        public TestManagerSettings() { }
    }

    public static class TestingManagers
    {
        public static void Test()
        {
            TestManager.Init();
        }
    }
}
