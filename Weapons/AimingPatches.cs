using Aki.Reflection.Patching;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
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
        public static float consoomTimer = 0f;

        public static void ADSCheck(Player player, EFT.Player.FirearmController fc)
        {
            if (player.IsYourPlayer && fc.Item != null)
            {
                FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                ThermalVisionComponent thermComponent = player.ThermalVisionObserver.Component;
                bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                bool thermalIsOn = thermComponent != null && (thermComponent.Togglable == null || thermComponent.Togglable.On);
                bool gearBlocksADS = !WeaponStats.WeaponCanFSADS && !PlayerState.GearAllowsADS;
                bool fsBlocksADS = Plugin.EnableFSPatch.Value && ((fsIsON && gearBlocksADS) || (gearBlocksADS && (fsComponent.Togglable == null || fsComponent == null)));
                bool toobBlocksADS = Plugin.EnableNVGPatch.Value && ((nvgIsOn && player.ProceduralWeaponAnimation.CurrentScope.IsOptic) || thermalIsOn);

                PlayerState.FSIsActive = fsIsON;
                PlayerState.NVGIsActive = nvgIsOn || thermalIsOn;

                if (HeadDeviceStateChanged) 
                {
                    StatCalc.GetGearPenalty(Utils.GetYourPlayer());
                    HeadDeviceStateChanged = false;
                }

                fc.UpdateHipInaccuracy(); //update hipfire to take NVG toggle into account

                if (Plugin.ServerConfig.enable_stances)
                {
                    if ((toobBlocksADS || fsBlocksADS))
                    {
                        if (!hasSetCanAds)
                        {
                            if (fc.IsAiming)
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

                    if (StanceController.CurrentStance == EStance.ActiveAiming && !hasSetActiveAimADS)
                    {
                        player.MovementContext.SetAimingSlowdown(true, 0.33f);
                        hasSetActiveAimADS = true;
                    }
                    else if (StanceController.CurrentStance != EStance.ActiveAiming && hasSetActiveAimADS)
                    {
                        player.MovementContext.SetAimingSlowdown(false, 0.33f);
                        if (fc.IsAiming)
                        {
                            player.MovementContext.SetAimingSlowdown(true, 0.33f);
                        }

                        hasSetActiveAimADS = false;
                    }

                    if (fc.IsAiming && StanceController.CurrentStance == EStance.PatrolStance)
                    {
                        StanceController.CurrentStance = EStance.None;
                    }

                    /*          if (isAiming && StanceController.CurrentStance == EStance.Melee)
                              {
                                  fc.ToggleAim();
                              }*/

                }

                if (player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire)
                {
                    StanceController.IsAiming = fc.IsAiming;
                    StanceController.PistolIsColliding = false;
                }
                else if (WeaponStats.IsStocklessPistol)
                {
                    StanceController.PistolIsColliding = true;
                }

                if (PlayerState.BlockFSWhileConsooming) 
                {
                    consoomTimer += Time.deltaTime;
                    if (consoomTimer >= 1f)
                    {
                        PlayerState.BlockFSWhileConsooming = false;
                        consoomTimer = 0f;
                    }
                }
            }
        }
    }

    //to prevent players toggling on device while drinking/eating to bypass restriction
    //look for ecommand.togglegoggles
    public class ToggleHeadDevicePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("method_17", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Player __instance)
        {
            if (__instance.IsYourPlayer)
            {
                Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
                if (fc != null)
                {
                    return fc.FirearmsAnimator.IsIdling() && !PlayerState.BlockFSWhileConsooming;
                };
                return !PlayerState.BlockFSWhileConsooming;
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
                    float totalSpeed = StanceController.CurrentStance == EStance.ActiveAiming ? baseSpeed * 1.45f : baseSpeed;
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