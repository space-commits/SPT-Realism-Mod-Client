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
using PlayerInterface = GInterface113;
using WeaponSkillsClass = EFT.SkillManager.GClass1638;

namespace RealismMod
{
    public class UpdateWeaponVariablesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_firearmAnimationData").GetValue(__instance);
           
            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    FirearmController firearmController = player.HandsController as FirearmController;
                    float totalPlayerWeight = PlayerProperties.TotalModifiedWeightMinusWeapon;
                    float playerWeightFactor = 1f - (totalPlayerWeight / 150f);

                    WeaponSkillsClass skillsClass = (WeaponSkillsClass)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_buffInfo").GetValue(__instance);
                    Player.ValueBlender valueBlender = (Player.ValueBlender)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimSwayBlender").GetValue(__instance);

                    float singleItemTotalWeight = weapon.GetSingleItemTotalWeight();
                    /*                    float ergoWeightFactor = WeaponProperties.ErgonomicWeight * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f));*/

                    float ergoFactor = Mathf.Clamp01(WeaponProperties.TotalErgo / 100f);
                    float baseAimspeed = Mathf.InverseLerp(1f, 80f, WeaponProperties.TotalErgo) * 1.25f;
                    float aimSpeed = Mathf.Clamp(baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)) * (1f + WeaponProperties.ModAimSpeedModifier) * playerWeightFactor, 0.55f, 1.4f);
                    valueBlender.Speed = __instance.SwayFalloff / aimSpeed;

                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimSwayStrength").SetValue(__instance, Mathf.InverseLerp(3f, 10f, singleItemTotalWeight * (1f - ergoFactor)));
  
                    __instance.UpdateSwayFactors();

                    aimSpeed = weapon.WeapClass == "pistol" ? aimSpeed * 1.35f : aimSpeed;
                    WeaponProperties.SightlessAimSpeed = aimSpeed;
                    WeaponProperties.ErgoStanceSpeed = baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)) * (weapon.WeapClass == "pistol" ? 1.5f : 1f);

                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").SetValue(__instance, aimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_ergonomicWeight").SetValue(__instance, WeaponProperties.ErgonomicWeight * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f)) * PlayerProperties.ErgoDeltaInjuryMulti);

                    Plugin.CurrentlyEquippedWeapon = weapon;    

                    if (Plugin.EnableLogging.Value == true) 
                    {
                        Logger.LogWarning("========UpdateWeaponVariables=======");
                        Logger.LogWarning("singleItemTotalWeight = " + singleItemTotalWeight);
                        Logger.LogWarning("total ergo = " + WeaponProperties.TotalErgo);
                        Logger.LogWarning("total ergo clamped= " + ergoFactor);
                        Logger.LogWarning("aimSpeed = " + aimSpeed);
                        Logger.LogWarning("base aimSpeed = " + baseAimspeed);
                        Logger.LogWarning("base ergofactor = " + ergoFactor);
                        Logger.LogWarning("total ergofactor = " + WeaponProperties.ErgoFactor * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f)) * PlayerProperties.ErgoDeltaInjuryMulti);
                    }
                }
            }
        }
    }

    public class PwaWeaponParamsPatch : ModulePatch
    {
        private static bool didAimWiggle = false;

        private static void DoADSWiggle(ProceduralWeaponAnimation pwa, Player player, float ergoWeightFactor, float playerWeightFactor, float newAimSpeed) 
        {
            if (StanceController.IsIdle() && WeaponProperties._WeapClass != "pistol")
            {
                pwa.Shootingg.ShotVals[3].Intensity = 0f;
                pwa.Shootingg.ShotVals[4].Intensity = 0f;
                Vector3 wiggleDir = new Vector3(-1.5f, -1.5f, 0f) * ergoWeightFactor * playerWeightFactor * (Plugin.HasOptic ? 0.5f : 1f);

                if (pwa.IsAiming && !didAimWiggle)
                {

                    StanceController.DoWiggleEffects(player, pwa, wiggleDir * newAimSpeed);
                    didAimWiggle = true;
                }
                else if (!pwa.IsAiming && didAimWiggle)
                {
                    StanceController.DoWiggleEffects(player, pwa, -wiggleDir * newAimSpeed * 0.45f);
                    didAimWiggle = false;
                }
            }

        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("method_21", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPostfix]
        private static void PatchPostfix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {

            PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_firearmAnimationData").GetValue(__instance);
            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    __instance.Overweight = 0;
                    __instance.CrankRecoil = Plugin.EnableCrank.Value;

                    FirearmController firearmController = player.HandsController as FirearmController;
                    if (firearmController != null)
                    {       
                        float updateErgoWeight = firearmController.ErgonomicWeight; //force ergo weight to update
                        float accuracy = weapon.GetTotalCenterOfImpact(false);
                        AccessTools.Field(typeof(Player.FirearmController), "float_2").SetValue(firearmController, accuracy);
                    }

                    Mod currentAimingMod = (__instance.CurrentAimingMod != null) ? __instance.CurrentAimingMod.Item as Mod : null;

                    float stanceMulti = StanceController.IsIdle() ? 1.6f : StanceController.WasActiveAim || StanceController.IsActiveAiming ? 1.5f : StanceController.WasHighReady || StanceController.IsHighReady ? 1.1f : StanceController.WasLowReady || StanceController.IsLowReady ? 1.3f : 1f;
                    float stockMulti = weapon.WeapClass != "pistol" && !WeaponProperties.HasShoulderContact ? 0.75f : 1f;
                    float totalSightlessAimSpeed = WeaponProperties.SightlessAimSpeed * PlayerProperties.ADSInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.5f));
                    float sightSpeedModi = currentAimingMod != null ? AttachmentProperties.AimSpeed(currentAimingMod) : 1f;
                    sightSpeedModi = currentAimingMod != null && (currentAimingMod.TemplateId == "5c07dd120db834001c39092d" || currentAimingMod.TemplateId == "5c0a2cec0db834001b7ce47d") && __instance.CurrentScope.IsOptic ? 1f : sightSpeedModi;
                    float totalSightedAimSpeed = Mathf.Clamp(totalSightlessAimSpeed * (1 + (sightSpeedModi / 100f)) * stanceMulti * stockMulti, 0.45f, 1.5f);
                    float newAimSpeed = Mathf.Max(totalSightedAimSpeed * PlayerProperties.ADSSprintMulti, 0.3f) * Plugin.GlobalAimSpeedModifier.Value;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").SetValue(__instance, newAimSpeed); //aimspeed
                    float aimingSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").GetValue(__instance); //aimspeed

                    Plugin.HasOptic = __instance.CurrentScope.IsOptic ? true : false;

                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f));
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_ergonomicWeight").SetValue(__instance, ergoWeight); 
                    float ergoWeightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 6f);
                    float totalPlayerWeight = PlayerProperties.TotalModifiedWeight - weapon.GetSingleItemTotalWeight();
                    float playerWeightFactor = 1f + (totalPlayerWeight / 300f);
                    float mountingBonus = StanceController.IsMounting ? StanceController.MountingSwayBonus : StanceController.BracingSwayBonus;
                    float breathIntensity;
                    float handsIntensity;

                    DoADSWiggle(__instance, player, ergoWeightFactor, playerWeightFactor, newAimSpeed);
          
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

                    float beltFedFactor = weapon.IsBeltMachineGun ? 1.35f : 1f;
                    float totalBreathIntensity = breathIntensity * __instance.IntensityByPoseLevel * Plugin.SwayIntensity.Value * beltFedFactor;
                    float totalInputIntensitry = handsIntensity * handsIntensity * Plugin.SwayIntensity.Value * beltFedFactor;
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


                    if (__instance.CurrentAimingMod != null) 
                    {
                        Plugin.Parralax = Plugin.HasOptic ? 0.04f * Plugin.ScopeAccuracyFactor : 0.045f * Plugin.ScopeAccuracyFactor;
                        string id = (__instance.CurrentAimingMod?.Item?.Id != null) ? __instance.CurrentAimingMod.Item.Id : "";
                        Plugin.ScopeID = id;
                        if (id != null)
                        {
                            if (Plugin.ZeroOffsetDict.TryGetValue(id, out Vector2 offset))
                            {
                                Plugin.ZeroRecoilOffset = offset;
                            }
                            else
                            {
                                Plugin.ZeroRecoilOffset = Vector2.zero;
                                Plugin.ZeroOffsetDict.Add(id, Plugin.ZeroRecoilOffset);
                            }
                        }
                    }

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("=====method_21========");
                        Logger.LogWarning("ADSInjuryMulti = " + PlayerProperties.ADSInjuryMulti);
                        Logger.LogWarning("remaining stam percentage = " + PlayerProperties.RemainingArmStamPercentage);
                        Logger.LogWarning("strength = " + PlayerProperties.StrengthSkillAimBuff);
                        Logger.LogWarning("sightSpeedModi = " + sightSpeedModi);
                        Logger.LogWarning("newAimSpeed = " + newAimSpeed);
                        Logger.LogWarning("_aimingSpeed = " + aimingSpeed);
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
                        __instance.HandsContainer.HandsRotation.InputIntensity = 0.25f;
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
        private static bool Prefix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_firearmAnimationData").GetValue(__instance);
            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {

                    float totalPlayerWeight = PlayerProperties.TotalModifiedWeight - weapon.GetSingleItemTotalWeight();
                    float playerWeightFactor = 1f + (totalPlayerWeight / 200f);
                    float beltFedFactor = weapon.IsBeltMachineGun ? 1.35f : 1f;
                    bool noShoulderContact = !WeaponProperties.HasShoulderContact && weapon.WeapClass != "pistol";
                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - (PlayerProperties.StrengthSkillAimBuff * 1.5f));
                    float weightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 6f) * beltFedFactor;
                    float displacementModifier = noShoulderContact ? Plugin.SwayIntensity.Value * 0.95f : Plugin.SwayIntensity.Value * 0.48f;//lower = less drag
                    float aimIntensity = noShoulderContact ? Plugin.SwayIntensity.Value * 0.95f : Plugin.SwayIntensity.Value * 0.57f;
          
                    float weapDisplacement = EFTHardSettings.Instance.DISPLACEMENT_STRENGTH_PER_KG.Evaluate(ergoWeight * weightFactor);//delay from moving mouse to the weapon moving to center of screen.
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_displacementStr").SetValue(__instance, weapDisplacement * weightFactor * displacementModifier * playerWeightFactor);

                    float swayStrength = EFTHardSettings.Instance.SWAY_STRENGTH_PER_KG.Evaluate(ergoWeight * weightFactor * playerWeightFactor);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_swayStrength").SetValue(__instance, swayStrength);

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
        private static bool Prefix(EFT.Animations.ProceduralWeaponAnimation __instance, float value)
        {
            PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_firearmAnimationData").GetValue(__instance);
            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    __instance.Breath.Overweight = value;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_overweight").SetValue(__instance, 0);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_overweightAimingMultiplier").SetValue(__instance, Mathf.Lerp(1f, Singleton<BackendConfigSettingsClass>.Instance.Stamina.AimingSpeedMultiplier, 0));
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
        private static bool Prefix(EFT.Animations.ProceduralWeaponAnimation __instance, ref float __result)
        {
            PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_firearmAnimationData").GetValue(__instance);
            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
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

    public class CalibrationLookAt : ModulePatch
    {
        private static float recordedDistance = 0f;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(ProceduralWeaponAnimation).GetMethod("method_7", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static void PatchPrefix(ProceduralWeaponAnimation __instance, ref Vector3 point)
        {
            if (__instance.CurrentAimingMod != null && !__instance.CurrentScope.IsOptic && Plugin.ScopeID != null && Plugin.ScopeID != "")
            {
                float distance = __instance.CurrentAimingMod.GetCurrentOpticCalibrationDistance();

                if (recordedDistance != distance)
                {
                    Plugin.ZeroRecoilOffset = Vector2.zero;
                    if (Plugin.ZeroOffsetDict.ContainsKey(Plugin.ScopeID))
                    {
                        Plugin.ZeroOffsetDict[Plugin.ScopeID] = Plugin.ZeroRecoilOffset;
                    }
                }

                recordedDistance = distance;
                float factor = distance / 25f; //need to find default zero
                Vector3 recoilOffset = new Vector3(Plugin.ZeroRecoilOffset.x * factor, Plugin.ZeroRecoilOffset.y * factor);
                Vector3 target = point + new Vector3(Plugin.MouseRotation.x * factor * -Plugin.Parralax, Plugin.MouseRotation.y * factor * Plugin.Parralax, 0f);
                target = Utils.YourPlayer.MovementContext.CurrentState.Name == EPlayerState.Sidestep ? point : target;
                point = Vector3.Lerp(point, target, 0.35f) + recoilOffset;
            }
        }
    }

    public class CalibrationLookAtScope : ModulePatch
    {
        private static float recordedDistance = 0f;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(ProceduralWeaponAnimation).GetMethod("method_5", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static void PatchPrefix(ProceduralWeaponAnimation __instance, ref Vector3 point)
        {
            if (__instance.CurrentAimingMod != null && Plugin.ScopeID != null && Plugin.ScopeID != "")
            {
                float distance = __instance.CurrentAimingMod.GetCurrentOpticCalibrationDistance();

                if (recordedDistance != distance)
                {
                    Plugin.ZeroRecoilOffset = Vector2.zero;
                    if (Plugin.ZeroOffsetDict.ContainsKey(Plugin.ScopeID))
                    {
                        Plugin.ZeroOffsetDict[Plugin.ScopeID] = Plugin.ZeroRecoilOffset;
                    }
                }

                recordedDistance = distance;
                float factor = distance / 50f; //need to find default zero
                Vector3 recoilOffset = new Vector3(Plugin.ZeroRecoilOffset.x * factor, Plugin.ZeroRecoilOffset.y * factor);
                Vector3 target = point + new Vector3(Plugin.MouseRotation.x * factor * -Plugin.Parralax, Plugin.MouseRotation.y * factor * Plugin.Parralax, 0f);
                target = Utils.YourPlayer.MovementContext.CurrentState.Name == EPlayerState.Sidestep ? point : target;
                point = Vector3.Lerp(point, target, 0.35f) + recoilOffset;
            }
        }
    }
}
