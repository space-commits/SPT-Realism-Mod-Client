using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static EFT.Player;

namespace RealismMod
{
    public static class StanceController 
    {
        public static string[] botsToUseTacticalStances = { "sptBear", "sptUsec", "exUsec", "pmcBot", "bossKnight", "followerBigPipe", "followerBirdEye", "bossGluhar", "followerGluharAssault", "followerGluharScout", "followerGluharSecurity", "followerGluharSnipe" };

        private static float clickDelay = 0.2f;
        private static float doubleClickTime;
        private static bool clickTriggered = true;
        public static int SelectedStance = 0;

        public static bool IsActiveAiming = false;
        public static bool PistolIsCompressed = false;
        public static bool IsHighReady = false;
        public static bool IsLowReady = false;
        public static bool IsShortStock = false;
        public static bool WasHighReady;
        public static bool WasLowReady;
        public static bool WasShortStock;
        public static bool WasActiveAim;

        public static bool IsFiringFromStance = false;
        public static float StanceShotTime = 0.0f;
        public static float ManipTime = 0.0f;

        public static float HighReadyBlackedArmTime = 0.0f;
        public static bool DoHighReadyInjuredAnim = false;

        public static bool SetAiming = false;
        public static bool SetActiveAiming = false;

        public static float HighReadyManipBuff = 1f;
        public static float HighReadyManipDebuff = 1f;
        public static float ActiveAimManipDebuff = 1f;
        public static float LowReadyManipBuff = 1f;

        public static bool CancelPistolStance = false;
        public static bool PistolIsColliding = false;
        public static bool CancelHighReady = false;
        public static bool CancelLowReady = false;
        public static bool CancelShortStock = false;
        public static bool CancelActiveAim = false;
        public static bool ResetStances = false;


