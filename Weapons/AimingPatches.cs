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
        public static bool AimStateChanged = false;
        public static bool HeadDeviceStateChanged = false;

        public static void ADSCheck(Player player, EFT.Player.FirearmController fc)
        {
            if (player.IsYourPlayer && fc.Item != null)
            {
                bool isAiming = (bool)AccessTools.Field(typeof(EFT.Player.FirearmController), "_isAiming").GetValue(fc);
                if (AimStateChanged || HeadDeviceStateChanged) 
                {
                    Utils.Logger.LogWarning("ADS Check");
                    FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                    NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                    ThermalVisionComponent thermComponent = player.ThermalVisionObserver.Component;
                    bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                    bool thermalIsOn = thermComponent != null && (thermComponent.Togglable == null || thermComponent.Togglable.On);
                    bool gearBlocksADS = Plugin.EnableFSPatch.Value && fsIsON && (!WeaponStats.WeaponCanFSADS && (!GearStats.AllowsADS(fsComponent.Item) || !PlayerState.GearAllowsADS));
                    bool toobBlocksADS = Plugin.EnableNVGPatch.Value && ((nvgIsOn && WeaponStats.HasOptic) || thermalIsOn);
                    if (Plugin.ServerConfig.recoil_attachment_overhaul && (toobBlocksADS || gearBlocksADS))
                    {
                        if (!hasSetCanAds)
                        {
                            if (isAiming)
                            {
                                fc.ToggleAim();
                            }
                            PlayerState.IsAllowedADS = false;
                            hasSetCanAds = true;
                        }
                    }
                    else
                    {
                        PlayerState.IsAllowedADS = true;
                        hasSetCanAds = false;
                    }
                    //no idea wtf this is
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
                            StanceController.CurrentStance = EStance.None;
                        }
                        wasToggled = false;
                    }

                    AimStateChanged = false;
                    HeadDeviceStateChanged = false;
                }

                if (StanceController.CurrentStance == EStance.IsActiveAiming && !hasSetActiveAimADS)
                {
                    player.MovementContext.SetAimingSlowdown(true, 0.33f);
                    hasSetActiveAimADS = true;
                }
                else if (StanceController.CurrentStance != EStance.IsActiveAiming && hasSetActiveAimADS)
                {
                    player.MovementContext.SetAimingSlowdown(false, 0.33f);
                    if (isAiming)
                    {
                        player.MovementContext.SetAimingSlowdown(true, 0.33f);
                    }

                    hasSetActiveAimADS = false;
                }

                if (isAiming || StanceController.CurrentStance == EStance.IsMeleeAttack)
                {
                    StanceController.CurrentStance = EStance.IsPatrolStance;
                }

                if (StanceController.CurrentStance == EStance.IsMeleeAttack && isAiming)
                {
                    fc.ToggleAim();
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

    //to prevent players toggling on device while drinking/eating to bypass restriction
    //look for ecommand.togglegoggles
    public class ToggleHeadDevicePatch : ModulePatch
    {
        private static FieldInfo playerField;
        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(MovementContext), "_player");
            return typeof(Player).GetMethod("method_17", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance)
        {
            if (__instance.IsYourPlayer == true)
            {
                Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
                bool isIdling = fc.FirearmsAnimator.IsIdling();
                return isIdling;
            }
            return true;
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
                    float baseSpeed = PlayerState.AimMoveSpeedBase * WeaponStats.AimMoveSpeedWeapModifier * PlayerState.AimMoveSpeedInjuryMulti;
                    float totalSpeed = StanceController.CurrentStance == EStance.IsActiveAiming ? baseSpeed * 1.3f : baseSpeed;
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
            Logger.LogWarning("set_IsAiming");
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
                Utils.Logger.LogWarning("Toggle ADS");
                AimController.AimStateChanged = true;
                bool gearFactorEnabled = Plugin.EnableFSPatch.Value || Plugin.EnableNVGPatch.Value;

                StanceController.CanResetAimDrain = true;

                if (PlayerState.SprintBlockADS && !PlayerState.TriedToADSFromSprint)
                {
                    PlayerState.TriedToADSFromSprint = true;
                    return false;
                }

                PlayerState.TriedToADSFromSprint = false;
                return gearFactorEnabled ? PlayerState.IsAllowedADS : true;
            }
            return true;
        }
    }
}