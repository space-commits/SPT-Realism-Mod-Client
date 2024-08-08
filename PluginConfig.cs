using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace RealismMod
{
    public static class PluginConfig
    {
        //movement
        public static ConfigEntry<bool> EnableMaterialSpeed { get; set; }
        public static ConfigEntry<bool> EnableSlopeSpeed { get; set; }

        //attchment + recoil overhaul
        public static ConfigEntry<bool> EnableZeroShift { get; set; }
        public static ConfigEntry<bool> IncreaseCOI { get; set; }

        //malf changes
        public static ConfigEntry<float> DuraMalfThreshold { get; set; }

        //recoil
        public static ConfigEntry<float> ResetTime { get; set; }
        public static ConfigEntry<float> SwayIntensity { get; set; }
        public static ConfigEntry<float> ProceduralIntensity { get; set; }
        public static ConfigEntry<float> RecoilIntensity { get; set; }
        public static ConfigEntry<float> VertMulti { get; set; }
        public static ConfigEntry<float> HorzMulti { get; set; }
        public static ConfigEntry<float> DispMulti { get; set; }
        public static ConfigEntry<float> CamMulti { get; set; }
        public static ConfigEntry<float> CamWiggle { get; set; }
        public static ConfigEntry<float> CamReturn { get; set; }
        public static ConfigEntry<bool> EnableAngle { get; set; }
        public static ConfigEntry<float> RecoilAngleMulti { get; set; }
        public static ConfigEntry<float> ConvergenceMulti { get; set; }
        public static ConfigEntry<float> RecoilDampingMulti { get; set; }
        public static ConfigEntry<float> HandsDampingMulti { get; set; }
        public static ConfigEntry<bool> EnableCrank { get; set; }
        public static ConfigEntry<bool> EnableAdditionalRec { get; set; }
        public static ConfigEntry<float> VisRecoilMulti { get; set; }
        public static ConfigEntry<float> ResetSpeed { get; set; }
        public static ConfigEntry<float> RecoilClimbFactor { get; set; }
        public static ConfigEntry<float> PistolRecClimbFactor { get; set; }
        public static ConfigEntry<float> RecoilDispersionFactor { get; set; }
        public static ConfigEntry<float> RecoilDispersionSpeed { get; set; }
        public static ConfigEntry<float> RecoilSmoothness { get; set; }
        public static ConfigEntry<float> NewPOASensitivity { get; set; }
        public static ConfigEntry<float> ResetSensitivity { get; set; }
        public static ConfigEntry<float> AfterRecoilRandomness { get; set; }
        public static ConfigEntry<float> RecoilRandomness { get; set; }
        public static ConfigEntry<bool> ResetVertical { get; set; }
        public static ConfigEntry<bool> ResetHorizontal { get; set; }
        public static ConfigEntry<float> RecoilClimbLimit { get; set; }
        public static ConfigEntry<float> PlayerControlMulti { get; set; }
        public static ConfigEntry<bool> EnableHybridRecoil { get; set; }
        public static ConfigEntry<bool> EnableHybridReset { get; set; }
        public static ConfigEntry<bool> HybridForAll { get; set; }
        public static ConfigEntry<bool> EnableMuzzleEffects { get; set; }

        //stat display
        public static ConfigEntry<bool> ShowBalance { get; set; }
        public static ConfigEntry<bool> ShowCamRecoil { get; set; }
        public static ConfigEntry<bool> ShowDispersion { get; set; }
        public static ConfigEntry<bool> ShowRecoilAngle { get; set; }
        public static ConfigEntry<bool> ShowSemiROF { get; set; }

        //reloading
        public static ConfigEntry<float> PistolGlobalAimSpeedModifier { get; set; }
        public static ConfigEntry<float> GlobalAimSpeedModifier { get; set; }
        public static ConfigEntry<float> GlobalReloadSpeedMulti { get; set; }
        public static ConfigEntry<float> GlobalFixSpeedMulti { get; set; }
        public static ConfigEntry<float> GlobalUBGLReloadMulti { get; set; }
        public static ConfigEntry<float> GlobalRechamberSpeedMulti { get; set; }
        public static ConfigEntry<float> GlobalShotgunRackSpeedFactor { get; set; }
        public static ConfigEntry<float> GlobalCheckChamberSpeedMulti { get; set; }
        public static ConfigEntry<float> GlobalCheckChamberShotgunSpeedMulti { get; set; }
        public static ConfigEntry<float> GlobalCheckChamberPistolSpeedMulti { get; set; }
        public static ConfigEntry<float> GlobalCheckAmmoPistolSpeedMulti { get; set; }
        public static ConfigEntry<float> GlobalCheckAmmoMulti { get; set; }
        public static ConfigEntry<float> QuickReloadSpeedMulti { get; set; }
        public static ConfigEntry<float> InternalMagReloadMulti { get; set; }
        public static ConfigEntry<float> GlobalBoltSpeedMulti { get; set; }
        public static ConfigEntry<float> RechamberPistolSpeedMulti { get; set; }

        //deafen patches
        public static ConfigEntry<float> DeafRate { get; set; }
        public static ConfigEntry<float> DeafReset { get; set; }
        public static ConfigEntry<float> VigRate { get; set; }
        public static ConfigEntry<float> VigReset { get; set; }
        public static ConfigEntry<float> GainCutoff { get; set; }
        public static ConfigEntry<float> DeafenResetDelay { get; set; }
        public static ConfigEntry<float> RealTimeGain { get; set; }
        public static ConfigEntry<float> HeadsetAmbientMulti { get; set; }
        public static ConfigEntry<float> DryVolumeMulti { get; set; }
        public static ConfigEntry<float> HeadsetThreshold { get; set; }
        public static ConfigEntry<float> HeadsetAttack { get; set; }
        public static ConfigEntry<float> GunshotVolume { get; set; }
        public static ConfigEntry<float> PlayerMovementVolume { get; set; }
        public static ConfigEntry<float> NPCMovementVolume { get; set; }
        public static ConfigEntry<float> SharedMovementVolume { get; set; }
        public static ConfigEntry<float> ADSVolume { get; set; }
        public static ConfigEntry<KeyboardShortcut> IncGain { get; set; }
        public static ConfigEntry<KeyboardShortcut> DecGain { get; set; }

        //ballistics
        public static ConfigEntry<float> DragModifier { get; set; }
        public static ConfigEntry<bool> EnablePlateChanges { get; set; }
        public static ConfigEntry<float> GlobalDamageModifier { get; set; }
        public static ConfigEntry<bool> EnableBodyHitZones { get; set; }
        public static ConfigEntry<bool> EnableHitSounds { get; set; }
        public static ConfigEntry<float> FleshHitSoundMulti { get; set; }
        public static ConfigEntry<float> ArmorCloseHitSoundMulti { get; set; }
        public static ConfigEntry<float> ArmorFarHitSoundMulti { get; set; }
        public static ConfigEntry<bool> CanDisarmPlayer { get; set; }
        public static ConfigEntry<bool> CanDisarmBot { get; set; }
        public static ConfigEntry<float> DisarmBaseChance { get; set; }
        public static ConfigEntry<bool> CanFellPlayer { get; set; }
        public static ConfigEntry<bool> CanFellBot { get; set; }
        public static ConfigEntry<float> FallBaseChance { get; set; }
        public static ConfigEntry<bool> EnableAmmoStats { get; set; }
        public static ConfigEntry<float> RagdollForceModifier { get; set; }
        public static ConfigEntry<bool> EnableRagdollFix { get; set; }

        //hazard zones
        public static ConfigEntry<float> DeviceVolume { get; set; }
        public static ConfigEntry<float> GasMaskBreathVolume { get; set; }

        //medical0
        public static ConfigEntry<bool> EnableMedNotes { get; set; }
        public static ConfigEntry<bool> ResourceRateChanges { get; set; }
        public static ConfigEntry<float> EnergyRateMulti { get; set; }
        public static ConfigEntry<float> HydrationRateMulti { get; set; }
        public static ConfigEntry<bool> GearBlocksHeal { get; set; }
        public static ConfigEntry<bool> GearBlocksEat { get; set; }
        public static ConfigEntry<bool> EnableAdrenaline { get; set; }
        public static ConfigEntry<bool> EnableTrnqtEffect { get; set; }
        public static ConfigEntry<bool> EnableHealthEffects { get; set; }
        public static ConfigEntry<KeyboardShortcut> DropGearKeybind { get; set; }

        //stances
        public static ConfigEntry<KeyboardShortcut> ActiveAimKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> LowReadyKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> HighReadyKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> ShortStockKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> CycleStancesKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> MountKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> PatrolKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> MeleeKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> StanceWheelComboKeyBind { get; set; }
        public static ConfigEntry<bool> UseMouseWheelStance { get; set; }
        public static ConfigEntry<bool> UseMouseWheelPlusKey { get; set; }
        public static ConfigEntry<bool> EnableFSPatch { get; set; }
        public static ConfigEntry<bool> EnableNVGPatch { get; set; }
        public static ConfigEntry<bool> EnableMountUI { get; set; }
        public static ConfigEntry<bool> ToggleActiveAim { get; set; }
        public static ConfigEntry<bool> ActiveAimReload { get; set; }
        public static ConfigEntry<bool> EnableAltPistol { get; set; }
        public static ConfigEntry<bool> EnableAltRifle { get; set; }
        public static ConfigEntry<bool> EnableIdleStamDrain { get; set; }
        public static ConfigEntry<bool> EnableStanceStamChanges { get; set; }
        public static ConfigEntry<bool> EnableTacSprint { get; set; }
        public static ConfigEntry<bool> BlockFiring { get; set; }
        public static ConfigEntry<bool> RememberStance { get; set; }
        public static ConfigEntry<bool> EnableSprintPenalty { get; set; }
        public static ConfigEntry<bool> EnableMouseSensPenalty { get; set; }
        public static ConfigEntry<float> LeftShoulderOffset { get; set; }
        public static ConfigEntry<float> WeapOffsetX { get; set; }
        public static ConfigEntry<float> WeapOffsetY { get; set; }
        public static ConfigEntry<float> WeapOffsetZ { get; set; }
        public static ConfigEntry<float> StanceRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> StanceTransitionSpeedMulti { get; set; }
        public static ConfigEntry<float> ThirdPersonPositionSpeed { get; set; }
        public static ConfigEntry<float> ThirdPersonRotationSpeed { get; set; }
        public static ConfigEntry<float> ActiveAimRotationX { get; set; }
        public static ConfigEntry<float> ActiveAimRotationY { get; set; }
        public static ConfigEntry<float> ActiveAimRotationZ { get; set; }
        public static ConfigEntry<float> PistolRotationX { get; set; }
        public static ConfigEntry<float> PistolRotationY { get; set; }
        public static ConfigEntry<float> PistolRotationZ { get; set; }
        public static ConfigEntry<float> ActiveAimSpeedMulti { get; set; }
        public static ConfigEntry<float> ActiveAimResetSpeedMulti { get; set; }
        public static ConfigEntry<float> ActiveAimRotationMulti { get; set; }
        public static ConfigEntry<float> PistolRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> HighReadyRotationMulti { get; set; }
        public static ConfigEntry<float> LowReadyRotationMulti { get; set; }
        public static ConfigEntry<float> ShortStockRotationMulti { get; set; }
        public static ConfigEntry<float> ShortStockAdditionalRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> ActiveAimAdditionalRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> HighReadyAdditionalRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> LowReadyAdditionalRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> PistolAdditionalRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> ActiveAimResetRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> PistolResetRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationMulti { get; set; }
        public static ConfigEntry<float> LowReadyResetRotationMulti { get; set; }
        public static ConfigEntry<float> ShortStockResetRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> HighReadySpeedMulti { get; set; }
        public static ConfigEntry<float> HighReadyResetSpeedMulti { get; set; }
        public static ConfigEntry<float> LowReadySpeedMulti { get; set; }
        public static ConfigEntry<float> LowReadyResetSpeedMulti { get; set; }
        public static ConfigEntry<float> PistolPosSpeedMulti { get; set; }
        public static ConfigEntry<float> PistolPosResetSpeedMulti { get; set; }
        public static ConfigEntry<float> ShortStockSpeedMulti { get; set; }
        public static ConfigEntry<float> ShortStockResetSpeedMulti { get; set; }
        public static ConfigEntry<float> ActiveThirdPersonPositionX { get; set; }
        public static ConfigEntry<float> ActiveThirdPersonPositionY { get; set; }
        public static ConfigEntry<float> ActiveThirdPersonPositionZ { get; set; }
        public static ConfigEntry<float> ActiveThirdPersonRotationX { get; set; }
        public static ConfigEntry<float> ActiveThirdPersonRotationY { get; set; }
        public static ConfigEntry<float> ActiveThirdPersonRotationZ { get; set; }
        public static ConfigEntry<float> ActiveAimAdditionalRotationX { get; set; }
        public static ConfigEntry<float> ActiveAimAdditionalRotationY { get; set; }
        public static ConfigEntry<float> ActiveAimAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> ActiveAimResetRotationX { get; set; }
        public static ConfigEntry<float> ActiveAimResetRotationY { get; set; }
        public static ConfigEntry<float> ActiveAimResetRotationZ { get; set; }
        public static ConfigEntry<float> HighReadyThirdPersonPositionX { get; set; }
        public static ConfigEntry<float> HighReadyThirdPersonPositionY { get; set; }
        public static ConfigEntry<float> HighReadyThirdPersonPositionZ { get; set; }
        public static ConfigEntry<float> HighReadyThirdPersonRotationX { get; set; }
        public static ConfigEntry<float> HighReadyThirdPersonRotationY { get; set; }
        public static ConfigEntry<float> HighReadyThirdPersonRotationZ { get; set; }
        public static ConfigEntry<float> HighReadyAdditionalRotationX { get; set; }
        public static ConfigEntry<float> HighReadyAdditionalRotationY { get; set; }
        public static ConfigEntry<float> HighReadyAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationX { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationY { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationZ { get; set; }
        public static ConfigEntry<float> LowReadyThirdPersonPositionX { get; set; }
        public static ConfigEntry<float> LowReadyThirdPersonPositionY { get; set; }
        public static ConfigEntry<float> LowReadyThirdPersonPositionZ { get; set; }
        public static ConfigEntry<float> LowReadyThirdPersonRotationX { get; set; }
        public static ConfigEntry<float> LowReadyThirdPersonRotationY { get; set; }
        public static ConfigEntry<float> LowReadyThirdPersonRotationZ { get; set; }
        public static ConfigEntry<float> LowReadyAdditionalRotationX { get; set; }
        public static ConfigEntry<float> LowReadyAdditionalRotationY { get; set; }
        public static ConfigEntry<float> LowReadyAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> LowReadyResetRotationX { get; set; }
        public static ConfigEntry<float> LowReadyResetRotationY { get; set; }
        public static ConfigEntry<float> LowReadyResetRotationZ { get; set; }
        public static ConfigEntry<float> PistolAdditionalRotationX { get; set; }
        public static ConfigEntry<float> PistolAdditionalRotationY { get; set; }
        public static ConfigEntry<float> PistolAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> PistolResetRotationX { get; set; }
        public static ConfigEntry<float> PistolResetRotationY { get; set; }
        public static ConfigEntry<float> PistolResetRotationZ { get; set; }
        public static ConfigEntry<float> ShortStockAdditionalRotationX { get; set; }
        public static ConfigEntry<float> ShortStockAdditionalRotationY { get; set; }
        public static ConfigEntry<float> ShortStockAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> ShortStockResetRotationX { get; set; }
        public static ConfigEntry<float> ShortStockResetRotationY { get; set; }
        public static ConfigEntry<float> ShortStockResetRotationZ { get; set; }
        public static ConfigEntry<float> PistolThirdPersonPositionX { get; set; }
        public static ConfigEntry<float> PistolThirdPersonPositionY { get; set; }
        public static ConfigEntry<float> PistolThirdPersonPositionZ { get; set; }
        public static ConfigEntry<float> PistolThirdPersonRotationX { get; set; }
        public static ConfigEntry<float> PistolThirdPersonRotationY { get; set; }
        public static ConfigEntry<float> PistolThirdPersonRotationZ { get; set; }
        public static ConfigEntry<float> PistolOffsetX { get; set; }
        public static ConfigEntry<float> PistolOffsetY { get; set; }
        public static ConfigEntry<float> PistolOffsetZ { get; set; }
        public static ConfigEntry<float> ActiveAimOffsetX { get; set; }
        public static ConfigEntry<float> ActiveAimOffsetY { get; set; }
        public static ConfigEntry<float> ActiveAimOffsetZ { get; set; }
        public static ConfigEntry<float> LowReadyOffsetX { get; set; }
        public static ConfigEntry<float> LowReadyOffsetY { get; set; }
        public static ConfigEntry<float> LowReadyOffsetZ { get; set; }
        public static ConfigEntry<float> LowReadyRotationX { get; set; }
        public static ConfigEntry<float> LowReadyRotationY { get; set; }
        public static ConfigEntry<float> LowReadyRotationZ { get; set; }
        public static ConfigEntry<float> HighReadyOffsetX { get; set; }
        public static ConfigEntry<float> HighReadyOffsetY { get; set; }
        public static ConfigEntry<float> HighReadyOffsetZ { get; set; }
        public static ConfigEntry<float> HighReadyRotationX { get; set; }
        public static ConfigEntry<float> HighReadyRotationY { get; set; }
        public static ConfigEntry<float> HighReadyRotationZ { get; set; }
        public static ConfigEntry<float> ShortStockThirdPersonPositionX { get; set; }
        public static ConfigEntry<float> ShortStockThirdPersonPositionY { get; set; }
        public static ConfigEntry<float> ShortStockThirdPersonPositionZ { get; set; }
        public static ConfigEntry<float> ShortStockThirdPersonRotationX { get; set; }
        public static ConfigEntry<float> ShortStockThirdPersonRotationY { get; set; }
        public static ConfigEntry<float> ShortStockThirdPersonRotationZ { get; set; }
        public static ConfigEntry<float> ShortStockOffsetX { get; set; }
        public static ConfigEntry<float> ShortStockOffsetY { get; set; }
        public static ConfigEntry<float> ShortStockOffsetZ { get; set; }
        public static ConfigEntry<float> ShortStockRotationX { get; set; }
        public static ConfigEntry<float> ShortStockRotationY { get; set; }
        public static ConfigEntry<float> ShortStockRotationZ { get; set; }
        public static ConfigEntry<float> ShortStockReadyOffsetX { get; set; }
        public static ConfigEntry<float> ShortStockReadyOffsetY { get; set; }
        public static ConfigEntry<float> ShortStockReadyOffsetZ { get; set; }
        public static ConfigEntry<float> ShortStockReadyRotationX { get; set; }
        public static ConfigEntry<float> ShortStockReadyRotationY { get; set; }
        public static ConfigEntry<float> ShortStockReadyRotationZ { get; set; }

        //dev config options
        public static ConfigEntry<bool> ZoneDebug { get; set; }
        public static ConfigEntry<String> TargetZone { get; set; }
        public static ConfigEntry<bool> EnableLogging { get; set; }
        public static ConfigEntry<bool> EnableBallisticsLogging { get; set; }
        public static ConfigEntry<float> test1 { get; set; }
        public static ConfigEntry<float> test2 { get; set; }
        public static ConfigEntry<float> test3 { get; set; }
        public static ConfigEntry<float> test4 { get; set; }
        public static ConfigEntry<float> test5 { get; set; }
        public static ConfigEntry<float> test6 { get; set; }
        public static ConfigEntry<float> test7 { get; set; }
        public static ConfigEntry<float> test8 { get; set; }
        public static ConfigEntry<float> test9 { get; set; }
        public static ConfigEntry<float> test10 { get; set; }
        public static ConfigEntry<KeyboardShortcut> AddEffectKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> AddZone { get; set; }
        public static ConfigEntry<int> AddEffectBodyPart { get; set; }
        public static ConfigEntry<String> AddEffectType { get; set; }
    }
}
