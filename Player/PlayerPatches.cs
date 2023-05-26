using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using BepInEx.Logging;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


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

    public class PlayerLateUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static float tick = 0f;

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (Utils.IsReady && __instance.IsYourPlayer)
            {
                Player.FirearmController fc = __instance.HandsController as Player.FirearmController;

                Plugin.IsSprinting = __instance.IsSprintEnabled;
                PlayerProperties.enviroType = __instance.Environment;
                Plugin.IsInInventory = __instance.IsInventoryOpened;

                tick += Time.deltaTime;

                if (!Utils.IsInHideout() && tick >= 10f && Plugin.HealthEffects.Value)
                {
                    RealismHealthController.PlayerInjuryStateCheck(__instance, Logger);
                }

                if (fc != null)
                {
                    ReloadController.ReloadStateCheck(__instance, fc, Logger);
                    AimController.ADSCheck(__instance, fc, Logger);

                    if (Plugin.EnableStanceStamChanges.Value == true)
                    {
                        StanceController.SetStanceStamina(__instance, fc);
                    }

                    float remainStamPercent = __instance.Physical.HandsStamina.Current / __instance.Physical.HandsStamina.TotalCapacity;
                    PlayerProperties.RemainingArmStamPercentage = 1f - (1f - remainStamPercent) / 3f;
                }
                else if (Plugin.EnableStanceStamChanges.Value == true)
                {
                    StanceController.ResetStanceStamina(__instance);
                }

                __instance.Physical.HandsStamina.Current = Mathf.Max(__instance.Physical.HandsStamina.Current, 1f);
            }
        }
    }
}

