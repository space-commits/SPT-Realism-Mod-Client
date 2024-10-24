﻿using EFT;
using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static ObjectInHandsAnimator;

namespace RealismMod
{
    public static class GameWorldController
    {
        public static bool DidExplosionClientSide { get; set; } = false;
        public static bool GameStarted { get; set; } = false;
        public static bool RanEarliestGameCheck { get; set; } = false;
        public static bool IsMapThatCanDoGasEvent { get; set; } = false;
        public static bool IsMapThatCanDoRadEvent { get; set; } = false;
        public static string CurrentMap { get; set; } = "";
        public static bool MapWithDynamicWeather { get; set; } = false;
        public static float CurrentMapRadStrength { get; private set; } = 0;
        public static float CurrentGasEventStrength { get; private set; } = 0;
        public static float CurrentGasEventStrengthBot { get; private set; } = 0;
        public static List<LampController> Lights { get; set; } = new List<LampController>();
        public static bool IsRightDateForExp { get; private set; }

        public static bool DoMapGasEvent
        {
            get
            {
                return IsMapThatCanDoGasEvent && Plugin.ModInfo.DoGasEvent && !Plugin.ModInfo.HasExploded && !DidExplosionClientSide && !(Plugin.ModInfo.IsPreExplosion && IsRightDateForExp);
            }

        }

        public static bool DoMapRads
        {
            get
            {
                return IsMapThatCanDoRadEvent && Plugin.ModInfo.HasExploded;
            }

        }

        public static bool MuteAmbientAudio
        {
            get
            {
                return Plugin.ModInfo.DoGasEvent || (Plugin.ModInfo.IsPreExplosion && GameWorldController.IsRightDateForExp) || DidExplosionClientSide || DoMapRads || Plugin.ModInfo.HasExploded;
            }
        }

        public static void ClearGameObjectLists() 
        {
            Lights.Clear(); 
        }

        public static void CalculateGasEventStrength() 
        {
            float fogStrength = Plugin.RealismWeatherComponent.TargetFog;
            float targetStrength = Mathf.Max(fogStrength * 2.2f, 0.06f);
            targetStrength = PlayerState.BtrState == EPlayerBtrState.Inside ? 0f : targetStrength;
            CurrentGasEventStrengthBot = targetStrength;
            CurrentGasEventStrength = Mathf.Lerp(CurrentGasEventStrength, targetStrength * (PlayerState.EnviroType == EnvironmentType.Indoor ? 0.5f : 1f), 0.05f);
        }

        public static void CalculateMapRadStrength()
        {
            float rainStrength = PlayerState.BtrState == EPlayerBtrState.Inside || PlayerState.EnviroType == EnvironmentType.Indoor ? 0f : Plugin.RealismWeatherComponent.TargetRain * 0.13f;
            float targetStrength = 0.1f + rainStrength;
            targetStrength = PlayerState.BtrState == EPlayerBtrState.Inside ? targetStrength * 0.25f : PlayerState.EnviroType == EnvironmentType.Indoor ? 0.5f : targetStrength;
            CurrentMapRadStrength = Mathf.Lerp(CurrentMapRadStrength, targetStrength, 0.05f);
        }

        public static void CheckDate() 
        {
            DateTime utcNow = DateTime.UtcNow;
            IsRightDateForExp = PluginConfig.ZoneDebug.Value || (utcNow.Month == 10 && utcNow.Day >= 31) || (utcNow.Month == 11 && utcNow.Day <= 5);
        }

        public static void GameWorldUpdate() 
        {
            if (DoMapGasEvent) CalculateGasEventStrength();
            if (DoMapRads) CalculateMapRadStrength();
        }

        public static void RunEarlyGameCheck()
        {
            if (!GameWorldController.RanEarliestGameCheck)
            {
                GameWorldController.CheckDate();
                Plugin.RequestRealismDataFromServer(EUpdateType.ModInfo);
                GameWorldController.RanEarliestGameCheck = true;
            }
        }
    
    }
}
