using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using UnityEngine;
using RaidStateClass = GClass2107;

namespace RealismMod
{
    public static class GameWorldController
    {
        public const float GAS_OPACITY_GLOBAL_MODI = 0.85f;
        public const float RAD_RAIN_MODI = 0.15f;
        public const float BASE_MAP_RAD_STRENGTH = 0.05f;
        public const float FOG_GAS_STRENGTH_MODI = 2.2f;
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
        public static List<ExfiltrationPoint> ExfilsInLocation { get; set; } = new List<ExfiltrationPoint>();
        public static bool IsRightDateForExp { get; private set; }
        public static float TimeInRaid { get; set; }
        public static GamePlayerOwner GamePlayerOwner { get; set; }

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

        public static bool IsInRaid()
        {
#pragma warning disable CS0618
            return RaidStateClass.InRaid;
#pragma warning disable CS0618
        }

        public static void Reset() 
        {
            Lights.Clear(); 
            ExfilsInLocation.Clear();
            GameStarted = false;
            RanEarliestGameCheck = false;
            TimeInRaid = 0f;
        }

        public static void CalculateGasEventStrength()
        {
            if (!DoMapGasEvent)
            {
                CurrentGasEventStrengthBot = 0f;
                CurrentGasEventStrength = 0f;
                return;
            }
            bool isIndoors = PlayerValues.EnviroType == EnvironmentType.Indoor;
            float enviroFactor = isIndoors ? 0.4f : 1f;
            float fogStrength = Plugin.RealismWeatherComponent.TargetFog;
            float baseStrength = fogStrength * FOG_GAS_STRENGTH_MODI;
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
            bool isIndoors = PlayerValues.EnviroType == EnvironmentType.Indoor;
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

        public static float GetHeadsetVolume() 
        {
            return PluginConfig.HeadsetGain.Value * 0.02f;
        }

        public static float GetGameVolumeAsFactor()
        {
            var instance = Singleton<SharedGameSettingsClass>.Instance;
            if (instance?.Sound?.Settings == null) return 1f;
            return instance.Sound.Settings.OverallVolume?.Value * 0.1f ?? 1f;
        }
        public static void ModifyLootResources(Item item) 
        {
            if (Plugin.ServerConfig.bot_loot_changes)
            {
                //if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"====item {item.LocalizedName()}===");

                ResourceComponent resourceComponent;
                if (item.TryGetItemComponent<ResourceComponent>(out resourceComponent))
                {
                    if (resourceComponent.MaxResource > 1)
                    {
                        resourceComponent.Value = Mathf.Round(resourceComponent.MaxResource * UnityEngine.Random.Range(0.35f, 1f));
                        if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"item {item.LocalizedName()}, value: {resourceComponent.Value}, max: {resourceComponent.MaxResource}");
                    }
                }
                FoodDrinkComponent foodComponent;
                if (item.TryGetItemComponent<FoodDrinkComponent>(out foodComponent))
                {
                    if (foodComponent.MaxResource > 1)
                    {
                        foodComponent.HpPercent = Mathf.Round(foodComponent.MaxResource * UnityEngine.Random.Range(0.35f, 1f));
                        if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"item {item.LocalizedName()}, value: {foodComponent.HpPercent}, max: {foodComponent.MaxResource}");
                    }
                }
                MedKitComponent medKitComponent;
                if (item.TryGetItemComponent<MedKitComponent>(out medKitComponent))
                {
                    if (medKitComponent.MaxHpResource > 1)
                    {
                        medKitComponent.HpResource = Mathf.Round(medKitComponent.MaxHpResource * UnityEngine.Random.Range(0.35f, 1f));
                        if (PluginConfig.ZoneDebug.Value) Utils.Logger.LogWarning($"item {item.LocalizedName()}, value: {medKitComponent.HpResource}, max: {medKitComponent.MaxHpResource}");
                    }
                }
            }
        }
    
    }
}
