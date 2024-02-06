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

        public static void ADSCheck(Player player, EFT.Player.FirearmController fc)
        {
            if (!player.IsAI && fc.Item != null)
            {
                bool isAiming = (bool)AccessTools.Field(typeof(EFT.Player.FirearmController), "_isAiming").GetValue(fc);
                FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                ThermalVisionComponent thermComponent = player.ThermalVisionObserver.Component;
                bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                bool thermalIsOn = thermComponent != null && (thermComponent.Togglable == null || thermComponent.Togglable.On);
                bool gearBlocksADS = Plugin.EnableFSPatch.Value && fsIsON && (!WeaponStats.WeaponCanFSADS && (!GearStats.AllowsADS(fsComponent.Item) || !PlayerStats.GearAllowsADS));
                bool visionDeviceBlocksADS = Plugin.EnableNVGPatch.Value && ((nvgIsOn && WeaponStats.HasOptic) || thermalIsOn);
                if (Plugin.ServerConfig.recoil_attachment_overhaul && (visionDeviceBlocksADS || gearBlocksADS))
                {
                    if (!hasSetCanAds)
                    {
                        if (isAiming)
                        {
                            fc.ToggleAim();
                        }
                        PlayerStats.IsAllowedADS = false;
                        hasSetCanAds = true;
                    }
                }
                else
                {
                    PlayerStats.IsAllowedADS = true;
                    hasSetCanAds = false;
                }


                if (StanceController.IsActiveAiming && !hasSetActiveAimADS)
                {
                    player.MovementContext.SetAimingSlowdown(true, 0.33f);
                    hasSetActiveAimADS = true;
                }
                else if (!StanceController.IsActiveAiming && hasSetActiveAimADS)
                {
                    player.MovementContext.SetAimingSlowdown(false, 0.33f);
                    if (isAiming)
                    {
                        player.MovementContext.SetAimingSlowdown(true, 0.33f);
                    }

                    hasSetActiveAimADS = false;
                }


                if (isAiming || StanceController.IsMeleeAttack)
                {
                    StanceController.IsPatrolStance = false;
                }

                if (StanceController.IsMeleeAttack && isAiming)
                {
                    fc.ToggleAim();
                }

                if (!wasToggled && (fsIsON || nvgIsOn))
                {
                    wasToggled = true;
                }
                if (wasToggled && (!fsIsON && !nvgIsOn))
                {
                    StanceController.WasActiveAim = false;
                    if (Plugin.ToggleActiveAim.Value)
                    {
                        StanceController.StanceBlender.Target = 0f;
                        StanceController.IsActiveAiming = false;
                    }
                    wasToggled = false;
                }

                if (player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire)
                {
                    StanceController.IsAiming = isAiming;
                    StanceController.PistolIsColliding = false;
                }
                else if (fc.Item.WeapClass == "pistol")
                {
                    StanceController.PistolIsColliding = true;
                }

            }
        }
    }

    public class SetAimingSlowdownPatch : ModulePatch
    {
        private static FieldInfo playerField;
        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(MovementContext), "_player");
            return typeof(MovementContext).GetMethod("SetAimingSlowdown", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, bool isAiming)
        {

            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (isAiming)
                {
                    //slow is hard set to 0.33 when called, 0.4-0.43 feels best.
                    float baseSpeed = PlayerStats.AimMoveSpeedBase * WeaponStats.AimMoveSpeedWeapModifier * PlayerStats.AimMoveSpeedInjuryMulti;
                    float totalSpeed = StanceController.IsActiveAiming ? baseSpeed * 1.3f : baseSpeed;
                    totalSpeed = WeaponStats._WeapClass == "pistol" ? totalSpeed + 0.15f : totalSpeed;
                    __instance.AddStateSpeedLimit(Mathf.Clamp(totalSpeed, 0.3f, 0.9f), Player.ESpeedLimit.Aiming);
                    return false;
                }
                __instance.RemoveStateSpeedLimit(Player.ESpeedLimit.Aiming);
                return false;
            }
            return true;
        }
    }

    public class SetAimingPatch : ModulePatch
    {
        private static FieldInfo playerField;
        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player");
            return typeof(EFT.Player.FirearmController).GetMethod("set_IsAiming", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance, bool value, ref bool ____isAiming)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer && __instance.Item.WeapClass == "pistol")
            {
                player.Physical.Aim((!____isAiming || !(player.MovementContext.StationaryWeapon == null)) ? 0f : __instance.ErgonomicWeight * 0.2f);
            }
        }
    }

    public class ToggleAimPatch : ModulePatch
    {
        private static FieldInfo playerField;
        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player");
            return typeof(EFT.Player.FirearmController).GetMethod("ToggleAim", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);

            if (player.IsYourPlayer)
            {
                bool gearFactorEnabled = Plugin.EnableFSPatch.Value || Plugin.EnableNVGPatch.Value;

                StanceController.CanResetAimDrain = true;

                if (PlayerStats.SprintBlockADS && !PlayerStats.TriedToADSFromSprint)
                {
                    PlayerStats.TriedToADSFromSprint = true;
                    return false;
                }

                PlayerStats.TriedToADSFromSprint = false;
                return gearFactorEnabled ? PlayerStats.IsAllowedADS : true;
            }
            return true;
        }
    }
}