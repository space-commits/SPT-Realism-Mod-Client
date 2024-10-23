using EFT;
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
        public const float RAD_RAIN_MODI = 0.15f;
        public const float BASE_MAP_RAD_STRENGTH = 0.05f;
        public const float FOG_GAS_MODI = 2.2f;
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
            if (!DoMapGasEvent)
            {
                CurrentGasEventStrengthBot = 0f;
                CurrentGasEventStrength = 0f;
                return;
            }
            bool isIndoors = PlayerState.EnviroType == EnvironmentType.Indoor;
            float enviroFactor = isIndoors ? 0.4f : 1f;
            float fogStrength = Plugin.RealismWeatherComponent.TargetFog;
            float baseStrength = fogStrength * FOG_GAS_MODI;
            baseStrength = Mathf.Clamp(baseStrength, 0.06f, 0.2f);
            float playerStrength = baseStrength * enviroFactor;
            CurrentGasEventStrengthBot = baseStrength;
            CurrentGasEventStrength = Mathf.Lerp(CurrentGasEventStrength, playerStrength, 0.025f);
        }

        public static void CalculateMapRadStrength()
        {
            if (!DoMapRads) 
            {
                CurrentMapRadStrength = 0f;
                return;
            }
            bool isIndoors = PlayerState.EnviroType == EnvironmentType.Indoor;
            float enviroFactor = isIndoors ? 0.4f : 1f;
            float rainStrength = isIndoors ? 0f : Plugin.RealismWeatherComponent.TargetRain * 0.15f;
            float baseStrength = BASE_MAP_RAD_STRENGTH * enviroFactor;
            float targetStrength = Mathf.Clamp(baseStrength + rainStrength, 0.01f, 0.16f);
            CurrentMapRadStrength = Mathf.Lerp(CurrentMapRadStrength, targetStrength, 0.025f);
        }

        public static void CheckDate() 
        {
            DateTime utcNow = DateTime.UtcNow;
            IsRightDateForExp = PluginConfig.ZoneDebug.Value || (utcNow.Month == 10 && utcNow.Day >= 31) || (utcNow.Month == 11 && utcNow.Day <= 5);
        }

        public static void GameWorldUpdate() 
        {
            CalculateGasEventStrength();
            CalculateMapRadStrength();
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
