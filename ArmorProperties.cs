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

        public static bool HasBypassedArmorr_DEPRICATED(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(armorItem.ConflictingItems[9]);
        }

        public static bool HasSideArmor(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(armorItem.ConflictingItems[10]);
        }

        public static bool HasStomachArmor(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(armorItem.ConflictingItems[11]);
        }

        public static bool HasHitSecondaryArmor_DEPRICATED(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(armorItem.ConflictingItems[12]);
        }

        public static bool HasNeckArmor(Item armorItem)
        {
            if (Utils.NullCheck(armorItem.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(armorItem.ConflictingItems[13]);
        }

    }
}
