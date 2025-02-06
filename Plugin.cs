using BepInEx;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using RealismMod.Health;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static RealismMod.Attributes;
using static RealismMod.ZoneSpawner;

namespace RealismMod
{
    public interface IRealismInfo { }

    public class RealismConfig : IRealismInfo
    {
        public bool recoil_attachment_overhaul { get; set; }
        public bool malf_changes { get; set; }
        public bool realistic_ballistics { get; set; }
        public bool med_changes { get; set; }
        public bool headset_changes { get; set; }
        public bool enable_stances { get; set; }
        public bool movement_changes { get; set; }
        public bool gear_weight { get; set; }
        public bool reload_changes { get; set; }
        public bool manual_chambering { get; set; }
        public bool food_changes { get; set; }
        public bool enable_hazard_zones { get; set; }
        public bool realistic_zombies { get; set; }
        public bool bot_loot_changes { get; set; }
        public bool spawn_waves { get; set; }
        public bool boss_spawns { get; set; }
        public bool loot_changes { get; set; }
    }

    public class RealismEventInfo : IRealismInfo
    {
        public bool IsHalloween { get; set; }
        public bool DoGasEvent { get; set; }
        public bool DoExtraCultists { get; set; }
        public bool DoExtraRaiders { get; set; }
        public bool IsChristmas { get; set; }
        public bool IsPreExplosion { get; set; }
        public bool HasExploded { get; set; }
        public bool IsNightTime { get; set; }   
    }

    public class RealismDir : IRealismInfo 
    {
        public string ServerBaseDirectory { get; set; }
    }

    public enum EUpdateType 
    {
        Full,
        ModInfo,
        ModConfig,
        TimeOfDay,
        Path
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, Plugin.PLUGINVERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string PLUGINVERSION = "1.5.0";

        public static Dictionary<Enum, Sprite> IconCache = new Dictionary<Enum, Sprite>();
        public static Dictionary<string, AudioClip> HitAudioClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> GasMaskAudioClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> HazardZoneClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> DeviceAudioClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> RadEventAudioClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> GasEventAudioClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> GasEventLongAudioClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> FoodPoisoningSfx = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();
        public static Dictionary<string, Texture> LoadedTextures = new Dictionary<string, Texture>();

        private string _baseBundleFilepath;

        //private float _realDeltaTime = 0f;
        private static float _averageFPS = 0f;
        public static float FPS = 0f;

        //mounting UI
        public static GameObject MountingUIGameObject { get; private set; }
        public MountingUI MountingUIComponent;

        //health controller
        public static RealismHealthController RealHealthController;

        //explosion
        public static GameObject ExplosionGO { get; private set; }

        //weather controller
        private GameObject RealismWeatherGameObject { get; set; }
        public static RealismWeatherController RealismWeatherComponent;

        //audio controller
        private GameObject AudioControllerGameObject { get; set; }
        public static RealismAudioControllerComponent RealismAudioControllerComponent;

        public static bool HasReloadedAudio = false;
        public static bool FikaPresent = false;
        public static bool FOVFixPresent = false;
        private bool _detectedMods = false;

        public static bool StartRechamberTimer = false;
        public static float ChamberTimer = 0f;
        public static bool CanLoadChamber = false;
        public static bool BlockChambering = false;

        private bool _gotProfileId = false;

        public static RealismConfig ServerConfig;
        public static RealismEventInfo ModInfo;
        public static RealismDir ModDir;

        private static T UpdateInfoFromServer<T>(string route) where T : class, IRealismInfo
        {
            Utils.Logger.LogWarning("update from server: " + route);

            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            try
            {
                var json = RequestHandler.GetJson(route);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Utils.Logger.LogError($"REALISM MOD ERROR: FAILED TO FETCH DATA FROM SERVER USING ROUTE {route}: {ex.Message}");
                return null;    
            }
        }

