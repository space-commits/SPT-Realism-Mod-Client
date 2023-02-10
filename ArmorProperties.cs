using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Text;

namespace RealismMod
{
    public static class ArmorProperties
    {
        public static bool AllowsADS(Item armorItem)
        {
            if (Helper.NullCheck(armorItem.ConflictingItems))
            {
                return true;
            }
            return bool.Parse(armorItem.ConflictingItems[1]);
        }

        public static string ArmorClass(Item armorItem)
        {
            if (Helper.NullCheck(armorItem.ConflictingItems))
            {
                return "Unclassified";
            }
            return armorItem.ConflictingItems[2];
        }

        public static bool CanSpall(Item armorItem)
        {
            if (Helper.NullCheck(armorItem.ConflictingItems))
            {
                return true;
            }
            return bool.Parse(armorItem.ConflictingItems[3]);
        }

        public static float SpallReduction(Item armorItem)
        {
            if (Helper.NullCheck(armorItem.ConflictingItems))
            {
                return 1;
            }
            return float.Parse(armorItem.ConflictingItems[4]);
        }

    }
}
