using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InputSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;



namespace RealismMod
{
    public static class AimController 
    {
        private static bool hasSetCanAds = false;
        private static bool hasSetActiveAimADS = false;
        private static bool wasToggled = false;

        public static void ADSCheck(Player player, EFT.Player.FirearmController fc, ManualLogSource logger)
        {
            if (!player.IsAI && fc.Item != null)
            {
                bool isAiming = (bool)AccessTools.Field(typeof(EFT.Player.FirearmController), "_isAiming").GetValue(fc);
                FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                bool gearBlocksADS = Plugin.EnableFSPatch.Value && fsIsON && (!WeaponProperties.WeaponCanFSADS && (!GearProperties.AllowsADS(fsComponent.Item) || !PlayerProperties.GearAllowsADS));
                if (Plugin.ModConfig.recoil_attachment_overhaul && ((Plugin.EnableNVGPatch.Value && nvgIsOn && Plugin.HasOptic) || gearBlocksADS))
                {
                    if (!hasSetCanAds)
                    {
                        PlayerProperties.IsAllowedADS = false;
                        player.ProceduralWeaponAnimation.IsAiming = false;
                        AccessTools.Field(typeof(EFT.Player.FirearmController), "_isAiming").SetValue(fc, false);
                        hasSetCanAds = true;
                    }
                }
                else
                { 
                    PlayerProperties.IsAllowedADS = true;
                    hasSetCanAds = false;
                }

                if (StanceController.IsActiveAiming && !isAiming)
                {
                    if (!hasSetActiveAimADS)
                    {
                        PlayerProperties.IsAllowedADS = false;
                        player.ProceduralWeaponAnimation.IsAiming = false;
                        AccessTools.Field(typeof(EFT.Player.FirearmController), "_isAiming").SetValue(fc, false);
                        player.MovementContext.SetAimingSlowdown(true, 0.33f);
                        hasSetActiveAimADS = true;
                    }

                }
                if (!StanceController.IsActiveAiming && hasSetActiveAimADS)
                {
                    player.MovementContext.SetAimingSlowdown(false, 0.33f);
                    hasSetActiveAimADS = false;
                }

                if (isAiming)
                {
                    player.MovementContext.SetAimingSlowdown(true, 0.33f);
                }

                if (!wasToggled && (fsIsON || nvgIsOn)) 
                {
                    wasToggled = true;
                }
                if (wasToggled == true && (!fsIsON && !nvgIsOn))
                {
                    StanceController.WasActiveAim = false;
                    if (Plugin.ToggleActiveAim.Value)
                    {
                        Plugin.StanceBlender.Target = 0f;
                        StanceController.IsActiveAiming = false;
                    }
                    wasToggled = false;
                }

                if (player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire)
                {
                    Plugin.IsAiming = isAiming;
                    StanceController.PistolIsColliding = false;
                }
                else if (fc.Item.WeapClass == "pistol")
                {
                    StanceController.PistolIsColliding = true;
                }

            }
        }
    }


    public class ToggleHoldingBreathPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("ToggleHoldingBreath", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance)
        {
            if (__instance.IsYourPlayer == true)
            {
                if (!Plugin.EnableHoldBreath.Value)
                {
                    return false;
                }
                return true;
            }
            return true;
        }
    }

    public class SetAimingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("set_IsAiming", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance, bool value, ref bool ____isAiming)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
            if (player.IsYourPlayer && __instance.Item.WeapClass == "pistol")
            {
                player.Physical.Aim((!____isAiming || !(player.MovementContext.StationaryWeapon == null)) ? 0f : __instance.ErgonomicWeight * 0.2f);
            }
        }
    }

    public class ToggleAimPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("ToggleAim", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);

            if (player.IsYourPlayer) 
            {
                bool gearFactorEnabled = Plugin.EnableFSPatch.Value || Plugin.EnableNVGPatch.Value;

                if (PlayerProperties.SprintBlockADS && !PlayerProperties.TriedToADSFromSprint) 
                {
                    PlayerProperties.TriedToADSFromSprint = true;
                    return false;
                }

                PlayerProperties.TriedToADSFromSprint = false;
                return gearFactorEnabled ? PlayerProperties.IsAllowedADS : true;
            }
            return true;
        }
    }
}