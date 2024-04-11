using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using CustomPlayerLoopSystem;
using EFT;
using EFT.Animations;
using EFT.Animations.NewRecoil;
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
using Random = System.Random;

namespace RealismMod
{
    public enum EBracingDirection 
    {
        Top,
        Left, 
        Right,
        None
    }

    public enum EStance
    {
        None,
        LowReady,
        HighReady,
        ShortStock,
        ActiveAiming,
        PatrolStance,
        Melee,
        PistolCompressed
    }

    public static class StanceController
    {
        public static string[] botsToUseTacticalStances = { "sptBear", "sptUsec", "exUsec", "pmcBot", "bossKnight", "followerBigPipe", "followerBirdEye", "bossGluhar", "followerGluharAssault", "followerGluharScout", "followerGluharSecurity", "followerGluharSnipe" };
/*        public static Dictionary<string, bool> LightDictionary = new Dictionary<string, bool>();*/
      
        public static Player.BetterValueBlender StanceBlender = new Player.BetterValueBlender
        {
            Speed = 5f,
            Target = 0f
        };

        public static float currentPistolXPos = 0f;
        public static Vector3 CoverWiggleDirection = Vector3.zero;
        public static Vector3 CoverOffset = Vector3.zero;
        public static Vector3 WeaponOffsetPosition = Vector3.zero;
        public static Vector3 StanceTargetPosition = Vector3.zero;
        private static Vector3 pistolLocalPosition = Vector3.zero;

        private const float clickDelay = 0.2f;
        private static float doubleClickTime = 0f;
        private static bool clickTriggered = true;
        public static int StanceIndex = 0;

        public static bool MeleeIsToggleable = true;
        public static bool CanDoMeleeDetection = false;
        public static bool MeleeHitSomething = false;
        public static bool IsFiringFromStance = false;
        public static float StanceShotTime = 0.0f;
        private static float ManipTime = 0.0f;
        public static float ManipTimer = 0.25f;
        public static float DampingTimer = 0.0f;
        public static float MeleeTimer = 0.0f;
        public static bool DoDampingTimer = false;
        public static bool CanResetDamping = true;

        public static float HighReadyBlackedArmTime = 0.0f;
        public static bool CanDoHighReadyInjuredAnim = false;

        public static bool HaveSetAiming = false;
        public static bool HaveSetActiveAim = false;

        public static float HighReadyManipBuff = 1f;
        public static float ActiveAimManipBuff = 1f;
        public static float LowReadyManipBuff = 1f;

        public static bool CancelPistolStance = false;
        public static bool PistolIsColliding = false;
        public static bool CancelHighReady = false;
        public static bool ModifyHighReady = false;
        public static bool CancelLowReady = false;
        public static bool CancelShortStock = false;
        public static bool CancelActiveAim = false;
        public static bool ShouldResetStances = false;
        public static bool DoMeleeReset = false;

        private static bool setRunAnim = false;
        private static bool haveResetRunAnim = false;

        public static bool HasResetActiveAim = true;
        public static bool HasResetLowReady = true;
        public static bool HasResetHighReady = true;
        public static bool HasResetShortStock = true;
        public static bool HasResetPistolPos = true;
        public static bool HasResetMelee = true;

        public static EStance StoredStance = EStance.None;
        public static EStance CurrentStance = EStance.None;
        public static bool WasActiveAim = false;

        public static EBracingDirection BracingDirection = EBracingDirection.None;
        public static bool IsBracing = false;
        public static bool IsMounting = false;
        public static float BracingSwayBonus = 1f;
        public static float BracingRecoilBonus = 1f;
        public static float MountingBreathReduction = 1f;
        public static float MountingRecoilBonus = 1f;
        public static bool BlockBreathEffect = false;
        public static float DismountTimer = 0.0f;
        public static bool CanDoDismountTimer = false;
        public static bool DidStanceWiggle = false;
        public static float WiggleReturnSpeed = 1f;
        public static bool toggledLight = false;
        public static bool IsInStance = false;
        public static bool CanResetAimDrain = false;
        public static bool IsAiming = false;
        public static bool IsInInventory = false;
        public static bool DidWeaponSwap = false;
        public static bool IsBlindFiring = false;
        public static bool IsInThirdPerson = false;

        private static bool regenStam = false;
        private static bool drainStam = false;
        private static bool neutral = false;
        private static bool wasBracing = false;
        private static bool wasMounting = false;
        private static bool wasAiming = false;
        private static EStance lastRecordedStance = EStance.None;
        public static bool HaveResetStamDrain = false;

        private static Vector3 currentMountedPos = Vector3.zero;
        private static float mountResetTimer = 0f;
        private static float mountBreathTimer = 0f;
        private static bool hasNotResetMounting = false;

        private static float getRestoreRate() 
        {
            float baseRestoreRate = 0f;
            if (CurrentStance == EStance.PatrolStance || IsMounting)
            {
                baseRestoreRate = 4f;
            }
            else if (CurrentStance == EStance.LowReady || CurrentStance == EStance.PistolCompressed || IsBracing)
            {
                baseRestoreRate = 2.4f;
            }
            else if (CurrentStance == EStance.HighReady)
            {
                baseRestoreRate = 1.85f;
            }
            else if (CurrentStance == EStance.ShortStock)
            {
                baseRestoreRate = 1.3f;
            }
            else if (IsIdle() && !Plugin.EnableIdleStamDrain.Value)
            {
                baseRestoreRate = 1f;
            }
            else 
            {
                baseRestoreRate = 4f;
            }
            float formfactor = WeaponStats.IsBullpup ? 1.05f : 1f;
            return (1f - ((WeaponStats.ErgoFactor * formfactor) / 100f)) * baseRestoreRate * PlayerState.ADSInjuryMulti;
        }

        private static float getDrainRate(Player player)
        {
            float baseDrainRate = 0f;
            if (player.Physical.HoldingBreath)
            {
                baseDrainRate = IsMounting ? 0.025f : IsBracing ? 0.05f : 0.3f;
            }
            else if (IsAiming)
            {
                baseDrainRate = 0.15f;
            }
            else if (CurrentStance == EStance.ActiveAiming && Plugin.EnableIdleStamDrain.Value)
            {
                baseDrainRate = 0.05f; 
            }
            else
            {
                baseDrainRate = 0.1f;
            }
            float formfactor = WeaponStats.IsBullpup ? 0.4f : 1f;
            return WeaponStats.ErgoFactor * formfactor * baseDrainRate * ((1f - PlayerState.ADSInjuryMulti) + 1f);
        }

