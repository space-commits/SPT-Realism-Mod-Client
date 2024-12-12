using EFT.InputSystem;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static EFT.Player;
using UnityEngine;
using InputClass1 = Class1576; // guessed for these two
using InputClass2 = Class1581;
using StatusStruct = GStruct414<GInterface339>; // no clue
using ItemEventClass = GClass2783;

namespace RealismMod
{
    public class KeyInputPatch1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InputClass1).GetMethod("TranslateCommand", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void RechamberRound(FirearmController fc, Player player)
        {
            Plugin.CanLoadChamber = true;
            int currentMagazineCount = fc.Weapon.GetCurrentMagazineCount();
            MagazineItemClass mag = fc.Weapon.GetCurrentMagazine();
            fc.FirearmsAnimator.SetAmmoInChamber(0);
            fc.FirearmsAnimator.SetAmmoOnMag(currentMagazineCount);
            fc.FirearmsAnimator.SetAmmoCompatible(true);
            StatusStruct gstruct = mag.Cartridges.PopTo(player.InventoryController, new ItemEventClass(fc.Item.Chambers[0]));
            WeaponManagerClass weaponStateClass = (WeaponManagerClass)AccessTools.Field(typeof(FirearmController), "weaponManagerClass").GetValue(fc);
            weaponStateClass.RemoveAllShells();
            AmmoItemClass bullet = (AmmoItemClass)gstruct.Value.ResultItem;
            fc.FirearmsAnimator.SetAmmoInChamber(1);
            fc.FirearmsAnimator.SetAmmoOnMag(fc.Weapon.GetCurrentMagazineCount());
            weaponStateClass.SetRoundIntoWeapon(bullet, 0);
            fc.FirearmsAnimator.Rechamber(true);
            Plugin.StartRechamberTimer = true;
            Plugin.ChamberTimer = 0f;
        }


        [PatchPrefix]
        private static bool PatchPrefix(InputClass1 __instance, ECommand command)
        {
            if (StanceController.CurrentStance != EStance.PistolCompressed && (command == ECommand.SelectFirstPrimaryWeapon || command == ECommand.SelectSecondPrimaryWeapon || command == ECommand.QuickSelectSecondaryWeapon))
            {
                StanceController.DidWeaponSwap = true;
                return true;
            }
            if (command == ECommand.ToggleStepLeft || command == ECommand.ToggleStepRight || command == ECommand.ReturnFromRightStep || command == ECommand.ReturnFromLeftStep)
            {
                StanceController.IsMounting = false;
                return true;
            }
            if (command == ECommand.ChamberUnload && Plugin.ServerConfig.manual_chambering)
            {
                Player player = Utils.GetYourPlayer();
                FirearmController fc = player.HandsController as FirearmController;
                if (player.MovementContext.CurrentState.Name != EPlayerState.Stationary
                    && !Plugin.CanLoadChamber && fc.Weapon.HasChambers && fc.Weapon.Chambers.Length == 1
                    && fc.Weapon.ChamberAmmoCount == 0 && fc.Weapon.GetCurrentMagazine() != null && fc.Weapon.GetCurrentMagazine().Count > 0)
                {
                    RechamberRound(fc, player);
                    return false;
                }
                return true;
            }
            if (command == ECommand.ToggleGoggles || command == ECommand.ChangeScope || command == ECommand.ChangeScopeMagnification)
            {
                AimController.HeadDeviceStateChanged = true;
                return true;
            }
            if (Plugin.ServerConfig.enable_stances && command == ECommand.LeftStanceToggle)
            {
                if (!StanceController.IsInForcedLowReady) StanceController.ToggleLeftShoulder();
                return false;
            }
            if (command == ECommand.ToggleBreathing && Plugin.ServerConfig.recoil_attachment_overhaul && StanceController.IsAiming)
            {
                Player player = Utils.GetYourPlayer();
                if (player.Physical.HoldingBreath) return true;
                FirearmController fc = player.HandsController as FirearmController;
                if(WeaponStats.TotalWeaponWeight <= 8f) StanceController.DoWiggleEffects(player, player.ProceduralWeaponAnimation, fc.Weapon, new Vector3(0.25f, 0.25f, 0.5f), wiggleFactor: 0.5f);
                return true;
            }
            if (Plugin.ServerConfig.enable_stances && PluginConfig.BlockFiring.Value && command == ECommand.ToggleShooting
                && !Plugin.RealHealthController.ArmsAreIncapacitated && !Plugin.RealHealthController.HasOverdosed
                && StanceController.CurrentStance != EStance.None && StanceController.CurrentStance != EStance.ActiveAiming
                && StanceController.CurrentStance != EStance.ShortStock && StanceController.CurrentStance != EStance.PistolCompressed)
            {
                StanceController.CurrentStance = EStance.None;
                StanceController.StoredStance = EStance.None;
                StanceController.StanceBlender.Target = 0f;
                return false;
            }
            return true;
        }
    }

    public class KeyInputPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InputClass2).GetMethod("TranslateCommand", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(InputClass2 __instance, ECommand command)
        {
            if (command == ECommand.ToggleProne || command == ECommand.ToggleDuck)
            {
                StanceController.IsMounting = false;
                return true;
            }
            if ((command == ECommand.ScrollNext || command == ECommand.ScrollPrevious) && (Input.GetKey(PluginConfig.StanceWheelComboKeyBind.Value.MainKey) && PluginConfig.UseMouseWheelPlusKey.Value))
            {
                return false;
            }
            return true;
        }
    }
}
