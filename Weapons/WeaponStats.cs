﻿using EFT.InventoryLogic;
using System.Collections.Generic;

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
            return !Utils.IsNull(weapon.ConflictingItems) ? weapon.ConflictingItems[1] : "Unknown";

        }

        public static float BaseTorqueDistance(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[2], out float result) ? result : 0f;

        }

        public static bool WepHasShoulderContact(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && bool.TryParse(weapon.ConflictingItems[3], out bool result) ? result : false;

        }

        public static float BaseReloadSpeed(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[4], out float result) ? result : 1f;

        }

        public static string OperationType(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) ? weapon.ConflictingItems[5] : "Unknown";

        }

        public static float WeaponAccuracy(Weapon weapon)
        {
 
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[6], out float result) ? result : 0f;

        }

        public static float RecoilDamping(Weapon weapon)
        {;
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[7], out float result) ? result : 0f;

        }

        public static float RecoilHandDamping(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[8], out float result) ? result : 0.65f;

        }

        public static bool WeaponAllowsADS(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && bool.TryParse(weapon.ConflictingItems[9], out bool result) ? result : false;

        }

        public static float BaseChamberSpeed(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[10], out float result) ? result : 1f;

        }

        public static float MaxChamberSpeed(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[11], out float result) ? result : 1.2f;

        }

        public static float MinChamberSpeed(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[12], out float result) ? result : 0.7f;

        }

        public static bool IsManuallyOperated(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && bool.TryParse(weapon.ConflictingItems[13], out bool result) ? result : false;

        }

        public static float MaxReloadSpeed(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[14], out float result) ? result : 1.2f;

        }

        public static float MinReloadSpeed(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[15], out float result) ? result : 0.7f;

        }

        public static float BaseChamberCheckSpeed(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[16], out float result) ? result : 1f;

        }

        public static float BaseFixSpeed(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[17], out float result) ? result : 1f;

        }

        public static float CameraReturnSpeed(Weapon weapon)
        {
            return !Utils.IsNull(weapon.ConflictingItems) && float.TryParse(weapon.ConflictingItems[18], out float result) ? result : 0.1f;

        }

        public static float AdapterPistolGripBonusVRecoil
        {
            get { return -1f; }
        }

        public static float AdapterPistolGripBonusHRecoil
        {
            get { return -2f; }
        }

        public static float AdapterPistolGripBonusDispersion
        {
            get { return -1f; }
        }

        public static float AdapterPistolGripBonusChamber
        {
            get { return 10f; }
        }

        public static float AdapterPistolGripBonusErgo
        {
            get { return 2f; }
        }

        public static float PumpGripReloadBonus
        {
            get { return 18f; }
        }

        public static float FoldedErgoFactor
        {
            get { return 1f; }
        }

        public static float FoldedHRecoilFactor
        {
            get { return 1.15f; }
        }

        public static float FoldedVRecoilFactor
        {
            get { return 1.5f; }
        }

        public static float FoldedCOIFactor
        {
            get { return 2f; }
        }

        public static float FoldedCamRecoilFactor
        {
            get { return 0.4f; }
        }

        public static float FoldedDispersionFactor
        {
            get { return 1.55f; }
        }

        public static float FoldedRecoilAngleFactor
        {
            get { return 1.15f; }
        }

        public static float ErgoStatFactor
        {
            get { return 7f; }
        }

        public static float RecoilStatFactor
        {
            get { return 3.5f; }
        }

        public static float TotalWeaponWeight = 0f;

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

        public static string _WeapClass = "";

        public static bool ShouldGetSemiIncrease = false;

        public static float ErgoDelta = 0f;

        public static int AutoFireRate = 0;

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

        public static float InitDispersion = 0f;

        public static float InitRecoilAngle = 0f;

        public static float InitTotalCOI = 0f;

        public static string SavedInstanceID = "";

        public static float InitPureErgo = 0f;

        public static float PureRecoilDelta = 0f;

        public static float PureErgoDelta = 0f;

        public static string Placement = "";

        public static float ErgonomicWeight = 1f;

        public static float ErgoFactor = 1f;

        public static float ADSDelta = 0f;

        public static float TotalRecoilDamping;

        public static float TotalRecoilHandDamping;

        public static bool WeaponCanFSADS = false;

        public static bool Folded = false;

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

        public static bool HasBayonet = false;
        public static float BaseMeleeDamage = 0f;
        public static float BaseMeleePen = 0f;
    }
}