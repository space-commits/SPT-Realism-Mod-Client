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
using WeaponSkillsClass = EFT.SkillManager.GClass1768;

namespace RealismMod
{
    public class UpdateWeaponVariablesPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {

            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                Weapon weapon = firearmController.Weapon;
                float totalPlayerWeight = PlayerState.TotalModifiedWeightMinusWeapon;
                float playerWeightFactor = 1f - (totalPlayerWeight / 150f);

                WeaponSkillsClass skillsClass = (WeaponSkillsClass)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_buffInfo").GetValue(__instance);
                Player.ValueBlender valueBlender = (Player.ValueBlender)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimSwayBlender").GetValue(__instance);

                float singleItemTotalWeight = weapon.GetSingleItemTotalWeight();
                float ergoWeightFactor = WeaponStats.ErgoFactor * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f));

                float ergoFactor = Mathf.Clamp01(WeaponStats.TotalErgo / 100f);
                float baseAimspeed = Mathf.InverseLerp(1f, 80f, WeaponStats.TotalErgo) * 1.25f;
                float aimSpeed = Mathf.Clamp(baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)) * (1f + WeaponStats.ModAimSpeedModifier) * playerWeightFactor, 0.55f, 1.4f);
                valueBlender.Speed = (__instance.SwayFalloff / aimSpeed);

                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimSwayStrength").SetValue(__instance, Mathf.InverseLerp(3f, 10f, singleItemTotalWeight * (1f - ergoFactor)));

                __instance.UpdateSwayFactors();

                aimSpeed = weapon.WeapClass == "pistol" ? aimSpeed * 1.35f : aimSpeed;
                WeaponStats.SightlessAimSpeed = aimSpeed;
                WeaponStats.ErgoStanceSpeed = baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)) * (weapon.WeapClass == "pistol" ? 1.5f : 1f);

                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").SetValue(__instance, aimSpeed);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_ergonomicWeight").SetValue(__instance, WeaponStats.ErgoFactor * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f)) * PlayerState.ErgoDeltaInjuryMulti);

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("========UpdateWeaponVariables=======");
                    Logger.LogWarning("singleItemTotalWeight = " + singleItemTotalWeight);
                    Logger.LogWarning("total ergo = " + WeaponStats.TotalErgo);
                    Logger.LogWarning("total ergo clamped= " + ergoFactor);
                    Logger.LogWarning("aimSpeed = " + aimSpeed);
                    Logger.LogWarning("base aimSpeed = " + baseAimspeed);
                    Logger.LogWarning("base ergofactor = " + ergoFactor);
                    Logger.LogWarning("total ergofactor = " + WeaponStats.ErgoFactor * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f)) * PlayerState.ErgoDeltaInjuryMulti);
                }
            }
        }
    }

    public class PwaWeaponParamsPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;
        private static FieldInfo float3Field;
        private static bool didAimWiggle = false;

        protected override MethodBase GetTargetMethod()
        {
            float3Field = AccessTools.Field(typeof(Player.FirearmController), "float_3");
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("method_23", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void DoADSWiggle(ProceduralWeaponAnimation pwa, Player player, FirearmController fc, float factor)
        {
            if (StanceController.IsIdle() && WeaponStats._WeapClass.ToLower() != "pistol")
            {
                StanceController.CanResetDamping = false;
                float rndX = UnityEngine.Random.Range(10f * 0.5f * factor, (10f * factor));
                float rndY = UnityEngine.Random.Range(10f * 0.5f * factor, (10f * factor));
                Vector3 wiggleDir = new Vector3(-rndX, -rndY, 0f);

                if (pwa.IsAiming && !didAimWiggle)
                {
                    StanceController.DoWiggleEffects(player, pwa, fc, wiggleDir, false);
                    didAimWiggle = true;
                }
                else if (!pwa.IsAiming && didAimWiggle)
                {
                    StanceController.DoWiggleEffects(player, pwa, fc, -wiggleDir * 0.45f, false);
                    didAimWiggle = false;
                }
                StanceController.DoDampingTimer = true;
            }
        }


        [PatchPostfix]
        private static void PatchPostfix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                Weapon weapon = firearmController.Weapon;

                if (weapon != null) 
                {
                    __instance.Overweight = 0;
                    __instance.CrankRecoil = Plugin.EnableCrank.Value;

                    float updateErgoWeight = firearmController.ErgonomicWeight; //force ergo weight to update

                    float accuracy = weapon.GetTotalCenterOfImpact(false);
                    float3Field.SetValue(firearmController, accuracy); //update accuracy value

                    Mod currentAimingMod = (__instance.CurrentAimingMod != null) ? __instance.CurrentAimingMod.Item as Mod : null;

                    float stanceMulti = 
                        StanceController.IsIdle() ? 1.6f 
                        : StanceController.WasActiveAim || StanceController.CurrentStance == EStance.IsActiveAiming ? 1.5f 
                        : StanceController.CurrentStance == EStance.IsHighReady || StanceController.CurrentStance == EStance.IsHighReady ? 1.1f 
                        : StanceController.StoredStance == EStance.IsLowReady || StanceController.CurrentStance == EStance.IsLowReady ? 1.3f 
                        : 1f;
                    float stockMulti = weapon.WeapClass != "pistol" && !WeaponStats.HasShoulderContact ? 0.75f : 1f;
                    float totalSightlessAimSpeed = WeaponStats.SightlessAimSpeed * PlayerState.ADSInjuryMulti * (Mathf.Max(PlayerState.RemainingArmStamPerc, 0.5f));
                    float sightSpeedModi = currentAimingMod != null ? AttachmentProperties.AimSpeed(currentAimingMod) : 1f;
                    sightSpeedModi = currentAimingMod != null && (currentAimingMod.TemplateId == "5c07dd120db834001c39092d" || currentAimingMod.TemplateId == "5c0a2cec0db834001b7ce47d") && __instance.CurrentScope.IsOptic ? 1f : sightSpeedModi;
                    float totalSightedAimSpeed = Mathf.Clamp(totalSightlessAimSpeed * (1 + (sightSpeedModi / 100f)) * stanceMulti * stockMulti, 0.45f, 1.5f);
                    float newAimSpeed = Mathf.Max(totalSightedAimSpeed * PlayerState.ADSSprintMulti, 0.3f) * Plugin.GlobalAimSpeedModifier.Value;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").SetValue(__instance, newAimSpeed); //aimspeed
                    float aimingSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").GetValue(__instance); //aimspeed

                    WeaponStats.HasOptic = __instance.CurrentScope.IsOptic ? true : false;

                    float ergoWeight = WeaponStats.ErgoFactor * PlayerState.ErgoDeltaInjuryMulti * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f));
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_ergonomicWeight").SetValue(__instance, ergoWeight);
                    float totalPlayerWeight = PlayerState.TotalModifiedWeightMinusWeapon;
                    float ergoWeightFactor = StatCalc.ProceduralIntensityFactorCalc(weapon.GetSingleItemTotalWeight(), weapon.WeapClass == "pistol" ? 1f : 4f);
                    float playerWeightFactor = 1f + (totalPlayerWeight / 200f);
                    float totalErgoFactor = 1f + ((ergoWeight * ergoWeightFactor * playerWeightFactor) / 100f);
                    float breathIntensity;
                    float handsIntensity;

                    if (!WeaponStats.HasShoulderContact && weapon.WeapClass != "pistol")
                    {
                        breathIntensity = Mathf.Clamp(0.55f * totalErgoFactor, 0.45f, 1.01f);
                        handsIntensity = Mathf.Clamp(0.55f * totalErgoFactor, 0.45f, 1.05f);
                    }
                    else if (!WeaponStats.HasShoulderContact && weapon.WeapClass == "pistol")
                    {
                        breathIntensity = Mathf.Clamp(0.5f * totalErgoFactor, 0.4f, 0.9f);
                        handsIntensity = Mathf.Clamp(0.5f * totalErgoFactor, 0.4f, 0.95f);
                    }
                    else
                    {
                        breathIntensity = Mathf.Clamp(0.45f * totalErgoFactor, 0.35f, 0.81f);
                        handsIntensity = Mathf.Clamp(0.45f * totalErgoFactor, 0.35f, 0.86f);
                    }

                    float beltFedFactor = weapon.IsBeltMachineGun ? 1.45f : 1f;
                    float totalBreathIntensity = breathIntensity * __instance.IntensityByPoseLevel * Plugin.SwayIntensity.Value * beltFedFactor;
                    float totalInputIntensitry = handsIntensity * handsIntensity * Plugin.SwayIntensity.Value * beltFedFactor;
                    PlayerState.TotalBreathIntensity = totalBreathIntensity;
                    PlayerState.TotalHandsIntensity = totalInputIntensitry;

                    if (PlayerState.HasFullyResetSprintADSPenalties)
                    {
                        __instance.Breath.Intensity = PlayerState.TotalBreathIntensity * StanceController.BracingSwayBonus;
                        __instance.HandsContainer.HandsRotation.InputIntensity = PlayerState.TotalHandsIntensity * StanceController.BracingSwayBonus;
                    }
                    else
                    {
                        __instance.Breath.Intensity = PlayerState.SprintTotalBreathIntensity;
                        __instance.HandsContainer.HandsRotation.InputIntensity = PlayerState.SprintTotalHandsIntensity;
                    }


                    if (__instance.CurrentAimingMod != null)
                    {
                        string id = (__instance.CurrentAimingMod?.Item?.Id != null) ? __instance.CurrentAimingMod.Item.Id : "";
                        WeaponStats.ScopeID = id;
                        if (id != null)
                        {
                            if (WeaponStats.ZeroOffsetDict.TryGetValue(id, out Vector2 offset))
                            {
                                WeaponStats.ZeroRecoilOffset = offset;
                            }
                            else
                            {
                                WeaponStats.ZeroRecoilOffset = Vector2.zero;
                                WeaponStats.ZeroOffsetDict.Add(id, WeaponStats.ZeroRecoilOffset);
                            }
                        }
                    }

                    DoADSWiggle(__instance, player, firearmController, totalErgoFactor);

                    __instance.CameraSmoothRecoil = 1;
                    __instance.CameraToWeaponAngleSpeedRange = Vector2.zero;
                    __instance.CameraToWeaponAngleStep = 0;

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("=====method_23========");
                        Logger.LogWarning("ADSInjuryMulti = " + PlayerState.ADSInjuryMulti);
                        Logger.LogWarning("remaining stam percentage = " + PlayerState.RemainingArmStamPerc);
                        Logger.LogWarning("strength = " + PlayerState.StrengthSkillAimBuff);
                        Logger.LogWarning("sightSpeedModi = " + sightSpeedModi);
                        Logger.LogWarning("newAimSpeed = " + newAimSpeed);
                        Logger.LogWarning("_aimingSpeed = " + aimingSpeed);
                        Logger.LogWarning("breathIntensity = " + breathIntensity);
                        Logger.LogWarning("handsIntensity = " + handsIntensity);
                        Logger.LogWarning("ergoWeight = " + ergoWeight);
                        Logger.LogWarning("ergoWeightFactor = " + ergoWeightFactor);
                        Logger.LogWarning("totalErgoFactor = " + totalErgoFactor);
                        Logger.LogWarning("player weight factor = " + playerWeightFactor);
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
    }

    public class UpdateSwayFactorsPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateSwayFactors", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return false;
            }
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                Weapon weapon = firearmController.Weapon;
                float weapWeight = weapon.GetSingleItemTotalWeight();
                float totalPlayerWeight = PlayerState.TotalModifiedWeight - weapWeight;
                float playerWeightFactor = 1f + (totalPlayerWeight / 200f);
                float beltFedFactor = weapon.IsBeltMachineGun ? 1.45f : 1f;
                bool noShoulderContact = !WeaponStats.HasShoulderContact && weapon.WeapClass != "pistol";
                float ergoWeight = WeaponStats.ErgoFactor * PlayerState.ErgoDeltaInjuryMulti * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f));
                float weightFactor = StatCalc.ProceduralIntensityFactorCalc(weapWeight, weapon.WeapClass == "pistol" ? 1f : 4f) * beltFedFactor;
                float displacementModifier = noShoulderContact ? Plugin.SwayIntensity.Value * 0.95f : Plugin.SwayIntensity.Value * 0.48f;//lower = less drag
                float aimIntensity = noShoulderContact ? Plugin.SwayIntensity.Value * 0.95f : Plugin.SwayIntensity.Value * 0.57f;

                float weapDisplacement = EFTHardSettings.Instance.DISPLACEMENT_STRENGTH_PER_KG.Evaluate(ergoWeight * weightFactor);//delay from moving mouse to the weapon moving to center of screen.
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_displacementStr").SetValue(__instance, weapDisplacement * displacementModifier * playerWeightFactor);

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
                    Logger.LogWarning("ergoWeight = " + ergoWeight);
                    Logger.LogWarning("ergoWeightFactor = " + weightFactor);
                }
                return false;
            }
            return true;
        }
    }

    public class SetOverweightPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("set_Overweight", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Animations.ProceduralWeaponAnimation __instance, float value)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null) 
            {
                return false;
            }
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                Weapon weapon = firearmController.Weapon;
                __instance.Breath.Overweight = value;
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_overweight").SetValue(__instance, 0);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_overweightAimingMultiplier").SetValue(__instance, Mathf.Lerp(1f, Singleton<BackendConfigSettingsClass>.Instance.Stamina.AimingSpeedMultiplier, 0));
                __instance.Walk.Overweight = Mathf.Lerp(0f, Singleton<BackendConfigSettingsClass>.Instance.Stamina.WalkVisualEffectMultiplier, value);

                return false;
            }
            return true;
        }
    }


    public class GetOverweightPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("get_Overweight", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Animations.ProceduralWeaponAnimation __instance, ref float __result)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return false;
            }
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                __result = 0;
                return false;
            }
            return true;
        }
    }

    public class CalibrationLookAt : ModulePatch
    {
        private static float recordedDistance = 0f;
        private static Vector3 recoilOffset = Vector3.zero;
        private static Vector3 target = Vector3.zero;
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("method_7", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPrefix(ProceduralWeaponAnimation __instance, ref Vector3 point)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                if (__instance.CurrentAimingMod != null && !__instance.CurrentScope.IsOptic && WeaponStats.ScopeID != null && WeaponStats.ScopeID != "")
                {
                    float distance = __instance.CurrentAimingMod.GetCurrentOpticCalibrationDistance();

                    if (recordedDistance != distance)
                    {
                        WeaponStats.ZeroRecoilOffset = Vector2.zero;
                        if (WeaponStats.ZeroOffsetDict.ContainsKey(WeaponStats.ScopeID))
                        {
                            WeaponStats.ZeroOffsetDict[WeaponStats.ScopeID] = WeaponStats.ZeroRecoilOffset;
                        }
                    }

                    recordedDistance = distance;
                    float factor = distance / 25f; //need to find default zero
                    recoilOffset.x = WeaponStats.ZeroRecoilOffset.x * factor;
                    recoilOffset.y = WeaponStats.ZeroRecoilOffset.y * factor;
                    point += recoilOffset;
                }
            }
        }
    }

    public class CalibrationLookAtScope : ModulePatch
    {
        private static float recordedDistance = 0f;
        private static Vector3 recoilOffset = Vector3.zero;
        private static Vector3 target = Vector3.zero;
        private static FieldInfo playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("method_5", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPrefix(ProceduralWeaponAnimation __instance, ref Vector3 point)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null)
            {
                return;
            }
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                if (__instance.CurrentAimingMod != null && WeaponStats.ScopeID != null && WeaponStats.ScopeID != "")
                {
                    float distance = __instance.CurrentAimingMod.GetCurrentOpticCalibrationDistance();

                    if (recordedDistance != distance)
                    {
                        WeaponStats.ZeroRecoilOffset = Vector2.zero;
                        if (WeaponStats.ZeroOffsetDict.ContainsKey(WeaponStats.ScopeID))
                        {
                            WeaponStats.ZeroOffsetDict[WeaponStats.ScopeID] = WeaponStats.ZeroRecoilOffset;
                        }
                    }

                    recordedDistance = distance;
                    float factor = distance / 50f; //need to find default zero
                    recoilOffset.x = WeaponStats.ZeroRecoilOffset.x * factor;
                    recoilOffset.y = WeaponStats.ZeroRecoilOffset.y * factor;
                    point += recoilOffset;
                }
            }
        }
    }
}
