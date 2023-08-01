using Aki.Common.Http;
using Aki.Common.Utils;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Animals;
using EFT.InventoryLogic;
using EFT.UI;
using Newtonsoft.Json;
using notGreg.UniformAim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static RealismMod.GearPatches;
using static RealismMod.Attributes;
using static RootMotion.FinalIK.AimPoser;
using HarmonyLib;

namespace RealismMod
{
    public class ConfigTemplate
    {
        public bool recoil_attachment_overhaul { get; set; }
        public bool malf_changes { get; set; }
        public bool realistic_ballistics { get; set; }
        public bool med_changes { get; set; }
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> ConvSemiMulti { get; set; }
        public static ConfigEntry<float> ConvAutoMulti { get; set; }
        public static ConfigEntry<float> resetTime { get; set; }
        public static ConfigEntry<float> vRecoilLimit { get; set; }
        public static ConfigEntry<float> hRecoilLimit { get; set; }
        public static ConfigEntry<float> convergenceLimit { get; set; }
        public static ConfigEntry<float> ConvergenceResetRate { get; set; }
        public static ConfigEntry<float> vRecoilChangeMulti { get; set; }
        public static ConfigEntry<float> vRecoilResetRate { get; set; }
        public static ConfigEntry<float> hRecoilChangeMulti { get; set; }
        public static ConfigEntry<float> hRecoilResetRate { get; set; }
        public static ConfigEntry<float> SensChangeRate { get; set; }
        public static ConfigEntry<float> SensResetRate { get; set; }
        public static ConfigEntry<float> SensLimit { get; set; }
        public static ConfigEntry<bool> ShowBalance { get; set; }
        public static ConfigEntry<bool> ShowCamRecoil { get; set; }
        public static ConfigEntry<bool> ShowDispersion { get; set; }
        public static ConfigEntry<bool> ShowRecoilAngle { get; set; }
        public static ConfigEntry<bool> ShowSemiROF { get; set; }
        public static ConfigEntry<bool> EnableFSPatch { get; set; }
        public static ConfigEntry<bool> EnableNVGPatch { get; set; }
        public static ConfigEntry<bool> EnableMalfPatch { get; set; }
        public static ConfigEntry<bool> InspectionlessMalfs { get; set; }
        public static ConfigEntry<bool> enableSGMastering { get; set; }
        public static ConfigEntry<bool> EnableStockSlots { get; set; }
        public static ConfigEntry<bool> EnableAmmoStats { get; set; }
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
        public static ConfigEntry<float> VertRecSemiMulti { get; set; }
        public static ConfigEntry<float> VertRecAutoMulti { get; set; }
        public static ConfigEntry<float> HorzRecSemiMulti { get; set; }
        public static ConfigEntry<float> HorzRecAutoMulti { get; set; }
        public static ConfigEntry<float> HorzRecLimit { get; set; }

        public static ConfigEntry<bool> EnableStatsDelta { get; set; }
        public static ConfigEntry<bool> EnableHipfireRecoilClimb { get; set; }
        public static ConfigEntry<bool> IncreaseCOI { get; set; }

        public static ConfigEntry<float> DeafRate { get; set; }
        public static ConfigEntry<float> DeafReset { get; set; }
        public static ConfigEntry<float> VigRate { get; set; }
        public static ConfigEntry<float> VigReset { get; set; }
        public static ConfigEntry<float> DistRate { get; set; }
        public static ConfigEntry<float> DistReset { get; set; }
        public static ConfigEntry<float> GainCutoff { get; set; }
        public static ConfigEntry<float> RealTimeGain { get; set; }
        public static ConfigEntry<float> HeadsetAmbientMulti { get; set; }
        public static ConfigEntry<KeyboardShortcut> IncGain { get; set; }
        public static ConfigEntry<KeyboardShortcut> DecGain { get; set; }

        public static ConfigEntry<KeyboardShortcut> ActiveAimKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> LowReadyKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> HighReadyKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> ShortStockKeybind { get; set; }
        public static ConfigEntry<KeyboardShortcut> CycleStancesKeybind { get; set; }

        public static ConfigEntry<bool> ToggleActiveAim { get; set; }
        public static ConfigEntry<bool> StanceToggleDevice { get; set; }
        public static ConfigEntry<bool> ActiveAimReload { get; set; }
        public static ConfigEntry<bool> EnableAltPistol { get; set; }
        public static ConfigEntry<bool> EnableIdleStamDrain { get; set; }
        public static ConfigEntry<bool> EnableStanceStamChanges { get; set; }
        public static ConfigEntry<bool> EnableTacSprint { get; set; }
        public static ConfigEntry<bool> EnableSprintPenalty { get; set; }
        
        public static ConfigEntry<float> WeapOffsetX { get; set; }
        public static ConfigEntry<float> WeapOffsetY { get; set; }
        public static ConfigEntry<float> WeapOffsetZ { get; set; }

        public static ConfigEntry<float> StanceTransitionSpeed { get; set; }
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

        public static ConfigEntry<bool> EnableArmorHitZones { get; set; }
        public static ConfigEntry<bool> EnableBodyHitZones { get; set; }
        public static ConfigEntry<bool> EnablePlayerArmorZones { get; set; }
        public static ConfigEntry<bool> EnableArmPen { get; set; }
        public static ConfigEntry<bool> EnableHitSounds { get; set; }
        public static ConfigEntry<float> FleshHitSoundMulti { get; set; }
        public static ConfigEntry<float> ArmorCloseHitSoundMulti { get; set; }
        public static ConfigEntry<float> ArmorFarHitSoundMulti { get; set; }
        public static ConfigEntry<bool> EnableRagdollFix { get; set; }
        public static ConfigEntry<float> RagdollForceModifier { get; set; }

        public static ConfigEntry<bool> EnableLogging { get; set; }
        public static ConfigEntry<bool> EnableBallisticsLogging { get; set; }

        public static ConfigEntry<KeyboardShortcut> AddEffectKeybind { get; set; }
        public static ConfigEntry<int> AddEffectBodyPart { get; set; }
        public static ConfigEntry<String> AddEffectType { get; set; }

        public static ConfigEntry<bool> EnableMedicalOvehaul { get; set; }
        public static ConfigEntry<bool> GearBlocksHeal { get; set; }
        public static ConfigEntry<bool> GearBlocksEat { get; set; }
        public static ConfigEntry<bool> EnableAdrenaline { get; set; }
        public static ConfigEntry<bool> EnableTrnqtEffect { get; set; }
        public static ConfigEntry<bool> EnableHealthEffects { get; set; }
        public static ConfigEntry<KeyboardShortcut> DropGearKeybind { get; set; }

        public static ConfigEntry<bool> EnableMaterialSpeed { get; set; }
        public static ConfigEntry<bool> EnableSlopeSpeed { get; set; }

        public static ConfigEntry<bool> CanDisarmPlayer { get; set; }
        public static ConfigEntry<bool> CanDisarmBot { get; set; }
        public static ConfigEntry<float> DisarmBaseChance { get; set; }

        public static ConfigEntry<bool> CanFellPlayer { get; set; }
        public static ConfigEntry<bool> CanFellBot { get; set; }
        public static ConfigEntry<float> FallBaseChance { get; set; }

        public static ConfigEntry<float> test1 { get; set; }
        public static ConfigEntry<float> test2 { get; set; }
        public static ConfigEntry<float> test3 { get; set; }
        public static ConfigEntry<float> test4 { get; set; }

        public static ConfigEntry<KeyboardShortcut> MountKeybind { get; set; }

        public static Weapon CurrentlyShootingWeapon;

        public static Vector3 TransformBaseStartPosition;
        public static Vector3 WeaponOffsetPosition;

        public static bool DidWeaponSwap = false;
        public static bool IsInInventory = false;

        public static bool IsFiring = false;

        public static bool FreeAimEnabled = false;
        public static bool IsInDeadZone = false;
        public static bool DeadZoneReduceSens = false;
        public static float DeadZoneSensAmount = 0.5f;
        public static bool DeadZonePanCamLeft = false;
        public static bool DeadZonePanCamRight = false;
        public static bool DeadZonePanCamUp = false;
        public static bool DeadZonePanCamDown = false;
        public static float CamRotationMulti = 1f;