        public static void SetStanceStamina(Player player, Player.FirearmController fc) 
        {
            if (fc.Item.WeapClass != "pistol")
            {
                if (!IsHighReady && !IsLowReady && !Plugin.IsAiming && !IsActiveAiming && !IsShortStock && Plugin.EnableIdleStamDrain.Value == true && !player.IsInPronePose)
                {
                    player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponProperties.ErgonomicWeight * 0.5f);
                }
                if (IsActiveAiming == true)
                {
                    player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponProperties.ErgonomicWeight * 0.3f);
                }
                else if (!Plugin.IsAiming && !Plugin.EnableIdleStamDrain.Value)
                {
                    player.Physical.Aim(0f);
                }
                if (IsHighReady == true && !IsLowReady && !Plugin.IsAiming && !IsShortStock)
                {
                    player.Physical.Aim(0f);
                    player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + ((1f - (WeaponProperties.ErgonomicWeight / 100f)) * 0.03f), player.Physical.HandsStamina.TotalCapacity);
                }
                if (IsLowReady == true && !IsHighReady && !Plugin.IsAiming && !IsShortStock)
                {
                    player.Physical.Aim(0f);
                    player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + ((1f - (WeaponProperties.ErgonomicWeight / 100f)) * 0.015f), player.Physical.HandsStamina.TotalCapacity);
                }
                if (IsShortStock == true && !IsHighReady && !Plugin.IsAiming && !IsLowReady)
                {
                    player.Physical.Aim(0f);
                    player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + ((1f - (WeaponProperties.ErgonomicWeight / 100f)) * 0.01f), player.Physical.HandsStamina.TotalCapacity);
                }
            }
            else
            {
                if (!Plugin.IsAiming)
                {
                    player.Physical.Aim(0f);
                    player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + ((1f - (WeaponProperties.ErgonomicWeight / 100f)) * 0.025f), player.Physical.HandsStamina.TotalCapacity);
                }
            }

            if (player.IsInventoryOpened == true || (player.IsInPronePose && !Plugin.IsAiming))
            {
                player.Physical.Aim(0f);
                player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + 0.04f, player.Physical.HandsStamina.TotalCapacity);
            }
        }

        public static void ResetStanceStamina(Player player) 
        {
            player.Physical.Aim(0f);
            player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + 0.04f, player.Physical.HandsStamina.TotalCapacity);
        }

        public static bool IsIdle()
        {
            return !IsActiveAiming && !IsHighReady && !IsLowReady && !IsShortStock && !WasHighReady && !WasLowReady && !WasShortStock ? true : false;
        }


        public static void StanceManipCancelTimer()
        {
            ManipTime += Time.deltaTime;

            if (ManipTime >= 0.35f)
            {
                CancelHighReady = false;
                CancelLowReady = false;
                CancelShortStock = false;
                CancelPistolStance = false;
                CancelActiveAim = false;
                ResetStances = false;
                ManipTime = 0f;
            }
        }


        public static void StanceShotTimer() 
        {
            StanceShotTime += Time.deltaTime;

            if (StanceShotTime >= 0.5f)
            {
                IsFiringFromStance = false;
                StanceShotTime = 0f;
            }
        }

        public static void StanceState() 
        {
            if (Utils.WeaponReady == true)
            {

                if (!Plugin.IsSprinting && WeaponProperties._WeapClass != "pistol")
                {

                    //cycle stances
                    if (Input.GetKeyUp(Plugin.CycleStancesKeybind.Value.MainKey))
                    {
                        if (Time.time <= doubleClickTime)
                        {
                            clickTriggered = true;
                            SelectedStance = 0;
                            IsHighReady = false;
                            IsLowReady = false;
                            IsShortStock = false;
                            IsActiveAiming = false;
                            WasHighReady = false;
                            WasLowReady = false;
                            WasShortStock = false;
                        }
                        else
                        {
                            clickTriggered = false;
                            doubleClickTime = Time.time + clickDelay;
                        }
                    }
                    else if (clickTriggered == false)
                    {
                        if (Time.time > doubleClickTime)
                        {
                            clickTriggered = true;
                            SelectedStance++;
                            SelectedStance = SelectedStance > 3 ? 1 : SelectedStance;
                            IsHighReady = SelectedStance == 1 ? true : false;
                            IsLowReady = SelectedStance == 2 ? true : false;
                            IsShortStock = SelectedStance == 3 ? true : false;
                            IsActiveAiming = false;
                            WasHighReady = IsHighReady;
                            WasLowReady = IsLowReady;
                            WasShortStock = IsShortStock;

                            if (IsHighReady == true && (PlayerProperties.RightArmBlacked == true || PlayerProperties.LeftArmBlacked == true))
                            {
                                DoHighReadyInjuredAnim = true;
                            }
                        }
                    }

                    //active aim
                    if (!Plugin.ToggleActiveAim.Value)
                    {
                        if (Input.GetKey(Plugin.ActiveAimKeybind.Value.MainKey) || (Input.GetKey(KeyCode.Mouse1) && !PlayerProperties.IsAllowedADS))
                        {
                            IsActiveAiming = true;
                            IsShortStock = false;
                            IsHighReady = false;
                            IsLowReady = false;
                            WasActiveAim = IsActiveAiming;
                            SetActiveAiming = true;
                        }
                        else if(SetActiveAiming == true)
                        {
                            IsActiveAiming = false;
                            IsHighReady = WasHighReady;
                            IsLowReady = WasLowReady;
                            IsShortStock = WasShortStock;
                            WasActiveAim = IsActiveAiming;
                            SetActiveAiming = false;
                        }
                    }
                    else
                    {
                        if (Input.GetKeyDown(Plugin.ActiveAimKeybind.Value.MainKey) || (Input.GetKeyDown(KeyCode.Mouse1) && !PlayerProperties.IsAllowedADS))
                        {
                            IsActiveAiming = !IsActiveAiming;
                            IsShortStock = false;
                            IsHighReady = false;
                            IsLowReady = false;
                            WasActiveAim = IsActiveAiming;
                            if (IsActiveAiming == false) 
                            {
                                IsHighReady = WasHighReady;
                                IsLowReady = WasLowReady;
                                IsShortStock = WasShortStock;
                            }
                        }
                    }

                    //short-stock
                    if (Input.GetKeyDown(Plugin.ShortStockKeybind.Value.MainKey))
                    {
                        IsShortStock = !IsShortStock;
                        IsHighReady = false;
                        IsLowReady = false;
                        IsActiveAiming = false;
                        WasActiveAim = IsActiveAiming;
                        WasHighReady = IsHighReady;
                        WasLowReady = IsLowReady;
                        WasShortStock = IsShortStock;
                    }

                    //high ready
                    if (Input.GetKeyDown(Plugin.HighReadyKeybind.Value.MainKey))
                    {
                        IsHighReady = !IsHighReady;
                        IsShortStock = false;
                        IsLowReady = false;
                        IsActiveAiming = false;
                        WasActiveAim = IsActiveAiming;
                        WasHighReady = IsHighReady;
                        WasLowReady = IsLowReady;
                        WasShortStock = IsShortStock;

                        if (IsHighReady == true && (PlayerProperties.RightArmBlacked == true || PlayerProperties.LeftArmBlacked == true)) 
                        {
                            DoHighReadyInjuredAnim = true;
                        }
                    }

                    //low ready
                    if (Input.GetKeyDown(Plugin.LowReadyKeybind.Value.MainKey))
                    {
                        IsLowReady = !IsLowReady;
                        IsHighReady = false;
                        IsActiveAiming = false;
                        IsShortStock = false;
                        WasActiveAim = IsActiveAiming;
                        WasHighReady = IsHighReady;
                        WasLowReady = IsLowReady;
                        WasShortStock = IsShortStock;
                    }

                    if (Plugin.IsAiming == true)
                    {
                        if (IsActiveAiming == true || WasActiveAim == true)
                        {
                            WasHighReady = false;
                            WasLowReady = false;
                            WasShortStock = false;
                        }
                        IsLowReady = false;
                        IsHighReady = false;
                        IsShortStock = false;
                        IsActiveAiming = false;
                        SetAiming = true;
                    }
                    else if (SetAiming == true)
                    {
                        IsLowReady = WasLowReady;
                        IsHighReady = WasHighReady;
                        IsShortStock = WasShortStock;
                        IsActiveAiming = WasActiveAim;
                        SetAiming = false;
                    }
                }

                if(DoHighReadyInjuredAnim == true)
                {
                    HighReadyBlackedArmTime += Time.deltaTime;
                    if (HighReadyBlackedArmTime >= 0.4f)
                    {
                        DoHighReadyInjuredAnim = false;
                        IsLowReady = true;
                        WasLowReady = IsLowReady;
                        IsHighReady = false;
                        WasHighReady = false;
                        HighReadyBlackedArmTime = 0f;
                    }
                }

                HighReadyManipBuff = IsHighReady == true ? 1.25f : 1f;
                HighReadyManipDebuff = IsHighReady == true ? 0.8f : 1f;
                ActiveAimManipDebuff = IsActiveAiming == true ? 0.8f : 1f;
                LowReadyManipBuff = IsLowReady == true ? 1.25f : 1f;


                if (ResetStances == true) 
                {
                    StanceManipCancelTimer();
                }

                if (Plugin.DidWeaponSwap == true || WeaponProperties._WeapClass == "pistol")
                {
                    SelectedStance = 0;
                    IsShortStock = false;
                    IsLowReady = false;
                    IsHighReady = false;
                    IsActiveAiming = false;
                    WasHighReady = false;
                    WasLowReady = false;
                    WasShortStock = false;
                    Plugin.DidWeaponSwap = false;
                }
            }

        }

        public static void DoPistolStances(bool isThirdPerson, ref EFT.Animations.ProceduralWeaponAnimation __instance, ref Quaternion currentRotation, float dt, ref bool hasResetPistolPos) 
        {
            float aimMulti = Mathf.Clamp(WeaponProperties.SightlessAimSpeed * PlayerProperties.ADSInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.5f)), 0.5f, 1.3f);
            float resetAimMulti = (1f - aimMulti) + 1f;
            float intensity = Mathf.Max(3f * (1f - PlayerProperties.WeaponSkillErgo) * resetAimMulti, 1f);
            float balanceFactor = 1f + (WeaponProperties.Balance / 100f);
            balanceFactor = WeaponProperties.Balance > 0f ? balanceFactor * -1f : balanceFactor;

            Vector3 pistolTargetRotation = new Vector3(Plugin.PistolRotationX.Value, Plugin.PistolRotationY.Value, Plugin.PistolRotationZ.Value);
            Quaternion pistolTargetQuaternion = Quaternion.Euler(pistolTargetRotation);
            Quaternion pistolMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.PistolAdditionalRotationX.Value, Plugin.PistolAdditionalRotationY.Value, Plugin.PistolAdditionalRotationZ.Value));
            Quaternion pistolRevertQuaternion = Quaternion.Euler(Plugin.PistolResetRotationX.Value * balanceFactor, Plugin.PistolResetRotationY.Value, Plugin.PistolResetRotationZ.Value);

            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, 1f);
            float pitch = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
            float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

            __instance.HandsContainer.WeaponRoot.localPosition = new Vector3(Plugin.PistolTransformNewStartPosition.x, __instance.HandsContainer.TrackingTransform.localPosition.y, __instance.HandsContainer.TrackingTransform.localPosition.z);

            if (!__instance.IsAiming && !StanceController.CancelPistolStance && !StanceController.PistolIsColliding)
            {
                __instance.CameraSmoothTime = 4f;

                StanceController.PistolIsCompressed = true;

                currentRotation = Quaternion.Lerp(currentRotation, pistolTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.PistolRotationSpeedMulti.Value * aimMulti);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.PistolTransformNewStartPosition, Plugin.PistolPosSpeedMulti.Value * aimMulti * dt);
                hasResetPistolPos = false;

                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = Plugin.ThirdPistolPosSpeedMulti.Value * aimMulti;
                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * new Vector3(Plugin.ThirdPistolOffsetX.Value, Plugin.ThirdPistolOffsetY.Value, Plugin.ThirdPistolOffsetZ.Value);
                }

                if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.PistolTransformNewStartPosition)
                {
                    currentRotation = Quaternion.Lerp(currentRotation, pistolMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.PistolAdditionalRotationSpeedMulti.Value * aimMulti);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                }
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && hasResetPistolPos != true)
            {
                __instance.CameraSmoothTime = 4f;

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                currentRotation = Quaternion.Lerp(currentRotation, pistolRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.PistolResetRotationSpeedMulti.Value * aimMulti);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, Plugin.PistolPosResetSpeedMulti.Value * aimMulti * dt);
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition)
            {
                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = 0.1f;
                }

                __instance.CameraSmoothTime = 8f;

                StanceController.PistolIsCompressed = false;

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                hasResetPistolPos = true;
            }
        }

        public static void DoRifleStances(ManualLogSource logger, bool isThirdPerson, ref EFT.Animations.ProceduralWeaponAnimation __instance, ref Quaternion currentRotation, float dt, ref bool isResettingShortStock, ref bool hasResetShortStock, ref bool hasResetLowReady, ref bool hasResetActiveAim, ref bool hasResetHighReady, ref bool isResettingHighReady, ref bool isResettingLowReady, ref bool isResettingActiveAim)
        {
            float aimMulti = Mathf.Clamp(WeaponProperties.SightlessAimSpeed * PlayerProperties.ADSInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.55f)), 0.5f, 0.95f);
            float resetAimMulti = (1f - aimMulti) + 1f;
            float intensity = Mathf.Max(2f * (1f - (PlayerProperties.AimSkillADSBuff * 0.5f)) * resetAimMulti, 1f);

            if (!StanceController.IsIdle())
            {
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, aimMulti);
            }

            bool isColliding = !__instance.OverlappingAllowsBlindfire;
            float collisionRotationFactor = isColliding ? 2f : 1f;
            float collisionPositionFactor = isColliding ? 2f : 1f;

            float thirdPersonHighReadyAddMulti = !isThirdPerson ? 1f : 1.2f;
            float thirdPersonLowReadyAddMulti = !isThirdPerson ? 1f : 2f;
            float thirdPersonShortStockAddtMulti = !isThirdPerson ? 1f : 2f;

            Vector3 activeAimTargetRotation = new Vector3(Plugin.ActiveAimRotationX.Value * aimMulti, Plugin.ActiveAimRotationY.Value * aimMulti, Plugin.ActiveAimRotationZ.Value * aimMulti);
            Vector3 activeAimRevertRotation = new Vector3(Plugin.ActiveAimResetRotationX.Value * resetAimMulti, Plugin.ActiveAimResetRotationY.Value * resetAimMulti, Plugin.ActiveAimResetRotationZ.Value * resetAimMulti);
            Quaternion activeAimTargetQuaternion = Quaternion.Euler(activeAimTargetRotation);
            Quaternion activeAimMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimAdditionalRotationX.Value * resetAimMulti, Plugin.ActiveAimAdditionalRotationY.Value * resetAimMulti, Plugin.ActiveAimAdditionalRotationZ.Value * resetAimMulti));
            Quaternion activeAimRevertQuaternion = Quaternion.Euler(activeAimRevertRotation);
            Vector3 activeTargetPostionThird = new Vector3(Plugin.ThirdActiveAimOffsetX.Value, Plugin.ThirdActiveAimOffsetY.Value, Plugin.ThirdActiveAimOffsetZ.Value);

            Vector3 lowReadyTargetRotation = new Vector3(Plugin.LowReadyRotationX.Value * aimMulti * collisionRotationFactor, Plugin.LowReadyRotationY.Value * aimMulti, Plugin.LowReadyRotationZ.Value * aimMulti);
            Quaternion lowReadyTargetQuaternion = Quaternion.Euler(lowReadyTargetRotation);
            Quaternion lowReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.LowReadyAdditionalRotationX.Value * resetAimMulti, Plugin.LowReadyAdditionalRotationY.Value * resetAimMulti, Plugin.LowReadyAdditionalRotationZ.Value * resetAimMulti));
            Quaternion lowReadyRevertQuaternion = Quaternion.Euler(Plugin.LowReadyResetRotationX.Value * resetAimMulti * thirdPersonLowReadyAddMulti, Plugin.LowReadyResetRotationY.Value * resetAimMulti * thirdPersonLowReadyAddMulti, Plugin.LowReadyResetRotationZ.Value * resetAimMulti * thirdPersonLowReadyAddMulti);
            Vector3 lowReadyTargetPosition = new Vector3(Plugin.LowReadyOffsetX.Value, Plugin.LowReadyOffsetY.Value, Plugin.LowReadyOffsetZ.Value);
            Vector3 lowReadyTargetPostionThird = new Vector3(Plugin.ThirdLowReadyOffsetX.Value, Plugin.ThirdLowReadyOffsetY.Value, Plugin.ThirdLowReadyOffsetZ.Value);

            Vector3 highReadyTargetRotation = new Vector3(Plugin.HighReadyRotationX.Value * aimMulti * collisionRotationFactor, Plugin.HighReadyRotationY.Value * aimMulti, Plugin.HighReadyRotationZ.Value * aimMulti);
            Quaternion highReadyTargetQuaternion = Quaternion.Euler(highReadyTargetRotation);
            Quaternion highReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.HighReadyAdditionalRotationX.Value * resetAimMulti * thirdPersonHighReadyAddMulti, Plugin.HighReadyAdditionalRotationY.Value * resetAimMulti * thirdPersonHighReadyAddMulti, Plugin.HighReadyAdditionalRotationZ.Value * resetAimMulti * thirdPersonHighReadyAddMulti));
            Quaternion highReadyRevertQuaternion = Quaternion.Euler(Plugin.HighReadyResetRotationX.Value * resetAimMulti * thirdPersonHighReadyAddMulti, Plugin.HighReadyResetRotationY.Value * resetAimMulti * thirdPersonHighReadyAddMulti, Plugin.HighReadyResetRotationZ.Value * resetAimMulti * thirdPersonHighReadyAddMulti);
            Vector3 highReadyTargetPosition = new Vector3(Plugin.HighReadyOffsetX.Value, Plugin.HighReadyOffsetY.Value, Plugin.HighReadyOffsetZ.Value);
            Vector3 highReadyTargetPostionThird = new Vector3(Plugin.ThirdHighReadyOffsetX.Value, Plugin.ThirdHighReadyOffsetY.Value, Plugin.ThirdHighReadyOffsetZ.Value);

            Vector3 shortStockTargetRotation = new Vector3(Plugin.ShortStockRotationX.Value * aimMulti, Plugin.ShortStockRotationY.Value * aimMulti, Plugin.ShortStockRotationZ.Value * aimMulti);
            Quaternion shortStockTargetQuaternion = Quaternion.Euler(shortStockTargetRotation);
            Quaternion shortStockMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ShortStockAdditionalRotationX.Value * resetAimMulti * thirdPersonShortStockAddtMulti, Plugin.ShortStockAdditionalRotationY.Value * resetAimMulti * thirdPersonShortStockAddtMulti, Plugin.ShortStockAdditionalRotationZ.Value * resetAimMulti * thirdPersonShortStockAddtMulti));
            Quaternion shortStockRevertQuaternion = Quaternion.Euler(Plugin.ShortStockResetRotationX.Value * resetAimMulti, Plugin.ShortStockResetRotationY.Value * resetAimMulti, Plugin.ShortStockResetRotationZ.Value * resetAimMulti);
            Vector3 shortStockTargetPosition = new Vector3(Plugin.ShortStockOffsetX.Value, Plugin.ShortStockOffsetY.Value, Plugin.ShortStockOffsetZ.Value);
            Vector3 shortTargetPostionThird = new Vector3(Plugin.ThirdShortStockOffsetX.Value, Plugin.ThirdShortStockOffsetY.Value, Plugin.ThirdShortStockOffsetZ.Value);

            //for setting baseline position
            __instance.HandsContainer.WeaponRoot.localPosition = Plugin.WeaponOffsetPosition;

            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, 1f);
            float pitch = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
            float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

            ////short-stock////
            if (StanceController.IsShortStock == true && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsLowReady && !__instance.IsAiming && !Plugin.IsSprinting && !StanceController.CancelShortStock)
            {
                __instance.CameraSmoothTime = 4f;

                float activeToShortMulti = 1f;
                float highToShort = 1f;
                isResettingShortStock = false;
                hasResetShortStock = false;
                hasResetLowReady = true;

                if (__instance.HandsContainer.TrackingTransform.localPosition != shortStockTargetPosition)
                {
                    if (!hasResetActiveAim)
                    {
                        activeToShortMulti = 1.15f;
                    }
                    if (!hasResetHighReady)
                    {
                        highToShort = 0.8f;
                    }
                }
                if (__instance.HandsContainer.TrackingTransform.localPosition == shortStockTargetPosition)
                {
                    hasResetActiveAim = true;
                    hasResetHighReady = true;
                }

                currentRotation = Quaternion.Lerp(currentRotation, shortStockTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ShortStockRotationSpeedMulti.Value);
                if (__instance.HandsContainer.TrackingTransform.localPosition != shortStockTargetPosition)
                {
                    currentRotation = Quaternion.Lerp(currentRotation, shortStockMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ShortStockAdditionalRotationSpeedMulti.Value);
                }

                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = Plugin.ThirdShortStockSpeedMulti.Value;
                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * shortTargetPostionThird;
                }

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, shortStockTargetPosition, aimMulti * dt * Plugin.ShortStockSpeedMulti.Value * activeToShortMulti * highToShort);

            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetShortStock && !StanceController.IsLowReady && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !isResettingActiveAim && !isResettingHighReady && !isResettingLowReady)
            {
                __instance.CameraSmoothTime = 4f;

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingShortStock = true;
                currentRotation = Quaternion.Lerp(currentRotation, shortStockRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ShortStockResetRotationSpeedMulti.Value);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, aimMulti * dt * Plugin.ShortStockResetSpeedMulti.Value);
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetShortStock)
            {
                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = 0.1f;
                }

                __instance.CameraSmoothTime = 8f;


                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                isResettingShortStock = false;
                hasResetShortStock = true;
            }

            ////high ready////
            if (StanceController.IsHighReady == true && !StanceController.IsActiveAiming && !StanceController.IsLowReady && !StanceController.IsShortStock && !__instance.IsAiming && !StanceController.IsFiringFromStance && !StanceController.CancelHighReady)
            {
                __instance.CameraSmoothTime = 4f;

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


                if (StanceController.DoHighReadyInjuredAnim == true)
                {
                    currentRotation = Quaternion.Lerp(currentRotation, highReadyTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.HighReadyRotationMulti.Value * 0.5f);
                    currentRotation = Quaternion.Lerp(currentRotation, highReadyMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value * 0.25f);
                    if (__instance.HandsContainer.TrackingTransform.localPosition != highReadyTargetPosition)
                    {
                        currentRotation = Quaternion.Lerp(currentRotation, highReadyMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value * 0.5f);
                        currentRotation = Quaternion.Lerp(currentRotation, lowReadyTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.HighReadyRotationMulti.Value * 0.25f);
                    }
                }
                else
                {
                    currentRotation = Quaternion.Lerp(currentRotation, highReadyTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.HighReadyRotationMulti.Value);

                    if (__instance.HandsContainer.TrackingTransform.localPosition != highReadyTargetPosition)
                    {
                        currentRotation = Quaternion.Lerp(currentRotation, highReadyMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value);
                    }
                }

                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = Plugin.ThirdHighReadySpeedMulti.Value * aimMulti;
                    if (!Plugin.IsSprinting)
                    {

                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * highReadyTargetPostionThird;
                    }
                    else
                    {
                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * new Vector3(highReadyTargetPostionThird.x, -0.2f, -0.025f);
                    }
                }

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, highReadyTargetPosition, aimMulti * dt * Plugin.HighReadySpeedMulti.Value * shortToHighMulti);

            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetHighReady && !StanceController.IsLowReady && !StanceController.IsActiveAiming && !StanceController.IsShortStock && !isResettingActiveAim && !isResettingLowReady && !isResettingShortStock)
            {
                __instance.CameraSmoothTime = 4f;

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingHighReady = true;

                currentRotation = Quaternion.Lerp(currentRotation, highReadyRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.HighReadyResetRotationMulti.Value);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, aimMulti * dt * Plugin.HighReadyResetSpeedMulti.Value);
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetHighReady)
            {
                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = 0.1f;
                }

                __instance.CameraSmoothTime = 8f;

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                isResettingHighReady = false;
                hasResetHighReady = true;
            }

            ////low ready////
            if (StanceController.IsLowReady == true && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsShortStock && !__instance.IsAiming && !Plugin.IsSprinting && !StanceController.IsFiringFromStance && !StanceController.CancelLowReady)
            {
                __instance.CameraSmoothTime = 4f;

                float resetToLowReadySpeedMulti = 1f;
                isResettingLowReady = false;
                hasResetLowReady = false;

                if ((!hasResetHighReady || !hasResetShortStock || !hasResetActiveAim) && __instance.HandsContainer.TrackingTransform.localPosition != lowReadyTargetPosition)
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
                if (__instance.HandsContainer.TrackingTransform.localPosition != lowReadyTargetPosition)
                {
                    currentRotation = Quaternion.Lerp(currentRotation, lowReadyMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.LowReadyAdditionalRotationSpeedMulti.Value);
                }
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = Plugin.ThirdLowReadySpeedMulti.Value * aimMulti;
                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * lowReadyTargetPostionThird;
                }

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, lowReadyTargetPosition, aimMulti * dt * Plugin.LowReadySpeedMulti.Value * resetToLowReadySpeedMulti);

            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetLowReady && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsShortStock && !isResettingActiveAim && !isResettingHighReady && !isResettingShortStock)
            {
                __instance.CameraSmoothTime = 4f;

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingLowReady = true;

                currentRotation = Quaternion.Lerp(currentRotation, lowReadyRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.LowReadyResetRotationMulti.Value);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, aimMulti * dt * Plugin.LowReadyResetSpeedMulti.Value);
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetLowReady)
            {
                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = 0.1f;
                }


                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                isResettingLowReady = false;
                hasResetLowReady = true;
            }

            ////active aiming////
            if (StanceController.IsActiveAiming == true && !__instance.IsAiming && !StanceController.IsLowReady && !StanceController.IsShortStock && !StanceController.IsHighReady && !Plugin.IsSprinting && !StanceController.CancelActiveAim)
            {
                __instance.CameraSmoothTime = 4f;

                float shortToActiveMulti = 1f;
                isResettingActiveAim = false;
                hasResetActiveAim = false;
                hasResetLowReady = true;
                hasResetHighReady = true;

                if (!hasResetShortStock && __instance.HandsContainer.TrackingTransform.localPosition != Plugin.ActiveAimTransformTargetPosition)
                {
                    shortToActiveMulti = 1.7f;
                }
                if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.ActiveAimTransformTargetPosition)
                {
                    hasResetShortStock = true;
                }

                currentRotation = Quaternion.Lerp(currentRotation, activeAimTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ActiveAimRotationSpeedMulti.Value);
                if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.ActiveAimTransformTargetPosition)
                {
                    currentRotation = Quaternion.Lerp(currentRotation, activeAimMiniTargetQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ActiveAimAdditionalRotationSpeedMulti.Value);
                }
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = Plugin.ThirdActiveAimSpeedMulti.Value * aimMulti;
                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * activeTargetPostionThird;
                }

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.ActiveAimTransformTargetPosition, aimMulti * dt * Plugin.ActiveAimSpeedMulti.Value * shortToActiveMulti);

            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetActiveAim && !StanceController.IsLowReady && !StanceController.IsHighReady && !StanceController.IsShortStock && !isResettingLowReady && !isResettingHighReady && !isResettingShortStock)
            {
                __instance.CameraSmoothTime = 4f;

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingActiveAim = true;

                currentRotation = Quaternion.Lerp(currentRotation, activeAimRevertQuaternion, __instance.CameraSmoothTime * aimMulti * dt * Plugin.ActiveAimResetRotationSpeedMulti.Value);
                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, aimMulti * dt * Plugin.ActiveAimResetSpeedMulti.Value);

                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && hasResetActiveAim == false)
            {
                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = 0.1f;
                }

                __instance.CameraSmoothTime = 8f;

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                isResettingActiveAim = false;
                hasResetActiveAim = true;
            }

            if (!StanceController.IsActiveAiming && !StanceController.IsShortStock)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireAccuracy;
            }
        }
    }


    public class OnWeaponDrawPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SkillsClass).GetMethod("OnWeaponDraw", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(SkillsClass __instance, Item item)
        {
            if (item?.Owner?.ID != null && (item.Owner.ID.StartsWith("pmc") || item.Owner.ID.StartsWith("scav")))
            {
                Plugin.DidWeaponSwap = true;
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
            Light light = (Light)AccessTools.Field(typeof(LaserBeam), "light_0").GetValue(__instance);
            light.intensity *= 0.1f;


            if ((StanceController.IsHighReady == true || StanceController.IsLowReady == true) && !Plugin.IsAiming)
            {
                Vector3 playerPos = Singleton<GameWorld>.Instance.AllPlayers[0].Transform.position;
                Vector3 lightPos = __instance.gameObject.transform.position;
                float distanceFromPlayer = Vector3.Distance(lightPos, playerPos);
                if (distanceFromPlayer <= 1.8f)
                {
                    return false;
                }
                else return true;
            }
            else
            {
                return true;
            }
        }
    }

    public class WeaponLengthPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("method_7", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            float length = (float)AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                WeaponProperties.BaseWeaponLength = length;
                WeaponProperties.NewWeaponLength = length >= 0.9f ? length * 1.15f : length;
            }
        }
    }

    public class WeaponOverlappingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("WeaponOverlapping", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);

            if (player.IsYourPlayer == true)
            {

                if ((StanceController.IsHighReady == true || StanceController.IsLowReady == true || StanceController.IsShortStock == true))
                {
                    AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.8f);
                    return;
                }
                if (StanceController.WasShortStock == true && Plugin.IsAiming)
                {
                    AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.7f);
                    return;
                }
                if (__instance.Item.WeapClass == "pistol")
                {
                    if (StanceController.PistolIsCompressed == true)
                    {
                        AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.7f);
                    }
                    else 
                    {
                        AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.9f);
                    }
                    return;
                }
                AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength);
                return;
            }
        }
    }

    public class WeaponOverlapViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("WeaponOverlapView", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            float float_0 = (float)AccessTools.Field(typeof(EFT.Player.FirearmController), "float_0").GetValue(__instance);

            if (float_0 > EFTHardSettings.Instance.STOP_AIMING_AT && __instance.IsAiming)
            {
                Plugin.IsAiming = true;
                return;
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

    public class ApplySimpleRotationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ApplySimpleRotation", BindingFlags.Instance | BindingFlags.Public);
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

                    if (firearmController.Item.WeapClass == "pistol" && Plugin.EnableAltPistol.Value == true)
                    {
                        StanceController.DoPistolStances(true, ref __instance, ref currentRotation, dt, ref hasResetPistolPos);
                    }
                    else
                    {
                        StanceController.DoRifleStances(Logger, true, ref __instance, ref currentRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim);
                    }

                }
                else if (player.IsAI == true)
                {
                    Quaternion currentRotation = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").GetValue(__instance);

                    Vector3 lowReadyTargetRotation = new Vector3(135.0f, 50.0f, -35.0f);
                    Quaternion lowReadyTargetQuaternion = Quaternion.Euler(lowReadyTargetRotation);
                    Vector3 lowReadyTargetPostion = new Vector3(0.04f, -0.05f, 0.0f);

                    Vector3 highReadyTargetRotation = new Vector3(-75.0f, 0.0f, 15.0f);
                    Quaternion highReadyTargetQuaternion = Quaternion.Euler(highReadyTargetRotation);
                    Vector3 highReadyTargetPostion = new Vector3(0.05f, 0.04f, -0.1f);

                    Vector3 peacefulPistolTargetRotation = new Vector3(15.0f, -15.0f, 15.0f);
                    Quaternion peacefulPistolTargetQuaternion = Quaternion.Euler(peacefulPistolTargetRotation);
                    Vector3 peacefulPistolTargetPosition = new Vector3(-0.1f, 0.15f, -0.12f);

                    Vector3 tacPistolTargetRotation = new Vector3(-2.5f, -20.0f, 0.0f);
                    Quaternion tacPistolTargetQuaternion = Quaternion.Euler(peacefulPistolTargetRotation);
                    Vector3 tacPistolTargetPosition = new Vector3(-0.05f, 0.15f, -0.15f);

                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, 1f);
                    float pitch = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                    float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);


                    if (firearmController.Item.WeapClass != "pistol")
                    {
                        ////low ready//// 
                        if ((player.AIData.BotOwner.Memory.IsPeace == true || !StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString()) && !player.IsSprintEnabled && !__instance.IsAiming && !player.AIData.BotOwner.ShootData.Shooting && (Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd) > 15f)) //&& player.AIData.BotOwner.Memory.IsPeace == true && && (Time.time - player.AIData.BotOwner.Memory.LastEnemyTimeSeen) > 1f
                        {
                            currentRotation = Quaternion.Lerp(currentRotation, lowReadyTargetQuaternion, __instance.CameraSmoothTime * dt * Plugin.LowReadyRotationMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * lowReadyTargetPostion;
                        }


                        ////high ready////
                        if (!player.AIData.BotOwner.Memory.IsPeace && StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString()) && !__instance.IsAiming && !player.AIData.BotOwner.ShootData.Shooting && (Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd) > 15f)//&& player.AIData.BotOwner.Memory.IsPeace == true && && (Time.time - player.AIData.BotOwner.Memory.LastEnemyTimeSeen) > 1f
                        {
                            player.BodyAnimatorCommon.SetFloat(GClass1645.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2);

                            currentRotation = Quaternion.Lerp(currentRotation, highReadyTargetQuaternion, __instance.CameraSmoothTime * dt * Plugin.HighReadyRotationMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                            __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * highReadyTargetPostion;
                        }
                        else
                        {
                            player.BodyAnimatorCommon.SetFloat(GClass1645.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)firearmController.Item.CalculateCellSize().X);
                        }

                        ////peaceful positon//// (player.AIData.BotOwner.Memory.IsPeace == true && !StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString()) && !player.IsSprintEnabled && !__instance.IsAiming && !player.AIData.BotOwner.ShootData.Shooting && (Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd) > 20f)
                    }
                    else
                    {
                        if ((player.AIData.BotOwner.Memory.IsPeace == true || !StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString())) && !player.IsSprintEnabled && !__instance.IsAiming && !player.AIData.BotOwner.ShootData.Shooting && (Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd) > 15f)
                        {
                            currentRotation = Quaternion.Lerp(currentRotation, peacefulPistolTargetQuaternion, __instance.CameraSmoothTime * dt * Plugin.PistolRotationSpeedMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * peacefulPistolTargetPosition;
                        }

                        if (StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString()) && !player.IsSprintEnabled && !__instance.IsAiming && !player.AIData.BotOwner.ShootData.Shooting && (Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd) > 15f)
                        {
                            currentRotation = Quaternion.Lerp(currentRotation, tacPistolTargetQuaternion, __instance.CameraSmoothTime * dt * Plugin.PistolRotationSpeedMulti.Value);
                            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                            __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * tacPistolTargetPosition;
                        }
                    }
                }
            }
            return true;
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
                        StanceController.DoPistolStances(false, ref __instance, ref currentRotation, dt, ref hasResetPistolPos);

                    }
                    else
                    {
                        StanceController.DoRifleStances(Logger, false, ref __instance, ref currentRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim);
                    }
                }
            }
            return true;
        }
    }

    public class UpdateHipInaccuracyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("UpdateHipInaccuracy", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                WeaponProperties.BaseHipfireAccuracy = player.ProceduralWeaponAnimation.Breath.HipPenalty;
            }
        }
    }
}
