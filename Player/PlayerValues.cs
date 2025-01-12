using EFT;

namespace RealismMod
{
    public static class PlayerValues
    {
        public const float AIM_MOVE_SPEED_BASE = 0.5f;

        public static bool IsScav = false;

        public static float FixSkillMulti = 1f;

        public static float ReloadSkillMulti = 1f;

        public static float ReloadInjuryMulti = 1f;

        public static float ADSInjuryMulti = 1f;

        public static float StanceInjuryMulti = 1f;

        public static float RecoilInjuryMulti = 1f;

        public static float ImmuneSkillWeak = 0f;
        public static float ImmuneSkillStrong = 0f;

        public static float StressResistanceFactor = 0f;

        public static float VitalityFactorStrong = 0f;

        public static float AimMoveSpeedInjuryMulti = 1f;

        public static float ErgoDeltaInjuryMulti = 1f;

        public static float StrengthSkillAimBuff = 0f;

        public static float StrengthWeightBuff = 0f;

        public static float EnduranceSkill = 0f;

        public static bool IsAllowedADS = true;

        public static bool GearAllowsADS = true;

        public static float GearReloadMulti = 1f;

        public static float BaseSprintSpeed = 1f;

        public static EnvironmentType EnviroType = EnvironmentType.Outdoor;
        public static EPlayerBtrState BtrState = EPlayerBtrState.Outside;

        public static bool IsClearingMalf = false;

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

        public static float BaseStaminaPerc = 1f;
        public static float CombinedStaminaPerc= 1f;

        public static float RemainingArmStamFactor = 1f;

        public static float RemainingArmStamReloadFactor = 1f;

        public static float AimSkillADSBuff = 0f;

        public static float HealthSprintSpeedFactor = 1f;

        public static float HealthSprintAccelFactor = 1f;

        public static float HealthWalkSpeedFactor = 1f;

        public static float HealthStamRegenFactor = 1f;

        public static float HealthResourceRateFactor = 0f;

        public static bool IsSprinting = false;
        public static bool WasSprinting = false;

        public static bool SprintBlockADS = false;
        public static bool TriedToADSFromSprint = false;

        public static bool HasFullyResetSprintADSPenalties = false;

        public static float TotalModifiedWeight = 1f;
        public static float TotalMousePenalty = 1f;
        public static float TotalModifiedWeightMinusWeapon = 1f;

        public static bool IsMoving = false;

        public static float DeviceBonus = 1f;
        public static bool HasActiveDevice = false;
        public static bool IRLaserActive = false;
        public static bool IRLightActive = false;
        public static bool LaserActive = false;
        public static bool WhiteLightActive = false;

        public static float GearErgoPenalty = 1f;
        public static float GearSpeedPenalty = 1f;

        public static bool BlockFSWhileConsooming = false;

        public static bool IsInLastStand = false;
    }
}