        public static void RequestRealismDataFromServer(EUpdateType updateType)
        {
            switch(updateType)
            {
                case EUpdateType.Full:
                    ServerConfig = UpdateInfoFromServer<RealismConfig>("/RealismMod/GetConfig");
                    ModInfo = UpdateInfoFromServer<RealismEventInfo>("/RealismMod/GetInfo");
                    ModDir = UpdateInfoFromServer<RealismDir>("/RealismMod/GetDirectory");
                    Utils.Logger.LogWarning("directory " + ModDir.ServerBaseDirectory);
                    break;
                case EUpdateType.ModInfo:
                    ModInfo = UpdateInfoFromServer<RealismEventInfo>("/RealismMod/GetInfo");
                    break;
                case EUpdateType.ModConfig:
                    ServerConfig = UpdateInfoFromServer<RealismConfig>("/RealismMod/GetConfig");
                    break;
                case EUpdateType.TimeOfDay:
                    ModInfo = UpdateInfoFromServer<RealismEventInfo>("/RealismMod/GetTimeOfDay");
                    break;
            }
        }

        private async void CacheIcons()
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
            IconCache.Add(ENewItemAttributeId.BallisticCoefficient, Resources.Load<Sprite>("characteristics/icons/SightingRange"));
            IconCache.Add(ENewItemAttributeId.ArmorDamage, Resources.Load<Sprite>("characteristics/icons/armorMaterial"));
            IconCache.Add(ENewItemAttributeId.FragmentationChance, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(ENewItemAttributeId.MalfunctionChance, Resources.Load<Sprite>("characteristics/icons/icon_info_raidmoddable"));
            IconCache.Add(ENewItemAttributeId.CanSpall, Resources.Load<Sprite>("characteristics/icons/icon_info_bulletspeed"));
            IconCache.Add(ENewItemAttributeId.SpallReduction, Resources.Load<Sprite>("characteristics/icons/Velocity"));
            IconCache.Add(ENewItemAttributeId.GearReloadSpeed, Resources.Load<Sprite>("characteristics/icons/weapFireType"));
            IconCache.Add(ENewItemAttributeId.CantADS, Resources.Load<Sprite>("characteristics/icons/SightingRange"));
            IconCache.Add(ENewItemAttributeId.CanADS, Resources.Load<Sprite>("characteristics/icons/SightingRange"));
            IconCache.Add(ENewItemAttributeId.NoiseReduction, Resources.Load<Sprite>("characteristics/icons/icon_info_loudness"));
            IconCache.Add(ENewItemAttributeId.ProjectileCount, Resources.Load<Sprite>("characteristics/icons/icon_info_bulletspeed"));
            IconCache.Add(ENewItemAttributeId.Convergence, Resources.Load<Sprite>("characteristics/icons/Ergonomics"));
            IconCache.Add(ENewItemAttributeId.HBleedType, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(ENewItemAttributeId.LimbHpPerTick, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(ENewItemAttributeId.HpPerTick, Resources.Load<Sprite>("characteristics/icons/hpResource"));
            IconCache.Add(ENewItemAttributeId.RemoveTrnqt, Resources.Load<Sprite>("characteristics/icons/hpResource"));
            IconCache.Add(ENewItemAttributeId.Comfort, Resources.Load<Sprite>("characteristics/icons/Weight"));
            IconCache.Add(ENewItemAttributeId.GasProtection, Resources.Load<Sprite>("characteristics/icons/icon_info_intoxication"));
            IconCache.Add(ENewItemAttributeId.RadProtection, Resources.Load<Sprite>("characteristics/icons/icon_info_radiation"));
            IconCache.Add(ENewItemAttributeId.PainKillerStrength, Resources.Load<Sprite>("characteristics/icons/hpResource"));
            IconCache.Add(ENewItemAttributeId.MeleeDamage, Resources.Load<Sprite>("characteristics/icons/icon_info_bloodloss"));
            IconCache.Add(ENewItemAttributeId.MeleePen, Resources.Load<Sprite>("characteristics/icons/icon_info_bulletspeed"));
            IconCache.Add(ENewItemAttributeId.OutOfRaidHP, Resources.Load<Sprite>("characteristics/icons/hpResource"));
            IconCache.Add(ENewItemAttributeId.StimType, Resources.Load<Sprite>("characteristics/icons/hpResource"));
            IconCache.Add(ENewItemAttributeId.DurabilityBurn, Resources.Load<Sprite>("characteristics/icons/Velocity"));
            IconCache.Add(ENewItemAttributeId.Heat, Resources.Load<Sprite>("characteristics/icons/Velocity"));
            IconCache.Add(ENewItemAttributeId.MuzzleFlash, Resources.Load<Sprite>("characteristics/icons/Velocity"));
            IconCache.Add(ENewItemAttributeId.Handling, Resources.Load<Sprite>("characteristics/icons/Ergonomics"));
            IconCache.Add(ENewItemAttributeId.AimStability, Resources.Load<Sprite>("characteristics/icons/SightingRange"));

            Sprite balanceSprite = await RequestResource<Sprite>(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\icons\\balance.png");
            Sprite recoilAngleSprite = await RequestResource<Sprite>(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\icons\\recoilAngle.png");
            IconCache.Add(ENewItemAttributeId.Balance, balanceSprite);
            IconCache.Add(ENewItemAttributeId.RecoilAngle, recoilAngleSprite);
        }

        private void LoadTextures()
        {
            string[] texFilesDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\masks\\", "*.png");

            foreach (string fileDir in texFilesDir)
            {
                LoadTexture(fileDir);
            }
        }

        private void LoadSprites()
        {
            string[] iconFilesDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\icons\\", "*.png");

            foreach (string fileDir in iconFilesDir)
            {
                LoadSprite(fileDir);
            }
        }

        private async void LoadSprite(string path)
        {
            LoadedSprites[Path.GetFileName(path)] = await RequestResource<Sprite>(path);
        }

        private async void LoadTexture(string path)
        {
            LoadedTextures[Path.GetFileName(path)] = await RequestResource<Texture>(path, true);
        }

        private async Task<T> RequestResource<T>(string path, bool isMask = false) where T : class
        {
            UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(path);
            UnityWebRequestAsyncOperation sendWeb = uwr.SendWebRequest();

            while (!sendWeb.isDone)
                await Task.Yield();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Logger.LogError("Realism Mod: Failed To Fetch Resource");
                return null;
            }
            else
            {
                Texture2D texture = ((DownloadHandlerTexture)uwr.downloadHandler).texture;

                if (typeof(T) == typeof(Texture))
                {
                    if (isMask)
                    {
                        Texture2D flipped = new Texture2D(texture.width, texture.height);
                        for (int y = 0; y < texture.height; y++)
                        {
                            for (int x = 0; x < texture.width; x++)
                            {
                                flipped.SetPixel(x, texture.height - y - 1, texture.GetPixel(x, y));
                            }
                        }
                        flipped.Apply();
                        texture = flipped;
                    }
                    return texture as T;
                }
                else if (typeof(T) == typeof(Sprite))
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    return sprite as T;
                }
                else
                {
                    Logger.LogError("Realism Mod: Unsupported resource type requested");
                    return null;
                }
            }
        }

        private async void LoadAudioClipHelper(string[] fileDirectories, Dictionary<string, AudioClip> clips) 
        {
            foreach (var fileDir in fileDirectories)
            {
                clips[Path.GetFileName(fileDir)] = await RequestAudioClip(fileDir);
            }
        }

        private void LoadAudioClips()
        {
            string[] hitSoundsDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\hitsounds");
            string[] gasMaskDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\gasmask");
            string[] hazardDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\zones");
            string[] deviceDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\devices");
            string[] gasEventAmbient = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\zones\\mapgas\\default");
            string[] radEventAmbient = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\zones\\maprads");
            string[] gasEventLongAmbient = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\zones\\mapgas\\long");
            string[] foodPoisoning = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\health\\foodpoisoning");

            HitAudioClips.Clear();
            GasMaskAudioClips.Clear();
            HazardZoneClips.Clear();
            DeviceAudioClips.Clear();
            GasEventAudioClips.Clear();
            RadEventAudioClips.Clear();
            FoodPoisoningSfx.Clear();

            LoadAudioClipHelper(hitSoundsDir, HitAudioClips);
            LoadAudioClipHelper(gasMaskDir, GasMaskAudioClips);
            LoadAudioClipHelper(hazardDir, HazardZoneClips);
            LoadAudioClipHelper(deviceDir, DeviceAudioClips);
            LoadAudioClipHelper(gasEventAmbient, GasEventAudioClips);
            LoadAudioClipHelper(radEventAmbient, RadEventAudioClips);
            LoadAudioClipHelper(gasEventLongAmbient, GasEventLongAudioClips);
            LoadAudioClipHelper(foodPoisoning, FoodPoisoningSfx);

            Plugin.HasReloadedAudio = true;
        }

        private async Task<AudioClip> RequestAudioClip(string path)
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
            UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            UnityWebRequestAsyncOperation sendWeb = uwr.SendWebRequest();

            while (!sendWeb.isDone)
                await Task.Yield();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Logger.LogError("Realism Mod: Failed To Fetch Audio Clip");
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(uwr);
                return audioclip;
            }
        }

