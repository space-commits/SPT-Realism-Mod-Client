/*using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using notGreg.UniformAim;
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

        private static bool setRunAnim = false;
        private static bool resetRunAnim = false;

        private static bool gotCurrentStam = false;
        private static float currentStam = 100f;

        public static Dictionary<string, bool> LightDictionary = new Dictionary<string, bool>();

        public static bool toggledLight = false;


        public static void SetStanceStamina(Player player, Player.FirearmController fc) 
        {
            if (!Plugin.IsSprinting)
            {
                gotCurrentStam = false;

                if (fc.Item.WeapClass != "pistol")
                {
                    if (!IsHighReady && !IsLowReady && !Plugin.IsAiming && !IsActiveAiming && !IsShortStock && Plugin.EnableIdleStamDrain.Value && !player.IsInPronePose)
                    {
                        player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponProperties.ErgoFactor * 0.8f * ((1f - PlayerProperties.ADSInjuryMulti) + 1f));
                    }
                    else if (IsActiveAiming)
                    {
                        player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponProperties.ErgoFactor * 0.4f * ((1f - PlayerProperties.ADSInjuryMulti) + 1f));
                    }
                    else if (!Plugin.IsAiming && !Plugin.EnableIdleStamDrain.Value)
                    {
                        player.Physical.Aim(0f);
                    }
                    if (IsHighReady && !IsLowReady && !Plugin.IsAiming && !IsShortStock)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + ((((1f - (WeaponProperties.ErgoFactor / 100f)) * 0.01f) * PlayerProperties.ADSInjuryMulti)), player.Physical.HandsStamina.TotalCapacity);
                    }
                    if (IsLowReady && !IsHighReady && !Plugin.IsAiming && !IsShortStock)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (((1f - (WeaponProperties.ErgoFactor / 100f)) * 0.03f) * PlayerProperties.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
                    }
                    if (IsShortStock && !IsHighReady && !Plugin.IsAiming && !IsLowReady)
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
            return !IsActiveAiming && !IsHighReady && !IsLowReady && !IsShortStock && !WasHighReady && !WasLowReady && !WasShortStock && !WasActiveAim ? true : false;
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

                if (!Plugin.IsSprinting && !Plugin.IsInInventory && WeaponProperties._WeapClass != "pistol")
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

                        if (IsHighReady == true && (PlayerProperties.RightArmRuined == true || PlayerProperties.LeftArmRuined == true)) 
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

                    if (DoHighReadyInjuredAnim == true)
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
                        IsLowReady = true;
                        WasLowReady = true;
                    }
                }

                HighReadyManipBuff = IsHighReady == true ? 1.2f : 1f;
                HighReadyManipDebuff = IsHighReady == true ? 0.8f : 1f;
                ActiveAimManipDebuff = IsActiveAiming == true ? 0.8f : 1f;
                LowReadyManipBuff = IsLowReady == true ? 1.2f : 1f;

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

        //doesn't work with multiple lights where one is off and the other is on
        public static void ToggleDevice(FirearmController fc, bool activating, ManualLogSource logger) 
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

                    fc.SetLightsState(new GStruct143[]
                    {
                        new GStruct143
                        {
                            Id = light.Item.Id,
                            IsActive = state,
                            LightMode = light.SelectedMode
                        }
                    }, false);
                }
            }
        }

        public static void DoPistolStances(bool isThirdPerson, ref EFT.Animations.ProceduralWeaponAnimation __instance, ref Quaternion stanceRotation, float dt, ref bool hasResetPistolPos, Player player, ManualLogSource logger, ref float rotationSpeed)
        {
            float aimMulti = Mathf.Clamp(WeaponProperties.SightlessAimSpeed, 0.65f, 1.45f);
            float stanceMulti = Mathf.Clamp(aimMulti * PlayerProperties.StanceInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.65f)), 0.5f, 1.45f);
            float invInjuryMulti = (1f - PlayerProperties.StanceInjuryMulti) + 1f;
            float resetAimMulti = (1f - stanceMulti) + 1f;
            float ergoDelta = (1f - WeaponProperties.ErgoDelta);
            float intensity = Mathf.Max(1f * (1f - PlayerProperties.WeaponSkillErgo) * resetAimMulti * invInjuryMulti * ergoDelta, 0.35f);
            float balanceFactor = 1f + (WeaponProperties.Balance / 100f);
            balanceFactor = WeaponProperties.Balance > 0f ? balanceFactor * -1f : balanceFactor;

            Vector3 pistolTargetRotation = new Vector3(Plugin.PistolRotationX.Value, Plugin.PistolRotationY.Value, Plugin.PistolRotationZ.Value);
            Vector3 thirdPistolTargetRotation = new Vector3(Plugin.ThirdPistolRotationX.Value, Plugin.ThirdPistolRotationY.Value, Plugin.ThirdPistolRotationZ.Value);
            Quaternion pistolTargetQuaternion = isThirdPerson ? Quaternion.Euler(thirdPistolTargetRotation) : Quaternion.Euler(pistolTargetRotation);
            Quaternion pistolMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.PistolAdditionalRotationX.Value, Plugin.PistolAdditionalRotationY.Value, Plugin.PistolAdditionalRotationZ.Value));
            Quaternion pistolRevertQuaternion = Quaternion.Euler(Plugin.PistolResetRotationX.Value * balanceFactor, Plugin.PistolResetRotationY.Value, Plugin.PistolResetRotationZ.Value);

            if (StanceController.PistolIsCompressed)
            {
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, 1f * PlayerProperties.StanceInjuryMulti);
            }

            float pitch = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
            float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

            __instance.HandsContainer.WeaponRoot.localPosition = new Vector3(Plugin.PistolTransformNewStartPosition.x, __instance.HandsContainer.TrackingTransform.localPosition.y, __instance.HandsContainer.TrackingTransform.localPosition.z);

            if (!__instance.IsAiming && !StanceController.CancelPistolStance && !StanceController.PistolIsColliding)
            {
                StanceController.PistolIsCompressed = true;

                stanceRotation = Quaternion.Lerp(stanceRotation, pistolTargetQuaternion, __instance.CameraSmoothTime * stanceMulti * dt * Plugin.PistolRotationSpeedMulti.Value * stanceMulti);
                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, stanceRotation);

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.PistolTransformNewStartPosition, Plugin.PistolPosSpeedMulti.Value * stanceMulti * dt);
                hasResetPistolPos = false;

                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = Plugin.ThirdPistolPosSpeedMulti.Value * stanceMulti;
                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * new Vector3(Plugin.ThirdPistolOffsetX.Value, Plugin.ThirdPistolOffsetY.Value, Plugin.ThirdPistolOffsetZ.Value);
                }

                if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.PistolTransformNewStartPosition)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolAdditionalRotationSpeedMulti.Value * stanceMulti;
                    stanceRotation = pistolMiniTargetQuaternion;
                }
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && hasResetPistolPos != true)
            {
                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolResetRotationSpeedMulti.Value * stanceMulti;
                stanceRotation = pistolRevertQuaternion;

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, Plugin.PistolPosResetSpeedMulti.Value * stanceMulti * dt);
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition)
            {
                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = 0.1f;
                }

                StanceController.PistolIsCompressed = false;

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                hasResetPistolPos = true;
            }
        }

        public static void DoRifleStances(ManualLogSource logger, Player player, Player.FirearmController fc, bool isThirdPerson, ref EFT.Animations.ProceduralWeaponAnimation __instance, ref Quaternion stanceRotation, float dt, ref bool isResettingShortStock, ref bool hasResetShortStock, ref bool hasResetLowReady, ref bool hasResetActiveAim, ref bool hasResetHighReady, ref bool isResettingHighReady, ref bool isResettingLowReady, ref bool isResettingActiveAim, ref float rotationSpeed)
        {
            float aimMulti = Mathf.Clamp(1f - ((1f - WeaponProperties.SightlessAimSpeed) * 1.5f), 0.6f, 0.98f);
            float stanceMulti = Mathf.Clamp(aimMulti * PlayerProperties.StanceInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.65f)), 0.45f, 0.95f);
            float invInjuryMulti = (1f - PlayerProperties.StanceInjuryMulti) + 1f;
            float resetAimMulti = (1f - stanceMulti) + 1f;
            float stocklessModifier = WeaponProperties.HasShoulderContact ? 1f : 2.3f;
            float ergoDelta = (1f - WeaponProperties.ErgoDelta);
            float intensity = Mathf.Max(1.5f * (1f - (PlayerProperties.AimSkillADSBuff * 0.5f)) * resetAimMulti * invInjuryMulti * stocklessModifier * ergoDelta, 0.5f);

            float pitch = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);

            bool isColliding = !__instance.OverlappingAllowsBlindfire;
            float collisionRotationFactor = isColliding ? 2f : 1f;
            float collisionPositionFactor = isColliding ? 2f : 1f;

            Vector3 activeAimTargetRotation = new Vector3(Plugin.ActiveAimRotationX.Value, Plugin.ActiveAimRotationY.Value, Plugin.ActiveAimRotationZ.Value);
            Vector3 thirdActiveAimTargetRotation = new Vector3(Plugin.ThirdActiveAimRotationX.Value, Plugin.ThirdActiveAimRotationY.Value, Plugin.ThirdActiveAimRotationZ.Value);
            Vector3 activeAimRevertRotation = new Vector3(Plugin.ActiveAimResetRotationX.Value * resetAimMulti, Plugin.ActiveAimResetRotationY.Value * resetAimMulti, Plugin.ActiveAimResetRotationZ.Value * resetAimMulti);
            Quaternion activeAimTargetQuaternion = isThirdPerson ? Quaternion.Euler(thirdActiveAimTargetRotation) : Quaternion.Euler(activeAimTargetRotation);
            Quaternion activeAimMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimAdditionalRotationX.Value * resetAimMulti, Plugin.ActiveAimAdditionalRotationY.Value * resetAimMulti, Plugin.ActiveAimAdditionalRotationZ.Value * resetAimMulti));
            Quaternion activeAimRevertQuaternion = Quaternion.Euler(activeAimRevertRotation);
            Vector3 activeAimTargetPosition =  new Vector3(Plugin.ActiveAimOffsetX.Value, Plugin.ActiveAimOffsetY.Value, Plugin.ActiveAimOffsetZ.Value);
            Vector3 activeTargetPostionThird = new Vector3(Plugin.ThirdActiveAimOffsetX.Value, Plugin.ThirdActiveAimOffsetY.Value, Plugin.ThirdActiveAimOffsetZ.Value);

            Vector3 lowReadyTargetRotation = new Vector3(Plugin.LowReadyRotationX.Value * collisionRotationFactor * resetAimMulti, Plugin.LowReadyRotationY.Value, Plugin.LowReadyRotationZ.Value);
            Vector3 thirdLowReadyTargetRotation = new Vector3(Plugin.ThirdLowReadyRotationX.Value * collisionRotationFactor * resetAimMulti, Plugin.ThirdLowReadyRotationY.Value, Plugin.ThirdLowReadyRotationZ.Value);
            Quaternion lowReadyTargetQuaternion = isThirdPerson ? Quaternion.Euler(thirdLowReadyTargetRotation) : Quaternion.Euler(lowReadyTargetRotation);
            Quaternion lowReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.LowReadyAdditionalRotationX.Value * resetAimMulti, Plugin.LowReadyAdditionalRotationY.Value * resetAimMulti, Plugin.LowReadyAdditionalRotationZ.Value * resetAimMulti));
            Quaternion lowReadyRevertQuaternion = Quaternion.Euler(Plugin.LowReadyResetRotationX.Value * resetAimMulti, Plugin.LowReadyResetRotationY.Value * resetAimMulti, Plugin.LowReadyResetRotationZ.Value * resetAimMulti);
            Vector3 lowReadyTargetPosition = new Vector3(Plugin.LowReadyOffsetX.Value, Plugin.LowReadyOffsetY.Value, Plugin.LowReadyOffsetZ.Value);
            Vector3 lowReadyTargetPostionThird = new Vector3(Plugin.ThirdLowReadyOffsetX.Value, Plugin.ThirdLowReadyOffsetY.Value, Plugin.ThirdLowReadyOffsetZ.Value);

            Vector3 highReadyTargetRotation = new Vector3(Plugin.HighReadyRotationX.Value * stanceMulti * collisionRotationFactor, Plugin.HighReadyRotationY.Value * stanceMulti, Plugin.HighReadyRotationZ.Value * stanceMulti);
            Vector3 thirdHighReadyTargetRotation = new Vector3(Plugin.ThirdHighReadyRotationX.Value * stanceMulti * collisionRotationFactor, Plugin.ThirdHighReadyRotationY.Value * stanceMulti, Plugin.ThirdHighReadyRotationZ.Value * stanceMulti);
            Quaternion highReadyTargetQuaternion = isThirdPerson ? Quaternion.Euler(thirdHighReadyTargetRotation) : Quaternion.Euler(highReadyTargetRotation);
            Quaternion highReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.HighReadyAdditionalRotationX.Value * resetAimMulti, Plugin.HighReadyAdditionalRotationY.Value * resetAimMulti, Plugin.HighReadyAdditionalRotationZ.Value * resetAimMulti));
            Quaternion highReadyRevertQuaternion = Quaternion.Euler(Plugin.HighReadyResetRotationX.Value * resetAimMulti, Plugin.HighReadyResetRotationY.Value * resetAimMulti, Plugin.HighReadyResetRotationZ.Value * resetAimMulti);
            Vector3 highReadyTargetPosition = new Vector3(Plugin.HighReadyOffsetX.Value, Plugin.HighReadyOffsetY.Value, Plugin.HighReadyOffsetZ.Value);
            Vector3 highReadyTargetPostionThird = new Vector3(Plugin.ThirdHighReadyOffsetX.Value, Plugin.ThirdHighReadyOffsetY.Value, Plugin.ThirdHighReadyOffsetZ.Value);

            Vector3 shortStockTargetRotation = new Vector3(Plugin.ShortStockRotationX.Value * stanceMulti, Plugin.ShortStockRotationY.Value * stanceMulti, Plugin.ShortStockRotationZ.Value * stanceMulti);
            Vector3 thirdShortStockTargetRotation = new Vector3(Plugin.ThirdShortStockRotationX.Value * stanceMulti, Plugin.ThirdShortStockRotationY.Value * stanceMulti, Plugin.ThirdShortStockRotationZ.Value * stanceMulti);
            Quaternion shortStockTargetQuaternion = isThirdPerson ? Quaternion.Euler(thirdShortStockTargetRotation) : Quaternion.Euler(shortStockTargetRotation);
            Quaternion shortStockMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ShortStockAdditionalRotationX.Value * resetAimMulti, Plugin.ShortStockAdditionalRotationY.Value * resetAimMulti, Plugin.ShortStockAdditionalRotationZ.Value * resetAimMulti));
            Quaternion shortStockRevertQuaternion = Quaternion.Euler(Plugin.ShortStockResetRotationX.Value * resetAimMulti, Plugin.ShortStockResetRotationY.Value * resetAimMulti, Plugin.ShortStockResetRotationZ.Value * resetAimMulti);
            Vector3 shortStockTargetPosition = new Vector3(Plugin.ShortStockOffsetX.Value, Plugin.ShortStockOffsetY.Value, Plugin.ShortStockOffsetZ.Value);
            Vector3 shortTargetPostionThird = new Vector3(Plugin.ThirdShortStockOffsetX.Value, Plugin.ThirdShortStockOffsetY.Value, Plugin.ThirdShortStockOffsetZ.Value);

            if (Plugin.EnableTacSprint.Value && (StanceController.IsHighReady || StanceController.WasHighReady) && !PlayerProperties.RightArmRuined)
            {
                player.BodyAnimatorCommon.SetFloat(GClass1647.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2f);
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
                    player.BodyAnimatorCommon.SetFloat(GClass1647.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)fc.Item.CalculateCellSize().X);
                    resetRunAnim = true;
                    setRunAnim = false;
                }
            }

            if (!StanceController.IsActiveAiming && !StanceController.IsShortStock)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireAccuracy * 2f;
            }
            if (StanceController.IsActiveAiming)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireAccuracy;
            }

            if (Plugin.StanceToggleDevice.Value)
            {
                if (!toggledLight && (IsHighReady || IsLowReady))
                {
                    ToggleDevice(fc, false, logger);
                    toggledLight = true;
                }
                if (toggledLight && !IsHighReady && !IsLowReady)
                {
                    ToggleDevice(fc, true, logger);
                    toggledLight = false;
                }
            }

            ////short-stock////
            if (StanceController.IsShortStock == true && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsLowReady && !__instance.IsAiming && !Plugin.IsSprinting && !StanceController.CancelShortStock)
            {
                __instance.Breath.HipPenalty = WeaponProperties.BaseHipfireAccuracy * 4f;

                float activeToShort = 1f;
                float highToShort = 1f;
                float lowToShort = 1f;
                isResettingShortStock = false;
                hasResetShortStock = false;


                if (__instance.HandsContainer.TrackingTransform.localPosition != shortStockTargetPosition)
                {
                    if (!hasResetActiveAim)
                    {
                        activeToShort = isThirdPerson ? 1.5f : 1.0f;
                    }
                    if (!hasResetHighReady)
                    {
                        highToShort = isThirdPerson ? 1.5f : 0.9f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToShort = isThirdPerson ? 1.5f : 1.0f;
                    }
                }
                if (__instance.HandsContainer.TrackingTransform.localPosition == shortStockTargetPosition)
                {
                    hasResetActiveAim = true;
                    hasResetHighReady = true;
                    hasResetLowReady = true;
                }

                rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockRotationMulti.Value;
                stanceRotation = shortStockTargetQuaternion;
                if (__instance.HandsContainer.TrackingTransform.localPosition != shortStockTargetPosition)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockAdditionalRotationSpeedMulti.Value;
                    stanceRotation = shortStockMiniTargetQuaternion;
                }

                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = Plugin.ThirdShortStockSpeedMulti.Value * stanceMulti;
                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * shortTargetPostionThird;
                }

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, shortStockTargetPosition, stanceMulti * dt * Plugin.ShortStockSpeedMulti.Value * activeToShort * highToShort * lowToShort);

            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetShortStock && !StanceController.IsLowReady && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !isResettingActiveAim && !isResettingHighReady && !isResettingLowReady)
            {
                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingShortStock = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockResetRotationSpeedMulti.Value;
                stanceRotation = shortStockRevertQuaternion;
                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, stanceMulti * dt * Plugin.ShortStockResetSpeedMulti.Value);
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetShortStock)
            {
                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = 0.1f;
                }

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
                float shortToHighMulti = 1.0f;
                float lowToHighMulti = 1.0f;
                float activeToHighMulti = 1.0f;
                isResettingHighReady = false;
                hasResetHighReady = false;

                if (__instance.HandsContainer.TrackingTransform.localPosition != highReadyTargetPosition)
                {
                    if (!hasResetShortStock) 
                    {
                        shortToHighMulti = isThirdPerson ? 1.5f : 0.8f;
                    }
                    if (!hasResetActiveAim)
                    {
                        activeToHighMulti = isThirdPerson ? 1.5f : 0.9f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToHighMulti = isThirdPerson ? 1.5f : 1.15f;
                    }
                }
                if (__instance.HandsContainer.TrackingTransform.localPosition == highReadyTargetPosition)
                {
                    hasResetActiveAim = true;
                    hasResetLowReady = true;
                    hasResetShortStock = true;
                }


                if (StanceController.DoHighReadyInjuredAnim)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value * 0.25f;
                    stanceRotation = highReadyMiniTargetQuaternion;
                    if (__instance.HandsContainer.TrackingTransform.localPosition != highReadyTargetPosition)
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyRotationMulti.Value * 0.25f;
                        stanceRotation = lowReadyTargetQuaternion;
                    }
                }
                else
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyRotationMulti.Value;
                    stanceRotation = highReadyTargetQuaternion;

                    if (__instance.HandsContainer.TrackingTransform.localPosition != highReadyTargetPosition)
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value;
                        stanceRotation = highReadyMiniTargetQuaternion;
                    }
                }

                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = Plugin.ThirdHighReadySpeedMulti.Value * stanceMulti;

                    if (!Plugin.EnableTacSprint.Value)
                    {
                        __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * highReadyTargetPostionThird;
                    }
                    else 
                    {
                        if (!Plugin.IsSprinting)
                        {
                            __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * highReadyTargetPostionThird;
                        }
                        else
                        {
                            __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * new Vector3(highReadyTargetPostionThird.x, -0.2f, -0.025f);
                        }
                    }
                }

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, highReadyTargetPosition, stanceMulti * dt * Plugin.HighReadySpeedMulti.Value * shortToHighMulti * lowToHighMulti * activeToHighMulti);
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetHighReady && !StanceController.IsLowReady && !StanceController.IsActiveAiming && !StanceController.IsShortStock && !isResettingActiveAim && !isResettingLowReady && !isResettingShortStock)
            {
                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = -intensity;
                }

                isResettingHighReady = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyResetRotationMulti.Value;
                stanceRotation = highReadyRevertQuaternion;
                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, stanceMulti * dt * Plugin.HighReadyResetSpeedMulti.Value);
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && !hasResetHighReady)
            {
                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = 0.1f;
                }

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                isResettingHighReady = false;
                hasResetHighReady = true;
            }

            ////low ready////
            if (StanceController.IsLowReady == true && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsShortStock && !__instance.IsAiming && !StanceController.IsFiringFromStance && !StanceController.CancelLowReady)
            {
                float highToLow = 1.0f;
                float shortToLow = 1.0f;
                float activeToLow = 1.0f;

                isResettingLowReady = false;
                hasResetLowReady = false;

                if (__instance.HandsContainer.TrackingTransform.localPosition != lowReadyTargetPosition)
                {
                    if (!hasResetHighReady) 
                    {
                        highToLow = isThirdPerson ? 2.0f : 1.3f;
                    }
                    if (!hasResetShortStock)
                    {
                        shortToLow = isThirdPerson ? 2.8f : 1.6f;
                    }
                    if (!hasResetActiveAim)
                    {
                        activeToLow = isThirdPerson ? 2.0f : 1.1f;
                    }

                }
                if (__instance.HandsContainer.TrackingTransform.localPosition == lowReadyTargetPosition)
                {
                    hasResetHighReady = true;
                    hasResetShortStock = true;
                    hasResetActiveAim = true;
                }

                rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyRotationMulti.Value;
                stanceRotation = lowReadyTargetQuaternion;
                if (__instance.HandsContainer.TrackingTransform.localPosition != lowReadyTargetPosition)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyAdditionalRotationSpeedMulti.Value;
                    stanceRotation = lowReadyMiniTargetQuaternion;
                }

                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = Plugin.ThirdLowReadySpeedMulti.Value * stanceMulti;
                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * lowReadyTargetPostionThird;
                }

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, lowReadyTargetPosition, stanceMulti * dt * Plugin.LowReadySpeedMulti.Value * highToLow * shortToLow * activeToLow);
            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetLowReady && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsShortStock && !isResettingActiveAim && !isResettingHighReady && !isResettingShortStock)
            {
                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingLowReady = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyResetRotationMulti.Value;
                stanceRotation = lowReadyRevertQuaternion;
                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, stanceMulti * dt * Plugin.LowReadyResetSpeedMulti.Value);
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
            if (StanceController.IsActiveAiming == true && !__instance.IsAiming && !StanceController.IsLowReady && !StanceController.IsShortStock && !StanceController.IsHighReady && !StanceController.CancelActiveAim)
            {
                float shortToActivePosMulti = 1f;
                float thirdShortToActiveMulti = 1f;
                float highToActive = 1f;
                float lowToActive = 1f;
                isResettingActiveAim = false;
                hasResetActiveAim = false;

                if (__instance.HandsContainer.TrackingTransform.localPosition != activeAimTargetPosition)
                {
                    if (!hasResetShortStock)
                    {
                        shortToActivePosMulti = isThirdPerson ? 1.7f : 1.3f;
                        thirdShortToActiveMulti = 0.6f; 
                    }
                    if (!hasResetHighReady)
                    {
                        highToActive = isThirdPerson ? 1.5f : 1.1f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToActive = isThirdPerson ? 1.5f : 1.05f;
                    }
                }
                if (__instance.HandsContainer.TrackingTransform.localPosition == activeAimTargetPosition)
                {
                    hasResetShortStock = true;
                    hasResetHighReady = true;
                    hasResetLowReady = true;
                }

                rotationSpeed = 4f * stanceMulti * dt * Plugin.ActiveAimRotationMulti.Value;
                stanceRotation = activeAimTargetQuaternion;
                if (__instance.HandsContainer.TrackingTransform.localPosition != activeAimTargetPosition)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ActiveAimAdditionalRotationSpeedMulti.Value;
                    stanceRotation = activeAimMiniTargetQuaternion;
                }

                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = Plugin.ThirdActiveAimSpeedMulti.Value * stanceMulti * thirdShortToActiveMulti;
                    __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * activeTargetPostionThird;
                }

                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, activeAimTargetPosition, stanceMulti * dt * Plugin.ActiveAimSpeedMulti.Value * shortToActivePosMulti * highToActive * lowToActive);

            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition != Plugin.TransformBaseStartPosition && !hasResetActiveAim && !StanceController.IsLowReady && !StanceController.IsHighReady && !StanceController.IsShortStock && !isResettingLowReady && !isResettingHighReady && !isResettingShortStock)
            {

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = intensity;
                }

                isResettingActiveAim = true;
                rotationSpeed = stanceMulti * dt * Plugin.ActiveAimResetRotationSpeedMulti.Value;
                stanceRotation = activeAimRevertQuaternion;
                __instance.HandsContainer.TrackingTransform.localPosition = Vector3.MoveTowards(__instance.HandsContainer.TrackingTransform.localPosition, Plugin.TransformBaseStartPosition, stanceMulti * dt * Plugin.ActiveAimResetSpeedMulti.Value);

            }
            else if (__instance.HandsContainer.TrackingTransform.localPosition == Plugin.TransformBaseStartPosition && hasResetActiveAim == false)
            {
                if (isThirdPerson)
                {
                    __instance.HandsContainer.HandsPosition.ReturnSpeed = 0.1f;
                }

                if (!isThirdPerson)
                {
                    __instance.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity;
                }

                isResettingActiveAim = false;
                hasResetActiveAim = true;
            }

       
        }
    }

*//*    public class LaserLateUpdatePatch : ModulePatch
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
                    Vector3 playerPos = Singleton<GameWorld>.Instance.AllPlayers[0].Transform.position;
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
*//*
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
            skipAnimation = StanceController.IsHighReady && Plugin.IsSprinting ? true : skipAnimation;
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
*//*                if (StanceController.WasShortStock == true && Plugin.IsAiming)
                {
                    AccessTools.Field(typeof(EFT.Player.FirearmController), "WeaponLn").SetValue(__instance, WeaponProperties.NewWeaponLength * 0.7f);
                    return;
                }*//*
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
                    Plugin.WeaponOffsetPosition = __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX.Value, Plugin.WeapOffsetY.Value, Plugin.WeapOffsetZ.Value);
                    __instance.HandsContainer.WeaponRoot.localPosition += new Vector3(Plugin.WeapOffsetX.Value, Plugin.WeapOffsetY.Value, Plugin.WeapOffsetZ.Value);
                    Plugin.TransformBaseStartPosition = new Vector3(0.0f, 0.0f, 0.0f);
                    Plugin.PistolTransformNewStartPosition = Plugin.TransformBaseStartPosition + new Vector3(Plugin.PistolOffsetX.Value, Plugin.PistolOffsetY.Value, Plugin.PistolOffsetZ.Value);
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

        private static bool hasResetActiveAim = true;
        private static bool hasResetLowReady = true;
        private static bool hasResetHighReady = true;
        private static bool hasResetShortStock = true;
        private static bool hasResetPistolPos = true;

        private static bool isResettingActiveAim = false;
        public static bool isResettingLowReady = false;
        private static bool isResettingHighReady = false;
        private static bool isResettingShortStock = false;

        private static float rotationSpeed = 1f;

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
                        StanceController.DoPistolStances(true, ref __instance, ref currentRotation, dt, ref hasResetPistolPos, player, Logger, ref rotationSpeed);
                    }
                    else
                    {
                        StanceController.DoRifleStances(Logger, player, firearmController, true, ref __instance, ref currentRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim, ref rotationSpeed);
                    }

                }
                else if (player.IsAI)
                {
                    Quaternion currentRotation = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").GetValue(__instance);

                    Vector3 lowReadyTargetRotation = new Vector3(135.0f, 50.0f, -35.0f);
                    Quaternion lowReadyTargetQuaternion = Quaternion.Euler(lowReadyTargetRotation);
                    Vector3 lowReadyTargetPostion = new Vector3(0.04f, -0.05f, 0.0f);

                    Vector3 highReadyTargetRotation = new Vector3(-75.0f, 0.0f, 15.0f);
                    Quaternion highReadyTargetQuaternion = Quaternion.Euler(highReadyTargetRotation);
                    Vector3 highReadyTargetPostion = new Vector3(0.05f, 0.04f, -0.1f);

                    Vector3 activeAimTargetRotation = new Vector3(0.0f, -130.0f, 0.0f);
                    Quaternion activeAimTargetQuaternion = Quaternion.Euler(activeAimTargetRotation);
                    Vector3 activeAimTargetPostion = new Vector3(0.0f, -0.025f, 0.0f);

                    Vector3 shortStockTargetRotation = new Vector3(0.0f, 60.0f, 0.0f);
                    Quaternion shortStockTargetQuaternion = Quaternion.Euler(shortStockTargetRotation);
                    Vector3 shortStockTargetPostion = new Vector3(0.06f, 0.2f, -0.15f);

                    Vector3 peacefulPistolTargetRotation = new Vector3(40.0f, -15.0f, 15.0f);
                    Quaternion peacefulPistolTargetQuaternion = Quaternion.Euler(peacefulPistolTargetRotation);
                    Vector3 peacefulPistolTargetPosition = new Vector3(-0.1f, 0.15f, -0.12f);

                    Vector3 tacPistolTargetRotation = new Vector3(-2.5f, -20.0f, 0.0f);
                    Quaternion tacPistolTargetQuaternion = Quaternion.Euler(peacefulPistolTargetRotation);
                    Vector3 tacPistolTargetPosition = new Vector3(-0.05f, 0.15f, -0.15f);

                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").SetValue(__instance, 1f * PlayerProperties.StanceInjuryMulti);
                    float pitch = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                    float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

                    FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                    NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                    bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);

                    float lastDistance = player.AIData.BotOwner.AimingData.LastDist2Target;
                    *//*                    Vector3 distanceVect = player.AIData.BotOwner.AimingData.RealTargetPoint - player.AIData.BotOwner.MyHead.position;
                                        float realDistance = distanceVect.magnitude;*//*

                    bool isTacBot = StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString());
                    bool isPeace = player.AIData.BotOwner.Memory.IsPeace;
                    bool notShooting = !player.AIData.BotOwner.ShootData.Shooting && Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd > 15f;

                    ////peaceful positon//// (player.AIData.BotOwner.Memory.IsPeace == true && !StanceController.botsToUseTacticalStances.Contains(player.AIData.BotOwner.Profile.Info.Settings.Role.ToString()) && !player.IsSprintEnabled && !__instance.IsAiming && !player.AIData.BotOwner.ShootData.Shooting && (Time.time - player.AIData.BotOwner.ShootData.LastTriggerPressd) > 20f)
                    if (isPeace && !player.IsSprintEnabled && player.MovementContext.StationaryWeapon == null && !__instance.IsAiming && !firearmController.IsInReloadOperation() && !firearmController.IsInventoryOpen() && !firearmController.IsInInteractionStrictCheck() && !firearmController.IsInSpawnOperation() && !firearmController.IsHandsProcessing()) // && player.AIData.BotOwner.WeaponManager.IsWeaponReady &&  player.AIData.BotOwner.WeaponManager.InIdleState()
                    {
                        player.HandsController.FirearmsAnimator.SetPatrol(true);
                    }
                    else
                    {
                        player.HandsController.FirearmsAnimator.SetPatrol(false);
                        if (firearmController.Item.WeapClass != "pistol")
                        {
                            ////low ready//// 
                            if (!isTacBot && !firearmController.IsInReloadOperation() && !player.IsSprintEnabled && !__instance.IsAiming && notShooting && (lastDistance >= 25f || lastDistance == 0f))    // (Time.time - player.AIData.BotOwner.Memory.LastEnemyTimeSeen) > 1f
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, lowReadyTargetQuaternion, __instance.CameraSmoothTime * dt * Plugin.LowReadyRotationMulti.Value);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                                __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * lowReadyTargetPostion;
                            }

                            ////high ready////
                            if (isTacBot && !firearmController.IsInReloadOperation() && !__instance.IsAiming && notShooting && (lastDistance >= 25f || lastDistance == 0f))
                            {
                                player.BodyAnimatorCommon.SetFloat(GClass1647.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2);

                                currentRotation = Quaternion.Lerp(currentRotation, highReadyTargetQuaternion, __instance.CameraSmoothTime * dt * Plugin.HighReadyRotationMulti.Value);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                                __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * highReadyTargetPostion;
                            }
                            else
                            {
                                player.BodyAnimatorCommon.SetFloat(GClass1647.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)firearmController.Item.CalculateCellSize().X);
                            }

                            ///active aim//// 
                            if (isTacBot && (((nvgIsOn || fsIsON) && !player.IsSprintEnabled && !firearmController.IsInReloadOperation() && lastDistance < 25f && lastDistance > 2f && lastDistance != 0f) || (__instance.IsAiming && (nvgIsOn && __instance.CurrentScope.IsOptic || fsIsON))))
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, activeAimTargetQuaternion, __instance.CameraSmoothTime * dt * Plugin.ActiveAimRotationMulti.Value);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                                __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * activeAimTargetPostion;
                            }

                            ///short stock//// 
                            if (isTacBot && !player.IsSprintEnabled && !firearmController.IsInReloadOperation() && lastDistance <= 2f && lastDistance != 0f)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, shortStockTargetQuaternion, __instance.CameraSmoothTime * dt * Plugin.ShortStockRotationMulti.Value);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);
                                __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * shortStockTargetPostion;
                            }
                        }
                        else
                        {
                            if (!isTacBot && !player.IsSprintEnabled && !__instance.IsAiming && notShooting)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, peacefulPistolTargetQuaternion, __instance.CameraSmoothTime * dt * Plugin.PistolRotationSpeedMulti.Value);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                                __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * peacefulPistolTargetPosition;
                            }

                            if (isTacBot && !player.IsSprintEnabled && !__instance.IsAiming && notShooting)
                            {
                                currentRotation = Quaternion.Lerp(currentRotation, tacPistolTargetQuaternion, __instance.CameraSmoothTime * dt * Plugin.PistolRotationSpeedMulti.Value);
                                AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_1").SetValue(__instance, currentRotation);

                                __instance.HandsContainer.HandsPosition.Zero = __instance.PositionZeroSum + pitch * tacPistolTargetPosition;
                            }
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

        private static bool hasResetActiveAim = true;
        private static bool hasResetLowReady = true;
        private static bool hasResetHighReady = true;
        private static bool hasResetShortStock = true;
        private static bool hasResetPistolPos = true;

        private static bool isResettingActiveAim = false;
        private static bool isResettingLowReady = false;
        private static bool isResettingHighReady = false;
        private static bool isResettingShortStock = false;

        private static Quaternion currentRotation = Quaternion.identity;
        private static Quaternion stanceRotation = Quaternion.identity;

        private static float stanceSpeed = 1f;

        [PatchPostfix]
        private static void Postfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, float dt)
        {

            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    if (firearmController.Item.WeapClass == "pistol" && !WeaponProperties.HasShoulderContact && Plugin.EnableAltPistol.Value)
                    {
                        hasResetActiveAim = true;
                        hasResetHighReady = true;
                        hasResetLowReady = true;
                        hasResetShortStock = true;
                        StanceController.DoPistolStances(false, ref __instance, ref stanceRotation, dt, ref hasResetPistolPos, player, Logger, ref stanceSpeed);
                    }
                    else
                    {
                        hasResetPistolPos = true;
                        StanceController.DoRifleStances(Logger, player, firearmController, false, ref __instance, ref stanceRotation, dt, ref isResettingShortStock, ref hasResetShortStock, ref hasResetLowReady, ref hasResetActiveAim, ref hasResetHighReady, ref isResettingHighReady, ref isResettingLowReady, ref isResettingActiveAim, ref stanceSpeed);
                    }

                    float float_9 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_9").GetValue(__instance);
                    float float_13 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_13").GetValue(__instance);
                    float float_14 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_14").GetValue(__instance);
                    float float_21 = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_21").GetValue(__instance);
                    Vector3 vector3_4 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").GetValue(__instance);
                    Vector3 vector3_6 = (Vector3)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_6").GetValue(__instance);
                    Quaternion quaternion_2 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_2").GetValue(__instance);
                    Quaternion quaternion_5 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_5").GetValue(__instance);
                    Quaternion quaternion_6 = (Quaternion)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").GetValue(__instance);
                    bool bool_1 = (bool)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "bool_1").GetValue(__instance);
                    float Single_3 = (float)AccessTools.Property(typeof(EFT.Animations.ProceduralWeaponAnimation), "Single_3").GetValue(__instance);

                    bool allStancesReset = hasResetActiveAim && hasResetLowReady && hasResetHighReady && hasResetShortStock && hasResetPistolPos;
                    bool isInStance = !allStancesReset || (StanceController.IsHighReady || StanceController.IsLowReady || StanceController.IsShortStock || StanceController.IsActiveAiming);

                    Vector3 vector = __instance.HandsContainer.HandsRotation.Get();
                    Vector3 value = __instance.HandsContainer.SwaySpring.Value;
                    vector += float_21 * (bool_1 ? __instance.AimingDisplacementStr : 1f) * new Vector3(value.x, 0f, value.z);
                    vector += value;
                    Vector3 position = __instance._shouldMoveWeaponCloser ? __instance.HandsContainer.RotationCenterWoStock : __instance.HandsContainer.RotationCenter;//
                    Vector3 worldPivot = __instance.HandsContainer.WeaponRootAnim.TransformPoint(position);//
*//*
                    vector3_4 = __instance.HandsContainer.WeaponRootAnim.position;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "vector3_4").SetValue(__instance, __instance.HandsContainer.WeaponRootAnim.position);
                    quaternion_5 = __instance.HandsContainer.WeaponRootAnim.localRotation;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_5").SetValue(__instance, __instance.HandsContainer.WeaponRootAnim.localRotation);
                    quaternion_6 = __instance.HandsContainer.WeaponRootAnim.rotation;
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "quaternion_6").SetValue(__instance, __instance.HandsContainer.WeaponRootAnim.rotation);
*//*
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

                    Logger.LogWarning("is in stance " + isInStance);
                    Logger.LogWarning("is aiming " + (__instance.IsAiming && allStancesReset));
                    Logger.LogWarning("IsFiringFromStance " + StanceController.IsFiringFromStance);

                    __instance.ApplyAimingAlignment(dt);
                    currentRotation = Quaternion.Slerp(currentRotation, __instance.IsAiming && allStancesReset ? quaternion_2 : isInStance && !StanceController.IsFiringFromStance ? stanceRotation : Quaternion.identity, isInStance ? stanceSpeed :  __instance.IsAiming ? __instance.CameraSmoothTime * float_9 * dt : __instance.CameraSmoothTime * dt);
                    Quaternion rhs = Quaternion.Euler(float_14 * Single_3 * vector3_6);
                    __instance.HandsContainer.WeaponRootAnim.SetPositionAndRotation(vector3_4, quaternion_6 * rhs * currentRotation);
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
                WeaponProperties.BaseHipfireAccuracy = player.ProceduralWeaponAnimation.Breath.HipPenalty;
            }
        }
    }
}
*/