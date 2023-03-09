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
                Plugin.SelectedStance = 0;
                Plugin.IsLowReady = false;
                Plugin.IsHighReady = false;
                Plugin.IsActiveAiming = false;
                Plugin.WasHighReady = false;
                Plugin.WasLowReady = false;
                Plugin.IsShortStock = false;

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
            if (Utils.CheckIsReady() && __instance.IsYourPlayer == true)
            {
                Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
                PlayerInjuryStateCheck(__instance, Logger);
                Plugin.IsSprinting = __instance.IsSprintEnabled;

                PlayerProperties.enviroType = __instance.Environment;

                if (fc != null)
                {
                    ReloadStateCheck(__instance, fc);

                    if (Plugin.IsHighReady == true || Plugin.WasHighReady == true)
                    {
                        __instance.BodyAnimatorCommon.SetFloat(GClass1645.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2f);
                    }
                    else
                    {
                        __instance.BodyAnimatorCommon.SetFloat(GClass1645.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)fc.Item.CalculateCellSize().X);
                    }

                    if (Plugin.EnableStanceStamChanges.Value == true) 
                    {
                        if (fc.Item.WeapClass != "pistol")
                        {
                            if (!Plugin.IsHighReady && !Plugin.IsLowReady && !Plugin.IsAiming && !Plugin.IsActiveAiming && !Plugin.IsShortStock && Plugin.EnableIdleStamDrain.Value == true)
                            {
                                __instance.Physical.Aim(!(__instance.MovementContext.StationaryWeapon == null) ? 0f : WeaponProperties.ErgonomicWeight * 0.5f);
                            }
                            if (Plugin.IsActiveAiming == true)
                            {
                                __instance.Physical.Aim(!(__instance.MovementContext.StationaryWeapon == null) ? 0f : WeaponProperties.ErgonomicWeight * 0.25f);
                            }
                            if (Plugin.IsHighReady == true && !Plugin.IsLowReady && !Plugin.IsAiming && !Plugin.IsShortStock)
                            {
                                __instance.Physical.Aim(0f);
                                __instance.Physical.HandsStamina.Current = Mathf.Min(__instance.Physical.HandsStamina.Current + ((1f - (WeaponProperties.ErgonomicWeight / 100f)) * 0.03f), __instance.Physical.HandsStamina.TotalCapacity);
                            }
                            if (Plugin.IsLowReady == true && !Plugin.IsHighReady && !Plugin.IsAiming && !Plugin.IsShortStock)
                            {
                                __instance.Physical.Aim(0f);
                                __instance.Physical.HandsStamina.Current = Mathf.Min(__instance.Physical.HandsStamina.Current + ((1f - (WeaponProperties.ErgonomicWeight / 100f)) * 0.015f), __instance.Physical.HandsStamina.TotalCapacity);
                            }
                            if (Plugin.IsShortStock == true && !Plugin.IsHighReady && !Plugin.IsAiming && !Plugin.IsLowReady)
                            {
                                __instance.Physical.Aim(0f);
                                __instance.Physical.HandsStamina.Current = Mathf.Min(__instance.Physical.HandsStamina.Current + ((1f - (WeaponProperties.ErgonomicWeight / 100f)) * 0.01f), __instance.Physical.HandsStamina.TotalCapacity);
                            }
                            __instance.Physical.HandsStamina.Current = Mathf.Max(__instance.Physical.HandsStamina.Current, 1f);

                        }
                        else
                        {
                            if (!Plugin.IsAiming)
                            {
                                __instance.Physical.Aim(0f);
                                __instance.Physical.HandsStamina.Current = Mathf.Min(__instance.Physical.HandsStamina.Current + ((1f - (WeaponProperties.ErgonomicWeight / 100f)) * 0.025f), __instance.Physical.HandsStamina.TotalCapacity);
                            }
                        }

                        if (__instance.IsInventoryOpened == true)
                        {
                            __instance.Physical.Aim(0f);
                        }
                    }
                }
                else if(Plugin.EnableStanceStamChanges.Value == true)
                {
                    __instance.Physical.Aim(0f);
                    __instance.Physical.HandsStamina.Current = Mathf.Min(__instance.Physical.HandsStamina.Current + 0.025f, __instance.Physical.HandsStamina.TotalCapacity);
                }
            }
        }


        public static void PlayerInjuryStateCheck(Player player, ManualLogSource logger)
        {
            bool rightArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool leftArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged);
            bool tremor = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.Tremor);

            if (!rightArmDamaged && !leftArmDamaged && !tremor)
            {
                PlayerProperties.AimMoveSpeedBase = 0.42f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1f;
                PlayerProperties.ADSInjuryMulti = 1f;
                PlayerProperties.ReloadInjuryMulti = 1f;
                PlayerProperties.RecoilInjuryMulti = 1f;
            }
            if ((rightArmDamaged == true && !leftArmDamaged) || tremor == true)
            {
                PlayerProperties.AimMoveSpeedBase = 0.38f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1.5f;
                PlayerProperties.ADSInjuryMulti = 0.65f;
                PlayerProperties.ReloadInjuryMulti = 0.85f;
                PlayerProperties.RecoilInjuryMulti = 1.05f;
            }
            if ((!rightArmDamaged && leftArmDamaged == true))
            {
                PlayerProperties.AimMoveSpeedBase = 0.34f;
                PlayerProperties.ErgoDeltaInjuryMulti = 2f;
                PlayerProperties.ADSInjuryMulti = 0.75f;
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
                PlayerProperties.ADSInjuryMulti = 0.55f;
                PlayerProperties.ReloadInjuryMulti = 0.75f;
                PlayerProperties.RecoilInjuryMulti = 1.15f;
            }
        }

        public static void ReloadStateCheck(Player player, EFT.Player.FirearmController fc)
        {
            Utils.IsInReloadOpertation = fc.IsInReloadOperation();

            if (Utils.IsInReloadOpertation == true)
            {
                if (Utils.IsAttemptingToReloadInternalMag == true)
                {
                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("IsAttemptingToReloadInternalMag = " + Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * Plugin.InternalMagReloadMulti.Value * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.6f, 1.25f));

                    }
                    player.HandsAnimator.SetAnimationSpeed(Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * Plugin.InternalMagReloadMulti.Value * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.6f, 1.25f));
                }
            }
            else
            {
                Utils.IsAttemptingToReloadInternalMag = false;
                Utils.IsAttemptingRevolverReload = false;
            }
        }
    }
}