        public static void SetStanceStamina(Player player)
        {
            bool isInRegenableStance = CurrentStance == EStance.HighReady || CurrentStance == EStance.LowReady || CurrentStance == EStance.PatrolStance || CurrentStance == EStance.ShortStock || (IsIdle() && !Plugin.EnableIdleStamDrain.Value);
            bool isInRegenableState = (!player.Physical.HoldingBreath && (IsMounting || IsBracing)) || player.IsInPronePose || CurrentStance == EStance.PistolCompressed || player.IsInventoryOpened;
            bool doRegen = ((isInRegenableStance && !IsAiming && !IsFiringFromStance) || isInRegenableState) && !PlayerState.IsSprinting;
            bool shouldDoIdleDrain = IsIdle() && Plugin.EnableIdleStamDrain.Value;
            bool shouldInterruptRegen = isInRegenableStance && (IsAiming || IsFiringFromStance);
            bool doDrain = (shouldInterruptRegen || !isInRegenableStance || shouldDoIdleDrain) && !isInRegenableState && !PlayerState.IsSprinting;
            bool doNeutral = PlayerState.IsSprinting;
            EStance stance = CurrentStance;

            if (IsAiming != wasAiming || regenStam != doRegen || drainStam != doDrain || neutral != doNeutral || lastRecordedStance != CurrentStance || IsMounting != wasMounting || IsBracing != wasBracing)
            {
                if (doDrain)
                {
                    player.Physical.Aim(1f);
                }
                else if (doRegen)
                {
                    player.Physical.Aim(0f);
                }
                else if (doNeutral)
                {
                    player.Physical.Aim(1f);
                }

                HaveResetStamDrain = false;
            }

            //drain
            if (doDrain)
            {
                player.Physical.HandsStamina.Multiplier = getDrainRate(player);
            }
            //regen
            else if (doRegen)
            {
                player.Physical.HandsStamina.Multiplier = getRestoreRate();
            }
            //no drain or regen
            else if (doNeutral)
            {
                player.Physical.HandsStamina.Multiplier = 0f;
            }

            regenStam = doRegen;
            drainStam = doDrain;
            neutral = doNeutral;
            wasBracing = IsBracing;
            wasMounting = IsMounting;
            wasAiming = IsAiming;
            lastRecordedStance = CurrentStance;
        }

        public static void UnarmedStanceStamina(Player player)
        {
            player.Physical.Aim(0f);
            player.Physical.HandsStamina.Multiplier = 1f;
            HaveResetStamDrain = true;
            regenStam = false;
            drainStam = false;
            neutral = false;
            wasBracing = false;
            wasMounting = false;
            wasAiming = false;
            lastRecordedStance = EStance.None;
        }

        public static bool IsIdle()
        {
            return CurrentStance == EStance.None && StoredStance == EStance.None && HasResetActiveAim && HasResetHighReady && HasResetLowReady && HasResetShortStock && HasResetPistolPos && HasResetMelee ? true : false;
        }

        public static void CancelAllStances()
        {
            CurrentStance = EStance.None;
            StoredStance = EStance.None;
            DidStanceWiggle = false;
            WasActiveAim = false;
        }

        private static void stanceManipCancelTimer()
        {
            ManipTime += Time.deltaTime;

            if (ManipTime >= ManipTimer)
            {
                CancelHighReady = false;
                ModifyHighReady = false;
                CancelLowReady = false;
                CancelShortStock = false;
                CancelPistolStance = false;
                CancelActiveAim = false;
                ShouldResetStances = false;
                ManipTimer = 0.25f;
                ManipTime = 0f;
            }
        }

