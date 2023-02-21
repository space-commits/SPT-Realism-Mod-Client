using Aki.Common.Http;
using Aki.Common.Utils;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static GClass1643;
using static RealismMod.ArmorPatches;
using static RealismMod.Attributes;
using static UnityEngine.Rendering.PostProcessing.HistogramMonitor;


namespace RealismMod
{

    public class ConfigTemplate
    {
        public bool recoil_attachment_overhaul { get; set; }
        public bool malf_changes { get; set; }
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
        public static ConfigEntry<bool> enableFSPatch { get; set; }
        public static ConfigEntry<bool> enableMalfPatch { get; set; }
        public static ConfigEntry<bool> enableSGMastering { get; set; }
        public static ConfigEntry<bool> enableProgramK { get; set; }
        public static ConfigEntry<bool> enableAmmoFirerateDisp { get; set; }
        public static ConfigEntry<bool> enableAmmoDamageDisp { get; set; }
        public static ConfigEntry<bool> enableAmmoPenDisp { get; set; }
        public static ConfigEntry<bool> enableAmmoArmorDamageDisp { get; set; }
        public static ConfigEntry<bool> enableAmmoFragDisp { get; set; }
        public static ConfigEntry<bool> enableBarrelFactor { get; set; }
        public static ConfigEntry<bool> enableReloadPatches { get; set; }
        public static ConfigEntry<bool> enableRealArmorClass { get; set; }
        public static ConfigEntry<bool> reduceCamRecoil { get; set; }
        public static ConfigEntry<float> convergenceSpeedCurve { get; set; }
        public static ConfigEntry<bool> EnableDeafen { get; set; }
        public static ConfigEntry<bool> enableHoldBreath { get; set; }
        public static ConfigEntry<float> DuraMalfThreshold { get; set; }
        public static ConfigEntry<bool> EnableRecoilClimb { get; set; }
        public static ConfigEntry<float> SwayIntensity { get; set; }
        public static ConfigEntry<float> RecoilIntensity { get; set; }
        public static ConfigEntry<bool> EnableStatsDelta { get; set; }
        public static ConfigEntry<bool> EnableHipfire { get; set; }
        public static ConfigEntry<bool> IncreaseCOI { get; set; }
        public static ConfigEntry<float> DeafRate { get; set; }
        public static ConfigEntry<float> DeafReset { get; set; }
        public static ConfigEntry<float> VigRate { get; set; }
        public static ConfigEntry<float> VigReset { get; set; }
        public static ConfigEntry<float> DistRate { get; set; }
        public static ConfigEntry<float> DistReset { get; set; }
        public static ConfigEntry<float> GainReduc { get; set; }
        public static ConfigEntry<float> RealTimeGain { get; set; }

        public static ConfigEntry<float> camX { get; set; }
        public static ConfigEntry<float> camY { get; set; }
        public static ConfigEntry<float> camZ { get; set; }

        public static ConfigEntry<float> rotationX { get; set; }
        public static ConfigEntry<float> rotationY { get; set; }
        public static ConfigEntry<float> rotationZ { get; set; }

        public static ConfigEntry<float> offsetX { get; set; }
        public static ConfigEntry<float> offsetY { get; set; }
        public static ConfigEntry<float> offsetZ { get; set; }

        public static ConfigEntry<float> changeTimeMult { get; set; }
        public static ConfigEntry<float> changeTimeIncrease { get; set; }
        public static ConfigEntry<float> resetTimeMult { get; set; }
        public static ConfigEntry<float> restTimeIncrease { get; set; }
        public static ConfigEntry<float> rotationMulti { get; set; }

        public static ConfigEntry<KeyboardShortcut> PassiveAimKeybind { get; set; }

        public static Vector3 WeapStartPosition = new Vector3(0.0f, 0.0f, 0.0f);
        public static Vector3 TargetPosition = new Vector3(0.0f, 0.0f, 0.0f);
        public static bool IsPassiveAiming = false;

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