        public static bool IsBotFiring = false;
        public static bool GrenadeExploded = false;
        public static bool IsAiming = false;
        public static bool IsBlindFiring = false;
        public static bool IsFiringMovement = false;
        public static float ShotTimer = 0.0f;
        public static float MovementSpeedShotTimer = 0.0f;
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

        public static bool UniformAimIsPresent = false;
        public static bool FovFixIsPresent = false;
        private static bool checkedForSensMods = false;

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
        public static PrismEffects PrismEffects;

        public static bool HasOptic = false;

        public static float healthControllerTick = 0f;

        public static bool IsInThirdPerson = false;

        public static Player.BetterValueBlender StanceBlender = new Player.BetterValueBlender
        {
            Speed = 5f,
            Target = 0f
        };

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
            IconCache.Add(ENewItemAttributeId.NoiseReduction, Resources.Load<Sprite>("characteristics/icons/icon_info_loudness"));
            IconCache.Add(ENewItemAttributeId.ProjectileCount, Resources.Load<Sprite>("characteristics/icons/icon_info_bulletspeed"));
            IconCache.Add(ENewItemAttributeId.Convergence, Resources.Load<Sprite>("characteristics/icons/Ergonomics"));
            IconCache.Add(ENewItemAttributeId.HBleedType, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(ENewItemAttributeId.LimbHpPerTick, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(ENewItemAttributeId.HpPerTick, Resources.Load<Sprite>("characteristics/icons/hpResource"));
            IconCache.Add(ENewItemAttributeId.RemoveTrnqt, Resources.Load<Sprite>("characteristics/icons/hpResource"));
            IconCache.Add(ENewItemAttributeId.Comfort, Resources.Load<Sprite>("characteristics/icons/Weight"));
            IconCache.Add(ENewItemAttributeId.PainKillerStrength, Resources.Load<Sprite>("characteristics/icons/hpResource"));

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
                    Logger.LogError("Realism: Error Requesting Textures");
                }
                else
                {
                    Texture2D cachedTexture = DownloadHandlerTexture.GetContent(uwr);
                    IconCache.Add(id, Sprite.Create(cachedTexture, new Rect(0, 0, cachedTexture.width, cachedTexture.height), new Vector2(0, 0)));
                }
            }
        }

        public static Dictionary<string, AudioClip> LoadedAudioClips = new Dictionary<string, AudioClip>();

        async static void LoadAudioClip(string path)
        {
            LoadedAudioClips[Path.GetFileName(path)] = await RequestAudioClip(path);
        }