        private static void stanceDampingTimer()
        {
            DampingTimer += Time.deltaTime;

            if (DampingTimer >= 0.01f) //0.05f
            {
                CanResetDamping = true;
                DoDampingTimer = false;
                DampingTimer = 0f;
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

        private static void meleeCooldownTimer()
        {
            MeleeTimer += Time.deltaTime;

            if (MeleeTimer >= 0.25f)
            {
                DoMeleeReset = false;
                MeleeIsToggleable = true;
                MeleeTimer = 0f;
            }
        }

        private static void doMeleeEffect()
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            Player.FirearmController fc = player.HandsController as Player.FirearmController;
            if (WeaponStats.HasBayonet)
            {

                int rndNum = UnityEngine.Random.Range(1, 10);
                string track = rndNum <= 5 ? "knife_1.wav" : "knife_2.wav";
                Singleton<BetterAudio>.Instance.PlayAtPoint(player.ProceduralWeaponAnimation.HandsContainer.WeaponRootAnim.position, Plugin.LoadedAudioClips[track], 2, BetterAudio.AudioSourceGroupType.Distant, 100, 2, EOcclusionTest.Continuous);
            }
            player.Physical.ConsumeAsMelee(2f + (WeaponStats.ErgoFactor / 100f));
        }

        private static void toggleStance(EStance targetStance, bool setPrevious = false, bool setPrevisousAsCurrent = false) 
        {
            if (setPrevious) StoredStance = CurrentStance;
            if (CurrentStance == targetStance) CurrentStance = EStance.None;
            else CurrentStance = targetStance;
            if (setPrevisousAsCurrent) StoredStance = CurrentStance;
        }

        public static void StanceState()
        {
            if (Utils.WeaponReady)
            {
                if (DoDampingTimer)
                {
                    stanceDampingTimer();
                }

                if (DoMeleeReset)
                {
                    meleeCooldownTimer();
                }

                //patrol
                if (MeleeIsToggleable && Input.GetKeyDown(Plugin.PatrolKeybind.Value.MainKey))
                {
                    toggleStance(EStance.PatrolStance);
                    StoredStance = EStance.None;
                    StanceBlender.Target = 0f;
                    DidStanceWiggle = false;
                }

                if (!PlayerState.IsSprinting && !IsInInventory && !WeaponStats.IsStocklessPistol)
                {
                    //cycle stances
                    if (MeleeIsToggleable && Input.GetKeyUp(Plugin.CycleStancesKeybind.Value.MainKey))
                    {
                        if (Time.time <= doubleClickTime)
                        {
                            clickTriggered = true;
                            StanceBlender.Target = 0f;
                            StanceIndex = 0;
                            CancelAllStances();
                            DidStanceWiggle = false;
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
                            StanceBlender.Target = 1f;
                            clickTriggered = true;
                            StanceIndex++;
                            StanceIndex = StanceIndex > 3 ? 1 : StanceIndex;
                            CurrentStance = (EStance)StanceIndex;
                            StoredStance = CurrentStance;
                            DidStanceWiggle = false;
                            if (CurrentStance == EStance.HighReady && (Plugin.RealHealthController.ArmsAreIncapacitated || Plugin.RealHealthController.HasOverdosed))
                            {
                                CanDoHighReadyInjuredAnim = true;
                            }
                        }
                    }

                    //active aim
                    if (!Plugin.ToggleActiveAim.Value)
                    {
                        if (MeleeIsToggleable && Input.GetKey(Plugin.ActiveAimKeybind.Value.MainKey) || (Input.GetKey(KeyCode.Mouse1) && !PlayerState.IsAllowedADS))
                        {
                            if (!HaveSetActiveAim)
                            {
                                DidStanceWiggle = false;
                            }

                            StanceBlender.Target = 1f;
                            CurrentStance = EStance.ActiveAiming;
                            WasActiveAim = true;
                            HaveSetActiveAim = true;
                        }
                        else if (HaveSetActiveAim)
                        {
                            StanceBlender.Target = 0f;
                            CurrentStance = StoredStance;
                            WasActiveAim = false;
                            HaveSetActiveAim = false;
                            DidStanceWiggle = false;
                        }
                    }
                    else
                    {
                        if (MeleeIsToggleable && Input.GetKeyDown(Plugin.ActiveAimKeybind.Value.MainKey) || (Input.GetKeyDown(KeyCode.Mouse1) && !PlayerState.IsAllowedADS))
                        {
                            StanceBlender.Target = StanceBlender.Target == 0f ? 1f : 0f;
                            toggleStance(EStance.ActiveAiming);
                            WasActiveAim = CurrentStance == EStance.ActiveAiming ? true : false;
                            DidStanceWiggle = false;
                            if (CurrentStance != EStance.ActiveAiming)
                            {
                                CurrentStance = StoredStance;
                            }
                        }
                    }


                    //Melee
                    if (!IsAiming && MeleeIsToggleable && Input.GetKeyDown(Plugin.MeleeKeybind.Value.MainKey))
                    {
                        CurrentStance = EStance.Melee;
                        StoredStance = EStance.None;
                        WasActiveAim = false;
                        DidStanceWiggle = false;
                        StanceBlender.Target = 1f;
                        MeleeIsToggleable = false;
                        MeleeHitSomething = false;
                    }

                    //short-stock
                    if (MeleeIsToggleable && Input.GetKeyDown(Plugin.ShortStockKeybind.Value.MainKey))
                    {
                        StanceBlender.Target = StanceBlender.Target == 0f ? 1f : 0f;
                        toggleStance(EStance.ShortStock, false, true);
                        WasActiveAim = false;
                        DidStanceWiggle = false;
                    }

                    //high ready
                    if (MeleeIsToggleable && Input.GetKeyDown(Plugin.HighReadyKeybind.Value.MainKey))
                    {
                        StanceBlender.Target = StanceBlender.Target == 0f ? 1f : 0f;
                        toggleStance(EStance.HighReady, false, true);
                        WasActiveAim = false;
                        DidStanceWiggle = false;

                        if (CurrentStance == EStance.HighReady && (Plugin.RealHealthController.ArmsAreIncapacitated || Plugin.RealHealthController.HasOverdosed))
                        {
                            CanDoHighReadyInjuredAnim = true;
                        }
                    }

                    //low ready
                    if (MeleeIsToggleable && Input.GetKeyDown(Plugin.LowReadyKeybind.Value.MainKey))
                    {
                        StanceBlender.Target = StanceBlender.Target == 0f ? 1f : 0f;
                        toggleStance(EStance.LowReady, false, true);
                        WasActiveAim = false;
                        DidStanceWiggle = false;
                    }

                    //cancel if aiming
                    if (IsAiming)
                    {
                        if (CurrentStance == EStance.ActiveAiming || WasActiveAim)
                        {
                            StoredStance = EStance.None;
                        }
                        CurrentStance = EStance.None;
                        HaveSetAiming = true;
                    }
                    else if (HaveSetAiming) 
                    {
                        CurrentStance = WasActiveAim ? EStance.ActiveAiming : StoredStance;
                        HaveSetAiming = false;
                    }
                }


                if (IsFiringFromStance)
                {
                    bool cancelCurrentStance = 
                        CurrentStance == EStance.HighReady || 
                        CurrentStance == EStance.LowReady ||
                        CurrentStance == EStance.PatrolStance;
/*                   bool cancelStoredStance = 
                        StoredStance == EStance.HighReady || 
                        (StoredStance == EStance.LowReady && !Plugin.RealHealthController.ArmsAreIncapacitated && !Plugin.RealHealthController.HasOverdosed) ||
                        StoredStance == EStance.PatrolStance;*/
                    if (cancelCurrentStance) 
                    {
                        CurrentStance = EStance.None;
                        StoredStance = EStance.None;
                        StanceBlender.Target = 0f;
                    }
                }

                if (CanDoHighReadyInjuredAnim)
                {
                    HighReadyBlackedArmTime += Time.deltaTime;
                    if (HighReadyBlackedArmTime >= 0.5f)
                    {
                        CanDoHighReadyInjuredAnim = false;
                        CurrentStance = EStance.LowReady;
                        StoredStance = EStance.LowReady;
                        HighReadyBlackedArmTime = 0f;
                    }
                }

                if ((Plugin.RealHealthController.ArmsAreIncapacitated || Plugin.RealHealthController.HasOverdosed) && !IsAiming && !IsFiringFromStance && CurrentStance != EStance.PatrolStance && CurrentStance != EStance.ShortStock && CurrentStance != EStance.ActiveAiming && CurrentStance != EStance.HighReady && MeleeIsToggleable)
                {
                    StanceBlender.Target = 1f;
                    CurrentStance = EStance.LowReady;
                    StoredStance = EStance.LowReady;
                    WasActiveAim = false;
                }
            }

            HighReadyManipBuff = CurrentStance == EStance.HighReady ? 1.2f : 1f;
            ActiveAimManipBuff = CurrentStance == EStance.ActiveAiming && Plugin.ActiveAimReload.Value ? 1.25f : 1f;
            LowReadyManipBuff = CurrentStance == EStance.LowReady ? 1.2f : 1f;

            if (ShouldResetStances)
            {
                stanceManipCancelTimer();
            }

            if (DidWeaponSwap)
            {
                CurrentStance = EStance.None;
                StoredStance = EStance.None;
                StanceTargetPosition = Vector3.zero;
                StanceBlender.Target = 0f;
                StanceIndex = 0;
                WasActiveAim = false;
                DidWeaponSwap = false;
            }
        }

        /*        //doesn't work with multiple lights where one is off and the other is on
                public static void ToggleDevice(Player.FirearmController fc, bool activating)
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
        */
        public static void DoPistolStances(bool isThirdPerson, EFT.Animations.ProceduralWeaponAnimation pwa, ref Quaternion stanceRotation, float dt, ref bool hasResetPistolPos, Player player, ref float rotationSpeed, ref bool isResettingPistol, Player.FirearmController fc)
        {
            float totalPlayerWeight = PlayerState.TotalModifiedWeightMinusWeapon;
            float playerWeightFactor = 1f + (totalPlayerWeight / 100f);
            float ergoMulti = Mathf.Clamp(WeaponStats.ErgoStanceSpeed, 0.65f, 1.45f);
            float stanceMulti = Mathf.Clamp(ergoMulti * PlayerState.StanceInjuryMulti * (Mathf.Max(PlayerState.RemainingArmStamPerc, 0.65f)), 0.5f, 1.45f);
            float balanceFactor = 1f + (WeaponStats.Balance / 100f);
            balanceFactor = WeaponStats.Balance > 0f ? balanceFactor * -1f : balanceFactor;
            float resetErgoMulti = (1f - stanceMulti) + 1f;

            float wiggleErgoMulti = Mathf.Clamp((WeaponStats.ErgoStanceSpeed * 0.25f), 0.1f, 1f);
            WiggleReturnSpeed = (1f - (PlayerState.AimSkillADSBuff * 0.5f)) * wiggleErgoMulti * PlayerState.StanceInjuryMulti * playerWeightFactor * (Mathf.Max(PlayerState.RemainingArmStamPerc, 0.65f));

            float movementFactor = PlayerState.IsMoving ? 1.3f : 1f;

            Quaternion pistolRevertQuaternion = Quaternion.Euler(Plugin.PistolResetRotationX.Value * balanceFactor, Plugin.PistolResetRotationY.Value, Plugin.PistolResetRotationZ.Value);
            Vector3 pistolTargetPosition = new Vector3(Plugin.PistolOffsetX.Value, Plugin.PistolOffsetY.Value, Plugin.PistolOffsetZ.Value);
            Vector3 pistolTargetRotation = new Vector3(Plugin.PistolRotationX.Value, Plugin.PistolRotationY.Value, Plugin.PistolRotationZ.Value);
            Quaternion pistolTargetQuaternion = Quaternion.Euler(pistolTargetRotation);
            Quaternion pistolMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.PistolAdditionalRotationX.Value, Plugin.PistolAdditionalRotationY.Value, Plugin.PistolAdditionalRotationZ.Value));

