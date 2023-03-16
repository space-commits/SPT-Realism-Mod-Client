using Aki.Common.Http;
using Aki.Common.Utils;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using EFT.InventoryLogic;
using EFT.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static RealismMod.ArmorPatches;
using static RealismMod.Attributes;


namespace RealismMod
{

    public class ConfigTemplate
    {
        public bool recoil_attachment_overhaul { get; set; }
        public bool malf_changes { get; set; }
        public bool realistic_ballistics { get; set; }
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> resetTime { get; set; }
        public static ConfigEntry<float> vRecoilLimit { get; set; }
        public static ConfigEntry<float> hRecoilLimit { get; set; }
        public static ConfigEntry<float> convergenceLimit { get; set; }
        public static ConfigEntry<float> convergenceResetRate { get; set; }
        public static ConfigEntry<float> vRecoilChangeMulti { get; set; }
        public static ConfigEntry<float> vRecoilResetRate { get; set; }
        public static ConfigEntry<float> hRecoilChangeMulti { get; set; }
        public static ConfigEntry<float> hRecoilResetRate { get; set; }
        public static ConfigEntry<float> SensChangeRate { get; set; }
        public static ConfigEntry<float> SensResetRate { get; set; }
        public static ConfigEntry<float> SensLimit { get; set; }
        public static ConfigEntry<bool> showBalance { get; set; }
        public static ConfigEntry<bool> showCamRecoil { get; set; }
        public static ConfigEntry<bool> showDispersion { get; set; }
        public static ConfigEntry<bool> showRecoilAngle { get; set; }
        public static ConfigEntry<bool> showSemiROF { get; set; }
        public static ConfigEntry<bool> EnableFSPatch { get; set; }
        public static ConfigEntry<bool> EnableNVGPatch { get; set; }
        public static ConfigEntry<bool> EnableMalfPatch { get; set; }
        public static ConfigEntry<bool> InspectionlessMalfs { get; set; }
        public static ConfigEntry<bool> enableSGMastering { get; set; }
        public static ConfigEntry<bool> EnableProgramK { get; set; }
        public static ConfigEntry<bool> EnableAmmoFirerateDisp { get; set; }
        public static ConfigEntry<bool> enableAmmoDamageDisp { get; set; }
        public static ConfigEntry<bool> enableAmmoPenDisp { get; set; }
        public static ConfigEntry<bool> enableAmmoArmorDamageDisp { get; set; }
        public static ConfigEntry<bool> enableAmmoFragDisp { get; set; }
        public static ConfigEntry<bool> EnableReloadPatches { get; set; }
        public static ConfigEntry<bool> EnableRealArmorClass { get; set; }
        public static ConfigEntry<bool> ReduceCamRecoil { get; set; }
        public static ConfigEntry<float> ConvergenceSpeedCurve { get; set; }
        public static ConfigEntry<bool> EnableDeafen { get; set; }
        public static ConfigEntry<bool> EnableHoldBreath { get; set; }
        public static ConfigEntry<float> DuraMalfThreshold { get; set; }
        public static ConfigEntry<bool> EnableRecoilClimb { get; set; }
        public static ConfigEntry<float> SwayIntensity { get; set; }
        public static ConfigEntry<float> RecoilIntensity { get; set; }
        public static ConfigEntry<bool> EnableStatsDelta { get; set; }
        public static ConfigEntry<bool> EnableHipfireRecoilClimb { get; set; }
        public static ConfigEntry<bool> IncreaseCOI { get; set; }
        public static ConfigEntry<float> DeafRate { get; set; }
        public static ConfigEntry<float> DeafReset { get; set; }
        public static ConfigEntry<float> VigRate { get; set; }
        public static ConfigEntry<float> VigReset { get; set; }
        public static ConfigEntry<float> DistRate { get; set; }
        public static ConfigEntry<float> DistReset { get; set; }
        public static ConfigEntry<float> GainReduc { get; set; }
        public static ConfigEntry<float> RealTimeGain { get; set; }

        public static ConfigEntry<KeyboardShortcut> ActiveAimKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> LowReadyKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> HighReadyKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> ShortStockKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> CycleStancesKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> DisableLaserKeybind { get; set; }

        public static ConfigEntry<bool> ToggleActiveAim { get; set; }

        public static ConfigEntry<bool> EnableAltPistol { get; set; }
        public static ConfigEntry<bool> EnableIdleStamDrain { get; set; }
        public static ConfigEntry<bool> EnableStanceStamChanges { get; set; }

        public static ConfigEntry<float> WeapOffsetX { get; set; }
        public static ConfigEntry<float> WeapOffsetY { get; set; }
        public static ConfigEntry<float> WeapOffsetZ { get; set; }

        public static ConfigEntry<float> ActiveAimRotationX { get; set; }
        public static ConfigEntry<float> ActiveAimRotationY { get; set; }
        public static ConfigEntry<float> ActiveAimRotationZ { get; set; }

        public static ConfigEntry<float> PistolRotationX { get; set; }
        public static ConfigEntry<float> PistolRotationY { get; set; }
        public static ConfigEntry<float> PistolRotationZ { get; set; }

        public static ConfigEntry<float> ActiveAimSpeedMulti { get; set; }
        public static ConfigEntry<float> ActiveAimResetSpeedMulti { get; set; }
        public static ConfigEntry<float> ActiveAimRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> PistolRotationSpeedMulti { get; set; }
        public static ConfigEntry<float> HighReadyRotationMulti { get; set; }
        public static ConfigEntry<float> LowReadyRotationMulti { get; set; }
        public static ConfigEntry<float> ShortStockRotationSpeedMulti { get; set; }

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

        public static ConfigEntry<float> ActiveAimAdditionalRotationX { get; set; }
        public static ConfigEntry<float> ActiveAimAdditionalRotationY { get; set; }
        public static ConfigEntry<float> ActiveAimAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> ActiveAimResetRotationX { get; set; }
        public static ConfigEntry<float> ActiveAimResetRotationY { get; set; }
        public static ConfigEntry<float> ActiveAimResetRotationZ { get; set; }

