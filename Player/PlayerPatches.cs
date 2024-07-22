using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.Animations.NewRecoil;
using EFT.InputSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using RealismMod.Weapons;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static EFT.Player;
using WeaponSkills = EFT.SkillManager.GClass1783;
using WeaponStateClass = GClass1668;
using EFT.AssetsManager;

namespace RealismMod
{
    public class SyncWithCharacterSkillsPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(EFT.Player.FirearmController).GetMethod("SyncWithCharacterSkills", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                WeaponSkills weaponInfo = player.Skills.GetWeaponInfo(__instance.Item);
                PlayerState.StrengthWeightBuff = player.Skills.StrengthBuffLiftWeightInc.Value;
                PlayerState.StrengthSkillAimBuff = player.Skills.StrengthBuffAimFatigue.Value;
                PlayerState.ReloadSkillMulti = weaponInfo.ReloadSpeed;
                PlayerState.FixSkillMulti = weaponInfo.FixSpeed;
                PlayerState.WeaponSkillErgo = weaponInfo.DeltaErgonomics;
                PlayerState.AimSkillADSBuff = weaponInfo.AimSpeed;
            }
        }
    }

    public class TotalWeightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Inventory).GetMethod("method_0", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(Inventory __instance, ref float __result)
        {
            __result = GearController.GetModifiedInventoryWeight(__instance);
            return false;
        }
    }

    public class PlayerInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (__instance.IsYourPlayer)
            {
                StatCalc.CalcPlayerWeightStats(__instance);
                GearController.SetGearParamaters(__instance);
                GearController.GetGearPenalty(__instance);
                if(Plugin.ServerConfig.enable_hazard_zones) Plugin.RealHealthController.CheckInventoryForHazardousMaterials(__instance.Inventory);

                if (Plugin.ServerConfig.med_changes) //also add check for hazard zones being enabled 
                {
                    GearController.CheckForDevices(__instance.Inventory);
                    PlayerState.IsScav = Singleton<GameWorld>.Instance.MainPlayer.Profile.Info.Side == EPlayerSide.Savage;
                    PlayerHazardBridge hazardBridge = __instance.gameObject.AddComponent<PlayerHazardBridge>();
                    hazardBridge._Player = __instance;
                }
            }
            if(Plugin.EnablePlateChanges.Value) BallisticsController.ModifyPlateColliders(__instance);
        }
    }

    public class OnItemAddedOrRemovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnItemAddedOrRemoved", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {

            if (__instance.IsYourPlayer)
            {
                StatCalc.CalcPlayerWeightStats(__instance);
                GearController.SetGearParamaters(__instance);
                GearController.GetGearPenalty(__instance);
                GearController.CheckForDevices(__instance.Inventory);
                if (Plugin.ServerConfig.enable_hazard_zones) Plugin.RealHealthController.CheckInventoryForHazardousMaterials(__instance.Inventory);
            }
        }
    }

    public class PlayerUpdatePatch : ModulePatch
    {
        private static FieldInfo surfaceField;

        private static float _sprintCooldownTimer = 0f;
        private static bool _doSwayReset = false;
        private static float _sprintTimer = 0f;
        private static bool _didSprintPenalties = false;
        private static bool _resetSwayAfterFiring = false;

        private static void DoSprintTimer(Player player, ProceduralWeaponAnimation pwa, Player.FirearmController fc, float mountingBonus)
        {
            _sprintCooldownTimer += Time.deltaTime;

            if (!_didSprintPenalties)
            {
                bool skipPenalty = RecoilController.IsFiring && !StanceController.IsAiming;
                float sprintDurationModi = 1f + (_sprintTimer / 7f);
                float ergoWeight = WeaponStats.ErgoFactor * (1f + (1f - PlayerState.GearErgoPenalty));
                ergoWeight = 1f + (ergoWeight / 200f);

                float breathIntensity = Mathf.Min(pwa.Breath.Intensity * sprintDurationModi * ergoWeight, skipPenalty ? 1f : 3f);
                float inputIntensitry = Mathf.Min(pwa.HandsContainer.HandsRotation.InputIntensity * sprintDurationModi * ergoWeight, skipPenalty ? 1f : 1.05f);
                pwa.Breath.Intensity = breathIntensity * mountingBonus;
                pwa.HandsContainer.HandsRotation.InputIntensity = inputIntensitry * mountingBonus;
                PlayerState.SprintTotalBreathIntensity = breathIntensity;
                PlayerState.SprintTotalHandsIntensity = inputIntensitry;
                PlayerState.SprintHipfirePenalty = Mathf.Min(1f + (_sprintTimer / 100f), 1.25f);
                PlayerState.ADSSprintMulti = Mathf.Max(1f - (_sprintTimer / 12f), 0.3f);

                _didSprintPenalties = true;
                _doSwayReset = false;
            }

            if (_sprintCooldownTimer >= 0.35f)
            {
                PlayerState.SprintBlockADS = false;
                if (PlayerState.TriedToADSFromSprint)
                {
                    fc.ToggleAim();
                }
            }
            if (_sprintCooldownTimer >= 4f)
            {
                PlayerState.WasSprinting = false;
                _doSwayReset = true;
                _sprintCooldownTimer = 0f;
                _sprintTimer = 0f;
            }
        }

        private static void ResetSwayParams(ProceduralWeaponAnimation pwa, float mountingBonus)
        {
            bool skipPenalty = RecoilController.IsFiring && !StanceController.IsAiming;
            float resetSwaySpeed = 0.035f;
            float resetSpeed = 0.4f;
            PlayerState.SprintTotalBreathIntensity = Mathf.Lerp(PlayerState.SprintTotalBreathIntensity, PlayerState.TotalBreathIntensity, resetSwaySpeed);
            PlayerState.SprintTotalHandsIntensity = Mathf.Lerp(PlayerState.SprintTotalHandsIntensity, PlayerState.TotalHandsIntensity, resetSwaySpeed);
            PlayerState.ADSSprintMulti = Mathf.Lerp(PlayerState.ADSSprintMulti, 1f, resetSpeed);
            PlayerState.SprintHipfirePenalty = Mathf.Lerp(PlayerState.SprintHipfirePenalty, 1f, resetSpeed);

            pwa.Breath.Intensity =  Mathf.Clamp(PlayerState.SprintTotalBreathIntensity * mountingBonus, 0.1f, skipPenalty ? 1f : 3f);
            pwa.HandsContainer.HandsRotation.InputIntensity = Mathf.Clamp(PlayerState.SprintTotalHandsIntensity * mountingBonus, 0.1f, skipPenalty ? 1f : 1.05f);

            if (Utils.AreFloatsEqual(1f, PlayerState.ADSSprintMulti) && Utils.AreFloatsEqual(pwa.Breath.Intensity, PlayerState.TotalBreathIntensity) && Utils.AreFloatsEqual(pwa.HandsContainer.HandsRotation.InputIntensity, PlayerState.TotalHandsIntensity))
            {
                _doSwayReset = false;
            }
        }

        //jump too
        private static void DoSprintPenalty(Player player, Player.FirearmController fc, float mountingBonus)
        {
            if (player.IsSprintEnabled || !player.MovementContext.IsGrounded || player.MovementContext.PlayerAnimatorIsJumpSetted())
            {
                float fallFactor = !player.MovementContext.IsGrounded ? 2.5f : 1f;
                float jumpFactor = player.MovementContext.PlayerAnimatorIsJumpSetted() ? 4f : 1f;
                _sprintTimer += Time.deltaTime * fallFactor * jumpFactor;
                if (_sprintTimer >= 1f)
                {
                    PlayerState.SprintBlockADS = true;
                    PlayerState.WasSprinting = true;
                    _didSprintPenalties = false;
                }
            }
            else
            {
                if (PlayerState.WasSprinting)
                {
                    DoSprintTimer(player, player.ProceduralWeaponAnimation, fc, mountingBonus);
                }
                if (_doSwayReset)
                {
                    ResetSwayParams(player.ProceduralWeaponAnimation, mountingBonus);
                }
            }

            if (!_doSwayReset && !PlayerState.WasSprinting)
            {
                PlayerState.HasFullyResetSprintADSPenalties = true;
            }
            else
            {
                PlayerState.HasFullyResetSprintADSPenalties = false;
            }

            if (RecoilController.IsFiring)
            {
                _doSwayReset = false;
                _resetSwayAfterFiring = false;
            }
            else if (!_resetSwayAfterFiring)
            {
                _resetSwayAfterFiring = true;
                _doSwayReset = true;
            }
        }

        private static void CalcBaseHipfireAccuracy(Player player)
        {
            float baseValue = 0.4f;
            float convergenceFactor = 1f - (RecoilController.BaseTotalConvergence / 100f);
            float dispersionFactor = 1f + (RecoilController.BaseTotalDispersion / 100f);
            float recoilFactor = 1f + ((RecoilController.BaseTotalVRecoil + RecoilController.BaseTotalHRecoil) / 100f);
            float totalPlayerWeight = PlayerState.TotalModifiedWeightMinusWeapon;
            float playerWeightFactor = 1f + (totalPlayerWeight / 100f);
            float healthFactor = PlayerState.RecoilInjuryMulti * (Plugin.RealHealthController.HasOverdosed ? 1.5f : 1f);
            float ergoWeight = WeaponStats.ErgoFactor * (1f + (1f - PlayerState.GearErgoPenalty));
            float ergoFactor = 1f + (ergoWeight / 200f);

            WeaponStats.BaseHipfireInaccuracy = Mathf.Clamp(baseValue * PlayerState.DeviceBonus * ergoFactor * healthFactor * convergenceFactor * dispersionFactor * recoilFactor * playerWeightFactor, 0.2f, 1f);
        }

        private static void GetStaminaPerc(Player player)
        {
            float remainArmStamPercent = Mathf.Min((player.Physical.HandsStamina.Current / player.Physical.HandsStamina.TotalCapacity) * (1f + PlayerState.StrengthSkillAimBuff), 1f);
            PlayerState.StaminaPerc = player.Physical.Stamina.Current / player.Physical.Stamina.TotalCapacity;

            PlayerState.RemainingArmStamPerc = 1f - ((1f - remainArmStamPercent) / 3f);
            PlayerState.RemainingArmStamPercReload = Mathf.Clamp(1f - ((1f - remainArmStamPercent) / 4f), 0.85f, 1f); 
        }

        private static void SetStancePWAValues(Player player, FirearmController fc)
        {
            if (StanceController.CanResetDamping)
            {
                float stockedPistolFactor = WeaponStats.IsStockedPistol ? 0.75f : 1f;
                NewRecoilShotEffect newRecoil = player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect as NewRecoilShotEffect;
                newRecoil.HandRotationRecoil.CategoryIntensityMultiplier = Mathf.Lerp(newRecoil.HandRotationRecoil.CategoryIntensityMultiplier, fc.Weapon.Template.RecoilCategoryMultiplierHandRotation * Plugin.RecoilIntensity.Value * stockedPistolFactor, 0.01f);
                newRecoil.HandRotationRecoil.ReturnTrajectoryDumping = Mathf.Lerp(newRecoil.HandRotationRecoil.ReturnTrajectoryDumping, fc.Weapon.Template.RecoilReturnPathDampingHandRotation * Plugin.HandsDampingMulti.Value, 0.01f);
                player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.Damping = Mathf.Lerp(player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.Damping, fc.Weapon.Template.RecoilDampingHandRotation * Plugin.RecoilDampingMulti.Value, 0.01f);
                player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = Mathf.Lerp(player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping, 0.41f, 0.01f);
                player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.ReturnSpeed = Mathf.Lerp(player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.ReturnSpeed, RecoilController.BaseTotalConvergence, 0.01f);
            }
            else
            {
                player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = 0.75f;
                NewRecoilShotEffect newRecoil = player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect as NewRecoilShotEffect;
                newRecoil.HandRotationRecoil.CategoryIntensityMultiplier = WeaponStats._WeapClass == "pistol" ? 0.4f : 0.3f;
                newRecoil.HandRotationRecoil.ReturnTrajectoryDumping = 0.8f;
                player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.Damping = 0.8f;
                player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.ReturnSpeed = 10f * StanceController.WiggleReturnSpeed;
            }

            player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandPositionRecoilEffect.Damping = 0.5f;
            player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandPositionRecoilEffect.ReturnSpeed = 0.08f;
            player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.RecoilProcessValues[3].IntensityMultiplicator = 0;
            player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.RecoilProcessValues[4].IntensityMultiplicator = 0;
        }

        private static void ChamberTimer(FirearmController fc)
        {
            Plugin.ChamberTimer += Time.deltaTime;
            if (Plugin.ChamberTimer >= 0.5f)
            {
                fc.FirearmsAnimator.Rechamber(false);
                fc.SetAnimatorAndProceduralValues();
                Plugin.StartRechamberTimer = false;
                Plugin.ChamberTimer = 0f;
            }
        }

        private static void PWAUpdate(Player player, Player.FirearmController fc) 
        {
            if (fc != null)
            {
                if (Plugin.StartRechamberTimer)
                {
                    ChamberTimer(fc);
                }

                if (Plugin.ServerConfig.enable_stances)
                {
                    StanceController.DoMounting(player, player.ProceduralWeaponAnimation, fc);
                }

                if (RecoilController.IsFiring)
                {
                    RecoilController.SetRecoilParams(player.ProceduralWeaponAnimation, fc.Item);
                    if (StanceController.CurrentStance == EStance.PatrolStance)
                    {
                        StanceController.CurrentStance = EStance.None;
                    }
                }

                ReloadController.ReloadStateCheck(player, fc, Logger);
                AimController.ADSCheck(player, fc);

                if (Plugin.EnableStanceStamChanges.Value && Plugin.ServerConfig.enable_stances)
                {
                    StanceController.SetStanceStamina(player);
                }

                GetStaminaPerc(player);

                if (!RecoilController.IsFiringMovement && Plugin.ServerConfig.enable_stances)
                {
                    SetStancePWAValues(player, fc);
                }
                player.MovementContext.SetPatrol(StanceController.CurrentStance == EStance.PatrolStance ? true : false);
            }
            else if (Plugin.ServerConfig.enable_stances && Plugin.EnableStanceStamChanges.Value && !StanceController.HaveResetStamDrain)
            {
                StanceController.UnarmedStanceStamina(player);
            }
            else 
            {
                StanceController.IsMounting = false;
            }

            CalcBaseHipfireAccuracy(player);
            float stanceHipFactor = StanceController.CurrentStance == EStance.ActiveAiming ? 0.7f : StanceController.CurrentStance == EStance.ShortStock ? 1.35f : 1.05f;
            player.ProceduralWeaponAnimation.Breath.HipPenalty = Mathf.Clamp(WeaponStats.BaseHipfireInaccuracy * PlayerState.SprintHipfirePenalty * stanceHipFactor, 0.1f, 0.5f);
        }

        protected override MethodBase GetTargetMethod()
        {
            surfaceField = AccessTools.Field(typeof(Player), "_currentSet");
            return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix] 
        private static void PatchPostfix(Player __instance)
        {
            if (Plugin.ServerConfig.headset_changes)
            {
                SurfaceSet currentSet = (SurfaceSet)surfaceField.GetValue(__instance);
                currentSet.SprintSoundBank.BaseVolume = Plugin.SharedMovementVolume.Value;
                currentSet.StopSoundBank.BaseVolume = Plugin.SharedMovementVolume.Value;
                currentSet.JumpSoundBank.BaseVolume = Plugin.SharedMovementVolume.Value;
                currentSet.LandingSoundBank.BaseVolume = Plugin.SharedMovementVolume.Value;
            }

            if (Utils.IsReady && __instance.IsYourPlayer)
            {
                Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
                PlayerState.IsSprinting = __instance.IsSprintEnabled;
                PlayerState.EnviroType = __instance.Environment;
                StanceController.IsInInventory = __instance.IsInventoryOpened;
                PlayerState.IsMoving = __instance.IsSprintEnabled || __instance.MovementContext.AbsoluteMovementDirection.x  > 0 || __instance.MovementContext.AbsoluteMovementDirection.y > 0;

                if (Plugin.EnableSprintPenalty.Value)
                {
                    DoSprintPenalty(__instance, fc, StanceController.BracingSwayBonus);
                }
                else PlayerState.HasFullyResetSprintADSPenalties = true;

                if (PlayerState.HasFullyResetSprintADSPenalties)
                {
                    __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerState.TotalBreathIntensity * StanceController.BracingSwayBonus;
                    __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerState.TotalHandsIntensity * StanceController.BracingSwayBonus;
                }

                if (Plugin.ServerConfig.recoil_attachment_overhaul)
                {
                    PWAUpdate(__instance, fc);
                }
            }
        }
    }
}