        async static Task<AudioClip> RequestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            AudioType audioType = AudioType.WAV;
            switch (extension) 
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
            }
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            var SendWeb = www.SendWebRequest();

            while (!SendWeb.isDone)
                await Task.Yield();

            if (www.isNetworkError || www.isHttpError)
            {
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(www);
                return audioclip;
            }
        }

        void Awake()
        {
            try
            {
                GetPaths();
                ConfigCheck();
                CacheIcons();

                string[] AudioFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/Realism/sounds/");
                foreach (string File in AudioFiles)
                {
                    LoadAudioClip(File);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }

            InitConfigs();

            if (ModConfig.recoil_attachment_overhaul)
            {
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

                //Sway and Aim Inertia
                new method_20Patch().Enable();
                new UpdateSwayFactorsPatch().Enable();
                new GetOverweightPatch().Enable();
                new SetOverweightPatch().Enable();

                //Recoil Patches
                new OnWeaponParametersChangedPatch().Enable();
                new ProcessPatch().Enable();
                new ShootPatch().Enable();
                new SetCurveParametersPatch().Enable();

                //Sensitivity Patches
                new AimingSensitivityPatch().Enable();
                new UpdateSensitivityPatch().Enable();
                new SensPatch().Enable();
                new GetRotationMultiplierPatch().Enable();

                //Aiming Patches
                new SetAimingPatch().Enable();
                new ToggleAimPatch().Enable();

                //Malf Patches
                if (Plugin.EnableMalfPatch.Value && ModConfig.malf_changes)
                {
                    new GetTotalMalfunctionChancePatch().Enable();
                }
                if (Plugin.InspectionlessMalfs.Value)
                {
                    new IsKnownMalfTypePatch().Enable();
                }

                //Reload Patches
                if (Plugin.EnableReloadPatches.Value)
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
                    new SetLauncherPatch().Enable();

                }

                new CheckAmmoFirearmControllerPatch().Enable();
                new CheckChamberFirearmControllerPatch().Enable();

                new SetAnimatorAndProceduralValuesPatch().Enable();
                new OnItemAddedOrRemovedPatch().Enable();

                if (Plugin.enableSGMastering.Value == true)
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
                new HeadsetConstructorPatch().Enable();
                new AmmoDuraBurnDisplayPatch().Enable();
                new AmmoMalfChanceDisplayPatch().Enable();
                new MagazineMalfChanceDisplayPatch().Enable();
                new BarrelModClassPatch().Enable();

                if (Plugin.IncreaseCOI.Value == true)
                {
                    new GetTotalCenterOfImpactPatch().Enable();
                }
            }


            //Ballistics
            if (ModConfig.realistic_ballistics)
            {
                new CreateShotPatch().Enable();
                new ApplyDamagePatch().Enable();
                new DamageInfoPatch().Enable();
                new ApplyDamageInfoPatch().Enable();
                new SetPenetrationStatusPatch().Enable();

                if (EnableRagdollFix.Value)
                {
                    new ApplyCorpseImpulsePatch().Enable();
                    /*  new RagdollPatch().Enable();*/
                }

                //Armor Class
                if (Plugin.EnableRealArmorClass.Value == true)
                {
                    new ArmorClassDisplayPatch().Enable();
                }

                if (Plugin.EnableArmorHitZones.Value)
                {
                    new ArmorZoneBaseDisplayPatch().Enable();
                    new ArmorZoneSringValueDisplayPatch().Enable();
                }

                new IsShotDeflectedByHeavyArmorPatch().Enable();

                if (Plugin.EnableArmPen.Value)
                {
                    new IsPenetratedPatch().Enable();
                }

                //Shot Effects
                if (Plugin.EnableDeafen.Value == true)
                {
                    new PrismEffectsPatch().Enable();
                    new VignettePatch().Enable();
                    new UpdatePhonesPatch().Enable();
                    new SetCompressorPatch().Enable();
                    new RegisterShotPatch().Enable();
                    new ExplosionPatch().Enable();
                    new GrenadeClassContusionPatch().Enable();
                }
            }

            new ArmorComponentPatch().Enable();
            new RigConstructorPatch().Enable();
            new BackpackConstructorPatch().Enable();

            //Player
            new PlayerInitPatch().Enable();
            new ToggleHoldingBreathPatch().Enable();

            //Movement
            if (EnableMaterialSpeed.Value) 
            {
                new CalculateSurfacePatch().Enable();
            }
            if (EnableMaterialSpeed.Value)
            {
                new CalculateSurfacePatch().Enable();
                new ClampSpeedPatch().Enable();
            }
            new SprintAccelerationPatch().Enable();
            new EnduranceSprintActionPatch().Enable();
            new EnduranceMovementActionPatch().Enable();

            //LateUpdate
            new PlayerLateUpdatePatch().Enable();

            //Stances
            new ApplyComplexRotationPatch().Enable();
            new ApplySimpleRotationPatch().Enable();
            new InitTransformsPatch().Enable();
            new ZeroAdjustmentsPatch().Enable();
            new WeaponOverlappingPatch().Enable();
            new WeaponLengthPatch().Enable();
            new OnWeaponDrawPatch().Enable();
            new UpdateHipInaccuracyPatch().Enable();
            new SetFireModePatch().Enable();
            new WeaponOverlapViewPatch().Enable();
            new CollisionPatch().Enable();

            new RotatePatch().Enable();
            new SetTiltPatch().Enable();

            //Health
            if (EnableMedicalOvehaul.Value && ModConfig.med_changes)
            {
                new ApplyItemPatch().Enable();
                new SetQuickSlotPatch().Enable();
                new ProceedPatch().Enable();
                new RemoveEffectPatch().Enable();
                new StamRegenRatePatch().Enable();
                new MedkitConstructorPatch().Enable();
                new HCApplyDamagePatch().Enable();
                new RestoreBodyPartPatch().Enable();
                new FlyingBulletPatch().Enable();   
            }
        }

        void Update()
        {
            if (!checkedForSensMods)
            {
                UniformAimIsPresent = Chainloader.PluginInfos.ContainsKey("com.notGreg.UniformAim") && Chainloader.PluginInfos.ContainsKey("com.notGreg.RealismModBridge");
                FovFixIsPresent = Chainloader.PluginInfos.ContainsKey("FOVFix") && Chainloader.PluginInfos.ContainsKey("Bridge");
                checkedForSensMods = true;
                Logger.LogWarning("FovFixIsPresent = " + FovFixIsPresent);
            }

            if (Utils.CheckIsReady())
            {
                if (ModConfig.recoil_attachment_overhaul) 
                {
                    if (Plugin.ShotCount > Plugin.PrevShotCount)
                    {
                        Plugin.IsFiring = true;
                        StanceController.IsFiringFromStance = true;
                        Plugin.IsFiringMovement = true; 

                        if ((Plugin.EnableRecoilClimb.Value && Plugin.IsAiming) || (Plugin.EnableHipfireRecoilClimb.Value == true && !Plugin.IsAiming))
                        {
                            RecoilController.DoRecoilClimb();
                        }

                        Plugin.PrevShotCount = Plugin.ShotCount;
                    }

                    if (Plugin.ShotCount == Plugin.PrevShotCount)
                    {
                        Plugin.ShotTimer += Time.deltaTime;
                        Plugin.MovementSpeedShotTimer += Time.deltaTime;

                        if (Plugin.ShotTimer >= Plugin.resetTime.Value)
                        {
                            Plugin.IsFiring = false;
                            Plugin.ShotCount = 0;
                            Plugin.PrevShotCount = 0;
                            Plugin.ShotTimer = 0f;
                        }

                        if (Plugin.MovementSpeedShotTimer >= 0.5f)
                        {
                            Plugin.IsFiringMovement = false;
                            Plugin.MovementSpeedShotTimer = 0f;
                        }

                        StanceController.StanceShotTimer();
                    }

                    if (Plugin.EnableDeafen.Value)
                    {
                        if (Input.GetKeyDown(Plugin.IncGain.Value.MainKey) && Plugin.HasHeadSet)
                        {
                            if (Plugin.RealTimeGain.Value < 20)
                            {
                                Plugin.RealTimeGain.Value += 1f;
                                Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0,0,0), Plugin.LoadedAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                            }
                        }
                        if (Input.GetKeyDown(Plugin.DecGain.Value.MainKey) && Plugin.HasHeadSet)
                        {
                
                            if (Plugin.RealTimeGain.Value > 0)
                            {
                                Plugin.RealTimeGain.Value -= 1f;
                                Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.LoadedAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                            }
                        }

                        if (PrismEffects != null) 
                        {
                            Deafening.DoDeafening();
                        }
            

                        if (Plugin.IsBotFiring)
                        {
                            Plugin.BotTimer += Time.deltaTime;
                            if (Plugin.BotTimer >= 0.5f)
                            {
                                Plugin.IsBotFiring = false;
                                Plugin.BotTimer = 0f;
                            }
                        }

                        if (Plugin.GrenadeExploded)
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
                        RecoilController.ResetRecoil();
                    }
                }
 
                StanceController.StanceState();

                if (Plugin.EnableMedicalOvehaul.Value && ModConfig.med_changes)
                {
                    healthControllerTick += Time.deltaTime;
                    RealismHealthController.HealthController(Logger);
                }
            }
        }

        public void InitConfigs()
        {
            string testing = ".0. Testing";
            string miscSettings = ".1. Misc. Settings";
            string ballSettings = ".2. Ballistics Settings";
            string recoilSettings = ".3. Recoil Settings";
            string advancedRecoilSettings = ".4. Advanced Recoil Settings";
            string statSettings = ".5. Stat Display Settings";
            /*                string AmmoSettings = "4. Ammo Stat Display Settings";*/
            string waponSettings = ".6. Weapon Settings";
            string healthSettings = ".7. Health and Meds Settings";
            string moveSettings = ".8. Movement Settings";
            string deafSettings = ".9. Deafening and Audio";
            string speed = "10. Weapon Speed Modifiers";
            string weapAimAndPos = "11. Weapon Stances And Position";
            string activeAim = "12. Active Aim";
            string highReady = "13. High Ready";
            string lowReady = "14. Low Ready";
            string pistol = "15. Pistol Position And Stance";
            string shortStock = "16. Short-Stocking";


            EnableMaterialSpeed = Config.Bind<bool>(moveSettings, "Enable Ground Material Speed Modifier", true, new ConfigDescription("Enables Movement Speed Being Affected By Ground Material (Concrete, Grass, Metal, Glass Etc.)", null, new ConfigurationManagerAttributes { Order = 20 }));
            EnableSlopeSpeed = Config.Bind<bool>(moveSettings, "Enable Ground Slope Speed Modifier", false, new ConfigDescription("Enables Slopes Slowing Down Movement. Can Cause Random Speed Slowdowns In Some Small Spots Due To BSG's Bad Map Geometry.", null, new ConfigurationManagerAttributes { Order = 10 }));

            EnableMedicalOvehaul = Config.Bind<bool>(healthSettings, "Enable Health & Medical Overhaul", true, new ConfigDescription("Enables The Overhaul Of The Health & Medical System. All Other Related Options Require This To Be Enabled.", null, new ConfigurationManagerAttributes { Order = 100 }));
            EnableTrnqtEffect = Config.Bind<bool>(healthSettings, "Enable Tourniquet Effect", true, new ConfigDescription("Tourniquet Will Drain HP Of The Limb They Are Applied To.", null, new ConfigurationManagerAttributes { Order = 90 }));
            GearBlocksEat = Config.Bind<bool>(healthSettings, "Gear Blocks Consumption", true, new ConfigDescription("Gear Blocks Eating & Drinking. This Includes Some Masks & NVGs & Faceshields That Are Toggled On.", null, new ConfigurationManagerAttributes { Order = 80 }));
            GearBlocksHeal = Config.Bind<bool>(healthSettings, "Gear Blocks Healing", true, new ConfigDescription("Gear Blocks Use Of Meds If The Wound Is Covered By It.", null, new ConfigurationManagerAttributes { Order = 70 }));
            EnableAdrenaline = Config.Bind<bool>(healthSettings, "Adrenaline", true, new ConfigDescription("If The Player Is Shot or Shot At They Will Get A Painkiller Effect, As Well As Tunnel Vision and Tremors. The Duration And Strength Of These Effects Are Determined By The Stress Resistence Skill.", null, new ConfigurationManagerAttributes { Order = 55 }));
            DropGearKeybind = Config.Bind(healthSettings, "Remove Gear Keybind (Double Press)", new KeyboardShortcut(KeyCode.P), new ConfigDescription("Removes Any Gear That Is Blocking The Healing Of A Wound, It's A Double Press Like Bag Keybind Is.", null, new ConfigurationManagerAttributes { Order = 50 }));

            AddEffectType = Config.Bind<string>(testing, "Effect Type", "HeavyBleeding", new ConfigDescription("HeavyBleeding, LightBleeding, Fracture.", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));
            AddEffectBodyPart = Config.Bind<int>(testing, "Body Part Index", 1, new ConfigDescription("Head = 0, Chest = 1, Stomach = 2, Letft Arm, Right Arm, Left Leg, Right Leg, Common (whole body)", null, new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true }));
            AddEffectKeybind = Config.Bind(testing, "Add Effect Keybind", new KeyboardShortcut(KeyCode.JoystickButton6), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true }));
            EnableBallisticsLogging = Config.Bind<bool>(testing, "Enable Ballistics Logging", false, new ConfigDescription("Enables Logging For Debug And Dev", null, new ConfigurationManagerAttributes { Order = 2, IsAdvanced = true }));
            EnableLogging = Config.Bind<bool>(testing, "Enable Logging", false, new ConfigDescription("Enables Logging For Debug And Dev", null, new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true }));
            test1 = Config.Bind<float>(testing, "test 1", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 600, IsAdvanced = true }));
            test2 = Config.Bind<float>(testing, "test 2", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 500, IsAdvanced = true }));
            test3 = Config.Bind<float>(testing, "test 3", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 400, IsAdvanced = true }));
            test4 = Config.Bind<float>(testing, "test 4", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 300, IsAdvanced = true }));


            EnableStockSlots = Config.Bind<bool>(miscSettings, "Enable Stock Slot Stat Modifiers", true, new ConfigDescription("Requires Restart. For Buffer Tubes That Have Multiple Stock Slots, Each Slot Will Modify The Ergo And Recoil Stats Of The Attached Stock.", null, new ConfigurationManagerAttributes { Order = 3 }));
            EnableFSPatch = Config.Bind<bool>(miscSettings, "Enable Faceshield Patch", true, new ConfigDescription("Faceshields Block ADS Unless The Specfic Stock/Weapon/Faceshield Allows It.", null, new ConfigurationManagerAttributes { Order = 4 }));
            EnableNVGPatch = Config.Bind<bool>(miscSettings, "Enable NVG ADS Patch", true, new ConfigDescription("Magnified Optics Block ADS When Using NVGs.", null, new ConfigurationManagerAttributes { Order = 5 }));
            EnableHoldBreath = Config.Bind<bool>(miscSettings, "Enable Hold Breath", false, new ConfigDescription("Re-Enabled Hold Breath. This Mod Is Balanced Around Not Being Able To Hold Breath.", null, new ConfigurationManagerAttributes { Order = 10 }));

            EnableArmorHitZones = Config.Bind<bool>(ballSettings, "Enable Armor Hit Zones", true, new ConfigDescription("Armor Protection Is Limited To Wear Plates Would Be, Adds Neck And Side Armor Zones. Arm And Stomach Armor Has Limited Protection.", null, new ConfigurationManagerAttributes { Order = 1 }));
            EnableBodyHitZones = Config.Bind<bool>(ballSettings, "Enable Body Hit Zones", true, new ConfigDescription("Divides Body Into A, C and D Hit Zones Like On IPSC Targets. In Addtion, There Are Upper Arm, Forearm, Thigh, Calf, Neck, Spine And Heart Hit Zones. Each Zone Modifies Damage And Bleed Chance. ", null, new ConfigurationManagerAttributes { Order = 10 }));
            EnablePlayerArmorZones = Config.Bind<bool>(ballSettings, "Enable Armor Hit Zones For Player.", true, new ConfigDescription("Enables Player To Use New Hit Zones.", null, new ConfigurationManagerAttributes { Order = 20 }));
            EnableArmPen = Config.Bind<bool>(ballSettings, "Enable Increased Arm Penetration", true, new ConfigDescription("Arm 'Armor' Is Reduced to Lvl 1, And Reduces Pen Of Bullets That Pass Through Them By A Lot Less. Arms Soak Up A Lot Less Damage Therefore Damage To Chest Is Increased.", null, new ConfigurationManagerAttributes { Order = 40 }));
            EnableHitSounds = Config.Bind<bool>(ballSettings, "Enable Hit Sounds", true, new ConfigDescription("Enables Additional Sounds To Be Played When Hitting The New Body Zones And Armor Hit Sounds By Material.", null, new ConfigurationManagerAttributes { Order = 50 }));
            FleshHitSoundMulti = Config.Bind<float>(ballSettings, "FleshHit Sound Multi.", 0.45f, new ConfigDescription("Raises/Lowers New Hit Sounds Volume.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 60 }));
            ArmorCloseHitSoundMulti = Config.Bind<float>(ballSettings, "Distant Armor Hit Sound Multi", 1.1f, new ConfigDescription("Raises/Lowers New Hit Sounds Volume.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 70 }));
            ArmorFarHitSoundMulti = Config.Bind<float>(ballSettings, "Close Armor Hit Sound Mutli", 1.2f, new ConfigDescription("Raises/Lowers New Hit Sounds Volume.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 80 }));
            EnableRealArmorClass = Config.Bind<bool>(ballSettings, "Show Real Armor Class", true, new ConfigDescription("Requiures Restart. Instead Of Showing The Armor's Class As A Number, Use The Real Armor Classification Instead.", null, new ConfigurationManagerAttributes { Order = 90 }));
            EnableRagdollFix = Config.Bind<bool>(ballSettings, "Enable Ragdoll Fix (Experimental)", true, new ConfigDescription("Requiures Restart. Enables Fix For Ragdolls Flying Into The Stratosphere.", null, new ConfigurationManagerAttributes { Order = 100 }));
            RagdollForceModifier = Config.Bind<float>(ballSettings, "Ragdoll Force Modifier", 1f, new ConfigDescription("Requires Ragdoll Fix To Be Enabled.", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 110 }));
            CanDisarmBot = Config.Bind<bool>(ballSettings, "Can Disarm Bot", true, new ConfigDescription("If Hit In The Arms, There Is A Chance That The Currently Equipped Weapon Will Be Dropped. Chance Is Modified By Bullet Kinetic Energy And Reduced If Hit Arm Armor, And Doubled If Forearm Is Hit.", null, new ConfigurationManagerAttributes { Order = 120 }));
            CanDisarmPlayer = Config.Bind<bool>(ballSettings, "Can Disarm Player", false, new ConfigDescription("If Hit In The Arms, There Is A Chance That The Currently Equipped Weapon Will Be Dropped. Chance Is Modified By Bullet Kinetic Energy And Reduced If Hit Arm Armor, And Doubled If Forearm Is Hit.", null, new ConfigurationManagerAttributes { Order = 130 }));
            DisarmBaseChance = Config.Bind<float>(ballSettings, "Disarm Base Chance.", 1f, new ConfigDescription("The Base Chance To Be Disarmed. 1 = 1% Chance. This Value Is Increased By The Bullet's Kinetic Energy, Reduced By Armor Armor If Hit, And Doubled If Forearm Is Hit.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, IsAdvanced = true, Order = 140 }));
            CanFellBot = Config.Bind<bool>(ballSettings, "Bot Can Fall", true, new ConfigDescription("If Hit In The Leg And The Leg Has/Will Have 0 HP, There Is A Chance That Prone Will Be Toggled. Chance Is Modified By Bullet Kinetic EnergyAnd Doubled If Calf Is Hit.", null, new ConfigurationManagerAttributes { Order = 150 }));
            CanFellPlayer = Config.Bind<bool>(ballSettings, "Player Can Fall", false, new ConfigDescription("If Hit In The Leg And The Leg Has/Will Have 0 HP, There Is A Chance That Prone Will Be Toggled. Chance Is Modified By Bullet Kinetic Energy And Doubled If Calf Is Hit.", null, new ConfigurationManagerAttributes { Order = 160 }));
            FallBaseChance = Config.Bind<float>(ballSettings, "Fall Base Chance", 10f, new ConfigDescription("The Base Chance To Toggle Prone If Shot In Leg. 1 = 1% Chance. This Value Is Increased By The Bullet's Kinetic Energy And Doubled If Calf Is Hit.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, IsAdvanced = true, Order = 170 }));

            EnableHipfireRecoilClimb = Config.Bind<bool>(recoilSettings, "Enable Hipfire Recoil Climb", true, new ConfigDescription("Requires Restart. Enabled Recoil Climbing While Hipfiring", null, new ConfigurationManagerAttributes { Order = 80 }));
            ReduceCamRecoil = Config.Bind<bool>(recoilSettings, "Reduce Camera Recoil", false, new ConfigDescription("Reduces Camera Recoil Per Shot. If Disabled, Camera Recoil Becomes More Intense As Weapon Recoil Increases.", null, new ConfigurationManagerAttributes { Order = 70 }));
            SensLimit = Config.Bind<float>(recoilSettings, "Sensitivity Lower Limit", 0.4f, new ConfigDescription("Sensitivity Lower Limit While Firing. Lower Means More Sensitivity Reduction. 100% Means No Sensitivity Reduction.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes { Order = 60 }));
            RecoilIntensity = Config.Bind<float>(recoilSettings, "Recoil Multi", 1.2f, new ConfigDescription("Changes The Overall Intenisty Of Recoil. This Will Increase/Decrease Horizontal Recoil, Dispersion, Vertical Recoil.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 50 }));
            VertRecAutoMulti = Config.Bind<float>(recoilSettings, "Vertical Recoil Auto Multi", 0.63f, new ConfigDescription("Only Applies To Assault Rifles, Carbines And DMRs.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 40 }));
            VertRecSemiMulti = Config.Bind<float>(recoilSettings, "Vertical Recoil Semi Multi", 1.35f, new ConfigDescription("Only Applies To Assault Rifles, Carbines And DMRs.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 30 }));
            HorzRecAutoMulti = Config.Bind<float>(recoilSettings, "Horizontal Recoil Auto Multi", 0.4f, new ConfigDescription("Only Applies To Assault Rifles, Carbines And DMRs..", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 20 }));
            HorzRecSemiMulti = Config.Bind<float>(recoilSettings, "Horizontal Recoil Semi Multi", 1.15f, new ConfigDescription("Only Applies To Assault Rifles, Carbines And DMRs.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 10 }));
            HorzRecLimit = Config.Bind<float>(recoilSettings, "Horizontal Recoil Limit", 15f, new ConfigDescription("Horizontal Recoil Limit For All Guns. A Value Of 15 Roughly Equals 150 Horizontal Recoil.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = 1 }));

            ConvSemiMulti = Config.Bind<float>(advancedRecoilSettings, "Semi Auto Convergence Multi", 0.69f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30, IsAdvanced = true }));
            ConvAutoMulti = Config.Bind<float>(advancedRecoilSettings, "Auto Convergence Multi", 0.59f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20, IsAdvanced = true }));
            EnableRecoilClimb = Config.Bind<bool>(advancedRecoilSettings, "Enable Recoil Climb", true, new ConfigDescription("The Core Of The Recoil Overhaul. Recoil Increase Per Shot, Nullifying Recoil Auto-Compensation In Full Auto And Requiring A Constant Pull Of The Mouse To Control Recoil. If Diabled Weapons Will Be Completely Unbalanced Without Stat Changes.", null, new ConfigurationManagerAttributes { Order = 13 }));
            SensChangeRate = Config.Bind<float>(advancedRecoilSettings, "Sensitivity Change Rate", 0.75f, new ConfigDescription("Rate At Which Sensitivity Is Reduced While Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 1f), new ConfigurationManagerAttributes { Order = 12 }));
            SensResetRate = Config.Bind<float>(advancedRecoilSettings, "Senisitivity Reset Rate", 1.2f, new ConfigDescription("Rate At Which Sensitivity Recovers After Firing. Higher Means Faster Rate.", new AcceptableValueRange<float>(1.01f, 2f), new ConfigurationManagerAttributes { Order = 11, IsAdvanced = true }));
            ConvergenceSpeedCurve = Config.Bind<float>(advancedRecoilSettings, "Convergence Multi", 0.9f, new ConfigDescription("The Convergence Curve. Lower Means More Recoil/Less Convergence.", new AcceptableValueRange<float>(0.01f, 1.5f), new ConfigurationManagerAttributes { Order = 10 }));
            vRecoilLimit = Config.Bind<float>(advancedRecoilSettings, "Vertical Recoil Upper Limit", 15.0f, new ConfigDescription("The Upper Limit For Vertical Recoil Increase As A Multiplier. E.g Value Of 10 Is A Limit Of 10x Starting Recoil.", new AcceptableValueRange<float>(1f, 50f), new ConfigurationManagerAttributes { Order = 9 }));
            vRecoilChangeMulti = Config.Bind<float>(advancedRecoilSettings, "Vertical Recoil Change Rate Multi", 1.01f, new ConfigDescription("A Multiplier For The Verftical Recoil Increase Per Shot.", new AcceptableValueRange<float>(0.9f, 1.1f), new ConfigurationManagerAttributes { Order = 8 }));
            vRecoilResetRate = Config.Bind<float>(advancedRecoilSettings, "Vertical Recoil Reset Rate", 0.91f, new ConfigDescription("The Rate At Which Vertical Recoil Resets Over Time After Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 0.99f), new ConfigurationManagerAttributes { Order = 7, IsAdvanced = true }));
            hRecoilLimit = Config.Bind<float>(advancedRecoilSettings, "Rearward Recoil Upper Limi", 1.25f, new ConfigDescription("The Upper Limit For Rearward Recoil Increase As A Multiplier. E.g Value Of 10 Is A Limit Of 10x Starting Recoil.", new AcceptableValueRange<float>(1f, 50f), new ConfigurationManagerAttributes { Order = 6 }));
            hRecoilChangeMulti = Config.Bind<float>(advancedRecoilSettings, "Rearward Recoil Change Rate Mult", 0.95f, new ConfigDescription("A Multiplier For The Rearward Recoil Increase Per Shot.", new AcceptableValueRange<float>(0.9f, 1.1f), new ConfigurationManagerAttributes { Order = 5 }));
            hRecoilResetRate = Config.Bind<float>(advancedRecoilSettings, "Rearward Recoil Reset Rate", 0.91f, new ConfigDescription("The Rate At Which Rearward Recoil Resets Over Time After Firing. Lower Means Faster Rate.", new AcceptableValueRange<float>(0.1f, 0.99f), new ConfigurationManagerAttributes { Order = 4, IsAdvanced = true }));
            ConvergenceResetRate = Config.Bind<float>(advancedRecoilSettings, "Convergence Reset Rate", 1.16f, new ConfigDescription("The Rate At Which Convergence Resets Over Time After Firing. Higher Means Faster Rate.", new AcceptableValueRange<float>(1.01f, 2f), new ConfigurationManagerAttributes { Order = 3, IsAdvanced = true }));
            convergenceLimit = Config.Bind<float>(advancedRecoilSettings, "Convergence Lower Limit", 0.3f, new ConfigDescription("The Lower Limit For Convergence. Convergence Is Kept In Proportion With Vertical Recoil While Firing, Down To The Set Limit. Value Of 0.3 Means Convegence Lower Limit Of 0.3 * Starting Convergance.", new AcceptableValueRange<float>(0.1f, 1.0f), new ConfigurationManagerAttributes { Order = 2, IsAdvanced = true }));
            resetTime = Config.Bind<float>(advancedRecoilSettings, "Time Before Reset", 0.15f, new ConfigDescription("The Time In Seconds That Has To Be Elapsed Before Firing Is Considered Over, Stats Will Not Reset Until This Timer Is Done. Helps Prevent Spam Fire In Full Auto.", new AcceptableValueRange<float>(0.1f, 0.55f), new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true }));

            EnableAmmoStats = Config.Bind<bool>(statSettings, "Display Ammo Stats", true, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 11 }));
            EnableStatsDelta = Config.Bind<bool>(statSettings, "Show Stats Delta Preview", false, new ConfigDescription("Requiures Restart. Shows A Preview Of The Difference To Stats Swapping/Removing Attachments Will Make. Warning: Will Degrade Performance Significantly When Moddig Weapons In Inspect Or Modding Screens.", null, new ConfigurationManagerAttributes { Order = 5 }));
            ShowBalance = Config.Bind<bool>(statSettings, "Show Balance Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 5 }));
            ShowCamRecoil = Config.Bind<bool>(statSettings, "Show Camera Recoil Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 4 }));
            ShowDispersion = Config.Bind<bool>(statSettings, "Show Dispersion Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 3 }));
            ShowRecoilAngle = Config.Bind<bool>(statSettings, "Show Recoil Angle Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use..", null, new ConfigurationManagerAttributes { Order = 2 }));
            ShowSemiROF = Config.Bind<bool>(statSettings, "Show Semi Auto ROF Stat", true, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 1 }));

            SwayIntensity = Config.Bind<float>(waponSettings, "Sway Intensity", 1.1f, new ConfigDescription("Changes The Intensity Of Aim Sway And Inertia.", new AcceptableValueRange<float>(0f, 3f), new ConfigurationManagerAttributes { Order = 1 }));
            EnableMalfPatch = Config.Bind<bool>(waponSettings, "Enable Malfunctions Changes", true, new ConfigDescription("Requires Restart. Malfunction Changes Must Be Enabled On The Server (Config App). Some Subsonic Ammo Needs Special Mods To Cycle, Malfunctions Can Happen At Any Durability But The Chance Is Significantly Reduced If Above The Durability Threshold.", null, new ConfigurationManagerAttributes { Order = 2 }));
            InspectionlessMalfs = Config.Bind<bool>(waponSettings, "Enable Inspectionless Malfunctions", true, new ConfigDescription("Requires Restart. You Don't Need To Inspect A Malfunction In Order To Clear It.", null, new ConfigurationManagerAttributes { Order = 3 }));
            DuraMalfThreshold = Config.Bind<float>(waponSettings, "Malfunction Durability Threshold", 98f, new ConfigDescription("Malfunction Changes Must Be Enabled On The Server (Config App) And 'Enable Malfunctions Changes' Must Be True. Malfunction Chance Is Significantly Reduced Until This Durability Threshold Is Exceeded.", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { Order = 4 }));
            enableSGMastering = Config.Bind<bool>(waponSettings, "Enable Increased Shotgun Mastery", true, new ConfigDescription("Requires Restart. Shotguns Will Get Set To Base Lvl 2 Mastery For Reload Animations, Giving Them Better Pump Animations. ADS while Reloading Is Unaffected.", null, new ConfigurationManagerAttributes { Order = 5 }));
            IncreaseCOI = Config.Bind<bool>(waponSettings, "Enable Increased Inaccuracy", true, new ConfigDescription("Requires Restart. Increases The Innacuracy Of All Weapons So That MOA/Accuracy Is A More Important Stat.", null, new ConfigurationManagerAttributes { Order = 6 }));

            HeadsetAmbientMulti = Config.Bind<float>(deafSettings, "Headset Ambient Multi", 10f, new ConfigDescription("Adjusts The Ambient Volume Reduction From Headsets. Headset Gain Also Affects Ambient Volume. Higher = Louder.", new AcceptableValueRange<float>(1f, 40f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 20 }));
            RealTimeGain = Config.Bind<float>(deafSettings, "Headset Gain", 13f, new ConfigDescription("WARNING: DO NOT SET THIS TOO HIGH, IT MAY DAMAGE YOUR HEARING! Most EFT Headsets Are Set To 13 By Default, Don't Make It Much Higher. Adjusts The Gain Of Equipped Headsets In Real Time, Acts Just Like The Volume Control On IRL Ear Defenders.", new AcceptableValueRange<float>(0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11 }));
            GainCutoff = Config.Bind<float>(deafSettings, "Headset Gain Cutoff Multi", 0.75f, new ConfigDescription("How Much Headset Gain Is Reduced While Firing. 0.75 = 25% Reduction.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10 }));
            DecGain = Config.Bind(deafSettings, "Reduce Gain Keybind", new KeyboardShortcut(KeyCode.KeypadMinus), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 9 }));
            IncGain = Config.Bind(deafSettings, "Increase Gain Keybind", new KeyboardShortcut(KeyCode.KeypadPlus), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 8 }));
            DeafRate = Config.Bind<float>(deafSettings, "Deafen Rate", 0.023f, new ConfigDescription("How Quickly Player Gets Deafened. Higher = Faster.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7, IsAdvanced = true }));
            DeafReset = Config.Bind<float>(deafSettings, "Deafen Reset Rate", 0.033f, new ConfigDescription("How Quickly Player Regains Hearing. Higher = Faster.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true }));
            VigRate = Config.Bind<float>(deafSettings, "Tunnel Effect Rate", 0.02f, new ConfigDescription("How Quickly Player Gets Tunnel Vission. Higher = Faster", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true }));
            VigReset = Config.Bind<float>(deafSettings, "Tunnel Effect Reset Rate", 0.02f, new ConfigDescription("How Quickly Player Recovers From Tunnel Vision. Higher = Faster", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true }));
            DistRate = Config.Bind<float>(deafSettings, "Distortion Rate", 0.16f, new ConfigDescription("How Quickly Player's Hearing Gets Distorted. Higher = Faster", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true }));
            DistReset = Config.Bind<float>(deafSettings, "Distortion Reset Rate", 0.25f, new ConfigDescription("How Quickly Player's Hearing Recovers From Distortion. Higher = Faster", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true }));
            EnableDeafen = Config.Bind<bool>(deafSettings, "Enable Deafening", true, new ConfigDescription("Requiures Restart. Enables Gunshots And Explosions Deafening The Player.", null, new ConfigurationManagerAttributes { Order = 1 }));

            EnableReloadPatches = Config.Bind<bool>(speed, "Enable Reload And Chamber Speed Changes", true, new ConfigDescription("Requires Restart. Weapon Weight, Magazine Weight, Attachment Reload And Chamber Speed Stat, Balance, Ergo And Arm Injury Affect Reload And Chamber Speed.", null, new ConfigurationManagerAttributes { Order = 17 }));
            GlobalAimSpeedModifier = Config.Bind<float>(speed, "Aim Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 16 }));
            GlobalReloadSpeedMulti = Config.Bind<float>(speed, "Magazine Reload Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 15 }));
            GlobalFixSpeedMulti = Config.Bind<float>(speed, "Malfunction Fix Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 14 }));
            GlobalUBGLReloadMulti = Config.Bind<float>(speed, "UBGL Reload Speed Multi", 1.35f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 13, IsAdvanced = true }));
            RechamberPistolSpeedMulti = Config.Bind<float>(speed, "Pistol Rechamber Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true }));
            GlobalRechamberSpeedMulti = Config.Bind<float>(speed, "Rechamber Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11 }));
            GlobalBoltSpeedMulti = Config.Bind<float>(speed, "Bolt Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10 }));
            GlobalShotgunRackSpeedFactor = Config.Bind<float>(speed, "Shotgun Rack Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 9 }));
            GlobalCheckChamberSpeedMulti = Config.Bind<float>(speed, "Chamber Check Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 8 }));
            GlobalCheckChamberShotgunSpeedMulti = Config.Bind<float>(speed, "Shotgun Chamber Check Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7, IsAdvanced = true }));
            GlobalCheckChamberPistolSpeedMulti = Config.Bind<float>(speed, "Pistol Chamber Check Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true }));
            GlobalCheckAmmoPistolSpeedMulti = Config.Bind<float>(speed, "Chamber Check Ammo Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true }));
            GlobalCheckAmmoMulti = Config.Bind<float>(speed, "Check Ammo Multi", 1.1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 5.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4 }));
            GlobalArmHammerSpeedMulti = Config.Bind<float>(speed, "Arm Hammer, Bolt Release, Slide Release Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true }));
            QuickReloadSpeedMulti = Config.Bind<float>(speed, "Quick Reload Multi", 1.4f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2 }));
            InternalMagReloadMulti = Config.Bind<float>(speed, "Internal Magazine Reload", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 2.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1 }));

            EnableSprintPenalty = Config.Bind<bool>(weapAimAndPos, "Enable Sprint Aim Penalties", true, new ConfigDescription("ADS Out Of Sprint Has A Short Delay, Reduced Aim Speed And Increased Sway. The Longer You Sprint The Bigger The Penalty.", null, new ConfigurationManagerAttributes { Order = 240 }));
            EnableTacSprint = Config.Bind<bool>(weapAimAndPos, "Enable High Ready Sprint Animation", false, new ConfigDescription("Enables Usage Of High Ready Sprint Animation When Sprinting From High Ready Position.", null, new ConfigurationManagerAttributes { Order = 230 }));
            EnableAltPistol = Config.Bind<bool>(weapAimAndPos, "Enable Alternative Pistol Position And ADS", true, new ConfigDescription("Pistol Will Be Held Centered And In A Compressed Stance. ADS Will Be Animated.", null, new ConfigurationManagerAttributes { Order = 229 }));
            EnableIdleStamDrain = Config.Bind<bool>(weapAimAndPos, "Enable Idle Arm Stamina Drain", false, new ConfigDescription("Arm Stamina Will Drain When Not In A Stance (High And Low Ready, Short-Stocking).", null, new ConfigurationManagerAttributes { Order = 210 }));
            EnableStanceStamChanges = Config.Bind<bool>(weapAimAndPos, "Enable Stance Stamina And Movement Effects", true, new ConfigDescription("Enabled Stances To Affect Stamina And Movement Speed. High + Low Ready, Short-Stocking And Pistol Idle Will Regenerate Stamina Faster And Optionally Idle With Rifles Drains Stamina. High Ready Has Faster Sprint Speed And Sprint Accel, Low Ready Has Faster Sprint Accel. Arm Stamina Won't Drain Regular Stamina If It Reaches 0.", null, new ConfigurationManagerAttributes { Order = 183 }));
            ToggleActiveAim = Config.Bind<bool>(weapAimAndPos, "Use Toggle For Active Aim", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 200 }));
            ActiveAimReload = Config.Bind<bool>(weapAimAndPos, "Allow Reload From Active Aim", false, new ConfigDescription("Allows Reload From Magazine While In Active Aim With Speed Bonus.", null, new ConfigurationManagerAttributes { Order = 190 }));
            StanceToggleDevice = Config.Bind<bool>(weapAimAndPos, "Stance Toggles Off Light/Laser", true, new ConfigDescription("Entering High/Low Ready Will Toggle Off Lights/Lasers.", null, new ConfigurationManagerAttributes { Order = 180 }));

            CycleStancesKeybind = Config.Bind(weapAimAndPos, "Cycle Stances Keybind", new KeyboardShortcut(KeyCode.J), new ConfigDescription("Cycles Between High, Low Ready and Short-Stocking. Double Click Returns To Idle.", null, new ConfigurationManagerAttributes { Order = 174 }));
            ActiveAimKeybind = Config.Bind(weapAimAndPos, "Active Aim Keybind", new KeyboardShortcut(KeyCode.LeftArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 173 }));
            HighReadyKeybind = Config.Bind(weapAimAndPos, "High Ready Keybind", new KeyboardShortcut(KeyCode.UpArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 172 }));
            LowReadyKeybind = Config.Bind(weapAimAndPos, "Low Ready Keybind", new KeyboardShortcut(KeyCode.DownArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 171 }));
            ShortStockKeybind = Config.Bind(weapAimAndPos, "Short-Stock Keybind", new KeyboardShortcut(KeyCode.RightArrow), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 170 }));
            MountKeybind = Config.Bind(weapAimAndPos, "Mounting Keybind", new KeyboardShortcut(KeyCode.KeypadMultiply), new ConfigDescription("Snaps To Cover, Toggle Only.", null, new ConfigurationManagerAttributes { Order = 160 }));

            WeapOffsetX = Config.Bind<float>(weapAimAndPos, "Weapon Position X-Axis", 0.0f, new ConfigDescription("Adjusts The Starting Position Of Weapon On Screen, Except Pistols.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 152 }));
            WeapOffsetY = Config.Bind<float>(weapAimAndPos, "Weapon Position Y-Axis", 0.0f, new ConfigDescription("Adjusts The Starting Position Of Weapon On Screen, Except Pistols.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 151 }));
            WeapOffsetZ = Config.Bind<float>(weapAimAndPos, "Weapon Position Z-Axis", 0.0f, new ConfigDescription("Adjusts The Starting Position Of Weapon On Screen, Except Pistols.", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 150 }));

            StanceTransitionSpeed = Config.Bind<float>(weapAimAndPos, "Stance Transition Speed", 5.0f, new ConfigDescription("Adjusts The Position Change Speed Between Stances.", new AcceptableValueRange<float>(1f, 60f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true }));
            ThirdPersonRotationSpeed = Config.Bind<float>(weapAimAndPos, "Third Person Postion Speed Multi", 1.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 144, IsAdvanced = true }));
            ThirdPersonPositionSpeed = Config.Bind<float>(weapAimAndPos, "Third Person Transition Speed Multi", 2.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 143, IsAdvanced = true }));

            ActiveAimAdditionalRotationSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Additonal Rotation Speed Multi.", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true }));
            ActiveAimResetRotationSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Reset Rotation Speed Multi.", 3.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true }));
            ActiveAimRotationMulti = Config.Bind<float>(activeAim, "Active Aim Rotation Speed Multi.", 2.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 144, IsAdvanced = true }));
            ActiveAimSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Speed Multi.", 12.0f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 143, IsAdvanced = true }));
            ActiveAimResetSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Reset Speed Multi.", 10f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 142, IsAdvanced = true }));

            ActiveAimOffsetX = Config.Bind<float>(activeAim, "Active Aim Position X-Axis", -0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 135, IsAdvanced = true }));
            ActiveAimOffsetY = Config.Bind<float>(activeAim, "Active Aim Position Y-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 134, IsAdvanced = true }));
            ActiveAimOffsetZ = Config.Bind<float>(activeAim, "Active Aim Position Z-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 133, IsAdvanced = true }));

            ActiveAimRotationX = Config.Bind<float>(activeAim, "Active Aim Rotation X-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 122, IsAdvanced = true }));
            ActiveAimRotationY = Config.Bind<float>(activeAim, "Active Aim Rotation Y-Axis", -30.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 121, IsAdvanced = true }));
            ActiveAimRotationZ = Config.Bind<float>(activeAim, "Active Aim Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 120, IsAdvanced = true }));
            
            ActiveAimAdditionalRotationX = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation X-Axis", -1.5f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 111, IsAdvanced = true }));
            ActiveAimAdditionalRotationY = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation Y-Axis", -70f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true }));
            ActiveAimAdditionalRotationZ = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation Z-Axis", 2f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true }));

            ActiveAimResetRotationX = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation X-Axis", 5.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 102, IsAdvanced = true }));
            ActiveAimResetRotationY = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation Y-Axis.", 55.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 101, IsAdvanced = true }));
            ActiveAimResetRotationZ = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation Z-Axis", -3.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 100, IsAdvanced = true }));

            HighReadyAdditionalRotationSpeedMulti = Config.Bind<float>(highReady, "High Ready Additonal Rotation Speed Multi.", 2f, new ConfigDescription("How Fast The Weapon Rotates Going Out Of Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 94, IsAdvanced = true }));
            HighReadyResetRotationMulti = Config.Bind<float>(highReady, "High Ready Reset Rotation Speed Multi", 3.5f, new ConfigDescription("How Fast The Weapon Rotates Going Out Of Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 93, IsAdvanced = true }));
            HighReadyRotationMulti = Config.Bind<float>(highReady, "High Ready Rotation Speed Multi", 1.8f, new ConfigDescription("How Fast The Weapon Rotates Going Into Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 92, IsAdvanced = true }));
            HighReadyResetSpeedMulti = Config.Bind<float>(highReady, "High Ready Reset Speed Multi", 6.0f, new ConfigDescription("How Fast The Weapon Moves Going Out Of Stance", new AcceptableValueRange<float>(1f, 100.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 91, IsAdvanced = true }));
            HighReadySpeedMulti = Config.Bind<float>(highReady, "High Ready Speed Multi", 7.2f, new ConfigDescription("How Fast The Weapon Moves Going Into Stance", new AcceptableValueRange<float>(1f, 100.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 90, IsAdvanced = true }));

            HighReadyOffsetX = Config.Bind<float>(highReady, "High Ready Position X-Axis", 0.005f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 85, IsAdvanced = true }));
            HighReadyOffsetY = Config.Bind<float>(highReady, "High Ready Position Y-Axis", 0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 84, IsAdvanced = true }));
            HighReadyOffsetZ = Config.Bind<float>(highReady, "High Ready Position Z-Axis", -0.05f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 83, IsAdvanced = true }));

            HighReadyRotationX = Config.Bind<float>(highReady, "High Ready Rotation X-Axis", -10.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 72, IsAdvanced = true }));
            HighReadyRotationY = Config.Bind<float>(highReady, "High Ready Rotation Y-Axis", 3.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 71, IsAdvanced = true }));
            HighReadyRotationZ = Config.Bind<float>(highReady, "High Ready Rotation Z-Axis", 3.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 70, IsAdvanced = true }));

            HighReadyAdditionalRotationX = Config.Bind<float>(highReady, "High Ready Additional Rotation X-Axis.", -10.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 69, IsAdvanced = true }));
            HighReadyAdditionalRotationY = Config.Bind<float>(highReady, "High Ready Additiona Rotation Y-Axis.", 10f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 68, IsAdvanced = true }));
            HighReadyAdditionalRotationZ = Config.Bind<float>(highReady, "High Ready Additional Rotation Z-Axis.", 2.5f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 67, IsAdvanced = true }));

            HighReadyResetRotationX = Config.Bind<float>(highReady, "High Ready Reset Rotation X-Axis", 0.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 66, IsAdvanced = true }));
            HighReadyResetRotationY = Config.Bind<float>(highReady, "High Ready Reset Rotation Y-Axis", 2.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 65, IsAdvanced = true }));
            HighReadyResetRotationZ = Config.Bind<float>(highReady, "High Ready Reset Rotation Z-Axis", 1.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 64, IsAdvanced = true }));

            LowReadyAdditionalRotationSpeedMulti = Config.Bind<float>(lowReady, "Low Ready Additonal Rotation Speed Multi", 0.5f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 64, IsAdvanced = true }));
            LowReadyResetRotationMulti = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Speed Multi", 2.5f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 63, IsAdvanced = true }));
            LowReadyRotationMulti = Config.Bind<float>(lowReady, "Low Ready Rotation Speed Multi", 2.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 62, IsAdvanced = true }));
            LowReadySpeedMulti = Config.Bind<float>(lowReady, "Low Ready Speed Multi", 18f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 61, IsAdvanced = true }));
            LowReadyResetSpeedMulti = Config.Bind<float>(lowReady, "Low Ready Reset Speed Multi", 7.2f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 60, IsAdvanced = true }));

            LowReadyOffsetX = Config.Bind<float>(lowReady, "Low Ready Position X-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 55, IsAdvanced = true }));
            LowReadyOffsetY = Config.Bind<float>(lowReady, "Low Ready Position Y-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 54, IsAdvanced = true }));
            LowReadyOffsetZ = Config.Bind<float>(lowReady, "Low Ready Position Z-Axis", 0.0f, new ConfigDescription("Weapon Position When In Stance..", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 53, IsAdvanced = true }));

            LowReadyRotationX = Config.Bind<float>(lowReady, "Low Ready Rotation X-Axis", 8f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 42, IsAdvanced = true }));
            LowReadyRotationY = Config.Bind<float>(lowReady, "Low Ready Rotation Y-Axis", -5.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 41, IsAdvanced = true }));
            LowReadyRotationZ = Config.Bind<float>(lowReady, "Low Ready Rotation Z-Axis", -1.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 40, IsAdvanced = true }));

            LowReadyAdditionalRotationX = Config.Bind<float>(lowReady, "Low Ready Additional Rotation X-Axis", 12.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 39, IsAdvanced = true }));
            LowReadyAdditionalRotationY = Config.Bind<float>(lowReady, "Low Ready Additional Rotation Y-Axis", -50.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 38, IsAdvanced = true }));
            LowReadyAdditionalRotationZ = Config.Bind<float>(lowReady, "Low Ready Additional Rotation Z-Axis", 0.5f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 37, IsAdvanced = true }));

            LowReadyResetRotationX = Config.Bind<float>(lowReady, "Low Ready Reset Rotation X-Axis", -2.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 36, IsAdvanced = true }));
            LowReadyResetRotationY = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Y-Axis", 2.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true }));
            LowReadyResetRotationZ = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Z-Axis", -0.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true }));

            PistolAdditionalRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Additional Rotation Speed Multi", 1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true }));
            PistolResetRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Reset Rotation Speed Multi", 5f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true }));
            PistolRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Rotation Speed Multi", 1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 33, IsAdvanced = true }));
            PistolPosSpeedMulti = Config.Bind<float>(pistol, "Pistol Position Speed Multi", 10.0f, new ConfigDescription("", new AcceptableValueRange<float>(1.0f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 32, IsAdvanced = true }));
            PistolPosResetSpeedMulti = Config.Bind<float>(pistol, "Pistol Position Reset Speed Multi", 7.0f, new ConfigDescription("", new AcceptableValueRange<float>(1.0f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true }));

            PistolOffsetX = Config.Bind<float>(pistol, "Pistol Position X-Axis.", 0.015f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 25, IsAdvanced = true }));
            PistolOffsetY = Config.Bind<float>(pistol, "Pistol Position Y-Axis.", 0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 24, IsAdvanced = true }));
            PistolOffsetZ = Config.Bind<float>(pistol, "Pistol Position Z-Axis.", -0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 23, IsAdvanced = true }));

            PistolRotationX = Config.Bind<float>(pistol, "Pistol Rotation X-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true }));
            PistolRotationY = Config.Bind<float>(pistol, "Pistol Rotation Y-Axis", -15f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, IsAdvanced = true }));
            PistolRotationZ = Config.Bind<float>(pistol, "Pistol Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true }));

            PistolAdditionalRotationX = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation X-Axis.", 0.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true }));
            PistolAdditionalRotationY = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation Y-Axis.", -10.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true }));
            PistolAdditionalRotationZ = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation Z-Axis.", 0.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true }));

            PistolResetRotationX = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation X-Axis", 1.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true }));
            PistolResetRotationY = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation Y-Axis", 2.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true }));
            PistolResetRotationZ = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation Z-Axis", 1.2f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true }));

            ShortStockAdditionalRotationSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Additional Rotation Speed Multi", 2.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true }));
            ShortStockResetRotationSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Reset Rotation Speed Multi", 2.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true }));
            ShortStockRotationMulti = Config.Bind<float>(shortStock, "Short-Stock Rotation Speed Multi", 2.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 33, IsAdvanced = true }));
            ShortStockSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Position Speed Multi", 6.0f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 32, IsAdvanced = true }));
            ShortStockResetSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Position Reset Speed Mult", 6.0f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true }));

            ShortStockOffsetX = Config.Bind<float>(shortStock, "Short-Stock Position X-Axis", 0.02f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 25, IsAdvanced = true }));
            ShortStockOffsetY = Config.Bind<float>(shortStock, "Short-Stock Position Y-Axis", 0.1f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 24, IsAdvanced = true }));
            ShortStockOffsetZ = Config.Bind<float>(shortStock, "Short-Stock Position Z-Axis", -0.025f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 23, IsAdvanced = true }));

            ShortStockRotationX = Config.Bind<float>(shortStock, "Short-Stock Rotation X-Axis", 0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true }));
            ShortStockRotationY = Config.Bind<float>(shortStock, "Short-Stock Rotation Y-Axis", -15.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, IsAdvanced = true }));
            ShortStockRotationZ = Config.Bind<float>(shortStock, "Short-Stock Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true }));

            ShortStockAdditionalRotationX = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation X-Axis", -5.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true }));
            ShortStockAdditionalRotationY = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation Y-Axis", -20.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true }));
            ShortStockAdditionalRotationZ = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation Z-Axis", 5.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true }));

            ShortStockResetRotationX = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation X-Axis", -5.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true }));
            ShortStockResetRotationY = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation Y-Axis", 1-2.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true }));
            ShortStockResetRotationZ = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation Z-Axis", 1.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true }));
        }
    }
}

