﻿using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using CustomPlayerLoopSystem;
using EFT;
using EFT.Animations;
using EFT.InputSystem;
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
using static EFT.Weather.WeatherDebug;
using static ShotEffector;
using static System.Net.Mime.MediaTypeNames;
using LightStruct = GStruct154;
using GlobalValues = GClass1710;
using PlayerInterface = GInterface114;

namespace RealismMod
{
    public static class StanceController
    {
        public static string[] botsToUseTacticalStances = { "sptBear", "sptUsec", "exUsec", "pmcBot", "bossKnight", "followerBigPipe", "followerBirdEye", "bossGluhar", "followerGluharAssault", "followerGluharScout", "followerGluharSecurity", "followerGluharSnipe" };

        private static float clickDelay = 0.2f;
        private static float doubleClickTime = 0f;
        private static bool clickTriggered = true;
        public static int SelectedStance = 0;

        public static bool IsActiveAiming = false;
        public static bool PistolIsCompressed = false;
        public static bool IsHighReady = false;
        public static bool IsLowReady = false;
        public static bool IsShortStock = false;
        public static bool WasHighReady = false;
        public static bool WasLowReady = false;
        public static bool WasShortStock = false;
        public static bool WasActiveAim = false;

        public static bool IsFiringFromStance = false;
        public static float StanceShotTime = 0.0f;
        public static float ManipTime = 0.0f;

        public static float HighReadyBlackedArmTime = 0.0f;
        public static bool DoHighReadyInjuredAnim = false;

        public static bool HaveSetAiming = false;
        public static bool SetActiveAiming = false;

        public static float HighReadyManipBuff = 1f;
        public static float ActiveAimManipBuff = 1f;
        public static float LowReadyManipBuff = 1f;

        public static bool CancelPistolStance = false;
        public static bool PistolIsColliding = false;
        public static bool CancelHighReady = false;
        public static bool CancelLowReady = false;
        public static bool CancelShortStock = false;
        public static bool CancelActiveAim = false;
        public static bool DoResetStances = false;

        private static bool setRunAnim = false;
        private static bool resetRunAnim = false;

        private static bool gotCurrentStam = false;
        private static float currentStam = 100f;

        public static Vector3 StanceTargetPosition = Vector3.zero;

        public static bool HasResetActiveAim = true;
        public static bool HasResetLowReady = true;
        public static bool HasResetHighReady = true;
        public static bool HasResetShortStock = true;
        public static bool HasResetPistolPos = true;

        public static Vector3 CoverWiggleDirection = Vector3.zero;
        public static Vector3 CoverDirection = Vector3.zero;

        public static float BracingSwayBonus = 1f;
        public static float BracingRecoilBonus = 1f;
        public static bool IsBracingTop = false;
        public static bool IsBracingLeftSide = false;
        public static bool IsBracingRightSide = false;
        public static bool IsBracingSide = false;
        public static float MountingSwayBonus = 1f;
        public static float MountingRecoilBonus = 1f;
        public static bool WeaponIsBracing = false;
        public static bool WeaponIsMounting = false;
        public static float DismountTimer = 0.0f;
        public static bool CanDoDismountTimer = false;

        public static Dictionary<string, bool> LightDictionary = new Dictionary<string, bool>();

        public static bool toggledLight = false;

        public static bool IsInStance = false;