        private AssetBundle LoadAndInitializePrefabs(string bundlePath)
        {
            string fullPath = Path.Combine(_baseBundleFilepath, bundlePath);
            AssetBundle bundle = AssetBundle.LoadFromFile(fullPath);
            return bundle;
        }

        private void LoadBundles() 
        {
            _baseBundleFilepath = Path.Combine(Environment.CurrentDirectory, "BepInEx\\plugins\\Realism\\bundles\\");

            Assets.GooBarrelBundle = LoadAndInitializePrefabs("hazard_assets\\yellow_barrel.bundle");
            Assets.BlueBoxBundle = LoadAndInitializePrefabs("hazard_assets\\bluebox.bundle");
            Assets.RedForkLiftBundle = LoadAndInitializePrefabs("hazard_assets\\redforklift.bundle");
            Assets.ElectroForkLiftBundle = LoadAndInitializePrefabs("hazard_assets\\electroforklift.bundle");
            Assets.LabsCrateBundle = LoadAndInitializePrefabs("hazard_assets\\labscrate.bundle");
            Assets.UralBundle = LoadAndInitializePrefabs("hazard_assets\\ural.bundle");
            Assets.BluePalletBundle = LoadAndInitializePrefabs("hazard_assets\\bluepallet.bundle");
            Assets.BlueFuelPalletClothBundle = LoadAndInitializePrefabs("hazard_assets\\bluebarrelpalletcloth.bundle");
            Assets.BarrelPileBundle = LoadAndInitializePrefabs("hazard_assets\\barrelpile.bundle");
            Assets.LabsCrateSmallBundle = LoadAndInitializePrefabs("hazard_assets\\labscratesmall.bundle");
            Assets.YellowPlasticPalletBundle = LoadAndInitializePrefabs("hazard_assets\\yellowbarrelpallet.bundle");
            Assets.WhitePlasticPalletBundle = LoadAndInitializePrefabs("hazard_assets\\whitebarrelpallet.bundle");
            Assets.MetalFenceBundle = LoadAndInitializePrefabs("hazard_assets\\metalfence.bundle");
            Assets.RedContainerBundle = LoadAndInitializePrefabs("hazard_assets\\redcontainer.bundle");
            Assets.BlueContainerBundle = LoadAndInitializePrefabs("hazard_assets\\bluecontainer.bundle");
            Assets.LabsBarrelPileBundle = LoadAndInitializePrefabs("hazard_assets\\labsbarrelpile.bundle");
            Assets.RadSign1 = LoadAndInitializePrefabs("hazard_assets\\radsign1.bundle");
            Assets.TerraGroupFence = LoadAndInitializePrefabs("hazard_assets\\terragroupchainfence.bundle");
            Assets.FogBundle = LoadAndInitializePrefabs("hazard_assets\\fog.bundle");
            Assets.GasBundle = LoadAndInitializePrefabs("hazard_assets\\gas.bundle");
            Assets.ExplosionBundle = LoadAndInitializePrefabs("exp\\expl.bundle");
            ExplosionGO = Assets.ExplosionBundle.LoadAsset<GameObject>("Assets/Explosion/Prefab/NUCLEAR_EXPLOSION.prefab");
            DontDestroyOnLoad(ExplosionGO);
        }