        public static ConfigEntry<float> HighReadyAdditionalRotationX { get; set; }
        public static ConfigEntry<float> HighReadyAdditionalRotationY { get; set; }
        public static ConfigEntry<float> HighReadyAdditionalRotationZ { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationX { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationY { get; set; }
        public static ConfigEntry<float> HighReadyResetRotationZ { get; set; }

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
        public static ConfigEntry<float> GlobalArmHammerSpeedMulti { get; set; }
        public static ConfigEntry<float> QuickReloadSpeedMulti { get; set; }
        public static ConfigEntry<float> InternalMagReloadMulti { get; set; }
        public static ConfigEntry<float> GlobalBoltSpeedMulti { get; set; }
        public static ConfigEntry<float> RechamberPistolSpeedMulti { get; set; }

        public static ConfigEntry<bool> EnableLogging { get; set; }



        public static Weapon CurrentlyEquipedWeapon;

        public static Vector3 WeaponStartPosition;
        public static Vector3 WeaponOffsetPosition;
        public static Vector3 PistolOffsetPostion;
        public static Vector3 PistolTransformNewStartPosition;
        public static Vector3 WeaponTransformNewStartPosition;
        public static Vector3 LowReadyTransformTargetPosition;
        public static Vector3 HighTransformTargetPosition;
        public static Vector3 TransformBaseStartPosition = new Vector3(0.0f, 0.0f, 0.0f);
        public static Vector3 ActiveAimTransformTargetPosition = new Vector3(0.0f, 0.0f, 0.0f);

        public static bool DidWeaponSwap = false;
        public static bool IsSprinting = false;

        public static bool IsFiring = false;

        public static bool IsBotFiring = false;
        public static bool GrenadeExploded = false;
        public static bool IsAiming;
        public static float Timer = 0.0f;
        public static float BotTimer = 0.0f;
        public static float GrenadeTimer = 0.0f;

        public static int ShotCount = 0;
        public static int PrevShotCount = ShotCount;
        public static bool StatsAreReset;

        public static float StartingRecoilAngle;

        public static float StartingAimSens;
        public static float CurrentAimSens = StartingAimSens;
        public static float StartingHipSens;
        public static float CurrentHipSens = StartingHipSens;
        public static bool CheckedForSens = false;

        public static float StartingDispersion;
        public static float CurrentDispersion;
        public static float DispersionProportionK;

        public static float StartingDamping;
        public static float CurrentDamping;

        public static float StartingHandDamping;
        public static float CurrentHandDamping;

        public static float StartingConvergence;
        public static float CurrentConvergence;
        public static float ConvergenceProporitonK;

        public static float StartingCamRecoilX;
        public static float StartingCamRecoilY;
        public static float CurrentCamRecoilX;
        public static float CurrentCamRecoilY;

        public static float StartingVRecoilX;
        public static float StartingVRecoilY;
        public static float CurrentVRecoilX;
        public static float CurrentVRecoilY;

        public static float StartingHRecoilX;
        public static float StartingHRecoilY;
        public static float CurrentHRecoilX;
        public static float CurrentHRecoilY;

        public static bool LauncherIsActive = false;

        public static Dictionary<Enum, Sprite> IconCache = new Dictionary<Enum, Sprite>();

        private string ModPath;
        private string ConfigFilePath;
        private string ConfigJson;
        public static ConfigTemplate ModConfig;
        private bool IsConfigCorrect = true;

        public static bool isUniformAimPresent = false;
        public static bool isBridgePresent = false;
        public static bool checkedForUniformAim = false;


        public static float MainVolume = 0f;
        public static float GunsVolume = 0f;
        public static float AmbientVolume = 0f;
        public static float AmbientOccluded = 0f;
        public static float CompressorDistortion = 0f;
        public static float CompressorResonance = 0f;
        public static float CompressorLowpass = 0f;
        public static float Compressor = 0f;
        public static float CompressorGain = 0f;

        public static bool HasHeadSet = false;
        public static CC_FastVignette Vignette;

        public static bool HasOptic = false;

        private void GetPaths()
        {
            var mod = RequestHandler.GetJson($"/RealismMod/GetInfo");
            ModPath = Json.Deserialize<string>(mod);
            ConfigFilePath = Path.Combine(ModPath, @"config\config.json");
        }

        private void ConfigCheck()
        {
            ConfigJson = File.ReadAllText(ConfigFilePath);
            ModConfig = JsonConvert.DeserializeObject<ConfigTemplate>(ConfigJson);
            if (!ModConfig.recoil_attachment_overhaul)
            {
                IsConfigCorrect = false;
                Logger.LogError("WARNING: 'Recoil, Ballistics and Attachment Overhaul' MUST be enabled in the config in order to use this plugin! Patches have been disabled.");
            }
        }

        private void CacheIcons()
        {
            IconCache.Add(ENewItemAttributeId.ShotDispersion, Resources.Load<Sprite>("characteristics/icons/Velocity"));
            IconCache.Add(ENewItemAttributeId.BluntThroughput, Resources.Load<Sprite>("characteristics/icons/armorMaterial"));
            IconCache.Add(ENewItemAttributeId.VerticalRecoil, Resources.Load<Sprite>("characteristics/icons/Ergonomics"));
            IconCache.Add(ENewItemAttributeId.HorizontalRecoil, Resources.Load<Sprite>("characteristics/icons/Recoil Back"));
            IconCache.Add(ENewItemAttributeId.Dispersion, Resources.Load<Sprite>("characteristics/icons/Velocity"));
            IconCache.Add(ENewItemAttributeId.CameraRecoil, Resources.Load<Sprite>("characteristics/icons/SightingRange"));
            IconCache.Add(ENewItemAttributeId.AutoROF, Resources.Load<Sprite>("characteristics/icons/bFirerate"));
            IconCache.Add(ENewItemAttributeId.SemiROF, Resources.Load<Sprite>("characteristics/icons/bFirerate"));
            IconCache.Add(ENewItemAttributeId.ReloadSpeed, Resources.Load<Sprite>("characteristics/icons/weapFireType"));
            IconCache.Add(ENewItemAttributeId.FixSpeed, Resources.Load<Sprite>("characteristics/icons/icon_info_raidmoddable"));
            IconCache.Add(ENewItemAttributeId.ChamberSpeed, Resources.Load<Sprite>("characteristics/icons/icon_info_raidmoddable"));
            IconCache.Add(ENewItemAttributeId.AimSpeed, Resources.Load<Sprite>("characteristics/icons/SightingRange"));
            IconCache.Add(ENewItemAttributeId.Firerate, Resources.Load<Sprite>("characteristics/icons/bFirerate"));
            IconCache.Add(ENewItemAttributeId.Damage, Resources.Load<Sprite>("characteristics/icons/icon_info_bulletspeed"));
            IconCache.Add(ENewItemAttributeId.Penetration, Resources.Load<Sprite>("characteristics/icons/armorClass"));
            IconCache.Add(ENewItemAttributeId.ArmorDamage, Resources.Load<Sprite>("characteristics/icons/armorMaterial"));
            IconCache.Add(ENewItemAttributeId.FragmentationChance, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(ENewItemAttributeId.MalfunctionChance, Resources.Load<Sprite>("characteristics/icons/icon_info_raidmoddable"));
            IconCache.Add(ENewItemAttributeId.CanSpall, Resources.Load<Sprite>("characteristics/icons/icon_info_bulletspeed"));
            IconCache.Add(ENewItemAttributeId.SpallReduction, Resources.Load<Sprite>("characteristics/icons/Velocity"));
            IconCache.Add(ENewItemAttributeId.GearReloadSpeed, Resources.Load<Sprite>("characteristics/icons/weapFireType"));
            IconCache.Add(ENewItemAttributeId.CanAds, Resources.Load<Sprite>("characteristics/icons/SightingRange"));
            _ = LoadTexture(ENewItemAttributeId.Balance, Path.Combine(ModPath, "res\\balance.png"));
            _ = LoadTexture(ENewItemAttributeId.RecoilAngle, Path.Combine(ModPath, "res\\recoilAngle.png"));
        }

        private async Task LoadTexture(Enum id, string path)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path))
            {
                uwr.SendWebRequest();

                while (!uwr.isDone)
                    await Task.Delay(5);

                if (uwr.responseCode != 200)
                {
                }
                else
                {
                    Texture2D cachedTexture = DownloadHandlerTexture.GetContent(uwr);
                    IconCache.Add(id, Sprite.Create(cachedTexture, new Rect(0, 0, cachedTexture.width, cachedTexture.height), new Vector2(0, 0)));
                }
            }
        }

