using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using BepInEx.Configuration;
using PracticeMode.Patches;
using System.Collections.Generic;
using System.IO;
using CustomGameModes.Patches;
using PracticeMode.Hooks;

#if TAIKO_IL2CPP
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP;
#endif

namespace PracticeMode
{
    public enum LogType
    {
        Info,
        Warning,
        Error,
        Fatal,
        Message,
        Debug
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, ModName, PluginInfo.PLUGIN_VERSION)]
#if TAIKO_MONO
    public class Plugin : BaseUnityPlugin
#elif TAIKO_IL2CPP
    public class Plugin : BasePlugin
#endif
    {
        const string ModName = "PracticeModeTest";

        public static Plugin Instance;
        private Harmony _harmony;
        public new static ManualLogSource Log;

        public ConfigEntry<bool> ConfigEnabled;
        public ConfigEntry<bool> ConfigExamplesEnabled;

        public ConfigEntry<bool> ConfigLoggingEnabled;
        public ConfigEntry<int> ConfigLoggingDetailLevelEnabled;

#if TAIKO_MONO
        private void Awake()
#elif TAIKO_IL2CPP
        public override void Load()
#endif
        {
            Instance = this;

#if TAIKO_MONO
            Log = Logger;
#elif TAIKO_IL2CPP
            Log = base.Log;
#endif

            SetupConfig();
            SetupHarmony();
        }

        private void SetupConfig()
        {
            var dataFolder = Path.Combine("BepInEx", "data", ModName);

            ConfigEnabled = Config.Bind("General",
                "Enabled",
                true,
                "Enables the mod.");

            ConfigLoggingEnabled = Config.Bind("Debug",
                "LoggingEnabled",
                true,
                "Enables logs to be sent to the console.");

            ConfigLoggingDetailLevelEnabled = Config.Bind("Debug",
                "LoggingDetailLevelEnabled",
                0,
                "Enables more detailed logs to be sent to the console. The higher the number, the more logs will be displayed. Mostly for my own debugging.");
        }

        private void SetupHarmony()
        {
            // Patch methods
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);

            if (ConfigEnabled.Value)
            {
                _harmony.PatchAll(typeof(SongSelectManagerHooks));
                _harmony.PatchAll(typeof(PracticeModeHooks));
                CustomModeSelectApi.AddButton("PracticeMode", "Practice Mode", "Enters the song select menu for practice mode!", new Color32(244, 219, 173, 255), () => PracticeModeMenu.ChangeScenePracticeMode());

                Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled.");
            }

            //if (ConfigExamplesEnabled.Value)
            //{
            //    _harmony.PatchAll(typeof(ExampleSingleHitBigNotesPatch));
            //    _harmony.PatchAll(typeof(ExampleSortByUraPatch));
            //    Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} Example Patches are loaded!");
            //}
        }

        public static MonoBehaviour GetMonoBehaviour() => TaikoSingletonMonoBehaviour<CommonObjects>.Instance;

        public void StartCustomCoroutine(IEnumerator enumerator)
        {
#if TAIKO_MONO
            GetMonoBehaviour().StartCoroutine(enumerator);
#elif TAIKO_IL2CPP
            GetMonoBehaviour().StartCoroutine(enumerator);
#endif
        }

        public void LogInfoInstance(LogType type, string value, int detailLevel = 0)
        {
            if (ConfigLoggingEnabled.Value && (ConfigLoggingDetailLevelEnabled.Value >= detailLevel))
            {
                switch (type)
                {
                    case LogType.Info:
                        Log.LogInfo("[" + detailLevel + "] " + value);
                        break;
                    case LogType.Warning:
                        Log.LogWarning("[" + detailLevel + "] " + value);
                        break;
                    case LogType.Error:
                        Log.LogError("[" + detailLevel + "] " + value);
                        break;
                    case LogType.Fatal:
                        Log.LogFatal("[" + detailLevel + "] " + value);
                        break;
                    case LogType.Message:
                        Log.LogMessage("[" + detailLevel + "] " + value);
                        break;
                    case LogType.Debug:
                        // I'm not sure if I should make this only happen in DEBUG mode
                        // Seems like a decent idea, I'll keep it until it seems like a bad idea
#if DEBUG
                        Log.LogDebug("[" + detailLevel + "] " + value);
#endif
                        break;
                    default:
                        break;
                }
            }
        }
        public static void LogInfo(LogType type, string value, int detailLevel = 0)
        {
            Instance.LogInfoInstance(type, value, detailLevel);
        }
        public static void LogInfo(LogType type, List<string> value, int detailLevel = 0)
        {
            if (value.Count == 0)
            {
                return;
            }
            string sendValue = value[0];
            for (int i = 1; i < value.Count; i++)
            {
                sendValue += "\n" + value[i];
            }
            Instance.LogInfoInstance(type, sendValue, detailLevel);
        }

    }
}