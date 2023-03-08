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

namespace RealismMod
{
    public class SetAimingSlowdownPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1598).GetMethod("SetAimingSlowdown", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref GClass1598 __instance, bool isAiming)
        {

            Player player = (Player)AccessTools.Field(typeof(GClass1598), "player_0").GetValue(__instance);
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

                    SkillsClass.GClass1675 skillsClass = (SkillsClass.GClass1675)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "gclass1675_0").GetValue(__instance);
                    Player.ValueBlender valueBlender = (Player.ValueBlender)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "valueBlender_0").GetValue(__instance);

                    float singleItemTotalWeight = firearmController.Item.GetSingleItemTotalWeight();
                    float ergoWeight = WeaponProperties.ErgonomicWeight; // in future should decrease with skill buff

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
                    WeaponProperties.SightlessAimSpeed = aimSpeed;

                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, aimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, ergoWeight * PlayerProperties.StrengthSkillAimBuff); //this is only called once, so can't do injury multi. It's probably uncessary to set the value here anyway, it's more just-in-case.
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
                    float baseAimSpeed = WeaponProperties.SightlessAimSpeed * PlayerProperties.ADSInjuryMulti;
                    baseAimSpeed = firearmController.Item.WeapClass == "pistol" ? baseAimSpeed * 0.9f : baseAimSpeed;
                    Plugin.SightlessADSSpeed = baseAimSpeed;
                    Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;
                    float sightSpeedModi = (currentAimingMod != null) ? AttachmentProperties.AimSpeed(currentAimingMod) : 1;
                    float newAimSpeed = baseAimSpeed * (1 + (sightSpeedModi / 100f));
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, newAimSpeed); //aimspeed

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("newAimSpeed = " + newAimSpeed);
                        Logger.LogWarning("ADSInjuryMulti = " + PlayerProperties.ADSInjuryMulti);
                    }

                    Plugin.HasOptic = __instance.CurrentScope.IsOptic ? true : false;

                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * PlayerProperties.StrengthSkillAimBuff;
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
                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * PlayerProperties.StrengthSkillAimBuff;
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
                    
                    if (firearmController.Item.WeapClass == "pistol" && firearmController.Item.CalculateCellSize().X <= 2 && Plugin.EnableAltPistol.Value == true)
                    {
                        Vector3 pistolTargetRotation = new Vector3(Plugin.PistolRotationX.Value, Plugin.PistolRotationY.Value, Plugin.PistolRotationZ.Value);
                        Quaternion pistolTargetQuaternion = Quaternion.Euler(pistolTargetRotation);
                        Quaternion pistolMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.PistolAdditionalRotationX.Value, Plugin.PistolAdditionalRotationY.Value, Plugin.PistolAdditionalRotationZ.Value));
                        Quaternion pistolRevertQuaternion = Quaternion.Euler(Plugin.PistolResetRotationX.Value, Plugin.PistolResetRotationY.Value, Plugin.PistolResetRotationZ.Value);

                        float aimMulti = 1 - ((1f - Plugin.SightlessADSSpeed) * 0.5f);

                        __instance.HandsContainer.WeaponRoot.localPosition = new Vector3(Plugin.PistolTransformNewStartPosition.x, __instance.HandsContainer.TrackingTransform.localPosition.y, __instance.HandsContainer.TrackingTransform.localPosition.z);

                        if (!__instance.IsAiming)
                        {
                            currentRotation = Quaternion.Lerp(currentRotation, pistolTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.PistolRotationSpeedMulti.Value * aimMulti);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.PistolTransformNewStartPosition, Plugin.PistolPosSpeedMulti.Value * aimMulti * dt);
                            hasResetPistolPos = false;

                            if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.PistolTransformNewStartPosition)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, pistolMiniTargetQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.PistolAdditionalRotationSpeedMulti.Value * Plugin.SightlessADSSpeed);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            }
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && hasResetPistolPos != true)
                        {
                            currentRotation = Quaternion.Lerp(currentRotation, pistolRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.PistolResetRotationSpeedMulti.Value * aimMulti);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, Plugin.PistolPosResetSpeedMulti.Value * aimMulti * dt);
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition) 
                        {
                            hasResetPistolPos = true;
                        }
                    }
                    else
                    {
                        Vector3 activeAimTargetRotation = new Vector3(Plugin.ActiveAimRotationX.Value, Plugin.ActiveAimRotationY.Value, Plugin.ActiveAimRotationZ.Value);
                        Vector3 activeAimRevertRotation = new Vector3(Plugin.ActiveAimResetRotationX.Value, Plugin.ActiveAimResetRotationY.Value, Plugin.ActiveAimResetRotationZ.Value);
                        Quaternion activeAimTargetQuaternion = Quaternion.Euler(activeAimTargetRotation);
                        Quaternion activeAimMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimAdditionalRotationX.Value, Plugin.ActiveAimAdditionalRotationY.Value, Plugin.ActiveAimAdditionalRotationZ.Value));
                        Quaternion activeAimRevertQuaternion = Quaternion.Euler(activeAimRevertRotation);

                        Vector3 lowReadyTargetRotation = new Vector3(Plugin.LowReadyRotationX.Value, Plugin.LowReadyRotationY.Value, Plugin.LowReadyRotationZ.Value);
                        Quaternion lowReadyTargetQuaternion = Quaternion.Euler(lowReadyTargetRotation);
                        Quaternion lowReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.LowReadyAdditionalRotationX.Value, Plugin.LowReadyAdditionalRotationY.Value, Plugin.LowReadyAdditionalRotationZ.Value));
                        Quaternion lowReadyRevertQuaternion = Quaternion.Euler(Plugin.LowReadyResetRotationX.Value, Plugin.LowReadyResetRotationY.Value, Plugin.LowReadyResetRotationZ.Value);
                        Vector3 lowReadyTargetPosition = new Vector3(Plugin.LowReadyOffsetX.Value, Plugin.LowReadyOffsetY.Value, Plugin.LowReadyOffsetZ.Value);

                        Vector3 highReadyTargetRotation = new Vector3(Plugin.HighReadyRotationX.Value, Plugin.HighReadyRotationY.Value, Plugin.HighReadyRotationZ.Value);
                        Quaternion highReadyTargetQuaternion = Quaternion.Euler(highReadyTargetRotation);
                        Quaternion highReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.HighReadyAdditionalRotationX.Value, Plugin.HighReadyAdditionalRotationY.Value, Plugin.HighReadyAdditionalRotationZ.Value));
                        Quaternion highReadyRevertQuaternion = Quaternion.Euler(Plugin.HighReadyResetRotationX.Value, Plugin.HighReadyResetRotationY.Value, Plugin.HighReadyResetRotationZ.Value);
                        Vector3 highReadyTargetPosition = new Vector3(Plugin.HighReadyOffsetX.Value, Plugin.HighReadyOffsetY.Value, Plugin.HighReadyOffsetZ.Value);

                        Vector3 shortStockTargetRotation = new Vector3(Plugin.ShortStockRotationX.Value, Plugin.ShortStockRotationY.Value, Plugin.ShortStockRotationZ.Value);
                        Quaternion shortStockTargetQuaternion = Quaternion.Euler(shortStockTargetRotation);
                        Quaternion shortStockMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ShortStockAdditionalRotationX.Value, Plugin.ShortStockAdditionalRotationY.Value, Plugin.ShortStockAdditionalRotationZ.Value));
                        Quaternion shortStockRevertQuaternion = Quaternion.Euler(Plugin.ShortStockResetRotationX.Value, Plugin.ShortStockResetRotationY.Value, Plugin.ShortStockResetRotationZ.Value);
                        Vector3 shortStockTargetPosition = new Vector3(Plugin.ShortStockOffsetX.Value, Plugin.ShortStockOffsetY.Value, Plugin.ShortStockOffsetZ.Value);

                        //for setting baseline position
                        __instance.HandsContainer.WeaponRoot.localPosition = Plugin.WeaponOffsetPosition;

