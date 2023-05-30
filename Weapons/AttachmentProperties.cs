using EFT.InventoryLogic;

namespace RealismMod
{
    public static class AttachmentProperties
    {

        public static string ModType(Item mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) ? mod.ConflictingItems[1] : "Unclassified";

        }

        public static float VerticalRecoil(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[2], out float result) ? result : mod.Recoil;

        }

        public static float HorizontalRecoil(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[3], out float result) ? result : mod.Recoil;

        }

        public static float Dispersion(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[4], out float result) ? result : 0f;

        }

        public static float CameraRecoil(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[5], out float result) ? result : 0f;

        }

        public static float AutoROF(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[6], out float result) ? result : 0f;

        }

        public static float SemiROF(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[7], out float result) ? result : 0f;

        }

        public static float ModMalfunctionChance(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[8], out float result) ? result : 0f;

        }

        public static float ReloadSpeed(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[9], out float result) ? result : 0f;

        }

        public static float AimSpeed(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[10], out float result) ? result : 0f;

        }

        public static float ChamberSpeed(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[11], out float result) ? result : 0f;

        }

        public static float ModConvergence(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[12], out float result) ? result : 0f;

        }

        public static bool CanCylceSubs(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && bool.TryParse(mod.ConflictingItems[13], out bool result) ? result : false;

        }

        public static float RecoilAngle(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[14], out float result) ? result : 0f;

        }

        public static bool StockAllowADS(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && bool.TryParse(mod.ConflictingItems[15], out bool result) ? result : false;

        }

        public static float FixSpeed(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[16], out float result) ? result : 0f;

        }

        public static float ModShotDispersion(Mod mod)
        {
            return !Utils.NullCheck(mod.ConflictingItems) && float.TryParse(mod.ConflictingItems[17], out float result) ? result : 0f;

        }
    }
}
