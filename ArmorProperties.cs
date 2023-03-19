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
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return true;
            }
            return bool.Parse(armorItem.ConflictingItems[1]);
        }

        public static string ArmorClass(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return "Unclassified";
            }
            return armorItem.ConflictingItems[2];
        }

        public static bool CanSpall(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return true;
            }
            return bool.Parse(armorItem.ConflictingItems[3]);
        }

        public static float SpallReduction(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return 1f;
            }
            return float.Parse(armorItem.ConflictingItems[4]);
        }

        public static float ReloadSpeedMulti(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return 1f;
            }
            return float.Parse(armorItem.ConflictingItems[5]);
        }


        public static float MinVelocity(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return 1f;
            }
            return float.Parse(armorItem.ConflictingItems[6]);
        }


        public static float MinKE(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return 1f;
            }
            return float.Parse(armorItem.ConflictingItems[7]);
        }


        public static float MinPen(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return 1f;
            }
            return float.Parse(armorItem.ConflictingItems[8]);
        }

    }
}
