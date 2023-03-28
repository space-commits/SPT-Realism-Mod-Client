using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;
using UnityEngine;



namespace RealismMod
{
    public static class AimController 
    {
        private static bool SetCanAds = false;
        private static bool SetActiveAimADS = false;
        private static bool SetRunAnim = false;
        private static bool ResetRunAnim = false;

        private static bool wasToggled = false;

        public static void ADSCheck(Player player, EFT.Player.FirearmController fc)
        {
            if (!player.IsAI && fc.Item != null)
            {
                bool isAiming = (bool)AccessTools.Field(typeof(EFT.Player.FirearmController), "_isAiming").GetValue(fc);
                FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                if (((Plugin.EnableNVGPatch.Value == true && nvgIsOn == true && Plugin.HasOptic) || (Plugin.EnableFSPatch.Value == true && ((fsIsON && !WeaponProperties.WeaponCanFSADS && !ArmorProperties.AllowsADS(fsComponent.Item)) || (!PlayerProperties.GearAllowsADS && !WeaponProperties.WeaponCanFSADS)))))
                {
                    if (!SetCanAds)
                    {
                        PlayerProperties.IsAllowedADS = false;
                        player.ProceduralWeaponAnimation.IsAiming = false;
                        AccessTools.Field(typeof(EFT.Player.FirearmController), "_isAiming").SetValue(fc, false);
                        SetCanAds = true;
                    }
                }
                else
                {
                    PlayerProperties.IsAllowedADS = true;
                    SetCanAds = false;
                }

                if (StanceController.IsActiveAiming == true)
                {
                    if (!SetActiveAimADS)
                    {
                        PlayerProperties.IsAllowedADS = false;
                        player.ProceduralWeaponAnimation.IsAiming = false;
                        AccessTools.Field(typeof(EFT.Player.FirearmController), "_isAiming").SetValue(fc, false);
                        player.MovementContext.SetAimingSlowdown(true, 0.33f);
                        SetActiveAimADS = true;
                    }

                }
                if (!StanceController.IsActiveAiming && !isAiming)
                {
                    player.MovementContext.SetAimingSlowdown(false, 0.33f);
                    SetActiveAimADS = false;
                }

                if (!wasToggled && (fsIsON == true || nvgIsOn == true)) 
                {
                    wasToggled = true;
                }
                if (wasToggled == true && (!fsIsON && !nvgIsOn))
                {
                    StanceController.WasActiveAim = false;
                    if (Plugin.ToggleActiveAim.Value == true)
                    {
                        StanceController.IsActiveAiming = false;
                    }
                    wasToggled = false;
                }

                if ((StanceController.IsHighReady == true || StanceController.WasHighReady == true) && !PlayerProperties.RightArmBlacked)
                {
                    if (!SetRunAnim)
                    {
                        player.BodyAnimatorCommon.SetFloat(GClass1645.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2f);

                        SetRunAnim = true;
                        ResetRunAnim = false;
                    }

                }
                else
                {
                    if (!ResetRunAnim)
                    {
                        player.BodyAnimatorCommon.SetFloat(GClass1645.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)fc.Item.CalculateCellSize().X);
                        ResetRunAnim = true;
                        SetRunAnim = false;
                    }

                }

                if (player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire == true)
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
            if (__instance.Item.WeapClass == "pistol")
            {
                player.Physical.Aim((!____isAiming || !(player.MovementContext.StationaryWeapon == null)) ? 0f : __instance.ErgonomicWeight * 0.2f);
            }
            Logger.LogWarning("set_IsAiming");
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
            if (Plugin.EnableFSPatch.Value == true && !player.IsAI)
            {
                return PlayerProperties.IsAllowedADS;
            }
            return true;
        }
    }
}