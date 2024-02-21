using Aki.Reflection.Patching;
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
        public static Vector2 MouseRotation = Vector2.zero;
        private static Vector3 pistolTargetPosition = new Vector3(Plugin.PistolOffsetX.Value, Plugin.PistolOffsetY.Value, Plugin.PistolOffsetZ.Value);
        private static Vector3 pistolTargetRotation = new Vector3(Plugin.PistolRotationX.Value, Plugin.PistolRotationY.Value, Plugin.PistolRotationZ.Value);
        private static Vector3 pistolLocalPosition = Vector3.zero;
        private static Vector3 activeAimTargetPosition = new Vector3(Plugin.ActiveAimOffsetX.Value, Plugin.ActiveAimOffsetY.Value, Plugin.ActiveAimOffsetZ.Value);
        private static Vector3 lowReadyTargetPosition = new Vector3(Plugin.LowReadyOffsetX.Value, Plugin.LowReadyOffsetY.Value, Plugin.LowReadyOffsetZ.Value);
        private static Vector3 highReadyTargetPosition = new Vector3(Plugin.HighReadyOffsetX.Value, Plugin.HighReadyOffsetY.Value, Plugin.HighReadyOffsetZ.Value);
        private static Vector3 shortStockTargetPosition = new Vector3(Plugin.ShortStockOffsetX.Value, Plugin.ShortStockOffsetY.Value, Plugin.ShortStockOffsetZ.Value);
        private static Quaternion pistolTargetQuaternion = Quaternion.Euler(pistolTargetRotation);
        private static Quaternion pistolMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.PistolAdditionalRotationX.Value, Plugin.PistolAdditionalRotationY.Value, Plugin.PistolAdditionalRotationZ.Value));
        private static Quaternion activeAimTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimRotationX.Value, Plugin.ActiveAimRotationY.Value, Plugin.ActiveAimRotationZ.Value));

        private const float clickDelay = 0.2f;
        private static float doubleClickTime = 0f;
        private static bool clickTriggered = true;
        public static int SelectedStance = 0;

        public static bool IsMeleeAttack = false;
        public static bool IsPatrolStance = false;
        public static bool IsActiveAiming = false;
        public static bool PistolIsCompressed = false;
        public static bool IsHighReady = false;
        public static bool IsLowReady = false;
        public static bool IsShortStock = false;
        public static bool WasHighReady = false;
        public static bool WasLowReady = false;
        public static bool WasShortStock = false;
        public static bool WasActiveAim = false;

        public static bool MeleeIsToggleable = true;
        public static bool CanDoMeleeDetection = false;
        public static bool MeleeHitSomething = false;
        public static bool IsFiringFromStance = false;
        public static float StanceShotTime = 0.0f;
        public static float ManipTime = 0.0f;
        public static float DampingTimer = 0.0f;
        public static float MeleeTimer = 0.0f;
        public static bool DoDampingTimer = false;
        public static bool CanResetDamping = true;

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
        public static bool DoMeleeReset = false;

        private static bool setRunAnim = false;
        private static bool resetRunAnim = false;

        private static bool gotCurrentStam = false;
        private static float currentStam = 100f;

        public static bool HasResetActiveAim = true;
        public static bool HasResetLowReady = true;
        public static bool HasResetHighReady = true;
        public static bool HasResetShortStock = true;
        public static bool HasResetPistolPos = true;
        public static bool HasResetMelee = true;

        public static EBracingDirection BracingDirection = EBracingDirection.None;
        public static float BracingSwayBonus = 1f;
        public static float BracingRecoilBonus = 1f;
        public static float MountingBreathReduction = 1f;
        public static float MountingRecoilBonus = 1f;
        public static bool BlockBreathEffect = false;
        public static bool IsBracing = false;
        public static bool IsMounting = false;
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

        public static void SetStanceStamina(Player player, Player.FirearmController fc)
        {
            if (!PlayerStats.IsSprinting)
            {
                gotCurrentStam = false;
                if (fc.Item.WeapClass != "pistol")
                {
                    bool isActuallyBracing = !IsMounting && IsBracing;
                    bool shooting = StanceController.IsFiringFromStance && (IsHighReady || IsLowReady);
                    bool canDoIdleStamDrain = Plugin.EnableIdleStamDrain.Value && ((!IsAiming && !IsActiveAiming && !IsMounting && !IsBracing && !player.IsInPronePose) || shooting);
                    bool canDoHighRegen = IsHighReady && !StanceController.IsFiringFromStance && !IsAiming;
                    bool canDoShortRegen = IsShortStock && !StanceController.IsFiringFromStance && !IsAiming;
                    bool canDoLowRegen = IsLowReady && !StanceController.IsFiringFromStance && !IsAiming;
                    bool canDoActiveAimDrain = IsActiveAiming && Plugin.EnableIdleStamDrain.Value;
                    bool aiming = IsAiming && CanResetAimDrain;

                    if (isActuallyBracing && !Plugin.EnableIdleStamDrain.Value)
                    {
                        player.Physical.Aim(0f);
                    }
                    else if (aiming)
                    {
                        player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponStats.ErgoFactor * 0.95f * ((1f - PlayerStats.ADSInjuryMulti) + 1f));
                        CanResetAimDrain = false;
                    }
                    else if (canDoIdleStamDrain)
                    {
                        player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponStats.ErgoFactor * 0.9f * ((1f - PlayerStats.ADSInjuryMulti) + 1f));
                    }
                    else if (canDoActiveAimDrain)
                    {
                        player.Physical.Aim(!(player.MovementContext.StationaryWeapon == null) ? 0f : WeaponStats.ErgoFactor * 0.6f * ((1f - PlayerStats.ADSInjuryMulti) + 1f));
                    }
                    else if (CanResetAimDrain)
                    {
                        player.Physical.Aim(0f);
                        CanResetAimDrain = false;
                    }

                    if (IsPatrolStance)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + ((((1f - (WeaponStats.ErgoFactor / 100f)) * 0.04f) * PlayerStats.ADSInjuryMulti)), player.Physical.HandsStamina.TotalCapacity);
                    }
                    else if (canDoHighRegen)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + ((((1f - (WeaponStats.ErgoFactor / 100f)) * 0.01f) * PlayerStats.ADSInjuryMulti)), player.Physical.HandsStamina.TotalCapacity);
                    }
                    else if (IsMounting || canDoLowRegen)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (((1f - (WeaponStats.ErgoFactor / 100f)) * 0.03f) * PlayerStats.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
                    }
                    else if (isActuallyBracing || canDoShortRegen)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (((1f - (WeaponStats.ErgoFactor / 100f)) * 0.02f) * PlayerStats.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
                    }
                }
                else
                {
                    if (!IsAiming)
                    {
                        player.Physical.Aim(0f);
                        player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (((1f - (WeaponStats.ErgoFactor / 100f)) * 0.025f) * PlayerStats.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
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

            if (player.IsInventoryOpened || (player.IsInPronePose && !IsAiming))
            {
                player.Physical.Aim(0f);
                player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (0.04f * PlayerStats.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
            }
        }

        public static void ResetStanceStamina(Player player)
        {
            player.Physical.Aim(0f);
            player.Physical.HandsStamina.Current = Mathf.Min(player.Physical.HandsStamina.Current + (0.04f * PlayerStats.ADSInjuryMulti), player.Physical.HandsStamina.TotalCapacity);
        }

        public static bool IsIdle()
        {
            return !IsPatrolStance && !IsMeleeAttack && !IsActiveAiming && !IsHighReady && !IsLowReady && !IsShortStock && !WasHighReady && !WasLowReady && !WasShortStock && !WasActiveAim && HasResetActiveAim && HasResetHighReady && HasResetLowReady && HasResetShortStock && HasResetPistolPos && HasResetMelee ? true : false;
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
            IsMounting = false;
            DidStanceWiggle = false;
            IsPatrolStance = false;
        }


        private static void stanceManipCancelTimer()
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
                    IsPatrolStance = !IsPatrolStance;
                    StanceBlender.Target = 0f;
                    IsHighReady = false;
                    IsLowReady = false;
                    IsActiveAiming = false;
                    WasActiveAim = IsActiveAiming;
                    WasHighReady = IsHighReady;
                    WasLowReady = IsLowReady;
                    WasShortStock = IsShortStock;
                    DidStanceWiggle = false;

                }

                if (!PlayerStats.IsSprinting && !IsInInventory && WeaponStats._WeapClass != "pistol")
                {
                    //cycle stances
                    if (MeleeIsToggleable && Input.GetKeyUp(Plugin.CycleStancesKeybind.Value.MainKey))
                    {
                        if (Time.time <= doubleClickTime)
                        {
                            StanceBlender.Target = 0f;
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
                            DidStanceWiggle = false;
                            IsPatrolStance = false;
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
                            SelectedStance++;
                            SelectedStance = SelectedStance > 3 ? 1 : SelectedStance;
                            IsHighReady = SelectedStance == 1 ? true : false;
                            IsLowReady = SelectedStance == 2 ? true : false;
                            IsShortStock = SelectedStance == 3 ? true : false;
                            IsActiveAiming = false;
                            WasHighReady = IsHighReady;
                            WasLowReady = IsLowReady;
                            WasShortStock = IsShortStock;
                            DidStanceWiggle = false;

                            if (IsHighReady && (Plugin.RealHealthController.ArmsAreIncapacitated || Plugin.RealHealthController.HasOverdosed))
                            {
                                DoHighReadyInjuredAnim = true;
                            }
                        }
                    }

                    //active aim
                    if (!Plugin.ToggleActiveAim.Value)
                    {
                        if (MeleeIsToggleable && Input.GetKey(Plugin.ActiveAimKeybind.Value.MainKey) || (Input.GetKey(KeyCode.Mouse1) && !PlayerStats.IsAllowedADS))
                        {
                            if (!SetActiveAiming)
                            {
                                DidStanceWiggle = false;
                            }
                            StanceBlender.Target = 1f;
                            IsActiveAiming = true;
                            IsShortStock = false;
                            IsHighReady = false;
                            IsLowReady = false;
                            IsPatrolStance = false;
                            WasActiveAim = IsActiveAiming;
                            SetActiveAiming = true;
                        }
                        else if (SetActiveAiming)
                        {
                            StanceController.StanceBlender.Target = 0f;
                            IsActiveAiming = false;
                            IsHighReady = WasHighReady;
                            IsLowReady = WasLowReady;
                            IsShortStock = WasShortStock;
                            WasActiveAim = IsActiveAiming;
                            SetActiveAiming = false;
                            DidStanceWiggle = false;
                        }
                    }
                    else
                    {
                        if (MeleeIsToggleable && Input.GetKeyDown(Plugin.ActiveAimKeybind.Value.MainKey) || (Input.GetKeyDown(KeyCode.Mouse1) && !PlayerStats.IsAllowedADS))
                        {
                            StanceBlender.Target = StanceBlender.Target == 0f ? 1f : 0f;
                            IsActiveAiming = !IsActiveAiming;
                            IsShortStock = false;
                            IsHighReady = false;
                            IsLowReady = false;
                            IsPatrolStance = false;
                            WasActiveAim = IsActiveAiming;
                            DidStanceWiggle = false;
                            if (IsActiveAiming == false)
                            {
                                IsHighReady = WasHighReady;
                                IsLowReady = WasLowReady;
                                IsShortStock = WasShortStock;
                            }
                        }
                    }


                    //Melee
                    if (MeleeIsToggleable && Input.GetKeyDown(Plugin.MeleeKeybind.Value.MainKey))
                    {
                        IsHighReady = false;
                        IsLowReady = false;
                        IsActiveAiming = false;
                        IsPatrolStance = false;
                        IsShortStock = false;
                        WasActiveAim = IsActiveAiming;
                        WasHighReady = IsHighReady;
                        WasLowReady = IsLowReady;
                        WasShortStock = IsShortStock;
                        DidStanceWiggle = false;
                        StanceBlender.Target = 1f;
                        IsMeleeAttack = true;
                        MeleeIsToggleable = false;
                        MeleeHitSomething = false;
                    }

                    //short-stock
                    if (MeleeIsToggleable && Input.GetKeyDown(Plugin.ShortStockKeybind.Value.MainKey))
                    {
                        StanceBlender.Target = StanceBlender.Target == 0f ? 1f : 0f;
                        IsShortStock = !IsShortStock;
                        IsHighReady = false;
                        IsLowReady = false;
                        IsActiveAiming = false;
                        IsPatrolStance = false;
                        WasActiveAim = IsActiveAiming;
                        WasHighReady = IsHighReady;
                        WasLowReady = IsLowReady;
                        WasShortStock = IsShortStock;
                        DidStanceWiggle = false;
                    }


                    //high ready
                    if (MeleeIsToggleable && Input.GetKeyDown(Plugin.HighReadyKeybind.Value.MainKey))
                    {
                        StanceBlender.Target = StanceBlender.Target == 0f ? 1f : 0f;
                        IsHighReady = !IsHighReady;
                        IsShortStock = false;
                        IsLowReady = false;
                        IsActiveAiming = false;
                        IsPatrolStance = false;
                        WasActiveAim = IsActiveAiming;
                        WasHighReady = IsHighReady;
                        WasLowReady = IsLowReady;
                        WasShortStock = IsShortStock;
                        DidStanceWiggle = false;

                        if (IsHighReady && (Plugin.RealHealthController.ArmsAreIncapacitated || Plugin.RealHealthController.HasOverdosed))
                        {
                            DoHighReadyInjuredAnim = true;
                        }
                    }

                    //low ready
                    if (MeleeIsToggleable && Input.GetKeyDown(Plugin.LowReadyKeybind.Value.MainKey))
                    {
                        StanceController.StanceBlender.Target = StanceController.StanceBlender.Target == 0f ? 1f : 0f;

                        IsLowReady = !IsLowReady;
                        IsHighReady = false;
                        IsActiveAiming = false;
                        IsShortStock = false;
                        IsPatrolStance = false;
                        WasActiveAim = IsActiveAiming;
                        WasHighReady = IsHighReady;
                        WasLowReady = IsLowReady;
                        WasShortStock = IsShortStock;
                        DidStanceWiggle = false;
                    }

                    if (IsAiming)
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
                        IsPatrolStance = false;
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

                    if ((Plugin.RealHealthController.ArmsAreIncapacitated || Plugin.RealHealthController.HasOverdosed) && !IsAiming && !IsShortStock && !IsActiveAiming && !IsHighReady)
                    {
                        StanceBlender.Target = 1f;
                        IsLowReady = true;
                        WasLowReady = true;
                    }
                }

                HighReadyManipBuff = IsHighReady ? 1.2f : 1f;
                ActiveAimManipBuff = IsActiveAiming && Plugin.ActiveAimReload.Value ? 1.25f : 1f;
                LowReadyManipBuff = IsLowReady ? 1.2f : 1f;

                if (DoResetStances)
                {
                    stanceManipCancelTimer();
                }

                if (DidWeaponSwap || WeaponStats._WeapClass == "pistol")
                {
                    if (DidWeaponSwap)
                    {
                        IsPatrolStance = false;
                        PistolIsCompressed = false;
                        StanceTargetPosition = Vector3.zero;
                        StanceBlender.Target = 0f;
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
                    DidWeaponSwap = false;
                }
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
            float totalPlayerWeight = PlayerStats.TotalModifiedWeightMinusWeapon;
            float playerWeightFactor = 1f + (totalPlayerWeight / 100f);
            float ergoMulti = Mathf.Clamp(WeaponStats.ErgoStanceSpeed, 0.65f, 1.45f);
            float stanceMulti = Mathf.Clamp(ergoMulti * PlayerStats.StanceInjuryMulti * (Mathf.Max(PlayerStats.RemainingArmStamPerc, 0.65f)), 0.5f, 1.45f);
            float balanceFactor = 1f + (WeaponStats.Balance / 100f);
            balanceFactor = WeaponStats.Balance > 0f ? balanceFactor * -1f : balanceFactor;
            float resetErgoMulti = (1f - stanceMulti) + 1f;

            float wiggleErgoMulti = Mathf.Clamp((WeaponStats.ErgoStanceSpeed * 0.25f), 0.1f, 1f);
            StanceController.WiggleReturnSpeed = (1f - (PlayerStats.AimSkillADSBuff * 0.5f)) * wiggleErgoMulti * PlayerStats.StanceInjuryMulti * playerWeightFactor * (Mathf.Max(PlayerStats.RemainingArmStamPerc, 0.65f));

            float movementFactor = PlayerStats.IsMoving ? 1.3f : 1f;

             Quaternion pistolRevertQuaternion = Quaternion.Euler(Plugin.PistolResetRotationX.Value * balanceFactor, Plugin.PistolResetRotationY.Value, Plugin.PistolResetRotationZ.Value);

            //I've no idea wtf is going on here but it sort of works
            if (!WeaponStats.HasShoulderContact && Plugin.EnableAltPistol.Value)
            {
                float targetPosX = 0.09f;
                if (!IsBlindFiring && !pwa.LeftStance) // !StanceController.CancelPistolStance
                {
                    targetPosX = Plugin.PistolOffsetX.Value;
                }

                currentPistolXPos = Mathf.Lerp(currentPistolXPos, targetPosX, dt * Plugin.PistolPosSpeedMulti.Value * stanceMulti * 0.5f);
                pistolLocalPosition.x = currentPistolXPos;
                pistolLocalPosition.y = pwa.HandsContainer.TrackingTransform.localPosition.y;
                pistolLocalPosition.z = pwa.HandsContainer.TrackingTransform.localPosition.z;
                pwa.HandsContainer.WeaponRoot.localPosition = pistolLocalPosition;
            }

            if (!pwa.IsAiming && !IsBlindFiring && !pwa.LeftStance && !StanceController.PistolIsColliding && !WeaponStats.HasShoulderContact && Plugin.EnableAltPistol.Value) //!StanceController.CancelPistolStance
            {
                pwa.Breath.HipPenalty = WeaponStats.BaseHipfireInaccuracy * PlayerStats.SprintHipfirePenalty;

                StanceController.PistolIsCompressed = true;
                isResettingPistol = false;
                hasResetPistolPos = false;

                StanceController.StanceBlender.Speed = Plugin.PistolPosSpeedMulti.Value * stanceMulti;
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, pistolTargetPosition, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * dt);

                if (StanceController.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolAdditionalRotationSpeedMulti.Value * stanceMulti;
                    stanceRotation = pistolMiniTargetQuaternion;
                }
                else 
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolRotationSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                    stanceRotation = pistolTargetQuaternion;
                }

                if (StanceController.StanceTargetPosition == pistolTargetPosition && StanceController.StanceBlender.Value >= 1f && !StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }
                else if (StanceController.StanceTargetPosition != pistolTargetPosition || StanceController.StanceBlender.Value < 1)
                {
                    StanceController.CanResetDamping = false;
                }

                if (StanceController.StanceBlender.Value < 0.95f)
                {
                    DidStanceWiggle = false;
                }
                if ((StanceController.StanceBlender.Value >= 1f && StanceController.StanceTargetPosition == pistolTargetPosition) && !StanceController.DidStanceWiggle)
                {
                    StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(-20f, 1f, 30f) * movementFactor);
                    StanceController.DidStanceWiggle = true;
                }

            }
            else if (StanceController.StanceBlender.Value > 0f && !hasResetPistolPos && !StanceController.PistolIsColliding)
            {
                StanceController.CanResetDamping = false;

                isResettingPistol = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.PistolResetRotationSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = pistolRevertQuaternion;
                StanceController.StanceBlender.Speed = Plugin.PistolPosResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
            }
            else if (StanceController.StanceBlender.Value == 0f && !hasResetPistolPos && !StanceController.PistolIsColliding)
            {
                if (!StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }

                StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(10f, 1f, -30f) * movementFactor);

                isResettingPistol = false;
                StanceController.PistolIsCompressed = false;
                stanceRotation = Quaternion.identity;
                hasResetPistolPos = true;
            }
        }

        public static void DoRifleStances(Player player, Player.FirearmController fc, bool isThirdPerson, EFT.Animations.ProceduralWeaponAnimation pwa, ref Quaternion stanceRotation, float dt, ref bool isResettingShortStock, ref bool hasResetShortStock, ref bool hasResetLowReady, ref bool hasResetActiveAim, ref bool hasResetHighReady, ref bool isResettingHighReady, ref bool isResettingLowReady, ref bool isResettingActiveAim, ref float rotationSpeed, ref bool hasResetMelee, ref bool isResettingMelee)
        {
            float totalPlayerWeight = PlayerStats.TotalModifiedWeightMinusWeapon;
            float playerWeightFactor = 1f + (totalPlayerWeight / 150f);
            float ergoMulti = Mathf.Clamp(WeaponStats.ErgoStanceSpeed * 1.15f, 0.55f, 1.2f);
            float stanceMulti = Mathf.Max(ergoMulti * PlayerStats.StanceInjuryMulti * (Mathf.Max(PlayerStats.RemainingArmStamPerc, 0.65f)), 0.35f);
            float resetErgoMulti = (1f - stanceMulti) + 1f;

            float wiggleErgoMulti = Mathf.Clamp((WeaponStats.ErgoStanceSpeed * 0.5f), 0.1f, 1f);
            float stocklessModifier = WeaponStats.HasShoulderContact ? 1f : 0.5f;
            StanceController.WiggleReturnSpeed = (1f - (PlayerStats.AimSkillADSBuff * 0.5f)) * wiggleErgoMulti * PlayerStats.StanceInjuryMulti * stocklessModifier * playerWeightFactor * (Mathf.Max(PlayerStats.RemainingArmStamPerc, 0.65f));

            bool isColliding = !pwa.OverlappingAllowsBlindfire;
            float collisionRotationFactor = isColliding ? 2f : 1f;
            float collisionPositionFactor = isColliding ? 2f : 1f;

            float thirdPersonMulti = isThirdPerson ? Plugin.ThirdPersonRotationMulti.Value : 1f;
           
            Quaternion activeAimMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimAdditionalRotationX.Value * resetErgoMulti, Plugin.ActiveAimAdditionalRotationY.Value * resetErgoMulti, Plugin.ActiveAimAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion activeAimRevertQuaternion = Quaternion.Euler(new Vector3(Plugin.ActiveAimResetRotationX.Value * resetErgoMulti, Plugin.ActiveAimResetRotationY.Value * resetErgoMulti, Plugin.ActiveAimResetRotationZ.Value * resetErgoMulti));
          
            Quaternion lowReadyTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.LowReadyRotationX.Value * collisionRotationFactor * resetErgoMulti * thirdPersonMulti, Plugin.LowReadyRotationY.Value * thirdPersonMulti, Plugin.LowReadyRotationZ.Value * thirdPersonMulti));
            Quaternion lowReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.LowReadyAdditionalRotationX.Value * resetErgoMulti, Plugin.LowReadyAdditionalRotationY.Value * resetErgoMulti, Plugin.LowReadyAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion lowReadyRevertQuaternion = Quaternion.Euler(Plugin.LowReadyResetRotationX.Value * resetErgoMulti, Plugin.LowReadyResetRotationY.Value * resetErgoMulti, Plugin.LowReadyResetRotationZ.Value * resetErgoMulti);

            Quaternion highReadyTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.HighReadyRotationX.Value * stanceMulti * collisionRotationFactor * thirdPersonMulti, Plugin.HighReadyRotationY.Value * stanceMulti * thirdPersonMulti, Plugin.HighReadyRotationZ.Value * stanceMulti * thirdPersonMulti));
            Quaternion highReadyMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.HighReadyAdditionalRotationX.Value * resetErgoMulti, Plugin.HighReadyAdditionalRotationY.Value * resetErgoMulti, Plugin.HighReadyAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion highReadyRevertQuaternion = Quaternion.Euler(Plugin.HighReadyResetRotationX.Value * resetErgoMulti, Plugin.HighReadyResetRotationY.Value * resetErgoMulti, Plugin.HighReadyResetRotationZ.Value * resetErgoMulti);

            Quaternion shortStockTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ShortStockRotationX.Value * stanceMulti, Plugin.ShortStockRotationY.Value * stanceMulti, Plugin.ShortStockRotationZ.Value * stanceMulti));
            Quaternion shortStockMiniTargetQuaternion = Quaternion.Euler(new Vector3(Plugin.ShortStockAdditionalRotationX.Value * resetErgoMulti, Plugin.ShortStockAdditionalRotationY.Value * resetErgoMulti, Plugin.ShortStockAdditionalRotationZ.Value * resetErgoMulti));
            Quaternion shortStockRevertQuaternion = Quaternion.Euler(Plugin.ShortStockResetRotationX.Value * resetErgoMulti, Plugin.ShortStockResetRotationY.Value * resetErgoMulti, Plugin.ShortStockResetRotationZ.Value * resetErgoMulti);
           
            Vector3 meleeTargetPosition = new Vector3(0f, -0.0275f, 0f); //new Vector3(0.02f, 0.08f, -0.07f);
            Quaternion meleeTargetQuaternion = Quaternion.Euler(new Vector3(-1.5f * resetErgoMulti, -5f * resetErgoMulti, -0.5f)); //-1f * resetErgoMulti, -5f * resetErgoMulti, -1f)

            float movementFactor = PlayerStats.IsMoving ? 1.25f : 1f;
            float beltfedFactor = fc.Item.IsBeltMachineGun ? 0.9f : 1f;

            //for setting baseline position
            if (!IsBlindFiring && !pwa.LeftStance)
            {
                pwa.HandsContainer.WeaponRoot.localPosition = WeaponOffsetPosition;
            }

            if (Plugin.EnableTacSprint.Value && (StanceController.IsHighReady || StanceController.WasHighReady) && !Plugin.RealHealthController.ArmsAreIncapacitated && !Plugin.RealHealthController.HasOverdosed)
            {
                player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, 2f);
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
                    player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)fc.Item.CalculateCellSize().X);
                    resetRunAnim = true;
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
            if (StanceController.IsShortStock && !StanceController.IsMeleeAttack && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsLowReady && !pwa.IsAiming && !StanceController.CancelShortStock && !IsBlindFiring && !pwa.LeftStance && !PlayerStats.IsSprinting)
            {
                float activeToShort = 1f;
                float highToShort = 1f;
                float lowToShort = 1f;
                isResettingShortStock = false;
                hasResetShortStock = false;
                hasResetMelee = true;

                if (StanceController.StanceTargetPosition != shortStockTargetPosition * thirdPersonMulti)
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

                if (StanceController.StanceTargetPosition == shortStockTargetPosition * thirdPersonMulti && StanceController.StanceBlender.Value >= 1f && !StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }
                else if (StanceController.StanceTargetPosition != shortStockTargetPosition * thirdPersonMulti || StanceController.StanceBlender.Value < 1)
                {
                    StanceController.CanResetDamping = false;
                }

                float transitionSpeedFactor = activeToShort * highToShort * lowToShort;

                if (StanceController.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                    stanceRotation = shortStockMiniTargetQuaternion;
                }
                else 
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                    stanceRotation = shortStockTargetQuaternion;
                }

                StanceController.StanceBlender.Speed = Plugin.ShortStockSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, shortStockTargetPosition * thirdPersonMulti, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * transitionSpeedFactor * dt);

                if ((StanceController.StanceBlender.Value >= 1f || StanceController.StanceTargetPosition == shortStockTargetPosition * thirdPersonMulti) && !StanceController.DidStanceWiggle)
                {
                    StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(10f, -10f, 50f) * movementFactor, true);
                    StanceController.DidStanceWiggle = true;
                }
            }
            else if (StanceController.StanceBlender.Value > 0f && !hasResetShortStock && !StanceController.IsLowReady && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !isResettingActiveAim && !isResettingHighReady && !isResettingLowReady && !isResettingMelee)
            {
                StanceController.CanResetDamping = false;
                isResettingShortStock = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.ShortStockResetRotationSpeedMulti.Value;
                stanceRotation = shortStockRevertQuaternion;
                StanceController.StanceBlender.Speed = Plugin.ShortStockResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
            }
            else if (StanceController.StanceBlender.Value == 0f && !hasResetShortStock)
            {
                if (!StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }

                StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(10f, -10f, -50f) * movementFactor, true);
                stanceRotation = Quaternion.identity;
                isResettingShortStock = false;
                hasResetShortStock = true;
            }

            ////high ready////
            if (StanceController.IsHighReady && !StanceController.IsMeleeAttack && !StanceController.IsActiveAiming && !StanceController.IsLowReady && !StanceController.IsShortStock && !pwa.IsAiming && !StanceController.IsFiringFromStance && !StanceController.CancelHighReady && !IsBlindFiring && !pwa.LeftStance)
            {
                float shortToHighMulti = 1.0f;
                float lowToHighMulti = 1.0f;
                float activeToHighMulti = 1.0f;
                isResettingHighReady = false;
                hasResetHighReady = false;
                hasResetMelee = true;

                if (StanceController.StanceTargetPosition != highReadyTargetPosition)
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
                        lowToHighMulti = 1.15f;
                    }
                }
                else
                {
                    hasResetActiveAim = true;
                    hasResetLowReady = true;
                    hasResetShortStock = true;
                }

                if (StanceController.StanceTargetPosition == highReadyTargetPosition && StanceController.StanceBlender.Value == 1 && !StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }
                else if (StanceController.StanceTargetPosition != highReadyTargetPosition || StanceController.StanceBlender.Value < 1)
                {
                    StanceController.CanResetDamping = false;
                }

                float transitionSpeedFactor = shortToHighMulti * lowToHighMulti * activeToHighMulti;

                if (StanceController.DoHighReadyInjuredAnim)
                {
                    if (StanceController.StanceBlender.Value < 1f)
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
                    if (StanceController.StanceBlender.Value < 1f)
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                        stanceRotation = highReadyMiniTargetQuaternion;
                    }
                    else 
                    {
                        rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                        stanceRotation = highReadyTargetQuaternion;
                    }
                }

                StanceController.StanceBlender.Speed = Plugin.HighReadySpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, highReadyTargetPosition, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * transitionSpeedFactor * dt);

                if ((StanceController.StanceBlender.Value >= 1f || StanceController.StanceTargetPosition == highReadyTargetPosition) && !StanceController.DidStanceWiggle)
                {
                    StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(10f, 5f, 50f) * movementFactor, true);
                    StanceController.DidStanceWiggle = true;
                }
            }
            else if (StanceController.StanceBlender.Value > 0f && !hasResetHighReady && !StanceController.IsLowReady && !StanceController.IsActiveAiming && !StanceController.IsShortStock && !isResettingActiveAim && !isResettingLowReady && !isResettingShortStock && !isResettingMelee)
            {
                StanceController.CanResetDamping = false;
                isResettingHighReady = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.HighReadyResetRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = highReadyRevertQuaternion;

                StanceController.StanceBlender.Speed = Plugin.HighReadyResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
            }
            else if (StanceController.StanceBlender.Value == 0f && !hasResetHighReady)
            {
                if (!StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }

                StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(-10f, -10f, -50f) * movementFactor, true);
                StanceController.DidStanceWiggle = false;

                stanceRotation = Quaternion.identity;

                isResettingHighReady = false;
                hasResetHighReady = true;
            }

            ////low ready////
            if (StanceController.IsLowReady && !StanceController.IsMeleeAttack && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsShortStock && !pwa.IsAiming && !StanceController.IsFiringFromStance && !StanceController.CancelLowReady && !IsBlindFiring && !pwa.LeftStance)
            {
                float highToLow = 1.0f;
                float shortToLow = 1.0f;
                float activeToLow = 1.0f;
                isResettingLowReady = false;
                hasResetLowReady = false;
                hasResetMelee = true;

                if (StanceController.StanceTargetPosition != lowReadyTargetPosition)
                {
                    if (!hasResetHighReady)
                    {
                        highToLow = 1.1f;
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

                if (StanceController.StanceTargetPosition == lowReadyTargetPosition && StanceController.StanceBlender.Value >= 1f && !StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }
                else if (StanceController.StanceTargetPosition != lowReadyTargetPosition || StanceController.StanceBlender.Value < 1)
                {
                    StanceController.CanResetDamping = false;
                }

                float transitionSpeedFactor = highToLow * shortToLow * activeToLow;

                if (StanceController.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyAdditionalRotationSpeedMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value * 0.8f : 1f) * transitionSpeedFactor;
                    stanceRotation = lowReadyMiniTargetQuaternion;
                }
                else 
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value * 0.8f : 1f) * transitionSpeedFactor;
                    stanceRotation = lowReadyTargetQuaternion;
                }

                StanceController.StanceBlender.Speed = Plugin.LowReadySpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value * 0.8f : 1f);
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, lowReadyTargetPosition, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * transitionSpeedFactor * dt);

                if ((StanceController.StanceBlender.Value >= 1f || StanceController.StanceTargetPosition == lowReadyTargetPosition) && !StanceController.DidStanceWiggle)
                {
                    StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(5f, -5f, -50f) * movementFactor, true);
                    StanceController.DidStanceWiggle = true;
                }
            }
            else if (StanceController.StanceBlender.Value > 0f && !hasResetLowReady && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsShortStock && !isResettingActiveAim && !isResettingHighReady && !isResettingShortStock && !isResettingMelee)
            {
                StanceController.CanResetDamping = false;

                isResettingLowReady = true;
                rotationSpeed = 4f * stanceMulti * dt * Plugin.LowReadyResetRotationMulti.Value * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value * 0.8f : 1f);
                stanceRotation = lowReadyRevertQuaternion;

                StanceController.StanceBlender.Speed = Plugin.LowReadyResetSpeedMulti.Value * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value * 0.8f : 1f);
            }
            else if (StanceController.StanceBlender.Value == 0f && !hasResetLowReady)
            {
                if (!StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }

                StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(7f, 4f, 25f) * movementFactor, true);
                StanceController.DidStanceWiggle = false;
                stanceRotation = Quaternion.identity;
                isResettingLowReady = false;
                hasResetLowReady = true;
            }

            ////active aiming////
            if (StanceController.IsActiveAiming && !StanceController.IsMeleeAttack && !pwa.IsAiming && !StanceController.IsLowReady && !StanceController.IsShortStock && !StanceController.IsHighReady && !StanceController.CancelActiveAim && !IsBlindFiring && !pwa.LeftStance)
            {
                float shortToActive = 1f;
                float highToActive = 1f;
                float lowToActive = 1f;
                isResettingActiveAim = false;
                hasResetActiveAim = false;
                hasResetMelee = true;

                if (StanceController.StanceTargetPosition != activeAimTargetPosition)
                {
                    if (!hasResetShortStock)
                    {
                        shortToActive = 1f;
                    }
                    if (!hasResetHighReady)
                    {
                        highToActive = 0.7f;
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

                if (StanceController.StanceTargetPosition == activeAimTargetPosition && StanceController.StanceBlender.Value == 1 && !StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }
                else if (StanceController.StanceTargetPosition != activeAimTargetPosition || StanceController.StanceBlender.Value < 1)
                {
                    StanceController.CanResetDamping = false;
                }

                float transitionSpeedFactor = shortToActive * highToActive * lowToActive;

 
                if (StanceController.StanceBlender.Value < 1f)
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ActiveAimAdditionalRotationSpeedMulti.Value * beltfedFactor * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                    stanceRotation = activeAimMiniTargetQuaternion;
                }
                else 
                {
                    rotationSpeed = 4f * stanceMulti * dt * Plugin.ActiveAimRotationMulti.Value * beltfedFactor * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f) * transitionSpeedFactor;
                    stanceRotation = activeAimTargetQuaternion;
                }

                StanceController.StanceBlender.Speed = Plugin.ActiveAimSpeedMulti.Value * stanceMulti * beltfedFactor * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, activeAimTargetPosition, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * transitionSpeedFactor * dt);

                if ((StanceController.StanceBlender.Value >= 1f || StanceController.StanceTargetPosition == activeAimTargetPosition) && !StanceController.DidStanceWiggle)
                {
                    StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(-10f, -5f, -30f), true);
                    StanceController.DidStanceWiggle = true;
                }
            }
            else if (StanceController.StanceBlender.Value > 0f && !hasResetActiveAim && !StanceController.IsLowReady && !StanceController.IsHighReady && !StanceController.IsShortStock && !isResettingLowReady && !isResettingHighReady && !isResettingShortStock && !isResettingMelee)
            {
                StanceController.CanResetDamping = false;

                isResettingActiveAim = true;
                rotationSpeed = stanceMulti * dt * Plugin.ActiveAimResetRotationSpeedMulti.Value * beltfedFactor * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = activeAimRevertQuaternion;
                StanceController.StanceBlender.Speed = Plugin.ActiveAimResetSpeedMulti.Value * stanceMulti * beltfedFactor * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);

            }
            else if (StanceController.StanceBlender.Value == 0f && !hasResetActiveAim)
            {
                if (!StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }

                StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(-10f, 5f, 40f) * movementFactor, true);
                StanceController.DidStanceWiggle = false;

                stanceRotation = Quaternion.identity;

                isResettingActiveAim = false;
                hasResetActiveAim = true;
            }

            ////Melee////
            if (StanceController.IsMeleeAttack && !StanceController.IsShortStock && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsLowReady && !pwa.IsAiming && !IsBlindFiring && !pwa.LeftStance && !PlayerStats.IsSprinting)
            {
                isResettingMelee = false;
                hasResetMelee = false;
                hasResetActiveAim = true;
                hasResetHighReady = true;
                hasResetLowReady = true;
                hasResetShortStock = true;

                if (StanceController.StanceTargetPosition == meleeTargetPosition && StanceController.StanceBlender.Value >= 1f && !StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }
                else if (StanceController.StanceTargetPosition != meleeTargetPosition || StanceController.StanceBlender.Value < 1)
                {
                    StanceController.CanResetDamping = false;
                }

                rotationSpeed = 15f * stanceMulti * dt * (isThirdPerson ? Plugin.ThirdPersonRotationSpeed.Value : 1f);
                stanceRotation = meleeTargetQuaternion;

                StanceController.StanceBlender.Speed = 30f * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
                StanceController.StanceTargetPosition = Vector3.Lerp(StanceController.StanceTargetPosition, meleeTargetPosition, Plugin.StanceTransitionSpeedMulti.Value * stanceMulti * dt * 2f);

                if ((StanceController.StanceBlender.Value >= 0.95f || StanceController.StanceTargetPosition == meleeTargetPosition))
                {
                    if (!StanceController.DidStanceWiggle)
                    {
                        doMeleeEffect();
                        StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(-20f, -10f, -100f) * movementFactor, true, 3f);
                        StanceController.DidStanceWiggle = true;
                    }
                }

                if (StanceController.StanceBlender.Value >= 0.8f)
                {
                    StanceController.CanDoMeleeDetection = true;
                }
                float distance = Vector3.Distance(StanceController.StanceTargetPosition, meleeTargetPosition);
                if (StanceController.StanceBlender.Value >= 1f && distance <= 0.001f) 
                {
                    StanceController.IsMeleeAttack = false;
                    StanceBlender.Target = 0f;
                }
            }
            else if (StanceController.StanceBlender.Value > 0f && !hasResetMelee) //&& !StanceController.IsLowReady && !StanceController.IsActiveAiming && !StanceController.IsHighReady && !StanceController.IsShortStock && !isResettingActiveAim && !isResettingHighReady && !isResettingLowReady && !isResettingShortStock
            {
                StanceController.CanDoMeleeDetection = false;
                StanceController.CanResetDamping = false;
                isResettingMelee = true;
                rotationSpeed = 10f * stanceMulti * dt;
                stanceRotation = Quaternion.Euler(Vector3.zero);
                StanceController.StanceBlender.Speed = 15f * stanceMulti * (isThirdPerson ? Plugin.ThirdPersonPositionSpeed.Value : 1f);
            }
            else if (StanceController.StanceBlender.Value == 0f && !hasResetMelee)
            {
                StanceController.DoMeleeReset = true;
                if (!StanceController.CanResetDamping)
                {
                    StanceController.DoDampingTimer = true;
                }
/*                StanceController.DoWiggleEffects(player, pwa, fc, new Vector3(Plugin.test4.Value, Plugin.test5.Value, Plugin.test6.Value), true);
*/              stanceRotation = Quaternion.identity;
                isResettingMelee = false;
                hasResetMelee = true;
            }

        }

        public static void DoWiggleEffects(Player player, ProceduralWeaponAnimation pwa, Player.FirearmController fc, Vector3 wiggleDirection, bool playSound = false, float volume = 1f)
        {
            if (playSound)
            {
                AccessTools.Method(typeof(Player), "method_46").Invoke(player, new object[] { volume });
            }

            for (int i = 0; i < pwa.Shootingg.CurrentRecoilEffect.RecoilProcessValues.Length; i++)
            {
                pwa.Shootingg.CurrentRecoilEffect.RecoilProcessValues[i].Process(wiggleDirection);
            }
        }

        private static Vector3 currentMountedPos = Vector3.zero;
        private static float resetTimer = 0f;
        private static float breathTimer = 0f;
        private static bool hasNotReset = false;
        public static void DoMounting(Player player, ProceduralWeaponAnimation pwa, Player.FirearmController fc, ref Vector3 weaponWorldPos, ref Vector3 mountWeapPosition, float dt, Vector3 referencePos)
        {
            bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
            float resetTime = isMoving ? 0.1f : 0.5f;

            if (StanceController.IsMounting && isMoving)
            {
                StanceController.IsMounting = false;
            }
            if (Input.GetKeyDown(Plugin.MountKeybind.Value.MainKey) && StanceController.IsBracing && player.ProceduralWeaponAnimation.OverlappingAllowsBlindfire)
            {
                StanceController.IsMounting = !StanceController.IsMounting;
                if (StanceController.IsMounting)
                {
                    mountWeapPosition = weaponWorldPos + StanceController.CoverOffset; // + StanceController.CoverDirection
                    DoWiggleEffects(player, pwa, fc, StanceController.IsMounting ? StanceController.CoverWiggleDirection : StanceController.CoverWiggleDirection * -1f, true);
                }

                float accuracy = fc.Item.GetTotalCenterOfImpact(false); //forces accuracy to update
                AccessTools.Field(typeof(Player.FirearmController), "float_3").SetValue(fc, accuracy);
            }
            if (Input.GetKeyDown(Plugin.MountKeybind.Value.MainKey) && !StanceController.IsBracing && StanceController.IsMounting)
            {
                StanceController.IsMounting = false;
            }

            if (StanceController.IsMounting)
            {
                StanceController.MountingBreathReduction = Mathf.Lerp(StanceController.MountingBreathReduction, 0f, 0.2f);
                float mountOrientationBonus = StanceController.BracingDirection == EBracingDirection.Top ? 0.75f : 1f;
                StanceController.BracingSwayBonus = Mathf.Lerp(StanceController.BracingSwayBonus, 0.4f * mountOrientationBonus, 0.2f);
                StanceController.BlockBreathEffect = true;
                hasNotReset = true;
                AccessTools.Field(typeof(TurnAwayEffector), "_turnAwayThreshold").SetValue(pwa.TurnAway, 1f);

                currentMountedPos.x = mountWeapPosition.x;
                currentMountedPos.y = mountWeapPosition.y;
                currentMountedPos.z = weaponWorldPos.z;

                weaponWorldPos = currentMountedPos; //this makes it feel less clamped to cover but allows h recoil + StanceController.CoverDirection
            }
            else if (hasNotReset && resetTimer < resetTime)
            {
                StanceController.BracingSwayBonus = 0f;
                resetTimer += dt;
                currentMountedPos = Vector3.Lerp(currentMountedPos, referencePos, 0.15f);
                weaponWorldPos = currentMountedPos;
            }
            else 
            {
/*                StanceController.MountingBreathReduction = Mathf.Lerp(StanceController.MountingBreathReduction, 1f, 0.001f);
*/              hasNotReset = false;
                resetTimer = 0f;
                if (StanceController.BlockBreathEffect)
                {
                    breathTimer += dt;
                    if (breathTimer >= 1.25f)
                    {
                        breathTimer = 0f;
                        StanceController.BlockBreathEffect = false;
                    }
                }
            }
        }  
    }
}
