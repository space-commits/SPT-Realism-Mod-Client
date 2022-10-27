using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace RealismMod
{
    public class AimingPatches : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("get_IsAiming", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Player.FirearmController __instance, ref bool ____isAiming)
        {
            if (Helper.isReady == true)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
                checkReloading(__instance, player);
                if (!player.IsAI)
                {
                    FaceShieldComponent component = player.FaceShieldObserver.Component;
                    bool isOn = component != null && (component.Togglable == null || component.Togglable.On);
                    if (isOn && !WeaponProperties.WeaponCanFSADS && !FaceShieldProperties.AllowsADS(component.Item))
                    {
                        Helper.IsAllowedAim = false;
                    }
                    else
                    {
                        Helper.IsAllowedAim = true;
                    }
                    Plugin.isAiming = ____isAiming;
                }
            }
        }

        public static void checkReloading(EFT.Player.FirearmController __instance, Player player)
        {
            if (__instance.IsInReloadOperation() == true && Helper.IsMagReloading == false)
            {
                Player.FirearmController.GClass1409 CurrentOperation = (Player.FirearmController.GClass1409)AccessTools.Property(typeof(EFT.Player.FirearmController), "CurrentOperation").GetValue(__instance);

                if (CurrentOperation is Player.FirearmController.GClass1410)
                {
                    Logger.LogInfo("Is Reloading, GClass1410");//magtube shotgun....and chambering a round from no mag reload for magfed weap...and SKS ammo reload
                }
                if (CurrentOperation is Player.FirearmController.GClass1431)
                {
                    Logger.LogInfo("Is Reloading, GClass1431");
                }
                if (CurrentOperation is Player.FirearmController.GClass1436)
                {
                    Logger.LogInfo("Is Reloading, GClass1436");
                }
                if (CurrentOperation is Player.FirearmController.GClass1441)
                {
                    Logger.LogInfo("Is Reloading, GClass1441");
                }
                if (CurrentOperation is Player.FirearmController.GClass1435)
                {
                    Logger.LogInfo("Is Reloading, GClass1435");
                }

                /*        player.HandsAnimator.SetAnimationSpeed(WeaponProperties.currentMagReloadSpeed);*/

                //simply check if isMagReloading is true, and if it isn't AND isReloading is true then modify animation speed, and when it isReloading is false and isMagrelaodign is also false, then reset speed.
                //the animation for chambering a round or rechambering needs to take into account  pump speed too, so those need to cehck if isreloading and ismagreloading istrue.
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
        private static bool Prefix(ref EFT.Player.FirearmController __instance)
        {

            if (Plugin.enableFSPatch.Value == true)
            {
                if (Helper.IsAllowedAim)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
    }
}