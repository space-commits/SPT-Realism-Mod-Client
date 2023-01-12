using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using System.Reflection;

namespace RealismMod
{
    public class UpdateSensitivityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("UpdateSensitivity");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Player.FirearmController __instance, ref bool ____isAiming, ref float ____aimingSens)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (!player.IsAI && ____isAiming)
            {
                if (Plugin.isUniformAimPresent == false || Plugin.isBridgePresent == false)
                {
                    Plugin.startingSens = ____aimingSens;
                    Plugin.currentSens = ____aimingSens;
                }
                else
                {
                    Plugin.currentSens = Plugin.startingSens;
                }
            }
        }
    }

    public class AimingSensitivityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("get_AimingSensitivity");
        }
        [PatchPostfix]
        public static void PatchPostfix(ref Player.FirearmController __instance, ref bool ____isAiming, ref float ____aimingSens)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (!player.IsAI && ____isAiming)
            {
                ____aimingSens = Plugin.currentSens;
            }
        }
    }
}
