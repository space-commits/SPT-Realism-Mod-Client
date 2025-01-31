using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using static EFT.Player;
using ProcessorClass = GClass2534;
using StaminaLevelClass = GClass816<float>;

namespace RealismMod
{
    class AimPunchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ForceEffector).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(ForceEffector __instance)
        {
            __instance.WiggleMagnitude = Singleton<BackendConfigSettingsClass>.Instance.AimPunchMagnitude * PluginConfig.AimPunchMulti.Value;
        }
    }

    //stop player camera following weapon muzzle
    public class CamRecoilPatch : ModulePatch
    {
        private static FieldInfo _cameraRecoilField;
        private static FieldInfo _cameraRecoilRotateField;
        private static FieldInfo _prevCameraTargetField;
        private static FieldInfo _headRotationField;
        private static FieldInfo _playerField;
        private static FieldInfo _fcField;
        private static float _camRecoilLerpTempSpeed = 1f;

        protected override MethodBase GetTargetMethod()
        {
            _cameraRecoilField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_cameraRecoilLerpTempSpeed");
            _cameraRecoilRotateField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_currentRecoilCameraRotate");
            _prevCameraTargetField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_previousCameraTargetRotation");
            _headRotationField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec");
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            _fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("method_19", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ProceduralWeaponAnimation __instance, float deltaTime)
        {
            FirearmController firearmController = (FirearmController)_fcField.GetValue(__instance);
            if (firearmController == null) return false;
            Player player = (Player)_playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                float _cameraRecoilLerpTempSpeed = (float)_cameraRecoilField.GetValue(__instance);
                Quaternion _currentRecoilCameraRotate = (Quaternion)_cameraRecoilRotateField.GetValue(__instance);
                Quaternion _previousCameraTargetRotation = (Quaternion)_prevCameraTargetField.GetValue(__instance);

                __instance.HandsContainer.CameraRotation.ReturnSpeed = 0.2f;
                __instance.HandsContainer.CameraRotation.Damping = 0.55f;

                Vector3 _headRotationVec = (Vector3)_headRotationField.GetValue(__instance);
                if (_headRotationVec != Vector3.zero)
                {
                    return false;
                }
                bool autoFireOn;
                if (((autoFireOn = (__instance.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect as NewRotationRecoilProcess).AutoFireOn) & __instance.IsAiming))
                {
                    if (!__instance.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.StableOn)
                    {
                        Quaternion baseLocalRotation = __instance.HandsContainer.CameraTransform.localRotation;
                        baseLocalRotation.y = 0f;
         /*               Quaternion rhs = Quaternion.Euler(current);*/
                        Quaternion newLocalRotation = baseLocalRotation; // * rhs //bsg uses this mmodifer
                        _camRecoilLerpTempSpeed = Mathf.Clamp(_camRecoilLerpTempSpeed + __instance.CameraToWeaponAngleStep * deltaTime, __instance.CameraToWeaponAngleSpeedRange.x, __instance.CameraToWeaponAngleSpeedRange.y) * 2f;
                        Quaternion newRecoilRotation = Quaternion.Lerp(_currentRecoilCameraRotate, newLocalRotation, _camRecoilLerpTempSpeed);
                        __instance.HandsContainer.CameraTransform.localRotation = newRecoilRotation;
                        _prevCameraTargetField.SetValue(__instance, newLocalRotation);
                        _cameraRecoilRotateField.SetValue(__instance, newRecoilRotation);
                        return false;
                    }
                    _camRecoilLerpTempSpeed = Mathf.Clamp(_camRecoilLerpTempSpeed + __instance.CameraToWeaponAngleStep * deltaTime, __instance.CameraToWeaponAngleSpeedRange.x, __instance.CameraToWeaponAngleSpeedRange.y) * 2f;
                    Quaternion newCameraRotation = Quaternion.Lerp(_currentRecoilCameraRotate, _previousCameraTargetRotation, _camRecoilLerpTempSpeed);
                    __instance.HandsContainer.CameraTransform.localRotation = newCameraRotation;
                    _cameraRecoilRotateField.SetValue(__instance, newCameraRotation);

                    return false;
                }
                else
                {
                    if (!autoFireOn & __instance.IsAiming)
                    {
                        Quaternion localCameraRotation = __instance.HandsContainer.CameraTransform.localRotation;
                        localCameraRotation.y = 0f;
          /*              Quaternion rhs = Quaternion.Euler(current);*/
                        Quaternion cameraLocalRotaitonModified = localCameraRotation; // * rhs //bsg uses this mmodifer
                        _camRecoilLerpTempSpeed = Mathf.Clamp(_camRecoilLerpTempSpeed - __instance.CameraToWeaponAngleStep * deltaTime, __instance.CameraToWeaponAngleSpeedRange.x, __instance.CameraToWeaponAngleSpeedRange.y);
                        Quaternion newCameraRotaiton = Quaternion.Lerp(_currentRecoilCameraRotate, cameraLocalRotaitonModified, _camRecoilLerpTempSpeed * 2f); //bsg does not multi by 2, can't remember why I do this :')
                        __instance.HandsContainer.CameraTransform.localRotation = newCameraRotaiton;
                        _prevCameraTargetField.SetValue(__instance, cameraLocalRotaitonModified);
                        _cameraRecoilRotateField.SetValue(__instance, newCameraRotaiton);
                        return false;
                    }
                    if (!__instance.IsAiming)
                    {
                        Quaternion cameraRotation = __instance.HandsContainer.CameraTransform.localRotation;
                        _camRecoilLerpTempSpeed = Mathf.Clamp(_camRecoilLerpTempSpeed - __instance.CameraToWeaponAngleStep * deltaTime, __instance.CameraToWeaponAngleSpeedRange.x, __instance.CameraToWeaponAngleSpeedRange.y) * 2f;  //bsg does not multi by 2, can't remember why I do this :')
                        _cameraRecoilField.SetValue(__instance, _camRecoilLerpTempSpeed);
                        Quaternion newCameraRotation = Quaternion.Lerp(_currentRecoilCameraRotate, cameraRotation, _camRecoilLerpTempSpeed); //__instance.CameraToWeaponAngleSpeedRange.y is what bsg uses
                        __instance.HandsContainer.CameraTransform.localRotation = newCameraRotation;
                        _prevCameraTargetField.SetValue(__instance, cameraRotation);
                        _cameraRecoilRotateField.SetValue(__instance, newCameraRotation);
                    }
                    return false;
                }
            }
            return false;
        }
    }

    //used to trigger update for aim and sway
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
            if (firearmController == null) return;
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                StatCalc.UpdateAimParameters(firearmController, __instance);
            }
        }
    }

    //used to frequently update contextual sway and  ADS speed
    public class PwaWeaponParamsPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo fcField;
        private static FieldInfo float3Field;
        private static bool didAimWiggle = false;

        private static void DoADSWiggle(ProceduralWeaponAnimation pwa, Player player, FirearmController fc, float factor)
        {
            if (StanceController.IsIdle() && WeaponStats.TotalWeaponWeight <= 8 &&  WeaponStats._WeapClass.ToLower() != "pistol")
            {
                StanceController.CanResetDamping = false;
                float mountingFactor = StanceController.IsMounting ? 0.1f : StanceController.IsBracing ? 0.25f : 1f;
                float headGearFactor = GearController.FSIsActive || GearController.NVGIsActive || GearController.HasGasMask ? 3f : 1f;
                float baseLine = Mathf.Clamp(3.5f * factor * headGearFactor * mountingFactor * WeaponStats.TotalAimStabilityModi, 0.1f, 17f);
                float rndX = UnityEngine.Random.Range(baseLine * 0.9f, baseLine);
                float rndY = UnityEngine.Random.Range(baseLine * 0.9f, baseLine);
                Vector3 wiggleDir = new Vector3(-rndX, -rndY, 0f);

                if (pwa.IsAiming && !didAimWiggle)
                {
                    if (!StanceController.IsFiringFromStance) StanceController.DoWiggleEffects(player, pwa, fc.Weapon, wiggleDir, wiggleFactor: factor, isADS: true);
                    didAimWiggle = true;
                }
                else if (!pwa.IsAiming && didAimWiggle)
                {
                    didAimWiggle = false;
                }
                StanceController.DoDampingTimer = true;
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            float3Field = AccessTools.Field(typeof(Player.FirearmController), "float_3");
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("method_23", BindingFlags.Instance | BindingFlags.Public);
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
                    __instance.CrankRecoil = PluginConfig.EnableCrank.Value;  // || (!WeaponStats.HasShoulderContact && WeaponStats._WeapClass != "pistol")

                    Mod currentAimingMod = (__instance.CurrentAimingMod != null) ? __instance.CurrentAimingMod.Item as Mod : null;
                    var aimingModStats = currentAimingMod == null ? null : Stats.GetDataObj<WeaponMod>(Stats.WeaponModStats, currentAimingMod.TemplateId);
                    WeaponStats.IsOptic = __instance.CurrentScope.IsOptic;
                    StatCalc.CalcSightAccuracy(currentAimingMod, aimingModStats);
                    float accuracy = weapon.GetTotalCenterOfImpact(false);
                    float3Field.SetValue(firearmController, accuracy); //update accuracy value

                    float totalPlayerWeight = PlayerValues.TotalModifiedWeightMinusWeapon;
                    float playerWeightADSFactor = 1f - (totalPlayerWeight / 200f);
                    float stanceMulti = 
                        StanceController.IsIdle() && !StanceController.IsLeftShoulder ? 1.75f 
                        : StanceController.WasActiveAim || StanceController.CurrentStance == EStance.ActiveAiming ? 1.65f 
                        : StanceController.CurrentStance == EStance.HighReady || StanceController.CurrentStance == EStance.HighReady ? 1.25f 
                        : StanceController.StoredStance == EStance.LowReady || StanceController.CurrentStance == EStance.LowReady ? 1.25f 
                        : StanceController.IsLeftShoulder ? 0.85f : 1f;
                    float stockMulti = weapon.WeapClass != "pistol" && !WeaponStats.HasShoulderContact ? 0.75f : 1f;

                    float totalSightlessAimSpeed = WeaponStats.SightlessAimSpeed * PlayerValues.ADSInjuryMulti * (Mathf.Max(PlayerValues.RemainingArmStamFactor, 0.35f));

                    float sightSpeedModi = currentAimingMod != null ? aimingModStats.AimSpeed : 1f;
                    sightSpeedModi = currentAimingMod != null && (currentAimingMod.TemplateId == "5c07dd120db834001c39092d" || currentAimingMod.TemplateId == "5c0a2cec0db834001b7ce47d") && __instance.CurrentScope.IsOptic ? 1f : sightSpeedModi;
                    float totalSightedAimSpeed = Mathf.Clamp(totalSightlessAimSpeed * (1 + (sightSpeedModi / 100f)) * stanceMulti * stockMulti * playerWeightADSFactor, 0.4f, 1.5f);
                   
                    float newAimSpeed = Mathf.Max(totalSightedAimSpeed * PlayerValues.ADSSprintMulti * Plugin.RealHealthController.AdrenalineADSBonus * (1f + WeaponStats.ModAimSpeedModifier), 0.28f) * (weapon.WeapClass == "pistol" ? PluginConfig.PistolGlobalAimSpeedModifier.Value : PluginConfig.GlobalAimSpeedModifier.Value);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").SetValue(__instance, newAimSpeed); //aimspeed

                    float leftShoulderFactor = StanceController.IsLeftShoulder ? 1.3f : 1f;
                    float formfactor = WeaponStats.IsBullpup ? 0.7f : 1f;
                    float ergoWeight = WeaponStats.ErgoFactor * PlayerValues.ErgoDeltaInjuryMulti * (1f - (PlayerValues.StrengthSkillAimBuff * 1.75f)) * (1f + (1f - PlayerValues.GearErgoPenalty));
                    float ergoWeightFactor = StatCalc.ProceduralIntensityFactorCalc(weapon.TotalWeight, weapon.WeapClass == "pistol" ? 1f : 4f);
                    float playerWeightSwayFactor = 1f + (totalPlayerWeight / 200f);
                    float totalErgoFactor = 1f + ((ergoWeight * ergoWeightFactor * playerWeightSwayFactor * leftShoulderFactor) / 100f);

                    float breathIntensity;
                    float handsIntensity;

                    if (!WeaponStats.HasShoulderContact && !WeaponStats.IsPistol)
                    {
                        breathIntensity = Mathf.Clamp(0.6f * totalErgoFactor, 0.47f, 1.01f);
                        handsIntensity = Mathf.Clamp(0.6f * totalErgoFactor, 0.47f, 1.05f);
                    }
                    else if (!WeaponStats.HasShoulderContact && (WeaponStats.IsStocklessPistol || WeaponStats.IsMachinePistol))
                    {
                        breathIntensity = Mathf.Clamp(0.55f * totalErgoFactor, 0.4f, 0.9f);
                        handsIntensity = Mathf.Clamp(0.55f * totalErgoFactor, 0.4f, 0.95f);
                    }
                    else
                    {
                        breathIntensity = Mathf.Clamp(0.4f * totalErgoFactor * formfactor, 0.33f, 0.85f);
                        handsIntensity = Mathf.Clamp(0.4f * totalErgoFactor * formfactor, 0.33f, 0.89f);
                    }

                    float chonkerFactor = weapon.Weight >= 9f ? 1.45f : 1f;
                    float totalBreathIntensity = breathIntensity * __instance.IntensityByPoseLevel * PluginConfig.ProceduralIntensity.Value * chonkerFactor;
                    float totalInputIntensitry = handsIntensity * handsIntensity * PluginConfig.ProceduralIntensity.Value * chonkerFactor;
                    PlayerValues.TotalBreathIntensity = totalBreathIntensity;
                    PlayerValues.TotalHandsIntensity = totalInputIntensitry;

                    //this is stupid, refactor
                    if (PlayerValues.HasFullyResetSprintADSPenalties)
                    {
                        __instance.Breath.Intensity = PlayerValues.TotalBreathIntensity * StanceController.BracingSwayBonus;
                        __instance.HandsContainer.HandsRotation.InputIntensity = PlayerValues.TotalHandsIntensity * StanceController.BracingSwayBonus;
                    }
                    else
                    {
                        __instance.Breath.Intensity = PlayerValues.SprintTotalBreathIntensity;
                        __instance.HandsContainer.HandsRotation.InputIntensity = PlayerValues.SprintTotalHandsIntensity;
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

                    if (PluginConfig.EnablePWALogging.Value == true)
                    {
                        Logger.LogWarning("=====method_23========");
                        Logger.LogWarning("ADSInjuryMulti = " + PlayerValues.ADSInjuryMulti);
                        Logger.LogWarning("remaining stam percentage = " + PlayerValues.RemainingArmStamFactor);
                        Logger.LogWarning("strength = " + PlayerValues.StrengthSkillAimBuff);
                        Logger.LogWarning("player weight = " + playerWeightADSFactor);
                        Logger.LogWarning("sightSpeedModi = " + sightSpeedModi);
                        Logger.LogWarning("totalSightlessAimSpeed = " + totalSightlessAimSpeed);
                        Logger.LogWarning("totalSightedAimSpeed = " + totalSightedAimSpeed);
                        Logger.LogWarning("newAimSpeed = " + newAimSpeed);
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

    //used to update weapon inertia
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

        private static float GetStanceFactor(ProceduralWeaponAnimation pwa, bool forDisplacement = false)
        {
            if (forDisplacement) 
            {
              return
              StanceController.IsMounting ? 0.2f :
              StanceController.IsBracing ? 0.35f :
              StanceController.IsLeftShoulder ? 1.15f :
              StanceController.CurrentStance == EStance.ShortStock ? 0.75f :
              StanceController.CurrentStance == EStance.HighReady ? 0.91f :
              StanceController.CurrentStance == EStance.LowReady ? 0.87f :
              StanceController.CurrentStance == EStance.ActiveAiming ? 0.95f : 
              1f;
            }

            return
            StanceController.IsMounting ? 0.05f :
            StanceController.IsBracing ? 0.1f :
            StanceController.IsLeftShoulder && !pwa.IsAiming ? 1.15f :
            StanceController.IsLeftShoulder ? 0.87f :
            pwa.IsAiming ? 0.75f :
            WeaponStats.TotalWeaponWeight > 1.6f && StanceController.CurrentStance == EStance.PistolCompressed ? 0.85f :
            StanceController.CurrentStance == EStance.PistolCompressed ? 1.15f :
            StanceController.CurrentStance == EStance.ShortStock ? 0.8f :
            StanceController.CurrentStance == EStance.HighReady ? 0.85f :
            StanceController.CurrentStance == EStance.LowReady ? 0.8f :
            StanceController.CurrentStance == EStance.ActiveAiming ? 0.9f : 
            1f;
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null) return false;
            Player player = (Player)playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                Weapon weapon = firearmController.Weapon;
                bool isPistol = WeaponStats.IsStocklessPistol || WeaponStats.IsMachinePistol;

                //works well except for the fact that BSG inconistently modified procedural intensity when changing pose, so therefore my changes become incosnsitent.
               // float poseFactor = player.IsInPronePose ? PluginConfig.test1.Value : 1f + ((1f - player.MovementContext.PoseLevel) * PluginConfig.test2.Value);

                float stanceFactor = GetStanceFactor(__instance, true);
                float stanceFactorMotion = GetStanceFactor(__instance);
                float weapWeight = weapon.TotalWeight;
                float formfactor = WeaponStats.IsBullpup ? 0.75f : 1f;
                float totalPlayerWeight = PlayerValues.TotalModifiedWeight - weapWeight;
                float playerWeightFactor = 1f + (totalPlayerWeight / 200f);
                bool noShoulderContact = !WeaponStats.HasShoulderContact; //maybe don't include pistol
                float ergoWeight = WeaponStats.ErgoFactor * PlayerValues.ErgoDeltaInjuryMulti * (1f - (PlayerValues.StrengthSkillAimBuff)) * (1f + (1f - PlayerValues.GearErgoPenalty));
                float weightFactor = StatCalc.ProceduralIntensityFactorCalc(weapWeight, isPistol ? 1.3f : 2.4f); 
                float displacementModifier = noShoulderContact ? PluginConfig.ProceduralIntensity.Value * 0.95f : PluginConfig.ProceduralIntensity.Value * 0.48f; // lower = less drag
                float aimIntensity = noShoulderContact ? PluginConfig.ProceduralIntensity.Value * 0.86f : PluginConfig.ProceduralIntensity.Value * 0.51f;
                float displacementFactor = isPistol ? 17.5f : 15.8f;
                float displacementLowerLimit = isPistol ? 0.73f : 0.88f;
                float displacementUpperLimit = isPistol ? 2.1f : 5.9f;
                float swayStrengthFactor = isPistol ? 42f : 169f; 
                float swayStrengthLowerLimit = isPistol ? 0.34f : 0.53f;
                float swayStrengthUpperLimit = isPistol ? 0.84f : 1.09f;

                float combinedFactors = ergoWeight * weightFactor * playerWeightFactor * formfactor;
                float displacementStrength = Mathf.Clamp(combinedFactors / displacementFactor, displacementLowerLimit, displacementUpperLimit); // inertia
                displacementStrength *= stanceFactor * WeaponStats.TotalWeaponHandlingModi * (__instance.IsAiming ? 1.09f : 1f); // be careful, also affects initial ADS displacement
                float swayStrength = Mathf.Clamp(combinedFactors / swayStrengthFactor, swayStrengthLowerLimit, swayStrengthUpperLimit); // side to side
                swayStrength *= stanceFactor * WeaponStats.TotalWeaponHandlingModi;

                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_displacementStr").SetValue(__instance, displacementStrength * displacementModifier * playerWeightFactor);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_swayStrength").SetValue(__instance, swayStrength);

                __instance.MotionReact.SwayFactors = new Vector3(swayStrength, __instance.IsAiming ? (swayStrength * 0.3f) : swayStrength, swayStrength) * Mathf.Clamp(aimIntensity * weightFactor * playerWeightFactor, aimIntensity, 1f); // the diving/tiling animation as you move weapon side to side.

                float motionWeaponFactor = WeaponStats.IsStocklessPistol || WeaponStats.IsMachinePistol || !WeaponStats.HasShoulderContact ? 1.55f : WeaponStats.IsBullpup ? 0.8f : 1f;
                float motionUpperLimit = isPistol ? 1.45f : 2.55f;
                float motionLowerLimit = isPistol ? 1.3f : 1.35f;
                WeaponStats.BaseWeaponMotionIntensity = Mathf.Clamp(0.065f * stanceFactorMotion * ergoWeight * playerWeightFactor * motionWeaponFactor * WeaponStats.TotalWeaponHandlingModi, motionLowerLimit, motionUpperLimit) * 0.33f;

                float walkMotionStockFactor = WeaponStats.IsBullpup ? 0.8f : WeaponStats.IsMachinePistol || WeaponStats.IsStocklessPistol ? 1.4f : !WeaponStats.HasShoulderContact ? 1.45f : 1f;
                float weaponWalkMotionFactor = 2.1f * (WeaponStats.ErgoFactor / 100f) * WeaponStats.TotalWeaponHandlingModi;
                weaponWalkMotionFactor = Mathf.Pow(weaponWalkMotionFactor, 0.87f);
                WeaponStats.WalkMotionIntensity = weaponWalkMotionFactor * walkMotionStockFactor * playerWeightFactor * (1f + (1f - PlayerValues.GearErgoPenalty));

                if (PluginConfig.EnablePWALogging.Value == true)
                {
                    Logger.LogWarning("=====UpdateSwayFactors====");
                    Logger.LogWarning("ergoWeight = " + ergoWeight);
                    Logger.LogWarning("weightFactor = " + weightFactor);
                    Logger.LogWarning("swayStrength = " + swayStrength);
                    Logger.LogWarning("displacementStrength = " + displacementStrength);
                    Logger.LogWarning("displacementModifier = " + displacementModifier);
                    Logger.LogWarning("aimIntensity = " + aimIntensity);
                    Logger.LogWarning("Sway Factors = " + __instance.MotionReact.SwayFactors);
                    Logger.LogWarning("Motion Intensity = " + WeaponStats.BaseWeaponMotionIntensity);
                    Logger.LogWarning("Walk Motion Intensity = " + WeaponStats.WalkMotionIntensity);
                    Logger.LogWarning("PlayerState.GearErgoPenalty = " + PlayerValues.GearErgoPenalty);
                    Logger.LogWarning("ergoWeightFactor = " + weightFactor);
                    Logger.LogWarning("stanceFactor = " + stanceFactor);
                    Logger.LogWarning("has bipod " + (firearmController.HasBipod && firearmController.BipodState));
                }
                return false;
            }
            return true;
        }
    }

    //modifies BSG's weapon sway calcs, additional factors, etc.
    public class BreathProcessPatch : ModulePatch
    {
        private static FieldInfo breathIntensityField;
        private static FieldInfo shakeIntensityField;
        private static FieldInfo breathFrequencyField;
        private static FieldInfo cameraSensitivityField;
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
            cameraSensitivityField = AccessTools.Field(typeof(BreathEffector), "_cameraSensetivity");
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
                float modSwayFactor = Mathf.Pow(WeaponStats.TotalAimStabilityModi, 1.2f);
                float holdBreathBonusSway = (__instance.Physical.HoldingBreath ? 0.495f : 1f) * modSwayFactor;
                float holdBreathBonusUpDown = (__instance.Physical.HoldingBreath ? 0.275f : 1f) * modSwayFactor;
                float swayFactor = (WeaponStats.IsOptic ? PluginConfig.SwayIntensity.Value : PluginConfig.SwayIntensity.Value * 1.1f);
                float t = lackOfOxygenStrength.Evaluate(__instance.OxygenLevel);
                float b = __instance.IsAiming ? 0.75f : 1f;
                breathIntensityField.SetValue(__instance, Mathf.Clamp(Mathf.Lerp(4f, b, t), 1f, 1.5f) * __instance.Intensity * holdBreathBonusUpDown * swayFactor);
                breathFrequencyField.SetValue(__instance, Mathf.Clamp(Mathf.Lerp(4f, 1f, t), 1f, 2.5f) * deltaTime * holdBreathBonusSway * swayFactor);
                shakeIntensityField.SetValue(__instance, holdBreathBonusSway * swayFactor);
                cameraSensitivityField.SetValue(__instance, Mathf.Lerp(2f, 0f, t) * __instance.Intensity);
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
            else if (!__instance.IsAiming && ShootController.IsFiring)
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

    //override BSG's playerweight calc, I do my own
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

    //override BSG's playerweight calc, I do my own
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

    //used for zero loss mechanic for reddots
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

    //used for zero loss mechanic for optics
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

    //with BSG's recoil rework, player camera is much more closely attached to the weapon. This is a problem for the stances, making them behave strangely
    //this patch attempts to rectify that
    /*    public class CalculateCameraPatch : ModulePatch
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
            private static void PatchPostfix(
                EFT.Animations.ProceduralWeaponAnimation __instance, ref Vector3 ____vCameraTarget,
                ref Player.ValueBlenderDelay ____tacticalReload, ref float ____aimLeftStanceAdditionalOffset,
                ref GInterface139 ____firearmAnimationData, ref float ____blindfireStrength, ref Quaternion ____rotation90deg,
                ref bool ____crankRecoil, ref Vector3 ____localAimShift, ref float ____leftStanceCurrentCurveValue,
                ref float ____compensatoryScale, ref Vector3 ____cameraByFOVOffset, ref float ____animatorPoseBlend,
                ref Vector3 ___vector)
            {
                FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
                if (firearmController == null) return;
                Player player = (Player)playerField.GetValue(firearmController);
                if (player != null && player.IsYourPlayer)
                {
                    if (!__instance.HandsContainer.WeaponRootAnim)
                    {
                        return;
                    }
                    Vector3 a = (__instance.BlindfireBlender.Value > 0f) ? (__instance.BlindFireCamera * Mathf.Abs(__instance.BlindfireBlender.Value / 2f)) : (__instance.SideFireCamera * Mathf.Abs(__instance.BlindfireBlender.Value / 2f));
                    __instance.HandsContainer.CameraRotation.Zero = new Vector3(0f, 0f, __instance.SmoothedTilt * __instance.PossibleTilt) + a * ____blindfireStrength;
                    Vector3 vector = Vector3.zero;
                    foreach (ValueTuple<AnimatorPose, float, bool> valueTuple in __instance.ActiveBlends)
                    {
                        float d = valueTuple.Item1.Blend.Evaluate(valueTuple.Item2);
                        vector += valueTuple.Item1.CameraPosition * d;
                        __instance.HandsContainer.CameraRotation.Zero += valueTuple.Item1.CameraRotation * d;
                    }
                    if (__instance.IsAiming && Mathf.Approximately(__instance.BlindfireBlender.Value, 0f) && __instance.ScopeAimTransforms.Count > 0)
                    {
                        if (____tacticalReload.Value > Mathf.Epsilon)
                        {
                            ____vCameraTarget = ____rotation90deg * GClass746.GetPositionRelativeToParent(__instance.HandsContainer.Weapon, __instance.CurrentScope.Bone) + __instance.HandsContainer.WeaponRoot.localPosition + ____rotation90deg * __instance.HandsContainer.WeaponRootAnim.localPosition;
                        }
                        else
                        {
                            ____vCameraTarget = __instance.HandsContainer.WeaponRoot.parent.InverseTransformPoint(__instance.CurrentScope.Bone.position);
                        }
                        if (__instance._currentAimingPlane != null)
                        {
                            Transform aimPointParent = __instance.AimPointParent;
                            Matrix4x4 matrix4x = Matrix4x4.TRS(aimPointParent.position, aimPointParent.rotation, __instance.Vector3_0);
                            float num = Mathf.Min(__instance._currentAimingPlane.Depth, __instance._farPlane.Depth - __instance.HandsContainer.Weapon.localPosition.y);
                            ____localAimShift.y = (____crankRecoil ? (-num + __instance.PositionZeroSum.y * 2f) : (-num));
                            Vector3 point = matrix4x.MultiplyPoint3x4(____localAimShift);
                            Transform parent = __instance.HandsContainer.WeaponRoot.parent;
                            if (____firearmAnimationData != null && ____leftStanceCurrentCurveValue > 0f)
                            {
                                parent = __instance.HandsContainer.CameraTransform.parent;
                            }
                            matrix4x = Matrix4x4.TRS(parent.position, parent.rotation, __instance.Vector3_0).inverse;
                            if (__instance.method_18())
                            {
                                Vector3 direction = __instance.CurrentScope.Bone.forward * -1f;
                                if (__instance.CurrentScope.Bone.name == "aim_camera")
                                {
                                    direction = __instance.CurrentScope.Bone.up * -1f;
                                }
                                Vector3 vector2 = __instance.HandsContainer.WeaponRoot.parent.InverseTransformDirection(direction);
                                float d2 = matrix4x.MultiplyPoint3x4(point).z + __instance._fovCompensatoryDistance - __instance.TurnAway.Position.y + __instance._cameraShiftToLineOfSight.x - ____vCameraTarget.z;
                                Vector3 vCameraTarget = ____vCameraTarget + vector2.normalized * d2;
                                ____vCameraTarget = vCameraTarget;
                            }
                            else
                            {
                                ____vCameraTarget.z = matrix4x.MultiplyPoint3x4(point).z + __instance._fovCompensatoryDistance - __instance.TurnAway.Position.y + __instance._cameraShiftToLineOfSight.x;
                            }
                        }
                        ____vCameraTarget.y = ____vCameraTarget.y + __instance._cameraShiftToLineOfSight.y;
                        if (__instance.Boolean_0)
                        {
                            ____vCameraTarget.z = ____vCameraTarget.z + ____aimLeftStanceAdditionalOffset * (1f - ____compensatoryScale);
                        }
                        ____vCameraTarget += ___vector;
                        return;
                    }
                    ____vCameraTarget = __instance.HandsContainer.CameraOffset + ____cameraByFOVOffset + __instance.TurnAway.CameraShift;
                    ____vCameraTarget = ((____animatorPoseBlend > 0f) ? (____vCameraTarget + vector) : ____vCameraTarget);
                }
            }
        }*/
}