        public static float EarProtectionFactor;
        public static float AmmoDeafFactor;
        public static float WeaponDeafFactor;
        public static float BotDeafFactor;
        public static float GrenadeDeafFactor;

        public static bool HasHeadSet = false;
        public static CC_FastVignette Vignette;

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
            if (ModConfig.recoil_attachment_overhaul == false)
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
 
                string MiscSettings = "1. Misc. Settings";
                string RecoilSettings = "2. Recoil Settings";
                string WeapStatSettings = "3. Weapon Stat Display Settings";
/*                string AmmoSettings = "4. Ammo Stat Display Settings";*/
                string AdvancedRecoilSettings = "4. Advanced Recoil Settings";
                string WeaponSettings = "5. Weapon Settings";
                string DeafSettings = "6. Deafening and Audio";
                string Testing = "7. Testing";


                /*   enableAmmoDamageDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Damage", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 5 }));
                   enableAmmoFragDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Fragmentation Chance", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 4 }));
                   enableAmmoPenDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Penetration", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 3 }));
                   enableAmmoArmorDamageDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Armor Damage", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 2 }));*/
                enableAmmoFirerateDisp = Config.Bind<bool>(MiscSettings, "Display Ammo Fire Rate", true, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 11 }));

                enableProgramK = Config.Bind<bool>(MiscSettings, "Enable Extended Stock Slots Compatibility", false, new ConfigDescription("Requires Restart. Enables Integration Of The Extended Stock Slots Mod. Each Buffer Position Increases Recoil Reduction While Reducing Ergo The Further Out The Stock Is Extended.", null, new ConfigurationManagerAttributes { Order = 2 }));
                enableFSPatch = Config.Bind<bool>(MiscSettings, "Enable Faceshield Patch", true, new ConfigDescription("Faceshields Block ADS Unless The Specfic Stock/Weapon/Faceshield Allows It.", null, new ConfigurationManagerAttributes { Order = 3 }));
                enableRealArmorClass = Config.Bind<bool>(MiscSettings, "Show Real Armor Class", true, new ConfigDescription("Requiures Restart. Instead Of Showing The Armor's Class As A Number, Use The Real Armor Classification Instead.", null, new ConfigurationManagerAttributes { Order = 8 }));
                enableHoldBreath = Config.Bind<bool>(MiscSettings, "Enable Hold Breath", false, new ConfigDescription("Enabled Hold Breath, Disabled By Default. The Mod Is Balanced Around Not Being Able To Hold Breath.", null, new ConfigurationManagerAttributes { Order = 10 }));

                EnableHipfire = Config.Bind<bool>(RecoilSettings, "Enable Hipfire Recoil Climb", true, new ConfigDescription("Requires Restart. Enabled Recoil Climbing While Hipfiring", null, new ConfigurationManagerAttributes { Order = 4 }));
                reduceCamRecoil = Config.Bind<bool>(RecoilSettings, "Reduce Camera Recoil", false, new ConfigDescription("Reduces Camera Recoil Per Shot. If Disabled, Camera Recoil Becomes More Intense As Weapon Recoil Increases.", null, new ConfigurationManagerAttributes { Order = 3 }));
                SensLimit = Config.Bind<float>(RecoilSettings, "Sensitivity Lower Limit", 0.4f, new ConfigDescription("Sensitivity Lower Limit While Firing. Lower Means More Sensitivity Reduction. 100% Means No Sensitivity Reduction.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes { Order = 2 }));
                RecoilIntensity = Config.Bind<float>(RecoilSettings, "Recoil Intensity", 1f, new ConfigDescription("Changes The Overall Intenisty Of Recoil. This Will Increase/Decrease Horizontal Recoil, Dispersion, Vertical Recoil.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 1 }));

                EnableRecoilClimb = Config.Bind<bool>(AdvancedRecoilSettings, "Enable Recoil Climb", true, new ConfigDescription("The Core Of The Recoil Overhaul. Recoil Increase Per Shot, Nullifying Recoil Auto-Compensation In Full Auto And Requiring A Constant Pull Of The Mouse To Control Recoil. If Diabled Weapons Will Be Completely Unbalanced Without Stat Changes.", null, new ConfigurationManagerAttributes { Order = 13 }));
                SensChangeRate = Config.Bind<float>(AdvancedRecoilSettings, "Sensitivity Change Rate", 0.75f, new ConfigDescription("Rate At Which Sensitivity Is Reduced While Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes { Order = 12 }));
                SensResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Senisitivity Reset Rate", 1.2f, new ConfigDescription("Rate At Which Sensitivity Recovers After Firing. Higher Means Faster Rate.", new AcceptableValueRange<float>(1.01f, 2f), new ConfigurationManagerAttributes { Order = 11 }));
                convergenceSpeedCurve = Config.Bind<float>(AdvancedRecoilSettings, "Convergence Curve Multi", 1.0f, new ConfigDescription("The Convergence Curve. Lower Means More Recoil.", new AcceptableValueRange<float>(0.01f, 1.5f), new ConfigurationManagerAttributes { Order = 10 }));
                vRecoilLimit = Config.Bind<float>(AdvancedRecoilSettings, "Vertical Recoil Upper Limit", 15.0f, new ConfigDescription("The Upper Limit For Vertical Recoil Increase As A Multiplier. E.g Value Of 10 Is A Limit Of 10x Starting Recoil.", new AcceptableValueRange<float>(1f, 50f), new ConfigurationManagerAttributes { Order = 9 }));
                vRecoilChangeMulti = Config.Bind<float>(AdvancedRecoilSettings, "Vertical Recoil Change Rate Multi", 1.01f, new ConfigDescription("A Multiplier For The Verftical Recoil Increase Per Shot.", new AcceptableValueRange<float>(0.9f, 1.1f), new ConfigurationManagerAttributes { Order = 8 }));
                vRecoilResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Vertical Recoil Reset Rate", 0.91f, new ConfigDescription("The Rate At Which Vertical Recoil Resets Over Time After Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 0.99f), new ConfigurationManagerAttributes { Order = 7 }));
                hRecoilLimit = Config.Bind<float>(AdvancedRecoilSettings, "Rearward Recoil Upper Limit", 2.5f, new ConfigDescription("The Upper Limit For Rearward Recoil Increase As A Multiplier. E.g Value Of 10 Is A Limit Of 10x Starting Recoil.", new AcceptableValueRange<float>(1f, 50f), new ConfigurationManagerAttributes { Order = 6 }));
                hRecoilChangeMulti = Config.Bind<float>(AdvancedRecoilSettings, "Rearward Recoil Change Rate Multi", 1.0f, new ConfigDescription("A Multiplier For The Rearward Recoil Increase Per Shot.", new AcceptableValueRange<float>(0.9f, 1.1f), new ConfigurationManagerAttributes { Order = 5 }));
                hRecoilResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Rearward Recoil Reset Rate", 0.91f, new ConfigDescription("The Rate At Which Rearward Recoil Resets Over Time After Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 0.99f), new ConfigurationManagerAttributes { Order = 4 }));
                convergenceResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Convergence Reset Rate", 1.16f, new ConfigDescription("The Rate At Which Convergence Resets Over Time After Firing. Higher Means Faster Rate.", new AcceptableValueRange<float>(1.01f, 2f), new ConfigurationManagerAttributes { Order = 3 }));
                convergenceLimit = Config.Bind<float>(AdvancedRecoilSettings, "Convergence Lower Limit", 0.3f, new ConfigDescription("The Lower Limit For Convergence. Convergence Is Kept In Proportion With Vertical Recoil While Firing, Down To The Set Limit. Value Of 0.3 Means Convegence Lower Limit Of 0.3 * Starting Convergance.", new AcceptableValueRange<float>(0.1f, 1.0f), new ConfigurationManagerAttributes { Order = 2 }));
                resetTime = Config.Bind<float>(AdvancedRecoilSettings, "Time Before Reset", 0.14f, new ConfigDescription("The Time In Seconds That Has To Be Elapsed Before Firing Is Considered Over, Stats Will Not Reset Until This Timer Is Done. Helps Prevent Spam Fire In Full Auto.", new AcceptableValueRange<float>(0.1f, 0.5f), new ConfigurationManagerAttributes { Order = 1 }));

                EnableStatsDelta = Config.Bind<bool>(WeapStatSettings, "Show Stats Delta Preview", false, new ConfigDescription("Requiures Restart. Shows A Preview Of The Difference To Stats Swapping/Removing Attachments Will Make. Warning: Will Degrade Performance Significantly When Moddig Weapons In Inspect Or Modding Screens.", null, new ConfigurationManagerAttributes { Order = 5 }));
                showBalance = Config.Bind<bool>(WeapStatSettings, "Show Balance Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 5 }));
                showCamRecoil = Config.Bind<bool>(WeapStatSettings, "Show Camera Recoil Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 4 }));
                showDispersion = Config.Bind<bool>(WeapStatSettings, "Show Dispersion Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 3 }));
                showRecoilAngle = Config.Bind<bool>(WeapStatSettings, "Show Recoil Angle Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use..", null, new ConfigurationManagerAttributes { Order = 2 }));
                showSemiROF = Config.Bind<bool>(WeapStatSettings, "Show Semi Auto ROF Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 1 }));

                SwayIntensity = Config.Bind<float>(WeaponSettings, "Sway Intensity", 1f, new ConfigDescription("Changes The Intensity Of Aim Sway And Inertia.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 1 }));
                enableReloadPatches = Config.Bind<bool>(WeaponSettings, "Enable Reload And Chamber Speed Changes", true, new ConfigDescription("Requires Restart. Weapon Weight, Magazine Weight, Attachment Reload And Chamber Speed Stat, Balance, Ergo And Arm Injury Affect Reload And Chamber Speed.", null, new ConfigurationManagerAttributes { Order = 2 }));
                enableBarrelFactor = Config.Bind<bool>(WeaponSettings, "Enable Barrel Muzzle Velocity Factor", true, new ConfigDescription("Requires Restart. Muzzle Velocity (Barrel Length) Modifies The Damage, Penetration, Velocity, Fragmentation Chance, And Ballistic Coeficient Of Projectiles.", null, new ConfigurationManagerAttributes { Order = 3 }));
                enableMalfPatch = Config.Bind<bool>(WeaponSettings, "Enable Malfunctions Changes", true, new ConfigDescription("Requires Restart. Malfunction Changes Must Be Enabled On The Server (Config App). You Don't Need To Inspect A Malfunction In Order To Clear It, Subsonic Ammo Needs Special Mods To Cycle, Malfunctions Can Happen At Any Durability But The Chance Is Halved If Above The Durability Threshold.", null, new ConfigurationManagerAttributes { Order = 4 }));
                DuraMalfThreshold = Config.Bind<float>(WeaponSettings, "Malfunction Durability Threshold", 98f, new ConfigDescription("Malfunction Changes Must Be Enabled On The Server (Config App) And 'Enable Malfunctions Changes' Must Be True. Malfunction Chance Is Reduced By Half Until This Durability Threshold Is Met.", new AcceptableValueRange<float>(80f, 100f), new ConfigurationManagerAttributes { Order = 5 }));
                enableSGMastering = Config.Bind<bool>(WeaponSettings, "Enable Increased Shotgun Mastery", true, new ConfigDescription("Requires Restart. Shotguns Will Get Set To Base Lvl 2 Mastery For Reload Animations, Giving Them Better Pump Animations. ADS while Reloading Is Unaffected.", null, new ConfigurationManagerAttributes { Order = 6 }));
                IncreaseCOI = Config.Bind<bool>(WeaponSettings, "Enable Increased Inaccuracy", true, new ConfigDescription("Requires Restart. Increases The Innacuracy Of All By All Weapons So That MOA/Accuracy Is A More Important Stat.", null, new ConfigurationManagerAttributes { Order = 6 }));

                EnableDeafen = Config.Bind<bool>(DeafSettings, "Enable Deafening", true, new ConfigDescription("Requiures Restart. Enables Gunshots And Explosions Deafening The Player.", null, new ConfigurationManagerAttributes { Order = 9 }));
                RealTimeGain = Config.Bind<float>(DeafSettings, "Headset Gain", 13f, new ConfigDescription("WARNING: DO NOT SET THIS TOO HIGH, IT MAY DAMAGE YOUR HEARING! Most EFT Headsets Are Set To 13 By Default, Don't Make It Much Higher. Adjusts The Gain Of Equipped Headsets In Real Time, Acts Just Like The Volume Control On IRL Ear Defenders.", new AcceptableValueRange<float>(0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 8 }));
                GainReduc = Config.Bind<float>(DeafSettings, "Headset Gain Cutoff Multi", 0.75f, new ConfigDescription("How Much Headset Gain Is Reduced While Firing. 0.75 = 25% Reduction.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7 }));
                DeafRate = Config.Bind<float>(DeafSettings, "Deafen Rate", 0.022f, new ConfigDescription("How Quickly Player Gets Deafened. Higher = Faster.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6 }));
                DeafReset = Config.Bind<float>(DeafSettings, "Deafen Reset Rate", 0.035f, new ConfigDescription("How Quickly Player Regains Hearing. Higher = Faster.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5 }));
                VigRate = Config.Bind<float>(DeafSettings, "Tunnel Effect Rate", 0.65f, new ConfigDescription("How Quickly Player Gets Tunnel Vission. Higher = Faster", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4 }));
                VigReset = Config.Bind<float>(DeafSettings, "Tunnel Effect Reset Rate", 1f, new ConfigDescription("How Quickly Player Recovers From Tunnel Vision. Higher = Faster", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3 }));
                DistRate = Config.Bind<float>(DeafSettings, "Distortion Rate", 0.16f, new ConfigDescription("How Quickly Player's Hearing Gets Distorted. Higher = Faster", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2 }));
                DistReset = Config.Bind<float>(DeafSettings, "Distortion Reset Rate", 0.25f, new ConfigDescription("How Quickly Player's Hearing Recovers From Distortion. Higher = Faster", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1 }));

                rotationX = Config.Bind<float>(Testing, "rotationX", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1 }));
                rotationY = Config.Bind<float>(Testing, "rotationY", 45.0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2 }));
                rotationZ = Config.Bind<float>(Testing, "rotationZ", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3 }));

                offsetX = Config.Bind<float>(Testing, "offsetX", -0.045f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4 }));
                offsetY = Config.Bind<float>(Testing, "offsetY", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5 }));
                offsetZ = Config.Bind<float>(Testing, "offsetZ", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6 }));

                camX = Config.Bind<float>(Testing, "camX", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7 }));
                camY = Config.Bind<float>(Testing, "camY", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 8 }));
                camZ = Config.Bind<float>(Testing, "camZ", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 9 }));

                changeTimeMult = Config.Bind<float>(Testing, "changeTimeMult", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-100f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10 }));
                changeTimeIncrease = Config.Bind<float>(Testing, "changeTimeIncrease", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-100f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11 }));
                resetTimeMult = Config.Bind<float>(Testing, "resetTimeMult", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-100f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12 }));
                restTimeIncrease = Config.Bind<float>(Testing, "restTimeIncrease", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-100f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 13 }));
                rotationMulti = Config.Bind<float>(Testing, "rotationMulti", 0.0f, new ConfigDescription("", new AcceptableValueRange<float>(-100f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 14 }));

                PassiveAimKeybind = Config.Bind(Testing, "", new KeyboardShortcut(KeyCode.X), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 15 }));

                if (enableProgramK.Value == true)
                {
                    Helper.ProgramKEnabled = true;
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
                if (Plugin.EnableHipfire.Value == true)
                {
                    new GetRotationMultiplierPatch().Enable();
                }

                //Aiming Patches + Reload Trigger
                new AimingPatch().Enable();
                new ToggleAimPatch().Enable();

                //Malf Patches
                if (enableMalfPatch.Value == true && ModConfig.malf_changes == true)
                {
                    new IsKnownMalfTypePatch().Enable();
                    new GetTotalMalfunctionChancePatch().Enable();
                }


                //Reload Patches
                if (Plugin.enableReloadPatches.Value == true)
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
                if (enableBarrelFactor.Value == true)
                {
                    new CreateShotPatch().Enable();
                }
/*
                new ApplyDamagePatch().Enable();
                new DamageInfoPatch().Enable();
                new ApplyDamageInfoPatch().Enable();
                new SetPenetrationStatusPatch().Enable();
*/
                //Armor
                if (enableRealArmorClass.Value == true)
                {
                    new ArmorClassDisplayPatch().Enable();
                }
                new ArmorComponentPatch().Enable();
                new RigConstructorPatch().Enable();

                //Player
                new EnduranceSprintActionPatch().Enable();
                new EnduranceMovementActionPatch().Enable();
                new ToggleHoldingBreathPatch().Enable();
                new PlayerInitPatch().Enable();

                //Shot Effects
                if (EnableDeafen.Value == true)
                {
                    new VignettePatch().Enable();
                    new UpdatePhonesPatch().Enable();
                    new SetCompressorPatch().Enable();
                    new RegisterShotPatch().Enable();
                    new ExplosionPatch().Enable();
                }

                //LateUpdate
                new PlayerLateUpdatePatch().Enable();

                new ApplyComplexRotationPatch().Enable();
                new InitTransformsPatch().Enable();

            }
        }

        void Update()
        {
            if (IsConfigCorrect == true)
            {
                if (checkedForUniformAim == false)
                {
                    isUniformAimPresent = Chainloader.PluginInfos.ContainsKey("com.notGreg.UniformAim");
                    isBridgePresent = Chainloader.PluginInfos.ContainsKey("com.notGreg.RealismModBridge");
                    checkedForUniformAim = true;
                }

                if (Helper.CheckIsReady())
                {

                    if (Plugin.ShotCount > Plugin.PrevShotCount)
                    {
                        Plugin.IsFiring = true;
                    }

                    if (Plugin.EnableRecoilClimb.Value == true && (Plugin.IsAiming == true || Plugin.EnableHipfire.Value == true))
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
                    }

                    if (Plugin.IsBotFiring == true)
                    {
                        Plugin.BotTimer += Time.deltaTime;
                        if (Plugin.BotTimer >= 1f)
                        {
                            Plugin.IsBotFiring = false;
                            Plugin.BotTimer = 0f;
                        }
                    }

                    if (Plugin.GrenadeExploded == true)
                    {
                        Plugin.GrenadeTimer += Time.deltaTime;
                        if (Plugin.GrenadeTimer >= 1f)
                        {
                            Plugin.GrenadeExploded = false;
                            Plugin.GrenadeTimer = 0f;
                        }
                    }

                    if (EnableDeafen.Value == true)
                    {
                        Deafening.DoDeafening();
                    }

                    if (Plugin.IsFiring == false)
                    {
                        Recoil.ResetRecoil();
                    }

                    if (Input.GetKeyDown(PassiveAimKeybind.Value.MainKey))
                    {
                        Plugin.IsPassiveAiming = true;
                    }
                    if (Input.GetKeyUp(PassiveAimKeybind.Value.MainKey))
                    {
                        Plugin.IsPassiveAiming = false;
                    }
                }
            }
        }
    }
}

