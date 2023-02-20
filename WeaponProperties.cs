using EFT.InventoryLogic;



namespace RealismMod
{


    public static class DisplayWeaponProperties
    {
        public static float ErgoDelta = 0;

        public static int AutoFireRate = 0;

        public static int SemiFireRate = 0;

        public static float Balance = 0;

        public static float VRecoilDelta = 0;

        public static float HRecoilDelta = 0;

        public static bool HasShoulderContact = true;

        public static float COIDelta = 0;

        public static float CamRecoil = 0;

        public static float Dispersion = 0;

        public static float RecoilAngle = 0;

        public static float TotalVRecoil = 0;

        public static float TotalHRecoil = 0;

        public static float TotalErgo = 0;

        public static float ErgnomicWeight = 0;
    }


    public static class WeaponProperties
    {

        public static string WeaponType(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return "";
            }
            return weapon.ConflictingItems[1];
        }

        public static float BaseTorqueDistance(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(weapon.ConflictingItems[2]);
        }

        public static bool WepHasShoulderContact(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(weapon.ConflictingItems[3]);
        }

        public static float BaseReloadSpeed(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return 1;
            }
            return float.Parse(weapon.ConflictingItems[4]);
        }

        public static string OperationType(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return "";
            }
            return weapon.ConflictingItems[5];
        }

        public static float WeaponAccuracy(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return 0;
            }
            return float.Parse(weapon.ConflictingItems[6]);
        }

        public static float RecoilDamping(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return 0.7f;
            }
            return float.Parse(weapon.ConflictingItems[7]);
        }

        public static float RecoilHandDamping(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return 0.65f;
            }
            return float.Parse(weapon.ConflictingItems[8]);
        }

        public static bool WeaponAllowsADS(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(weapon.ConflictingItems[9]);
        }

        public static float BaseChamberSpeed(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return 1;
            }
            return float.Parse(weapon.ConflictingItems[10]);
        }

        public static float MaxChamberSpeed(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return 1.2f;
            }
            return float.Parse(weapon.ConflictingItems[11]);
        }


        public static float MinChamberSpeed(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return 0.7f;
            }
            return float.Parse(weapon.ConflictingItems[12]);
        }

        public static bool IsManuallyOperated(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return false;
            }
            return bool.Parse(weapon.ConflictingItems[13]);
        }

        public static float MaxReloadSpeed(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return 1.2f;
            }
            return float.Parse(weapon.ConflictingItems[14]);
        }

        public static float MinReloadSpeed(Weapon weapon)
        {
            if (Helper.NullCheck(weapon.ConflictingItems))
            {
                return 0.7f;
            }
            return float.Parse(weapon.ConflictingItems[15]);
        }

        public static bool _IsManuallyOperated = false;

        public static float InternalMagReloadMulti = 1f;

        public static float QuickReloadSpeedMulti = 1.5f;

        public static float ArmHammerSpeedMulti = 1f;

        public static float CheckAmmoMulti = 1f;

        public static float CheckAmmoPistolSpeedMulti = 1f;

        public static float CheckChamberPistolSpeedMulti = 1f;

        public static float CheckChamberShotgunSpeedMulti = 1f;

        public static float CheckChamberSpeedMulti = 1f;

        public static float ShotgunRackSpeedFactor = 1;

        public static float RechamberSpeedMulti = 1f;

        public static float RechamberPistolSpeedMulti = 1f;

        public static float GlobalReloadSpeedMulti = 1f;

        public static float GlobalFixSpeedMulti = 1f;

        public static float GlobalUBGLReloadMulti = 1f;

        public static float GlobalBoltSpeedMulti = 1f;

        public static float TotalModDuraBurn = 1;

        public static float TotalMalfChance = 0;

        public static bool CanCycleSubs = false;

        public static string _WeapClass = "";

        public static bool ShouldGetSemiIncrease = false;

        public static float AdapterPistolGripBonusVRecoil = -1;

        public static float AdapterPistolGripBonusHRecoil = -2;

        public static float AdapterPistolGripBonusDispersion = -1;

        public static float AdapterPistolGripBonusChamber = 10;

        public static float AdapterPistolGripBonusErgo = 2;

        public static float PumpGripReloadBonus = 18f;

        public static float FoldedErgoFactor = 0.5f;

        public static float FoldedHRecoilFactor = 1.15f;

        public static float FoldedVRecoilFactor = 1.55f;

        public static float FoldedCOIFactor = 2f;

        public static float FoldedCamRecoilFactor = 0.5f;

        public static float FoldedDispersionFactor = 1.55f;

        public static float FoldedRecoilAngleFactor = 1.35f;

        public static float ErgoStatFactor = 7f;

        public static float RecoilStatFactor = 3.5f;

        public static float ErgoDelta = 0;

        public static int AutoFireRate = 0;

        public static int SemiFireRate = 0;

        public static float Balance = 0;

        public static float VRecoilDelta = 0;

        public static float HRecoilDelta = 0;

        public static bool HasShoulderContact = true;

        public static float ShotDispDelta = 0;

        public static float COIDelta = 0;

        public static float CamRecoil = 0;

        public static float Dispersion = 0;

        public static float RecoilAngle = 0;

        public static float TotalVRecoil = 0;

        public static float TotalHRecoil = 0;

        public static float TotalErgo = 0;

        public static float SDTotalErgo = 0;

        public static float SDTotalVRecoil = 0;

        public static float SDTotalHRecoil = 0;

        public static float SDBalance = 0;

        public static float SDCamRecoil = 0;

        public static float SDDispersion = 0;

        public static float SDRecoilAngle = 0;

        public static float SDTotalCOI = 0;

        public static string SavedInstanceID = "";

        public static float SDPureErgo = 0;

        public static float PureRecoilDelta = 0;

        public static float PureErgoDelta = 0;

        public static string Placement = "";

        public static float ErgonomicWeight = 1;

        public static float ADSDelta = 0;

        public static float TotalRecoilDamping;

        public static float TotalRecoilHandDamping;

        public static bool WeaponCanFSADS = false;

        public static bool Folded = false;

        public static float SDReloadSpeedModifier = 1f;

        public static float SDFixSpeedModifier = 1f;

        public static float TotalReloadSpeedLessMag = 1f;

        public static float TotalChamberSpeed = 1f;

        public static float SDChamberSpeedModifier = 1f;

        public static float AimMoveSpeedModifier = 1f;

        public static float AimSpeedModifier = 1f;

        public static float GlobalAimSpeedModifier = 1f;

        public static float AimSpeed = 1f;

        public static float CurrentMagReloadSpeed = 1f;
        public static float NewMagReloadSpeed = 1f;

        public static float ConvergenceChangeRate = 0.98f;
        public static float ConvergenceResetRate = 1.16f;
        public static float ConvergenceLimit = 0.3f;

        public static float CamRecoilChangeRate = 0.987f;
        public static float CamRecoilResetRate = 1.17f;
        public static float CamRecoilLimit = 0.45f;

        public static float VRecoilChangeRate = 1.005f;
        public static float VRecoilResetRate = 0.91f;
        public static float VRecoilLimit = 10;

        public static float HRecoilChangeRate = 1.005f;
        public static float HRecoilResetRate = 0.91f;
        public static float HRecoilLimit = 10;

        public static float DampingChangeRate = 0.98f;
        public static float DampingResetRate = 1.07f;
        public static float DampingLimit = 0.5f;

        public static float DispersionChangeRate = 0.95f;
        public static float DispersionResetRate = 1.05f;
        public static float DispersionLimit = 0.5f;
    }
}