/*                        if (Plugin.IsShortStock == false)
                        {
                            __instance._shouldMoveWeaponCloser = Plugin.baseShouldMoveCloser;
                        }*/


                        ////short-stock////
                        if (Plugin.IsShortStock == true && !Plugin.IsActiveAiming && !Plugin.IsHighReady && !Plugin.IsLowReady && !__instance.IsAiming)
                        {
                            isResettingShortStock = false;
                            hasResetShortStock = false;
                            hasResetLowReady = true;
                            hasResetActiveAim = true;
                            hasResetHighReady = true;
/*                            __instance._shouldMoveWeaponCloser = true;
*/
                            currentRotation = Quaternion.Lerp(currentRotation, shortStockTargetQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.ShortStockRotationSpeedMulti.Value * Plugin.SightlessADSSpeed);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, shortStockTargetPosition, Plugin.SightlessADSSpeed * dt * Plugin.ShortStockSpeedMulti.Value);

                            if (__instance.HandsContainer.TrackingTransform.localPosition != shortStockTargetPosition)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, shortStockMiniTargetQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.ShortStockAdditionalRotationSpeedMulti.Value * Plugin.SightlessADSSpeed);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            }
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetShortStock && !Plugin.IsLowReady && !Plugin.IsActiveAiming && !Plugin.IsHighReady && !isResettingActiveAim && !isResettingHighReady && !isResettingLowReady)
                        {
                            isResettingShortStock = true;
                            currentRotation = Quaternion.Lerp(currentRotation, shortStockRevertQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.ShortStockResetRotationSpeedMulti.Value * Plugin.SightlessADSSpeed);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, Plugin.SightlessADSSpeed * dt * Plugin.ShortStockResetSpeedMulti.Value);
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetShortStock)
                        {
                            Logger.LogWarning("reset");

                            isResettingShortStock = false;
                            hasResetShortStock = true;
                        }

                        ////high ready////
                        if (Plugin.IsHighReady == true && !Plugin.IsActiveAiming && !Plugin.IsLowReady && !Plugin.IsShortStock && !__instance.IsAiming)
                        {
                            isResettingHighReady = false;
                            hasResetHighReady = false;
                            hasResetActiveAim = true;
                            hasResetLowReady = true;
                            hasResetShortStock = true;

                            currentRotation = Quaternion.Lerp(currentRotation, highReadyTargetQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.HighReadyRotationMulti.Value * Plugin.SightlessADSSpeed);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, highReadyTargetPosition, Plugin.SightlessADSSpeed * dt * Plugin.HighReadySpeedMulti.Value);

                            if (__instance.HandsContainer.TrackingTransform.localPosition != highReadyTargetPosition)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, highReadyMiniTargetQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value * Plugin.SightlessADSSpeed);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            }
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetHighReady && !Plugin.IsLowReady && !Plugin.IsActiveAiming && !Plugin.IsShortStock && !isResettingActiveAim && !isResettingLowReady && !isResettingShortStock)
                        {
                            isResettingHighReady = true;

                            currentRotation = Quaternion.Lerp(currentRotation, highReadyRevertQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.HighReadyResetRotationMulti.Value * Plugin.SightlessADSSpeed);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, Plugin.SightlessADSSpeed * dt * Plugin.HighReadyResetSpeedMulti.Value);
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetHighReady)
                        {
 
                            isResettingHighReady = false;
                            hasResetHighReady = true;
                        }

                        ////low ready////
                        if (Plugin.IsLowReady == true && !Plugin.IsActiveAiming && !Plugin.IsHighReady && !Plugin.IsShortStock && !__instance.IsAiming)
                        {
                            isResettingLowReady = false;
                            hasResetLowReady = false;
                            hasResetActiveAim = true;
                            hasResetHighReady = true;
                            hasResetShortStock = true;

                            currentRotation = Quaternion.Lerp(currentRotation, lowReadyTargetQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.LowReadyRotationMulti.Value * Plugin.SightlessADSSpeed);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, lowReadyTargetPosition, Plugin.SightlessADSSpeed * dt * Plugin.LowReadySpeedMulti.Value);

                            if (__instance.HandsContainer.TrackingTransform.localPosition != lowReadyTargetPosition)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, lowReadyMiniTargetQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.LowReadyAdditionalRotationSpeedMulti.Value * Plugin.SightlessADSSpeed);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            }
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetLowReady && !Plugin.IsActiveAiming && !Plugin.IsHighReady && !Plugin.IsShortStock && !isResettingActiveAim && !isResettingHighReady && !isResettingShortStock)
                        {
                            isResettingLowReady = true;
        
                            currentRotation = Quaternion.Lerp(currentRotation, lowReadyRevertQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.LowReadyResetRotationMulti.Value * Plugin.SightlessADSSpeed);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, Plugin.SightlessADSSpeed * dt * Plugin.LowReadyResetSpeedMulti.Value);
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetLowReady)
                        {
                            isResettingLowReady = false;
                            hasResetLowReady = true;
                        }

                        ////active aiming////
                        if (Plugin.IsActiveAiming == true && !__instance.IsAiming && !Plugin.IsLowReady && !Plugin.IsShortStock && !Plugin.IsHighReady)
                        {
                            isResettingActiveAim = false;
                            hasResetActiveAim = false;
                            hasResetLowReady = true;
                            hasResetHighReady = true;
                            hasResetShortStock = true;

                            currentRotation = Quaternion.Lerp(currentRotation, activeAimTargetQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.ActiveAimRotationSpeedMulti.Value * Plugin.SightlessADSSpeed);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.ActiveAimTransformTargetPosition, Plugin.SightlessADSSpeed * dt * Plugin.ActiveAimSpeedMulti.Value);

                            if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.ActiveAimTransformTargetPosition)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, activeAimMiniTargetQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.ActiveAimAdditionalRotationSpeedMulti.Value * Plugin.SightlessADSSpeed);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            }
                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetActiveAim && !Plugin.IsLowReady && !Plugin.IsHighReady && !Plugin.IsShortStock && !isResettingLowReady && !isResettingHighReady && !isResettingShortStock)
                        {
                            isResettingActiveAim = true;

                            currentRotation = Quaternion.Lerp(currentRotation, activeAimRevertQuaternion, __instance.CameraSmoothTime * Plugin.SightlessADSSpeed * dt * Plugin.ActiveAimResetRotationSpeedMulti.Value * Plugin.SightlessADSSpeed);
                            __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, Plugin.SightlessADSSpeed * dt * Plugin.ActiveAimResetSpeedMulti.Value);

                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                        }
                        else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && hasResetActiveAim == false)
                        {
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
