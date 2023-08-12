using Aki.Reflection.Patching;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Diz.Skinning;
using EFT.CameraControl;
using System.Collections;
using EFT.Interactive;
using EFT.Animations;
using System.Linq;
using static EFT.Player;
using System.ComponentModel;
using static EFT.ClientPlayer;

namespace RealismMod
{

    //to find float_9 on new client version, look for: public float AimingSpeed { get{ return this.float_9; } }
    //to finf float_19 again, it's set to ErgnomicWeight in this method.
    public class UpdateWeaponVariablesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            GInterface114 ginterface114 = (GInterface114)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "ginterface114_0").GetValue(__instance);
            if (ginterface114 != null && ginterface114.Weapon != null)
            {
                Weapon weapon = ginterface114.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    FirearmController firearmController = player.HandsController as FirearmController;
                    float totalPlayerWeight = PlayerProperties.TotalModifiedWeightMinusWeapon;
                    float playerWeightFactor = 1f - (totalPlayerWeight / 150f);

                    SkillsClass.GClass1743 skillsClass = (SkillsClass.GClass1743)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "gclass1743_0").GetValue(__instance);
                    Player.ValueBlender valueBlender = (Player.ValueBlender)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "valueBlender_0").GetValue(__instance);

                    float singleItemTotalWeight = weapon.GetSingleItemTotalWeight();
                    /*                    float ergoWeightFactor = WeaponProperties.ErgonomicWeight * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f));*/

                    float ergoFactor = Mathf.Clamp01(WeaponProperties.TotalErgo / 100f);
                    float baseAimspeed = Mathf.InverseLerp(1f, 65f, WeaponProperties.TotalErgo);
                    float aimSpeed = Mathf.Clamp(baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)) * (1f + WeaponProperties.ModAimSpeedModifier) * playerWeightFactor, 0.55f, 1.4f);
                    valueBlender.Speed = __instance.SwayFalloff / aimSpeed;

                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_16").SetValue(__instance, Mathf.InverseLerp(3f, 10f, singleItemTotalWeight * (1f - ergoFactor)));
  
                    __instance.UpdateSwayFactors();

                    aimSpeed = weapon.WeapClass == "pistol" ? aimSpeed * 1.35f : aimSpeed;
                    WeaponProperties.SightlessAimSpeed = aimSpeed;
                    WeaponProperties.ErgoStanceSpeed = Mathf.Clamp(baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)), 0.55f, 1.4f);

                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, aimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, WeaponProperties.ErgonomicWeight * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f)) * PlayerProperties.ErgoDeltaInjuryMulti);

                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.StartingConvergence * __instance.Aiming.RecoilConvergenceMult;
                    __instance.HandsContainer.Recoil.Damping = WeaponProperties.TotalRecoilDamping;

                    if (Plugin.EnableLogging.Value == true) 
                    {
                        Logger.LogWarning("========UpdateWeaponVariables=======");
                        Logger.LogWarning("singleItemTotalWeight = " + singleItemTotalWeight);
                        Logger.LogWarning("total ergo = " + WeaponProperties.TotalErgo);
                        Logger.LogWarning("total ergo clamped= " + ergoFactor);
                        Logger.LogWarning("aimSpeed = " + aimSpeed);
                        Logger.LogWarning("base ergofactor = " + ergoFactor);
                        Logger.LogWarning("total ergofactor = " + WeaponProperties.ErgoFactor * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f)) * PlayerProperties.ErgoDeltaInjuryMulti);
                    }
                }
            }
        }
    }

    public class PwaWeaponParamsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("method_21", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {

            GInterface114 ginterface114 = (GInterface114)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "ginterface114_0").GetValue(__instance);
            if (ginterface114 != null && ginterface114.Weapon != null)
            {
                Weapon weapon = ginterface114.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    FirearmController firearmController = player.HandsController as FirearmController;
                    //force ergo weight to update
                    if (firearmController != null) 
                    {
                        float updateErgoWeight = firearmController.ErgonomicWeight;
                        float accuracy = weapon.GetTotalCenterOfImpact(false);
                        AccessTools.Field(typeof(Player.FirearmController), "float_1").SetValue(firearmController, accuracy);
                    }

                    Mod currentAimingMod = (__instance.CurrentAimingMod != null) ? __instance.CurrentAimingMod.Item as Mod : null;

                    float idleMulti = StanceController.IsIdle() ? 1.3f : 1f;
                    float stockMulti = weapon.WeapClass != "pistol" && !WeaponProperties.HasShoulderContact ? 0.75f : 1f;
                    float totalSightlessAimSpeed = WeaponProperties.SightlessAimSpeed * PlayerProperties.ADSInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.5f));
                    float sightSpeedModi = currentAimingMod != null ? AttachmentProperties.AimSpeed(currentAimingMod) : 1f;
                    float totalSightedAimSpeed = Mathf.Clamp(totalSightlessAimSpeed * (1 + (sightSpeedModi / 100f)) * idleMulti * stockMulti, 0.45f, 1.5f);
                    float newAimSpeed = Mathf.Max(totalSightedAimSpeed * PlayerProperties.ADSSprintMulti, 0.3f) * Plugin.GlobalAimSpeedModifier.Value;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, newAimSpeed); //aimspeed
                    float float_9 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").GetValue(__instance); //aimspeed

                    /*                    float totalWeight = firearmController.Item.WeapClass == "pistol" ? firearmController.Item.GetSingleItemTotalWeight() * 2 : firearmController.Item.GetSingleItemTotalWeight();
                    */
                    Plugin.HasOptic = __instance.CurrentScope.IsOptic ? true : false;

                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f));
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, ergoWeight); 
                    float ergoWeightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 6f);
                    float totalPlayerWeight = PlayerProperties.TotalModifiedWeight - weapon.GetSingleItemTotalWeight();
                    float playerWeightFactor = 1f + (totalPlayerWeight / 400f);
                    float mountingBonus = StanceController.WeaponIsMounting ? StanceController.MountingSwayBonus : StanceController.BracingSwayBonus;
                    float breathIntensity;
                    float handsIntensity;

                    if (!WeaponProperties.HasShoulderContact && weapon.WeapClass != "pistol")
                    {
                        breathIntensity = Mathf.Min(0.78f * ergoWeightFactor * playerWeightFactor, 1.01f);
                        handsIntensity = Mathf.Min(0.78f * ergoWeightFactor, 1.05f);
                    }
                    else if (!WeaponProperties.HasShoulderContact && weapon.WeapClass == "pistol" )
                    {
                        breathIntensity = Mathf.Min(0.58f * ergoWeightFactor * playerWeightFactor, 0.9f);
                        handsIntensity = Mathf.Min(0.58f * ergoWeightFactor, 0.95f);
                    }
                    else
                    {
                        breathIntensity = Mathf.Min(0.57f * ergoWeightFactor * playerWeightFactor, 0.81f);
                        handsIntensity = Mathf.Min(0.57f * ergoWeightFactor, 0.86f);
                    }

                    float totalBreathIntensity = breathIntensity * __instance.IntensityByPoseLevel * Plugin.SwayIntensity.Value;
                    float totalInputIntensitry = handsIntensity * handsIntensity * Plugin.SwayIntensity.Value;
                    PlayerProperties.TotalBreathIntensity = totalBreathIntensity;
                    PlayerProperties.TotalHandsIntensity = totalInputIntensitry;

                    if (PlayerProperties.HasFullyResetSprintADSPenalties)
                    {
                        __instance.Breath.Intensity = PlayerProperties.TotalBreathIntensity;
                        __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity; 
                    }
                    else
                    {
                        __instance.Breath.Intensity = PlayerProperties.SprintTotalBreathIntensity;
                        __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.SprintTotalHandsIntensity;
                    }

                    __instance.Overweight = 0;

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("=====method_21========");
                        Logger.LogWarning("ADSInjuryMulti = " + PlayerProperties.ADSInjuryMulti);
                        Logger.LogWarning("remaining stam percentage = " + PlayerProperties.RemainingArmStamPercentage);
                        Logger.LogWarning("strength = " + PlayerProperties.StrengthSkillAimBuff);
                        Logger.LogWarning("sightSpeedModi = " + sightSpeedModi);
                        Logger.LogWarning("newAimSpeed = " + newAimSpeed);
                        Logger.LogWarning("float_9 = " + float_9);
                        Logger.LogWarning("breathIntensity = " + breathIntensity);
                        Logger.LogWarning("handsIntensity = " + handsIntensity);
                    }
                }
            }
            else
            {
                if (__instance.PointOfView == EPointOfView.FirstPerson)
                {
                    int AimIndex = (int)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "AimIndex").GetValue(__instance);
                    if (!__instance.Sprint && AimIndex < __instance.ScopeAimTransforms.Count)
                    {
                        __instance.Breath.Intensity = 0.5f * __instance.IntensityByPoseLevel;
                        __instance.HandsContainer.HandsRotation.InputIntensity = 0.5f * 0.5f;
                    }
                }
            }
        }
    }

    public class UpdateSwayFactorsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateSwayFactors", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            GInterface114 ginterface114 = (GInterface114)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "ginterface114_0").GetValue(__instance);

            if (ginterface114 != null && ginterface114.Weapon != null)
            {
                Weapon weapon = ginterface114.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    float totalPlayerWeight = PlayerProperties.TotalModifiedWeight - weapon.GetSingleItemTotalWeight();
                    float playerWeightFactor = 1f + (totalPlayerWeight / 200f);
                    bool noShoulderContact = !WeaponProperties.HasShoulderContact && weapon.WeapClass != "pistol";
                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f));
                    float weightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 6f);
                    float displacementModifier = noShoulderContact ? Plugin.SwayIntensity.Value * 0.75f : Plugin.SwayIntensity.Value * 0.55f;//lower = less drag
                    float aimIntensity = noShoulderContact ? Plugin.SwayIntensity.Value * 0.75f : Plugin.SwayIntensity.Value * 0.55f;

                    float swayStrength = EFTHardSettings.Instance.SWAY_STRENGTH_PER_KG.Evaluate(ergoWeight * weightFactor * playerWeightFactor);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_20").SetValue(__instance, swayStrength);

                    float weapDisplacement = EFTHardSettings.Instance.DISPLACEMENT_STRENGTH_PER_KG.Evaluate(ergoWeight * weightFactor);//delay from moving mouse to the weapon moving to center of screen.
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_21").SetValue(__instance, weapDisplacement * weightFactor * displacementModifier * playerWeightFactor);

                    __instance.MotionReact.SwayFactors = new Vector3(swayStrength, __instance.IsAiming ? (swayStrength * 0.3f) : swayStrength, swayStrength) * Mathf.Clamp(aimIntensity * weightFactor * playerWeightFactor, aimIntensity, 1f); // the diving/tiling animation as you move weapon side to side.


                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("=====UpdateSwayFactors====");
                        Logger.LogWarning("ergoWeight = " + ergoWeight);
                        Logger.LogWarning("weightFactor = " + weightFactor);
                        Logger.LogWarning("swayStrength = " + swayStrength);
                        Logger.LogWarning("weapDisplacement = " + weapDisplacement);
                        Logger.LogWarning("displacementModifier = " + displacementModifier);
                        Logger.LogWarning("aimIntensity = " + aimIntensity);
                        Logger.LogWarning("Sway Factors = " + __instance.MotionReact.SwayFactors);
                    }
                    return false;
                }
            }
            return true;
        }
    }

    public class SetOverweightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("set_Overweight", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float value)
        {
            GInterface114 ginterface114 = (GInterface114)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "ginterface114_0").GetValue(__instance);

            if (ginterface114 != null && ginterface114.Weapon != null)
            {
                Weapon weapon = ginterface114.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    __instance.Breath.Overweight = value;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_2").SetValue(__instance, 0);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_10").SetValue(__instance, Mathf.Lerp(1f, Singleton<BackendConfigSettingsClass>.Instance.Stamina.AimingSpeedMultiplier, 0));
                    __instance.Walk.Overweight = Mathf.Lerp(0f, Singleton<BackendConfigSettingsClass>.Instance.Stamina.WalkVisualEffectMultiplier, value);

                    return false;
                }
            }
            return true;
        }
    }


    public class GetOverweightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("get_Overweight", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref EFT.Animations.ProceduralWeaponAnimation __instance, ref float __result)
        {

            GInterface114 ginterface114 = (GInterface114)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "ginterface114_0").GetValue(__instance);
            if (ginterface114 != null && ginterface114.Weapon != null)
            {
                Weapon weapon = ginterface114.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    __result = 0;
                    return false;
                }
            }
            return true;
        }
    }
}
