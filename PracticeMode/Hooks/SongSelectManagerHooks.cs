using HarmonyLib;
using PracticeMode.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PracticeMode.Hooks
{
    internal class SongSelectManagerHooks
    {
        [HarmonyPatch(typeof(SongSelectManager))]
        [HarmonyPatch(nameof(SongSelectManager.Start))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPostfix]
        public static void SongSelectManager_Start_Postfix(SongSelectManager __instance)
        {
            if (PracticeModeMenu.IsInPracticeMode)
            {
                PracticeModeHooks.speed = 1;
            }
        }

        [HarmonyPatch(typeof(SongSelectBgImage))]
        [HarmonyPatch(nameof(SongSelectBgImage.Start))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPostfix]
        public static void SongSelectBgImage_Start_Postfix(SongSelectBgImage __instance)
        {
            if (PracticeModeMenu.IsInPracticeMode)
            {
                __instance.bgImageA.gameObject.SetActive(false);
                __instance.bgImageB.gameObject.SetActive(false);
                __instance.bgImageC.gameObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(SongSelectManager))]
        [HarmonyPatch(nameof(SongSelectManager.Update))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPrefix]
        public static bool SongSelectManager_Update_Prefix(SongSelectManager __instance)
        {
            if (PracticeModeMenu.IsInPracticeMode)
            {
                if (TaikoSingletonMonoBehaviour<ControllerManager>.Instance.GetCancelDown(ControllerManager.ControllerPlayerNo.Player1) &&
                    __instance.CurrentState == SongSelectManager.State.SongSelect)
                {
                    PracticeModeMenu.IsInPracticeMode = false;

                    TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MySoundManager.CommonSePlay("don", false, false);
                    TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MySceneManager.ChangeScene("SongSelect", false);

                    return false;
                }
            }
            return true;
        }
    }
}
