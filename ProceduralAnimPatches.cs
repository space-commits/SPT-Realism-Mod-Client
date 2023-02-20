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
                    float baseSpeed = PlayerProperties.AimMoveSpeedBase;
                    __instance.AddStateSpeedLimit(Math.Max(baseSpeed * WeaponProperties.AimMoveSpeedModifier, 0.15f), Player.ESpeedLimit.Aiming);

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
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);
            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    //to find float_9 on new client version, look for: public float AimingSpeed { get{ return this.float_9; } }
                    //to finf float_19 again, it's set to ErgnomicWeight in this method.

                    float _aimsSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").GetValue(__instance);
                    SkillsClass.GClass1675 skillsClass = (SkillsClass.GClass1675)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "gclass1675_0").GetValue(__instance);
                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.StartingConvergence * __instance.Aiming.RecoilConvergenceMult;
                    __instance.HandsContainer.Recoil.Damping = WeaponProperties.TotalRecoilDamping;
                    __instance.HandsContainer.HandsPosition.Damping = WeaponProperties.TotalRecoilHandDamping;
                    float aimSpeed = _aimsSpeed * (1f + WeaponProperties.AimSpeedModifier) * WeaponProperties.GlobalAimSpeedModifier; //*PlayerProperties.StrengthSkillAimBuff
                    WeaponProperties.AimSpeed = aimSpeed;
                    Logger.LogWarning("base aim speed = " + _aimsSpeed);
                    Logger.LogWarning("total aimSpeed = " + aimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, aimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, WeaponProperties.ErgonomicWeight * PlayerProperties.StrengthSkillAimBuff); //this is only called once, so can't do injury multi. It's probably uncessary to set the value here anyway, it's more just-in-case.
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
                if (player.IsYourPlayer == true)
                {
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_2").SetValue(__instance, 0);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_10").SetValue(__instance, Mathf.Lerp(1f, Singleton<BackendConfigSettingsClass>.Instance.Stamina.AimingSpeedMultiplier, 0));

                    __result = 0;
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

        [PatchPrefix]
        private static bool Prefix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {

            Vector3 targetPosition = new Vector3(Plugin.offsetX.Value, Plugin.offsetY.Value, Plugin.offsetZ.Value);
            Vector3 targetRotation = new Vector3(Plugin.rotationX.Value, Plugin.rotationY.Value, Plugin.rotationZ.Value);
            Quaternion targetQuaternion = Quaternion.Euler(targetRotation);

            float float_14 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
            float aimSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").GetValue(__instance);
            Quaternion currentRotation = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").GetValue(__instance);
            float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").SetValue(__instance, __instance.HandsContainer.WeaponRootAnim.position);
            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_5").SetValue(__instance, __instance.HandsContainer.WeaponRootAnim.localRotation);
            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").SetValue(__instance, __instance.HandsContainer.WeaponRootAnim.rotation);

            Quaternion quaternion_6 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").GetValue(__instance);
            Vector3 vector3_4 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").GetValue(__instance);
            Vector3 vector3_6 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").GetValue(__instance);

            if (Input.GetKey(KeyCode.U))
            {
                currentRotation = Quaternion.Lerp(currentRotation, targetQuaternion, __instance.CameraSmoothTime * aimSpeed * dt);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                __instance.HandsContainer.WeaponRoot.position = targetPosition;
            }
            return true;
            /*           Vector3 targetPositiion = new Vector3(Plugin.offsetX.Value, Plugin.offsetY.Value, Plugin.offsetZ.Value);
                       Vector3 targetRotation = new Vector3(Plugin.rotationX.Value, Plugin.rotationY.Value, Plugin.rotationZ.Value);
                       Quaternion targetQuaternion = Quaternion.Euler(targetRotation);

                       *//*                __instance.HandsContainer.WeaponRootAnim.rotation = Quaternion.Euler(targetRotation);
                                       __instance.HandsContainer.WeaponRootAnim.localRotation = Quaternion.Euler(targetRotation);
                                       __instance.HandsContainer.WeaponRootAnim.localPosition = new Vector3(Plugin.offsetX.Value, Plugin.offsetY.Value, Plugin.offsetZ.Value);
                                       __instance.HandsContainer.WeaponRootAnim.position = new Vector3(Plugin.offsetX.Value, Plugin.offsetY.Value, Plugin.offsetZ.Value);*//*


                       float float_21 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_21").GetValue(__instance);
                       float float_13 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_13").GetValue(__instance);
                       float float_14 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                       float aimSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").GetValue(__instance);
                       Quaternion quaternion_1 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").GetValue(__instance);
                       Quaternion quaternion_2 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_2").GetValue(__instance);
                       bool bool_1 = (bool)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "bool_1").GetValue(__instance);
                       float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

                       Vector3 vector = __instance.HandsContainer.HandsRotation.Get();
                       Vector3 value = __instance.HandsContainer.SwaySpring.Value;
                       vector += float_21 * (bool_1 ? __instance.AimingDisplacementStr : 1f) * new Vector3(value.x, 0f, value.z);
                       vector += value;
                       Vector3 position = __instance._shouldMoveWeaponCloser ? __instance.HandsContainer.RotationCenterWoStock : __instance.HandsContainer.RotationCenter;
                       Vector3 worldPivot = __instance.HandsContainer.WeaponRootAnim.TransformPoint(position);

                       AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").SetValue(__instance, __instance.HandsContainer.WeaponRootAnim.position);
                       //these are probably supposed to be base values, setting them to my target doesn;t make ense.
                       AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_5").SetValue(__instance, __instance.HandsContainer.WeaponRootAnim.localRotation);
                       AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").SetValue(__instance, __instance.HandsContainer.WeaponRootAnim.rotation);

                       Quaternion quaternion_6 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").GetValue(__instance);
                       Vector3 vector3_4 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").GetValue(__instance);
                       Vector3 vector3_6 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").GetValue(__instance);

                       __instance.DeferredRotateWithCustomOrder(__instance.HandsContainer.WeaponRootAnim, worldPivot, vector);
                       Vector3 vector2 = __instance.HandsContainer.Recoil.Get();
                       if (vector2.magnitude > 1E-45f)
                       {
                           if (float_13 < 1f && __instance.ShotNeedsFovAdjustments)
                           {
                               vector2.x = Mathf.Atan(Mathf.Tan(vector2.x * 0.017453292f) * float_13) * 57.29578f;
                               vector2.z = Mathf.Atan(Mathf.Tan(vector2.z * 0.017453292f) * float_13) * 57.29578f;
                           }
                           Vector3 worldPivot2 = vector3_4 + quaternion_6 * __instance.HandsContainer.RecoilPivot;
                           __instance.DeferredRotate(__instance.HandsContainer.WeaponRootAnim, worldPivot2, quaternion_6 * vector2);
                       }
                       __instance.ApplyAimingAlignment(dt);
                       if (Input.GetKey(KeyCode.U))
                       {
                           Logger.LogWarning("vector3_4" + vector3_4);
                           quaternion_1 = Quaternion.Lerp(quaternion_1, targetQuaternion, __instance.CameraSmoothTime * aimSpeed * dt);
                           AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, quaternion_1);
                           Quaternion rhs = Quaternion.Euler(float_14 * Single_3 * vector3_6);
                           __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(vector3_4, quaternion_6 * rhs * quaternion_1);
                           return false;
                       }
                       else
                       {
                           Logger.LogWarning("else");
                           quaternion_1 = Quaternion.Lerp(quaternion_1, __instance.IsAiming ? quaternion_2 : Quaternion.identity, __instance.CameraSmoothTime * aimSpeed * dt);
                           Quaternion rhs = Quaternion.Euler(float_14 * Single_3 * vector3_6);
                           AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, quaternion_1);
                           __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(vector3_4, quaternion_6 * rhs * quaternion_1);
                       }
                       return true;*/

        }
    }


    public class ZeroAdjustmentsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ZeroAdjustments", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
  
            Logger.LogWarning("ZeroAdjustments");
        }
    }

    /* public class AlignCollimatorPatch : ModulePatch
     {
         protected override MethodBase GetTargetMethod()
         {
             return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("method_12", BindingFlags.Instance | BindingFlags.NonPublic);
         }

         [PatchPrefix]
         private static bool Prefix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
         {

             Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);
             bool bool_6 = (bool)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "bool_6").GetValue(__instance);
             Vector3 vector3_8 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_8").GetValue(__instance);
             float float_5 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_5").GetValue(__instance);
             var method_3 = AccessTools.Method(typeof(Weapon), "method_3");

             if (!bool_6)
             {
                 return false;
             }
             ProceduralWeaponAnimation.GClass2064 currentScope = __instance.CurrentScope;
             if (currentScope == null || currentScope.IsOptic || currentScope.ScopePrefabCache == null || !currentScope.ScopePrefabCache.HasCollimators)
             {
                 return false;
             }
             Transform lensCenter = currentScope.ScopePrefabCache.GetLensCenter();
             Vector3 position = (Vector3)method_3.Invoke(__instance, new object[] { vector3_8 });
             Transform weaponTransform = __instance.HandsContainer.Weapon;
             Vector3 lenseCenterVector = weaponTransform.InverseTransformPoint(lensCenter.position);
             Vector3 vector2 = weaponTransform.InverseTransformPoint(position);
             Vector3 vector3 = weaponTransform.InverseTransformPoint(currentScope.Bone.position);
             Vector2 vector4 = new Vector2(-lenseCenterVector.y, lenseCenterVector.z);
             Vector2 vector5 = new Vector2(-vector2.y, vector2.z);
             Vector2 vector6 = new Vector2(-vector3.y, vector3.z);
             float num = vector5.y - vector4.y;
             float num2 = vector5.x - vector4.x;
             float num3 = Mathf.Atan(num / num2);
             AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_5").SetValue(__instance, num3 * 57.29578f);

             if (__instance.PointOfView != EPointOfView.FirstPerson)
             {
                 return false;
             }
             Vector2 normalized = (vector4 - vector5).normalized;
             float d = vector4.x - vector6.x;
             Vector2 cameraShiftToLineOfSight = vector4 + normalized * d - vector6;
             __instance._cameraShiftToLineOfSight = cameraShiftToLineOfSight;
             return false;
         }
     }

     public class LerpCameraPatch : ModulePatch
     {
         protected override MethodBase GetTargetMethod()
         {
             return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("LerpCamera", BindingFlags.Instance | BindingFlags.Public);
         }

         [PatchPrefix]
         private static bool Prefix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
         {

             Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);
             Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
             if (player.IsYourPlayer == true)
             {
                 Vector3 _vCameraTarget = (Vector3)AccessTools.Field(typeof(EFT.Player.FirearmController), "_vCameraTarget").GetValue(__instance);
                 float float_10 = (float)AccessTools.Field(typeof(EFT.Player.FirearmController), "float_10").GetValue(__instance);
                 float float_9 = (float)AccessTools.Field(typeof(EFT.Player.FirearmController), "float_9").GetValue(__instance);
                 float float_16 = (float)AccessTools.Field(typeof(EFT.Player.FirearmController), "float_16").GetValue(__instance);
                 Player.ValueBlender valueBlender_0 = (Player.ValueBlender)AccessTools.Field(typeof(EFT.Player.FirearmController), "valueBlender_0").GetValue(__instance);
                 Player.ValueBlenderDelay valueBlenderDelay_0 = (Player.ValueBlenderDelay)AccessTools.Field(typeof(EFT.Player.FirearmController), "valueBlenderDelay_0").GetValue(__instance);
                 float Single_1 = (float)AccessTools.Field(typeof(EFT.Player.FirearmController), "Single_1").GetValue(__instance);
                 Quaternion quaternion_3 = (Quaternion)AccessTools.Field(typeof(EFT.Player.FirearmController), "quaternion_3").GetValue(__instance);
                 Quaternion quaternion_4 = (Quaternion)AccessTools.Field(typeof(EFT.Player.FirearmController), "quaternion_4").GetValue(__instance);
                 Vector3 vector3_2 = (Vector3)AccessTools.Field(typeof(EFT.Player.FirearmController), "vector3_2").GetValue(__instance);
                 Vector3 vector3_7 = (Vector3)AccessTools.Field(typeof(EFT.Player.FirearmController), "vector3_7").GetValue(__instance);

                 Vector3 localPosition = __instance.HandsContainer.CameraTransform.localPosition;
                 Vector2 a = new Vector2(localPosition.x, localPosition.y);
                 Vector2 b = new Vector2(_vCameraTarget.x, _vCameraTarget.y);
                 float num = __instance.IsAiming ? (float_9 * __instance.CameraSmoothBlender.Value * float_10) : __instance.CameraSmoothOut;
                 Vector2 vector = Vector2.Lerp(a, b, dt * num);
                 float num2 = localPosition.z;
                 float num3 = __instance.CameraSmoothTime * dt;
                 float num4 = __instance.IsAiming ? (1f + __instance.HandsContainer.HandsPosition.GetRelative().y * 100f + __instance.TurnAway.Position.y * 10f) : __instance.CameraSmoothOut;
                 num2 = Mathf.Lerp(num2, _vCameraTarget.z, num3 * num4);
                 Vector3 localPosition2 = new Vector3(vector.x, vector.y, num2) + __instance.HandsContainer.CameraPosition.GetRelative();
                 if (float_16 > 0f)
                 {
                     float value = valueBlender_0.Value;
                     if (__instance.IsAiming && value > 0f)
                     {
                         __instance.HandsContainer.SwaySpring.ApplyVelocity(vector3_2 * value);
                     }
                 }
                 __instance.HandsContainer.CameraTransform.localPosition = localPosition2;
                 Quaternion b2 = __instance.HandsContainer.CameraAnimatedFP.localRotation * __instance.HandsContainer.CameraAnimatedTP.localRotation;
                 __instance.HandsContainer.CameraTransform.localRotation = Quaternion.Lerp(quaternion_3, b2, Single_1 * (1f - valueBlenderDelay_0.Value)) * Quaternion.Euler(__instance.HandsContainer.CameraRotation.Get() + vector3_7) * quaternion_4;
                 Logger.LogWarning("====localPosition.=====");
                 Logger.LogWarning("x" + localPosition2.x);
                 Logger.LogWarning("y" + localPosition2.y);
                 Logger.LogWarning("z" + localPosition2.z);
                 Logger.LogWarning("========================");
                 Logger.LogWarning("====localRotation=====");
                 Logger.LogWarning("w" + __instance.HandsContainer.CameraTransform.localRotation.w);
                 Logger.LogWarning("x" + __instance.HandsContainer.CameraTransform.localRotation.x);
                 Logger.LogWarning("y" + __instance.HandsContainer.CameraTransform.localRotation.y);
                 Logger.LogWarning("z" + __instance.HandsContainer.CameraTransform.localRotation.z);
                 Logger.LogWarning("========================");

                 return false;
             }
             return true;
         }
     }
 */
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
                    float baseAimSpeed = WeaponProperties.AimSpeed * PlayerProperties.ADSInjuryMulti;
                    Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;
                    float sightSpeedModi = (currentAimingMod != null) ? AttachmentProperties.AimSpeed(currentAimingMod) : 1;
                    float newAimSpeed = baseAimSpeed * (1 + (sightSpeedModi / 100f));
                    Logger.LogWarning("aimSpeed = " + newAimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, newAimSpeed); //aimspeed

                    float ergoWeight = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * PlayerProperties.StrengthSkillAimBuff;
                    float ergoWeightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 6f);
                    float breathIntensity;
                    float handsIntensity;

                    if (WeaponProperties.HasShoulderContact == false)
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
                        Logger.LogWarning("Range finder sway");
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

                    if (WeaponProperties.HasShoulderContact == false && firearmController.Item.WeapClass != "pistol")
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
}
