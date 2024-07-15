using HarmonyLib;
using PracticeMode.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace PracticeMode.Hooks
{
    internal class PracticeModeHooks
    {
        public static float speed = 1.0f;

        public static void IncreaseGameSpeed()
        {
            if (speed < 1.0f)
            {
                SetGameSpeed(speed + 0.05f);
            }
        }
        public static void DecreaseGameSpeed()
        {
            if (speed > 0.5f)
            {
                SetGameSpeed(speed - 0.05f);
            }
        }

        static TextMeshProUGUI speedDisplay = null;
        public static void SetGameSpeed(float newSpeed)
        {
            speed = newSpeed;
            Plugin.Log.LogInfo("Speed: " + speed.ToString("0.00") + "x");
            if (speedDisplay != null)
            {
                speedDisplay.text = newSpeed.ToString("0.00") + "x";
            }
        }

        private static float GetPitchFromGameSpeed(float newSpeed)
        {
            return (float)(Math.Log(newSpeed) * (1200 / Math.Log(2)));
        }

        [HarmonyPatch(typeof(EnsoSound))]
        [HarmonyPatch(nameof(EnsoSound.PlaySong))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPostfix]
        public static void EnsoSound_PlaySong_Postfix(EnsoSound __instance)
        {
            if (PracticeModeMenu.IsInPracticeMode)
            {
                var player = __instance.songPlayer.Player;
                var playback = __instance.songPlayer.Playback;

                player.SetPitch(GetPitchFromGameSpeed(speed));
                player.Update(playback);

                if (speedDisplay == null)
                {
                    var speedObj = new GameObject("SpeedDisplay");
                    var parentObj = GameObject.Find("Canvas");
                    speedObj.transform.SetParent(parentObj.transform);
                    speedObj.transform.position = new Vector3(-475, 45);

                    FontTMPManager fontTMPMgr = TaikoSingletonMonoBehaviour<CommonObjects>.Instance.MyDataManager.FontTMPMgr;
                    var font = fontTMPMgr.GetDescriptionFontAsset(DataConst.FontType.EFIGS);
                    var fontMaterial = fontTMPMgr.GetDescriptionFontMaterial(DataConst.FontType.EFIGS, DataConst.DescriptionFontMaterialType.OutlineBlack025);

                    speedDisplay = speedObj.AddComponent<TextMeshProUGUI>();
                    speedDisplay.text = "1.00x";
                    speedDisplay.enableAutoSizing = true;
                    speedDisplay.fontSizeMax = 1000;
                    speedDisplay.verticalAlignment = VerticalAlignmentOptions.Middle;

                    speedDisplay.font = font;
                    speedDisplay.fontMaterial = fontMaterial;
                }
            }
        }

        [HarmonyPatch(typeof(EnsoSound))]
        [HarmonyPatch(nameof(EnsoSound.Initialize))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPostfix]
        public static void EnsoSound_Initialize_Postfix(EnsoSound __instance, ref EnsoData.Settings settings)
        {
            if (PracticeModeMenu.IsInPracticeMode)
            {
                __instance.songPlayer = new CriPlayer(false);
                __instance.songPlayer.CueSheetName = settings.songFilePath;
                __instance.songPlayer.Player.SetVolume(__instance.songVolume);
            }
        }

        private static long prevStartTime = 0;
        [HarmonyPatch(typeof(EnsoSound))]
        [HarmonyPatch(nameof(EnsoSound.PrepareSong))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPostfix]
        public static void EnsoSound_PrepareSong_Postfix(EnsoSound __instance, long startTimeMs)
        {
            if (PracticeModeMenu.IsInPracticeMode)
            {
                prevStartTime = startTimeMs;
                Plugin.LogInfo(LogType.Info, "Prepare Song");
            }
        }

        [HarmonyPatch(typeof(CriPlayer))]
        [HarmonyPatch(nameof(CriPlayer.GetPosition))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPostfix]
        public static void CriPlayer_GetPosition_Postfix(CriPlayer __instance, ref double __result)
        {
            if (PracticeModeMenu.IsInPracticeMode)
            {
                if (__instance.IsLoadSucceed)
                {
                    // Without multiplying by speed, the notes will be very choppy
                    var newTime = (__instance.Playback.GetTime() - prevStartTime) * speed;
                    __result = newTime + prevStartTime;
                    //Plugin.Log.LogInfo("GetPosition Current time: " + __result);
                    return;
                }
                __result = -1.0;
            }
        }

        [HarmonyPatch(typeof(EnsoGameManager))]
        [HarmonyPatch(nameof(EnsoGameManager.GetDeltaTimeMsec))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPostfix]
        public static void EnsoGameManager_GetDeltaTimeMsec_Postfix(EnsoGameManager __instance, ref double __result)
        {
            if (PracticeModeMenu.IsInPracticeMode)
            {
                __result = speed * (double)Time.deltaTime * 1000;
            }
        }

        // TODO: Make timeJump a config option
        static float timeJump = 40;
        static float bigTimeJump = 400;

        // idk if inputBuffer is even necessary. Left/Right should be as smooth as possible. 
        // I suppose limiting it to time makes it the same regardless of FPS though.
        static float inputBuffer = 0.0025f;
        static float speedChangeInputBuffer = 0.25f;
        static float currentBuffer = 0;


        static ControllerManager.Dir previousDir = ControllerManager.Dir.None;
        static float previousPressTime = 0;
        // doubleTapSpeed is in seconds
        static readonly float doubleTapSpeed = 0.15f;
        static bool hasLetGo = false;

        static bool PauseMenuVisible = true;
        static bool DontHideMenu = false;

        static GameObject pauseMenuObj = null;
        static EnsoPauseMenu pauseMenu = null;

        [HarmonyPatch(typeof(EnsoGameManager))]
        [HarmonyPatch(nameof(EnsoGameManager.Update))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPostfix]
        public static void EnsoGameManager_Update_Postfix(EnsoGameManager __instance)
        {
            if (PracticeModeMenu.IsInPracticeMode)
            {
                if (currentBuffer != 0)
                {
                    currentBuffer -= Time.deltaTime;
                    currentBuffer = Math.Max(currentBuffer, 0);
                }
                if (__instance.ensoParam.IsPause)
                {
                    if (pauseMenuObj == null)
                    {
                        pauseMenuObj = GameObject.Find("CanvasPause");
                    }
                    if (pauseMenu == null)
                    {
                        var tmpObj = GameObject.Find("PauseMenu");
                        pauseMenu = tmpObj.GetComponent<EnsoPauseMenu>();
                    }

                    ControllerManager.Dir dir = TaikoSingletonMonoBehaviour<ControllerManager>.Instance.GetDirectionButton(ControllerManager.ControllerPlayerNo.Player1, ControllerManager.Prio.None, false);
                    if (dir == ControllerManager.Dir.None)
                    {
                        currentBuffer = 0;
                        if (Time.time - previousPressTime >= doubleTapSpeed)
                        {
                            hasLetGo = false;
                        }
                        else
                        {
                            hasLetGo = true;
                        }
                    }
                    if (!PauseMenuVisible)
                    {
                        if ((dir == ControllerManager.Dir.Left || dir == ControllerManager.Dir.Right) && currentBuffer == 0)
                        {
                            currentBuffer = inputBuffer;

                            bool fastScroll = false;
                            if (dir == previousDir && Time.time - previousPressTime < doubleTapSpeed && hasLetGo)
                            {
                                fastScroll = true;
                            }

                            previousPressTime = Time.time;
                            previousDir = dir;

                            var jump = timeJump;
                            if (fastScroll)
                            {
                                jump = bigTimeJump;
                            }
                            if (dir == ControllerManager.Dir.Left)
                            {
                                jump *= -1;
                            }
                            __instance.ensoParam.TotalTime += jump;
                            __instance.totalTime = __instance.ensoParam.TotalTime;
                            __instance.taikoCorePlayer.ResetToRetry();
                            __instance.taikoCorePlayer.Update(__instance.ensoParam.TotalTime);
                        }
                        if ((dir == ControllerManager.Dir.Up || dir == ControllerManager.Dir.Down) && currentBuffer == 0)
                        {
                            currentBuffer = speedChangeInputBuffer;

                            if (dir == ControllerManager.Dir.Up)
                            {
                                IncreaseGameSpeed();
                            }
                            else if (dir == ControllerManager.Dir.Down)
                            {
                                DecreaseGameSpeed();
                            }
                        }
                        if (TaikoSingletonMonoBehaviour<ControllerManager>.Instance.GetOkDown(ControllerManager.ControllerPlayerNo.Player1) && currentBuffer == 0)
                        {
                            __instance.ensoParam.PauseResult = EnsoPlayingParameter.PauseResults.Continue;
                            PauseMenuVisible = true;
                        }
                        if (TaikoSingletonMonoBehaviour<ControllerManager>.Instance.GetMenuDown(ControllerManager.ControllerPlayerNo.Player1) && currentBuffer == 0)
                        {
                            var canvas = pauseMenuObj.GetComponent<Canvas>();
                            canvas.scaleFactor = 1f;
                            PauseMenuVisible = true;
                            DontHideMenu = true;
                            pauseMenu.SetIgnoreUiTime();
                        }
                    }
                    else
                    {
                        if (TaikoSingletonMonoBehaviour<ControllerManager>.Instance.GetMenuDown(ControllerManager.ControllerPlayerNo.Player1) && currentBuffer == 0)
                        {
                            if (pauseMenuObj != null && !DontHideMenu)
                            {
                                var canvas = pauseMenuObj.GetComponent<Canvas>();
                                canvas.scaleFactor = 0f;
                                PauseMenuVisible = false;
                                // This basically has to be so large that it will never reach 0
                                // Or large enough where you'd need to keep the game up overnight for it to reach 0
                                // In which case, I blame you, rather than me
                                pauseMenu.ignoreUiControl = 500000f;
                            }
                        }
                    }
                }
                else
                {
                    DontHideMenu = false;
                }
            }
        }


        [HarmonyPatch(typeof(EnsoGameManager))]
        [HarmonyPatch(nameof(EnsoGameManager.CheckEnsoEnd))]
        [HarmonyPatch(MethodType.Normal)]
        [HarmonyPrefix]
        public static bool EnsoGameManager_CheckEnsoEnd_Prefix(EnsoGameManager __instance)
        {
            if (PracticeModeMenu.IsInPracticeMode)
            {
                TaikoCoreFrameResults frameResults = __instance.taikoCorePlayer.GetFrameResults();
                if (__instance.ensoParam.EnsoEndType == EnsoPlayingParameter.EnsoEndTypes.None && frameResults.isPastLastOnpuJustTime)
                {
                    if (frameResults.totalTime >= frameResults.fumenLength + 1000)
                    {
                        __instance.ensoParam.IsPause = true;
                    }
                }

                // We never want the song to fully end while in Practice mode
                if (__instance.ensoParam.EnsoEndType == EnsoPlayingParameter.EnsoEndTypes.None)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return true;
        }
    }
}
