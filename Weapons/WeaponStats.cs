using EFT.InventoryLogic;
using System.Collections.Generic;
using UnityEngine;

namespace RealismMod
{


    public static class UIWeaponStats
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
        public static float CamReturnSpeed = 0;
        public static float TotalConvergence = 0;
        public static float ConvergenceDelta = 0;
    }


    public static class WeaponStats
    {

        public static string WeaponType(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) ? weapon.ConflictingItems[1] : "Unknown";

        }

        public static float BaseTorqueDistance(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[2], out float result) ? result : 0f;

        }

        public static bool WepHasShoulderContact(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && bool.TryParse(weapon.ConflictingItems[3], out bool result) ? result : false;

        }

        public static float BaseReloadSpeed(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[4], out float result) ? result : 1f;

        }

        public static string OperationType(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) ? weapon.ConflictingItems[5] : "Unknown";

        }

        public static float WeaponAccuracy(Weapon weapon)
        {
 
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[6], out float result) ? result : 0f;

        }

        public static float RecoilDamping(Weapon weapon)
        {;
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[7], out float result) ? result : 0f;

        }

        public static float RecoilHandDamping(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[8], out float result) ? result : 0.65f;

        }

        public static bool WeaponAllowsADS(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && bool.TryParse(weapon.ConflictingItems[9], out bool result) ? result : false;

        }

        public static float BaseChamberSpeed(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[10], out float result) ? result : 1f;

        }

        public static float MaxChamberSpeed(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[11], out float result) ? result : 1.2f;

        }

        public static float MinChamberSpeed(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[12], out float result) ? result : 0.7f;

        }

        public static bool IsManuallyOperated(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && bool.TryParse(weapon.ConflictingItems[13], out bool result) ? result : false;

        }

        public static float MaxReloadSpeed(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[14], out float result) ? result : 1.2f;

        }

        public static float MinReloadSpeed(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[15], out float result) ? result : 0.7f;

        }

        public static float BaseChamberCheckSpeed(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[16], out float result) ? result : 1f;

        }

        public static float BaseFixSpeed(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[17], out float result) ? result : 1f;

        }

        public static float VisualRecoilMulti(Weapon weapon)
        {
            return !Utils.IsConfItemNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[18], out float result) ? result : 0.1f;

        }

        public const float AdapterPistolGripBonusVRecoil = -1f;
        public const float AdapterPistolGripBonusHRecoil = -2f;
        public const float AdapterPistolGripBonusDispersion = -1f;
        public const float AdapterPistolGripBonusChamber = 20f;
        public const float AdapterPistolGripBonusErgo = 2f;
        public const float PumpGripReloadBonus = 20f;
        public const float FoldedErgoFactor = 0.85f;
        public const float FoldedHRecoilFactor = 1.15f;
        public const float FoldedVRecoilFactor = 1.65f;
        public const float FoldedCOIFactor = 2.5f;
        public const float FoldedCamRecoilFactor = 0.6f;
        public const float FoldedDispersionFactor = 1.75f;
        public const float FoldedRecoilAngleFactor = 1.15f;
        public const float FoldedConvergenceFactor = 0.7f;

        public static float BaseWeaponMotionIntensity = 1f;
        public static float WalkMotionIntensity = 1f;

        public static float TotalWeaponWeight = 0f;
        public static float TotalWeaponLength = 0f;

        public static float TotalCameraReturnSpeed = 0.1f;

        public static float BaseHipfireInaccuracy;

        public static float BaseWeaponLength = 0f;
        public static float NewWeaponLength = 0f;

        public static string WeapID = "";

        public static float TotalChamberCheckSpeed = 1;

        public static bool _IsManuallyOperated = false;

        public static float TotalModDuraBurn = 1;

        public static float TotalMalfChance = 0;
        public static float MalfChanceDelta = 0;

        public static bool CanCycleSubs = false;
        public static bool HasBooster = false;

        public static string _WeapClass = "";

        public static bool IsPistol = false;
        public static bool IsStocklessPistol = false;
        public static bool IsStockedPistol = false;
        public static bool IsMachinePistol = false;
        public static bool IsBullpup = false;

        public static bool ShouldGetSemiIncrease = false;

        public static float ErgoDelta = 0f;

        public static int AutoFireRate = 0;
        public static float FireRateDelta = 0;
        public static float AutoFireRateDelta = 0;
        public static float SemiFireRateDelta = 0;
        public static int SemiFireRate = 0;

        public static float Balance = 0f;

        public static float VRecoilDelta = 0f;

        public static float HRecoilDelta = 0f;

        public static bool HasShoulderContact = true;

        public static float ShotDispDelta = 0f;

        public static float COIDelta = 0f;

        public static float TotalCamRecoil = 0f;

        public static float TotalDispersion = 0f;
        public static float TotalDispersionDelta = 1f;

        public static float TotalRecoilAngle = 0f;

        public static float TotalVRecoil = 0f;

        public static float TotalHRecoil = 0f;

        public static float TotalErgo = 0f;

        public static float InitTotalErgo = 0f;

        public static float InitTotalVRecoil = 0f;

        public static float InitTotalHRecoil = 0f;

        public static float InitBalance = 0f;

        public static float InitCamRecoil = 0f;

        public static float TotalModdedConv = 0f;
        public static float ConvergenceDelta = 0f;

        public static float VelocityDelta = 0f;

        public static string Caliber = "";

        public static float MuzzleLoudness = 0f;

        public static float TotalMuzzleFlash = 0f;
        public static float TotalGas = 0f;

        public static bool HasMuzzleDevice = false;
        public static bool HasSuppressor = false;

        public static bool IsDirectImpingement = false;

        public static float InitDispersion = 0f;

        public static float InitRecoilAngle = 0f;

        public static float InitTotalCOI = 0f;

        public static string SavedInstanceID = "";

        public static float InitPureErgo = 0f;

        public static float PureRecoilDelta = 0f;

        public static float PureErgoDelta = 0f;

        public static float TotalWeaponHandlingModi = 0f;
        public static float TotalAimStabilityModi = 0f;

        public static string Placement = "";

        public static float ErgonomicWeight = 1f;

        public static float ErgoFactor = 1f;

        public static float ADSDelta = 0f;

        public static float TotalRecoilDamping;

        public static float TotalRecoilHandDamping;

        public static bool WeaponCanFSADS = false;

        public static float SDReloadSpeedModifier = 1f;

        public static float SDFixSpeedModifier = 1f;

        public static float TotalReloadSpeedLessMag = 1f;

        public static float TotalChamberSpeed = 1f;

        public static float TotalFixSpeed = 1f;

        public static float TotalFiringChamberSpeed = 1f;

        public static float SDChamberSpeedModifier = 1f;

        public static float AimMoveSpeedWeapModifier = 1f;

        public static float ModAimSpeedModifier = 1f;

        public static float GlobalAimSpeedModifier = 1f;

        public static float SightlessAimSpeed = 1f;

        public static float ErgoStanceSpeed = 1f;

        public static float CurrentMagReloadSpeed = 1f;
        public static float NewMagReloadSpeed = 1f;

        public static bool HasBipod = false;

        public static bool HasBayonet = false;
        public static float BaseMeleeDamage = 0f;
        public static float BaseMeleePen = 0f;

        public static float CurrentVisualRecoilMulti = 1f;

        public static Dictionary<string, Vector2> ZeroOffsetDict = new Dictionary<string, Vector2>();
        public static Vector2 ZeroRecoilOffset = Vector2.zero;
        public static float ScopeAccuracyFactor = 0f;
        public static string ScopeID = "";
        public static bool IsOptic = false;
    }
}