        public static void SetStanceStamina(Player player, Player.FirearmController fc)
        {
            if (!PlayerProperties.IsSprinting && (WeaponIsMounting || (!WeaponIsBracing && Plugin.EnableIdleStamDrain.Value)))
            {
                gotCurrentStam = false;

                if (fc.Item.WeapClass != "pistol")
                {
                    if (Plugin.IsAiming || (Plugin.EnableIdleStamDrain.Value && !IsActiveAiming && !WeaponIsMounting && !WeaponIsBracing && !player.IsInPronePose && (!IsHighReady && !IsLowReady && !IsShortStock && !Plugin.IsFiring)))
                    {
                        player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponProperties.ErgoFactor * 0.8f * ((1f - PlayerProperties.ADSInjuryMulti) + 1f));
                    }
                    else if (IsActiveAiming)
                    {
                        player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponProperties.ErgoFactor * 0.2f * ((1f - PlayerProperties.ADSInjuryMulti) + 1f));
                    }
                    else if (!Plugin.EnableIdleStamDrain.Value)
                    {
                        player.Physical.Aim(0);
                    }

                    if (IsHighReady && !Plugin.IsFiring && !IsLowReady && !Plugin.IsAiming && !IsShortStock)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + ((((1f - (WeaponProperties.ErgoFactor / 100f)) * 0.01f) * PlayerProperties.ADSInjuryMulti)), player.Physical.HandsStamina.TotalCapacity);
                    }
                    if (WeaponIsMounting || (IsLowReady && !Plugin.IsFiring && !IsHighReady && !Plugin.IsAiming && !IsShortStock))
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (((1f - (WeaponProperties.ErgoFactor / 100f)) * 0.03f) * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
                    }
                    if (IsShortStock && !Plugin.IsFiring && !IsHighReady && !Plugin.IsAiming && !IsLowReady)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (((1f - (WeaponProperties.ErgoFactor / 100f)) * 0.01f) * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
                    }
                }
                else
                {
                    if (!Plugin.IsAiming)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (((1f - (WeaponProperties.ErgoFactor / 100f)) * 0.025f) * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
                    }
                }
            }
            else
            {
                if (!gotCurrentStam)
                {
                    currentStam = player.Physical.HandsStamina.Current;
                    gotCurrentStam = true;
                }

                player.Physical.Aim(0f);
                player.Physical.HandsStamina.Current = currentStam;
            }

            if (player.IsInventoryOpened || (player.IsInPronePose && !Plugin.IsAiming))
            {
                player.Physical.Aim(0f);
                player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (0.04f * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
            }
        }

        public static void ResetStanceStamina(Player player)
        {
            player.Physical.Aim(0f);
            player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (0.04f * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
        }

        public static bool IsIdle()
        {
            return !IsActiveAiming && !IsHighReady && !IsLowReady && !IsShortStock && !WasHighReady && !WasLowReady && !WasShortStock && !WasActiveAim && HasResetActiveAim && HasResetHighReady && HasResetLowReady && HasResetShortStock && HasResetPistolPos ? true : false;
        }


        public static void CancelAllStances()
        {
            IsActiveAiming = false;
            WasActiveAim = false;
            IsHighReady = false;
            WasHighReady = false;
            IsLowReady = false;
            WasLowReady = false;
            IsShortStock = false;
            WasShortStock = false;
            WeaponIsMounting = false;
        }


        public static void StanceManipCancelTimer()
        {
            ManipTime += Time.deltaTime;

            if (ManipTime >= 0.25f)
            {
                CancelHighReady = false;
                CancelLowReady = false;
                CancelShortStock = false;
                CancelPistolStance = false;
                CancelActiveAim = false;
                DoResetStances = false;
                ManipTime = 0f;
            }
        }


        public static void StanceShotTimer()
        {
            StanceShotTime += Time.deltaTime;

            if (StanceShotTime >= 0.55f)
            {
                IsFiringFromStance = false;
                StanceShotTime = 0f;
            }
        }

        private static void showMountingUI() 
        {
            if ((int)Time.time % 4 == 0)
            {
                if (StanceController.WeaponIsBracing && !StanceController.WeaponIsMounting)
                {
                    AmmoCountPanel panelUI = (AmmoCountPanel)AccessTools.Field(typeof(BattleUIScreen), "_ammoCountPanel").GetValue(Singleton<GameUI>.Instance.BattleUiScreen);
                    panelUI.Show("Bracing");
                }
                else if (StanceController.WeaponIsMounting)
                {
                    AmmoCountPanel panelUI = (AmmoCountPanel)AccessTools.Field(typeof(BattleUIScreen), "_ammoCountPanel").GetValue(Singleton<GameUI>.Instance.BattleUiScreen);
                    panelUI.Show("Mounting");
                }
            }
        }

        public static void StanceState()
        {
            if (Utils.WeaponReady == true)
            {

                showMountingUI();

                if (!PlayerProperties.IsSprinting && !Plugin.IsInInventory && WeaponProperties._WeapClass != "pistol")
                {
                    //cycle stances
                    if (Input.GetKeyUp(Plugin.CycleStancesKeybind.Value.MainKey))
                    {
                        if (Time.time <= doubleClickTime)
                        {
                            Plugin.StanceBlender.Target = 0f;
                            clickTriggered = true;
                            SelectedStance = 0;
                            IsHighReady = false;
                            IsLowReady = false;
                            IsShortStock = false;
                            IsActiveAiming = false;
                            WasActiveAim = false;
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
                    else if (!clickTriggered)
                    {
                        if (Time.time > doubleClickTime)
                        {
                            Plugin.StanceBlender.Target = 1f;
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

                            if (IsHighReady == true && (PlayerProperties.RightArmRuined == true || PlayerProperties.LeftArmRuined == true))
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
                            Plugin.StanceBlender.Target = 1f;
                            IsActiveAiming = true;
                            IsShortStock = false;
                            IsHighReady = false;
                            IsLowReady = false;
                            WasActiveAim = IsActiveAiming;
                            SetActiveAiming = true;
                        }
                        else if (SetActiveAiming == true)
                        {
                            Plugin.StanceBlender.Target = 0f;
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
                            Plugin.StanceBlender.Target = Plugin.StanceBlender.Target == 0f ? 1f : 0f;
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
                        Plugin.StanceBlender.Target = Plugin.StanceBlender.Target == 0f ? 1f : 0f;

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
                        Plugin.StanceBlender.Target = Plugin.StanceBlender.Target == 0f ? 1f : 0f;

                        IsHighReady = !IsHighReady;
                        IsShortStock = false;
                        IsLowReady = false;
                        IsActiveAiming = false;
                        WasActiveAim = IsActiveAiming;
                        WasHighReady = IsHighReady;
                        WasLowReady = IsLowReady;
                        WasShortStock = IsShortStock;

                        if (IsHighReady == true && (PlayerProperties.RightArmRuined == true || PlayerProperties.LeftArmRuined == true))
                        {
                            DoHighReadyInjuredAnim = true;
                        }
                    }

                    //low ready
                    if (Input.GetKeyDown(Plugin.LowReadyKeybind.Value.MainKey))
                    {
                        Plugin.StanceBlender.Target = Plugin.StanceBlender.Target == 0f ? 1f : 0f;

                        IsLowReady = !IsLowReady;
                        IsHighReady = false;
                        IsActiveAiming = false;
                        IsShortStock = false;
                        WasActiveAim = IsActiveAiming;
                        WasHighReady = IsHighReady;
                        WasLowReady = IsLowReady;
                        WasShortStock = IsShortStock;
                    }

                    if (Plugin.IsAiming)
                    {
                        if (IsActiveAiming || WasActiveAim)
                        {
                            WasHighReady = false;
                            WasLowReady = false;
                            WasShortStock = false;
                        }
                        IsLowReady = false;
                        IsHighReady = false;
                        IsShortStock = false;
                        IsActiveAiming = false;
                        HaveSetAiming = true;
                    }
                    else if (HaveSetAiming)
                    {
                        IsLowReady = WasLowReady;
                        IsHighReady = WasHighReady;
                        IsShortStock = WasShortStock;
                        IsActiveAiming = WasActiveAim;
                        HaveSetAiming = false;
                    }

                    if (DoHighReadyInjuredAnim)
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

                    if ((PlayerProperties.LeftArmRuined || PlayerProperties.RightArmRuined) && !Plugin.IsAiming && !IsShortStock && !IsActiveAiming && !IsHighReady)
                    {
                        Plugin.StanceBlender.Target = 1f;
                        IsLowReady = true;
                        WasLowReady = true;
                    }
                }

                HighReadyManipBuff = IsHighReady? 1.2f : 1f;
                ActiveAimManipBuff = IsActiveAiming && Plugin.ActiveAimReload.Value ? 1.25f : 1f;
                LowReadyManipBuff = IsLowReady ? 1.2f : 1f;

                if (DoResetStances)
                {
                    StanceManipCancelTimer();
                }

                if (Plugin.DidWeaponSwap || WeaponProperties._WeapClass == "pistol")
                {
                    if(Plugin.DidWeaponSwap)
                    {
                        StanceController.PistolIsCompressed = false;
                        StanceController.StanceTargetPosition = Vector3.zero;
                        Plugin.StanceBlender.Target = 0f;
                    }

                    SelectedStance = 0;
                    IsShortStock = false;
                    IsLowReady = false;
                    IsHighReady = false;
                    IsActiveAiming = false;
                    WasHighReady = false;
                    WasLowReady = false;
                    WasShortStock = false;
                    WasActiveAim = false;
                    Plugin.DidWeaponSwap = false;
                }
            }

        }

        //doesn't work with multiple lights where one is off and the other is on
        public static void ToggleDevice(FirearmController fc, bool activating)
        {
            foreach (Mod mod in fc.Item.Mods)
            {
                LightComponent light;
                if (mod.TryGetItemComponent<LightComponent>(out light))
                {
                    if (!LightDictionary.ContainsKey(mod.Id))
                    {
                        LightDictionary.Add(mod.Id, light.IsActive);
                    }

                    bool isOn = light.IsActive;
                    bool state = false;

                    if (!activating && isOn)
                    {
                        state = false;
                        LightDictionary[mod.Id] = true;
                    }
                    if (!activating && !isOn)
                    {
                        LightDictionary[mod.Id] = false;
                        return;
                    }
                    if (activating && isOn)
                    {
                        return;
                    }
                    if (activating && !isOn && LightDictionary[mod.Id])
                    {
                        state = true;
                    }
                    else if (activating && !isOn)
                    {
                        return;
                    }

                    fc.SetLightsState(new LightStruct[]
                    {
                        new LightStruct
                        {
                            Id = light.Item.Id,
                            IsActive = state,
                            LightMode = light.SelectedMode
                        }
                    }, false);
                }
            }
        }

        //move this to the patch classes
        public static float currentX = 0f;

        public static void DoPistolStances(bool isThirdPerson, ref EFT.Animations.ProceduralWeaponAnimation __instance, ref Quaternion stanceRotation, float dt, ref bool hasResetPistolPos, Player player, ManualLogSource logger, ref float rotationSpeed, ref bool isResettingPistol, FirearmController fc)
        {
            float totalPlayerWeight = PlayerProperties.TotalModifiedWeightMinusWeapon;
            float playerWeightFactor = 1f + (totalPlayerWeight / 100f);
            float ergoMulti = Mathf.Clamp(WeaponProperties.ErgoStanceSpeed, 0.65f, 1.45f);
            float stanceMulti = Mathf.Clamp(ergoMulti * PlayerProperties.StanceInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.65f)), 0.5f, 1.45f);
            float invInjuryMulti = (1f - PlayerProperties.StanceInjuryMulti) + 1f;
            float resetErgoMulti = (1f - stanceMulti) + 1f;
            float ergoDelta = (1f - WeaponProperties.ErgoDelta);
            float intensity = Mathf.Max(1f * (1f - PlayerProperties.WeaponSkillErgo) * resetErgoMulti * invInjuryMulti * ergoDelta * playerWeightFactor, 0.35f);
            float balanceFactor = 1f + (WeaponProperties.Balance / 100f);
            balanceFactor = WeaponProperties.Balance > 0f ? balanceFactor * -1f : balanceFactor;

            Vector3 pistolTargetPosition = new Vector3(Plugin.PistolOffsetX.Value, Plugin.PistolOffsetY.Value, Plugin.PistolOffsetZ.Value);
            Vector3 pistolTargetRotation = new Vector3(Plugin.PistolRotationX.Value, Plugin.PistolRotationY.Value, Plugin.PistolRotationZ.Value);
            Quaternion pistolTargetQuaternion = Quaternion.Euler(pistolTargetRotation);
            Quaternion pistolMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.PistolAdditionalRotationX.Value, Plugin.PistolAdditionalRotationY.Value, Plugin.PistolAdditionalRotationZ.Value));
            Quaternion pistolRevertQuaternion = Quaternion.Euler(Plugin.PistolResetRotationX.Value * balanceFactor, Plugin.PistolResetRotationY.Value, Plugin.PistolResetRotationZ.Value);

            //I've no idea wtf is going on here but it sort of works
            float targetPos = 0.09f;
            if (!Plugin.IsBlindFiring && !StanceController.CancelPistolStance)
            {
                targetPos = Plugin.PistolOffsetX.Value;
            }
             currentX = Mathf.Lerp(currentX, targetPos, dt * Plugin.PistolPosSpeedMulti.Value * stanceMulti * 0.5f);

            __instance.HandsContainer.WeaponRoot.localPosition = new Vector3(currentX, __instance.HandsContainer.TrackingTransform.localPosition.y, __instance.HandsContainer.TrackingTransform.localPosition.z);

            if (!__instance.IsAiming && !StanceController.CancelPistolStance && !Plugin.IsBlindFiring && !StanceController.PistolIsColliding)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireInaccuracy * PlayerProperties. SprintHipfirePenalty;

                StanceController.PistolIsCompressed = true;
                isResettingPistol = false;
                hasResetPistolPos = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                Plugin.StanceBlender.Speed = Plugin.PistolPosSpeedMulti.Value * stanceMulti;
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, pistolTargetPosition, Plugin.StanceTransitionSpeed.Value * stanceMulti * dt);

                rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolRotationSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = pistolTargetQuaternion;
                if (Plugin.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolAdditionalRotationSpeedMulti.Value * stanceMulti;
                    stanceRotation = pistolMiniTargetQuaternion;
                }
            }
            else if (Plugin.StanceBlender.Value > 0f && !hasResetPistolPos && !StanceController.PistolIsColliding)
            {
                isResettingPistol = true;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolResetRotationSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = pistolRevertQuaternion;
                Plugin.StanceBlender.Speed = Plugin.PistolPosResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
            }
            else if (Plugin.StanceBlender.Value == 0f && !hasResetPistolPos && !StanceController.PistolIsColliding)
            {
                isResettingPistol = false;

                StanceController.PistolIsCompressed = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                stanceRotation = Quaternion.identity;

                hasResetPistolPos = true;
            }
        }

        public static void DoRifleStances(ManualLogSource logger, Player player, Player.FirearmController fc, bool isThirdPerson, ref EFT.Animations.ProceduralWeaponAnimation __instance, ref Quaternion stanceRotation, float dt, ref bool isResettingShortStock, ref bool hasResetShortStock, ref bool hasResetLowReady, ref bool hasResetActiveAim, ref bool hasResetHighReady, ref bool isResettingHighReady, ref bool isResettingLowReady, ref bool isResettingActiveAim, ref float rotationSpeed)
        {
            float totalPlayerWeight = PlayerProperties.TotalModifiedWeightMinusWeapon;
            float playerWeightFactor = 1f + (totalPlayerWeight / 150f);
            float ergoMulti = Mathf.Clamp(WeaponProperties.ErgoStanceSpeed, 0.55f, 1f);
            float stanceMulti = Mathf.Clamp(ergoMulti * PlayerProperties.StanceInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.65f)), 0.5f, 1f);
            float invInjuryMulti = (1f - PlayerProperties.StanceInjuryMulti) + 1f;
            float resetErgoMulti = (1f - stanceMulti) + 1f;
            float stocklessModifier = WeaponProperties.HasShoulderContact ? 1f : 2.4f;
            float ergoDelta = (1f - WeaponProperties.ErgoDelta);
            float intensity = Mathf.Max(1.2f * (1f - (PlayerProperties.AimSkillADSBuff * 0.5f)) * resetErgoMulti * invInjuryMulti * stocklessModifier * ergoDelta * playerWeightFactor, 0.5f);

    
            float pitch = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);

            bool isColliding = !__instance.OverlappingAllowsBlindfire;
            float collisionRotationFactor = isColliding ? 2f : 1f;
            float collisionPositionFactor = isColliding ? 2f : 1f;

            Vector3 activeAimTargetRotation = new Vector3(Plugin.ActiveAimRotationX.Value, Plugin.ActiveAimRotationY.Value, Plugin.ActiveAimRotationZ.Value);
            Vector3 activeAimRevertRotation = new Vector3(Plugin.ActiveAimResetRotationX.Value * resetErgoMulti, Plugin.ActiveAimResetRotationY.Value * resetErgoMulti, Plugin.ActiveAimResetRotationZ.Value * resetErgoMulti);
            Quaternion activeAimTargetQuaternion = Quaternion.Euler(activeAimTargetRotation);
            Quaternion activeAimMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimAdditionalRotationX.Value * resetErgoMulti, Plugin.ActiveAimAdditionalRotationY.Value * resetErgoMulti, Plugin.ActiveAimAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion activeAimRevertQuaternion = Quaternion.Euler(activeAimRevertRotation);
            Vector3 activeAimTargetPosition = new Vector3(Plugin.ActiveAimOffsetX.Value, Plugin.ActiveAimOffsetY.Value, Plugin.ActiveAimOffsetZ.Value);

            Vector3 lowReadyTargetRotation = new Vector3(Plugin.LowReadyRotationX.Value * collisionRotationFactor * resetErgoMulti, Plugin.LowReadyRotationY.Value, Plugin.LowReadyRotationZ.Value);
            Quaternion lowReadyTargetQuaternion = Quaternion.Euler(lowReadyTargetRotation);
            Quaternion lowReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.LowReadyAdditionalRotationX.Value * resetErgoMulti, Plugin.LowReadyAdditionalRotationY.Value * resetErgoMulti, Plugin.LowReadyAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion lowReadyRevertQuaternion = Quaternion.Euler(Plugin.LowReadyResetRotationX.Value * resetErgoMulti, Plugin.LowReadyResetRotationY.Value * resetErgoMulti, Plugin.LowReadyResetRotationZ.Value * resetErgoMulti);
            Vector3 lowReadyTargetPosition = new Vector3(Plugin.LowReadyOffsetX.Value, Plugin.LowReadyOffsetY.Value, Plugin.LowReadyOffsetZ.Value);

            Vector3 highReadyTargetRotation = new Vector3(Plugin.HighReadyRotationX.Value * stanceMulti * collisionRotationFactor, Plugin.HighReadyRotationY.Value * stanceMulti, Plugin.HighReadyRotationZ.Value * stanceMulti);
            Quaternion highReadyTargetQuaternion = Quaternion.Euler(highReadyTargetRotation);
            Quaternion highReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.HighReadyAdditionalRotationX.Value * resetErgoMulti, Plugin.HighReadyAdditionalRotationY.Value * resetErgoMulti, Plugin.HighReadyAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion highReadyRevertQuaternion = Quaternion.Euler(Plugin.HighReadyResetRotationX.Value * resetErgoMulti, Plugin.HighReadyResetRotationY.Value * resetErgoMulti, Plugin.HighReadyResetRotationZ.Value * resetErgoMulti);
            Vector3 highReadyTargetPosition = new Vector3(Plugin.HighReadyOffsetX.Value, Plugin.HighReadyOffsetY.Value, Plugin.HighReadyOffsetZ.Value);

            Vector3 shortStockTargetRotation = new Vector3(Plugin.ShortStockRotationX.Value * stanceMulti, Plugin.ShortStockRotationY.Value * stanceMulti, Plugin.ShortStockRotationZ.Value * stanceMulti);
            Quaternion shortStockTargetQuaternion = Quaternion.Euler(shortStockTargetRotation);
            Quaternion shortStockMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ShortStockAdditionalRotationX.Value * resetErgoMulti, Plugin.ShortStockAdditionalRotationY.Value * resetErgoMulti, Plugin.ShortStockAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion shortStockRevertQuaternion = Quaternion.Euler(Plugin.ShortStockResetRotationX.Value * resetErgoMulti, Plugin.ShortStockResetRotationY.Value * resetErgoMulti, Plugin.ShortStockResetRotationZ.Value * resetErgoMulti);
            Vector3 shortStockTargetPosition = new Vector3(Plugin.ShortStockOffsetX.Value, Plugin.ShortStockOffsetY.Value, Plugin.ShortStockOffsetZ.Value);

            //for setting baseline position

            if (!Plugin.IsBlindFiring) 
            {
                __instance.HandsContainer.WeaponRoot.localPosition = Plugin.WeaponOffsetPosition;
            }

            if (Plugin.EnableTacSprint.Value && (StanceController.IsHighReady || StanceController.WasHighReady) && !PlayerProperties.RightArmRuined)
            {
                player.BodyAnimatorCommon.SetFloat(GClass1710.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2f);
                if (!setRunAnim)
                {
                    setRunAnim = true;
                    resetRunAnim = false;
                }
            }
            else if (Plugin.EnableTacSprint.Value)
            {
                if (!resetRunAnim)
                {
                    player.BodyAnimatorCommon.SetFloat(GClass1710.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)fc.Item.CalculateCellSize().X);
                    resetRunAnim = true;
                    setRunAnim = false;
                }
            }

            if (!StanceController.IsActiveAiming && !StanceController.IsShortStock)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireInaccuracy * 1.1f * PlayerProperties.SprintHipfirePenalty;
            }
            if (StanceController.IsActiveAiming)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireInaccuracy * 0.8f * PlayerProperties.SprintHipfirePenalty;
            }

            if (Plugin.StanceToggleDevice.Value)
            {
                if (!toggledLight && (IsHighReady || IsLowReady))
                {
                    ToggleDevice(fc, false);
                    toggledLight = true;
                }
                if (toggledLight && !IsHighReady && !IsLowReady)
                {
                    ToggleDevice(fc, true);
                    toggledLight = false;
                }
            }

            ////short-stock////
            if (StanceController.IsShortStock && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsLowReady && !__instance.IsAiming && !StanceController.CancelShortStock && !Plugin.IsBlindFiring && !PlayerProperties.IsSprinting)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireInaccuracy * 1.5f * PlayerProperties.SprintHipfirePenalty;

                float activeToShort = 1f;
                float highToShort = 1f;
                float lowToShort = 1f;
                isResettingShortStock = false;
                hasResetShortStock = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                if (StanceController.StanceTargetPosition != shortStockTargetPosition)
                {
                    if (!hasResetActiveAim)
                    {
                        activeToShort = 1.75f;
                    }
                    if (!hasResetHighReady)
                    {
                        highToShort = 1.15f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToShort = 1.15f;
                    }
                }
                if (StanceController.StanceTargetPosition == shortStockTargetPosition)
                {
                    hasResetActiveAim = true;
                    hasResetHighReady = true;
                    hasResetLowReady = true;
                }

                float transitionSpeedFactor = activeToShort * highToShort * lowToShort;

                rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                stanceRotation = shortStockTargetQuaternion;

                if (Plugin.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                    stanceRotation = shortStockMiniTargetQuaternion;
                }

                Plugin.StanceBlender.Speed = Plugin.ShortStockSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, shortStockTargetPosition, Plugin.StanceTransitionSpeed.Value * stanceMulti * dt);

            }
            else if (Plugin.StanceBlender.Value > 0f && !hasResetShortStock && !StanceController.IsLowReady && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !isResettingActiveAim && !isResettingHighReady && !isResettingLowReady)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingShortStock = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockResetRotationSpeedMulti.Value;
                stanceRotation = shortStockRevertQuaternion;
                Plugin.StanceBlender.Speed = Plugin.ShortStockResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);

            }
            else if (Plugin.StanceBlender.Value == 0f && !hasResetShortStock)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                stanceRotation = Quaternion.identity;

                isResettingShortStock = false;
                hasResetShortStock = true;
            }

            ////high ready////
            if (StanceController.IsHighReady && !StanceController.IsActiveAiming && !StanceController.IsLowReady && !StanceController.IsShortStock && !__instance.IsAiming && !StanceController.IsFiringFromStance && !StanceController.CancelHighReady && !Plugin.IsBlindFiring)
            {
                float shortToHighMulti = 1.0f;
                float lowToHighMulti = 1.0f;
                float activeToHighMulti = 1.0f;
                isResettingHighReady = false;
                hasResetHighReady = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                if (StanceController.StanceTargetPosition != highReadyTargetPosition)
                {
                    if (!hasResetShortStock)
                    {
                        shortToHighMulti = 1.15f;
                    }
                    if (!hasResetActiveAim)
                    {
                        activeToHighMulti = 1.85f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToHighMulti = 1.25f;
                    }
                }
                if (StanceController.StanceTargetPosition == highReadyTargetPosition)
                {
                    hasResetActiveAim = true;
                    hasResetLowReady = true;
                    hasResetShortStock = true;
                }

                float transitionSpeedFactor = shortToHighMulti * lowToHighMulti * activeToHighMulti;

                if (StanceController.DoHighReadyInjuredAnim)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                    stanceRotation = highReadyMiniTargetQuaternion;
                    if (Plugin.StanceBlender.Value < 1f)
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                        stanceRotation = lowReadyTargetQuaternion;
                    }
                }
                else
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                    stanceRotation = highReadyTargetQuaternion;
                    if (Plugin.StanceBlender.Value < 1f)
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                        stanceRotation = highReadyMiniTargetQuaternion;
                    }
                }

                Plugin.StanceBlender.Speed = Plugin.HighReadySpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition , highReadyTargetPosition, Plugin.StanceTransitionSpeed.Value * stanceMulti * dt);   
            
            }
            else if (Plugin.StanceBlender.Value > 0f && !hasResetHighReady && !StanceController.IsLowReady && !StanceController.IsActiveAiming && !StanceController.IsShortStock && !isResettingActiveAim && !isResettingLowReady && !isResettingShortStock)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = -intensity;
                }

                isResettingHighReady = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyResetRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = highReadyRevertQuaternion;

                Plugin.StanceBlender.Speed = Plugin.HighReadyResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
            }
            else if (Plugin.StanceBlender.Value == 0f && !hasResetHighReady)
            {

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                stanceRotation = Quaternion.identity;

                isResettingHighReady = false;
                hasResetHighReady = true;
            }

            ////low ready////
            if (StanceController.IsLowReady && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsShortStock && !__instance.IsAiming && !StanceController.IsFiringFromStance && !StanceController.CancelLowReady && !Plugin.IsBlindFiring)
            {
                float highToLow = 1.0f;
                float shortToLow = 1.0f;
                float activeToLow = 1.0f;
                isResettingLowReady = false;
                hasResetLowReady = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                if (StanceController.StanceTargetPosition != lowReadyTargetPosition)
                {
                    if (!hasResetHighReady)
                    {
                        highToLow = 1.25f;
                    }
                    if (!hasResetShortStock)
                    {
                        shortToLow = 1.15f;
                    }
                    if (!hasResetActiveAim)
                    {
                        activeToLow = 1.85f;
                    }

                }
                if (StanceController.StanceTargetPosition == lowReadyTargetPosition)
                {
                    hasResetHighReady = true;
                    hasResetShortStock = true;
                    hasResetActiveAim = true;
                }

                float transitionSpeedFactor = highToLow * shortToLow * activeToLow;

                rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                stanceRotation = lowReadyTargetQuaternion;
                if (Plugin.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                    stanceRotation = lowReadyMiniTargetQuaternion;
                }

                Plugin.StanceBlender.Speed = Plugin.LowReadySpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, lowReadyTargetPosition, Plugin.StanceTransitionSpeed.Value * stanceMulti * dt);

            }
            else if (Plugin.StanceBlender.Value > 0f && !hasResetLowReady && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsShortStock && !isResettingActiveAim && !isResettingHighReady && !isResettingShortStock)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingLowReady = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyResetRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = lowReadyRevertQuaternion;

                Plugin.StanceBlender.Speed = Plugin.LowReadyResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);

            }
            else if (Plugin.StanceBlender.Value == 0f && !hasResetLowReady)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties) 
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                stanceRotation = Quaternion.identity;

                isResettingLowReady = false;
                hasResetLowReady = true;
            }

            ////active aiming////
            if (StanceController.IsActiveAiming && !__instance.IsAiming && !StanceController.IsLowReady && !StanceController.IsShortStock && !StanceController.IsHighReady && !StanceController.CancelActiveAim && !Plugin.IsBlindFiring)
            {
                float shortToActive = 1f;
                float highToActive = 1f;
                float lowToActive = 1f;
                isResettingActiveAim = false;
                hasResetActiveAim = false;

                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                if (StanceController.StanceTargetPosition != activeAimTargetPosition)
                {
                    if (!hasResetShortStock)
                    {
                        shortToActive = 2.15f;
                    }
                    if (!hasResetHighReady)
                    {
                        highToActive = 2.4f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToActive = 2.25f;
                    }
                }
                if (StanceController.StanceTargetPosition == activeAimTargetPosition)
                {
                    hasResetShortStock = true;
                    hasResetHighReady = true;
                    hasResetLowReady = true;
                }

                float transitionSpeedFactor = shortToActive * highToActive * lowToActive;

                rotationSpeed = 4f * stanceMulti * dt * Plugin.ActiveAimRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                stanceRotation = activeAimTargetQuaternion;
                if (Plugin.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ActiveAimAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                    stanceRotation = activeAimMiniTargetQuaternion;
                }

                Plugin.StanceBlender.Speed = Plugin.ActiveAimSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, activeAimTargetPosition, Plugin.StanceTransitionSpeed.Value * stanceMulti * dt);

            }
            else if (Plugin.StanceBlender.Value > 0f && !hasResetActiveAim && !StanceController.IsLowReady && !StanceController.IsHighReady && !StanceController.IsShortStock && !isResettingLowReady && !isResettingHighReady && !isResettingShortStock)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingActiveAim = true;
                rotationSpeed = stanceMulti * dt * Plugin.ActiveAimResetRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = activeAimRevertQuaternion;
                Plugin.StanceBlender.Speed = Plugin.ActiveAimResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);

            }
            else if (Plugin.StanceBlender.Value == 0f && hasResetActiveAim == false)
            {
                if (PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                stanceRotation = Quaternion.identity;

                isResettingActiveAim = false;
                hasResetActiveAim = true;
            }
        }

        private static void doMountingEffects(Player player, ProceduralWeaponAnimation pwa) 
        {
            AccessTools.Method(typeof(Player), "method_40").Invoke(player, new object[] { 2f });
            for (int i = 0; i < pwa.Shootingg.ShotVals.Length; i++)
            {
                pwa.Shootingg.ShotVals[i].Process(StanceController.WeaponIsMounting ? StanceController.CoverWiggleDirection : StanceController.CoverWiggleDirection * -1f);
            }
        }

        public static void DoMounting(ManualLogSource Logger, Player player, ProceduralWeaponAnimation pwa, ref Vector3 weaponWorldPos, ref Vector3 mountWeapPosition) 
        {
            bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

            if (StanceController.WeaponIsMounting && (isMoving || !Plugin.IsAiming)) 
            {
                StanceController.WeaponIsMounting = false;
                doMountingEffects(player, pwa);
            }
            if (Plugin.IsAiming && Input.GetKeyDown(Plugin.MountKeybind.Value.MainKey) && StanceController.WeaponIsBracing && player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire)
            {
                StanceController.WeaponIsMounting = !StanceController.WeaponIsMounting;
                if (StanceController.WeaponIsMounting)
                {
                    mountWeapPosition = weaponWorldPos; // + StanceController.CoverDirection
                }

                doMountingEffects(player, pwa);
            }
            if (Input.GetKeyDown(Plugin.MountKeybind.Value.MainKey) && !StanceController.WeaponIsBracing && StanceController.WeaponIsMounting)
            {
                StanceController.WeaponIsMounting = false;
                doMountingEffects(player, pwa);
            }
            if (StanceController.WeaponIsMounting)
            {
                AccessTools.Field(typeof(TurnAwayEffector), "_turnAwayThreshold").SetValue(pwa.TurnAway, 1f);
               /* weaponWorldPos = mountWeapPosition + StanceController.CoverDirection; */ // this makes it feel more clamped to cover but no H recoil
                weaponWorldPos = new Vector3(mountWeapPosition.x, mountWeapPosition.y, weaponWorldPos.z); //this makes it feel less clamped to cover but allows h recoil + StanceController.CoverDirection

   /*             StanceController.CanDoDismountTimer = true;*/

                /*pwa.HandsContainer.CameraTransform.localRotation = cameraLocalRotation;*/
                /*player.CurrentState.RotationSpeedClamp = Plugin.test1.Value;*/
            }
/*            else if (StanceController.CanDoDismountTimer) 
            {
                if (!savedResetPostion) 
                {
                    resetPostion = weaponWorldPos;
                    savedResetPostion = true;
                }

                StanceController.DismountTimer += Time.deltaTime;
                if (StanceController.DismountTimer > Plugin.test1.Value)
                {
                    StanceController.CanDoDismountTimer = false;
                    StanceController.DismountTimer = 0f;
                    return;
                }

                AccessTools.Field(typeof(TurnAwayEffector), "_turnAwayThreshold").SetValue(pwa.TurnAway, 0.5f);
                resetPostion = Vector3.Lerp(resetPostion, pwa.HandsContainer.WeaponRootAnim.position, Plugin.test2.Value);
                weaponWorldPos = resetPostion;
            }*/

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
            if (Utils.IsReady)
            {
                if ((StanceController.IsHighReady == true || StanceController.IsLowReady == true) && !Plugin.IsAiming)
                {
                    Vector3 playerPos = Singleton<GameWorld>.Instance.AllAlivePlayersList[0].Transform.position;
                    Vector3 lightPos = __instance.gameObject.transform.position;
                    float distanceFromPlayer = Vector3.Distance(lightPos, playerPos);
                    if (distanceFromPlayer <= 1.8f)
                    {
                        return false;
                    }
                    return true;
                }
                return true;
            }
            else
            {
                return true;
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

    public class SetFireModePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetFireMode", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(FirearmsAnimator __instance, Weapon.EFireMode fireMode, bool skipAnimation = false)
        {
            __instance.ResetLeftHand();
            skipAnimation = StanceController.IsHighReady && PlayerProperties.IsSprinting ? true : skipAnimation;
            WeaponAnimationSpeedControllerClass.SetFireMode(__instance.Animator, (float)fireMode);
            if (!skipAnimation)
            {
                WeaponAnimationSpeedControllerClass.TriggerFiremodeSwitch(__instance.Animator);
            }
            return false;
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
            if (player.IsYourPlayer)
            {
                WeaponProperties.BaseWeaponLength = length;
                WeaponProperties.NewWeaponLength = length >= 0.9f ? length * 1.15f : length;
            }
        }
    }

    public class OperateStationaryWeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OperateStationaryWeapon", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (__instance.IsYourPlayer)
            {
                StanceController.CancelAllStances();
                Plugin.StanceBlender.Target = 0f;
                StanceController.StanceTargetPosition = Vector3.zero;

            }
        }
    }

    public class CollisionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("method_8", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static int timer = 0;

        private static void setMountingStatus(bool isTop, bool isRightide) 
        {
            if (isTop)
            {
                StanceController.IsBracingTop = true;
                StanceController.IsBracingSide = false;
                StanceController.IsBracingRightSide = false;
                StanceController.IsBracingLeftSide = false;
            }
            else
            {
                StanceController.IsBracingSide = true;
                StanceController.IsBracingTop = false;

                if (isRightide)
                {
                    StanceController.IsBracingRightSide = true;
                    StanceController.IsBracingLeftSide = false;
                }
                else 
                {
                    StanceController.IsBracingRightSide = false;
                    StanceController.IsBracingLeftSide = true;
                }
            }
        }

        private static void doStability(bool isTop, bool isRightide, string weapClass) 
        {
            if (!StanceController.WeaponIsMounting) 
            {
                setMountingStatus(isTop, isRightide);
            }

            StanceController.WeaponIsBracing = true;

            float mountOrientationBonus = StanceController.IsBracingTop ? 0.75f : 1f;
            float mountingRecoilLimit = weapClass == "pistol" ? 0.1f : 0.65f;

            StanceController.BracingSwayBonus = Mathf.Lerp(StanceController.BracingSwayBonus, 0.75f * mountOrientationBonus, 0.25f);
            StanceController.BracingRecoilBonus = Mathf.Lerp(StanceController.BracingRecoilBonus, 0.85f * mountOrientationBonus, 0.25f);
            StanceController.MountingSwayBonus = Mathf.Clamp(0.55f * mountOrientationBonus, 0.4f, 1f);
            StanceController.MountingRecoilBonus = Mathf.Clamp(mountingRecoilLimit * mountOrientationBonus, 0.1f, 1f);
        }

        [PatchPrefix]
        private static void PatchPrefix(Player.FirearmController __instance, Vector3 origin, float ln, Vector3? weaponUp = null)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer)
            {
                timer += 1;
                if (timer >= 60) 
                {
                    timer = 0;

                    int int_0 = (int)AccessTools.Field(typeof(EFT.Player.FirearmController), "int_0").GetValue(__instance);
                    RaycastHit[] raycastHit_0 = AccessTools.StaticFieldRefAccess<EFT.Player.FirearmController, RaycastHit[]>("raycastHit_0");
                    Func<RaycastHit, bool> func_1 = (Func<RaycastHit, bool>)AccessTools.Field(typeof(EFT.Player.FirearmController), "func_1").GetValue(__instance);

                    string weapClass = __instance.Item.WeapClass;

                    float wiggleAmount = 6f;
                    float moveToCoverOffset = 0.005f;

                    Transform weapTransform = player.ProceduralWeaponAnimation.HandsContainer.WeaponRootAnim;
                    Vector3 linecastDirection = weapTransform.TransformDirection(Vector3.up);

                    Vector3 startDown = weapTransform.position + weapTransform.TransformDirection(new Vector3(0f, 0f, -0.12f));
                    Vector3 startLeft = weapTransform.position + weapTransform.TransformDirection(new Vector3(0.1f, 0f, 0f));
                    Vector3 startRight = weapTransform.position + weapTransform.TransformDirection(new Vector3(-0.1f, 0f, 0f));

                    Vector3 forwardDirection = startDown - linecastDirection * ln;
                    Vector3 leftDirection = startLeft - linecastDirection * ln;
                    Vector3 rightDirection = startRight - linecastDirection * ln;
                    /*
                                    DebugGizmos.SingleObjects.Line(startDown, forwardDirection, Color.red, 0.02f, true, 0.3f, true);
                                    DebugGizmos.SingleObjects.Line(startLeft, leftDirection, Color.green, 0.02f, true, 0.3f, true);
                                    DebugGizmos.SingleObjects.Line(startRight, rightDirection, Color.yellow, 0.02f, true, 0.3f, true);*/

                    RaycastHit raycastHit;
                    if (GClass682.Linecast(startDown, forwardDirection, out raycastHit, EFTHardSettings.Instance.WEAPON_OCCLUSION_LAYERS, false, raycastHit_0, func_1))
                    {
                        doStability(true, false, weapClass);
                        StanceController.CoverWiggleDirection = new Vector3(wiggleAmount, 0f, 0f);
                        StanceController.CoverDirection = new Vector3(0f, -moveToCoverOffset, 0f);
                        return;
                    }
                    if (GClass682.Linecast(startLeft, leftDirection, out raycastHit, EFTHardSettings.Instance.WEAPON_OCCLUSION_LAYERS, false, raycastHit_0, func_1))
                    {
                        doStability(false, false, weapClass);
                        StanceController.CoverWiggleDirection = new Vector3(0f, wiggleAmount, 0f);
                        StanceController.CoverDirection = new Vector3(moveToCoverOffset, 0f, 0f);
                        return;
                    }
                    if (GClass682.Linecast(startRight, rightDirection, out raycastHit, EFTHardSettings.Instance.WEAPON_OCCLUSION_LAYERS, false, raycastHit_0, func_1))
                    {
                        doStability(false, true, weapClass);
                        StanceController.CoverWiggleDirection = new Vector3(0f, -wiggleAmount, 0f);
                        StanceController.CoverDirection = new Vector3(-moveToCoverOffset, 0f, 0f);
                        return;
                    }

                    StanceController.BracingSwayBonus = Mathf.Lerp(StanceController.BracingSwayBonus, 1f, 0.5f);
                    StanceController.BracingRecoilBonus = Mathf.Lerp(StanceController.BracingRecoilBonus, 1f, 0.5f);
                    StanceController.WeaponIsBracing = false;
                }
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
        private static bool PatchPrefix(Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);

            if (player.IsYourPlayer && StanceController.WeaponIsMounting)
            {
                return false;
            }
            return true;
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
                        AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.75f);
                    }
                    else
                    {
                        AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.85f);
                    }
                    return;
                }
                AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength);
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

            PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "ginterface114_0").GetValue(__instance);

            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer)
                {
                    Plugin.WeaponOffsetPosition = __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX.Value, Plugin.WeapOffsetY.Value, Plugin.WeapOffsetZ.Value);
                    __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX.Value, Plugin.WeapOffsetY.Value, Plugin.WeapOffsetZ.Value);
                    Plugin.TransformBaseStartPosition = new Vector3(0.0f, 0.0f, 0.0f);
                }
            }
        }
    }


    public class ZeroAdjustmentsPatch : ModulePatch
    {
        private static Vector3 targetPosition = Vector3.zero;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ZeroAdjustments", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {

            PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "ginterface114_0").GetValue(__instance);

            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.IsYourPlayer) // player.MovementContext.CurrentState.Name != EPlayerState.Stationary && player.IsYourPlayer
                {
                    float collidingModifier = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);
                    Vector3 blindfirePosition = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_5").GetValue(__instance);

                    Vector3 highReadyTargetPosition = new Vector3(Plugin.HighReadyOffsetX.Value, Plugin.HighReadyOffsetY.Value, Plugin.HighReadyOffsetZ.Value);

                    __instance.PositionZeroSum.y = (__instance._shouldMoveWeaponCloser ? 0.05f : 0f);
                    __instance.RotationZeroSum.y = __instance.SmoothedTilt * __instance.PossibleTilt;

                    float stanceBlendValue = Plugin.StanceBlender.Value;
                    float stanceAbs = Mathf.Abs(stanceBlendValue);

                    float blindFireBlendValue = __instance.BlindfireBlender.Value;
                    float blindFireAbs = Mathf.Abs(blindFireBlendValue);

                    if (blindFireAbs > 0f)
                    {
                        Plugin.IsBlindFiring = true;
                        float pitch = ((Mathf.Abs(__instance.Pitch) < 45f) ? 1f : ((90f - Mathf.Abs(__instance.Pitch)) / 45f));
                        AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").SetValue(__instance, pitch);
                        AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").SetValue(__instance, ((blindFireBlendValue > 0f) ? (__instance.BlindFireRotation * blindFireAbs) : (__instance.SideFireRotation * blindFireAbs)));
                        targetPosition = ((blindFireBlendValue > 0f) ? (__instance.BlindFireOffset * blindFireAbs) : (__instance.SideFireOffset * blindFireAbs));
                        targetPosition += StanceController.StanceTargetPosition;
                        __instance.BlindFireEndPosition = ((blindFireBlendValue > 0f) ? __instance.BlindFireOffset : __instance.SideFireOffset);
                        __instance.BlindFireEndPosition *= pitch;
                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * collidingModifier * targetPosition;
                        __instance.HandsContainer.HandsRotation.Zero = __instance.RotationZeroSum;

                        return false;
                    }

                    Plugin.IsBlindFiring = false;

                    if (stanceAbs > 0f)
                    {
                        float pitch = ((Mathf.Abs(__instance.Pitch) < 45f) ? 1f : ((90f - Mathf.Abs(__instance.Pitch)) / 45f));
                        AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").SetValue(__instance, pitch);
                        targetPosition = StanceController.StanceTargetPosition * stanceAbs;
                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * targetPosition;
                        __instance.HandsContainer.HandsRotation.Zero = __instance.RotationZeroSum;
                        return false;
                    }

                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + collidingModifier * targetPosition;
                    __instance.HandsContainer.HandsRotation.Zero = __instance.RotationZeroSum;
                    return false;
                }
            }
            return true;
        }
    }

    public class ApplySimpleRotationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("ApplySimpleRotation", BindingFlags.Instance | BindingFlags.Public);
        }

        private static bool hasResetActiveAim = true;
        private static bool hasResetLowReady = true;
        private static bool hasResetHighReady = true;
        private static bool hasResetShortStock = true;
        private static bool hasResetPistolPos = true;

        private static bool isResettingActiveAim = false;
        private static bool isResettingLowReady = false;
        private static bool isResettingHighReady = false;
        private static bool isResettingShortStock = false;
        private static bool isResettingPistol = false;

        private static Quaternion currentRotation = Quaternion.identity;
        private static Quaternion stanceRotation = Quaternion.identity;

        private static float stanceSpeed = 1f;

        [PatchPostfix]
        private static void Postfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {

            PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "ginterface114_0").GetValue(__instance);

            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);

                if (player != null) 
                {
                    FirearmController firearmController = player.HandsController as FirearmController;
                    if (player.IsYourPlayer)
                    {
                        Plugin.IsInThirdPerson = true;

                        float aimSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").GetValue(__instance);
                        float float_14 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                        Vector3 vector3_4 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").GetValue(__instance);
                        Vector3 vector3_6 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").GetValue(__instance);
                        Quaternion quaternion_2 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_2").GetValue(__instance);
                        Quaternion quaternion_6 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").GetValue(__instance);
                        bool bool_1 = (bool)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "bool_1").GetValue(__instance);
                        float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

                        bool isPistol = weapon.WeapClass == "pistol";
                        bool allStancesReset = hasResetActiveAim && hasResetLowReady && hasResetHighReady && hasResetShortStock && hasResetPistolPos;
                        bool isInStance = StanceController.IsHighReady || StanceController.IsLowReady || StanceController.IsShortStock || StanceController.IsActiveAiming;
                        bool isInShootableStance = StanceController.IsShortStock || StanceController.IsActiveAiming || isPistol;
                        bool cancelBecauseSooting = StanceController.IsFiringFromStance && !StanceController.IsActiveAiming && !StanceController.IsShortStock && !isPistol;
                        bool doStanceRotation = (isInStance || !allStancesReset || StanceController.PistolIsCompressed) && !cancelBecauseSooting;
                        bool allowActiveAimReload = Plugin.ActiveAimReload.Value && PlayerProperties.IsInReloadOpertation && !PlayerProperties.IsAttemptingToReloadInternalMag && !PlayerProperties.IsQuickReloading;
                        bool cancelStance = (StanceController.CancelActiveAim && StanceController.IsActiveAiming && !allowActiveAimReload) || (StanceController.CancelHighReady && StanceController.IsHighReady) || (StanceController.CancelLowReady && StanceController.IsLowReady) || (StanceController.CancelShortStock && StanceController.IsShortStock) || (StanceController.CancelPistolStance && StanceController.PistolIsCompressed);

                        currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && allStancesReset ? quaternion_2 : doStanceRotation ? stanceRotation : Quaternion.identity, doStanceRotation ? stanceSpeed : __instance.IsAiming ? 8f * aimSpeed * dt : 8f * dt);

                        Quaternion rhs = Quaternion.Euler(float_14 * Single_3 * vector3_6);
                        __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(vector3_4, quaternion_6 * rhs * currentRotation);

                        if (isPistol && !WeaponProperties.HasShoulderContact && Plugin.EnableAltPistol.Value)
                        {
                            if (StanceController.PistolIsCompressed && !Plugin.IsAiming && !isResettingPistol && !Plugin.IsBlindFiring)
                            {
                                Plugin.StanceBlender.Target = 1f;
                            }
                            else
                            {
                                Plugin.StanceBlender.Target = 0f;
                            }

                            if ((!StanceController.PistolIsCompressed && !Plugin.IsAiming && !isResettingPistol) || (Plugin.IsBlindFiring))
                            {
                                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                            }

                            hasResetActiveAim = true;
                            hasResetHighReady = true;
                            hasResetLowReady = true;
                            hasResetShortStock = true;
                            StanceController.DoPistolStances(true, ref __instance, ref stanceRotation, dt, ref hasResetPistolPos, player, Logger, ref stanceSpeed, ref isResettingPistol, firearmController);
                        }
                        else
                        {
                            if ((!isInStance && allStancesReset) || (cancelBecauseSooting && !isInShootableStance) || Plugin.IsAiming || cancelStance || Plugin.IsBlindFiring)
                            {
                                Plugin.StanceBlender.Target = 0f;
                            }
                            else if (isInStance)
                            {
                                Plugin.StanceBlender.Target = 1f;
                            }

                            if (((!isInStance && allStancesReset) && !cancelBecauseSooting && !Plugin.IsAiming) || (Plugin.IsBlindFiring))
                            {
                                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                            }

                            hasResetPistolPos = true;
                            StanceController.DoRifleStances(Logger, player, firearmController, true, ref __instance, ref stanceRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim, ref stanceSpeed);
                        }

                        StanceController.HasResetActiveAim = hasResetActiveAim;
                        StanceController.HasResetHighReady = hasResetHighReady;
                        StanceController.HasResetLowReady = hasResetLowReady;
                        StanceController.HasResetShortStock = hasResetShortStock;
                        StanceController.HasResetPistolPos = hasResetPistolPos;

                    }
                    else if (player.IsAI)
                    {
                        Quaternion targetRotation = Quaternion.identity;
                        Quaternion quaternion_2 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_2").GetValue(__instance);
                        Quaternion currentRotation = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_3").GetValue(__instance);
                        Quaternion quaternion_6 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").GetValue(__instance);

                        float float_14 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                        Vector3 vector3_4 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").GetValue(__instance);
                        Vector3 vector3_6 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").GetValue(__instance);

                        Vector3 lowReadyTargetRotation = new Vector3(18.0f, 5.0f, -1.0f);
                        Quaternion lowReadyTargetQuaternion = Quaternion.Euler(lowReadyTargetRotation);
                        Vector3 lowReadyTargetPostion = new Vector3(0.06f, 0.04f, 0.0f);

                        Vector3 highReadyTargetRotation = new Vector3(-15.0f, 3.0f, 3.0f);
                        Quaternion highReadyTargetQuaternion = Quaternion.Euler(highReadyTargetRotation);
                        Vector3 highReadyTargetPostion = new Vector3(0.05f, 0.1f, -0.12f);

                        Vector3 activeAimTargetRotation = new Vector3(0.0f, -40.0f, 0.0f);
                        Quaternion activeAimTargetQuaternion = Quaternion.Euler(activeAimTargetRotation);
                        Vector3 activeAimTargetPostion = new Vector3(0.0f, 0.0f, 0.0f);

                        Vector3 shortStockTargetRotation = new Vector3(0.0f, -28.0f, 0.0f);
                        Quaternion shortStockTargetQuaternion = Quaternion.Euler(shortStockTargetRotation);
                        Vector3 shortStockTargetPostion = new Vector3(0.05f, 0.18f, -0.2f);

                        Vector3 tacPistolTargetRotation = new Vector3(0.0f, -20.0f, 0.0f);
                        Quaternion tacPistolTargetQuaternion = Quaternion.Euler(tacPistolTargetRotation);
                        Vector3 tacPistolTargetPosition = new Vector3(-0.1f, 0.1f, -0.05f);

                        Vector3 normalPistolTargetRotation = new Vector3(0f, -5.0f, 0.0f);
                        Quaternion normalPistolTargetQuaternion = Quaternion.Euler(normalPistolTargetRotation);
                        Vector3 normalPistolTargetPosition = new Vector3(-0.05f, 0.0f, 0.0f);

                        AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, 1f);
                        float pitch = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                        float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

                        FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                        NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                        bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                        bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);

                        float lastDistance = player.AIData.BotOwner.AimingData.LastDist2Target;
                        Vector3 distanceVect = player.AIData.BotOwner.AimingData.RealTargetPoint - player.AIData.BotOwner.MyHead.position;
                        float realDistance = distanceVect.magnitude;

                        bool isTacBot = StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString());
                        bool isPeace = player.AIData.BotOwner.Memory.IsPeace;
                        bool notShooting = !player.AIData.BotOwner.ShootData.Shooting && Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd > 15f;
                        bool isInStance = false;
                        float stanceSpeed = 1f;

                        ////peaceful positon//// (player.AIData.BotOwner.Memory.IsPeace == true && !StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString()) && !player.IsSprintEnabled && !__instance.IsAiming && !player.AIData.BotOwner.ShootData.Shooting && (Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd) > 20f)

                        if (player.AIData.BotOwner.GetPlayer.MovementContext.BlindFire == 0)
                        {
                            if (isPeace && !player.IsSprintEnabled && player.MovementContext.StationaryWeapon == null && !__instance.IsAiming && !firearmController.IsInReloadOperation() && !firearmController.IsInventoryOpen() && !firearmController.IsInInteractionStrictCheck() && !firearmController.IsInSpawnOperation() && !firearmController.IsHandsProcessing()) // && player.AIData.BotOwner.WeaponManager.IsWeaponReady &&  player.AIData.BotOwner.WeaponManager.InIdleState()
                            {
                                isInStance = true;
                                player.HandsController.FirearmsAnimator.SetPatrol(true);
                            }
                            else
                            {
                                player.HandsController.FirearmsAnimator.SetPatrol(false);
                                if (weapon.WeapClass != "pistol")
                                {
                                    ////low ready//// 
                                    if (!isTacBot && !firearmController.IsInReloadOperation() && !player.IsSprintEnabled && !__instance.IsAiming && notShooting && (lastDistance >= 25f || lastDistance == 0f))    // (Time.time - player.AIData.BotOwner.Memory.LastEnemyTimeSeen) > 1f
                                    {
                                        isInStance = true;
                                        stanceSpeed = 4f * dt * 3f;
                                        targetRotation = lowReadyTargetQuaternion;
                                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * lowReadyTargetPostion;
                                    }

                                    ////high ready////
                                    if (isTacBot && !firearmController.IsInReloadOperation() && !__instance.IsAiming && notShooting && (lastDistance >= 25f || lastDistance == 0f))
                                    {
                                        isInStance = true;
                                        player.BodyAnimatorCommon.SetFloat(GClass1710.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2);
                                        stanceSpeed = 4f * dt * 2.7f;
                                        targetRotation = highReadyTargetQuaternion;
                                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * highReadyTargetPostion;
                                    }
                                    else
                                    {
                                        player.BodyAnimatorCommon.SetFloat(GClass1710.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)firearmController.Item.CalculateCellSize().X);
                                    }

                                    ///active aim//// 
                                    if (isTacBot && (((nvgIsOn || fsIsON) && !player.IsSprintEnabled && !firearmController.IsInReloadOperation() && lastDistance < 25f && lastDistance > 2f && lastDistance != 0f) || (__instance.IsAiming && (nvgIsOn && __instance.CurrentScope.IsOptic || fsIsON))))
                                    {
                                        isInStance = true;
                                        stanceSpeed = 4f * dt * 1.5f;
                                        targetRotation = activeAimTargetQuaternion;
                                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * activeAimTargetPostion;
                                    }

                                    ///short stock//// 
                                    if (isTacBot && !player.IsSprintEnabled && !firearmController.IsInReloadOperation() && lastDistance <= 2f && lastDistance != 0f)
                                    {
                                        isInStance = true;
                                        stanceSpeed = 4f * dt * 3f;
                                        targetRotation = shortStockTargetQuaternion;
                                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * shortStockTargetPostion;
                                    }
                                }
                                else
                                {
                                    if (!isTacBot && !player.IsSprintEnabled && !__instance.IsAiming && notShooting)
                                    {
                                        isInStance = true;
                                        stanceSpeed = 4f * dt * 1.5f;
                                        targetRotation = normalPistolTargetQuaternion;
                                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * normalPistolTargetPosition;
                                    }

                                    if (isTacBot && !player.IsSprintEnabled && !__instance.IsAiming && notShooting)
                                    {
                                        isInStance = true;
                                        stanceSpeed = 4f * dt * 1.5f;
                                        targetRotation = tacPistolTargetQuaternion;
                                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * tacPistolTargetPosition;
                                    }
                                }
                            }
                        }

                        Quaternion rhs = Quaternion.Euler(float_14 * Single_3 * vector3_6);
                        currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && !isInStance ? quaternion_2 : isInStance ? targetRotation : Quaternion.identity, isInStance ? stanceSpeed : 8f * dt);
                        __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(vector3_4, quaternion_6 * rhs * currentRotation);
                        AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_3").SetValue(__instance, currentRotation);
                    }
                }
            }
        }
    }

    public class RotatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementState).GetMethod("Rotate", BindingFlags.Instance | BindingFlags.Public);
        }

        private static Vector2 initialRotation = Vector3.zero;

        [PatchPrefix]
        private static bool Prefix(MovementState __instance, ref Vector2 deltaRotation, bool ignoreClamp)
        {
            GClass1667 MovementContext = (GClass1667)AccessTools.Field(typeof(MovementState), "MovementContext").GetValue(__instance);
            Player player = (Player)AccessTools.Field(typeof(GClass1667), "player_0").GetValue(MovementContext);

            if (player.IsYourPlayer) 
            {

                if (!StanceController.WeaponIsMounting)
                {
                    initialRotation = MovementContext.Rotation;
                }

                if (StanceController.WeaponIsMounting && !ignoreClamp)
                {
                    FirearmController fc = player.HandsController as FirearmController;

                    Vector2 currentRotation = MovementContext.Rotation;

                    deltaRotation *= (fc.AimingSensitivity * 0.9f);

                    float lowerClampXLimit = StanceController.IsBracingTop ? -17f : StanceController.IsBracingRightSide ? -4f : -15f;
                    float upperClampXLimit = StanceController.IsBracingTop ? 17f : StanceController.IsBracingRightSide ? 15f : 1f;

                    float lowerClampYLimit = StanceController.IsBracingTop ? -10f : -8f;
                    float upperClampYLimit = StanceController.IsBracingTop ? 10f : 15f;

                    float relativeLowerXLimit = initialRotation.x + lowerClampXLimit;
                    float relativeUpperXLimit = initialRotation.x + upperClampXLimit;
                    float relativeLowerYLimit = initialRotation.y + lowerClampYLimit;
                    float relativeUpperYLimit = initialRotation.y + upperClampYLimit;

                    float clampedX = Mathf.Clamp(currentRotation.x + deltaRotation.x, relativeLowerXLimit, relativeUpperXLimit);
                    float clampedY = Mathf.Clamp(currentRotation.y + deltaRotation.y, relativeLowerYLimit, relativeUpperYLimit);

                    deltaRotation = new Vector2(clampedX - currentRotation.x, clampedY - currentRotation.y);

                    deltaRotation = MovementContext.ClampRotation(deltaRotation);

                    MovementContext.Rotation += deltaRotation;

                    return false;
                }
            }
            return true;
        }
    }

    public class SetTiltPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementState).GetMethod("SetTilt", BindingFlags.Instance | BindingFlags.Public);
        }

        public static float currentTilt = 0f;
        public static float currentPoseLevel = 0f;

        [PatchPrefix]
        private static void Prefix(MovementState __instance, float tilt)
        {
            GClass1667 MovementContext = (GClass1667)AccessTools.Field(typeof(MovementState), "MovementContext").GetValue(__instance);
            Player player_0 = (Player)AccessTools.Field(typeof(GClass1667), "player_0").GetValue(MovementContext);
            if (player_0.IsYourPlayer) 
            {
                if (!StanceController.WeaponIsMounting) 
                {
                    currentTilt = tilt;
                    currentPoseLevel = MovementContext.PoseLevel;
                }
                if (currentTilt != tilt || currentPoseLevel != MovementContext.PoseLevel || !MovementContext.IsGrounded) 
                {
                    StanceController.WeaponIsMounting = false;
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

        private static bool hasResetActiveAim = true;
        private static bool hasResetLowReady = true;
        private static bool hasResetHighReady = true;
        private static bool hasResetShortStock = true;
        private static bool hasResetPistolPos = true;

        private static bool isResettingActiveAim = false;
        private static bool isResettingLowReady = false;
        private static bool isResettingHighReady = false;
        private static bool isResettingShortStock = false;
        private static bool isResettingPistol = false;

        private static Quaternion currentRotation = Quaternion.identity;
        private static Quaternion stanceRotation = Quaternion.identity;

        private static float stanceSpeed = 1f;

        private static Vector3 mountWeapPosition = Vector3.zero;
        private static Quaternion mountWeapRotation = Quaternion.identity;

        [PatchPostfix]
        private static void Postfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {
            PlayerInterface playerInterface = (PlayerInterface)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "ginterface114_0").GetValue(__instance);
            if (playerInterface != null && playerInterface.Weapon != null)
            {
                Weapon weapon = playerInterface.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.IsYourPlayer)
                {
                    FirearmController firearmController = player.HandsController as FirearmController;

                    Plugin.IsInThirdPerson = false;

                    float float_9 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").GetValue(__instance);
                    float float_13 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_13").GetValue(__instance);
                    float float_14 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                    float float_21 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_21").GetValue(__instance);
                    Vector3 vector3_6 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").GetValue(__instance);
                    Quaternion quaternion_2 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_2").GetValue(__instance);
                    Quaternion quaternion_5 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_5").GetValue(__instance);
                    Quaternion quaternion_6 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").GetValue(__instance);
                    bool bool_1 = (bool)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "bool_1").GetValue(__instance);
                    float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

        
                    Vector3 vector = __instance.HandsContainer.HandsRotation.Get();
                    Vector3 value = __instance.HandsContainer.SwaySpring.Value;
                    vector += float_21 * (bool_1 ? __instance.AimingDisplacementStr : 1f) * new Vector3(value.x, 0f, value.z);
                    vector += value;
                    Vector3 position = __instance._shouldMoveWeaponCloser ? __instance.HandsContainer.RotationCenterWoStock : __instance.HandsContainer.RotationCenter;
                    Vector3 worldPivot = __instance.HandsContainer.WeaponRootAnim.TransformPoint(position);
                    Vector3 weaponWorldPos = __instance.HandsContainer.WeaponRootAnim.position;

                    StanceController.DoMounting(Logger, player, __instance, ref weaponWorldPos, ref mountWeapPosition);

                    __instance.DeferredRotateWithCustomOrder(__instance.HandsContainer.WeaponRootAnim, worldPivot, vector);
                    Vector3 vector2 = __instance.HandsContainer.Recoil.Get();
                    if (vector2.magnitude > 1E-45f)
                    {
                        if (float_13 < 1f && __instance.ShotNeedsFovAdjustments)
                        {
                            vector2.x = Mathf.Atan(Mathf.Tan(vector2.x * 0.017453292f) * float_13) * 57.29578f;
                            vector2.z = Mathf.Atan(Mathf.Tan(vector2.z * 0.017453292f) * float_13) * 57.29578f;
                        }
                        Vector3 worldPivot2 = weaponWorldPos + quaternion_6 * __instance.HandsContainer.RecoilPivot;
                        __instance.DeferredRotate(__instance.HandsContainer.WeaponRootAnim, worldPivot2, quaternion_6 * vector2);
                    }

                    __instance.ApplyAimingAlignment(dt);

                    bool isPistol = firearmController.Item.WeapClass == "pistol";
                    bool allStancesReset = hasResetActiveAim && hasResetLowReady && hasResetHighReady && hasResetShortStock && hasResetPistolPos;
                    bool isInStance = StanceController.IsHighReady || StanceController.IsLowReady || StanceController.IsShortStock || StanceController.IsActiveAiming;
                    bool isInShootableStance = StanceController.IsShortStock || StanceController.IsActiveAiming || isPistol;
                    bool cancelBecauseSooting = StanceController.IsFiringFromStance && !StanceController.IsActiveAiming && !StanceController.IsShortStock && !isPistol;
                    bool doStanceRotation = (isInStance || !allStancesReset || StanceController.PistolIsCompressed) && !cancelBecauseSooting;
                    bool allowActiveAimReload = Plugin.ActiveAimReload.Value && PlayerProperties.IsInReloadOpertation && !PlayerProperties.IsAttemptingToReloadInternalMag && !PlayerProperties.IsQuickReloading;
                    bool cancelStance = (StanceController.CancelActiveAim && StanceController.IsActiveAiming && !allowActiveAimReload) || (StanceController.CancelHighReady && StanceController.IsHighReady) || (StanceController.CancelLowReady && StanceController.IsLowReady) || (StanceController.CancelShortStock && StanceController.IsShortStock) || (StanceController.CancelPistolStance && StanceController.PistolIsCompressed);

                    currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && allStancesReset ? quaternion_2 : doStanceRotation ? stanceRotation : Quaternion.identity, doStanceRotation ? stanceSpeed : __instance.IsAiming ? 8f * float_9 * dt : 8f * dt);
                    Quaternion rhs = Quaternion.Euler(float_14 * Single_3 * vector3_6);
                    __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(weaponWorldPos, quaternion_6 * rhs * currentRotation);

                    if (isPistol && !WeaponProperties.HasShoulderContact && Plugin.EnableAltPistol.Value)
                    {
                        if (StanceController.PistolIsCompressed && !Plugin.IsAiming && !isResettingPistol && !Plugin.IsBlindFiring)
                        {
                            Plugin.StanceBlender.Target = 1f;
                        }
                        else
                        {
                            Plugin.StanceBlender.Target = 0f;
                        }
                                    
                        if ((!StanceController.PistolIsCompressed && !Plugin.IsAiming && !isResettingPistol) || (Plugin.IsBlindFiring)) 
                        {
                            StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                        }

                        hasResetActiveAim = true;
                        hasResetHighReady = true;
                        hasResetLowReady = true;
                        hasResetShortStock = true;
                        StanceController.DoPistolStances(false, ref __instance, ref stanceRotation, dt, ref hasResetPistolPos, player, Logger, ref stanceSpeed, ref isResettingPistol, firearmController);
                    }
                    else
                    {
                        if ((!isInStance && allStancesReset) || (cancelBecauseSooting && !isInShootableStance) || Plugin.IsAiming || cancelStance || Plugin.IsBlindFiring)
                        {
                            Plugin.StanceBlender.Target = 0f;
                        }
                        else if (isInStance)
                        {
                            Plugin.StanceBlender.Target = 1f;
                        }

                        if (((!isInStance && allStancesReset) && !cancelBecauseSooting && !Plugin.IsAiming) || (Plugin.IsBlindFiring))
                        {
                            StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, Vector3.zero, 5f * dt);
                        }

                        hasResetPistolPos = true;
                        StanceController.DoRifleStances(Logger, player, firearmController, false, ref __instance, ref stanceRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim, ref stanceSpeed);
                    }

                    StanceController.HasResetActiveAim = hasResetActiveAim;
                    StanceController.HasResetHighReady = hasResetHighReady;
                    StanceController.HasResetLowReady = hasResetLowReady;
                    StanceController.HasResetShortStock = hasResetShortStock;
                    StanceController.HasResetPistolPos = hasResetPistolPos;
                }
            }
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
                WeaponProperties.BaseHipfireInaccuracy = player.ProceduralWeaponAnimation.Breath.HipPenalty;
            }
        }
    }
}
