using HarmonyLib;
using PracticeMode.Hooks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace PracticeMode.Patches
{
    internal class PracticeModeMenu
    {
        public static bool IsInPracticeMode = false;

        public static void ChangeScenePracticeMode()
        {
            TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MySceneManager.ChangeRelayScene("SongSelect", true);

            Plugin.Instance.StartCoroutine(CreatePracticeModeScene());
        }

        private static IEnumerator CreatePracticeModeScene()
        {
            IsInPracticeMode = true;

            while (!TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MySceneManager.IsSceneChanged || TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MySceneManager.CurrentSceneName != "SongSelect")
            {
                yield return null;
            }

        }
    }
}
