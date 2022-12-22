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

namespace RealismMod
{
    public class SetAimingSlowdownPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1485).GetMethod("SetAimingSlowdown", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref GClass1485 __instance, bool isAiming)
        {

            Player player = (Player)AccessTools.Field(typeof(GClass1485), "player_0").GetValue(__instance);
            if (!player.IsAI)
            {
                if (isAiming)
                {
                    //slow is hard set to 0.33 when called, 0.4-0.43 feels best.
                    float baseSpeed = PlayerProperties.AimMoveSpeedBase;
                    __instance.AddStateSpeedLimit(Math.Max((baseSpeed) + WeaponProperties.AimMoveSpeedModifier, 0.15f), Player.ESpeedLimit.Aiming);

                    return false;
                }
                __instance.RemoveStateSpeedLimit(Player.ESpeedLimit.Aiming);
                return false;
            }
            return true;
        }
    }

    public class UpdateWeaponVariablesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, ref float ___float_7, ref Player.ValueBlender ___valueBlender_0)
        {
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (!player.IsAI)
                {

                    float _aimsSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_7").GetValue(__instance);
                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.startingConvergence * __instance.Aiming.RecoilConvergenceMult;
                    __instance.HandsContainer.Recoil.Damping = WeaponProperties.TotalRecoilDamping;
                    __instance.HandsContainer.HandsPosition.Damping = WeaponProperties.TotalRecoilHandDamping;
                    float aimSpeed = _aimsSpeed * (1f + WeaponProperties.AimSpeedModifier) * WeaponProperties.GlobalAimSpeedModifier;
                    WeaponProperties.AimSpeed = aimSpeed;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_7").SetValue(__instance, aimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_17").SetValue(__instance, WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * PlayerProperties.StrengthSkillAimBuff);
                }
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
                if (!player.IsAI)
                {
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_2").SetValue(__instance, 0);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_8").SetValue(__instance, Mathf.Lerp(1f, Singleton<BackendConfigSettingsClass>.Instance.Stamina.AimingSpeedMultiplier, 0));

                    __result = 0;
                }
            }
        }
    }



    public class method_17Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("method_17", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (!player.IsAI)
                {
                    float baseAimSpeed = WeaponProperties.AimSpeed * PlayerProperties.ADSInjuryMulti;
                    Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;
                    float sightSpeedModi = (currentAimingMod != null) ? AttachmentProperties.AimSpeed(currentAimingMod) : 1;
                    float newAimSpeed = baseAimSpeed * (1 + (sightSpeedModi / 100f));
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_7").SetValue(__instance, newAimSpeed);

                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * PlayerProperties.StrengthSkillAimBuff;
                    float ergoWeightFactor = (ergoWeight / 250) + 1f;
                    float breathIntensity = Mathf.Min(0.64f * ergoWeightFactor, 1.15f);
                    float handsIntensity = Mathf.Min(0.59f * ergoWeightFactor, 1.15f);

                    if (WeaponProperties.HasShoulderContact == false)
                    {
                        breathIntensity = Mathf.Min(0.9f * ergoWeightFactor, 1.25f);
                        handsIntensity = Mathf.Min(0.9f * ergoWeightFactor, 1.25f);
                    }
                    if (firearmController.Item.WeapClass == "pistol" && WeaponProperties.HasShoulderContact != true)
                    {
                        breathIntensity = Mathf.Min(0.78f * ergoWeightFactor, 1.25f);
                        handsIntensity = Mathf.Min(0.73f * ergoWeightFactor, 1.25f);
                    }

                    __instance.Breath.Intensity = breathIntensity * __instance.IntensityByPoseLevel; //both aim sway and up and down breathing
                    __instance.HandsContainer.HandsRotation.InputIntensity = (__instance.HandsContainer.HandsPosition.InputIntensity = handsIntensity * handsIntensity); //also breathing and sway but different, the hands doing sway motion but camera bobbing up and down.
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

                if (!player.IsAI)
                {
                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * PlayerProperties.StrengthSkillAimBuff;
                    float weightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 25f);
                    float displacementModifier = 0.4f;//lower = less drag
                    float aimIntensity = __instance.IntensityByAiming * 0.4f;

                    if (WeaponProperties.HasShoulderContact == false && firearmController.Item.WeapClass != "pistol")
                    {
                        aimIntensity = __instance.IntensityByAiming * 1f;
                    }

                    float swayStrength = EFTHardSettings.Instance.SWAY_STRENGTH_PER_KG.Evaluate(ergoWeight);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_18").SetValue(__instance, swayStrength);

                    float weapDisplacement = EFTHardSettings.Instance.DISPLACEMENT_STRENGTH_PER_KG.Evaluate(ergoWeight);//delay from moving mouse to the weapon moving to center of screen.
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, weapDisplacement * weightFactor * displacementModifier);

                    __instance.MotionReact.SwayFactors = new Vector3(swayStrength, __instance.IsAiming ? (swayStrength * 0.3f) : swayStrength, swayStrength) * Mathf.Clamp(aimIntensity * weightFactor, aimIntensity, 1.1f); // the diving/tiling animation as you move weapon side to side.
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
}
