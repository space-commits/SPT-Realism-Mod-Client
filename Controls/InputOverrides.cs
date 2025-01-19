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
using InputClass1 = Class1581;
using InputClass2 = Class1579;
using StatusStruct = GStruct446<GInterface385>;
using ItemEventClass = EFT.InventoryLogic;
using EFT.Animations;

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
            StatusStruct gstruct = mag.Cartridges.PopTo(player.InventoryController, fc.Item.Chambers[0].CreateItemAddress());
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
            //needed to trigger stance cancel on weapon swap
            if (StanceController.CurrentStance != EStance.PistolCompressed && (command == ECommand.SelectFirstPrimaryWeapon || command == ECommand.SelectSecondPrimaryWeapon || command == ECommand.QuickSelectSecondaryWeapon))
            {
                StanceController.DidWeaponSwap = true;
                return true;
            }
            //needed to cancel mounting
            if (command == ECommand.ToggleStepLeft || command == ECommand.ToggleStepRight || command == ECommand.ReturnFromRightStep || command == ECommand.ReturnFromLeftStep)
            {
                StanceController.IsMounting = false;
                return true;
            }
            //needed for manual chambering
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
            //needed to toggle my own left shoulder swap
            if (Plugin.ServerConfig.enable_stances && command == ECommand.LeftStanceToggle)
            {
                if (!StanceController.IsInForcedLowReady) StanceController.ToggleLeftShoulder();
                return false;
            }
            //do wiggle effects on hold breath
            if (command == ECommand.ToggleBreathing && Plugin.ServerConfig.recoil_attachment_overhaul && StanceController.IsAiming)
            {
                Player player = Utils.GetYourPlayer();
                if (player.Physical.HoldingBreath) return true;
                FirearmController fc = player.HandsController as FirearmController;
                if(WeaponStats.TotalWeaponWeight <= 8f) StanceController.DoWiggleEffects(player, player.ProceduralWeaponAnimation, fc.Weapon, new Vector3(0.25f, 0.25f, 0.5f), wiggleFactor: 0.5f);
                return true;
            }
            //cancel stances
            bool shouldBlockFiring = Plugin.ServerConfig.enable_stances && PluginConfig.BlockFiring.Value;
            bool isInBlockableState = !Plugin.RealHealthController.ArmsAreIncapacitated && !Plugin.RealHealthController.HasOverdosed;
            bool isInBlockableStance = StanceController.CurrentStance != EStance.None && StanceController.CurrentStance != EStance.ActiveAiming && StanceController.CurrentStance != EStance.ShortStock && StanceController.CurrentStance != EStance.PistolCompressed;
            if (command == ECommand.ToggleShooting && shouldBlockFiring && isInBlockableState && isInBlockableStance)
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
            //cancel mounting
            if (command == ECommand.ToggleProne || command == ECommand.ToggleDuck)
            {
                StanceController.IsMounting = false;
                return true;
            }
            //disable EFT scoll input check if using scroll wheel to change stances
            if ((command == ECommand.ScrollNext || command == ECommand.ScrollPrevious) && (Input.GetKey(PluginConfig.StanceWheelComboKeyBind.Value.MainKey) && PluginConfig.UseMouseWheelPlusKey.Value))
            {
                return false;
            }
            if (command == ECommand.WeaponMounting && PluginConfig.OverrideMounting.Value)
            {
                Player player = Utils.GetYourPlayer();
                ProceduralWeaponAnimation pwa = player.ProceduralWeaponAnimation;
                FirearmController fc = player.HandsController as FirearmController;

                if (StanceController.IsBracing && !StanceController.IsColliding)
                {
                    if (WeaponStats.IsUsingBipod && StanceController.BracingDirection != EBracingDirection.Top) return false;
                    StanceController.IsMounting = !StanceController.IsMounting;
                    if (StanceController.IsMounting) StanceController.CancelAllStances();
                    StanceController.DoWiggleEffects(player, pwa, fc.Weapon, StanceController.IsMounting ? StanceController.CoverWiggleDirection : StanceController.CoverWiggleDirection * -1f, true, wiggleFactor: 0.5f);
                }
                if (!StanceController.IsBracing && StanceController.IsMounting)
                {
                    StanceController.IsMounting = false;
                }
                return false;
            }
            return true;
        }
    }
}