        private void LoadMountingUI()
        {
            MountingUIGameObject = new GameObject();
            MountingUIComponent = MountingUIGameObject.AddComponent<MountingUI>();
            DontDestroyOnLoad(MountingUIGameObject);
        }

        private void LoadWeatherController()
        {
            RealismWeatherGameObject = new GameObject();
            RealismWeatherComponent = RealismWeatherGameObject.AddComponent<RealismWeatherController>();
            DontDestroyOnLoad(RealismWeatherGameObject);
        }

        private void LoadAudioController() 
        {
            AudioControllerGameObject = new GameObject();
            RealismAudioControllerComponent = AudioControllerGameObject.AddComponent<RealismAudioControllerComponent>();
            DontDestroyOnLoad(AudioControllerGameObject);
        }

        private void LoadHealthController()
        {
            DamageTracker dmgTracker = new DamageTracker();
            RealismHealthController healthController = new RealismHealthController(dmgTracker);
            RealHealthController = healthController;
        }

        void Awake()
        {
            Utils.Logger = Logger;
        
            try
            {
                RequestRealismDataFromServer(EUpdateType.Full);
                LoadBundles();   
                LoadSprites();
                LoadTextures();
                LoadAudioClips();
                CacheIcons();
                ZoneData.DeserializeZoneData();
                Stats.GetStats();

            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }

            LoadMountingUI();
            LoadWeatherController();
            LoadHealthController();
            LoadAudioController();

            PluginConfig.InitConfigBindings(Config);

            MoveDaCube.InitTempBindings(Config); //TEMPORARY

            LoadGeneralPatches();

            //hazards
            if (ServerConfig.enable_hazard_zones) 
            {
                LoadHazardPatches();
            }
            //malfunctions
            if (ServerConfig.malf_changes)
            {
                LoadMalfPatches();
            }

            //recoil and attachments
            if (ServerConfig.recoil_attachment_overhaul) 
            {
                LoadRecoilPatches();
            }

            LoadReloadPatches();
  
            //Ballistics
            if (ServerConfig.realistic_ballistics)
            {
                LoadBallisticsPatches();
            }

            //Deafen Effects
            if (ServerConfig.headset_changes)
            {
                LoadDeafenPatches();
            }

            //gear patces
            if (ServerConfig.gear_weight)
            {      
                new TotalWeightPatch().Enable();
            }

            //Movement
            if (ServerConfig.movement_changes) 
            {
                LoadMovementPatches();
            }

            //Stances
            if (ServerConfig.enable_stances) 
            {
                LoadStancePatches();
            }

            //also needed for visual recoil
            if (ServerConfig.enable_stances || ServerConfig.realistic_ballistics)
            {
                new ApplyComplexRotationPatch().Enable(); 
            }

            if (ServerConfig.enable_stances || ServerConfig.headset_changes) 
            {
                new ADSAudioPatch().Enable();
            }

            //Health
            if (ServerConfig.med_changes)
            {
                LoadMedicalPatches();
            }

            //needed for food and meds
            if (ServerConfig.med_changes || ServerConfig.food_changes)
            {
                new ApplyItemStashPatch().Enable();
                new StimStackPatch1().Enable();
                new StimStackPatch2().Enable();
            }

        }

