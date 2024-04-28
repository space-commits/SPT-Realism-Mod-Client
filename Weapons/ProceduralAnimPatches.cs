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
using WeaponSkillsClass = EFT.SkillManager.GClass1771;
using EFT.Animations.NewRecoil;
using StaminaLevelClass = GClass753<float>;
using ProcessorClass = GClass2213;

namespace RealismMod
{
    public class CamRecoilPatch : ModulePatch
    {
        private static FieldInfo cameraRecoilField;
        private static FieldInfo cameraRecoilRotateField;
        private static FieldInfo prevCameraTargetField;
        private static FieldInfo headRotationField;
        private static FieldInfo playerField;
        private static FieldInfo fcField;
        private static float camSpeed = 1f;

        protected override MethodBase GetTargetMethod()
        {
            cameraRecoilField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_cameraRecoilLerpTempSpeed");
            cameraRecoilRotateField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_currentRecoilCameraRotate");
            prevCameraTargetField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_previousCameraTargetRotation");
            headRotationField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec");
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("method_19", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ProceduralWeaponAnimation __instance, float deltaTime)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null) return false;
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                float _cameraRecoilLerpTempSpeed = (float)cameraRecoilField.GetValue(__instance);
                Quaternion _currentRecoilCameraRotate = (Quaternion)cameraRecoilRotateField.GetValue(__instance);
                Quaternion _previousCameraTargetRotation = (Quaternion)prevCameraTargetField.GetValue(__instance);

                __instance.HandsContainer.CameraRotation.ReturnSpeed = 0.2f;
                __instance.HandsContainer.CameraRotation.Damping = 0.55f;

                Vector3 _headRotationVec = (Vector3)headRotationField.GetValue(__instance);
                if (_headRotationVec != Vector3.zero)
                {
                    return false;
                }
   
