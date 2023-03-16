using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static EFT.Player;


namespace RealismMod
{

    public class PlayerInitPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {

            if (__instance.IsYourPlayer == true)
            {
                StatCalc.SetGearParamaters(__instance);
                StanceController.SelectedStance = 0;
                StanceController.IsLowReady = false;
                StanceController.IsHighReady = false;
                StanceController.IsActiveAiming = false;
                StanceController.WasHighReady = false;
                StanceController.WasLowReady = false;
                StanceController.IsShortStock = false;

            }
        }
    }

    //need to get non-armor chest rig reload multi, also check if any armor component should disable ADS
    public class OnItemAddedOrRemovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnItemAddedOrRemoved", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {

            if (__instance.IsYourPlayer == true)
            {
                StatCalc.SetGearParamaters(__instance);
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

    public class PlayerLateUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (Utils.CheckIsReady() == true && __instance.IsYourPlayer == true)
            {
                Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
                PlayerInjuryStateCheck(__instance, Logger);
                Plugin.IsSprinting = __instance.IsSprintEnabled;

                PlayerProperties.enviroType = __instance.Environment;

                if (fc != null)
                {
                    ReloadStateCheck(__instance, fc);

                    if (Plugin.EnableStanceStamChanges.Value == true) 
                    {
                        StanceController.SetStanceStamina(__instance, fc);
                    }

                    PlayerProperties.RemainingArmStamPercentage = Mathf.Min(__instance.Physical.HandsStamina.Current * 1.75f, __instance.Physical.HandsStamina.TotalCapacity) / __instance.Physical.HandsStamina.TotalCapacity;
                }
                else if(Plugin.EnableStanceStamChanges.Value == true)
                {
                    StanceController.ResetStanceStamina(__instance);
                }

                __instance.Physical.HandsStamina.Current = Mathf.Max(__instance.Physical.HandsStamina.Current, 1f);
            }
        }


        public static void PlayerInjuryStateCheck(Player player, ManualLogSource logger)
        {
            bool rightArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool leftArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged);
            bool tremor = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.Tremor);

            PlayerProperties.RightArmBlacked = rightArmDamaged;
            PlayerProperties.LeftArmBlacked = leftArmDamaged;

            if (!rightArmDamaged && !leftArmDamaged && !tremor)
            {
                PlayerProperties.AimMoveSpeedBase = 0.42f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1f;
                PlayerProperties.ADSInjuryMulti = 1f;
                PlayerProperties.ReloadInjuryMulti = 1f;
                PlayerProperties.RecoilInjuryMulti = 1f;
            }
            if (tremor == true)
            {
                PlayerProperties.AimMoveSpeedBase = 0.4f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1.15f;
                PlayerProperties.ADSInjuryMulti = 0.85f;
                PlayerProperties.ReloadInjuryMulti = 0.9f;
                PlayerProperties.RecoilInjuryMulti = 1.025f;
            }
            if ((rightArmDamaged == true && !leftArmDamaged))
            {
                PlayerProperties.AimMoveSpeedBase = 0.38f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1.5f;
                PlayerProperties.ADSInjuryMulti = 0.6f;
                PlayerProperties.ReloadInjuryMulti = 0.85f;
                PlayerProperties.RecoilInjuryMulti = 1.05f;
            }
            if ((!rightArmDamaged && leftArmDamaged == true))
            {
                PlayerProperties.AimMoveSpeedBase = 0.34f;
                PlayerProperties.ErgoDeltaInjuryMulti = 2f;
                PlayerProperties.ADSInjuryMulti = 0.7f;
                PlayerProperties.ReloadInjuryMulti = 0.8f;
                PlayerProperties.RecoilInjuryMulti = 1.1f;
            }
            if (rightArmDamaged == true && leftArmDamaged == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    logger.LogWarning("both arms damaged");
                }
                PlayerProperties.AimMoveSpeedBase = 0.3f;
                PlayerProperties.ErgoDeltaInjuryMulti = 3.5f;
                PlayerProperties.ADSInjuryMulti = 0.5f;
                PlayerProperties.ReloadInjuryMulti = 0.75f;
                PlayerProperties.RecoilInjuryMulti = 1.15f;
            }
        }

        public static void ReloadStateCheck(Player player, EFT.Player.FirearmController fc)
        {
            PlayerProperties.IsInReloadOpertation = fc.IsInReloadOperation();

            if (PlayerProperties.IsInReloadOpertation == true)
            {
                if (PlayerProperties.IsAttemptingToReloadInternalMag == true)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("IsAttemptingToReloadInternalMag = " + Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * Plugin.InternalMagReloadMulti.Value * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.5f)), 0.6f, 1.25f));

                    }
                    player.HandsAnimator.SetAnimationSpeed(Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * Plugin.InternalMagReloadMulti.Value * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.5f)), 0.6f, 1.25f));
                }
            }
            else
            {
                PlayerProperties.IsAttemptingToReloadInternalMag = false;
                PlayerProperties.IsAttemptingRevolverReload = false;
            }
        }
    }
}

