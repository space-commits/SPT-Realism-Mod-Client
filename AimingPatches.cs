using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;
using UnityEngine;



namespace RealismMod
{



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
        }
    }

    public class GetAimingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("get_IsAiming", BindingFlags.Public | BindingFlags.Instance);
        }

        private static bool SetCanAds = false;
        private static bool SetActiveAimADS = false;
        private static bool SetRunAnim = false;
        private static bool ResetRunAnim = false;
        private static bool ToggledADS;

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance, ref bool ____isAiming)
        {
            if (Utils.IsReady == true)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
                if (!player.IsAI)
                {
                    FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                    NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                    bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                    if (((Plugin.EnableNVGPatch.Value == true && nvgIsOn == true && Plugin.HasOptic) || (Plugin.EnableFSPatch.Value == true && ((fsIsON && !WeaponProperties.WeaponCanFSADS && !ArmorProperties.AllowsADS(fsComponent.Item)) || (!PlayerProperties.GearAllowsADS && !WeaponProperties.WeaponCanFSADS)))))
                    {
                        if (!SetCanAds)
                        {
                            PlayerProperties.IsAllowedADS = false;
                            player.MovementContext.SetAimingSlowdown(false, 0.33f);
                            player.ProceduralWeaponAnimation.IsAiming = false;
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
                            player.MovementContext.SetAimingSlowdown(true, 0.33f);
                            SetActiveAimADS = true;
                        }

                    }
                    if (!StanceController.IsActiveAiming && !____isAiming)
                    {
                        player.MovementContext.SetAimingSlowdown(false, 0.33f);
                        SetActiveAimADS = false;
                    }



                    if (StanceController.IsHighReady == true || StanceController.WasHighReady == true)
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
                            player.BodyAnimatorCommon.SetFloat(GClass1645.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)__instance.Item.CalculateCellSize().X);
                            ResetRunAnim = true;
                            SetRunAnim = false;
                        }

                    }

      /*              if (!StanceController.CanADSFromStance && ____isAiming == true)
                    {
                        PlayerProperties.IsAllowedADS = false;
                        player.MovementContext.SetAimingSlowdown(false, 0.33f);
                        player.ProceduralWeaponAnimation.IsAiming = false;
                        ToggledADS = true;

                        Logger.LogWarning("CAN'T AIM!");
                    }
                    if (StanceController.CanADSFromStance && ToggledADS == true)
                    {
                        PlayerProperties.IsAllowedADS = true;
                        player.MovementContext.SetAimingSlowdown(true, 0.33f);
                        player.ProceduralWeaponAnimation.IsAiming = true;

                        Logger.LogWarning("CAN AIM!");

                        ToggledADS = false;
                    }*/

                    if (player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire) 
                    {
                        Plugin.IsAiming = ____isAiming;
                    }
                   
                }
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
            if (Plugin.EnableFSPatch.Value == true && !player.IsAI)
            {
                return PlayerProperties.IsAllowedADS;
            }
            return true;
        }
    }
}