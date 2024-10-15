using BepInEx.Configuration;
using System;
using UnityEngine;


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
        public static ConfigEntry<float> DuraMalfReductionThreshold { get; set; }
        public static ConfigEntry<float> DuraMalfThreshold { get; set; }
        public static ConfigEntry<float> MalfMulti { get; set; }

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
        public static ConfigEntry<bool> EnableTrueHazardRates { get; set; }
        public static ConfigEntry<KeyboardShortcut> MuteGeigerKey { get; set; }
        public static ConfigEntry<KeyboardShortcut> MuteGasAnalyserKey { get; set; }

        //medical
        public static ConfigEntry<bool> EnableMedNotes { get; set; }
        public static ConfigEntry<bool> ResourceRateChanges { get; set; }
        public static ConfigEntry<bool> PassiveRegen { get; set; }
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

        public static void InitConfigBindings(ConfigFile Config)
        {
            string testing = ".0. Testing";
            string miscSettings = ".1. Misc. Settings.";
            string ballSettings = ".2. Ballistics Settings.";
            string recoilSettings = ".3. Recoil Settings.";
            string advancedRecoilSettings = ".4. Advanced Recoil Settings.";
            string statSettings = ".5. Stat Display Settings.";
            string waponSettings = ".6. Weapon Settings.";
            string healthSettings = ".7. Health and Meds Settings.";
            string zoneSettings = ".8. Hazard Zone Settings.";
            string moveSettings = ".8. Movement Settings.";
            string deafSettings = ".9. Deafening and Audio.";
            string speed = "10. Weapon Speed Modifiers.";
            string weapAimAndPos = "11. Weapon Stances And Position.";
            string stanceBinds = "12. Weapon Stances Keybinds.";
            string activeAim = "13. Active Aim.";
            string highReady = "14. High Ready.";
            string lowReady = "15. Low Ready.";
            string pistol = "16. Pistol Position And Stance.";
            string shortStock = "17. Short-Stocking.";
            string thirdPerson = "18. Third Person Animations.";

            test1 = Config.Bind<float>(testing, "test 1", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 170, IsAdvanced = true, Browsable = true }));
            test2 = Config.Bind<float>(testing, "test 2", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 160, IsAdvanced = true, Browsable = true }));
            test3 = Config.Bind<float>(testing, "test 3", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 150, IsAdvanced = true, Browsable = true }));
            test4 = Config.Bind<float>(testing, "test 4", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 140, IsAdvanced = true, Browsable = true }));
            test5 = Config.Bind<float>(testing, "test 5", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true, Browsable = true }));
            test6 = Config.Bind<float>(testing, "test 6", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true, Browsable = true }));
            test7 = Config.Bind<float>(testing, "test 7", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true, Browsable = true }));
            test8 = Config.Bind<float>(testing, "test 8", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true, Browsable = true }));
            test9 = Config.Bind<float>(testing, "test 9", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 90, IsAdvanced = true, Browsable = true }));
            test10 = Config.Bind<float>(testing, "test 10", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 80, IsAdvanced = true, Browsable = true }));
            AddZone = Config.Bind(testing, "Create Debug Zone", new KeyboardShortcut(KeyCode.None), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 70, IsAdvanced = true, Browsable = true }));
            TargetZone = Config.Bind<string>(testing, "TargetZone", "", new ConfigDescription("DebugZone", null, new ConfigurationManagerAttributes { Order = 65, IsAdvanced = true, Browsable = true }));
            AddEffectType = Config.Bind<string>(testing, "Effect Type", "", new ConfigDescription("HeavyBleeding, LightBleeding, Fracture, removeHP, addHP.", null, new ConfigurationManagerAttributes { Order = 60, IsAdvanced = true, Browsable = true }));
            AddEffectBodyPart = Config.Bind<int>(testing, "Body Part Index", 1, new ConfigDescription("Head = 0, Chest = 1, Stomach = 2, Letft Arm, Right Arm, Left Leg, Right Leg, Common (whole body)", null, new ConfigurationManagerAttributes { Order = 50, IsAdvanced = true, Browsable = true }));
            AddEffectKeybind = Config.Bind(testing, "Add Effect Keybind", new KeyboardShortcut(KeyCode.None), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 40, IsAdvanced = true, Browsable = true }));
            ZoneDebug = Config.Bind<bool>(testing, "Enable Zone Debug", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30, IsAdvanced = true, Browsable = true }));
            EnableBallisticsLogging = Config.Bind<bool>(testing, "Enable Ballistics Logging", false, new ConfigDescription("Enables Logging For Debug And Dev", null, new ConfigurationManagerAttributes { Order = 20, IsAdvanced = true, Browsable = true }));
            EnableLogging = Config.Bind<bool>(testing, "Enable Logging", false, new ConfigDescription("Enables Logging For Debug And Dev", null, new ConfigurationManagerAttributes { Order = 10, IsAdvanced = true, Browsable = true }));

            RecoilIntensity = Config.Bind<float>(recoilSettings, "Recoil Intensity", 1.3f, new ConfigDescription("Changes The Overall Intenisty Of Recoil. This Will Increase/Decrease Horizontal Recoil, Dispersion, Vertical Recoil. Does Not Affect Recoil Climb Much, Mostly Spread And Visual.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 50, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            VertMulti = Config.Bind<float>(recoilSettings, "Vertical Recoil Multi.", 1.05f, new ConfigDescription("Up/Down. Will Also Increase Recoil Climb.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 40, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            HorzMulti = Config.Bind<float>(recoilSettings, "Horizontal Recoil Multi", 1.0f, new ConfigDescription("Forward/Back. Will Also Increase Weapon Shake While Firing.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 30, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            DispMulti = Config.Bind<float>(recoilSettings, "Dispersion Recoil Multi", 1.0f, new ConfigDescription("Spread. Will Also Increase S-Pattern Size.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 20, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            CamMulti = Config.Bind<float>(recoilSettings, "Camera Recoil Multi", 1.0f, new ConfigDescription("Visual Camera Recoil.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 10, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            EnableAngle = Config.Bind<bool>(recoilSettings, "Enable Recoil Angle", Plugin.ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Weapons Will Recoil At Different Angles, And Weight Out Front Will Make The Angle More Steep. If Disabled All Recoil Will Be At 90 Degrees.", null, new ConfigurationManagerAttributes { Order = 3, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            RecoilAngleMulti = Config.Bind<float>(recoilSettings, "Recoil Angle Multi", 1.0f, new ConfigDescription("Multiplier For Recoil Angle, Lower = Steeper Angle.", new AcceptableValueRange<float>(0.8f, 1.2f), new ConfigurationManagerAttributes { Order = 2, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ConvergenceMulti = Config.Bind<float>(recoilSettings, "Convergence Multi", 1.0f, new ConfigDescription("AKA Auto-Compensation. Higher = Snappier Recoil, Faster Reset And Tighter Recoil Pattern.", new AcceptableValueRange<float>(0f, 40f), new ConfigurationManagerAttributes { Order = 1, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));

            AfterRecoilRandomness = Config.Bind<float>(advancedRecoilSettings, "Reset Recoil Randomness Multi", 1f, new ConfigDescription("Higher = More Deviation From Point Of Aim After Firing", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { Order = 140, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            RecoilRandomness = Config.Bind<float>(advancedRecoilSettings, "Recoil Randomness", 2.8f, new ConfigDescription("Higher = Recoil Bounces Around More, More Erratic Recoil Pattern", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { Order = 135, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            CamReturn = Config.Bind<float>(advancedRecoilSettings, "Camera Recoil Speed", 0.07f, new ConfigDescription("Higher = More Faster Camera Recoil", new AcceptableValueRange<float>(0f, 0.5f), new ConfigurationManagerAttributes { Order = 132, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            CamWiggle = Config.Bind<float>(advancedRecoilSettings, "Camera Recoil Wiggle", 0.81f, new ConfigDescription("Higher = More Camera Wiggle", new AcceptableValueRange<float>(0f, 0.9f), new ConfigurationManagerAttributes { Order = 130, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            EnableAdditionalRec = Config.Bind<bool>(advancedRecoilSettings, "Enable Additional Visual Recoil", Plugin.ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Enables Additonal Visual Recoil Elements. Makes The Weapon Visually Move More In New Directions While Firing, Doesn't Have A Significant Effect On Spread.", null, new ConfigurationManagerAttributes { Order = 120, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            VisRecoilMulti = Config.Bind<float>(advancedRecoilSettings, "Visual Recoil Multi", 1f, new ConfigDescription("Multi For All Of The Mod's Visual Recoil Elements, Makes The Weapon Vibrate More While Firing. Visual Recoil Is Affected By Weapon Stats.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 110, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            EnableHybridRecoil = Config.Bind<bool>(advancedRecoilSettings, "Enable Hybrid Recoil System", false, new ConfigDescription("Combines Steady Recoil Climb With Auto-Compensation. If You Do Not Attempt To Control Recoil, Auto-Compensation Will Decrease Resulting In More Muzzle Flip. If You Control The Recoil, Auto-Comp Increases And Muzzle Flip Decreases.", null, new ConfigurationManagerAttributes { Order = 100, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            HybridForAll = Config.Bind<bool>(advancedRecoilSettings, "Enable Hybrid Recoil For All Weapons", false, new ConfigDescription("By Default This Hybrid System Is Only Enabled For Pistols And Stockless/Folded Stocked Weapons.", null, new ConfigurationManagerAttributes { Order = 90, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            EnableHybridReset = Config.Bind<bool>(advancedRecoilSettings, "Enable Recoil Reset For Hybrid Recoil", false, new ConfigDescription("Enables Recoil Reset For Pistols And Stockless/Folded Stocked Weapons That Are Using Hybrid Recoil, If The Other Reset Options Are Enabled.", null, new ConfigurationManagerAttributes { Order = 90, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            PlayerControlMulti = Config.Bind<float>(advancedRecoilSettings, "Player Control Strength.", 100f, new ConfigDescription("How Quickly The Weapon Responds To Mouse Input If Using The Hybrid Recoil System.", new AcceptableValueRange<float>(0f, 200f), new ConfigurationManagerAttributes { Order = 85, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ResetVertical = Config.Bind<bool>(advancedRecoilSettings, "Enable Vertical Reset", true, new ConfigDescription("Enables Weapon Reseting Back To Original Vertical Position.", null, new ConfigurationManagerAttributes { Order = 80, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ResetHorizontal = Config.Bind<bool>(advancedRecoilSettings, "Enable Horizontal Reset", false, new ConfigDescription("Enables Weapon Reseting Back To Original Horizontal Position.", null, new ConfigurationManagerAttributes { Order = 70, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ResetSpeed = Config.Bind<float>(advancedRecoilSettings, "Reset Speed", 0.002f, new ConfigDescription("How Fast The Weapon's Vertical Position Resets After Firing. Weapon's Convergence Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 60, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ResetSensitivity = Config.Bind<float>(advancedRecoilSettings, "Reset Sensitvity", 0.14f, new ConfigDescription("The Amount Of Mouse Movement Needed After Firing Needed To Cancel Reseting Back To Weapon's Original Position. Lower = Less Movement Needed.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 50, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            NewPOASensitivity = Config.Bind<float>(advancedRecoilSettings, "Reset Position Shift Sensitvity", 0.5f, new ConfigDescription("Multi For The Amount Of Mouse Movement Needed While Firing To Change The Position To Where Aim Will Reset After Firing. Lower = Less Movement Needed.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 45, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            RecoilSmoothness = Config.Bind<float>(advancedRecoilSettings, "Recoil Smoothness", 0.07f, new ConfigDescription("How Fast Recoil Moves Weapon While Firing, Higher Value Increases Smoothness.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 40, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            RecoilClimbFactor = Config.Bind<float>(advancedRecoilSettings, "Recoil Climb Multi.", 0.12f, new ConfigDescription("Multiplier For How Much Non-Pistols Climbs Vertically Per Shot. Weapon's Vertical Recoil Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 30, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            PistolRecClimbFactor = Config.Bind<float>(advancedRecoilSettings, "Pistol Recoil Climb Multi", 0.02f, new ConfigDescription("Multiplier For How Much Pistols Vertically Per Shot. Weapon's Vertical Recoil Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 29, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            RecoilClimbLimit = Config.Bind<float>(advancedRecoilSettings, "Recoil Climb Limit", 7f, new ConfigDescription("How Far Recoil Can Climb.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = 25, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            RecoilDispersionFactor = Config.Bind<float>(advancedRecoilSettings, "S-Pattern Multi.", 0.05f, new ConfigDescription("Increases The Size The Classic S Pattern. Weapon's Dispersion Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 20, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            RecoilDispersionSpeed = Config.Bind<float>(advancedRecoilSettings, "S-Pattern Speed Multi", 2f, new ConfigDescription("Increases The Speed At Which Recoil Makes The Classic S Pattern.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 10, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ResetTime = Config.Bind<float>(advancedRecoilSettings, "Reset Delay", 0.15f, new ConfigDescription("The Time In Seconds That Has To Be Elapsed Before Firing Is Considered Over, Recoil Will Not Reset Until It Is Over.", new AcceptableValueRange<float>(0.01f, 0.5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 4, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            EnableCrank = Config.Bind<bool>(advancedRecoilSettings, "Rearward Recoil", Plugin.ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Makes Recoil Go Towards Player's Shoulder Instead Of Forward.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            HandsDampingMulti = Config.Bind<float>(advancedRecoilSettings, "Rearward Recoil Wiggle Multi", 1f, new ConfigDescription("The Amount Of Rearward Wiggle After Firing.", new AcceptableValueRange<float>(0.1f, 1.5f), new ConfigurationManagerAttributes { Order = 2, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            RecoilDampingMulti = Config.Bind<float>(advancedRecoilSettings, "Vertical Recoil Wiggle Multi", 1f, new ConfigDescription("The Amount Of Vertical Wiggle After Firing.", new AcceptableValueRange<float>(0.1f, 1.5f), new ConfigurationManagerAttributes { Order = 1, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));

            EnableMaterialSpeed = Config.Bind<bool>(moveSettings, "Enable Ground Material Speed Modifier", Plugin.ServerConfig.movement_changes, new ConfigDescription("Enables Movement Speed Being Affected By Ground Material (Concrete, Grass, Metal, Glass Etc.)", null, new ConfigurationManagerAttributes { Order = 20, Browsable = Plugin.ServerConfig.movement_changes }));
            EnableSlopeSpeed = Config.Bind<bool>(moveSettings, "Enable Ground Slope Speed Modifier", false, new ConfigDescription("Enables Slopes Slowing Down Movement. Can Cause Random Speed Slowdowns In Some Small Spots Due To BSG's Bad Map Geometry.", null, new ConfigurationManagerAttributes { Order = 10, Browsable = Plugin.ServerConfig.movement_changes }));

            DeviceVolume = Config.Bind<float>(zoneSettings, "Device Volume", 1.2f, new ConfigDescription("Volume Modifier For Geiger And Gas Analyser.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 1, Browsable = Plugin.ServerConfig.enable_hazard_zones }));
            GasMaskBreathVolume = Config.Bind<float>(zoneSettings, "Gas Mask Breath Volume", 0.8f, new ConfigDescription("Volume Modifier For Gas Mask SFX.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 2, Browsable = Plugin.ServerConfig.enable_hazard_zones }));
            EnableTrueHazardRates = Config.Bind<bool>(zoneSettings, "Display True Hazard Rates", false, new ConfigDescription("Enable To Show The 'True' Hazard Rate, I.E Not Factoring Meds Or Gas Mask.", null, new ConfigurationManagerAttributes { Order = 3, Browsable = Plugin.ServerConfig.enable_hazard_zones }));
            MuteGasAnalyserKey = Config.Bind(zoneSettings, "Mute Gas Analysed Key", new KeyboardShortcut(KeyCode.M, new[] { KeyCode.LeftControl }), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 8, Browsable = Plugin.ServerConfig.headset_changes }));
            MuteGeigerKey = Config.Bind(zoneSettings, "Mute Geiger Key", new KeyboardShortcut(KeyCode.M, new[] { KeyCode.LeftAlt }), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 8, Browsable = Plugin.ServerConfig.headset_changes }));

            EnableMedNotes = Config.Bind<bool>(healthSettings, "Medical Notifications", Plugin.ServerConfig.med_changes, new ConfigDescription("Enables Notifications For Medical Status Effects, Healing Etc..", null, new ConfigurationManagerAttributes { Order = 130, Browsable = Plugin.ServerConfig.med_changes }));
            ResourceRateChanges = Config.Bind<bool>(healthSettings, "Enable Hydration/Energy Loss Rate Changes", Plugin.ServerConfig.med_changes, new ConfigDescription("Enables Changes To How Hydration And Energy Loss Rates Are Calculated. They Are Increased By Injuries, Drug Use, Sprinting And Weight.", null, new ConfigurationManagerAttributes { Order = 120, Browsable = Plugin.ServerConfig.med_changes }));
            PassiveRegen = Config.Bind<bool>(healthSettings, "Enable Passive Regen", Plugin.ServerConfig.med_changes, new ConfigDescription("Enables Regen Under Certain Conditions, And If THe Player Has Not Taken Damage For Some Time", null, new ConfigurationManagerAttributes { Order = 115, Browsable = Plugin.ServerConfig.med_changes }));
            HydrationRateMulti = Config.Bind<float>(healthSettings, "Hydration Drain Rate Multi.", 0.5f, new ConfigDescription("Lower = Less Drain", new AcceptableValueRange<float>(0.1f, 1.5f), new ConfigurationManagerAttributes { Order = 110, Browsable = Plugin.ServerConfig.med_changes }));
            EnergyRateMulti = Config.Bind<float>(healthSettings, "Energy Drain Rate Multi.", 0.3f, new ConfigDescription("Lower = Less Drain", new AcceptableValueRange<float>(0.1f, 1.5f), new ConfigurationManagerAttributes { Order = 100, Browsable = Plugin.ServerConfig.med_changes }));
            EnableTrnqtEffect = Config.Bind<bool>(healthSettings, "Enable Tourniquet Effect", Plugin.ServerConfig.med_changes, new ConfigDescription("Tourniquet Will Drain HP Of The Limb They Are Applied To.", null, new ConfigurationManagerAttributes { Order = 90, Browsable = Plugin.ServerConfig.med_changes }));
            GearBlocksEat = Config.Bind<bool>(healthSettings, "Gear Blocks Consumption", Plugin.ServerConfig.med_changes, new ConfigDescription("Gear Blocks Eating & Drinking. This Includes Some Masks & NVGs & Faceshields That Are Toggled On.", null, new ConfigurationManagerAttributes { Order = 80, Browsable = Plugin.ServerConfig.med_changes }));
            GearBlocksHeal = Config.Bind<bool>(healthSettings, "Gear Blocks Healing", false, new ConfigDescription("Gear Blocks Use Of Meds If The Wound Is Covered By It.", null, new ConfigurationManagerAttributes { Order = 70, Browsable = Plugin.ServerConfig.med_changes }));
            EnableAdrenaline = Config.Bind<bool>(healthSettings, "Adrenaline", Plugin.ServerConfig.med_changes, new ConfigDescription("If The Player Is Shot or Shot At They Will Get A Painkiller Effect, As Well As Tunnel Vision and Tremors. The Duration And Strength Of These Effects Are Determined By The Stress Resistence Skill.", null, new ConfigurationManagerAttributes { Order = 55, Browsable = Plugin.ServerConfig.med_changes }));
            DropGearKeybind = Config.Bind(healthSettings, "Remove Gear Keybind (Double Press)", new KeyboardShortcut(KeyCode.P), new ConfigDescription("Removes Any Gear That Is Blocking The Healing Of A Wound, It's A Double Press Like Bag Keybind Is.", null, new ConfigurationManagerAttributes { Order = 50, Browsable = Plugin.ServerConfig.med_changes }));

            EnableFSPatch = Config.Bind<bool>(miscSettings, "Enable Faceshield Patch", Plugin.ServerConfig.enable_stances, new ConfigDescription("Faceshields Block ADS Unless The Specfic Stock/Weapon/Faceshield Allows It.", null, new ConfigurationManagerAttributes { Order = 4, Browsable = Plugin.ServerConfig.enable_stances }));
            EnableNVGPatch = Config.Bind<bool>(miscSettings, "Enable NVG ADS Patch", Plugin.ServerConfig.enable_stances, new ConfigDescription("Magnified Optics Block ADS When Using NVGs.", null, new ConfigurationManagerAttributes { Order = 5, Browsable = Plugin.ServerConfig.enable_stances }));
            EnableMouseSensPenalty = Config.Bind<bool>(miscSettings, "Enable Weight Mouse Sensitivity Penalty", Plugin.ServerConfig.gear_weight, new ConfigDescription("Instead Of Using Gear Mouse Sens Penalty Stats, It Is Calculated Based On The Gear + Content's Weight As Modified By The Comfort Stat.", null, new ConfigurationManagerAttributes { Order = 20, Browsable = Plugin.ServerConfig.gear_weight }));
            EnableZeroShift = Config.Bind<bool>(miscSettings, "Enable Zero Shift", Plugin.ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Sights Simulate Losing Zero While Firing. The Reticle Has A Chance To Move Off Target. The Chance Is Determined By The Scope And Its Mount's Accuracy Stat, And The Weapon's Recoil. High Quality Scopes And Mounts Won't Lose Zero. SCAR-H Has Worse Zero-Shift.", null, new ConfigurationManagerAttributes { Order = 30, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));

            DragModifier = Config.Bind<float>(ballSettings, "Ballistic Coefficient Modifier", 1.25f, new ConfigDescription("Determines The Amount Of Drag On Projectiles. Higher Value = Slower Flight Time And More Drop.", new AcceptableValueRange<float>(0.5f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 150, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            GlobalDamageModifier = Config.Bind<float>(ballSettings, "Global Damage Modifier", 1f, new ConfigDescription("Lower = Less Damage Received (Except Head) For Bots And Player.", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 140, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            EnablePlateChanges = Config.Bind<bool>(ballSettings, "Enable Armor Plate Hitbox Changes", Plugin.ServerConfig.realistic_ballistics, new ConfigDescription("Reduces The Size Of Armor Plate Hitboxes To Be Closer To Real Life, And Closer To How They Were When First Implemented.", null, new ConfigurationManagerAttributes { Order = 130, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            EnableBodyHitZones = Config.Bind<bool>(ballSettings, "Enable Body Hit Zones", Plugin.ServerConfig.realistic_ballistics, new ConfigDescription("Divides Body Into A, C and D Hit Zones Like On IPSC Targets. In Addtion, There Are Upper Arm, Forearm, Thigh, Calf, Neck, Spine And Heart Hit Zones. Each Zone Modifies Damage And Bleed Chance. ", null, new ConfigurationManagerAttributes { Order = 120, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            EnableHitSounds = Config.Bind<bool>(ballSettings, "Enable Hit Sounds", Plugin.ServerConfig.realistic_ballistics, new ConfigDescription("Enables Additional Sounds To Be Played When Hitting The New Body Zones And Armor Hit Sounds By Material.", null, new ConfigurationManagerAttributes { Order = 110, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            FleshHitSoundMulti = Config.Bind<float>(ballSettings, "Flesh Hit Sound Multi", 1f, new ConfigDescription("Raises/Lowers New Hit Sounds Volume.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 100, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            ArmorCloseHitSoundMulti = Config.Bind<float>(ballSettings, "Close Armor Hit Sound Multi", 1f, new ConfigDescription("Raises/Lowers New Hit Sounds Volume.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 90, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            ArmorFarHitSoundMulti = Config.Bind<float>(ballSettings, "Distant Armor Hit Sound Mutli", 1f, new ConfigDescription("Raises/Lowers New Hit Sounds Volume.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 80, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            EnableRagdollFix = Config.Bind<bool>(ballSettings, "Enable Ragdoll Fix (Experimental)", false, new ConfigDescription("Requiures Restart. Enables Fix For Ragdolls Flying Into The Stratosphere.", null, new ConfigurationManagerAttributes { Order = 70, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            RagdollForceModifier = Config.Bind<float>(ballSettings, "Ragdoll Force Modifier", 1f, new ConfigDescription("Requires Ragdoll Fix To Be Enabled.", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 60, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            DisarmBaseChance = Config.Bind<float>(ballSettings, "Disarm Base Chance.", 1f, new ConfigDescription("The Base Chance To Be Disarmed. 1 = 1% Chance. This Value Is Increased By The Bullet's Kinetic Energy, Reduced By Armor Armor If Hit, And Doubled If Forearm Is Hit.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, IsAdvanced = true, Order = 50, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            FallBaseChance = Config.Bind<float>(ballSettings, "Fall Base Chance", 20f, new ConfigDescription("The Base Chance To Toggle Prone If Shot In Leg. 1 = 1% Chance. This Value Is Increased By The Bullet's Kinetic Energy And Doubled If Calf Is Hit.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, IsAdvanced = true, Order = 40, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            CanFellBot = Config.Bind<bool>(ballSettings, "Enable Bot Knockdown", Plugin.ServerConfig.realistic_ballistics, new ConfigDescription("If Hit In The Leg And The Leg Has/Will Have 0 HP, There Is A Chance That Prone Will Be Toggled. Chance Is Modified By Bullet Kinetic EnergyAnd Doubled If Calf Is Hit.", null, new ConfigurationManagerAttributes { Order = 30, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            CanFellPlayer = Config.Bind<bool>(ballSettings, "Enable Player Knockdown", Plugin.ServerConfig.realistic_ballistics, new ConfigDescription("If Hit In The Leg And The Leg Has/Will Have 0 HP, There Is A Chance That Prone Will Be Toggled. Chance Is Modified By Bullet Kinetic Energy And Doubled If Calf Is Hit.", null, new ConfigurationManagerAttributes { Order = 20, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            CanDisarmBot = Config.Bind<bool>(ballSettings, "Can Disarm Bot.", false, new ConfigDescription("If Hit In The Arms, There Is A Chance That The Currently Equipped Weapon Will Be Dropped. Chance Is Modified By Bullet Kinetic Energy And Reduced If Hit Arm Armor, And Doubled If Forearm Is Hit. WARNING: Disarmed Bots Will Become Passive And Not Attack Player, So This Is Disabled By Default.", null, new ConfigurationManagerAttributes { Order = 10, Browsable = Plugin.ServerConfig.realistic_ballistics }));
            CanDisarmPlayer = Config.Bind<bool>(ballSettings, "Can Disarm Player", Plugin.ServerConfig.realistic_ballistics, new ConfigDescription("If Hit In The Arms, There Is A Chance That The Currently Equipped Weapon Will Be Dropped. Chance Is Modified By Bullet Kinetic Energy And Reduced If Hit Arm Armor, And Doubled If Forearm Is Hit.", null, new ConfigurationManagerAttributes { Order = 1, Browsable = Plugin.ServerConfig.realistic_ballistics }));

            EnableAmmoStats = Config.Bind<bool>(statSettings, "Display Ammo Stats", Plugin.ServerConfig.realistic_ballistics, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 11, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ShowBalance = Config.Bind<bool>(statSettings, "Show Balance Stat", Plugin.ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 5, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ShowCamRecoil = Config.Bind<bool>(statSettings, "Show Camera Recoil Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 4, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ShowDispersion = Config.Bind<bool>(statSettings, "Show Dispersion Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 3, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ShowRecoilAngle = Config.Bind<bool>(statSettings, "Show Recoil Angle Stat", Plugin.ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use..", null, new ConfigurationManagerAttributes { Order = 2, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ShowSemiROF = Config.Bind<bool>(statSettings, "Show Semi Auto ROF Stat", Plugin.ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 1, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));

            EnableMuzzleEffects = Config.Bind<bool>(waponSettings, "Enable Muzzle Effects.", Plugin.ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Enanbes Changes To Muzzle Flash, Smoke, Etc. And Makes Their Intensity Dependent On Caliber, Weapon Condition, Attachments Etc.", null, new ConfigurationManagerAttributes { Order = 40, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            SwayIntensity = Config.Bind<float>(waponSettings, "Sway Intensity.", 1f, new ConfigDescription("Changes The Intensity Of Aim Sway.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 30, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            ProceduralIntensity = Config.Bind<float>(waponSettings, "Procedural Intensity.", 1f, new ConfigDescription("Changes The Intensity Of Procedural Animations, Including Sway, Weapon Movement, And Weapon Inertia.", new AcceptableValueRange<float>(0f, 3f), new ConfigurationManagerAttributes { Order = 20, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            DuraMalfReductionThreshold = Config.Bind<float>(waponSettings, "Malfunction Reduction Durability Threshold", 90f, new ConfigDescription("Malfunction Chance Is Significantly Reduced Until This Durability Threshold Is Exceeded.", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { Order = 10, Browsable = Plugin.ServerConfig.malf_changes }));
            DuraMalfThreshold = Config.Bind<float>(waponSettings, "Malfunction Durability Threshold", 95f, new ConfigDescription("Malfunction Chance Is Almost 0 Till This Durabiltiy Threshold Is Met, Unless One Of A Number Of Critera Or Thresholds Is Met (Heat, Burst Round Count, Mag Malf Chance, Ammo Malf Chance, Weapon Mod Malf Chance, Subsonic Ammo + Gun That Can't Cycle It, Etc.)", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { Order = 8, Browsable = Plugin.ServerConfig.malf_changes }));
            MalfMulti = Config.Bind<float>(waponSettings, "Malfunction Multi", 0.9f, new ConfigDescription("Malfunction Chance Multiplier.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 10, Browsable = Plugin.ServerConfig.malf_changes }));
            IncreaseCOI = Config.Bind<bool>(waponSettings, "Enable Increased Inaccuracy", Plugin.ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Requires Restart. Increases The Innacuracy Of All Weapons So That MOA/Accuracy Is A More Important Stat.", null, new ConfigurationManagerAttributes { Order = 1, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));

            DryVolumeMulti = Config.Bind<float>(deafSettings, "Headset Base Volume Reduction Multi", 1f, new ConfigDescription("Multi For How Much Headsets Reduce Audio Volume By, Not Including Gain", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 100, IsAdvanced = false, Browsable = Plugin.ServerConfig.headset_changes }));
            HeadsetThreshold = Config.Bind<float>(deafSettings, "Headset Cutoff Threshold Offset", -5f, new ConfigDescription("Threshold For How Loud Something Has To Be To Reduce Volume. Offset reduces or increases value. Lower Offset = More Sensitive. Offset Value of -5 Will Make It More Sensitive, A Value Of 5 Less.", new AcceptableValueRange<float>(-35f, -1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 90, IsAdvanced = false, Browsable = Plugin.ServerConfig.headset_changes }));
            HeadsetAttack = Config.Bind<float>(deafSettings, "Headset Attack", 1f, new ConfigDescription("How Quickly The Headset Will Start Reducing Volume. Lower = Faster.", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 80, IsAdvanced = false, Browsable = Plugin.ServerConfig.headset_changes }));
            HeadsetAmbientMulti = Config.Bind<float>(deafSettings, "Headset Ambient Modifier", 5f, new ConfigDescription("Adjusts The Ambient Volume Reduction From Headsets. Headset Gain Also Affects Ambient Volume. Higher = Louder.", new AcceptableValueRange<float>(-20f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 70, Browsable = Plugin.ServerConfig.headset_changes }));
            SharedMovementVolume = Config.Bind<float>(deafSettings, "Shared Movement Volume Multi", 1f, new ConfigDescription("Multiplier For Player + NPC Sprint Volume. Has To Be Shared Due To BSG Jank.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 60, IsAdvanced = false, Browsable = Plugin.ServerConfig.headset_changes }));
            NPCMovementVolume = Config.Bind<float>(deafSettings, "NPC Movement Volume Multi", 1f, new ConfigDescription("Multiplier For NPC Movement Volume. Includes Walking And Equipment Rattle.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 50, IsAdvanced = false, Browsable = Plugin.ServerConfig.headset_changes }));
            PlayerMovementVolume = Config.Bind<float>(deafSettings, "Player Movement Volume Multi", 1f, new ConfigDescription("Multiplier For Player Movment Volume.  Includes Walking And Equipment Rattle.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 40, IsAdvanced = false, Browsable = Plugin.ServerConfig.headset_changes }));
            ADSVolume = Config.Bind<float>(deafSettings, "ADS Volume Multi", 1f, new ConfigDescription("ADS Volume.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = false, Browsable = Plugin.ServerConfig.headset_changes }));
            GunshotVolume = Config.Bind<float>(deafSettings, "Gunshot Volume", -5f, new ConfigDescription("Offset For Volume Of Gunshots When Not Using Headsets. Lower = Quieter. Use Gain Cutoff For Headsets", new AcceptableValueRange<float>(-50f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = false, Browsable = Plugin.ServerConfig.headset_changes }));
            RealTimeGain = Config.Bind<float>(deafSettings, "Headset Gain", 9f, new ConfigDescription("WARNING: BE CAREFUL INCREASING THIS TOO HIGH! IT MAY DAMAGE YOUR HEARING! Adjusts The Gain Of Equipped Headsets In Real Time, Acts Just Like The Volume Control On IRL Ear Defenders.", new AcceptableValueRange<float>(0f, 30f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, Browsable = Plugin.ServerConfig.headset_changes }));
            GainCutoff = Config.Bind<float>(deafSettings, "Headset Gain Cutoff Multi", 0.75f, new ConfigDescription("How Much Headset Gain Is Reduced By While Firing. 0.75 = 25% Reduction.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, Browsable = Plugin.ServerConfig.headset_changes }));
            DeafenResetDelay = Config.Bind<float>(deafSettings, "Deafen Reset Delay", 0.5f, new ConfigDescription("How Long It Takes For Headset Gain To Be Restored Or Deafening Effects To Start Reseting", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, Browsable = Plugin.ServerConfig.headset_changes }));
            DecGain = Config.Bind(deafSettings, "Reduce Gain Keybind", new KeyboardShortcut(KeyCode.KeypadMinus), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 9, Browsable = Plugin.ServerConfig.headset_changes }));
            IncGain = Config.Bind(deafSettings, "Increase Gain Keybind", new KeyboardShortcut(KeyCode.KeypadPlus), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 8, Browsable = Plugin.ServerConfig.headset_changes }));
            DeafRate = Config.Bind<float>(deafSettings, "Deafen Rate", 0.006f, new ConfigDescription("How Quickly Player Gets Deafened. Higher = Faster.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7, IsAdvanced = true, Browsable = Plugin.ServerConfig.headset_changes }));
            DeafReset = Config.Bind<float>(deafSettings, "Deafen Reset Rate.", 0.025f, new ConfigDescription("How Quickly Player Regains Hearing. Higher = Faster.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true, Browsable = Plugin.ServerConfig.headset_changes }));
            VigRate = Config.Bind<float>(deafSettings, "Tunnel Effect Rate", 0.02f, new ConfigDescription("How Quickly Player Gets Tunnel Vission. Higher = Faster", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true, Browsable = Plugin.ServerConfig.headset_changes }));
            VigReset = Config.Bind<float>(deafSettings, "Tunnel Effect Reset Rate.", 0.035f, new ConfigDescription("How Quickly Player Recovers From Tunnel Vision. Higher = Faster", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true, Browsable = Plugin.ServerConfig.headset_changes }));

            PistolGlobalAimSpeedModifier = Config.Bind<float>(speed, "Pistol Aim Speed Multi.", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 17, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalAimSpeedModifier = Config.Bind<float>(speed, "Aim Speed Multi.", 1.2f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 16, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalReloadSpeedMulti = Config.Bind<float>(speed, "Magazine Reload Speed Multi", 1.125f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 15, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalFixSpeedMulti = Config.Bind<float>(speed, "Malfunction Fix Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 14, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalUBGLReloadMulti = Config.Bind<float>(speed, "UBGL Reload Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 13, IsAdvanced = true, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            RechamberPistolSpeedMulti = Config.Bind<float>(speed, "Pistol Rechamber Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalRechamberSpeedMulti = Config.Bind<float>(speed, "Rechamber Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalBoltSpeedMulti = Config.Bind<float>(speed, "Bolt Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalShotgunRackSpeedFactor = Config.Bind<float>(speed, "Shotgun Rack Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 9, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalCheckChamberSpeedMulti = Config.Bind<float>(speed, "Chamber Check Speed Multi", 1.25f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 8, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalCheckChamberShotgunSpeedMulti = Config.Bind<float>(speed, "Shotgun Chamber Check Speed Multi", 1.25f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7, IsAdvanced = true, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalCheckChamberPistolSpeedMulti = Config.Bind<float>(speed, "Pistol Chamber Check Speed Multi", 1.25f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalCheckAmmoPistolSpeedMulti = Config.Bind<float>(speed, "Pistol Check Ammo Multi", 1.25f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            GlobalCheckAmmoMulti = Config.Bind<float>(speed, "Check Ammo Multi.", 1.3f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            QuickReloadSpeedMulti = Config.Bind<float>(speed, "Quick Reload Multi", 1.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));
            InternalMagReloadMulti = Config.Bind<float>(speed, "Internal Magazine Reload", 1.15f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, Browsable = Plugin.ServerConfig.recoil_attachment_overhaul }));

            RememberStance = Config.Bind<bool>(weapAimAndPos, "Remember Stance", true, new ConfigDescription("Remember Stance After Actions (Using Items).", null, new ConfigurationManagerAttributes { Order = 260, Browsable = Plugin.ServerConfig.enable_stances }));
            BlockFiring = Config.Bind<bool>(weapAimAndPos, "Block Shooting While In Stance", false, new ConfigDescription("Blocks Firing While In A Stance, Will Cancel Stance If Attempting To Fire.", null, new ConfigurationManagerAttributes { Order = 250, Browsable = Plugin.ServerConfig.enable_stances }));
            EnableSprintPenalty = Config.Bind<bool>(weapAimAndPos, "Enable Sprint Aim Penalties", Plugin.ServerConfig.movement_changes, new ConfigDescription("ADS Out Of Sprint Has A Short Delay, Reduced Aim Speed And Increased Sway. The Longer You Sprint The Bigger The Penalty.", null, new ConfigurationManagerAttributes { Order = 240, Browsable = Plugin.ServerConfig.enable_stances }));
            EnableTacSprint = Config.Bind<bool>(weapAimAndPos, "Enable High Ready Sprint Animation", Plugin.ServerConfig.enable_stances, new ConfigDescription("Enables Usage Of High Ready Sprint Animation When Sprinting From High Ready Position.", null, new ConfigurationManagerAttributes { Order = 230, Browsable = Plugin.ServerConfig.enable_stances }));
            EnableAltPistol = Config.Bind<bool>(weapAimAndPos, "Enable Alternative Pistol Position And ADS", Plugin.ServerConfig.enable_stances, new ConfigDescription("Pistol Will Be Held Centered And In A Compressed Stance. ADS Will Be Animated.", null, new ConfigurationManagerAttributes { Order = 229, Browsable = Plugin.ServerConfig.enable_stances }));
            EnableAltRifle = Config.Bind<bool>(weapAimAndPos, "Enable Alternative Rifle Position And ADS (WIP)", false, new ConfigDescription("Rfile Will Move Closer To Camera When Aiming, Leading To Smoother ADS From Stances. Also Standardizes All Rifle Positions. Ignores 'Rifle Position' Settings.", null, new ConfigurationManagerAttributes { Order = 220, Browsable = Plugin.ServerConfig.enable_stances }));
            EnableIdleStamDrain = Config.Bind<bool>(weapAimAndPos, "Enable Idle Arm Stamina Drain", Plugin.ServerConfig.enable_stances, new ConfigDescription("Arm Stamina Will Drain When Not In A Stance (High And Low Ready, Short-Stocking).", null, new ConfigurationManagerAttributes { Order = 210, Browsable = Plugin.ServerConfig.enable_stances }));
            EnableStanceStamChanges = Config.Bind<bool>(weapAimAndPos, "Enable Stance Stamina And Movement Effects", Plugin.ServerConfig.enable_stances, new ConfigDescription("Enabled Stances And Mounting To Affect Stamina And Movement Speed. Stamina Drain May Not Work Correctly If Disabled. High + Low Ready, Short-Stocking And Pistol Idle Will Regenerate Stamina Faster And Optionally Idle With Rifles Drains Stamina. High Ready Has Faster Sprint Speed And Sprint Accel, Low Ready Has Faster Sprint Accel. Arm Stamina Won't Drain Regular Stamina If It Reaches 0.", null, new ConfigurationManagerAttributes { Order = 183, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimReload = Config.Bind<bool>(weapAimAndPos, "Allow Reload From Active Aim", false, new ConfigDescription("Allows Reload From Magazine While In Active Aim With Speed Bonus.", null, new ConfigurationManagerAttributes { Order = 190, Browsable = Plugin.ServerConfig.enable_stances }));
            EnableMountUI = Config.Bind<bool>(weapAimAndPos, "Enable Mounting UI", Plugin.ServerConfig.enable_stances, new ConfigDescription("If Enabled, An Icon On Screen Will Indicate If Player Is Bracing, Mounting And What Side Of Cover They Are On.", null, new ConfigurationManagerAttributes { Order = 179, Browsable = Plugin.ServerConfig.enable_stances }));
            LeftShoulderOffset = Config.Bind<float>(weapAimAndPos, "Left Shoulder Offset", -0.13f, new ConfigDescription("", new AcceptableValueRange<float>(-0.2f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 153, Browsable = Plugin.ServerConfig.enable_stances }));
            WeapOffsetX = Config.Bind<float>(weapAimAndPos, "Rifle Position X-Axis", -0.04f, new ConfigDescription("Adjusts The Starting Position Of Rifle On Screen If Alt Rifle Is Disabled", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 152, Browsable = Plugin.ServerConfig.enable_stances }));
            WeapOffsetY = Config.Bind<float>(weapAimAndPos, "Rifle Position Y-Axis", -0.015f, new ConfigDescription("Adjusts The Starting Position Of Rifle On Screen If Alt Rifle Is Disabled", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 151, Browsable = Plugin.ServerConfig.enable_stances }));
            WeapOffsetZ = Config.Bind<float>(weapAimAndPos, "Rifle Position Z-Axis", 0f, new ConfigDescription("Adjusts The Starting Position Of Rifle On Screen If Alt Rifle Is Disabled", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 150, Browsable = Plugin.ServerConfig.enable_stances }));
            StanceRotationSpeedMulti = Config.Bind<float>(weapAimAndPos, "Stance Rotation Speed Multi", 1f, new ConfigDescription("Adjusts The Speed Of Stance Rotation Changes.", new AcceptableValueRange<float>(0.1f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 146, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            StanceTransitionSpeedMulti = Config.Bind<float>(weapAimAndPos, "Stance Transition Speed.", 15.0f, new ConfigDescription("Adjusts The Position Change Speed Between Stances", new AcceptableValueRange<float>(1f, 35f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            CycleStancesKeybind = Config.Bind(stanceBinds, "Cycle Stances Keybind", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Cycles Between High, Low Ready and Short-Stocking. Double Click Returns To Idle.", null, new ConfigurationManagerAttributes { Order = 80, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimKeybind = Config.Bind(stanceBinds, "Active Aim Keybind", new KeyboardShortcut(KeyCode.Mouse4), new ConfigDescription("Cants The Weapon Sideways, Improving Hipfire Accuracy.", null, new ConfigurationManagerAttributes { Order = 90, Browsable = Plugin.ServerConfig.enable_stances }));
            ToggleActiveAim = Config.Bind<bool>(stanceBinds, "Use Toggle For Active Aim", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyKeybind = Config.Bind(stanceBinds, "High Ready Keybind", new KeyboardShortcut(KeyCode.Mouse3, new[] { KeyCode.LeftAlt }), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyKeybind = Config.Bind(stanceBinds, "Low Ready Keybind", new KeyboardShortcut(KeyCode.Mouse3, new[] { KeyCode.LeftControl }), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockKeybind = Config.Bind(stanceBinds, "Short-Stock Keybind", new KeyboardShortcut(KeyCode.J), new ConfigDescription("Tucks The Weapon's Stock Under Player's Arm, Shortening The Overall Length Of The Wweapon To Prevent Muzzle Being Pushed Away From Target.", null, new ConfigurationManagerAttributes { Order = 130, Browsable = Plugin.ServerConfig.enable_stances }));
            MountKeybind = Config.Bind(stanceBinds, "Mounting Keybind", new KeyboardShortcut(KeyCode.M), new ConfigDescription("Snaps To Cover To Improve Weapon Stability And Recoil, Toggle Only.", null, new ConfigurationManagerAttributes { Order = 140, Browsable = Plugin.ServerConfig.enable_stances }));
            PatrolKeybind = Config.Bind(stanceBinds, "Patrol/Neutral Stance Keybind", new KeyboardShortcut(KeyCode.K), new ConfigDescription("Puts The Weapon In A Neutral Position, Improving Arm Stam Regen And Walk Speed. For Maximum Larping.", null, new ConfigurationManagerAttributes { Order = 155, Browsable = Plugin.ServerConfig.enable_stances }));
            MeleeKeybind = Config.Bind(stanceBinds, "Melee Keybind", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Strike With Muzzle Or Bayonet Of Equipped Weapon.", null, new ConfigurationManagerAttributes { Order = 150, Browsable = Plugin.ServerConfig.enable_stances }));
            UseMouseWheelStance = Config.Bind<bool>(stanceBinds, "Enable Mouse Wheel Stance Switching", Plugin.ServerConfig.enable_stances, new ConfigDescription("Switches Between High And Low Ready Via Mouse Wheel.", null, new ConfigurationManagerAttributes { Order = 160, Browsable = Plugin.ServerConfig.enable_stances }));
            UseMouseWheelPlusKey = Config.Bind<bool>(stanceBinds, "Require Key + Mouse Wheel", Plugin.ServerConfig.enable_stances, new ConfigDescription("Require Keybind + Mouse Wheel To Change Stance.", null, new ConfigurationManagerAttributes { Order = 170, Browsable = Plugin.ServerConfig.enable_stances }));
            StanceWheelComboKeyBind = Config.Bind(stanceBinds, "Keybind To Use With Mouse Wheel", new KeyboardShortcut(KeyCode.LeftControl), new ConfigDescription("Key Used In Combination With Mouse Wheel If Enabled ", null, new ConfigurationManagerAttributes { Order = 180, Browsable = Plugin.ServerConfig.enable_stances }));

            ThirdPersonRotationSpeed = Config.Bind<float>(thirdPerson, "Third Person Rotation Speed Multi", 1.5f, new ConfigDescription("Speed Of Stance Rotation Change In Third Person.", new AcceptableValueRange<float>(0.1f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1000, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ThirdPersonPositionSpeed = Config.Bind<float>(thirdPerson, "Third Person Position Speed Multi", 1.0f, new ConfigDescription("Speed Of Stance Position Change In Third Person.", new AcceptableValueRange<float>(0.1f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1100, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            PistolThirdPersonPositionX = Config.Bind<float>(thirdPerson, "Pistol Third Person Position X-Axis", -0.03f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 260, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolThirdPersonPositionY = Config.Bind<float>(thirdPerson, "Pistol Third Person Position Y-Axis", 0.04f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 250, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolThirdPersonPositionZ = Config.Bind<float>(thirdPerson, "Pistol Third Person Position Z-Axis", -0.05f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 240, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolThirdPersonRotationX = Config.Bind<float>(thirdPerson, "Pistol Third Person Rotation X-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 230, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolThirdPersonRotationY = Config.Bind<float>(thirdPerson, "Pistol Third Person Rotation Y-Axis", -15f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 220, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolThirdPersonRotationZ = Config.Bind<float>(thirdPerson, "Pistol Third Person Rotation Z-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 210, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ShortStockThirdPersonPositionX = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Position X-Axis", 0.03f, new ConfigDescription("", new AcceptableValueRange<float>(-3f,3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 200, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockThirdPersonPositionY = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Position Y-Axis", 0.065f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 190, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockThirdPersonPositionZ = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Position Z-Axis", -0.075f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 180, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockThirdPersonRotationX = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Rotation X-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 170, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockThirdPersonRotationY = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Rotation Y-Axis", -15f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 160, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockThirdPersonRotationZ = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Rotation Z-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 150, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ActiveThirdPersonPositionX = Config.Bind<float>(thirdPerson, "Active Aim Third Person Position X-Axis", -0.02f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 140, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveThirdPersonPositionY = Config.Bind<float>(thirdPerson, "Active Aim Third Person Position Y-Axis", -0.02f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 130, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveThirdPersonPositionZ = Config.Bind<float>(thirdPerson, "Active Aim Third Person Position Z-Axis", 0.02f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 120, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveThirdPersonRotationX = Config.Bind<float>(thirdPerson, "Active Aim Third Person Rotation X-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveThirdPersonRotationY = Config.Bind<float>(thirdPerson, "Active Aim Third Person Rotation Y-Axis", -35f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 100, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveThirdPersonRotationZ = Config.Bind<float>(thirdPerson, "Active Aim Third Person Rotation Z-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 90, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            HighReadyThirdPersonPositionX = Config.Bind<float>(thirdPerson, "High Ready Third Person Position X-Axis", 0.02f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 80, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyThirdPersonPositionY = Config.Bind<float>(thirdPerson, "High Ready Third Person Position Y-Axis", 0.05f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 70, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyThirdPersonPositionZ = Config.Bind<float>(thirdPerson, "High Ready Third Person Position Z-Axis", -0.045f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 60, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyThirdPersonRotationX = Config.Bind<float>(thirdPerson, "High Ready Third Person Rotation X-Axis", -8f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 50, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyThirdPersonRotationY = Config.Bind<float>(thirdPerson, "High Ready Third Person Rotation Y-Axis", -25f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 40, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyThirdPersonRotationZ = Config.Bind<float>(thirdPerson, "High Ready Third Person Rotation Z-Axis", -0f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            LowReadyThirdPersonPositionX = Config.Bind<float>(thirdPerson, "Low Ready Third Person Position X-Axis", 0.01f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 20, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyThirdPersonPositionY = Config.Bind<float>(thirdPerson, "Low Ready Third Person Position Y-Axis", -0.025f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyThirdPersonPositionZ = Config.Bind<float>(thirdPerson, "Low Ready Third Person Position Z-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-3f, 3f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 9, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyThirdPersonRotationX = Config.Bind<float>(thirdPerson, "Low Ready Third Person Rotation X-Axis", 24f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 8, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyThirdPersonRotationY = Config.Bind<float>(thirdPerson, "Low Ready Third Person Rotation Y-Axis", 10f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyThirdPersonRotationZ = Config.Bind<float>(thirdPerson, "Low Ready Third Person Rotation Z-Axis", -1f, new ConfigDescription("", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ActiveAimAdditionalRotationSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Additonal Rotation Speed Multi.", 2f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimResetRotationSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Reset Rotation Speed Multi.", 3.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimRotationMulti = Config.Bind<float>(activeAim, "Active Aim Rotation Speed Multi.", 2f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 144, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Speed Multi", 15f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 143, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimResetSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Reset Speed Multi", 6f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 142, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ActiveAimOffsetX = Config.Bind<float>(activeAim, "Active Aim Position X-Axis", -0.02f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 135, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimOffsetY = Config.Bind<float>(activeAim, "Active Aim Position Y-Axis", 0.008f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 134, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimOffsetZ = Config.Bind<float>(activeAim, "Active Aim Position Z-Axis", 0f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 133, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ActiveAimRotationX = Config.Bind<float>(activeAim, "Active Aim Rotation X-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 122, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimRotationY = Config.Bind<float>(activeAim, "Active Aim Rotation Y-Axis", -35.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 121, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimRotationZ = Config.Bind<float>(activeAim, "Active Aim Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 120, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ActiveAimAdditionalRotationX = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation X-Axis", 0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 111, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimAdditionalRotationY = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation Y-Axis", -35f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimAdditionalRotationZ = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation Z-Axis", 0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ActiveAimResetRotationX = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation X-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 102, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimResetRotationY = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation Y-Axis.", 20.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 101, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ActiveAimResetRotationZ = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation Z-Axis", -1f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 100, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            HighReadyAdditionalRotationSpeedMulti = Config.Bind<float>(highReady, "High Ready Additonal Rotation Speed Multi.", 0.1f, new ConfigDescription("How Fast The Weapon Rotates Going Out Of Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 94, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyResetRotationMulti = Config.Bind<float>(highReady, "High Ready Reset Rotation Speed Multi.", 1.5f, new ConfigDescription("How Fast The Weapon Rotates Going Out Of Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 93, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyRotationMulti = Config.Bind<float>(highReady, "High Ready Rotation Speed Multi.", 2f, new ConfigDescription("How Fast The Weapon Rotates Going Into Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 92, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyResetSpeedMulti = Config.Bind<float>(highReady, "High Ready Reset Speed Multi", 6.5f, new ConfigDescription("How Fast The Weapon Moves Going Out Of Stance", new AcceptableValueRange<float>(1f, 100.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 91, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadySpeedMulti = Config.Bind<float>(highReady, "High Ready Speed Multi", 6f, new ConfigDescription("How Fast The Weapon Moves Going Into Stance", new AcceptableValueRange<float>(1f, 100.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 90, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            HighReadyOffsetX = Config.Bind<float>(highReady, "High Ready Position X-Axis", 0.005f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 85, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyOffsetY = Config.Bind<float>(highReady, "High Ready Position Y-Axis", 0.035f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 84, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyOffsetZ = Config.Bind<float>(highReady, "High Ready Position Z-Axis", -0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 83, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            HighReadyRotationX = Config.Bind<float>(highReady, "High Ready Rotation X-Axis", -8.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 72, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyRotationY = Config.Bind<float>(highReady, "High Ready Rotation Y-Axis", -20.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 71, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyRotationZ = Config.Bind<float>(highReady, "High Ready Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 70, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            HighReadyAdditionalRotationX = Config.Bind<float>(highReady, "High Ready Additional Rotation X-Axis", -50.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 69, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyAdditionalRotationY = Config.Bind<float>(highReady, "High Ready Additiona Rotation Y-Axis", -25f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 68, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyAdditionalRotationZ = Config.Bind<float>(highReady, "High Ready Additional Rotation Z-Axis", -5f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 67, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            HighReadyResetRotationX = Config.Bind<float>(highReady, "High Ready Reset Rotation X-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 66, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyResetRotationY = Config.Bind<float>(highReady, "High Ready Reset Rotation Y-Axis", 2f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 65, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            HighReadyResetRotationZ = Config.Bind<float>(highReady, "High Ready Reset Rotation Z-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 64, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            LowReadyAdditionalRotationSpeedMulti = Config.Bind<float>(lowReady, "Low Ready Additonal Rotation Speed Multi", 0.5f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 64, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyResetRotationMulti = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Speed Multi", 1.7f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 63, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyRotationMulti = Config.Bind<float>(lowReady, "Low Ready Rotation Speed Multi", 1.5f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 62, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadySpeedMulti = Config.Bind<float>(lowReady, "Low Ready Speed Multi.", 14f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 61, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyResetSpeedMulti = Config.Bind<float>(lowReady, "Low Ready Reset Speed Multi", 8.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 60, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            LowReadyOffsetX = Config.Bind<float>(lowReady, "Low Ready Position X-Axis", 0f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 55, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyOffsetY = Config.Bind<float>(lowReady, "Low Ready Position Y-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 54, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyOffsetZ = Config.Bind<float>(lowReady, "Low Ready Position Z-Axis", 0.0f, new ConfigDescription("Weapon Position When In Stance..", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 53, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            LowReadyRotationX = Config.Bind<float>(lowReady, "Low Ready Rotation X-Axis", 8f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 42, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyRotationY = Config.Bind<float>(lowReady, "Low Ready Rotation Y-Axis", -5.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 41, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyRotationZ = Config.Bind<float>(lowReady, "Low Ready Rotation Z-Axis", -1.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 40, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            LowReadyAdditionalRotationX = Config.Bind<float>(lowReady, "Low Ready Additional Rotation X-Axis", 12.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 39, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyAdditionalRotationY = Config.Bind<float>(lowReady, "Low Ready Additional Rotation Y-Axis", -1f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 38, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyAdditionalRotationZ = Config.Bind<float>(lowReady, "Low Ready Additional Rotation Z-Axis", 0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 37, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            LowReadyResetRotationX = Config.Bind<float>(lowReady, "Low Ready Reset Rotation X-Axis", -1.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 36, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyResetRotationY = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Y-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            LowReadyResetRotationZ = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Z-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            PistolAdditionalRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Additional Rotation Speed Multi", 0.1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolResetRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Reset Rotation Speed Multi", 2f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Rotation Speed Multi", 1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 33, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolPosSpeedMulti = Config.Bind<float>(pistol, "Pistol Position Speed Multi", 6.0f, new ConfigDescription("", new AcceptableValueRange<float>(1.0f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 32, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolPosResetSpeedMulti = Config.Bind<float>(pistol, "Pistol Position Reset Speed Multi", 16.0f, new ConfigDescription("", new AcceptableValueRange<float>(1.0f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            PistolOffsetX = Config.Bind<float>(pistol, "Pistol Position X-Axis.", 0f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 25, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolOffsetY = Config.Bind<float>(pistol, "Pistol Position Y-Axis.", 0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 24, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolOffsetZ = Config.Bind<float>(pistol, "Pistol Position Z-Axis.", -0.015f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 23, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            PistolRotationX = Config.Bind<float>(pistol, "Pistol Rotation X-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolRotationY = Config.Bind<float>(pistol, "Pistol Rotation Y-Axis", -5f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolRotationZ = Config.Bind<float>(pistol, "Pistol Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            PistolAdditionalRotationX = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation X-Axis.", 0.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolAdditionalRotationY = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation Y-Axis.", 0.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolAdditionalRotationZ = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation Z-Axis.", 0.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            PistolResetRotationX = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation X-Axis", -1f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolResetRotationY = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation Y-Axis", 0.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            PistolResetRotationZ = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ShortStockAdditionalRotationSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Additional Rotation Speed Multi", 1.5f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockResetRotationSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Reset Rotation Speed Multi", 1.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockRotationMulti = Config.Bind<float>(shortStock, "Short-Stock Rotation Speed Multi", 1.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 33, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Position Speed Multi.", 4f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 32, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockResetSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Position Reset Speed Mult", 3.8f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ShortStockOffsetX = Config.Bind<float>(shortStock, "Short-Stock Position X-Axis", 0.02f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 25, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockOffsetY = Config.Bind<float>(shortStock, "Short-Stock Position Y-Axis", 0.1f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 24, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockOffsetZ = Config.Bind<float>(shortStock, "Short-Stock Position Z-Axis", -0.025f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 23, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ShortStockRotationX = Config.Bind<float>(shortStock, "Short-Stock Rotation X-Axis", 0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockRotationY = Config.Bind<float>(shortStock, "Short-Stock Rotation Y-Axis", -15.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockRotationZ = Config.Bind<float>(shortStock, "Short-Stock Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ShortStockAdditionalRotationX = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation X-Axis.", -3.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockAdditionalRotationY = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation Y-Axis.", -15.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockAdditionalRotationZ = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation Z-Axis.", 1.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));

            ShortStockResetRotationX = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation X-Axis", -1.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockResetRotationY = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation Y-Axis", 2f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
            ShortStockResetRotationZ = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation Z-Axis", 0.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-300f, 300f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true, Browsable = Plugin.ServerConfig.enable_stances }));
        }
    }
}
