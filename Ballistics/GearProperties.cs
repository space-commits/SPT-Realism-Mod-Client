using BepInEx.Logging;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Text;

namespace RealismMod
{
    public static class GearProperties
    {
        public static bool AllowsADS(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && bool.TryParse(armorItem.ConflictingItems[1], out bool result) ? result : true;
        }

        public static string ArmorClass(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) ? armorItem.ConflictingItems[2] : "Unclassified";
        }

        public static bool CanSpall(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && bool.TryParse(armorItem.ConflictingItems[3], out bool result) ? result : false;
        }

        public static float SpallReduction(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && float.TryParse(armorItem.ConflictingItems[4], out float result) ? result : 1f;
        }

        public static float ReloadSpeedMulti(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && float.TryParse(armorItem.ConflictingItems[5], out float result) ? result : 1f;
        }

        public static float MinVelocity(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && float.TryParse(armorItem.ConflictingItems[6], out float result) ? result : 1f;
        }

        public static float MinKE(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && float.TryParse(armorItem.ConflictingItems[7], out float result) ? result : 1f;
        }

        public static float MinPen(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && float.TryParse(armorItem.ConflictingItems[8], out float result) ? result : 1f;
        }

        public static bool BlocksMouth(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && bool.TryParse(armorItem.ConflictingItems[9], out bool result) ? result : false;
        }

        public static bool HasSideArmor(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && bool.TryParse(armorItem.ConflictingItems[10], out bool result) ? result : false;
        }

        public static bool HasStomachArmor(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && bool.TryParse(armorItem.ConflictingItems[11], out bool result) ? result : false;
        }

        public static bool HasHitSecondaryArmor_DEPRICATED(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && bool.TryParse(armorItem.ConflictingItems[12], out bool result) ? result : false;
        }

        public static bool HasNeckArmor(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && bool.TryParse(armorItem.ConflictingItems[13], out bool result) ? result : false;
        }

        public static float DbLevel(Item armorItem)
        {
            return !Utils.NullCheck(armorItem.ConflictingItems) && float.TryParse(armorItem.ConflictingItems[14], out float result) ? result : 0f;
        }

    }
}
