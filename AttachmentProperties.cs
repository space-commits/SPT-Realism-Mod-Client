using EFT.InventoryLogic;

namespace RealismMod
{
    public static class AttachmentProperties
    {

        public static string ModType(Item mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return "";
            }
            return mod.ConflictingItems[1];
        }

        public static float VerticalRecoil(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return mod.Recoil;
            }
            return float.Parse(mod.ConflictingItems[2]);
        }

        public static float HorizontalRecoil(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return mod.Recoil;
            }
            return float.Parse(mod.ConflictingItems[3]);
        }

        public static float Dispersion(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[4]);
        }

        public static float CameraRecoil(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[5]);
        }

        public static float AutoROF(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[6]);
        }

        public static float SemiROF(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[7]);
        }

        public static float ModMalfunctionChance(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[8]);
        }

        public static float ReloadSpeed(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[9]);
        }

        public static float AimSpeed(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[10]);
        }

        public static float ChamberSpeed(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[11]);
        }

        public static float ModConvergence(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[12]);
        }

        public static bool CanCylceSubs(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(mod.ConflictingItems[13]);
        }

        public static float RecoilAngle(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[14]);
        }

        public static bool StockAllowADS(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(mod.ConflictingItems[15]);
        }

        public static float FixSpeed(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[16]);
        }

        public static float ModShotDispersion(Mod mod)
        {
            if (Utils.NullCheck(mod.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(mod.ConflictingItems[17]);
        }
    }
}
