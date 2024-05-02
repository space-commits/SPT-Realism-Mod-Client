using BepInEx.Logging;
using EFT.InventoryLogic;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod.Weapons
{
    public static class ReloadController
    {
        public static void SetMagReloadSpeeds(Player.FirearmController __instance, MagazineClass magazine, bool isQuickReload = false)
        {
            PlayerState.IsMagReloading = true;
            StanceController.CancelLowReady = true;
            Weapon weapon = __instance.Item;

            if (PlayerState.NoCurrentMagazineReload)
            {
                Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);
                MagReloadSpeedModifier(weapon, magazine, false, true);
                player.HandsAnimator.SetAnimationSpeed(Mathf.Clamp(WeaponStats.CurrentMagReloadSpeed * PlayerState.ReloadInjuryMulti * PlayerState.ReloadSkillMulti * PlayerState.GearReloadMulti * StanceController.HighReadyManipBuff * StanceController.ActiveAimManipBuff * (Mathf.Max(PlayerState.RemainingArmStamPerc, 0.8f)), 0.65f, 1.35f));
            }
            else
            {
                MagReloadSpeedModifier(weapon, magazine, true, false, isQuickReload);
            }
        }

        public static void MagReloadSpeedModifier(Weapon weapon, MagazineClass magazine, bool isNewMag, bool reloadFromNoMag, bool isQuickReload = false)
        {
            float magWeight = weapon.IsBeltMachineGun ? magazine.GetSingleItemTotalWeight() * StatCalc.MagWeightMult * 0.5f : magazine.GetSingleItemTotalWeight() * StatCalc.MagWeightMult;
            float magWeightFactor = (magWeight / -100f) + 1f;
            float magSpeed = AttachmentProperties.ReloadSpeed(magazine);
            float reloadSpeedModiLessMag = WeaponStats.TotalReloadSpeedLessMag;
            float stockModifier = weapon.WeapClass != "pistol" && !WeaponStats.HasShoulderContact ? 0.8f : 1f;

            float magSpeedMulti = (magSpeed / 100f) + 1f;
            float totalReloadSpeed = magSpeedMulti * magWeightFactor * reloadSpeedModiLessMag * stockModifier;

            if (reloadFromNoMag)
            {
                WeaponStats.NewMagReloadSpeed = totalReloadSpeed;
                WeaponStats.CurrentMagReloadSpeed = totalReloadSpeed;
            }
            else
            {
                if (isNewMag)
                {
                    WeaponStats.NewMagReloadSpeed = totalReloadSpeed;
                }
                else
                {
                    WeaponStats.CurrentMagReloadSpeed = totalReloadSpeed;
                }
            }

            if (isQuickReload)
            {
                WeaponStats.NewMagReloadSpeed *= Plugin.QuickReloadSpeedMulti.Value;
                WeaponStats.CurrentMagReloadSpeed *= Plugin.QuickReloadSpeedMulti.Value;
            }

            WeaponStats.NewMagReloadSpeed *= Plugin.GlobalReloadSpeedMulti.Value;
            WeaponStats.CurrentMagReloadSpeed *= Plugin.GlobalReloadSpeedMulti.Value;
        }

        public static void ReloadStateCheck(Player player, EFT.Player.FirearmController fc, ManualLogSource logger)
        {
            PlayerState.IsInReloadOpertation = fc.IsInReloadOperation();

            if (PlayerState.IsInReloadOpertation)
            {
                if (StanceController.CurrentStance == EStance.PatrolStance)
                {
                    StanceController.CurrentStance = EStance.None;
                }

                StanceController.ModifyHighReady = true;
                StanceController.CancelShortStock = true;
                StanceController.CancelActiveAim = true;

                if (PlayerState.IsAttemptingToReloadInternalMag && Plugin.ServerConfig.reload_changes)
                {
                    StanceController.CancelHighReady = fc.Item.WeapClass != "shotgun" ? true : false;
                    StanceController.CancelLowReady = fc.Item.WeapClass == "shotgun" || fc.Item.WeapClass == "pistol" ? true : false;

                    float highReadyBonus = fc.Item.WeapClass == "shotgun" && StanceController.CurrentStance == EStance.HighReady == true ? StanceController.HighReadyManipBuff : 1f;
                    float lowReadyBonus = fc.Item.WeapClass != "shotgun" && StanceController.CurrentStance == EStance.LowReady == true ? StanceController.LowReadyManipBuff : 1f;

                    float IntenralMagReloadSpeed = Mathf.Clamp(WeaponStats.CurrentMagReloadSpeed * Plugin.InternalMagReloadMulti.Value * PlayerState.ReloadSkillMulti * PlayerState.ReloadInjuryMulti * highReadyBonus * lowReadyBonus * (Mathf.Max(PlayerState.RemainingArmStamPercReload, 0.8f)), 0.65f, 1.45f);
                    player.HandsAnimator.SetAnimationSpeed(IntenralMagReloadSpeed);

                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("IsAttemptingToReloadInternalMag = " + IntenralMagReloadSpeed);
                    }
                }
            }
            else
            {
                PlayerState.IsAttemptingToReloadInternalMag = false;
                PlayerState.IsAttemptingRevolverReload = false;
            }
        }
    }


}