            //I've no idea wtf is going on here but it sort of works
            if (!WeaponStats.HasShoulderContact && Plugin.EnableAltPistol.Value)
            {
                float targetPosX = 0.09f;
                if (!IsBlindFiring && !pwa.LeftStance) // !CancelPistolStance
                {
                    targetPosX = Plugin.PistolOffsetX.Value;
                }

                currentPistolXPos = Mathf.Lerp(currentPistolXPos, targetPosX, dt * Plugin.PistolPosSpeedMulti.Value * stanceMulti * 0.5f);
                pistolLocalPosition.x = currentPistolXPos;
                pistolLocalPosition.y = pwa.HandsContainer.TrackingTransform.localPosition.y;
                pistolLocalPosition.z = pwa.HandsContainer.TrackingTransform.localPosition.z;  
                pwa.HandsContainer.WeaponRoot.localPosition = pistolLocalPosition;
            }

            if (!pwa.IsAiming && !IsBlindFiring && !pwa.LeftStance && !PistolIsColliding && !WeaponStats.HasShoulderContact && Plugin.EnableAltPistol.Value) //!CancelPistolStance
            {
                CurrentStance = EStance.PistolCompressed;
                StoredStance = EStance.None;
                isResettingPistol = false;
                hasResetPistolPos = false;

                StanceBlender.Speed = Plugin.PistolPosSpeedMulti.Value * stanceMulti;
                StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, pistolTargetPosition, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * dt);

                if (StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolAdditionalRotationSpeedMulti.Value * stanceMulti;
                    stanceRotation = pistolMiniTargetQuaternion;
                }
                else 
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolRotationSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                    stanceRotation = pistolTargetQuaternion;
                }

                if (StanceTargetPosition == pistolTargetPosition && StanceBlender.Value >= 1f && !CanResetDamping)
                {
                    DoDampingTimer = true;
                }
                else if (StanceTargetPosition != pistolTargetPosition || StanceBlender.Value < 1)
                {
                    CanResetDamping = false;
                }