        void Awake()
        {

            try
            {
                GetPaths();
                ConfigCheck();
                CacheIcons();
            }
            catch (Exception exception)
            {
                IsConfigCorrect = false;
                Logger.LogError(exception);
            }


            if (IsConfigCorrect == true)
            {
 
                string MiscSettings = ".1. Misc. Settings";
                string RecoilSettings = ".2. Recoil Settings";
                string AdvancedRecoilSettings = ".3. Advanced Recoil Settings";
                string WeapStatSettings = ".4. Weapon Stat Display Settings";
/*                string AmmoSettings = "4. Ammo Stat Display Settings";*/
                string WeaponSettings = ".5. Weapon Settings";
                string DeafSettings = ".6. Deafening and Audio";
                string Speed = ".7. Weapon Speed Modifiers";
                string WeapAimAndPos = ".8. Weapon Stances And Position";
                string ActiveAim = ".9. Active Aim";
                string HighReady = "10. High Ready";
                string LowReady = "11. Low Ready";
                string Pistol = "12. Pistol Position And Stance";
                string ShortStock = "13. Short-Stocking";

                /*   enableAmmoDamageDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Damage", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 5 }));
                   enableAmmoFragDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Fragmentation Chance", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 4 }));
                   enableAmmoPenDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Penetration", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 3 }));
                   enableAmmoArmorDamageDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Armor Damage", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 2 }));*/
                EnableAmmoFirerateDisp = Config.Bind<bool>(MiscSettings, "Display Ammo Fire Rate", true, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 11 }));

                EnableLogging = Config.Bind<bool>(MiscSettings, "Enable Logging", false, new ConfigDescription("Enables Logging For Debug And Dev", null, new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true }));
                EnableProgramK = Config.Bind<bool>(MiscSettings, "Enable Extended Stock Slots Compatibility", false, new ConfigDescription("Requires Restart. Enables Integration Of The Extended Stock Slots Mod. Each Buffer Position Increases Recoil Reduction While Reducing Ergo The Further Out The Stock Is Extended.", null, new ConfigurationManagerAttributes { Order = 2 }));
                EnableFSPatch = Config.Bind<bool>(MiscSettings, "Enable Faceshield Patch", true, new ConfigDescription("Faceshields Block ADS Unless The Specfic Stock/Weapon/Faceshield Allows It.", null, new ConfigurationManagerAttributes { Order = 3 }));
                EnableNVGPatch = Config.Bind<bool>(MiscSettings, "Enable NVG ADS Patch", true, new ConfigDescription("Magnified Optics Block ADS When Using NVGs.", null, new ConfigurationManagerAttributes { Order = 4 }));
                EnableRealArmorClass = Config.Bind<bool>(MiscSettings, "Show Real Armor Class", true, new ConfigDescription("Requiures Restart. Instead Of Showing The Armor's Class As A Number, Use The Real Armor Classification Instead.", null, new ConfigurationManagerAttributes { Order = 8 }));
                EnableHoldBreath = Config.Bind<bool>(MiscSettings, "Enable Hold Breath", false, new ConfigDescription("Enabled Hold Breath, Disabled By Default. The Mod Is Balanced Around Not Being Able To Hold Breath.", null, new ConfigurationManagerAttributes { Order = 10 }));

