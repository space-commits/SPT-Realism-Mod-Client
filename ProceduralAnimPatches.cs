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
                    SkillsClass.GClass1681 skillsClass = (SkillsClass.GClass1681)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "gclass1681_0").GetValue(__instance);
                    Player.ValueBlender valueBlender = (Player.ValueBlender)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "valueBlender_0").GetValue(__instance);

                    float singleItemTotalWeight = firearmController.Item.GetSingleItemTotalWeight();
                    float ergoWeight = WeaponProperties.ErgonomicWeight * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f)); //maybe apply sterngth skill buff, but might be OP

                    float ergoFactor = Mathf.Clamp01(WeaponProperties.TotalErgo / 100f);
                    float baseAimspeed = Mathf.InverseLerp(1f, 65f, WeaponProperties.TotalErgo);
                    float aimSpeed = Mathf.Clamp(baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)) * (1f + WeaponProperties.ModAimSpeedModifier), 0.65f, 1.4f);
                    valueBlender.Speed = __instance.SwayFalloff / aimSpeed;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_16").SetValue(__instance, Mathf.InverseLerp(3f, 10f, singleItemTotalWeight * (1f - ergoFactor)));
                    __instance.UpdateSwayFactors();

                    aimSpeed = firearmController.Item.WeapClass == "pistol" ? aimSpeed * 1.35f : aimSpeed;
                    WeaponProperties.SightlessAimSpeed = aimSpeed;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, aimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, WeaponProperties.ErgonomicWeight * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f)) * PlayerProperties.ErgoDeltaInjuryMulti);

                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.StartingConvergence * __instance.Aiming.RecoilConvergenceMult;
                    __instance.HandsContainer.Recoil.Damping = WeaponProperties.TotalRecoilDamping;
                    __instance.HandsContainer.HandsPosition.Damping = WeaponProperties.TotalRecoilHandDamping;

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("========UpdateWeaponVariables=======");
                        Logger.LogWarning("singleItemTotalWeight = " + singleItemTotalWeight);
                        Logger.LogWarning("total ergo = " + WeaponProperties.TotalErgo);
                        Logger.LogWarning("total ergo clamped= " + ergoFactor);
                        Logger.LogWarning("aimSpeed = " + aimSpeed);
                        Logger.LogWarning("base ergoWeight = " + ergoWeight);
                        Logger.LogWarning("total ergoWeight = " + WeaponProperties.ErgonomicWeight * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f)) * PlayerProperties.ErgoDeltaInjuryMulti);
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
                    //force ergo weight to update
                    float updateErgoWeight = firearmController.ErgonomicWeight;

                    Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;
                    
                    float idleMulti = StanceController.IsIdle() ? 1.3f : 1f;
                    float stockMulti = !WeaponProperties.HasShoulderContact && firearmController.Item.WeapClass != "pistol" ? 0.6f : 1f;
                    float totalSightlessAimSpeed = WeaponProperties.SightlessAimSpeed * PlayerProperties.ADSInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.5f));
                    float sightSpeedModi = currentAimingMod != null ? AttachmentProperties.AimSpeed(currentAimingMod) : 1f;
                    float newAimSpeed = Mathf.Clamp(totalSightlessAimSpeed * (1 + (sightSpeedModi / 100f)) * idleMulti * stockMulti, 0.45f, 1.5f) * Plugin.GlobalAimSpeedModifier.Value;
                    
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, newAimSpeed); //aimspeed
                    float float_9 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").GetValue(__instance); //aimspeed
                   
                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("=====method_20========");
                        Logger.LogWarning("ADSInjuryMulti = " + PlayerProperties.ADSInjuryMulti);
                        Logger.LogWarning("remaining stam percentage = " + PlayerProperties.RemainingArmStamPercentage);
                        Logger.LogWarning("strength = " + PlayerProperties.StrengthSkillAimBuff);
                        Logger.LogWarning("sightSpeedModi = " + sightSpeedModi);
                        Logger.LogWarning("newAimSpeed = " + newAimSpeed);
                        Logger.LogWarning("float_9 = " + float_9);
                    }

                    Plugin.HasOptic = __instance.CurrentScope.IsOptic ? true : false;

                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f));
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, ergoWeight); 
                    float ergoWeightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 6f);
                    float breathIntensity;
                    float handsIntensity;

                    if (!WeaponProperties.HasShoulderContact && firearmController.Item.WeapClass != "pistol")
                    {
                        breathIntensity = Mathf.Min(0.75f * ergoWeightFactor, 0.96f);
                        handsIntensity = Mathf.Min(0.75f * ergoWeightFactor, 1f);
                    }
                    else if (!WeaponProperties.HasShoulderContact && firearmController.Item.WeapClass == "pistol" )
                    {
                        breathIntensity = Mathf.Min(0.56f * ergoWeightFactor, 0.9f);
                        handsIntensity = Mathf.Min(0.56f * ergoWeightFactor, 0.95f);
                    }
                    else
                    {
                        breathIntensity = Mathf.Min(0.55f * ergoWeightFactor, 0.81f);
                        handsIntensity = Mathf.Min(0.55f * ergoWeightFactor, 0.86f);
                    }

                    breathIntensity *= Plugin.SwayIntensity.Value;
                    handsIntensity *= Plugin.SwayIntensity.Value;

                    __instance.Breath.Intensity = breathIntensity * __instance.IntensityByPoseLevel; //both aim sway and up and down breathing
                    __instance.HandsContainer.HandsRotation.InputIntensity = (__instance.HandsContainer.HandsPosition.InputIntensity = handsIntensity * handsIntensity); //also breathing and sway but different, the hands doing sway motion but camera bobbing up and down. 
                    PlayerProperties.TotalHandsIntensity = __instance.HandsContainer.HandsRotation.InputIntensity;

                    __instance.Shootingg.Intensity = Plugin.RecoilIntensity.Value;
                    __instance.Overweight = 0;
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
                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f));
                    float weightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 6f);
                    float displacementModifier = 0.4f;//lower = less drag
                    float aimIntensity = Plugin.SwayIntensity.Value * 0.4f;

                    if (!WeaponProperties.HasShoulderContact && firearmController.Item.WeapClass != "pistol")
                    {
                        aimIntensity = Plugin.SwayIntensity.Value * 1.15f;
                        displacementModifier = 0.7f;
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

    public class SetOverweightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("set_Overweight", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float value)
        {

            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
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

            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    __result = 0;
                    return false;
                }
            }
            return true;
        }
    }
}
