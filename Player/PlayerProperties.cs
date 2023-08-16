using EFT;

namespace RealismMod
{
    public static class PlayerProperties
    {
        public static float FixSkillMulti = 1f;

        public static float ReloadSkillMulti = 1f;

        public static float ReloadInjuryMulti = 1f;

        public static float ADSInjuryMulti = 1f;

        public static float StanceInjuryMulti = 1f;

        public static float RecoilInjuryMulti = 1f;

        public static float AimMoveSpeedBase = 0.55f;

        public static float AimMoveSpeedInjuryMulti = 1f;

        public static float ErgoDeltaInjuryMulti = 1f;

        public static float StrengthSkillAimBuff = 0f;

        public static bool IsAllowedADS = true;

        public static bool GearAllowsADS = true;

        public static float GearReloadMulti = 1f;

        public static float BaseSprintSpeed = 1f;

        public static EnvironmentType enviroType;

        public static bool IsClearingMalf;

        public static bool IsAllowedAim = true;

        public static bool IsAttemptingToReloadInternalMag = false;

        public static bool IsMagReloading = false;

        public static bool IsInReloadOpertation = false;

        public static bool IsQuickReloading = false;

        public static bool NoCurrentMagazineReload = false;

        public static bool IsAttemptingRevolverReload = false;

        public static float TotalHandsIntensity = 1f;
        public static float TotalBreathIntensity = 1f;

        public static float SprintTotalHandsIntensity = 1f;
        public static float SprintTotalBreathIntensity = 1f;
        public static float SprintHipfirePenalty = 1f;

        public static float ADSSprintMulti = 1f;

        public static float WeaponSkillErgo = 0f;

        public static float RemainingArmStamPercentage = 1f;

        public static float AimSkillADSBuff = 0f;

        public static bool RightArmRuined = false;

        public static bool LeftArmRuined = false;

        public static float HealthSprintSpeedFactor = 1f;

        public static float HealthSprintAccelFactor = 1f;

        public static float HealthWalkSpeedFactor = 1f;

        public static float HealthStamRegenFactor = 1f;

        public static float HealthResourceRateFactor = 0f;

        public static float StressResistanceFactor = 0f;

        public static bool IsSprinting = false;
        public static bool WasSprinting = false;

        public static bool SprintBlockADS = false;
        public static bool TriedToADSFromSprint = false;

        public static bool HasFullyResetSprintADSPenalties = false;

        public static float TotalUnmodifiedWeight = 0f;
        public static float TotalModifiedWeight = 0f;
        public static float TotalModifiedWeightMinusWeapon = 0f;
    }
}
