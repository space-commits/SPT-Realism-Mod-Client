using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace RealismMod
{
    public class IsAimingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("get_IsAiming", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Player.FirearmController __instance, ref bool ____isAiming)
        {

            Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
            if (!player.IsAI)
            {
                FaceShieldComponent component = player.FaceShieldObserver.Component;
                bool isOn = component != null && (component.Togglable == null || component.Togglable.On);
                if (isOn && !WeaponProperties.WeaponCanFSADS && !FaceShieldProperties.AllowsADS(component.Item))
                {
                    ____isAiming = false;
                }
                Plugin.isAiming = ____isAiming;
            }
        }
    }
}