                EnableHipfireRecoilClimb = Config.Bind<bool>(RecoilSettings, "Enable Hipfire Recoil Climb", true, new ConfigDescription("Requires Restart. Enabled Recoil Climbing While Hipfiring", null, new ConfigurationManagerAttributes { Order = 4 }));
                ReduceCamRecoil = Config.Bind<bool>(RecoilSettings, "Reduce Camera Recoil", false, new ConfigDescription("Reduces Camera Recoil Per Shot. If Disabled, Camera Recoil Becomes More Intense As Weapon Recoil Increases.", null, new ConfigurationManagerAttributes { Order = 3 }));
                SensLimit = Config.Bind<float>(RecoilSettings, "Sensitivity Lower Limit", 0.4f, new ConfigDescription("Sensitivity Lower Limit While Firing. Lower Means More Sensitivity Reduction. 100% Means No Sensitivity Reduction.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes { Order = 2 }));
                RecoilIntensity = Config.Bind<float>(RecoilSettings, "Recoil Multi", 1.15f, new ConfigDescription("Changes The Overall Intenisty Of Recoil. This Will Increase/Decrease Horizontal Recoil, Dispersion, Vertical Recoil.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 1 }));

                EnableRecoilClimb = Config.Bind<bool>(AdvancedRecoilSettings, "Enable Recoil Climb", true, new ConfigDescription("The Core Of The Recoil Overhaul. Recoil Increase Per Shot, Nullifying Recoil Auto-Compensation In Full Auto And Requiring A Constant Pull Of The Mouse To Control Recoil. If Diabled Weapons Will Be Completely Unbalanced Without Stat Changes.", null, new ConfigurationManagerAttributes { Order = 13 }));
                SensChangeRate = Config.Bind<float>(AdvancedRecoilSettings, "Sensitivity Change Rate", 0.75f, new ConfigDescription("Rate At Which Sensitivity Is Reduced While Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes { Order = 12 }));
                SensResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Senisitivity Reset Rate", 1.2f, new ConfigDescription("Rate At Which Sensitivity Recovers After Firing. Higher Means Faster Rate.", new AcceptableValueRange<float>(1.01f, 2f), new ConfigurationManagerAttributes { Order = 11, IsAdvanced = true }));
                ConvergenceSpeedCurve = Config.Bind<float>(AdvancedRecoilSettings, "Convergence Multi", 0.9f, new ConfigDescription("The Convergence Curve. Lower Means More Recoil/Less Convergence.", new AcceptableValueRange<float>(0.01f, 1.5f), new ConfigurationManagerAttributes { Order = 10 }));
                vRecoilLimit = Config.Bind<float>(AdvancedRecoilSettings, "Vertical Recoil Upper Limit", 15.0f, new ConfigDescription("The Upper Limit For Vertical Recoil Increase As A Multiplier. E.g Value Of 10 Is A Limit Of 10x Starting Recoil.", new AcceptableValueRange<float>(1f, 50f), new ConfigurationManagerAttributes { Order = 9 }));
                vRecoilChangeMulti = Config.Bind<float>(AdvancedRecoilSettings, "Vertical Recoil Change Rate Multi", 1.01f, new ConfigDescription("A Multiplier For The Verftical Recoil Increase Per Shot.", new AcceptableValueRange<float>(0.9f, 1.1f), new ConfigurationManagerAttributes { Order = 8 }));
                vRecoilResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Vertical Recoil Reset Rate", 0.91f, new ConfigDescription("The Rate At Which Vertical Recoil Resets Over Time After Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 0.99f), new ConfigurationManagerAttributes { Order = 7, IsAdvanced = true }));
                hRecoilLimit = Config.Bind<float>(AdvancedRecoilSettings, "Rearward Recoil Upper Limit", 2.5f, new ConfigDescription("The Upper Limit For Rearward Recoil Increase As A Multiplier. E.g Value Of 10 Is A Limit Of 10x Starting Recoil.", new AcceptableValueRange<float>(1f, 50f), new ConfigurationManagerAttributes { Order = 6 }));
                hRecoilChangeMulti = Config.Bind<float>(AdvancedRecoilSettings, "Rearward Recoil Change Rate Multi", 1.0f, new ConfigDescription("A Multiplier For The Rearward Recoil Increase Per Shot.", new AcceptableValueRange<float>(0.9f, 1.1f), new ConfigurationManagerAttributes { Order = 5 }));
                hRecoilResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Rearward Recoil Reset Rate", 0.91f, new ConfigDescription("The Rate At Which Rearward Recoil Resets Over Time After Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 0.99f), new ConfigurationManagerAttributes { Order = 4, IsAdvanced = true }));
                convergenceResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Convergence Reset Rate", 1.16f, new ConfigDescription("The Rate At Which Convergence Resets Over Time After Firing. Higher Means Faster Rate.", new AcceptableValueRange<float>(1.01f, 2f), new ConfigurationManagerAttributes { Order = 3, IsAdvanced = true }));
                convergenceLimit = Config.Bind<float>(AdvancedRecoilSettings, "Convergence Lower Limit", 0.3f, new ConfigDescription("The Lower Limit For Convergence. Convergence Is Kept In Proportion With Vertical Recoil While Firing, Down To The Set Limit. Value Of 0.3 Means Convegence Lower Limit Of 0.3 * Starting Convergance.", new AcceptableValueRange<float>(0.1f, 1.0f), new ConfigurationManagerAttributes { Order = 2, IsAdvanced = true }));
                resetTime = Config.Bind<float>(AdvancedRecoilSettings, "Time Before Reset", 0.14f, new ConfigDescription("The Time In Seconds That Has To Be Elapsed Before Firing Is Considered Over, Stats Will Not Reset Until This Timer Is Done. Helps Prevent Spam Fire In Full Auto.", new AcceptableValueRange<float>(0.1f, 0.5f), new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true }));

                EnableStatsDelta = Config.Bind<bool>(WeapStatSettings, "Show Stats Delta Preview", false, new ConfigDescription("Requiures Restart. Shows A Preview Of The Difference To Stats Swapping/Removing Attachments Will Make. Warning: Will Degrade Performance Significantly When Moddig Weapons In Inspect Or Modding Screens.", null, new ConfigurationManagerAttributes { Order = 5 }));
                showBalance = Config.Bind<bool>(WeapStatSettings, "Show Balance Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 5 }));
                showCamRecoil = Config.Bind<bool>(WeapStatSettings, "Show Camera Recoil Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 4 }));
                showDispersion = Config.Bind<bool>(WeapStatSettings, "Show Dispersion Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 3 }));
                showRecoilAngle = Config.Bind<bool>(WeapStatSettings, "Show Recoil Angle Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use..", null, new ConfigurationManagerAttributes { Order = 2 }));
                showSemiROF = Config.Bind<bool>(WeapStatSettings, "Show Semi Auto ROF Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 1 }));

                SwayIntensity = Config.Bind<float>(WeaponSettings, "Sway Intensity", 1f, new ConfigDescription("Changes The Intensity Of Aim Sway And Inertia.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 1 }));
                EnableMalfPatch = Config.Bind<bool>(WeaponSettings, "Enable Malfunctions Changes", true, new ConfigDescription("Requires Restart. Malfunction Changes Must Be Enabled On The Server (Config App). Some Subsonic Ammo Needs Special Mods To Cycle, Malfunctions Can Happen At Any Durability But The Chance Is Halved If Above The Durability Threshold.", null, new ConfigurationManagerAttributes { Order = 2 }));
                InspectionlessMalfs = Config.Bind<bool>(WeaponSettings, "Enable Inspectionless Malfunctions", true, new ConfigDescription("Requires Restart. You Don't Need To Inspect A Malfunction In Order To Clear It.", null, new ConfigurationManagerAttributes { Order = 3 }));
                DuraMalfThreshold = Config.Bind<float>(WeaponSettings, "Malfunction Durability Threshold", 98f, new ConfigDescription("Malfunction Changes Must Be Enabled On The Server (Config App) And 'Enable Malfunctions Changes' Must Be True. Malfunction Chance Is Reduced By Half Until This Durability Threshold Is Met.", new AcceptableValueRange<float>(80f, 100f), new ConfigurationManagerAttributes { Order = 4 }));
                enableSGMastering = Config.Bind<bool>(WeaponSettings, "Enable Increased Shotgun Mastery", true, new ConfigDescription("Requires Restart. Shotguns Will Get Set To Base Lvl 2 Mastery For Reload Animations, Giving Them Better Pump Animations. ADS while Reloading Is Unaffected.", null, new ConfigurationManagerAttributes { Order = 5 }));
                IncreaseCOI = Config.Bind<bool>(WeaponSettings, "Enable Increased Inaccuracy", true, new ConfigDescription("Requires Restart. Increases The Innacuracy Of All Weapons So That MOA/Accuracy Is A More Important Stat.", null, new ConfigurationManagerAttributes { Order = 6 }));

                EnableDeafen = Config.Bind<bool>(DeafSettings, "Enable Deafening", true, new ConfigDescription("Requiures Restart. Enables Gunshots And Explosions Deafening The Player.", null, new ConfigurationManagerAttributes { Order = 9 }));
                RealTimeGain = Config.Bind<float>(DeafSettings, "Headset Gain", 13f, new ConfigDescription("WARNING: DO NOT SET THIS TOO HIGH, IT MAY DAMAGE YOUR HEARING! Most EFT Headsets Are Set To 13 By Default, Don't Make It Much Higher. Adjusts The Gain Of Equipped Headsets In Real Time, Acts Just Like The Volume Control On IRL Ear Defenders.", new AcceptableValueRange<float>(0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 8 }));
                GainReduc = Config.Bind<float>(DeafSettings, "Headset Gain Cutoff Multi", 0.75f, new ConfigDescription("How Much Headset Gain Is Reduced While Firing. 0.75 = 25% Reduction.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7 }));
                DeafRate = Config.Bind<float>(DeafSettings, "Deafen Rate", 0.023f, new ConfigDescription("How Quickly Player Gets Deafened. Higher = Faster.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true }));
                DeafReset = Config.Bind<float>(DeafSettings, "Deafen Reset Rate", 0.033f, new ConfigDescription("How Quickly Player Regains Hearing. Higher = Faster.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true }));
                VigRate = Config.Bind<float>(DeafSettings, "Tunnel Effect Rate", 0.65f, new ConfigDescription("How Quickly Player Gets Tunnel Vission. Higher = Faster", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true }));
                VigReset = Config.Bind<float>(DeafSettings, "Tunnel Effect Reset Rate", 1f, new ConfigDescription("How Quickly Player Recovers From Tunnel Vision. Higher = Faster", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true }));
                DistRate = Config.Bind<float>(DeafSettings, "Distortion Rate", 0.16f, new ConfigDescription("How Quickly Player's Hearing Gets Distorted. Higher = Faster", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true }));
                DistReset = Config.Bind<float>(DeafSettings, "Distortion Reset Rate", 0.25f, new ConfigDescription("How Quickly Player's Hearing Recovers From Distortion. Higher = Faster", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true }));

                EnableReloadPatches = Config.Bind<bool>(Speed, "Enable Reload And Chamber Speed Changes", true, new ConfigDescription("Requires Restart. Weapon Weight, Magazine Weight, Attachment Reload And Chamber Speed Stat, Balance, Ergo And Arm Injury Affect Reload And Chamber Speed.", null, new ConfigurationManagerAttributes { Order = 17 }));
                GlobalAimSpeedModifier = Config.Bind<float>(Speed, "Aim Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 16 }));
                GlobalReloadSpeedMulti = Config.Bind<float>(Speed, "Magazine Reload Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 15 }));
                GlobalFixSpeedMulti = Config.Bind<float>(Speed, "Malfunction Fix Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 14 }));
                GlobalUBGLReloadMulti = Config.Bind<float>(Speed, "UBGL Reload Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 13, IsAdvanced = true }));
                RechamberPistolSpeedMulti = Config.Bind<float>(Speed, "Pistol Rechamber Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true }));
                GlobalRechamberSpeedMulti = Config.Bind<float>(Speed, "Rechamber Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11 }));
                GlobalBoltSpeedMulti = Config.Bind<float>(Speed, "Bolt Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10 }));
                GlobalShotgunRackSpeedFactor = Config.Bind<float>(Speed, "Shotgun Rack Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 9 }));
                GlobalCheckChamberSpeedMulti = Config.Bind<float>(Speed, "Chamber Check Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 8 }));
                GlobalCheckChamberShotgunSpeedMulti = Config.Bind<float>(Speed, "Shotgun Chamber Check Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7, IsAdvanced = true }));
                GlobalCheckChamberPistolSpeedMulti = Config.Bind<float>(Speed, "Pistol Chamber Check Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true }));
                GlobalCheckAmmoPistolSpeedMulti = Config.Bind<float>(Speed, "Chamber Check Ammo Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true }));
                GlobalCheckAmmoMulti = Config.Bind<float>(Speed, "Check Ammo Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4 }));
                GlobalArmHammerSpeedMulti = Config.Bind<float>(Speed, "Arm Hammer, Bolt Release, Slide Release Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true }));
                QuickReloadSpeedMulti = Config.Bind<float>(Speed, "Quick Reload Multi", 1.4f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2 }));
                InternalMagReloadMulti = Config.Bind<float>(Speed, "Internal Magazine Reload", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1 }));

                EnableAltPistol = Config.Bind<bool>(WeapAimAndPos, "Enable Alternative Pistol Position And ADS", true, new ConfigDescription("Pistol Will Be Held Centered And In A Compressed Stance. ADS Will Be Animated.", null, new ConfigurationManagerAttributes { Order = 177 }));
                EnableIdleStamDrain = Config.Bind<bool>(WeapAimAndPos, "Enable Idle Arm Stamina Drain", false, new ConfigDescription("Arm Stamina Will Drain When Not In A Stance (High And Low Ready, Short-Stocking).", null, new ConfigurationManagerAttributes { Order = 176 }));
                EnableStanceStamChanges = Config.Bind<bool>(WeapAimAndPos, "Enable Stance Stamina And Movement Effects", true, new ConfigDescription("Enabled Stances To Affect Stamina And Movement Speed. High + Low Ready, Short-Stocking And Pistol Idle Will Regenerate Stamina Faster And Optionally Idle With Rifles Drains Stamina. High Ready Has Faster Sprint Speed And Sprint Acceleration, Low Ready Has Faster Spritn Accel. Arm Stamina Won't Start Drain Regular Stamina If It Reaches 0.", null, new ConfigurationManagerAttributes { Order = 175 }));

                CycleStancesKeybind = Config.Bind(WeapAimAndPos, "Cycle Stances Keybind", new KeyboardShortcut(KeyCode.J), new ConfigDescription("Cycles Between High, Low Ready and Short-Stocking. Double Click Returns To Idle.", null, new ConfigurationManagerAttributes { Order = 174 }));
                ActiveAimKeybind = Config.Bind(WeapAimAndPos, "Active Aim Keybind", new KeyboardShortcut(KeyCode.LeftArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 173 }));
                HighReadyKeybind = Config.Bind(WeapAimAndPos, "High Ready Keybind", new KeyboardShortcut(KeyCode.UpArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 172 }));
                LowReadyKeybind = Config.Bind(WeapAimAndPos, "Low Ready Keybind", new KeyboardShortcut(KeyCode.DownArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 171 }));
                ShortStockKeybind = Config.Bind(WeapAimAndPos, "Short-Stock Keybind", new KeyboardShortcut(KeyCode.RightArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170 }));

                ToggleActiveAim = Config.Bind<bool>(WeapAimAndPos, "Use Toggle For Active Aim", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 162 }));

                WeapOffsetX = Config.Bind<float>(WeapAimAndPos, "Weapon Position X-Axis", 0.0f, new ConfigDescription("Adjusts The Starting Position Of Weapon On Screen, Except Pistols.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 152 }));
                WeapOffsetY = Config.Bind<float>(WeapAimAndPos, "Weapon Position Y-Axis", 0.0f, new ConfigDescription("Adjusts The Starting Position Of Weapon On Screen, Except Pistols.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 151 }));
                WeapOffsetZ = Config.Bind<float>(WeapAimAndPos, "Weapon Position Z-Axis", 0.0f, new ConfigDescription("Adjusts The Starting Position Of Weapon On Screen, Except Pistols.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 150 }));

                ActiveAimAdditionalRotationSpeedMulti = Config.Bind<float>(ActiveAim, "Active Aim Additonal Rotation Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true }));
                ActiveAimResetRotationSpeedMulti = Config.Bind<float>(ActiveAim, "Active Aim Reset Rotation Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true }));
                ActiveAimRotationSpeedMulti = Config.Bind<float>(ActiveAim, "Active Aim Rotation Speed Multi", 0.48f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 144, IsAdvanced = true }));
                ActiveAimSpeedMulti = Config.Bind<float>(ActiveAim, "Active Aim Speed Multi", 0.36f, new ConfigDescription("", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 143, IsAdvanced = true }));
                ActiveAimResetSpeedMulti = Config.Bind<float>(ActiveAim, "Active Aim Reset Speed Multi", 0.25f, new ConfigDescription("", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 142, IsAdvanced = true }));

                ActiveAimOffsetX = Config.Bind<float>(ActiveAim, "Active Aim Position X-Axis", -0.08f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-0.12f, 0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 132, IsAdvanced = true }));
                ActiveAimOffsetY = Config.Bind<float>(ActiveAim, "Active Aim Position Y-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 131, IsAdvanced = true }));
                ActiveAimOffsetZ = Config.Bind<float>(ActiveAim, "Active Aim Position Z-Axis", 0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 130, IsAdvanced = true }));

                ActiveAimRotationX = Config.Bind<float>(ActiveAim, "Active Aim Rotation X-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 122, IsAdvanced = true }));
                ActiveAimRotationY = Config.Bind<float>(ActiveAim, "Active Aim Rotation Y-Axis", -130.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 121, IsAdvanced = true }));
                ActiveAimRotationZ = Config.Bind<float>(ActiveAim, "Active Aim Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 120, IsAdvanced = true }));

                ActiveAimAdditionalRotationX = Config.Bind<float>(ActiveAim, "Active Aiming Additional Rotation X-Axis", 10f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 111, IsAdvanced = true }));
                ActiveAimAdditionalRotationY = Config.Bind<float>(ActiveAim, "Active Aiming Additional Rotation Y-Axis", -5f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true }));
                ActiveAimAdditionalRotationZ = Config.Bind<float>(ActiveAim, "Active Aiming Additional Rotation Z-Axis", 10f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true }));

                ActiveAimResetRotationX = Config.Bind<float>(ActiveAim, "Active Aiming Reset Rotation X-Axis", 5.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 102, IsAdvanced = true }));
                ActiveAimResetRotationY = Config.Bind<float>(ActiveAim, "Active Aiming Reset Rotation Y-Axis", 15.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 101, IsAdvanced = true }));
                ActiveAimResetRotationZ = Config.Bind<float>(ActiveAim, "Active Aiming Reset Rotation Z-Axis", -3.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 100, IsAdvanced = true }));

                HighReadyAdditionalRotationSpeedMulti = Config.Bind<float>(HighReady, "High Ready Additonal Rotation Speed Multi", 0.5f, new ConfigDescription("How Fast The Weapon Rotates Going Out Of Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 94, IsAdvanced = true }));
                HighReadyResetRotationMulti = Config.Bind<float>(HighReady, "High Ready Reset Rotation Speed Multi", 0.67f, new ConfigDescription("How Fast The Weapon Rotates Going Out Of Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 93, IsAdvanced = true }));
                HighReadyRotationMulti = Config.Bind<float>(HighReady, "High Ready Rotation Speed Multi", 0.36f, new ConfigDescription("How Fast The Weapon Rotates Going Into Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 92, IsAdvanced = true }));
                HighReadyResetSpeedMulti = Config.Bind<float>(HighReady, "High Ready Reset Speed Multi", 0.25f, new ConfigDescription("How Fast The Weapon Moves Going Out Of Stance", new AcceptableValueRange<float>(0.000001f, 5.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 91, IsAdvanced = true }));
                HighReadySpeedMulti = Config.Bind<float>(HighReady, "High Ready Speed Multi", 0.25f, new ConfigDescription("How Fast The Weapon Moves Going Into Stance", new AcceptableValueRange<float>(0.000001f, 5.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 90, IsAdvanced = true }));

                HighReadyOffsetX = Config.Bind<float>(HighReady, "High Ready Position X-Axis", 0.0f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 82, IsAdvanced = true }));
                HighReadyOffsetY = Config.Bind<float>(HighReady, "High Ready Position Y-Axis", -0.06f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 81, IsAdvanced = true }));
                HighReadyOffsetZ = Config.Bind<float>(HighReady, "High Ready Position Z-Axis", -0.03f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 80, IsAdvanced = true }));

                HighReadyRotationX = Config.Bind<float>(HighReady, "High Ready Rotation X-Axis", -70.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 72, IsAdvanced = true }));
                HighReadyRotationY = Config.Bind<float>(HighReady, "High Ready Rotation Y-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 71, IsAdvanced = true }));
                HighReadyRotationZ = Config.Bind<float>(HighReady, "High Ready Rotation Z-Axis", 30.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 70, IsAdvanced = true }));

                HighReadyAdditionalRotationX = Config.Bind<float>(HighReady, "High Ready Additional Rotation X-Axis", -10.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 69, IsAdvanced = true }));
                HighReadyAdditionalRotationY = Config.Bind<float>(HighReady, "High Ready Additiona Rotation Y-Axis", 30.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 68, IsAdvanced = true }));
                HighReadyAdditionalRotationZ = Config.Bind<float>(HighReady, "High Ready Additional Rotation Z-Axis", 5.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 67, IsAdvanced = true }));

                HighReadyResetRotationX = Config.Bind<float>(HighReady, "High Ready Reset Rotation X-Axis", 8.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 66, IsAdvanced = true }));
                HighReadyResetRotationY = Config.Bind<float>(HighReady, "High Ready Reset Rotation Y-Axis", -15.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 65, IsAdvanced = true }));
                HighReadyResetRotationZ = Config.Bind<float>(HighReady, "High Ready Reset Rotation Z-Axis", -4.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 64, IsAdvanced = true }));

                LowReadyAdditionalRotationSpeedMulti = Config.Bind<float>(LowReady, "Low Ready Additonal Rotation Speed Multi", 0.8f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 64, IsAdvanced = true }));
                LowReadyResetRotationMulti = Config.Bind<float>(LowReady, "Low Ready Reset Rotation Speed Multi", 0.8f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 63, IsAdvanced = true }));
                LowReadyRotationMulti = Config.Bind<float>(LowReady, "Low Ready Rotation Speed Multi", 0.4f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 62, IsAdvanced = true }));
                LowReadySpeedMulti = Config.Bind<float>(LowReady, "Low Ready Speed Multi", 0.2f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 5.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 61, IsAdvanced = true }));
                LowReadyResetSpeedMulti = Config.Bind<float>(LowReady, "Low Ready Reset Speed Multi", 0.2f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 5.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 60, IsAdvanced = true }));

                LowReadyOffsetX = Config.Bind<float>(LowReady, "Low Ready Position X-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 52, IsAdvanced = true }));
                LowReadyOffsetY = Config.Bind<float>(LowReady, "Low Ready Position Y-Axis", 0.025f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 51, IsAdvanced = true }));
                LowReadyOffsetZ = Config.Bind<float>(LowReady, "Low Ready Position Z-Axis", 0.04f, new ConfigDescription("Weapon Position When In Stance..", new AcceptableValueRange<float>(-1f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 50, IsAdvanced = true }));

                LowReadyRotationX = Config.Bind<float>(LowReady, "Low Ready Rotation X-Axis", 50.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 42, IsAdvanced = true }));
                LowReadyRotationY = Config.Bind<float>(LowReady, "Low Ready Rotation Y-Axis", 10.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 41, IsAdvanced = true }));
                LowReadyRotationZ = Config.Bind<float>(LowReady, "Low Ready Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 40, IsAdvanced = true }));

                LowReadyAdditionalRotationX = Config.Bind<float>(LowReady, "Low Ready Additional Rotation X-Axis", 20.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 39, IsAdvanced = true }));
                LowReadyAdditionalRotationY = Config.Bind<float>(LowReady, "Low Ready Additional Rotation Y-Axis", -10.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 38, IsAdvanced = true }));
                LowReadyAdditionalRotationZ = Config.Bind<float>(LowReady, "Low Ready Additional Rotation Z-Axis", -3.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 37, IsAdvanced = true }));

                LowReadyResetRotationX = Config.Bind<float>(LowReady, "Low Ready Reset Rotation X-Axis", -9.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 36, IsAdvanced = true }));
                LowReadyResetRotationY = Config.Bind<float>(LowReady, "Low Ready Reset Rotation Y-Axis", -5.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true }));
                LowReadyResetRotationZ = Config.Bind<float>(LowReady, "Low Ready Reset Rotation Z-Axis", -1.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true }));

                PistolAdditionalRotationSpeedMulti = Config.Bind<float>(Pistol, "Pistol Additional Rotation Speed Multi", 1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true }));
                PistolResetRotationSpeedMulti = Config.Bind<float>(Pistol, "Pistol Reset Rotation Speed Multi", 1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true }));
                PistolRotationSpeedMulti = Config.Bind<float>(Pistol, "Pistol Rotation Speed Multi", 1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 33, IsAdvanced = true }));
                PistolPosSpeedMulti = Config.Bind<float>(Pistol, "Pistol Position Speed Multi", 0.2f, new ConfigDescription("", new AcceptableValueRange<float>(-2.0f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 32, IsAdvanced = true }));
                PistolPosResetSpeedMulti = Config.Bind<float>(Pistol, "Pistol Position Reset Speed Multi", 0.18f, new ConfigDescription("", new AcceptableValueRange<float>(-2.0f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true }));

                PistolOffsetX = Config.Bind<float>(Pistol, "Pistol Position X-Axis", 0.03f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 22, IsAdvanced = true }));
                PistolOffsetY = Config.Bind<float>(Pistol, "Pistol Position Y-Axis", -0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 21, IsAdvanced = true }));
                PistolOffsetZ = Config.Bind<float>(Pistol, "Pistol Position Z-Axis", -0.03f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 20, IsAdvanced = true }));

                PistolRotationX = Config.Bind<float>(Pistol, "Pistol Rotation X-Axis", -2.5f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true }));
                PistolRotationY = Config.Bind<float>(Pistol, "Pistol Rotation Y-Axis", -20f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, IsAdvanced = true }));
                PistolRotationZ = Config.Bind<float>(Pistol, "Pistol Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true }));

                PistolAdditionalRotationX = Config.Bind<float>(Pistol, "Pistol Ready Additional Rotation X-Axis", 2.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true }));
                PistolAdditionalRotationY = Config.Bind<float>(Pistol, "Pistol Ready Additional Rotation Y-Axis", 8.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true }));
                PistolAdditionalRotationZ = Config.Bind<float>(Pistol, "Pistol Ready Additional Rotation Z-Axis", 2.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true }));

                PistolResetRotationX = Config.Bind<float>(Pistol, "Pistol Ready Reset Rotation X-Axis", 2f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true }));
                PistolResetRotationY = Config.Bind<float>(Pistol, "Pistol Ready Reset Rotation Y-Axis", 2.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true }));
                PistolResetRotationZ = Config.Bind<float>(Pistol, "Pistol Ready Reset Rotation Z-Axis", -1.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true }));

                ShortStockAdditionalRotationSpeedMulti = Config.Bind<float>(ShortStock, "Short-Stock Additional Rotation Speed Multi", 0.9f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true }));
                ShortStockResetRotationSpeedMulti = Config.Bind<float>(ShortStock, "Short-Stock Reset Rotation Speed Multi", 0.9f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true }));
                ShortStockRotationSpeedMulti = Config.Bind<float>(ShortStock, "Short-Stock Rotation Speed Multi", 0.7f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 33, IsAdvanced = true }));
                ShortStockSpeedMulti = Config.Bind<float>(ShortStock, "Short-Stock Position Speed Multi", 0.45f, new ConfigDescription("", new AcceptableValueRange<float>(-2.0f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 32, IsAdvanced = true }));
                ShortStockResetSpeedMulti = Config.Bind<float>(ShortStock, "Short-Stock Position Reset Speed Mult", 0.35f, new ConfigDescription("", new AcceptableValueRange<float>(-2.0f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true }));

                ShortStockOffsetX = Config.Bind<float>(ShortStock, "Short-Stock Position X-Axis", 0.035f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 22, IsAdvanced = true }));
                ShortStockOffsetY = Config.Bind<float>(ShortStock, "Short-Stock Position Y-Axis", -0.05f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 21, IsAdvanced = true }));
                ShortStockOffsetZ = Config.Bind<float>(ShortStock, "Short-Stock Position Z-Axis", -0.1f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 20, IsAdvanced = true }));

                ShortStockRotationX = Config.Bind<float>(ShortStock, "Short-Stock Rotation X-Axis", 0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true }));
                ShortStockRotationY = Config.Bind<float>(ShortStock, "Short-Stock Rotation Y-Axis", -100.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, IsAdvanced = true }));
                ShortStockRotationZ = Config.Bind<float>(ShortStock, "Short-Stock Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true }));

                ShortStockAdditionalRotationX = Config.Bind<float>(ShortStock, "Short-Stock Ready Additional Rotation X-Axis", -11.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true }));
                ShortStockAdditionalRotationY = Config.Bind<float>(ShortStock, "Short-Stock Ready Additional Rotation Y-Axis", 0.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true }));
                ShortStockAdditionalRotationZ = Config.Bind<float>(ShortStock, "Short-Stock Ready Additional Rotation Z-Axis", 11.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true }));

                ShortStockResetRotationX = Config.Bind<float>(ShortStock, "Short-Stock Ready Reset Rotation X-Axis", -5.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true }));
                ShortStockResetRotationY = Config.Bind<float>(ShortStock, "Short-Stock Ready Reset Rotation Y-Axis", 10.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true }));
                ShortStockResetRotationZ = Config.Bind<float>(ShortStock, "Short-Stock Ready Reset Rotation Z-Axis", -2.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true }));

                if (EnableProgramK.Value == true)
                {
                    Utils.ProgramKEnabled = true;
                    Logger.LogInfo("Realism Mod: ProgramK Compatibiltiy Enabled!");
                }

                //Stat assignment patches
                new COIDeltaPatch().Enable();
                new TotalShotgunDispersionPatch().Enable();
                new GetDurabilityLossOnShotPatch().Enable();
                new AutoFireRatePatch().Enable();
                new SingleFireRatePatch().Enable();
                new ErgoDeltaPatch().Enable();
                new ErgoWeightPatch().Enable();
                new method_9Patch().Enable();

                new SyncWithCharacterSkillsPatch().Enable();
                new UpdateWeaponVariablesPatch().Enable();
                new SetAimingSlowdownPatch().Enable();

                //sway and aim inertia
                new method_20Patch().Enable();
                new UpdateSwayFactorsPatch().Enable();
                new OverweightPatch().Enable();

                //Recoil Patches
                new OnWeaponParametersChangedPatch().Enable();
                new ProcessPatch().Enable();
                new ShootPatch().Enable();
                new SetCurveParametersPatch().Enable();

                //Sensitivity Patches
                new AimingSensitivityPatch().Enable();
                new UpdateSensitivityPatch().Enable();
                if (Plugin.EnableHipfireRecoilClimb.Value == true)
                {
                    new GetRotationMultiplierPatch().Enable();
                }

                //Aiming Patches + Reload Trigger
                new GetAimingPatch().Enable();
                new SetAimingPatch().Enable();
                new ToggleAimPatch().Enable();

                //Malf Patches
                if (Plugin.EnableMalfPatch.Value == true && ModConfig.malf_changes == true)
                {
                    new GetTotalMalfunctionChancePatch().Enable();
                }
                if (Plugin.InspectionlessMalfs.Value == true) 
                {
                    new IsKnownMalfTypePatch().Enable();
                }

                //Reload Patches
                if (Plugin.EnableReloadPatches.Value == true)
                {
                    new CanStartReloadPatch().Enable();
                    new ReloadMagPatch().Enable();
                    new QuickReloadMagPatch().Enable();
                    new ReloadWithAmmoPatch().Enable();
                    new ReloadBarrelsPatch().Enable();
                    new ReloadRevolverDrumPatch().Enable();

                    new OnMagInsertedPatch().Enable();
                    new SetMagTypeCurrentPatch().Enable();
                    new SetMagTypeNewPatch().Enable();
                    new SetMagInWeaponPatch().Enable();

                    new RechamberSpeedPatch().Enable();
                    new SetMalfRepairSpeedPatch().Enable();
                    new SetBoltActionReloadPatch().Enable();

                    new CheckChamberPatch().Enable();
                    new SetSpeedParametersPatch().Enable();
                    new CheckAmmoPatch().Enable();
                    new SetHammerArmedPatch().Enable();

                    new OnItemAddedOrRemovedPatch().Enable();
                    new SetLauncherPatch().Enable();

                    new CheckAmmoFirearmControllerPatch().Enable();
                    new SetAnimatorAndProceduralValuesPatch().Enable();
                }

                if (enableSGMastering.Value == true)
                {
                    new SetWeaponLevelPatch().Enable();
                }

                //Stat Display Patches
                new ModConstructorPatch().Enable();
                new WeaponConstructorPatch().Enable();
                new HRecoilDisplayStringValuePatch().Enable();
                new HRecoilDisplayDeltaPatch().Enable();
                new VRecoilDisplayStringValuePatch().Enable();
                new VRecoilDisplayDeltaPatch().Enable();
                new ModVRecoilStatDisplayPatchFloat().Enable();
                new ModVRecoilStatDisplayPatchString().Enable();
                new ErgoDisplayDeltaPatch().Enable();
                new ErgoDisplayStringValuePatch().Enable();
                new COIDisplayDeltaPatch().Enable();
                new COIDisplayStringValuePatch().Enable();
                new FireRateDisplayStringValuePatch().Enable();
                new GetCachedReadonlyQualitiesPatch().Enable();
                new CenterOfImpactMOAPatch().Enable();
                new ModErgoStatDisplayPatch().Enable();
                new GetAttributeIconPatches().Enable();

                if (IncreaseCOI.Value == true)
                {
                    new GetTotalCenterOfImpactPatch().Enable();
                }

                //Ballistics
                if (ModConfig.realistic_ballistics == true)
                {
                    new CreateShotPatch().Enable();
                    new ApplyDamagePatch().Enable();
                    new DamageInfoPatch().Enable();
                    new ApplyDamageInfoPatch().Enable();
                    new SetPenetrationStatusPatch().Enable();

                    //Armor Class
                    if (EnableRealArmorClass.Value == true)
                    {
                        new ArmorClassDisplayPatch().Enable();
                    }
                }

                new ArmorComponentPatch().Enable();
                new RigConstructorPatch().Enable();

                //Player
                new EnduranceSprintActionPatch().Enable();
                new EnduranceMovementActionPatch().Enable();
                new ToggleHoldingBreathPatch().Enable();
                new PlayerInitPatch().Enable();

                //movement
                new SprintAccelerationPatch().Enable();

                //Shot Effects
                if (EnableDeafen.Value == true)
                {
                    new VignettePatch().Enable();
                    new UpdatePhonesPatch().Enable();
                    new SetCompressorPatch().Enable();
                    new RegisterShotPatch().Enable();
                    new ExplosionPatch().Enable();
                    new GrenadeClassContusionPatch().Enable();
                }

                //LateUpdate
                new PlayerLateUpdatePatch().Enable();

                //stances
                new ApplyComplexRotationPatch().Enable();
                new InitTransformsPatch().Enable();
                new LaserLateUpdatePatch().Enable();
                new WeaponOverlappingPatch().Enable();
                new WeaponLengthPatch().Enable();
                new WeaponOverlappingPatch().Enable();
            }
        }

        void Update()
        {
            if (IsConfigCorrect == true)
            {
                if (!checkedForUniformAim)
                {
                    isUniformAimPresent = Chainloader.PluginInfos.ContainsKey("com.notGreg.UniformAim");
                    isBridgePresent = Chainloader.PluginInfos.ContainsKey("com.notGreg.RealismModBridge");
                    checkedForUniformAim = true;
                }

                if (Utils.CheckIsReady())
                {

                    if (Plugin.ShotCount > Plugin.PrevShotCount)
                    {
                        Plugin.IsFiring = true;
                        StanceController.IsFiringFromStance = true;
                    }

                    if (Plugin.EnableRecoilClimb.Value == true && (Plugin.IsAiming == true || Plugin.EnableHipfireRecoilClimb.Value == true))
                    {
                        Recoil.DoRecoilClimb();
                    }

                    if (Plugin.ShotCount == Plugin.PrevShotCount)
                    {
                        Plugin.Timer += Time.deltaTime;
                 
                        if (Plugin.Timer >= Plugin.resetTime.Value)
                        {
                            Plugin.IsFiring = false;
                            Plugin.ShotCount = 0;
                            Plugin.PrevShotCount = 0;
                            Plugin.Timer = 0f;
                        }

                        StanceController.StanceShotTimer();
                    }

                    if (EnableDeafen.Value == true)
                    {
                        Deafening.DoDeafening();

                        if (Plugin.IsBotFiring == true)
                        {
                            Plugin.BotTimer += Time.deltaTime;
                            if (Plugin.BotTimer >= 0.5f)
                            {
                                Plugin.IsBotFiring = false;
                                Plugin.BotTimer = 0f;
                            }
                        }

                        if (Plugin.GrenadeExploded == true)
                        {
                            Plugin.GrenadeTimer += Time.deltaTime;
                            if (Plugin.GrenadeTimer >= 0.7f)
                            {
                                Plugin.GrenadeExploded = false;
                                Plugin.GrenadeTimer = 0f;
                            }
                        }
                    }

                    if (!Plugin.IsFiring)
                    {
                        Recoil.ResetRecoil();
                    }

                    StanceController.StanceState();

                    Logger.LogWarning("is aiming " + Plugin.IsAiming);

                }
            }
        }
    }
}

