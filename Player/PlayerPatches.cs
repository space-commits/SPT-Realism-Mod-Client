using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using BepInEx.Logging;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using WeaponSkills = EFT.SkillManager.GClass1771;
using WeightClass = GClass754<float>;
using Comfort.Common;
using InputClass = Class1451;
using static EFT.Player;
using StatusStruct = GStruct414<GInterface324>;
using ItemEventClass = GClass2767;
using WeaponStateClass = GClass1668;
using EFT.InputSystem;
using EFT.Animations.NewRecoil;


namespace RealismMod
{
    public class KeyInputPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InputClass).GetMethod("TranslateCommand", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void RechamberRound(FirearmController fc, Player player) 
        {
            Plugin.CanLoadChamber = true;
            int currentMagazineCount = fc.Weapon.GetCurrentMagazineCount();
            MagazineClass mag = fc.Weapon.GetCurrentMagazine();
            fc.FirearmsAnimator.SetAmmoInChamber(0);
            fc.FirearmsAnimator.SetAmmoOnMag(currentMagazineCount);
            fc.FirearmsAnimator.SetAmmoCompatible(true);
            StatusStruct gstruct = mag.Cartridges.PopTo(player.GClass2761_0, new ItemEventClass(fc.Item.Chambers[0]));
            WeaponStateClass weaponStateClass = (WeaponStateClass)AccessTools.Field(typeof(FirearmController), "gclass1668_0").GetValue(fc);
            weaponStateClass.RemoveAllShells();
            BulletClass bullet = (BulletClass)gstruct.Value.ResultItem;
            fc.FirearmsAnimator.SetAmmoInChamber(1);
            fc.FirearmsAnimator.SetAmmoOnMag(fc.Weapon.GetCurrentMagazineCount());
            weaponStateClass.SetRoundIntoWeapon(bullet, 0);
            fc.FirearmsAnimator.Rechamber(true);
            Plugin.startRechamberTimer = true;
            Plugin.chamberTimer = 0f;
        }


        [PatchPrefix]
        private static bool PatchPrefix(InputClass __instance, ECommand command)
        {
            if (command == ECommand.ChamberUnload && Plugin.ServerConfig.manual_chambering)
            {
                Player player = Utils.GetYourPlayer();
                FirearmController fc = player.HandsController as FirearmController;
                if (!Plugin.CanLoadChamber && fc.Weapon.HasChambers && fc.Weapon.Chambers.Length == 1 && fc.Weapon.ChamberAmmoCount == 0 && fc.Weapon.GetCurrentMagazine() != null && fc.Weapon.GetCurrentMagazine().Count > 0)
                {
                    RechamberRound(fc, player);
                    return false;
                }
            }
            if (command == ECommand.ToggleGoggles)
            {
                AimController.HeadDeviceStateChanged = true;
            }
            if (command == ECommand.ToggleBreathing && Plugin.ServerConfig.recoil_attachment_overhaul && StanceController.IsAiming)
            {
                Player player = Utils.GetYourPlayer();
                if (player.Physical.HoldingBreath) return true;
                FirearmController fc = player.HandsController as FirearmController;
                StanceController.DoWiggleEffects(player, player.ProceduralWeaponAnimation, fc.Weapon, new Vector3(0.75f, 0.75f, 1.25f));
            }
            if (Plugin.BlockFiring.Value && command == ECommand.ToggleShooting && !Plugin.RealHealthController.HasOverdosed && StanceController.CurrentStance != EStance.None && StanceController.CurrentStance != EStance.ActiveAiming && StanceController.CurrentStance != EStance.ShortStock && StanceController.CurrentStance != EStance.PistolCompressed)
            {
                StanceController.CurrentStance = EStance.None;
                StanceController.StoredStance = EStance.None;
                StanceController.StanceBlender.Target = 0f;
                return false;
            }
            if (Utils.Verified && (command == ECommand.EndSprinting || command == ECommand.EndShooting || command == ECommand.ToggleDuck || command == ECommand.Jump))
            {
                return false;
            }
            return true;
        }
    }


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
                PlayerState.StrengthSkillAimBuff = player.Skills.StrengthBuffAimFatigue.Value;
                PlayerState.ReloadSkillMulti = Mathf.Max(1, ((weaponInfo.ReloadSpeed - 1f) * 0.5f) + 1f);
                PlayerState.FixSkillMulti = weaponInfo.FixSpeed;
                PlayerState.WeaponSkillErgo = weaponInfo.DeltaErgonomics;
                PlayerState.AimSkillADSBuff = weaponInfo.AimSpeed;
                PlayerState.StressResistanceFactor = player.Skills.StressPain.Value;
            }
        }
    }

    public class TotalWeightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Inventory).GetMethod("method_0", BindingFlags.Instance | BindingFlags.Public);
        }

        private static float getTotalWeight(Inventory invClass)
        {
            float modifiedWeight = 0f;
            float trueWeight = 0f;
            foreach (EquipmentSlot equipmentSlot in EquipmentClass.AllSlotNames)
            {
                Slot slot = invClass.Equipment.GetSlot(equipmentSlot);
                IEnumerable<Item> items = slot.Items;
                foreach (Item item in items)
                {
                    float itemTotalWeight = item.GetSingleItemTotalWeight();
                    trueWeight += itemTotalWeight;
                    if (equipmentSlot == EquipmentSlot.Backpack || equipmentSlot == EquipmentSlot.TacticalVest || equipmentSlot == EquipmentSlot.ArmorVest || equipmentSlot == EquipmentSlot.Headwear)
                    {
                        modifiedWeight += itemTotalWeight * GearStats.ComfortModifier(item);
                    }
                    else
                    {
                        modifiedWeight += itemTotalWeight;
                    }
                }
            }

            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("==================");
                Logger.LogWarning("Total Modified Weight " + modifiedWeight);
                Logger.LogWarning("Total Unmodified Weight " + trueWeight);
                Logger.LogWarning("==================");
            }

            return modifiedWeight;
        }

        [PatchPrefix]
        private static bool PatchPrefix(Inventory __instance, ref float __result)
        {
            __result = getTotalWeight(__instance);
            return false;
        }
    }

    public class PlayerInitPatch    : ModulePatch
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
                StatCalc.SetGearParamaters(__instance);
            }
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
                StatCalc.SetGearParamaters(__instance);
            }
        }
    }

    public class PlayerLateUpdatePatch : ModulePatch
    {
        private static FieldInfo surfaceField;

        private static float sprintCooldownTimer = 0f;
        private static bool doSwayReset = false;
        private static float sprintTimer = 0f;
        private static bool didSprintPenalties = false;
        private static bool resetSwayAfterFiring = false;

        private static void doSprintTimer(ProceduralWeaponAnimation pwa, Player.FirearmController fc, float mountingBonus)
        {
            sprintCooldownTimer += Time.deltaTime;

            if (!didSprintPenalties)
            {
                float sprintDurationModi = 1f + (sprintTimer / 7f);

                float breathIntensity = Mathf.Min(pwa.Breath.Intensity * sprintDurationModi, 3f);
                float inputIntensitry = Mathf.Min(pwa.HandsContainer.HandsRotation.InputIntensity * sprintDurationModi, 1.05f);
                pwa.Breath.Intensity = breathIntensity * mountingBonus;
                pwa.HandsContainer.HandsRotation.InputIntensity = inputIntensitry * mountingBonus;
                PlayerState.SprintTotalBreathIntensity = breathIntensity;
                PlayerState.SprintTotalHandsIntensity = inputIntensitry;
                PlayerState.SprintHipfirePenalty = Mathf.Min(1f + (sprintTimer / 100f), 1.25f);
                PlayerState.ADSSprintMulti = Mathf.Max(1f - (sprintTimer / 12f), 0.3f);


                didSprintPenalties = true;
                doSwayReset = false;
            }

            if (sprintCooldownTimer >= 0.35f)
            {
                PlayerState.SprintBlockADS = false;
                if (PlayerState.TriedToADSFromSprint)
                {
                    fc.ToggleAim();
                }
            }
            if (sprintCooldownTimer >= 4f)
            {
                PlayerState.WasSprinting = false;
                doSwayReset = true;
                sprintCooldownTimer = 0f;
                sprintTimer = 0f;
            }
        }

        private static void resetSwayParams(ProceduralWeaponAnimation pwa, float mountingBonus)
        {
            float resetSwaySpeed = 0.035f;
            float resetSpeed = 0.4f;
            PlayerState.SprintTotalBreathIntensity = Mathf.Lerp(PlayerState.SprintTotalBreathIntensity, PlayerState.TotalBreathIntensity, resetSwaySpeed);
            PlayerState.SprintTotalHandsIntensity = Mathf.Lerp(PlayerState.SprintTotalHandsIntensity, PlayerState.TotalHandsIntensity, resetSwaySpeed);
            PlayerState.ADSSprintMulti = Mathf.Lerp(PlayerState.ADSSprintMulti, 1f, resetSpeed);
            PlayerState.SprintHipfirePenalty = Mathf.Lerp(PlayerState.SprintHipfirePenalty, 1f, resetSpeed);

            pwa.Breath.Intensity = PlayerState.SprintTotalBreathIntensity * mountingBonus;
            pwa.HandsContainer.HandsRotation.InputIntensity = PlayerState.SprintTotalHandsIntensity * mountingBonus;

            if (Utils.AreFloatsEqual(1f, PlayerState.ADSSprintMulti) && Utils.AreFloatsEqual(pwa.Breath.Intensity, PlayerState.TotalBreathIntensity) && Utils.AreFloatsEqual(pwa.HandsContainer.HandsRotation.InputIntensity, PlayerState.TotalHandsIntensity))
            {
                doSwayReset = false;
            }
        }

        private static void DoSprintPenalty(Player player, Player.FirearmController fc, float mountingBonus)
        {
            if (player.IsSprintEnabled)
            {
                sprintTimer += Time.deltaTime;
                if (sprintTimer >= 1f)
                {
                    PlayerState.SprintBlockADS = true;
                    PlayerState.WasSprinting = true;
                    didSprintPenalties = false;
                }
            }
            else
            {
                if (PlayerState.WasSprinting)
                {
                    doSprintTimer(player.ProceduralWeaponAnimation, fc, mountingBonus);
                }
                if (doSwayReset)
                {
                    resetSwayParams(player.ProceduralWeaponAnimation, mountingBonus);
                }
            }

            if (!doSwayReset && !PlayerState.WasSprinting)
            {
                PlayerState.HasFullyResetSprintADSPenalties = true;
            }
            else
            {
                PlayerState.HasFullyResetSprintADSPenalties = false;
            }

            if (RecoilController.IsFiring)
            {
                doSwayReset = false;
                resetSwayAfterFiring = false;
            }
            else if (!resetSwayAfterFiring)
            {
                resetSwayAfterFiring = true;
                doSwayReset = true;
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
            float ergoFactor = 1f + (WeaponStats.ErgoFactor / 200f);
           
            WeaponStats.BaseHipfireInaccuracy = Mathf.Clamp(baseValue * PlayerState.DeviceBonus * ergoFactor * healthFactor * convergenceFactor * dispersionFactor * recoilFactor * playerWeightFactor, 0.2f, 1f);
        }

        private static void PWAUpdate(Player player, Player.FirearmController fc) 
        {
            if (fc != null)
            {
                if (Plugin.startRechamberTimer)
                {
                    Plugin.chamberTimer += Time.deltaTime;
                    if (Plugin.chamberTimer >= 1f)
                    {
                        fc.FirearmsAnimator.Rechamber(false);
                        fc.SetAnimatorAndProceduralValues();
                        Plugin.startRechamberTimer = false;
                        Plugin.chamberTimer = 0f;
                    }
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

                if (Plugin.EnableStanceStamChanges.Value)
                {
                    StanceController.SetStanceStamina(player);
                }

                float remainStamPercent = player.Physical.HandsStamina.Current / player.Physical.HandsStamina.TotalCapacity;
                PlayerState.RemainingArmStamPerc = 1f - ((1f - remainStamPercent) / 3f);
                PlayerState.RemainingArmStamPercReload = 1f - ((1f - remainStamPercent) / 4f);

                if (!RecoilController.IsFiringMovement)
                {
                    if (StanceController.CanResetDamping)
                    {
                        float stockedPistolFactor = WeaponStats.IsStockedPistol ? 0.75f : 1f;
                        NewRecoilShotEffect newRecoil = player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect as NewRecoilShotEffect;
                        newRecoil.HandRotationRecoil.CategoryIntensityMultiplier = Mathf.Lerp(newRecoil.HandRotationRecoil.CategoryIntensityMultiplier, fc.Weapon.Template.RecoilCategoryMultiplierHandRotation * Plugin.RecoilIntensity.Value * stockedPistolFactor, 0.01f);
                        newRecoil.HandRotationRecoil.ReturnTrajectoryDumping = Mathf.Lerp(newRecoil.HandRotationRecoil.ReturnTrajectoryDumping, fc.Weapon.Template.RecoilReturnPathDampingHandRotation * Plugin.HandsDampingMulti.Value, 0.01f);
                        player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.Damping = Mathf.Lerp(player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.Damping, fc.Weapon.Template.RecoilDampingHandRotation * Plugin.RecoilDampingMulti.Value, 0.01f);
                        player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = Mathf.Lerp(player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping, 0.45f, 0.01f);
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
                player.MovementContext.SetPatrol(StanceController.CurrentStance == EStance.PatrolStance ? true : false);
            }
            else if (Plugin.ServerConfig.enable_stances && Plugin.EnableStanceStamChanges.Value && !StanceController.HaveResetStamDrain)
            {
                StanceController.UnarmedStanceStamina(player);
            }

            CalcBaseHipfireAccuracy(player);
            float stanceHipFactor = StanceController.CurrentStance == EStance.ActiveAiming ? 0.7f : StanceController.CurrentStance == EStance.ShortStock ? 1.35f : 1.05f;
            player.ProceduralWeaponAnimation.Breath.HipPenalty = Mathf.Clamp(WeaponStats.BaseHipfireInaccuracy * PlayerState.SprintHipfirePenalty * stanceHipFactor, 0.2f, 1.6f);
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
                PlayerState.IsMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

                if (Plugin.EnableSprintPenalty.Value && Plugin.ServerConfig.enable_stances)
                {
                    DoSprintPenalty(__instance, fc, StanceController.BracingSwayBonus);
                    if (PlayerState.HasFullyResetSprintADSPenalties) //!RecoilController.IsFiring && 
                    {
                        __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerState.TotalBreathIntensity * StanceController.BracingSwayBonus;
                        __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerState.TotalHandsIntensity * StanceController.BracingSwayBonus;
                    }
                }
                if (Plugin.ServerConfig.recoil_attachment_overhaul)
                {
                    PWAUpdate(__instance, fc);
                }

            }
        }
    }
}

