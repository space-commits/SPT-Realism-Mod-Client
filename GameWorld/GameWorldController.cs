using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public static class GameWorldController
    {
        public static bool GameStarted { get; set; } = false;
        public static string CurrentMap { get; set; } = "";
        public static bool MapWithDynamicWeather { get; set; } = false;
        public static float CurrentGasEventStrength { get; private set; } = 0;
        public static float CurrentGasEventStrengthBot { get; private set; } = 0;


        public static bool IsHalloween
        {
            get 
            {
                return Plugin.ModInfo.IsHalloween;
            }

        }

        public static bool DoMapGasEvent
        {
            get
            {
                return Plugin.ModInfo.DoGasEvent;
            }

        }

        public static bool MuteAmbientAudio
        {
            get
            {
                return DoMapGasEvent || HazardTracker.IsPreExplosion || HazardTracker.HasExploded;
            }
        }

        public static void CalculateGasEventStrength() 
        {
            float fogStrength = Plugin.RealismWeatherComponent.TargetFog;
            float targetStrength = fogStrength * 0.5f;
            CurrentGasEventStrengthBot = targetStrength;
            CurrentGasEventStrength = Mathf.Lerp(CurrentGasEventStrength, targetStrength * (PlayerState.EnviroType == EnvironmentType.Indoor ? 0.5f : 1f), 0.05f);
        }

        public static void GameWorldUpdate() 
        {
            if (DoMapGasEvent) CalculateGasEventStrength();
        }
    
    }
}
