using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RealismMod
{
    public class IsKnownMalfTypePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon.GClass2195).GetMethod("IsKnownMalfType", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostfix(ref bool __result)
        {
            __result = true;
        }
    }
}