                bool autoFireOn;
                if (((autoFireOn = (__instance.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect as NewRotationRecoilProcess).AutoFireOn) & __instance.IsAiming))
                {
                    if (!__instance.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.StableOn)
                    {
                        Quaternion localRotation = __instance.HandsContainer.CameraTransform.localRotation;
                        localRotation.y = 0f;
                        Quaternion quaternion = localRotation;
                        camSpeed = Mathf.Clamp(camSpeed + __instance.CameraToWeaponAngleStep * deltaTime, __instance.CameraToWeaponAngleSpeedRange.x, __instance.CameraToWeaponAngleSpeedRange.y) * 2f;
                        Quaternion quaternion2 = Quaternion.Lerp(_currentRecoilCameraRotate, quaternion, camSpeed);
                        __instance.HandsContainer.CameraTransform.localRotation = quaternion2;
                        prevCameraTargetField.SetValue(__instance, quaternion);
                        cameraRecoilRotateField.SetValue(__instance, quaternion2);
                        return false;
                    }
                    camSpeed = Mathf.Clamp(camSpeed + __instance.CameraToWeaponAngleStep * deltaTime, __instance.CameraToWeaponAngleSpeedRange.x, __instance.CameraToWeaponAngleSpeedRange.y) * 2f;
                    Quaternion newCameraRotation = Quaternion.Lerp(_currentRecoilCameraRotate, _previousCameraTargetRotation, camSpeed);
                    __instance.HandsContainer.CameraTransform.localRotation = newCameraRotation;
                    cameraRecoilRotateField.SetValue(__instance, newCameraRotation);

                    return false;
                }
                else
                {
                    if (!autoFireOn & __instance.IsAiming)
                    {
                        Quaternion cameraRotation = __instance.HandsContainer.CameraTransform.localRotation;
                        cameraRotation.y = 0f;
                        Quaternion cameraLocalRotaitonModified = cameraRotation;
                        camSpeed = Mathf.Clamp(camSpeed - __instance.CameraToWeaponAngleStep * deltaTime, __instance.CameraToWeaponAngleSpeedRange.x, __instance.CameraToWeaponAngleSpeedRange.y);
                        Quaternion newCameraRotaiton = Quaternion.Lerp(_currentRecoilCameraRotate, cameraLocalRotaitonModified, camSpeed * 2f);
                        __instance.HandsContainer.CameraTransform.localRotation = newCameraRotaiton;
                        prevCameraTargetField.SetValue(__instance, cameraLocalRotaitonModified);
                        cameraRecoilRotateField.SetValue(__instance, newCameraRotaiton);
                        return false;
                    }
                    if (!__instance.IsAiming)
                    {
                        Quaternion cameraRotation = __instance.HandsContainer.CameraTransform.localRotation;
                        camSpeed = Mathf.Clamp(camSpeed - __instance.CameraToWeaponAngleStep * deltaTime, __instance.CameraToWeaponAngleSpeedRange.x, __instance.CameraToWeaponAngleSpeedRange.y) * 2f;
                        cameraRecoilField.SetValue(__instance, camSpeed);
                        Quaternion newCameraRotation = Quaternion.Lerp(_currentRecoilCameraRotate, cameraRotation, camSpeed);
                        __instance.HandsContainer.CameraTransform.localRotation = newCameraRotation;
                        prevCameraTargetField.SetValue(__instance, cameraRotation);
                        cameraRecoilRotateField.SetValue(__instance, newCameraRotation);
                    }
                    return false;
                }
            }
            return false;
        }
    }

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
                WeaponSkillsClass skillsClass = (WeaponSkillsClass)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_buffInfo").GetValue(__instance);
                Player.ValueBlender valueBlender = (Player.ValueBlender)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimSwayBlender").GetValue(__instance);

                float ergoWeightFactor = weapon.GetSingleItemTotalWeight() * (1f - WeaponStats.PureErgoDelta) * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f));

                float baseAimspeed = Mathf.InverseLerp(1f, 80f, WeaponStats.TotalErgo) * 1.25f;
                float aimSpeed = Mathf.Clamp(baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)) * (1f + WeaponStats.ModAimSpeedModifier), 0.55f, 1.4f);
                valueBlender.Speed = __instance.SwayFalloff * aimSpeed * 4.35f;

                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimSwayStrength").SetValue(__instance, Mathf.InverseLerp(1f, 18f, ergoWeightFactor));

                __instance.UpdateSwayFactors();

                aimSpeed = weapon.WeapClass == "pistol" ? aimSpeed * 1.35f : aimSpeed;
                WeaponStats.SightlessAimSpeed = aimSpeed;
                WeaponStats.ErgoStanceSpeed = baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)) * (weapon.WeapClass == "pistol" ? 1.5f : 1f);

                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").SetValue(__instance, aimSpeed);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_ergonomicWeight").SetValue(__instance, WeaponStats.ErgoFactor * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f)));

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("========UpdateWeaponVariables=======");
                    Logger.LogWarning("total ergo = " + WeaponStats.TotalErgo);
                    Logger.LogWarning("aimSpeed = " + aimSpeed);
                    Logger.LogWarning("base aimSpeed = " + baseAimspeed);
                    Logger.LogWarning("total ergofactor = " + WeaponStats.ErgoFactor * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f)));
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
                float rndX = UnityEngine.Random.Range(8f * 0.7f * factor, 8f * factor);
                float rndY = UnityEngine.Random.Range(8f * 0.7f * factor, 8f * factor);
                Vector3 wiggleDir = new Vector3(-rndX, -rndY, 0f);

                if (pwa.IsAiming && !didAimWiggle)
                {
                    StanceController.DoWiggleEffects(player, pwa, fc.Weapon, wiggleDir, wiggleFactor: factor, isADS: true);
                    didAimWiggle = true;
                }
                else if (!pwa.IsAiming && didAimWiggle)
                {
                    didAimWiggle = false;
                }
                StanceController.DoDampingTimer = true;
            }
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null) return;
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                Weapon weapon = firearmController.Weapon;
                if (weapon != null) 
                {
                    __instance.Overweight = 0;
                    __instance.CrankRecoil = !Plugin.EnableCrank.Value || (!WeaponStats.HasShoulderContact && WeaponStats._WeapClass != "pistol") ? false : true;

                    float updateErgoWeight = firearmController.ErgonomicWeight; //force ergo weight to update

                    float accuracy = weapon.GetTotalCenterOfImpact(false);
                    float3Field.SetValue(firearmController, accuracy); //update accuracy value

                    Mod currentAimingMod = (__instance.CurrentAimingMod != null) ? __instance.CurrentAimingMod.Item as Mod : null;
                    WeaponStats.IsOptic = __instance.CurrentScope.IsOptic;
                    WeaponStats.IsCantedSight = __instance.CurrentScope.Rotation < 0f;

                    float totalPlayerWeight = PlayerState.TotalModifiedWeightMinusWeapon;
                    float stanceMulti = 
                        StanceController.IsIdle() ? 1.6f 
                        : StanceController.WasActiveAim || StanceController.CurrentStance == EStance.ActiveAiming ? 1.6f 
                        : StanceController.CurrentStance == EStance.HighReady || StanceController.CurrentStance == EStance.HighReady ? 1.25f 
                        : StanceController.StoredStance == EStance.LowReady || StanceController.CurrentStance == EStance.LowReady ? 1.3f 
                        : 1f;
                    float stockMulti = weapon.WeapClass != "pistol" && !WeaponStats.HasShoulderContact ? 0.75f : 1f;
                    float totalSightlessAimSpeed = WeaponStats.SightlessAimSpeed * PlayerState.ADSInjuryMulti * (Mathf.Max(PlayerState.RemainingArmStamPerc, 0.45f));
                    float sightSpeedModi = currentAimingMod != null ? AttachmentProperties.AimSpeed(currentAimingMod) : 1f;
                    sightSpeedModi = currentAimingMod != null && (currentAimingMod.TemplateId == "5c07dd120db834001c39092d" || currentAimingMod.TemplateId == "5c0a2cec0db834001b7ce47d") && __instance.CurrentScope.IsOptic ? 1f : sightSpeedModi;
                    float playerWeightADSFactor = 1f - (totalPlayerWeight / 200f);
                    float cantedFactor = WeaponStats.IsCantedSight ? 0.5f : 1f;
                    float totalSightedAimSpeed = Mathf.Clamp(totalSightlessAimSpeed * (1 + (sightSpeedModi / 100f)) * stanceMulti * stockMulti * playerWeightADSFactor * cantedFactor, 0.4f, 1.5f);
                    float newAimSpeed = Mathf.Max(totalSightedAimSpeed * PlayerState.ADSSprintMulti, 0.3f) * Plugin.GlobalAimSpeedModifier.Value;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").SetValue(__instance, newAimSpeed); //aimspeed
                    float aimingSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").GetValue(__instance); //aimspeed

                    float formfactor = WeaponStats.IsBullpup ? 0.75f : 1f;
                    float ergoWeight = WeaponStats.ErgoFactor * PlayerState.ErgoDeltaInjuryMulti * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f));
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_ergonomicWeight").SetValue(__instance, ergoWeight);
                    float ergoWeightFactor = StatCalc.ProceduralIntensityFactorCalc(weapon.GetSingleItemTotalWeight(), weapon.WeapClass == "pistol" ? 1f : 4f);
                    float playerWeightSwayFactor = 1f + (totalPlayerWeight / 200f);
                    float totalErgoFactor = 1f + ((ergoWeight * ergoWeightFactor * playerWeightSwayFactor) / 100f);
                    float breathIntensity;
                    float handsIntensity;

                    if (!WeaponStats.HasShoulderContact && weapon.WeapClass != "pistol")
                    {
                        breathIntensity = Mathf.Clamp(0.6f * totalErgoFactor, 0.45f, 1.01f);
                        handsIntensity = Mathf.Clamp(0.6f * totalErgoFactor, 0.45f, 1.05f);
                    }
                    else if (!WeaponStats.HasShoulderContact && weapon.WeapClass == "pistol")
                    {
                        breathIntensity = Mathf.Clamp(0.55f * totalErgoFactor, 0.4f, 0.9f);
                        handsIntensity = Mathf.Clamp(0.55f * totalErgoFactor, 0.4f, 0.95f);
                    }
                    else
                    {
                        breathIntensity = Mathf.Clamp(0.5f * totalErgoFactor * formfactor, 0.35f, 0.81f);
                        handsIntensity = Mathf.Clamp(0.5f * totalErgoFactor * formfactor, 0.35f, 0.86f);
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
                        Logger.LogWarning("player weight factor = " + playerWeightSwayFactor);
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
                float formfactor = WeaponStats.IsBullpup ? 0.75f : weapon.IsBeltMachineGun ? 1.4f : 1f;
                float weapWeight = weapon.GetSingleItemTotalWeight();
                float totalPlayerWeight = PlayerState.TotalModifiedWeight - weapWeight;
                float playerWeightFactor = 1f + (totalPlayerWeight / 200f);
                bool noShoulderContact = !WeaponStats.HasShoulderContact && weapon.WeapClass != "pistol";
                float ergoWeight = WeaponStats.ErgoFactor * PlayerState.ErgoDeltaInjuryMulti * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f)) * formfactor;
                float weightFactor = StatCalc.ProceduralIntensityFactorCalc(weapWeight, weapon.WeapClass == "pistol" ? 1f : 4f);
                float displacementModifier = noShoulderContact ? Plugin.SwayIntensity.Value * 0.95f : Plugin.SwayIntensity.Value * 0.48f;//lower = less drag
                float aimIntensity = noShoulderContact ? Plugin.SwayIntensity.Value * 0.86f : Plugin.SwayIntensity.Value * 0.51f;

                float displacementStrength = Mathf.Clamp((ergoWeight * weightFactor * playerWeightFactor) / 50f, 0.8f, 3f);
                float swayStrength = Mathf.Clamp((ergoWeight * weightFactor * playerWeightFactor) / 60f, 0.6f, 1.1f);

                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_displacementStr").SetValue(__instance, displacementStrength * displacementModifier * playerWeightFactor);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_swayStrength").SetValue(__instance, swayStrength);

                __instance.MotionReact.SwayFactors = new Vector3(swayStrength, __instance.IsAiming ? (swayStrength * 0.3f) : swayStrength, swayStrength) * Mathf.Clamp(aimIntensity * weightFactor * playerWeightFactor, aimIntensity, 1f); // the diving/tiling animation as you move weapon side to side.

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("=====UpdateSwayFactors====");
                    Logger.LogWarning("ergoWeight = " + ergoWeight);
                    Logger.LogWarning("weightFactor = " + weightFactor);
                    Logger.LogWarning("swayStrength = " + swayStrength);
                    Logger.LogWarning("displacementStrength = " + displacementStrength);
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

    public class BreathProcessPatch : ModulePatch
    {
        private static FieldInfo breathIntensityField;
        private static FieldInfo shakeIntensityField;
        private static FieldInfo breathFrequencyField;
        private static FieldInfo cameraSensetivityField;
        private static FieldInfo baseHipRandomAmplitudesField;
        private static FieldInfo shotEffectorField;
        private static FieldInfo handsRotationSpringField;
        private static FieldInfo processorsField;
        private static FieldInfo lackOfOxygenStrengthField;

        protected override MethodBase GetTargetMethod()
        {
            breathIntensityField = AccessTools.Field(typeof(BreathEffector), "_breathIntensity");
            shakeIntensityField = AccessTools.Field(typeof(BreathEffector), "_shakeIntensity");
            breathFrequencyField = AccessTools.Field(typeof(BreathEffector), "_breathFrequency");
            cameraSensetivityField = AccessTools.Field(typeof(BreathEffector), "_cameraSensetivity");
            baseHipRandomAmplitudesField = AccessTools.Field(typeof(BreathEffector), "_baseHipRandomAmplitudes");
            shotEffectorField = AccessTools.Field(typeof(BreathEffector), "_shotEffector");
            handsRotationSpringField = AccessTools.Field(typeof(BreathEffector), "_handsRotationSpring");
            lackOfOxygenStrengthField = AccessTools.Field(typeof(BreathEffector), "_lackOfOxygenStrength");
            processorsField = AccessTools.Field(typeof(BreathEffector), "_processors");

            return typeof(BreathEffector).GetMethod("Process", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(BreathEffector __instance, float deltaTime)
        {
            float breathFrequency = (float)AccessTools.Field(typeof(BreathEffector), "_breathFrequency").GetValue(__instance);
            float cameraSensetivity = (float)AccessTools.Field(typeof(BreathEffector), "_cameraSensetivity").GetValue(__instance);
            Vector2 baseHipRandomAmplitudes = (Vector2)AccessTools.Field(typeof(BreathEffector), "_baseHipRandomAmplitudes").GetValue(__instance);
            ShotEffector shotEffector = (ShotEffector)AccessTools.Field(typeof(BreathEffector), "_shotEffector").GetValue(__instance);
            Spring handsRotationSpring = (Spring)AccessTools.Field(typeof(BreathEffector), "_handsRotationSpring").GetValue(__instance);
            AnimationCurve lackOfOxygenStrength = (AnimationCurve)AccessTools.Field(typeof(BreathEffector), "_lackOfOxygenStrength").GetValue(__instance);
            ProcessorClass[] processors = (ProcessorClass[])AccessTools.Field(typeof(BreathEffector), "_processors").GetValue(__instance);

            float amplGain = Mathf.Sqrt(__instance.AmplitudeGain.Value);
            __instance.HipXRandom.Amplitude = Mathf.Clamp(baseHipRandomAmplitudes.x + amplGain, 0f, 3f);
            __instance.HipZRandom.Amplitude = Mathf.Clamp(baseHipRandomAmplitudes.y + amplGain, 0f, 3f);
            __instance.HipXRandom.Hardness = (__instance.HipZRandom.Hardness = __instance.Hardness.Value);
            shakeIntensityField.SetValue(__instance, 1f);
            bool isInjured = __instance.TremorOn || __instance.Fracture || Plugin.RealHealthController.ArmsAreIncapacitated || Plugin.RealHealthController.HasOverdosed;
            float intensityHolder = 1f;

            if (Time.time < __instance.StiffUntill)
            {
                float intensity = Mathf.Clamp(-__instance.StiffUntill + Time.time + 1f, isInjured ? 0.75f : 0.3f, 1f);
                breathIntensityField.SetValue(__instance, intensity * __instance.Intensity);
                shakeIntensityField.SetValue(__instance, intensity);
                intensityHolder = intensity;
            }
            else
            {
                float holdBreathBonusSway = __instance.Physical.HoldingBreath ? 0.4f : 1f;
                float holdBreathBonusUpDown = __instance.Physical.HoldingBreath ? 0.3f : 1f;
                float t = lackOfOxygenStrength.Evaluate(__instance.OxygenLevel);
                float b = __instance.IsAiming ? 0.75f : 1f;
                breathIntensityField.SetValue(__instance, Mathf.Clamp(Mathf.Lerp(4f, b, t), 1f, 1.5f) * __instance.Intensity * holdBreathBonusUpDown);
                breathFrequencyField.SetValue(__instance, Mathf.Clamp(Mathf.Lerp(4f, 1f, t), 1f, 2.5f) * deltaTime * holdBreathBonusSway);
                shakeIntensityField.SetValue(__instance, holdBreathBonusSway);
                cameraSensetivityField.SetValue(__instance, Mathf.Lerp(2f, 0f, t) * __instance.Intensity);
                breathFrequency = (float)AccessTools.Field(typeof(BreathEffector), "_breathFrequency").GetValue(__instance);
                cameraSensetivity = (float)AccessTools.Field(typeof(BreathEffector), "_cameraSensetivity").GetValue(__instance);
            }

            StaminaLevelClass staminaLevel = __instance.StaminaLevel;
            __instance.YRandom.Amplitude = __instance.BreathParams.AmplitudeCurve.Evaluate(staminaLevel);
            float stamFactor = __instance.BreathParams.Delay.Evaluate(staminaLevel);
            __instance.XRandom.MinMaxDelay = (__instance.YRandom.MinMaxDelay = new Vector2(stamFactor / 2f, stamFactor));
            __instance.YRandom.Hardness = __instance.BreathParams.Hardness.Evaluate(staminaLevel);
            float randomY = __instance.YRandom.GetValue(deltaTime);
            float randomX = __instance.XRandom.GetValue(deltaTime);
            handsRotationSpring.AddAcceleration(new Vector3(Mathf.Max(0f, -randomY) * (1f - staminaLevel) * 2f, randomY, randomX) * ((float)shakeIntensityField.GetValue(__instance) * __instance.Intensity));
            Vector3 breathVector = Vector3.zero;

            if (isInjured)
            {
                float tremorSpeed = __instance.TremorOn ? deltaTime : (deltaTime / 2f);
                tremorSpeed *= intensityHolder;
                float tremorXRandom = __instance.TremorXRandom.GetValue(tremorSpeed);
                float tremorYRandom = __instance.TremorYRandom.GetValue(tremorSpeed);
                float tremorZRnadom = __instance.TremorZRandom.GetValue(tremorSpeed);
                if ((__instance.Fracture || Plugin.RealHealthController.ArmsAreIncapacitated || Plugin.RealHealthController.HasOverdosed) && !__instance.IsAiming)
                {
                    tremorXRandom += Mathf.Max(0f, randomY) * Mathf.Lerp(1f, 100f / __instance.EnergyFractureLimit, staminaLevel);
                }
                breathVector = new Vector3(tremorXRandom, tremorYRandom, tremorZRnadom) * __instance.Intensity;
            }
            else if (!__instance.IsAiming && RecoilController.IsFiring)
            {
                breathVector = new Vector3(__instance.HipXRandom.GetValue(deltaTime), 0f, __instance.HipZRandom.GetValue(deltaTime)) * (__instance.Intensity * __instance.HipPenalty);
            }

            if (Vector3.SqrMagnitude(breathVector - shotEffector.CurrentRecoilEffect.HandRotationRecoilEffect.Offset) > 0.01f)
            {
                shotEffector.CurrentRecoilEffect.HandRotationRecoilEffect.Offset = Vector3.Lerp(shotEffector.CurrentRecoilEffect.HandRotationRecoilEffect.Offset, breathVector, 0.1f);
            }
            else
            {
                shotEffector.CurrentRecoilEffect.HandRotationRecoilEffect.Offset = breathVector;
            }

            processors[0].ProcessRaw(breathFrequency, (float)breathIntensityField.GetValue(__instance) * 0.75f);
            processors[1].ProcessRaw(breathFrequency, (float)breathIntensityField.GetValue(__instance) * cameraSensetivity * 0.75f);
            return false;
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