        private void CheckForMods() 
        {
            if (!_detectedMods && (int)Time.time % 5 == 0)
            {
                _detectedMods = true;
                if (Chainloader.PluginInfos.ContainsKey("com.fika.core"))
                {
                    FikaPresent = true;
                }
                if (Chainloader.PluginInfos.ContainsKey("FOVFix"))
                {
                    FOVFixPresent = true;
                }
            }
        }

        private void CheckForProfileData() 
        {
            //keep trying to get player profile id and update hazard values
            if (!_gotProfileId)
            {
                try
                {
                    var sessionData = Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession();
                    ProfileData.PMCProfileId = sessionData.Profile.Id;
                    ProfileData.ScavProfileId = sessionData.ProfileOfPet.Id;
                    ProfileData.PMCLevel = sessionData.Profile.Info.Level;
                    if (ServerConfig.enable_hazard_zones)
                    {
                        HazardTracker.GetHazardValues(ProfileData.PMCProfileId);
                    }
                    _gotProfileId = true;
                }
                catch 
                {
                    if (PluginConfig.EnableGeneralLogging.Value) Logger.LogWarning("Realism Mod: Error Getting Profile ID, Retrying");
                }
            }
        }

        private void ZoneDebugUpdate() 
        {
            if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                var player = Utils.GetYourPlayer().Transform;
#pragma warning disable CS4014
                Utils.LoadLoot(player.position, player.rotation, PluginConfig.TargetZone.Value);
#pragma warning disable CS4014
                Utils.Logger.LogWarning("\"position\": {" + "\"x\":" + player.position.x + "," + "\"y\":" + player.position.y + "," + "\"z\":" + player.position.z + "},");
                Utils.Logger.LogWarning("new Vector3(" + player.position.x + "f, " + player.position.y + "f, " + player.position.z + "f)");
                Utils.Logger.LogWarning("\"rotation\": {" + "\"x\":" + player.rotation.eulerAngles.x + "," + "\"y\":" + player.eulerAngles.y + "," + "\"z\":" + player.eulerAngles.z + "}");
            }
            if (Input.GetKeyDown(KeyCode.Keypad0)) HazardTracker.WipeTracker();
         