                if (StanceBlender.Value < 0.95f || CancelPistolStance)
                {
                    DidStanceWiggle = false;
                }
                if ((StanceBlender.Value >= 1f && StanceTargetPosition == pistolTargetPosition) && !DidStanceWiggle)
                {
                    DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(-25f, 10f, 0f) * movementFactor);
                    DidStanceWiggle = true;
                    CancelPistolStance = false;
                }

            }
            else if (StanceBlender.Value > 0f && !hasResetPistolPos && !PistolIsColliding)
            {
                CanResetDamping = false;

                isResettingPistol = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolResetRotationSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = pistolRevertQuaternion;
                StanceBlender.Speed = Plugin.PistolPosResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
            }
            else if (StanceBlender.Value == 0f && !hasResetPistolPos && !PistolIsColliding)
            {
                if (!CanResetDamping)
                {
                    DoDampingTimer = true;
                }

                DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(10f, 1f, -30f) * movementFactor);

                isResettingPistol = false;
                CurrentStance = EStance.None;
                stanceRotation = Quaternion.identity;
                hasResetPistolPos = true;
            }
        }

        public static void DoRifleStances(Player player, Player.FirearmController fc, bool isThirdPerson, EFT.Animations.ProceduralWeaponAnimation pwa, ref Quaternion stanceRotation, float dt, ref bool isResettingShortStock, ref bool hasResetShortStock, ref bool hasResetLowReady, ref bool hasResetActiveAim, ref bool hasResetHighReady, ref bool isResettingHighReady, ref bool isResettingLowReady, ref bool isResettingActiveAim, ref float rotationSpeed, ref bool hasResetMelee, ref bool isResettingMelee, ref bool didHalfMeleeAnim)
        {
            float totalPlayerWeight = PlayerState.TotalModifiedWeightMinusWeapon;
            float playerWeightFactor = 1f + (totalPlayerWeight / 150f);
            float ergoMulti = Mathf.Clamp(WeaponStats.ErgoStanceSpeed * 1.15f, 0.55f, 1.2f);
            float stanceMulti = Mathf.Clamp(ergoMulti * PlayerState.StanceInjuryMulti * (Mathf.Max(PlayerState.RemainingArmStamPerc, 0.65f)), 0.45f, 1.25f);
            float resetErgoMulti = (1f - stanceMulti) + 1f;

            float wiggleErgoMulti = Mathf.Clamp((WeaponStats.ErgoStanceSpeed * 0.5f), 0.1f, 1f);
            float stocklessModifier = WeaponStats.HasShoulderContact ? 1f : 0.5f;
            WiggleReturnSpeed = (1f - (PlayerState.AimSkillADSBuff * 0.5f)) * wiggleErgoMulti * PlayerState.StanceInjuryMulti * stocklessModifier * playerWeightFactor * (Mathf.Max(PlayerState.RemainingArmStamPerc, 0.65f));

            bool isColliding = !pwa.OverlappingAllowsBlindfire;
            float collisionRotationFactor = isColliding ? 2f : 1f;
            float collisionPositionFactor = isColliding ? 2f : 1f;

            float thirdPersonMulti = isThirdPerson ? Plugin.ThirdPersonRotationMulti.Value : 1f;

            Quaternion activeAimMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimAdditionalRotationX.Value * resetErgoMulti, Plugin.ActiveAimAdditionalRotationY.Value * resetErgoMulti, Plugin.ActiveAimAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion activeAimRevertQuaternion = IsAiming && WeaponStats.IsCantedSight ? Quaternion.identity : Quaternion.Euler(new Vector3(Plugin.ActiveAimResetRotationX.Value * resetErgoMulti, Plugin.ActiveAimResetRotationY.Value * resetErgoMulti, Plugin.ActiveAimResetRotationZ.Value * resetErgoMulti));
            Vector3 activeAimTargetPosition = new Vector3(Plugin.ActiveAimOffsetX.Value, Plugin.ActiveAimOffsetY.Value, Plugin.ActiveAimOffsetZ.Value);
            Vector3 activeAimTargetPosition2 = new Vector3(Plugin.ActiveAimOffsetX.Value, -0.03f, Plugin.ActiveAimOffsetZ.Value);
            Quaternion activeAimTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimRotationX.Value, Plugin.ActiveAimRotationY.Value, Plugin.ActiveAimRotationZ.Value));

            Quaternion lowReadyTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.LowReadyRotationX.Value * collisionRotationFactor * resetErgoMulti * thirdPersonMulti, Plugin.LowReadyRotationY.Value * thirdPersonMulti, Plugin.LowReadyRotationZ.Value * thirdPersonMulti));
            Quaternion lowReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.LowReadyAdditionalRotationX.Value * resetErgoMulti, Plugin.LowReadyAdditionalRotationY.Value * resetErgoMulti, Plugin.LowReadyAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion lowReadyRevertQuaternion = Quaternion.Euler(Plugin.LowReadyResetRotationX.Value * resetErgoMulti, Plugin.LowReadyResetRotationY.Value * resetErgoMulti, Plugin.LowReadyResetRotationZ.Value * resetErgoMulti);
            Vector3 lowReadyTargetPosition = new Vector3(Plugin.LowReadyOffsetX.Value, Plugin.LowReadyOffsetY.Value, Plugin.LowReadyOffsetZ.Value);

            Vector3 highReadyTargetPosition = new Vector3(Plugin.HighReadyOffsetX.Value, Plugin.HighReadyOffsetY.Value * (ModifyHighReady ? 0.25f : 1f), Plugin.HighReadyOffsetZ.Value);
            Quaternion highReadyTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.HighReadyRotationX.Value * stanceMulti * collisionRotationFactor * thirdPersonMulti, Plugin.HighReadyRotationY.Value * stanceMulti * thirdPersonMulti * (ModifyHighReady ? -1f : 1f), Plugin.HighReadyRotationZ.Value * stanceMulti * thirdPersonMulti));
            Quaternion highReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.HighReadyAdditionalRotationX.Value * resetErgoMulti, Plugin.HighReadyAdditionalRotationY.Value * resetErgoMulti, Plugin.HighReadyAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion highReadyRevertQuaternion = Quaternion.Euler(Plugin.HighReadyResetRotationX.Value * resetErgoMulti, Plugin.HighReadyResetRotationY.Value * resetErgoMulti, Plugin.HighReadyResetRotationZ.Value * resetErgoMulti);

            Quaternion shortStockTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ShortStockRotationX.Value * stanceMulti, Plugin.ShortStockRotationY.Value * stanceMulti, Plugin.ShortStockRotationZ.Value * stanceMulti));
            Quaternion shortStockMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ShortStockAdditionalRotationX.Value * resetErgoMulti, Plugin.ShortStockAdditionalRotationY.Value * resetErgoMulti, Plugin.ShortStockAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion shortStockRevertQuaternion = Quaternion.Euler(Plugin.ShortStockResetRotationX.Value * resetErgoMulti, Plugin.ShortStockResetRotationY.Value * resetErgoMulti, Plugin.ShortStockResetRotationZ.Value * resetErgoMulti);
            Vector3 shortStockTargetPosition = new Vector3(Plugin.ShortStockOffsetX.Value, Plugin.ShortStockOffsetY.Value, Plugin.ShortStockOffsetZ.Value);

            Quaternion meleeTargetQuaternion = Quaternion.Euler(new Vector3(2.5f * resetErgoMulti, -15f * resetErgoMulti, -1f));
            Quaternion meleeTargetQuaternion2 = Quaternion.Euler(new Vector3(-1.5f * resetErgoMulti, -7.5f * resetErgoMulti, -0.5f));
            Vector3 meleeTargetPosition = new Vector3(0f, 0.06f, 0f);
            Vector3 meleeTargetPosition2 = new Vector3(0f, -0.0275f, 0f);

            float movementFactor = PlayerState.IsMoving ? 1.25f : 1f;
            float beltfedFactor = fc.Item.IsBeltMachineGun ? 0.85f : 1f;

            //for setting baseline position
            if (!IsBlindFiring && !pwa.LeftStance)
            {
                pwa.HandsContainer.WeaponRoot.localPosition = WeaponOffsetPosition;
            }

            if (Plugin.EnableTacSprint.Value && (CurrentStance == EStance.HighReady || StoredStance == EStance.HighReady) && !Plugin.RealHealthController.ArmsAreIncapacitated && !Plugin.RealHealthController.HasOverdosed && !fc.Weapon.IsBeltMachineGun)
            {
                player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2f);
                if (!setRunAnim)
                {
                    setRunAnim = true;
                    haveResetRunAnim = false;
                }
            }
            else if (Plugin.EnableTacSprint.Value)
            {
                if (!haveResetRunAnim)
                {
                    player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)fc.Item.CalculateCellSize().X);
                    haveResetRunAnim = true;
                    setRunAnim = false;
                }
            }

    /*        if (Plugin.StanceToggleDevice.Value)
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
            }*/

            ////short-stock////
            if (CurrentStance == EStance.ShortStock && !pwa.IsAiming && !CancelShortStock && !IsBlindFiring && !pwa.LeftStance && !PlayerState.IsSprinting)
            {
                float activeToShort = 1f;
                float highToShort = 1f;
                float lowToShort = 1f;
                isResettingShortStock = false;
                hasResetShortStock = false;
                hasResetMelee = true;

                if (StanceTargetPosition != shortStockTargetPosition * thirdPersonMulti)
                {
                    if (!hasResetActiveAim)
                    {
                        activeToShort = 0.65f;
                    }
                    if (!hasResetHighReady)
                    {
                        highToShort = 0.9f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToShort = 0.7f;
                    }
                }
                else
                {
                    hasResetActiveAim = true;
                    hasResetHighReady = true;
                    hasResetLowReady = true;
                }

                if (StanceTargetPosition == shortStockTargetPosition * thirdPersonMulti && StanceBlender.Value >= 1f && !CanResetDamping)
                {
                    DoDampingTimer = true;
                }
                else if (StanceTargetPosition != shortStockTargetPosition * thirdPersonMulti || StanceBlender.Value < 1)
                {
                    CanResetDamping = false;
                }

                float transitionPositionFactor = activeToShort * highToShort * lowToShort;
                float transitionRotationFactor = activeToShort * highToShort * lowToShort * (transitionPositionFactor != 1f ? 0.9f : 1f);

                if (StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionRotationFactor;
                    stanceRotation = shortStockMiniTargetQuaternion;
                }
                else 
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionRotationFactor;
                    stanceRotation = shortStockTargetQuaternion;
                }

                StanceBlender.Speed = Plugin.ShortStockSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
                StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, shortStockTargetPosition * thirdPersonMulti, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * transitionPositionFactor * dt);

                if ((StanceBlender.Value >= 1f || StanceTargetPosition == shortStockTargetPosition * thirdPersonMulti) && !DidStanceWiggle)
                {
                    DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(10f, -10f, 50f) * movementFactor, true);
                    DidStanceWiggle = true;
                }
            }
            else if (StanceBlender.Value > 0f && !hasResetShortStock && CurrentStance != EStance.LowReady && CurrentStance != EStance.ActiveAiming && CurrentStance != EStance.HighReady && !isResettingActiveAim && !isResettingHighReady && !isResettingLowReady && !isResettingMelee)
            {
                CanResetDamping = false;
                isResettingShortStock = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockResetRotationSpeedMulti.Value;
                stanceRotation = shortStockRevertQuaternion;
                StanceBlender.Speed = Plugin.ShortStockResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
            }
            else if (StanceBlender.Value == 0f && !hasResetShortStock)
            {
                if (!CanResetDamping)
                {
                    DoDampingTimer = true;
                }

                DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(10f, -10f, -50f) * movementFactor, true);
                DidStanceWiggle = false;
                stanceRotation = Quaternion.identity;
                isResettingShortStock = false;
                hasResetShortStock = true;
            }

            ////high ready////
            if (CurrentStance == EStance.HighReady && !pwa.IsAiming && !IsFiringFromStance && !CancelHighReady && !IsBlindFiring && !pwa.LeftStance)
            {
                float shortToHighMulti = 1.0f;
                float lowToHighMulti = 1.0f;
                float activeToHighMulti = 1.0f;
                isResettingHighReady = false;
                hasResetHighReady = false;
                hasResetMelee = true;

                if (StanceTargetPosition != highReadyTargetPosition)
                {
                    if (!hasResetShortStock)
                    {
                        shortToHighMulti = 1f;
                    }
                    if (!hasResetActiveAim)
                    {
                        activeToHighMulti = 1f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToHighMulti = 1f;
                    }
                }
                else
                {
                    hasResetActiveAim = true;
                    hasResetLowReady = true;
                    hasResetShortStock = true;
                }

                if (StanceTargetPosition == highReadyTargetPosition && StanceBlender.Value == 1 && !CanResetDamping)
                {
                    DoDampingTimer = true;
                }
                else if (StanceTargetPosition != highReadyTargetPosition || StanceBlender.Value < 1)
                {
                    CanResetDamping = false;
                }

                float transitionPositionFactor = shortToHighMulti * lowToHighMulti * activeToHighMulti;
                float transitionRotationFactor = shortToHighMulti * lowToHighMulti * activeToHighMulti * (transitionPositionFactor != 1f ? 0.9f : 1f);

                if (CanDoHighReadyInjuredAnim)
                {
                    if (StanceBlender.Value < 1f)
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                        stanceRotation = lowReadyTargetQuaternion;
                    }
                    else 
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                        stanceRotation = highReadyMiniTargetQuaternion;
                    }
                }
                else
                {
                    if (StanceBlender.Value < 1f)
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionRotationFactor;
                        stanceRotation = highReadyMiniTargetQuaternion;
                    }
                    else
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionRotationFactor;
                        stanceRotation = highReadyTargetQuaternion;
                    }
                }

                StanceBlender.Speed = Plugin.HighReadySpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
                StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, highReadyTargetPosition, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * transitionPositionFactor * dt);

                if ((StanceBlender.Value >= 1f || StanceTargetPosition == highReadyTargetPosition) && !DidStanceWiggle)
                {
                    DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(11f, 5.5f, 50f) * movementFactor, true);
                    DidStanceWiggle = true;
                }
            }
            else if (StanceBlender.Value > 0f && !hasResetHighReady && CurrentStance != EStance.LowReady && CurrentStance != EStance.ActiveAiming && CurrentStance != EStance.ShortStock && !isResettingActiveAim && !isResettingLowReady && !isResettingShortStock && !isResettingMelee)
            {
                CanResetDamping = false;
                isResettingHighReady = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyResetRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = highReadyRevertQuaternion;
                StanceBlender.Speed = Plugin.HighReadyResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
            }
            else if (StanceBlender.Value == 0f && !hasResetHighReady)
            {
                if (!CanResetDamping)
                {
                    DoDampingTimer = true;
                }

                DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(15, -10, -20) * movementFactor, true); //-10f, -10f, -50f
                DidStanceWiggle = false;
                stanceRotation = Quaternion.identity;
                isResettingHighReady = false;
                hasResetHighReady = true;
            }

            ////low ready////
            if (CurrentStance == EStance.LowReady && !pwa.IsAiming && !IsFiringFromStance && !CancelLowReady && !IsBlindFiring && !pwa.LeftStance)
            {
                float highToLow = 1.0f;
                float shortToLow = 1.0f;
                float activeToLow = 1.0f;
                isResettingLowReady = false;
                hasResetLowReady = false;
                hasResetMelee = true;

                if (StanceTargetPosition != lowReadyTargetPosition)
                {
                    if (!hasResetHighReady)
                    {
                        highToLow = 0.95f;
                    }
                    if (!hasResetShortStock)
                    {
                        shortToLow = 0.8f;
                    }
                    if (!hasResetActiveAim)
                    {
                        activeToLow = 0.85f;
                    }
                }
                else
                {
                    hasResetHighReady = true;
                    hasResetShortStock = true;
                    hasResetActiveAim = true;
                }

                if (StanceTargetPosition == lowReadyTargetPosition && StanceBlender.Value >= 1f && !CanResetDamping)
                {
                    DoDampingTimer = true;
                }
                else if (StanceTargetPosition != lowReadyTargetPosition || StanceBlender.Value < 1)
                {
                    CanResetDamping = false;
                }

                float transitionPositionFactor = highToLow * shortToLow * activeToLow;
                float transitionRotationFactor = highToLow * shortToLow * activeToLow * (transitionPositionFactor != 1f ? 0.9f : 1f);

                if (StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value * 0.8f : 1f) * transitionRotationFactor;
                    stanceRotation = lowReadyMiniTargetQuaternion;
                }
                else 
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value * 0.8f : 1f) * transitionRotationFactor;
                    stanceRotation = lowReadyTargetQuaternion;
                }

                StanceBlender.Speed = Plugin.LowReadySpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value * 0.8f : 1f);
                StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, lowReadyTargetPosition, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * transitionPositionFactor * dt);

                if ((StanceBlender.Value >= 1f || StanceTargetPosition == lowReadyTargetPosition) && !DidStanceWiggle)
                {
                    DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(7f, -7f, -50f) * movementFactor, true);
                    DidStanceWiggle = true;
                }
            }
            else if (StanceBlender.Value > 0f && !hasResetLowReady && CurrentStance != EStance.ActiveAiming && CurrentStance != EStance.HighReady && CurrentStance != EStance.ShortStock && !isResettingActiveAim && !isResettingHighReady && !isResettingShortStock && !isResettingMelee)
            {
                CanResetDamping = false;

                isResettingLowReady = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyResetRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value * 0.8f : 1f);
                stanceRotation = lowReadyRevertQuaternion;

                StanceBlender.Speed = Plugin.LowReadyResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value * 0.8f : 1f);
            }
            else if (StanceBlender.Value == 0f && !hasResetLowReady)
            {
                if (!CanResetDamping)
                {
                    DoDampingTimer = true;
                }

                DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(7f, 4f, 25f) * movementFactor, true);
                DidStanceWiggle = false;
                stanceRotation = Quaternion.identity;
                isResettingLowReady = false;
                hasResetLowReady = true;
            }

            ////active aiming////
            if (CurrentStance == EStance.ActiveAiming && !CancelActiveAim && !IsBlindFiring && !pwa.LeftStance)
            {
                float shortToActive = 1f;
                float highToActive = 1f;
                float lowToActive = 1f;
                isResettingActiveAim = false;
                hasResetActiveAim = false;
                hasResetMelee = true;

                if (StanceTargetPosition != activeAimTargetPosition)
                {
                    if (!hasResetShortStock)
                    {
                        shortToActive = 1f;
                    }
                    if (!hasResetHighReady)
                    {
                        highToActive = 1f;
                    }
                    if (!hasResetLowReady)
                    {
                        lowToActive = 0.75f;
                    }
                }
                else
                {
                    hasResetShortStock = true;
                    hasResetHighReady = true;
                    hasResetLowReady = true;
                }

                if (StanceTargetPosition == activeAimTargetPosition && StanceBlender.Value == 1 && !CanResetDamping)
                {
                    DoDampingTimer = true;
                }
                else if (StanceTargetPosition != activeAimTargetPosition || StanceBlender.Value < 1)
                {
                    CanResetDamping = false;
                }

                float transitionPositionFactor = shortToActive * highToActive * lowToActive;
                float transitionRotationFactor = shortToActive * highToActive * lowToActive * (transitionPositionFactor != 1f ? 0.9f : 1f);

                if (StanceBlender.Value < 1f)
                {
                    StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, activeAimTargetPosition2, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * transitionPositionFactor * dt);
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ActiveAimAdditionalRotationSpeedMulti.Value * beltfedFactor * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionRotationFactor;
                    stanceRotation = activeAimMiniTargetQuaternion;
                }
                else
                {
                   StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, activeAimTargetPosition, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * transitionPositionFactor * dt);
                   rotationSpeed = 4f * stanceMulti * dt * Plugin.ActiveAimRotationMulti.Value * beltfedFactor * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionRotationFactor;
                   stanceRotation = activeAimTargetQuaternion;
                }

                StanceBlender.Speed = Plugin.ActiveAimSpeedMulti.Value * stanceMulti * beltfedFactor * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);

                if ((StanceBlender.Value >= 1f || StanceTargetPosition == activeAimTargetPosition) && !DidStanceWiggle)
                {
                    DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(-20f, -15f, 0f), true);
                    DidStanceWiggle = true;
                }
            }
            else if (StanceBlender.Value > 0f && !hasResetActiveAim && CurrentStance != EStance.LowReady && CurrentStance != EStance.HighReady && CurrentStance != EStance.ShortStock && !isResettingLowReady && !isResettingHighReady && !isResettingShortStock && !isResettingMelee)
            {
                CanResetDamping = false;

                isResettingActiveAim = true;
                rotationSpeed = stanceMulti * dt * Plugin.ActiveAimResetRotationSpeedMulti.Value * beltfedFactor * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = activeAimRevertQuaternion;
                StanceBlender.Speed = Plugin.ActiveAimResetSpeedMulti.Value * stanceMulti * beltfedFactor * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);

            }
            else if (StanceBlender.Value == 0f && !hasResetActiveAim)
            {
                if (!CanResetDamping)
                {
                    DoDampingTimer = true;
                }

                DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(-10f, 5f, -25f) * movementFactor, true);
                DidStanceWiggle = false;

                stanceRotation = Quaternion.identity;

                isResettingActiveAim = false;
                hasResetActiveAim = true;
            }

            ////Melee////
            if (CurrentStance == EStance.Melee && !pwa.IsAiming && !IsBlindFiring && !pwa.LeftStance && !PlayerState.IsSprinting)
            {
                isResettingMelee = false;
                hasResetMelee = false;
                hasResetActiveAim = true;
                hasResetHighReady = true;
                hasResetLowReady = true;
                hasResetShortStock = true;

                if (StanceTargetPosition == meleeTargetPosition2 && StanceBlender.Value >= 1f && !CanResetDamping)
                {
                    DoDampingTimer = true;
                }
                else if (StanceTargetPosition != meleeTargetPosition2 || StanceBlender.Value < 1)
                {
                    CanResetDamping = false;
                }

                rotationSpeed = 10f * Mathf.Clamp(stanceMulti, 0.8f, 1f) * dt * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);

                float initialPosDistance = Vector3.Distance(StanceTargetPosition, meleeTargetPosition);
                float finalPosDistance = Vector3.Distance(StanceTargetPosition, meleeTargetPosition2);

                if (initialPosDistance > 0.001f && !didHalfMeleeAnim) 
                {
                    stanceRotation = meleeTargetQuaternion;
                    StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, meleeTargetPosition, Plugin.StanceTransitionSpeedMulti.Value * Mathf.Clamp(stanceMulti, 0.75f, 1f) * dt * 1.5f * beltfedFactor);
                }
                else
                {
                    didHalfMeleeAnim = true;
                    stanceRotation = meleeTargetQuaternion2;
                    StanceTargetPosition = Vector3.Lerp(StanceTargetPosition, meleeTargetPosition2, Plugin.StanceTransitionSpeedMulti.Value * Mathf.Clamp(stanceMulti, 0.75f, 1f) * dt * 2f * beltfedFactor);
                }

                StanceBlender.Speed = 50f * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);

                if (StanceBlender.Value >= 1f && finalPosDistance <= 0.001f && !DidStanceWiggle)
                {
                    doMeleeEffect();
                    DoWiggleEffects(player, pwa, fc.Weapon, new Vector3(-20f, -10f, -90f) * movementFactor, true, 3f);
                    DidStanceWiggle = true;
                }

                if (StanceBlender.Value >= 0.9f && didHalfMeleeAnim)
                {
                    CanDoMeleeDetection = true;
                }

                if (StanceBlender.Value >= 1f && finalPosDistance <= 0.001f)
                {
                    CurrentStance = StoredStance;
                    StanceBlender.Target = 0f;
                }
            }
            else if (StanceBlender.Value > 0f && !hasResetMelee) //&& !IsLowReady && !IsActiveAiming && !IsHighReady && !IsShortStock && !isResettingActiveAim && !isResettingHighReady && !isResettingLowReady && !isResettingShortStock
            {
                CanDoMeleeDetection = false;
                CanResetDamping = false;
                isResettingMelee = true;
                rotationSpeed = 10f * stanceMulti * dt;
                stanceRotation = Quaternion.identity;
                StanceBlender.Speed = 15f * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
            }
            else if (StanceBlender.Value == 0f && !hasResetMelee)
            {
                DoMeleeReset = true;
                if (!CanResetDamping)
                {
                    DoDampingTimer = true;
                }
                stanceRotation = Quaternion.identity;
                isResettingMelee = false;
                hasResetMelee = true;
                didHalfMeleeAnim = false;
            }

        }

        public static void DoWiggleEffects(Player player, ProceduralWeaponAnimation pwa, Weapon weapon, Vector3 wiggleDirection, bool playSound = false, float volume = 1f, float wiggleFactor = 1f, bool isADS = false)
        {
            if (playSound)
            {
                AccessTools.Method(typeof(Player), "method_46").Invoke(player, new object[] { volume });
            }

            NewRecoilShotEffect newRecoil = pwa.Shootingg.CurrentRecoilEffect as NewRecoilShotEffect;
            if (isADS) 
            {
                newRecoil.HandRotationRecoil.ReturnTrajectoryDumping = 0.3f * wiggleFactor;
                pwa.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.Damping = 0.3f * wiggleFactor;
            }
            player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.RecoilProcessValues[3].IntensityMultiplicator = 0;
            player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.RecoilProcessValues[4].IntensityMultiplicator = 0;
            for (int i = 0; i < pwa.Shootingg.CurrentRecoilEffect.RecoilProcessValues.Length; i++)
            {
                pwa.Shootingg.CurrentRecoilEffect.RecoilProcessValues[i].Process(wiggleDirection);
            }
            player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.RecoilProcessValues[3].IntensityMultiplicator = 0;
            player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.RecoilProcessValues[4].IntensityMultiplicator = 0;
        }


        public static void DoMounting(Player player, ProceduralWeaponAnimation pwa, Player.FirearmController fc, ref Vector3 weaponWorldPos, ref Vector3 mountWeapPosition, float dt, Vector3 referencePos)
        {
            float resetTime = PlayerState.IsMoving ? 0.15f : 0.5f;
            if (IsMounting && PlayerState.IsMoving)
            {
                IsMounting = false;
            }
            if (Input.GetKeyDown(Plugin.MountKeybind.Value.MainKey) && IsBracing && player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire)
            {
                IsMounting = !IsMounting;
                if (IsMounting)
                {
                    mountWeapPosition = weaponWorldPos + CoverOffset; // + CoverDirection
                    DoWiggleEffects(player, pwa, fc.Weapon, IsMounting ? CoverWiggleDirection : CoverWiggleDirection * -1f, true);
                }

                float accuracy = fc.Item.GetTotalCenterOfImpact(false); //forces accuracy to update
                AccessTools.Field(typeof(Player.FirearmController), "float_3").SetValue(fc, accuracy);
            }
            if (Input.GetKeyDown(Plugin.MountKeybind.Value.MainKey) && !IsBracing && IsMounting)
            {
                IsMounting = false;
            }

            if (IsMounting)
            {
                MountingBreathReduction = Mathf.Lerp(MountingBreathReduction, 0f, 0.2f);
                float mountOrientationBonus = BracingDirection == EBracingDirection.Top ? 0.75f : 1f;
                BracingSwayBonus = Mathf.Lerp(BracingSwayBonus, 0.4f * mountOrientationBonus, 0.2f);
                BlockBreathEffect = true;
                hasNotResetMounting = true;
                AccessTools.Field(typeof(TurnAwayEffector), "_turnAwayThreshold").SetValue(pwa.TurnAway, 1f);

                currentMountedPos.x = mountWeapPosition.x;
                currentMountedPos.y = mountWeapPosition.y;
                currentMountedPos.z = weaponWorldPos.z;

                weaponWorldPos = currentMountedPos; //this makes it feel less clamped to cover but allows h recoil + CoverDirection
            }
            else if (hasNotResetMounting && mountResetTimer < resetTime)
            {
                BracingSwayBonus = 0f;
                mountResetTimer += dt;
                currentMountedPos = Vector3.Lerp(currentMountedPos, referencePos, 0.15f);
                weaponWorldPos = currentMountedPos;
            }
            else 
            {
/*                MountingBreathReduction = Mathf.Lerp(MountingBreathReduction, 1f, 0.001f);
*/              hasNotResetMounting = false;
                mountResetTimer = 0f;
                if (BlockBreathEffect)
                {
                    mountBreathTimer += dt;
                    if (mountBreathTimer >= 1.25f)
                    {
                        mountBreathTimer = 0f;
                        BlockBreathEffect = false;
                    }
                }
            }
        }  
    }
}
