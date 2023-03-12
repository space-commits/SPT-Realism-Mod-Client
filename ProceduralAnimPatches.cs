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

namespace RealismMod
{
    public class SetAimingSlowdownPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1601).GetMethod("SetAimingSlowdown", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref GClass1601 __instance, bool isAiming)
        {

            Player player = (Player)AccessTools.Field(typeof(GClass1601), "player_0").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (isAiming)
                {
                    //slow is hard set to 0.33 when called, 0.4-0.43 feels best.
                    float baseSpeed = PlayerProperties.AimMoveSpeedBase * WeaponProperties.AimMoveSpeedModifier;
                    float totalSpeed = Plugin.IsActiveAiming ? baseSpeed * 1.25f : baseSpeed;
                    __instance.AddStateSpeedLimit(Math.Max(totalSpeed, 0.15f), Player.ESpeedLimit.Aiming);

                    return false;
                }
                __instance.RemoveStateSpeedLimit(Player.ESpeedLimit.Aiming);
                return false;
            }
            return true;
        }
    }

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
                    float ergoWeight = WeaponProperties.ErgonomicWeight; //maybe apply sterngth skill buff, but might be OP

                    float ergo = Mathf.Clamp01(WeaponProperties.TotalErgo / 100f);
                    float t = Mathf.InverseLerp(0.6f, 9f, ergoWeight);
                    float a = Mathf.Lerp(2f, 2.4f, t);
                    float b = Mathf.Lerp(0.35f, 0.95f, t);
                    float t2 = (ergo < 0.25f) ? (0.25f + 3f * ergo * ergo) : (2f * ergo - ergo * ergo);
                    float aimSpeed = Mathf.Clamp(1f / Mathf.Lerp(a, b, t2) * (1f + skillsClass.AimSpeed) * (1f + WeaponProperties.ModAimSpeedModifier) * WeaponProperties.GlobalAimSpeedModifier, 0.3f, 1.3f);
                    valueBlender.Speed = __instance.SwayFalloff / aimSpeed;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_16").SetValue(__instance, Mathf.InverseLerp(3f, 8f, singleItemTotalWeight * (1f - ergo)));
                    __instance.UpdateSwayFactors();

                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.StartingConvergence * __instance.Aiming.RecoilConvergenceMult;
                    __instance.HandsContainer.Recoil.Damping = WeaponProperties.TotalRecoilDamping;
                    __instance.HandsContainer.HandsPosition.Damping = WeaponProperties.TotalRecoilHandDamping;
                    aimSpeed = firearmController.Item.WeapClass == "pistol" ? aimSpeed * 1.2f : aimSpeed;
                    WeaponProperties.SightlessAimSpeed = firearmController.Item.WeapClass == "pistol" ? Mathf.Min(aimSpeed, 1.2f) : Mathf.Min(aimSpeed, 0.9f);

                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, aimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, WeaponProperties.ErgonomicWeight * (1f - PlayerProperties.StrengthSkillAimBuff) * PlayerProperties.ErgoDeltaInjuryMulti);


                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("UpdateWeaponVariables");
                        Logger.LogWarning("total ergo = " + WeaponProperties.TotalErgo);
                        Logger.LogWarning("total ergo clamped= " + ergo);
                        Logger.LogWarning("aimSpeed = " + aimSpeed);
                        Logger.LogWarning("base ergoWeight = " + ergoWeight);
                        Logger.LogWarning("total ergoWeight = " + WeaponProperties.ErgonomicWeight * (1f - PlayerProperties.StrengthSkillAimBuff) * PlayerProperties.ErgoDeltaInjuryMulti);
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
                    float idleBonus = Utils.IsIdle() == true ? 1.1f : 1f;
                    float totalSightlessAimSpeed = WeaponProperties.SightlessAimSpeed * PlayerProperties.ADSInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.5f));
                    Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;
                    float sightSpeedModi = (currentAimingMod != null) ? AttachmentProperties.AimSpeed(currentAimingMod) : 1f;
                    float newAimSpeed = Mathf.Clamp(totalSightlessAimSpeed * (1 + (sightSpeedModi / 100f)) * idleBonus, 0.35f, 1f);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, newAimSpeed); //aimspeed

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("method_20");
                        Logger.LogWarning("newAimSpeed = " + newAimSpeed);
                        Logger.LogWarning("ADSInjuryMulti = " + PlayerProperties.ADSInjuryMulti);
                        Logger.LogWarning("remaining stam percentage = " + PlayerProperties.RemainingArmStamPercentage);
                        Logger.LogWarning("strength = " + PlayerProperties.StrengthSkillAimBuff);
                    }

                    Plugin.HasOptic = __instance.CurrentScope.IsOptic ? true : false;

                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - PlayerProperties.StrengthSkillAimBuff);
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
                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - PlayerProperties.StrengthSkillAimBuff);
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
                        Logger.LogWarning("UpdateSwayFactors");
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

    public class LaserLateUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LaserBeam).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(LaserBeam __instance)
        {
            if (Plugin.IsHighReady == true || Plugin.IsLowReady == true || Input.GetKey(Plugin.ActiveAimKeybind.Value.MainKey))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class InitTransformsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("InitTransforms", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {


            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    Plugin.WeaponStartPosition = __instance.HandsContainer.WeaponRoot.localPosition;
                    Plugin.WeaponOffsetPosition = __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX.Value, Plugin.WeapOffsetY.Value, Plugin.WeapOffsetZ.Value);
                    Plugin.PistolOffsetPostion = __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.PistolOffsetX.Value, __instance.HandsContainer.WeaponRoot.localPosition.y, __instance.HandsContainer.WeaponRoot.localPosition.z);
                    __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX.Value, Plugin.WeapOffsetY.Value, Plugin.WeapOffsetZ.Value);
                    Plugin.TransformBaseStartPosition = new Vector3(0.0f, 0.0f, 0.0f);
                    Plugin.ActiveAimTransformTargetPosition = Plugin.TransformBaseStartPosition + new Vector3(Plugin.ActiveAimOffsetX.Value, Plugin.ActiveAimOffsetY.Value, Plugin.ActiveAimOffsetZ.Value);
                    Plugin.PistolTransformNewStartPosition = Plugin.TransformBaseStartPosition + new Vector3(Plugin.PistolOffsetX.Value, Plugin.PistolOffsetY.Value, Plugin.PistolOffsetZ.Value);
                    Plugin.LowReadyTransformTargetPosition = Plugin.TransformBaseStartPosition + new Vector3(Plugin.LowReadyOffsetX.Value, Plugin.LowReadyOffsetY.Value, Plugin.LowReadyOffsetZ.Value);
                    Plugin.HighTransformTargetPosition = Plugin.TransformBaseStartPosition + new Vector3(Plugin.HighReadyOffsetX.Value, Plugin.HighReadyOffsetY.Value, Plugin.HighReadyOffsetZ.Value);
                }
            }
        }
    }


    public class ApplyComplexRotationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ApplyComplexRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        public static bool hasResetActiveAim = true;
        public static bool hasResetLowReady = true;
        public static bool hasResetHighReady = true;
        public static bool hasResetShortStock = true;
        public static bool hasResetPistolPos = true;

        public static bool isResettingActiveAim = false;
        public static bool isResettingLowReady = false;
        public static bool isResettingHighReady = false;
        public static bool isResettingShortStock = false;

        [PatchPrefix]
        private static bool Postfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {

            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    Quaternion currentRotation = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").GetValue(__instance);

                    if (firearmController.Item.WeapClass == "pistol" && WeaponProperties.HasShoulderContact == false && Plugin.EnableAltPistol.Value == true)
                    {
                        float aimMulti = Mathf.Clamp(WeaponProperties.SightlessAimSpeed * PlayerProperties.ADSInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.5f)), 0.5f, 1.3f);
                        float resetAimMulti = (1f - aimMulti) + 1f;
                        float intensity = Mathf.Max(3f * (1f - PlayerProperties.WeaponSkillErgo) * resetAimMulti, 1f);
                        float balanceFactor = 1f + (WeaponProperties.Balance / 100f);
                        balanceFactor = WeaponProperties.Balance > 0f ? balanceFactor  * - 1f : balanceFactor;

                        Vector3 pistolTargetRotation = new Vector3(Plugin.PistolRotationX.Value, Plugin.PistolRotationY.Value, Plugin.PistolRotationZ.Value);
                        Quaternion pistolTargetQuaternion = Quaternion.Euler(pistolTargetRotation);
                        Quaternion pistolMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.PistolAdditionalRotationX.Value, Plugin.PistolAdditionalRotationY.Value, Plugin.PistolAdditionalRotationZ.Value));
                        Quaternion pistolRevertQuaternion = Quaternion.Euler(Plugin.PistolResetRotationX.Value * balanceFactor, Plugin.PistolResetRotationY.Value, Plugin.PistolResetRotationZ.Value);

                        __instance.HandsContainer.WeaponRoot.localPosition = new Vector3(Plugin.PistolTransformNewStartPosition.x, __instance.HandsContainer.TrackingTransform.localPosition.y, __instance.HandsContainer.TrackingTransform.localPosition.z);

                        if (!__instance.IsAiming && !PlayerProperties.IsInReloadOpertation && !PlayerProperties.IsManipulatingWeapon && !Plugin.IsHighReady)
                        {
                            currentRotation = Quaternion.Lerp(currentRotation, pistolTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.PistolRotationSpeedMulti.Value * aimMulti);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.PistolTransformNewStartPosition, Plugin.PistolPosSpeedMulti.Value * aimMulti * dt);
                            hasResetPistolPos = false;

                            if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.PistolTransformNewStartPosition)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, pistolMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.PistolAdditionalRotationSpeedMulti.Value * aimMulti);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            }
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && hasResetPistolPos != true)
                        {
                            __instance.HandsContainer.HandsRotation.InputIntensity = intensity;

                            currentRotation = Quaternion.Lerp(currentRotation, pistolRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.PistolResetRotationSpeedMulti.Value * aimMulti);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, Plugin.PistolPosResetSpeedMulti.Value * aimMulti * dt);
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition) 
                        {
                            __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                            hasResetPistolPos = true;
                        }
                    }
                    else
                    {
                        float aimMulti = Mathf.Clamp(WeaponProperties.SightlessAimSpeed * PlayerProperties.ADSInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.55f)), 0.45f, 0.85f);
                        float resetAimMulti = (1f - aimMulti) + 1f;
                        float intensity = Mathf.Max(3f * (1f - PlayerProperties.AimSkillADSBuff) * resetAimMulti, 1f);

                        if (!Utils.IsIdle())
                        {
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, aimMulti);
                        }

                        Vector3 activeAimTargetRotation = new Vector3(Plugin.ActiveAimRotationX.Value * aimMulti, Plugin.ActiveAimRotationY.Value * aimMulti, Plugin.ActiveAimRotationZ.Value * aimMulti);
                        Vector3 activeAimRevertRotation = new Vector3(Plugin.ActiveAimResetRotationX.Value * resetAimMulti, Plugin.ActiveAimResetRotationY.Value * resetAimMulti, Plugin.ActiveAimResetRotationZ.Value * resetAimMulti);
                        Quaternion activeAimTargetQuaternion = Quaternion.Euler(activeAimTargetRotation);
                        Quaternion activeAimMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimAdditionalRotationX.Value * resetAimMulti, Plugin.ActiveAimAdditionalRotationY.Value * resetAimMulti, Plugin.ActiveAimAdditionalRotationZ.Value * resetAimMulti));
                        Quaternion activeAimRevertQuaternion = Quaternion.Euler(activeAimRevertRotation);

                        Vector3 lowReadyTargetRotation = new Vector3(Plugin.LowReadyRotationX.Value * aimMulti, Plugin.LowReadyRotationY.Value * aimMulti, Plugin.LowReadyRotationZ.Value * aimMulti);
                        Quaternion lowReadyTargetQuaternion = Quaternion.Euler(lowReadyTargetRotation);
                        Quaternion lowReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.LowReadyAdditionalRotationX.Value * resetAimMulti, Plugin.LowReadyAdditionalRotationY.Value * resetAimMulti, Plugin.LowReadyAdditionalRotationZ.Value * resetAimMulti));
                        Quaternion lowReadyRevertQuaternion = Quaternion.Euler(Plugin.LowReadyResetRotationX.Value * resetAimMulti, Plugin.LowReadyResetRotationY.Value * resetAimMulti, Plugin.LowReadyResetRotationZ.Value * resetAimMulti);
                        Vector3 lowReadyTargetPosition = new Vector3(Plugin.LowReadyOffsetX.Value, Plugin.LowReadyOffsetY.Value, Plugin.LowReadyOffsetZ.Value);

                        Vector3 highReadyTargetRotation = new Vector3(Plugin.HighReadyRotationX.Value * aimMulti, Plugin.HighReadyRotationY.Value * aimMulti, Plugin.HighReadyRotationZ.Value * aimMulti);
                        Quaternion highReadyTargetQuaternion = Quaternion.Euler(highReadyTargetRotation);
                        Quaternion highReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.HighReadyAdditionalRotationX.Value * resetAimMulti, Plugin.HighReadyAdditionalRotationY.Value * resetAimMulti, Plugin.HighReadyAdditionalRotationZ.Value * resetAimMulti));
                        Quaternion highReadyRevertQuaternion = Quaternion.Euler(Plugin.HighReadyResetRotationX.Value * resetAimMulti, Plugin.HighReadyResetRotationY.Value * resetAimMulti, Plugin.HighReadyResetRotationZ.Value * resetAimMulti);
                        Vector3 highReadyTargetPosition = new Vector3(Plugin.HighReadyOffsetX.Value, Plugin.HighReadyOffsetY.Value, Plugin.HighReadyOffsetZ.Value);

                        Vector3 shortStockTargetRotation = new Vector3(Plugin.ShortStockRotationX.Value * aimMulti, Plugin.ShortStockRotationY.Value * aimMulti, Plugin.ShortStockRotationZ.Value * aimMulti);
                        Quaternion shortStockTargetQuaternion = Quaternion.Euler(shortStockTargetRotation);
                        Quaternion shortStockMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ShortStockAdditionalRotationX.Value * resetAimMulti, Plugin.ShortStockAdditionalRotationY.Value * resetAimMulti, Plugin.ShortStockAdditionalRotationZ.Value * resetAimMulti));
                        Quaternion shortStockRevertQuaternion = Quaternion.Euler(Plugin.ShortStockResetRotationX.Value * resetAimMulti, Plugin.ShortStockResetRotationY.Value * resetAimMulti, Plugin.ShortStockResetRotationZ.Value * resetAimMulti);
                        Vector3 shortStockTargetPosition = new Vector3(Plugin.ShortStockOffsetX.Value, Plugin.ShortStockOffsetY.Value, Plugin.ShortStockOffsetZ.Value);

                        //for setting baseline position
                        __instance.HandsContainer.WeaponRoot.localPosition = Plugin.WeaponOffsetPosition;

                        /*                        if (Plugin.IsShortStock == false)
                                                {
                                                    __instance._shouldMoveWeaponCloser = Plugin.baseShouldMoveCloser;
                                                }*/

                        ////short-stock////
                        if (Plugin.IsShortStock == true && !Plugin.IsActiveAiming && !Plugin.IsHighReady && !Plugin.IsLowReady && !__instance.IsAiming && !Plugin.IsSprinting)
                        {
                            isResettingShortStock = false;
                            hasResetShortStock = false;
                            hasResetLowReady = true;
                            hasResetActiveAim = true;
                            hasResetHighReady = true;
/*                            __instance._shouldMoveWeaponCloser = true;
*/
                            currentRotation = Quaternion.Lerp(currentRotation, shortStockTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ShortStockRotationSpeedMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, shortStockTargetPosition, aimMulti * dt * Plugin.ShortStockSpeedMulti.Value);

                            if (__instance.HandsContainer.TrackingTransform.localPosition != shortStockTargetPosition)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, shortStockMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ShortStockAdditionalRotationSpeedMulti.Value);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            }
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetShortStock && !Plugin.IsLowReady && !Plugin.IsActiveAiming && !Plugin.IsHighReady && !isResettingActiveAim && !isResettingHighReady && !isResettingLowReady)
                        {
                            __instance.HandsContainer.HandsRotation.InputIntensity = intensity;

                            isResettingShortStock = true;
                            currentRotation = Quaternion.Lerp(currentRotation, shortStockRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ShortStockResetRotationSpeedMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, aimMulti * dt * Plugin.ShortStockResetSpeedMulti.Value);
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetShortStock)
                        {
                            __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                            isResettingShortStock = false;
                            hasResetShortStock = true;
                        }

                        ////high ready////
                        if (Plugin.IsHighReady == true && !Plugin.IsActiveAiming && !Plugin.IsLowReady && !Plugin.IsShortStock && !__instance.IsAiming)
                        {
                            float shortToHighMulti = 1f;
                            isResettingHighReady = false;
                            hasResetHighReady = false;
                            hasResetActiveAim = true;
                            hasResetLowReady = true;

                            if (!hasResetShortStock && __instance.HandsContainer.TrackingTransform.localPosition != highReadyTargetPosition)
                            {
                                shortToHighMulti = 0.8f;
                            }
                            if (__instance.HandsContainer.TrackingTransform.localPosition == highReadyTargetPosition)
                            {
                                hasResetShortStock = true;

                            }

                            currentRotation = Quaternion.Lerp(currentRotation, highReadyTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.HighReadyRotationMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, highReadyTargetPosition, aimMulti * dt * Plugin.HighReadySpeedMulti.Value * shortToHighMulti);

                            if (__instance.HandsContainer.TrackingTransform.localPosition != highReadyTargetPosition)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, highReadyMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            }
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetHighReady && !Plugin.IsLowReady && !Plugin.IsActiveAiming && !Plugin.IsShortStock && !isResettingActiveAim && !isResettingLowReady && !isResettingShortStock)
                        {
                            __instance.HandsContainer.HandsRotation.InputIntensity = intensity;

                            isResettingHighReady = true;

                            currentRotation = Quaternion.Lerp(currentRotation, highReadyRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.HighReadyResetRotationMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, aimMulti * dt * Plugin.HighReadyResetSpeedMulti.Value);
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetHighReady)
                        {
                            __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                            isResettingHighReady = false;
                            hasResetHighReady = true;
                        }

                        ////low ready////
                        if (Plugin.IsLowReady == true && !Plugin.IsActiveAiming && !Plugin.IsHighReady && !Plugin.IsShortStock && !__instance.IsAiming && !Plugin.IsSprinting)
                        {
                            float resetToLowReadySpeedMulti = 1f;
                            isResettingLowReady = false;
                            hasResetLowReady = false;
                     
                            if ((!hasResetHighReady  || !hasResetShortStock || !hasResetActiveAim) && __instance.HandsContainer.TrackingTransform.localPosition != lowReadyTargetPosition)
                            {
                                resetToLowReadySpeedMulti = 1.5f;
                            }
                            if (__instance.HandsContainer.TrackingTransform.localPosition == lowReadyTargetPosition) 
                            {
                                hasResetHighReady = true;
                                hasResetShortStock = true;
                                hasResetActiveAim = true;
                            }

                            currentRotation = Quaternion.Lerp(currentRotation, lowReadyTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.LowReadyRotationMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, lowReadyTargetPosition, aimMulti * dt * Plugin.LowReadySpeedMulti.Value * resetToLowReadySpeedMulti);

                            if (__instance.HandsContainer.TrackingTransform.localPosition != lowReadyTargetPosition)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, lowReadyMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.LowReadyAdditionalRotationSpeedMulti.Value);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            }
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetLowReady && !Plugin.IsActiveAiming && !Plugin.IsHighReady && !Plugin.IsShortStock && !isResettingActiveAim && !isResettingHighReady && !isResettingShortStock)
                        {
                            __instance.HandsContainer.HandsRotation.InputIntensity = intensity;

                            isResettingLowReady = true;
        
                            currentRotation = Quaternion.Lerp(currentRotation, lowReadyRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.LowReadyResetRotationMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, aimMulti * dt * Plugin.LowReadyResetSpeedMulti.Value);
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetLowReady)
                        {
                            __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                            isResettingLowReady = false;
                            hasResetLowReady = true;
                        }

                        ////active aiming////
                        if (Plugin.IsActiveAiming == true && !__instance.IsAiming && !Plugin.IsLowReady && !Plugin.IsShortStock && !Plugin.IsHighReady && !Plugin.IsSprinting)
                        {
                            isResettingActiveAim = false;
                            hasResetActiveAim = false;
                            hasResetLowReady = true;
                            hasResetHighReady = true;
                            hasResetShortStock = true;

                            currentRotation = Quaternion.Lerp(currentRotation, activeAimTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ActiveAimRotationSpeedMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.ActiveAimTransformTargetPosition, aimMulti * dt * Plugin.ActiveAimSpeedMulti.Value);

                            if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.ActiveAimTransformTargetPosition)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, activeAimMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ActiveAimAdditionalRotationSpeedMulti.Value);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            }
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetActiveAim && !Plugin.IsLowReady && !Plugin.IsHighReady && !Plugin.IsShortStock && !isResettingLowReady && !isResettingHighReady && !isResettingShortStock)
                        {
                            __instance.HandsContainer.HandsRotation.InputIntensity = intensity;

                            isResettingActiveAim = true;

                            currentRotation = Quaternion.Lerp(currentRotation, activeAimRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ActiveAimResetRotationSpeedMulti.Value);
                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, aimMulti * dt * Plugin.ActiveAimResetSpeedMulti.Value);

                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && hasResetActiveAim == false)
                        {
                            __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                            isResettingActiveAim = false;
                            hasResetActiveAim = true;
                        }
                    }
                }
            }
            return true;
        }
    }
}
