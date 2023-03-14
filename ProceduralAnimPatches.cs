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
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);
            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    SkillsClass.GClass1678 skillsClass = (SkillsClass.GClass1678)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "gclass1678_0").GetValue(__instance);
                    Player.ValueBlender valueBlender = (Player.ValueBlender)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "valueBlender_0").GetValue(__instance);

                    float singleItemTotalWeight = firearmController.Item.GetSingleItemTotalWeight();
                    float ergoWeight = WeaponProperties.ErgonomicWeight * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f)); //maybe apply sterngth skill buff, but might be OP

                    float ergo = Mathf.Clamp01(WeaponProperties.TotalErgo / 100f);
                    float t = Mathf.InverseLerp(0.6f, 9f, ergoWeight);
                    float a = Mathf.Lerp(2f, 2.4f, t);
                    float b = Mathf.Lerp(0.35f, 0.95f, t);
                    float t2 = (ergo < 0.25f) ? (0.25f + 3f * ergo * ergo) : (2f * ergo - ergo * ergo);
                    float aimSpeed = Mathf.Clamp(1f / Mathf.Lerp(a, b, t2) * (1f + (skillsClass.AimSpeed * 0.5f)) * (1f + WeaponProperties.ModAimSpeedModifier) * WeaponProperties.GlobalAimSpeedModifier, 0.3f, 1.3f);
                    valueBlender.Speed = __instance.SwayFalloff / aimSpeed;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_16").SetValue(__instance, Mathf.InverseLerp(3f, 8f, singleItemTotalWeight * (1f - ergo)));
                    __instance.UpdateSwayFactors();

                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.StartingConvergence * __instance.Aiming.RecoilConvergenceMult;
                    __instance.HandsContainer.Recoil.Damping = WeaponProperties.TotalRecoilDamping;
                    __instance.HandsContainer.HandsPosition.Damping = WeaponProperties.TotalRecoilHandDamping;
                    aimSpeed = firearmController.Item.WeapClass == "pistol" ? aimSpeed * 1.2f : aimSpeed;
                    WeaponProperties.SightlessAimSpeed = firearmController.Item.WeapClass == "pistol" ? Mathf.Min(aimSpeed, 1.2f) : Mathf.Min(aimSpeed, 0.9f);

                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, aimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, WeaponProperties.ErgonomicWeight * (1f - PlayerProperties.StrengthSkillAimBuff * 1.5f) * PlayerProperties.ErgoDeltaInjuryMulti);


                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("========UpdateWeaponVariables=======");
                        Logger.LogWarning("total ergo = " + WeaponProperties.TotalErgo);
                        Logger.LogWarning("total ergo clamped= " + ergo);
                        Logger.LogWarning("aimSpeed = " + aimSpeed);
                        Logger.LogWarning("base ergoWeight = " + ergoWeight);
                        Logger.LogWarning("total ergoWeight = " + WeaponProperties.ErgonomicWeight * (1f - PlayerProperties.StrengthSkillAimBuff * 1.5f) * PlayerProperties.ErgoDeltaInjuryMulti);
                    }
                }
            }
        }
    }

    public class method_20Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("method_20", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    float idleBonus = StanceController.IsIdle() == true ? 1.1f : 1f;
                    float totalSightlessAimSpeed = WeaponProperties.SightlessAimSpeed * PlayerProperties.ADSInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.5f));
                    Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;
                    float sightSpeedModi = (currentAimingMod != null) ? AttachmentProperties.AimSpeed(currentAimingMod) : 1f;
                    float newAimSpeed = Mathf.Clamp(totalSightlessAimSpeed * (1 + (sightSpeedModi / 100f)) * idleBonus, 0.35f, 1f);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, newAimSpeed); //aimspeed

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("=====method_20========");
                        Logger.LogWarning("newAimSpeed = " + newAimSpeed);
                        Logger.LogWarning("ADSInjuryMulti = " + PlayerProperties.ADSInjuryMulti);
                        Logger.LogWarning("remaining stam percentage = " + PlayerProperties.RemainingArmStamPercentage);
                        Logger.LogWarning("strength = " + PlayerProperties.StrengthSkillAimBuff);
                    }

                    Plugin.HasOptic = __instance.CurrentScope.IsOptic ? true : false;

                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - PlayerProperties.StrengthSkillAimBuff * 1.5f);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, ergoWeight); 
                    float ergoWeightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 6f);
                    float breathIntensity;
                    float handsIntensity;

                    if (!WeaponProperties.HasShoulderContact)
                    {
                        breathIntensity = Mathf.Min(0.75f * ergoWeightFactor, 0.9f);
                        handsIntensity = Mathf.Min(0.75f * ergoWeightFactor, 0.95f);
                    }
                    else if (firearmController.Item.WeapClass == "pistol" && WeaponProperties.HasShoulderContact != true)
                    {
                        breathIntensity = Mathf.Min(0.58f * ergoWeightFactor, 0.75f);
                        handsIntensity = Mathf.Min(0.58f * ergoWeightFactor, 0.8f);
                    }
                    else
                    {
                        breathIntensity = Mathf.Min(0.55f * ergoWeightFactor, 0.85f);
                        handsIntensity = Mathf.Min(0.55f * ergoWeightFactor, 0.9f);
                    }

                    breathIntensity *= Plugin.SwayIntensity.Value;
                    handsIntensity *= Plugin.SwayIntensity.Value;

                    __instance.Shootingg.Intensity = Plugin.RecoilIntensity.Value;

                    __instance.Breath.Intensity = breathIntensity * __instance.IntensityByPoseLevel; //both aim sway and up and down breathing
                    __instance.HandsContainer.HandsRotation.InputIntensity = (__instance.HandsContainer.HandsPosition.InputIntensity = handsIntensity * handsIntensity); //also breathing and sway but different, the hands doing sway motion but camera bobbing up and down. 
                    PlayerProperties.TotalHandsIntensity = __instance.HandsContainer.HandsRotation.InputIntensity;
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
                        __instance.HandsContainer.HandsRotation.InputIntensity = (__instance.HandsContainer.HandsPosition.InputIntensity = 0.5f * 0.5f);
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
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);

                if (player.IsYourPlayer == true)
                {
                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - PlayerProperties.StrengthSkillAimBuff * 1.5f);
                    float weightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 6f);
                    float displacementModifier = 0.4f;//lower = less drag
                    float aimIntensity = Plugin.SwayIntensity.Value * 0.4f;

                    if (!WeaponProperties.HasShoulderContact && firearmController.Item.WeapClass != "pistol")
                    {
                        aimIntensity = Plugin.SwayIntensity.Value * 1.1f;
                    }

                    float swayStrength = EFTHardSettings.Instance.SWAY_STRENGTH_PER_KG.Evaluate(ergoWeight * weightFactor);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_20").SetValue(__instance, swayStrength);

                    float weapDisplacement = EFTHardSettings.Instance.DISPLACEMENT_STRENGTH_PER_KG.Evaluate(ergoWeight * weightFactor);//delay from moving mouse to the weapon moving to center of screen.
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_21").SetValue(__instance, weapDisplacement * weightFactor * displacementModifier);

                    __instance.MotionReact.SwayFactors = new Vector3(swayStrength, __instance.IsAiming ? (swayStrength * 0.3f) : swayStrength, swayStrength) * Mathf.Clamp(aimIntensity * weightFactor, aimIntensity, 1.1f); // the diving/tiling animation as you move weapon side to side.


                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("=====UpdateSwayFactors====");
                        Logger.LogWarning("ergoWeight = " + ergoWeight);
                        Logger.LogWarning("weightFactor = " + weightFactor);
                        Logger.LogWarning("swayStrength = " + swayStrength);
                        Logger.LogWarning("weapDisplacement = " + weapDisplacement);
                        Logger.LogWarning("Sway Factors = " + __instance.MotionReact.SwayFactors);
                    }

                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
    }

    public class OverweightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("get_Overweight", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, ref float __result)
        {

            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_2").SetValue(__instance, 0);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_10").SetValue(__instance, Mathf.Lerp(1f, Singleton<BackendConfigSettingsClass>.Instance.Stamina.AimingSpeedMultiplier, 0));

                    __result = 0;
                }
            }
        }
    }
}