            if (Input.GetKeyDown(PluginConfig.AddZone.Value.MainKey)) DebugZones();

            //if (Input.GetKeyDown(KeyCode.Keypad5)) Instantiate(Plugin.ExplosionGO, new Vector3(PluginConfig.test1.Value, PluginConfig.test2.Value, PluginConfig.test3.Value), new Quaternion(0, 0, 0, 0)); //new Vector3(1000f, 0f, 317f)
        }

        //games procedural animations are highly affected by FPS. I balanced everything at 144 FPS, so need to factor it.    
        private void SetFps()
        {
            _averageFPS += ((Time.deltaTime / Time.timeScale) - _averageFPS) * 0.035f;
            FPS = (1f / _averageFPS);
            if (float.IsNaN(FPS) || FPS <= 1f) FPS = 144f;
            FPS = Mathf.Clamp(FPS, 30f, 200f);
        }

        void Update()
        {
            //TEMPORARY
            if (GameWorldController.GameStarted && PluginConfig.ZoneDebug.Value) MoveDaCube.Update();

            SetFps();
            CheckForProfileData();
            CheckForMods();

            if (ServerConfig.enable_hazard_zones) HazardTracker.OutOfRaidUpdate();
            if (PluginConfig.ZoneDebug.Value) ZoneDebugUpdate();

            Utils.CheckIsReady();
            if (Utils.PlayerIsReady)
            {
                GameWorldController.GameWorldUpdate();

                if (ServerConfig.med_changes) RealHealthController.ControllerUpdate();

                if (!Plugin.HasReloadedAudio)
                {
                    LoadAudioClips();
                }

                if (ServerConfig.headset_changes && ScreenEffectsController.PrismEffects != null)
                {
                    HeadsetGainController.AdjustHeadsetVolume();
                    DeafenController.DoDeafening();
                }
                if (ServerConfig.enable_stances) 
                {
                    StanceController.StanceUpdate();
                }
            }
            else 
            {
                HasReloadedAudio = false;
            }
        }

        private void LoadGeneralPatches()
        {
            if (ServerConfig.spawn_waves) new SpawnUpdatePatch().Enable();
  
            //deafening + adrenaline trigger
            new FlyingBulletPatch().Enable();

            //misc
            new ChamberCheckUIPatch().Enable();

            //multiple
            new KeyInputPatch1().Enable();
            new KeyInputPatch2().Enable();
            new SyncWithCharacterSkillsPatch().Enable();
            new OnItemAddedOrRemovedPatch().Enable();
            new PlayerUpdatePatch().Enable();
            new PlayerInitPatch().Enable();
            new FaceshieldMaskPatch().Enable();
            new PlayPhrasePatch().Enable();
            new OnGameStartPatch().Enable();
            new OnGameEndPatch().Enable();
            new QuestCompletePatch().Enable();

            //stats used by multiple features
            new RigConstructorPatch().Enable();
            new EquipmentPenaltyComponentPatch().Enable();

            if (ServerConfig.loot_changes)
            {
                new StaticLootSpawnPatch().Enable();
                new RigidLootSpawnPatch().Enable();
            }
        }

