using Aki.Reflection.Patching;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RealismMod
{
    public class IsPlayerEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotGroupClass).GetMethod("IsPlayerEnemy", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(BotGroupClass __instance, ref bool __result)
        {
/*            if (StanceController.IsHighReady || StanceController.IsLowReady)
            {
                __result = false;
                Logger.LogWarning("is not enemy");
            }
            else 
            {
                __result = true;
                Logger.LogWarning("is enemy");
            }*/
            __result = false;
            return false;
        }
    }

    public class IsPlayerEnemyByRolePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotGroupClass).GetMethod("IsPlayerEnemyByRole", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(BotGroupClass __instance, ref bool __result)
        {
            /*            if (StanceController.IsHighReady || StanceController.IsLowReady)
                        {
                            __result = false;
                            Logger.LogWarning("is not enemy");
                        }
                        else 
                        {
                            __result = true;
                            Logger.LogWarning("is enemy");
                        }*/
            __result = false;
            return false;
        }
    }
}
