using BepInEx;
using System;
using UnityEngine;
using Aki.Common.Http;
using Aki.Common.Utils;
using System.IO;
using System.Collections.Generic;
using BepInEx.Configuration;
using static RealismMod.Attributes;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
using static RealismMod.ArmorPatches;
using BepInEx.Bootstrap;

namespace RealismMod
{

    public class ConfigTemplate
    {
        public bool recoil_attachment_overhaul { get; set; }
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
        public static ConfigEntry<float> sensChangeRate { get; set; }
        public static ConfigEntry<float> sensResetRate { get; set; }
        public static ConfigEntry<float> sensLimit { get; set; }
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
        public static ConfigEntry<bool> enableReloadPafches { get; set; }
        public static ConfigEntry<bool> enableRealArmorClass { get; set; }


        public static bool isFiring = false;
        public static bool isAiming;
        public static float timer = 0.0f;
        public static float shotCount = 0;
        private float prevShotCount = shotCount;
        private bool statsAreReset;

        public static float startingRecoilAngle;

        public static float startingSens;
        public static float currentSens = startingSens;

        public static float startingDispersion;
        public static float currentDispersion;
        public static float dispersionProportionK;

        public static float startingDamping;
        public static float currentDamping;

        public static float startingHandDamping;
        public static float currentHandDamping;

        public static float startingConvergence;
        public static float currentConvergence;
        public static float convergenceProporitonK;

        public static float startingCamRecoilX;
        public static float startingCamRecoilY;
        public static float currentCamRecoilX;
        public static float currentCamRecoilY;

        public static float startingVRecoilX;
        public static float startingVRecoilY;
        public static float currentVRecoilX;
        public static float currentVRecoilY;

        public static float startingHRecoilX;
        public static float startingHRecoilY;
        public static float currentHRecoilX;
        public static float currentHRecoilY;

        public static Dictionary<Enum, Sprite> IconCache = new Dictionary<Enum, Sprite>();

        private string ModPath;
        private string ConfigFilePath;
        private string ConfigJson;
        private ConfigTemplate ModConfig;
        private bool IsConfigCorrect = true;

        public static bool isUniformAimPresent = false;
        public static bool isBridgePresent = false;
        public static bool checkedForUniformAim = false;

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

            GetPaths();
            ConfigCheck();
            CacheIcons();

            if (IsConfigCorrect == true)
            {
 
                string MiscSettings = "1. Misc. Settigns";
                string WeapStatSettings = "2. Weapon Stat Settings";
                string RecoilSettings = "3. Recoil Settings";
                string AdvancedRecoilSettings = "4. Advanced Settings";

                /*   enableAmmoDamageDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Damage", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 4 }));
                   enableAmmoFragDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Fragmentation Chance", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 3 }));
                   enableAmmoPenDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Penetration", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 2 }));
                   enableAmmoArmorDamageDisp = Config.Bind<bool>(AmmoSettings, "Display Ammo Armor Damage", false, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 1 }));*/

                enableAmmoFirerateDisp = Config.Bind<bool>(MiscSettings, "Display Ammo Fire Rate", true, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 5 }));
                enableProgramK = Config.Bind<bool>(MiscSettings, "Enable Extended Stock Slots Compatibility", false, new ConfigDescription("Requires Restart. Enables Integration Of The Extended Stock Slots Mod. Each Buffer Position Increases Recoil Reduction While Reducing Ergo The Further Out The Stock Is Extended.", null, new ConfigurationManagerAttributes { Order = 1 }));
                enableFSPatch = Config.Bind<bool>(MiscSettings, "Enable Faceshield Patch", true, new ConfigDescription("Faceshields Block ADS Unless The Specfic Stock/Weapon/Faceshield Allows It.", null, new ConfigurationManagerAttributes { Order = 2 }));
                enableMalfPatch = Config.Bind<bool>(MiscSettings, "Enable Inspectionless Malfuctions Patch", true, new ConfigDescription("Requires Restart. You Don't Need To Inspect A Malfunction In Order To Clear It.", null, new ConfigurationManagerAttributes { Order = 3 }));
                enableSGMastering = Config.Bind<bool>(MiscSettings, "Enable Increased Shotgun Mastery", true, new ConfigDescription("Requires Restart. Shotguns Will Get Set To Base Lvl 2 Mastery For Reload Animations, Giving Them Better Pump Animations. ADS while Reloading Is Unaffected.", null, new ConfigurationManagerAttributes { Order = 4 }));
                enableBarrelFactor = Config.Bind<bool>(MiscSettings, "Enable Barrel Factor", true, new ConfigDescription("Requires Restart. Barrel Length Modifies The Damage, Penetration, Velocity, Fragmentation Chance, And Ballistic Coeficient Of Projectiles.", null, new ConfigurationManagerAttributes { Order = 5 }));
                enableRealArmorClass = Config.Bind<bool>(MiscSettings, "Show Real Armor Class", false, new ConfigDescription("Requiures Restart. Instead Of Showing The Armor's Class As A Number, Use The Real Armor Classification Instead.", null, new ConfigurationManagerAttributes { Order = 6 }));