        private void LoadHazardPatches()
        {
            new HealthPanelPatch().Enable();
            new DropItemPatch().Enable();
            new GetAvailableActionsPatch().Enable();
            if (ServerConfig.boss_spawns) new BossSpawnPatch().Enable();
            new LampPatch().Enable();
            new AmbientSoundPlayerGroupPatch().Enable();
            new DayTimeAmbientPatch().Enable();
            new DayTimeSpawnPatch().Enable();
            new BirdPatch().Enable();
        }

        private void LoadMalfPatches()
        {
            new RemoveSillyBossForcedMalf().Enable();
            new GetTotalMalfunctionChancePatch().Enable();
            new IsKnownMalfTypePatch().Enable();
            new RemoveForcedMalf().Enable();
            if (ServerConfig.manual_chambering)
            {
                new SetAmmoCompatiblePatch().Enable();
                new StartReloadPatch().Enable();
                new StartEquipWeapPatch().Enable();
                new SetAmmoOnMagPatch().Enable();
                new PreChamberLoadPatch().Enable();
            }
        }

        private void LoadRecoilPatches()
        {
            //procedural animations
            /*new CalculateCameraPatch().Enable();*/
            if (PluginConfig.EnableMuzzleEffects.Value) new MuzzleEffectsPatch().Enable();
            new UpdateWeaponVariablesPatch().Enable();
            new SetAimingSlowdownPatch().Enable();
            new PwaWeaponParamsPatch().Enable();
            new UpdateSwayFactorsPatch().Enable();
            new GetOverweightPatch().Enable();
            new SetOverweightPatch().Enable();
            new BreathProcessPatch().Enable();
            new CamRecoilPatch().Enable();

            //weapon and related
            new TotalShotgunDispersionPatch().Enable();
            new GetDurabilityLossOnShotPatch().Enable();
            new FireratePitchPatch().Enable();
            new AutoFireRatePatch().Enable();
            new SingleFireRatePatch().Enable();
            new ErgoDeltaPatch().Enable();
            new ErgoWeightPatch().Enable();
            new PlayerErgoPatch().Enable();
            new ToggleAimPatch().Enable();
            new GetMalfunctionStatePatch().Enable();
            if (PluginConfig.EnableZeroShift.Value)
            {
                new CalibrationLookAt().Enable();
                new CalibrationLookAtScope().Enable();
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

            new FireRateDisplayStringValuePatch().Enable();

            new ModErgoStatDisplayPatch().Enable();
            new GetAttributeIconPatches().Enable();
            new MagazineMalfChanceDisplayPatch().Enable();
            new BarrelModClassPatch().Enable();
            new AmmoCaliberPatch().Enable();

            new COIDeltaPatch().Enable();
            new CenterOfImpactMOAPatch().Enable();
            new COIDisplayDeltaPatch().Enable();
            new COIDisplayStringValuePatch().Enable();
            new GetTotalCenterOfImpactPatch().Enable();

            //Recoil Patches
            //new GetCameraRotationRecoilPatch().Enable(); makes recoil feel iffy, doesn't seem needed
            new RecalcWeaponParametersPatch().Enable();
            new AddRecoilForcePatch().Enable();
            new RecoilAnglesPatch().Enable();
            new ShootPatch().Enable();
            new RotatePatch().Enable();
        }

        private void LoadReloadPatches()
        {
            //Reload Patches
            if (ServerConfig.reload_changes)
            {
                new CanStartReloadPatch().Enable();
                new ReloadMagPatch().Enable();
                new QuickReloadMagPatch().Enable();
                new SetMagTypeCurrentPatch().Enable();
                new SetMagTypeNewPatch().Enable();
                new SetMagInWeaponPatch().Enable();
                new SetMalfRepairSpeedPatch().Enable();
                new BoltActionReloadPatch().Enable();
                new SetWeaponLevelPatch().Enable();
            }

            if (ServerConfig.reload_changes || ServerConfig.recoil_attachment_overhaul || ServerConfig.enable_stances)
            {
                new ReloadWithAmmoPatch().Enable();
                new ReloadBarrelsPatch().Enable();
                new ReloadCylinderMagazinePatch().Enable();
                new OnMagInsertedPatch().Enable();
                new SetSpeedParametersPatch().Enable();
                new CheckAmmoPatch().Enable();
                new CheckChamberPatch().Enable();
                new RechamberPatch().Enable();
                new SetAnimatorAndProceduralValuesPatch().Enable();
                new AimPunchPatch().Enable();
            }
        }

        private void LoadStancePatches()
        {
            new DoorAnimationOverride().Enable();
            new ChangeScopePatch().Enable();
            new TacticalReloadPatch().Enable();
            new WeaponOverlapViewPatch().Enable();
            new CollisionPatch().Enable();
            new WeaponOverlappingPatch().Enable();
            new WeaponLengthPatch().Enable();
            new ApplySimpleRotationPatch().Enable();
            new InitTransformsPatch().Enable();
            new ZeroAdjustmentsPatch().Enable();
            new OnWeaponDrawPatch().Enable();
            new UpdateHipInaccuracyPatch().Enable();
            new SetFireModePatch().Enable();
            new OperateStationaryWeaponPatch().Enable();
            new SetTiltPatch().Enable();
            new BattleUIScreenPatch().Enable();
            new ChangePosePatch().Enable();
            new MountingAndCollisionPatch().Enable();
            new ShouldMoveWeapCloserPatch().Enable();
        }

        private void LoadMedicalPatches()
        {
            new SetQuickSlotPatch().Enable();
            new ApplyItemPatch().Enable();
            new BreathIsAudiblePatch().Enable();
            new SetMedsInHandsPatch().Enable();
            new ProceedMedsPatch().Enable();
            new RemoveEffectPatch().Enable();
            new StaminaRegenRatePatch().Enable();
            new HealthEffectsConstructorPatch().Enable();
            new HCApplyDamagePatch().Enable();
            new RestoreBodyPartPatch().Enable();
            new ToggleHeadDevicePatch().Enable();
            new HealCostDisplayShortPatch().Enable();
            new HealCostDisplayFullPatch().Enable();
        }

        private void LoadMovementPatches()
        {
            if (PluginConfig.EnableMaterialSpeed.Value)
            {
                new CalculateSurfacePatch().Enable();
            }
            new ClampSpeedPatch().Enable();
            new SprintAccelerationPatch().Enable();
            new EnduranceSprintActionPatch().Enable();
            new EnduranceMovementActionPatch().Enable();
        }

        private void LoadDeafenPatches()
        {
            new PrismEffectsEnablePatch().Enable();
            new PrismEffectsDisablePatch().Enable();
            new UpdatePhonesPatch().Enable();
            new RegisterShotPatch().Enable();
            new ExplosionPatch().Enable();
            new GrenadeClassContusionPatch().Enable();
            new CovertMovementVolumePatch().Enable();
            new CovertMovementVolumeBySpeedPatch().Enable();
            new CovertEquipmentVolumePatch().Enable();
            new HeadsetConstructorPatch().Enable();
            new GunshotVolumePatch().Enable();
        }

        private void LoadBallisticsPatches()
        {
            /*new SetSkinPatch().Enable();*/
            new PenetrationUIPatch().Enable();
            new InitiateShotPatch().Enable();
            new VelocityPatch().Enable();
            new CreateShotPatch().Enable();
            new ApplyArmorDamagePatch().Enable();
            new ApplyDamageInfoPatch().Enable();
            new SetPenetrationStatusPatch().Enable();
            new IsPenetratedPatch().Enable();
            new AfterPenPlatePatch().Enable();
            new IsShotDeflectedByHeavyArmorPatch().Enable();
            new ArmorLevelUIPatch().Enable();
            new ArmorLevelDisplayPatch().Enable();
            new ArmorClassStringPatch().Enable();
            new DamageInfoPatch().Enable();
            if (PluginConfig.EnableRagdollFix.Value) new ApplyCorpseImpulsePatch().Enable();
            new GetCachedReadonlyQualitiesPatch().Enable();
        }
    }
}



