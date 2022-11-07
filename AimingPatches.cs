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
  
                if (!player.IsAI)
                {
                    checkReloading(__instance, player);
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
            Helper.IsInReloadOpertation = __instance.IsInReloadOperation();
            bool rightArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool leftArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged);
            if (rightArmDamaged == true && leftArmDamaged == false)
            {
                PlayerProperties.InjuryMulti = 0.8f;
            }
            if (rightArmDamaged == false && leftArmDamaged == true)
            {
                PlayerProperties.InjuryMulti = 0.7f;
            }
            if (rightArmDamaged == true && leftArmDamaged == true)
            {
                PlayerProperties.InjuryMulti = 0.5f;
            }
            if (Helper.IsInReloadOpertation == true)
            {
                if (Helper.IsAttemptingToReloadInternalMag == true)
                {
                    float reloadBonus = 0.17f;
/*                    if (Helper.isAttemptingRevolverReload == true)
                    {
                        reloadBonus += 0.05f;
                    }*/
                    player.HandsAnimator.SetAnimationSpeed(reloadBonus + (WeaponProperties.currentMagReloadSpeed * PlayerProperties.ReloadSkillMulti));
                }
            }
            else
            {
                Helper.IsAttemptingToReloadInternalMag = false;
                Helper.isAttemptingRevolverReload = false;
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
        private static bool Prefix()
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