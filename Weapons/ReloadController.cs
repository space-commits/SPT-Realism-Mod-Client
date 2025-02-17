using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace RealismMod.Weapons
{
    public static class ReloadController
    {
        public static float MinimumReloadSpeed = 0.7f;
        public static float MaxInternalReloadSpeed = 1.5f;
        public static float MaxReloadSpeed
        {
            get 
            {
                return PlayerValues.IsQuickReloading ? 1.65f : 1.4f;
            }

        }  

        public static void SetMagReloadSpeeds(Player.FirearmController __instance, MagazineItemClass magazine, bool isQuickReload = false)
        {
            PlayerValues.IsMagReloading = true;
            StanceController.CancelLowReady = true;
            StanceController.CancelLeftShoulder = true;
            Weapon weapon = __instance.Item;

            if (PlayerValues.NoCurrentMagazineReload)
            {
                Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);
                MagReloadSpeedModifier(weapon, magazine, false, true);
                player.HandsAnimator.SetAnimationSpeed(Mathf.Clamp(
                    WeaponStats.CurrentMagReloadSpeed * PlayerValues.ReloadInjuryMulti * PlayerValues.ReloadSkillMulti * 
                    PlayerValues.GearReloadMulti * StanceController.HighReadyManipBuff * StanceController.ActiveAimManipBuff *
                    Plugin.RealHealthController.AdrenalineReloadBonus * (Mathf.Max(PlayerValues.RemainingArmStamFactor, 0.8f)), 0.65f, 1.35f));
            }
            else
            {
                MagReloadSpeedModifier(weapon, magazine, true, false, isQuickReload);
            }
        }

        public static void MagReloadSpeedModifier(Weapon weapon, MagazineItemClass magazine, bool isNewMag, bool reloadFromNoMag, bool isQuickReload = false)
        {
            var weaponModStats = TemplateStats.GetDataObj<WeaponMod>(TemplateStats.WeaponModStats, magazine.TemplateId);
            float magWeight = weapon.IsBeltMachineGun ? magazine.TotalWeight * StatCalc.MagWeightMult * 0.5f : magazine.TotalWeight * StatCalc.MagWeightMult;
            float magWeightFactor = (magWeight / -100f) + 1f;
            float magSpeed = weaponModStats.ReloadSpeed;
            float reloadSpeedModiLessMag = WeaponStats.TotalReloadSpeedLessMag;
            float stockModifier = weapon.WeapClass != "pistol" && !WeaponStats.HasShoulderContact ? 0.8f : 1f;
            float playerWeightFactor = 1f - (PlayerValues.TotalModifiedWeightMinusWeapon * 0.001f);

            float magSpeedMulti = (magSpeed / 100f) + 1f;
            float totalReloadSpeed = magSpeedMulti * magWeightFactor * reloadSpeedModiLessMag * stockModifier * playerWeightFactor;

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
                WeaponStats.NewMagReloadSpeed *= PluginConfig.QuickReloadSpeedMulti.Value;
                WeaponStats.CurrentMagReloadSpeed *= PluginConfig.QuickReloadSpeedMulti.Value;
            }

            WeaponStats.NewMagReloadSpeed *= PluginConfig.GlobalReloadSpeedMulti.Value;
            WeaponStats.CurrentMagReloadSpeed *= PluginConfig.GlobalReloadSpeedMulti.Value;
        }

        public static void ReloadStateCheck(Player player, EFT.Player.FirearmController fc)
        {
            PlayerValues.IsInReloadOpertation = fc.IsInReloadOperation();

            if (PlayerValues.IsInReloadOpertation)
            {
                if (StanceController.CurrentStance == EStance.PatrolStance)
                {
                    StanceController.CurrentStance = EStance.None;
                }

                StanceController.ModifyHighReady = true;
                StanceController.CancelShortStock = true;
                StanceController.CancelActiveAim = true;
                StanceController.CancelLeftShoulder = true;

                if (PlayerValues.IsAttemptingToReloadInternalMag && Plugin.ServerConfig.reload_changes)
                {
                    StanceController.CancelHighReady = fc.Item.WeapClass != "shotgun" ? true : false;
                    StanceController.CancelLowReady = fc.Item.WeapClass == "shotgun" || fc.Item.WeapClass == "pistol" ? true : false;

                    float highReadyBonus = fc.Item.WeapClass == "shotgun" && StanceController.CurrentStance == EStance.HighReady == true ? StanceController.HighReadyManipBuff : 1f;
                    float lowReadyBonus = fc.Item.WeapClass != "shotgun" && StanceController.CurrentStance == EStance.LowReady == true ? StanceController.LowReadyManipBuff : 1f;

                    float IntenralMagReloadSpeed = Mathf.Clamp(WeaponStats.CurrentMagReloadSpeed * PluginConfig.InternalMagReloadMulti.Value * PlayerValues.ReloadSkillMulti * PlayerValues.ReloadInjuryMulti * highReadyBonus * lowReadyBonus * PlayerValues.RemainingArmStamReloadFactor, MinimumReloadSpeed, MaxInternalReloadSpeed);
                    player.HandsAnimator.SetAnimationSpeed(IntenralMagReloadSpeed);

                    if (PluginConfig.EnableReloadLogging.Value == true)
                    {
                        Utils.Logger.LogWarning("IsAttemptingToReloadInternalMag = " + IntenralMagReloadSpeed);
                    }
                }
            }
            else
            {
                PlayerValues.IsAttemptingToReloadInternalMag = false;
                PlayerValues.IsAttemptingRevolverReload = false;
            }
        }
    }


}