                sensLimit = Config.Bind<float>(RecoilSettings, "Sensitivity Lower Limit", 0.5f, new ConfigDescription("Sensitivity Lower Limit While Firing. Lower Means More Sensitivity Reduction. 100% Means No Sensitivity Reduction.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 2 }));
                sensChangeRate = Config.Bind<float>(RecoilSettings, "Sensitivity Change Rate", 0.82f, new ConfigDescription("Rate At Which Sensitivity Is Reduced While Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes { Order = 1 }));
                sensResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Senisitivity Reset Rate", 1.2f, new ConfigDescription("Rate At Which Sensitivity Recovers After Firing. Higher Means Faster Rate.", new AcceptableValueRange<float>(1.01f, 2f), new ConfigurationManagerAttributes { Order = 9 }));

                vRecoilLimit = Config.Bind<float>(AdvancedRecoilSettings, "Vertical Recoil Upper Limit", 10f, new ConfigDescription("The Upper Limit For Vertical Recoil Increase As A Multiplier. E.g Value Of 10 Is A Limit Of 10x Starting Recoil.", new AcceptableValueRange<float>(1f, 50f), new ConfigurationManagerAttributes { Order = 8 }));
                vRecoilChangeMulti = Config.Bind<float>(AdvancedRecoilSettings, "Vertical Recoil Change Rate Multi", 1.01f, new ConfigDescription("A Multiplier For The Vertical Recoil Increase Per Shot.", new AcceptableValueRange<float>(1.0f, 1.1f), new ConfigurationManagerAttributes { Order = 7 }));
                vRecoilResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Vertical Recoil Reset Rate", 0.91f, new ConfigDescription("The Rate At Which Vertical Recoil Resets Over Time After Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 0.99f), new ConfigurationManagerAttributes { Order = 6 }));
                hRecoilLimit = Config.Bind<float>(AdvancedRecoilSettings, "Rearward Recoil Upper Limit", 10f, new ConfigDescription("The Upper Limit For Rearward Recoil Increase As A Multiplier. E.g Value Of 10 Is A Limit Of 10x Starting Recoil.", new AcceptableValueRange<float>(1f, 50f), new ConfigurationManagerAttributes { Order = 5 }));
                hRecoilChangeMulti = Config.Bind<float>(AdvancedRecoilSettings, "Rearward Recoil Change Rate Multi", 1.01f, new ConfigDescription("A Multiplier For The Rearward Recoil Increase Per Shot.", new AcceptableValueRange<float>(1.0f, 1.1f), new ConfigurationManagerAttributes { Order = 4 }));
                hRecoilResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Rearward Recoil Reset Rate", 0.91f, new ConfigDescription("The Rate At Which Rearward Recoil Resets Over Time After Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 0.99f), new ConfigurationManagerAttributes { Order = 3 }));
                convergenceResetRate = Config.Bind<float>(AdvancedRecoilSettings, "Convergence Reset Rate", 1.16f, new ConfigDescription("The Rate At Which Convergence Resets Over Time After Firing. Higher Means Faster Rate.", new AcceptableValueRange<float>(1.01f, 2f), new ConfigurationManagerAttributes { Order = 2 }));
                convergenceLimit = Config.Bind<float>(AdvancedRecoilSettings, "Convergence Lower Limit", 0.3f, new ConfigDescription("The Lower Limit For Convergence. Convergence Is Kept In Proportion With Vertical Recoil While Firing, Down To The Set Limit. Value Of 0.3 Means Convegence Lower Limit Of 0.3 * Starting Convergance.", new AcceptableValueRange<float>(0.1f, 1.0f), new ConfigurationManagerAttributes { Order = 1 }));
                resetTime = Config.Bind<float>(AdvancedRecoilSettings, "Time Before Reset", 0.15f, new ConfigDescription("The Time In Seconds That Has To Be Elapsed Before Firing Is Considered Over, Stats Will Not Reset Until This Timer Is Done. Helps Prevent Spam Fire In Full Auto.", new AcceptableValueRange<float>(0.1f, 0.3f), new ConfigurationManagerAttributes { Order = 1 }));

                showBalance = Config.Bind<bool>(WeapStatSettings, "Show Balance Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 5 }));
                showCamRecoil = Config.Bind<bool>(WeapStatSettings, "Show Camera Recoil Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 4 }));
                showDispersion = Config.Bind<bool>(WeapStatSettings, "Show Dispersion Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 3 }));
                showRecoilAngle = Config.Bind<bool>(WeapStatSettings, "Show Recoil Angle Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use..", null, new ConfigurationManagerAttributes { Order = 2 }));
                showSemiROF = Config.Bind<bool>(WeapStatSettings, "Show Semi Auto ROF Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 1 }));

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
                new method_5Patch().Enable();

                new SyncWithCharacterSkillsPatch().Enable();
                new UpdateWeaponVariablesPatch().Enable();
                new SetAimingSlowdownPatch().Enable();

                //sway and aim inertia
                new method_17Patch().Enable();
                new UpdateSwayFactorsPatch().Enable();
                new OverweightPatch().Enable();

                //Recoil Patches
                new OnWeaponParametersChangedPatch().Enable();
                new ProcessPatch().Enable();
                new ShootPatch().Enable();
                new AimingSensitivityPatch().Enable();
                new UpdateSensitivityPatch().Enable();

                //Aiming Patches + Reload Trigger
                new AimingPatch().Enable();
                new ToggleAimPatch().Enable();

                //Malf Patches
                if (enableMalfPatch.Value == true)
                {
                    new IsKnownMalfTypePatch().Enable();
                }

                //Reload Patches
                if (Plugin.enableReloadPafches.Value == true)
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
                }

                if (enableSGMastering.Value == true)
                {
                    new SetWeaponLevelPatch().Enable();
                }

                //Stat Display Patches
                new ModConstructorPatch().Enable();
                new WeaponConstructorPatch().Enable();
                new HRecoilDisplayValuePatch().Enable();
                new HRecoilDisplayDeltaPatch().Enable();
                new VRecoilDisplayValuePatch().Enable();
                new VRecoilDisplayDeltaPatch().Enable();
                new ModVRecoilStatDisplayPatchFloat().Enable();
                new ModVRecoilStatDisplayPatchString().Enable();
                new ErgoDisplayDeltaPatch().Enable();
                new ErgoDisplayValuePatch().Enable();
                new COIDisplayDeltaPatch().Enable();
                new COIDisplayValuePatch().Enable();
                new FireRateDisplayStringPatch().Enable();
                new GetCachedReadonlyQualitiesPatch().Enable();
                new CenterOfImpactMOAPatch().Enable();
                new ModErgoStatDisplayPatch().Enable();

                new GetAttributeIconPatches().Enable();

                //Ballistics
                if (enableBarrelFactor.Value == true)
                {
                    new CreateShotPatch().Enable();
                }

                //Armor
                if (enableRealArmorClass.Value == true)
                {
                    new ArmorClassDisplayPatch().Enable();
                }
                new ArmorComponentPatch().Enable();

                //Player
                new EnduranceSprintActionPatch().Enable();
                new EnduranceMovementActionPatch().Enable();

                //Tests

            }
        }


        void Update()
        {
            if (checkedForUniformAim == false)
            {

                isUniformAimPresent = Chainloader.PluginInfos.ContainsKey("com.notGreg.UniformAim");
                isBridgePresent = Chainloader.PluginInfos.ContainsKey("com.notGreg.RealismModBridge");
                checkedForUniformAim = true;
            }

            if (Helper.CheckIsReady())
            {
                Helper.IsReady = true;
                if (isAiming == true)
                {
                    if (shotCount > prevShotCount)
                    {
                        if (shotCount >= 1 && shotCount <= 3)
                        {
                            currentVRecoilX = Mathf.Clamp((float)Math.Round(currentVRecoilX * 1.13f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilX, startingVRecoilX * Plugin.vRecoilLimit.Value);
                            currentVRecoilY = Mathf.Clamp((float)Math.Round(currentVRecoilY * 1.13f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilY, startingVRecoilY * Plugin.vRecoilLimit.Value);

                            currentHRecoilX = Mathf.Clamp((float)Math.Round(currentHRecoilX * 1.12f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilX, startingHRecoilX * Plugin.hRecoilLimit.Value);
                            currentHRecoilY = Mathf.Clamp((float)Math.Round(currentHRecoilY * 1.13f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilY, startingHRecoilY * Plugin.hRecoilLimit.Value);

                            currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence), 2), startingConvergence * Plugin.convergenceLimit.Value, currentConvergence);

                        }
                        if (shotCount >= 4 && shotCount <= 5)
                        {
                            currentVRecoilX = Mathf.Clamp((float)Math.Round(currentVRecoilX * 1.125f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilX, startingVRecoilX * Plugin.vRecoilLimit.Value);
                            currentVRecoilY = Mathf.Clamp((float)Math.Round(currentVRecoilY * 1.125f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilY, startingVRecoilY * Plugin.vRecoilLimit.Value);

                            currentHRecoilX = Mathf.Clamp((float)Math.Round(currentHRecoilX * 1.125f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilX, startingHRecoilX * Plugin.hRecoilLimit.Value);
                            currentHRecoilY = Mathf.Clamp((float)Math.Round(currentHRecoilY * 1.125f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilY, startingHRecoilY * Plugin.hRecoilLimit.Value);

                            currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence), 2), startingConvergence * Plugin.convergenceLimit.Value, currentConvergence);

                        }
                        if (shotCount > 5 && shotCount <= 7)
                        {
                            currentVRecoilX = Mathf.Clamp((float)Math.Round(currentVRecoilX * 1.1f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilX, startingVRecoilX * Plugin.vRecoilLimit.Value);
                            currentVRecoilY = Mathf.Clamp((float)Math.Round(currentVRecoilY * 1.1f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilY, startingVRecoilY * Plugin.vRecoilLimit.Value);

                            currentHRecoilX = Mathf.Clamp((float)Math.Round(currentHRecoilX * 1.1f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilX, startingHRecoilX * Plugin.hRecoilLimit.Value);
                            currentHRecoilY = Mathf.Clamp((float)Math.Round(currentHRecoilY * 1.1f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilY, startingHRecoilY * Plugin.hRecoilLimit.Value);

                            currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence), 2), startingConvergence * Plugin.convergenceLimit.Value, currentConvergence);

                        }
                        if (shotCount > 8 && shotCount <= 10)
                        {
                            currentVRecoilX = Mathf.Clamp((float)Math.Round(currentVRecoilX * 1.08f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilX, startingVRecoilX * Plugin.vRecoilLimit.Value);
                            currentVRecoilY = Mathf.Clamp((float)Math.Round(currentVRecoilY * 1.08f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilY, startingVRecoilY * Plugin.vRecoilLimit.Value);

                            currentHRecoilX = Mathf.Clamp((float)Math.Round(currentHRecoilX * 1.07f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilX, startingHRecoilX * Plugin.hRecoilLimit.Value);
                            currentHRecoilY = Mathf.Clamp((float)Math.Round(currentHRecoilY * 1.07f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilY, startingHRecoilY * Plugin.hRecoilLimit.Value);

                            currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence), 2), startingConvergence * Plugin.convergenceLimit.Value, currentConvergence);
                        }
                        if (shotCount > 10 && shotCount <= 15)
                        {
                            currentVRecoilX = Mathf.Clamp((float)Math.Round(currentVRecoilX * 1.05f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilX, startingVRecoilX * Plugin.vRecoilLimit.Value);
                            currentVRecoilY = Mathf.Clamp((float)Math.Round(currentVRecoilY * 1.05f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilY, startingVRecoilY * Plugin.vRecoilLimit.Value);

                            currentHRecoilX = Mathf.Clamp((float)Math.Round(currentHRecoilX * 1.04f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilX, startingHRecoilX * Plugin.hRecoilLimit.Value);
                            currentHRecoilY = Mathf.Clamp((float)Math.Round(currentHRecoilY * 1.04f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilY, startingHRecoilY * Plugin.hRecoilLimit.Value);

                            currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence), 2), startingConvergence * Plugin.convergenceLimit.Value, currentConvergence);

                            currentDamping = Mathf.Clamp((float)Math.Round(currentDamping * 0.98f, 3), startingDamping * WeaponProperties.DampingLimit, currentDamping);
                            currentHandDamping = Mathf.Clamp((float)Math.Round(currentHandDamping * 0.98f, 3), startingHandDamping * WeaponProperties.DampingLimit, currentHandDamping);

                        }

                        if (shotCount > 15 && shotCount <= 20)
                        {
                            currentVRecoilX = Mathf.Clamp((float)Math.Round(currentVRecoilX * 1.03f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilX, startingVRecoilX * Plugin.vRecoilLimit.Value);
                            currentVRecoilY = Mathf.Clamp((float)Math.Round(currentVRecoilY * 1.03 * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilY, startingVRecoilY * Plugin.vRecoilLimit.Value);

                            currentHRecoilX = Mathf.Clamp((float)Math.Round(currentHRecoilX * 1.02f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilX, startingHRecoilX * Plugin.hRecoilLimit.Value);
                            currentHRecoilY = Mathf.Clamp((float)Math.Round(currentHRecoilY * 1.02f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilY, startingHRecoilY * Plugin.hRecoilLimit.Value);

                            currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence), 2), startingConvergence * Plugin.convergenceLimit.Value, currentConvergence);

                            currentDamping = Mathf.Clamp((float)Math.Round(currentDamping * 0.98f, 3), startingDamping * WeaponProperties.DampingLimit, currentDamping);
                            currentHandDamping = Mathf.Clamp((float)Math.Round(currentHandDamping * 0.98f, 3), startingHandDamping * WeaponProperties.DampingLimit, currentHandDamping);
                        }

                        if (shotCount > 20 && shotCount <= 25)
                        {
                            currentVRecoilX = Mathf.Clamp((float)Math.Round(currentVRecoilX * 1.03f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilX, startingVRecoilX * Plugin.vRecoilLimit.Value);
                            currentVRecoilY = Mathf.Clamp((float)Math.Round(currentVRecoilY * 1.03f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilY, startingVRecoilY * Plugin.vRecoilLimit.Value);

                            currentHRecoilX = Mathf.Clamp((float)Math.Round(currentHRecoilX * 1.01f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilX, startingHRecoilX * Plugin.hRecoilLimit.Value);
                            currentHRecoilY = Mathf.Clamp((float)Math.Round(currentHRecoilY * 1.01f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilY, startingHRecoilY * Plugin.hRecoilLimit.Value);

                            currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence), 2), startingConvergence * Plugin.convergenceLimit.Value, currentConvergence);

                            currentDamping = Mathf.Clamp((float)Math.Round(currentDamping * 0.98f, 3), startingDamping * WeaponProperties.DampingLimit, currentDamping);
                            currentHandDamping = Mathf.Clamp((float)Math.Round(currentHandDamping * 0.98f, 3), startingHandDamping * WeaponProperties.DampingLimit, currentHandDamping);
                        }

                        if (shotCount > 25 && shotCount <= 30)
                        {
                            currentVRecoilX = Mathf.Clamp((float)Math.Round(currentVRecoilX * 1.03f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilX, startingVRecoilX * Plugin.vRecoilLimit.Value);
                            currentVRecoilY = Mathf.Clamp((float)Math.Round(currentVRecoilY * 1.03f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilY, startingVRecoilY * Plugin.vRecoilLimit.Value);

                            currentHRecoilX = Mathf.Clamp((float)Math.Round(currentHRecoilX * 1.01f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilX, startingHRecoilX * Plugin.hRecoilLimit.Value);
                            currentHRecoilY = Mathf.Clamp((float)Math.Round(currentHRecoilY * 1.01f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilY, startingHRecoilY * Plugin.hRecoilLimit.Value);

                            currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence), 2), startingConvergence * Plugin.convergenceLimit.Value, currentConvergence);

                            currentDamping = Mathf.Clamp((float)Math.Round(currentDamping * 0.98f, 3), startingDamping * WeaponProperties.DampingLimit, currentDamping);
                            currentHandDamping = Mathf.Clamp((float)Math.Round(currentHandDamping * 0.98f, 3), startingHandDamping * WeaponProperties.DampingLimit, currentHandDamping);
                        }

                        if (shotCount > 30 && shotCount <= 35)
                        {
                            currentVRecoilX = Mathf.Clamp((float)Math.Round(currentVRecoilX * 1.03f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilX, startingVRecoilX * Plugin.vRecoilLimit.Value);
                            currentVRecoilY = Mathf.Clamp((float)Math.Round(currentVRecoilY * 1.03f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilY, startingVRecoilY * Plugin.vRecoilLimit.Value);

                            currentHRecoilX = Mathf.Clamp((float)Math.Round(currentHRecoilX * 1.01f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilX, startingHRecoilX * Plugin.hRecoilLimit.Value);
                            currentHRecoilY = Mathf.Clamp((float)Math.Round(currentHRecoilY * 1.01f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilY, startingHRecoilY * Plugin.hRecoilLimit.Value);

                            currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence), 2), startingConvergence * Plugin.convergenceLimit.Value, currentConvergence);

                            currentDamping = Mathf.Clamp((float)Math.Round(currentDamping * 0.98f, 3), startingDamping * WeaponProperties.DampingLimit, currentDamping);
                            currentHandDamping = Mathf.Clamp((float)Math.Round(currentHandDamping * 0.98f, 3), startingHandDamping * WeaponProperties.DampingLimit, currentHandDamping);
                        }

                        if (shotCount > 35)
                        {
                            currentVRecoilX = Mathf.Clamp((float)Math.Round(currentVRecoilX * 1.03f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilX, startingVRecoilX * Plugin.vRecoilLimit.Value);
                            currentVRecoilY = Mathf.Clamp((float)Math.Round(currentVRecoilY * 1.03f * Plugin.vRecoilChangeMulti.Value, 3), currentVRecoilY, startingVRecoilY * Plugin.vRecoilLimit.Value);

                            currentHRecoilX = Mathf.Clamp((float)Math.Round(currentHRecoilX * 1.01f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilX, startingHRecoilX * Plugin.hRecoilLimit.Value);
                            currentHRecoilY = Mathf.Clamp((float)Math.Round(currentHRecoilY * 1.01f * Plugin.hRecoilChangeMulti.Value, 3), currentHRecoilY, startingHRecoilY * Plugin.hRecoilLimit.Value);

                            currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((convergenceProporitonK / currentVRecoilX), currentConvergence), 2), startingConvergence * Plugin.convergenceLimit.Value, currentConvergence);

                            currentDamping = Mathf.Clamp((float)Math.Round(currentDamping * 0.98f, 3), startingDamping * WeaponProperties.DampingLimit, currentDamping);
                            currentHandDamping = Mathf.Clamp((float)Math.Round(currentHandDamping * 0.98f, 3), startingHandDamping * WeaponProperties.DampingLimit, currentHandDamping);
                        }

                        currentSens = Mathf.Clamp((float)Math.Round(currentSens * Plugin.sensChangeRate.Value, 4), startingSens * Plugin.sensLimit.Value, currentSens);

                        prevShotCount = shotCount;
                        isFiring = true;
                    }
                }
                else
                {
                    if (shotCount > prevShotCount)
                    {
                        prevShotCount = shotCount;
                        isFiring = true;
                    }
                }


                if (shotCount == prevShotCount)
                {
                    timer += Time.deltaTime;
                    if (timer >= Plugin.resetTime.Value)
                    {
                        isFiring = false;
                        shotCount = 0;
                        prevShotCount = 0;
                        timer = 0f;
                    }
                }

                if (isFiring == false)
                {
                    if (startingSens <= currentSens && startingConvergence <= currentConvergence && startingVRecoilX >= currentVRecoilX)
                    {
                        statsAreReset = true;
                    }
                    else
                    {
                        statsAreReset = false;
                    }
                }

                if (statsAreReset == false && isFiring == false)
                {

                    currentSens = Mathf.Clamp(currentSens * sensResetRate.Value, currentSens, startingSens);

                    currentConvergence = Mathf.Clamp(currentConvergence * Plugin.convergenceResetRate.Value, currentConvergence, startingConvergence);

                    currentDamping = Mathf.Clamp(currentDamping * WeaponProperties.DampingResetRate, currentDamping, startingDamping);
                    currentHandDamping = Mathf.Clamp(currentHandDamping * WeaponProperties.DampingResetRate, currentHandDamping, startingHandDamping);

                    currentVRecoilX = Mathf.Clamp(currentVRecoilX * Plugin.vRecoilResetRate.Value, startingVRecoilX, currentVRecoilX);
                    currentVRecoilY = Mathf.Clamp(currentVRecoilY * Plugin.vRecoilResetRate.Value, startingVRecoilY, currentVRecoilY);

                    currentHRecoilX = Mathf.Clamp(currentHRecoilX * Plugin.hRecoilResetRate.Value, startingHRecoilX, currentHRecoilX);
                    currentHRecoilY = Mathf.Clamp(currentHRecoilY * Plugin.hRecoilResetRate.Value, startingHRecoilY, currentHRecoilY);

                }
                if (statsAreReset == true && isFiring == false)
                {
                    Plugin.currentSens = Plugin.startingSens;
                }
            }
            else
            {
                Helper.IsReady = false;
            }
        }
    }
}

