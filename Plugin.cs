using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Weather;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using static RealismMod.Attributes;
using static RealismMod.GameWorldController;


namespace RealismMod
{
    public class RealismConfig
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
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, Plugin.pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string pluginVersion = "1.4.2";

        public static Dictionary<Enum, Sprite> IconCache = new Dictionary<Enum, Sprite>();
        public static Dictionary<string, AudioClip> HitAudioClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> GasMaskAudioClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> HazardZoneClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> DeviceAudioClips = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();
        public static Dictionary<string, Texture> LoadedTextures = new Dictionary<string, Texture>();

        public static RealismConfig ServerConfig;

        private float _realDeltaTime = 0f;
        public static float FPS = 1f;

        //mounting UI
        public static GameObject MountingUIGameObject { get; private set; }
        public MountingUI MountingUIComponent;

        //health controller
        public static RealismHealthController RealHealthController;

        //explosion
        public static UnityEngine.Object ExplosionPrefab { get; private set; }

        //weather controller
        public static GameObject RealismWeatherGameObject { get; private set; }
        public RealismWeatherController RealismWeatherComponent;

        public static bool HasReloadedAudio = false;
        public static bool FikaPresent = false;
        public static bool FOVFixPresent = false;
        private bool _detectedMods = false;

        public static bool StartRechamberTimer = false;
        public static float ChamberTimer = 0f;
        public static bool CanLoadChamber = false;
        public static bool BlockChambering = false;

        public static string CurrentProfileId = string.Empty;
        public static string PMCProfileId = string.Empty;
        public static string ScavProfileId = string.Empty;
        private bool _gotProfileId = false;

        private void LoadConfig()
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            try
            {
                var jsonString = RequestHandler.GetJson("/RealismMod/GetInfo");
                ServerConfig = JsonConvert.DeserializeObject<RealismConfig>(jsonString);
            }
            catch (Exception ex)
            {
                Logger.LogError($"REALISM MOD ERROR: FAILED TO FETCH CONFIG DATA FROM SERVER: {ex.Message}");
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

        private async void LoadAudioClips()
        {
            string[] hitSoundsDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\hitsounds");
            string[] gasMaskDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\gasmask");
            string[] hazardDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\zones");
            string[] deviceDir = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\sounds\\devices");

            HitAudioClips.Clear();
            GasMaskAudioClips.Clear();
            HazardZoneClips.Clear();
            DeviceAudioClips.Clear();

            foreach (string fileDir in hitSoundsDir)
            {
                HitAudioClips[Path.GetFileName(fileDir)] = await RequestAudioClip(fileDir);
            }
            foreach (string fileDir in gasMaskDir)
            {
                GasMaskAudioClips[Path.GetFileName(fileDir)] = await RequestAudioClip(fileDir);
            }
            foreach (string fileDir in hazardDir)
            {
                HazardZoneClips[Path.GetFileName(fileDir)] = await RequestAudioClip(fileDir);
            }
            foreach (string fileDir in deviceDir)
            {
                DeviceAudioClips[Path.GetFileName(fileDir)] = await RequestAudioClip(fileDir);
            }

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

        private void LoadExplosion()
        {
            String filename = Path.Combine(Environment.CurrentDirectory, "BepInEx/plugins/Realism/exp/expl.bundle");
            var bundle = AssetBundle.LoadFromFile(filename);
            ExplosionPrefab = bundle.LoadAsset("Assets/Explosion/Prefab/NUCLEAR_EXPLOSION.prefab");
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
                /*LoadExplosion();*/
                LoadConfig();
                LoadSprites();
                LoadTextures();
                LoadAudioClips();
                CacheIcons();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }

            LoadMountingUI();
           /* LoadWeatherController();*/
            LoadHealthController();
            InitConfigBindings();

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
                if (Chainloader.PluginInfos.ContainsKey("com.servph.realisticrecoil") && ServerConfig.recoil_attachment_overhaul)
                {
                    NotificationManagerClass.DisplayWarningNotification("ERROR: COMBAT OVERHAUL DETECTED, IT IS NOT COMPATIBLE!", EFT.Communications.ENotificationDurationType.Long);
                }
                if (Chainloader.PluginInfos.ContainsKey("com.IcyClawz.MunitionsExpert") && ServerConfig.recoil_attachment_overhaul)
                {
                    NotificationManagerClass.DisplayWarningNotification("ERROR: MUNITIONS EXPERT DETECTED, IT IS NOT COMPATIBLE!", EFT.Communications.ENotificationDurationType.Long);
                }
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

        private void CheckForProfileID() 
        {
            //keep trying to get player profile id and update hazard values
            if (!_gotProfileId)
            {
                try
                {
                    PMCProfileId = Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession().Profile.Id;
                    ScavProfileId = Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession().ProfileOfPet.Id;
                    if(ServerConfig.enable_hazard_zones) HazardTracker.GetHazardValues(PMCProfileId);
                    _gotProfileId = true;
                }
                catch 
                {
                    if (PluginConfig.EnableLogging.Value) Logger.LogWarning("Realism Mod: Error Getting Profile ID, Retrying");
                }
            }
        }

        void Update()
        {
            //games procedural animations are highly affected by FPS. I balanced everything at 144 FPS, so need to factor it.    
            _realDeltaTime += (Time.unscaledDeltaTime - _realDeltaTime) * 0.1f;
            FPS = 1.0f / _realDeltaTime;

            CheckForProfileID();
            CheckForMods();

            Utils.CheckIsReady();
            if (Utils.IsReady)
            {
              /*  if (GameWorldController.GameStarted && Input.GetKeyDown(KeyCode.N))
                {
                    var player = Utils.GetYourPlayer().Transform;
                    Instantiate(ExplosionPrefab, new Vector3(1000f, 0f, 317f), new Quaternion(0, 0, 0, 0));
                }*/

                if (PluginConfig.ZoneDebug.Value && Input.GetKeyDown(KeyCode.Keypad0))
                {
                    HazardTracker.WipeTracker();              
                }

                if (PluginConfig.ZoneDebug.Value && Input.GetKeyDown(PluginConfig.AddZone.Value.MainKey))
                {
                    DebugZones();
                }

                if (!Plugin.HasReloadedAudio)
                {
                    LoadAudioClips();
                    Plugin.HasReloadedAudio = true;
                }

                RecoilController.RecoilUpdate();

                if (ServerConfig.headset_changes)
                {
                    AudioControllers.HeadsetVolumeAdjust();
                    if (DeafeningController.PrismEffects != null)
                    {
                        DeafeningController.DoDeafening();
                    }
                }
                if (ServerConfig.enable_stances) 
                {
                    StanceController.StanceState();
                }
            }
            else
            {
                HasReloadedAudio = false;
            }
            if (ServerConfig.med_changes)
            {
                RealHealthController.ControllerUpdate();
            }
        }

        private void LoadGeneralPatches()
        {
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
        }

        private void LoadHazardPatches()
        {
            new HealthPanelPatch().Enable();
        }

        private void LoadMalfPatches()
        {
            new GetTotalMalfunctionChancePatch().Enable();
            new IsKnownMalfTypePatch().Enable();
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

            new PenetrationUIPatch().Enable();

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
            new GetCameraRotationRecoilPatch().Enable();
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
            }
        }

        private void LoadStancePatches()
        {
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
            new OperateStationaryWeaponPatch().Enable();
            new SetTiltPatch().Enable();
            new BattleUIScreenPatch().Enable();
            new ChangePosePatch().Enable();
            new MountingPatch().Enable();
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
            new MedkitConstructorPatch().Enable();
            new HealthEffectsConstructorPatch().Enable();
            new HCApplyDamagePatch().Enable();
            new RestoreBodyPartPatch().Enable();
            new FlyingBulletPatch().Enable();
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
            new SetCompressorPatch().Enable();
            new RegisterShotPatch().Enable();
            new ExplosionPatch().Enable();
            new GrenadeClassContusionPatch().Enable();
            new CovertMovementVolumePatch().Enable();
            new CovertMovementVolumeBySpeedPatch().Enable();
            new CovertEquipmentVolumePatch().Enable();
            new HeadsetConstructorPatch().Enable();
        }

        private void LoadBallisticsPatches()
        {
            /*new SetSkinPatch().Enable();*/
            /*new CollidersPatch().Enable();*/
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

        private void InitConfigBindings()
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

            PluginConfig.test1 = Config.Bind<float>(testing, "test 1", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 170, IsAdvanced = true, Browsable = true }));
            PluginConfig.test2 = Config.Bind<float>(testing, "test 2", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 160, IsAdvanced = true, Browsable = true }));
            PluginConfig.test3 = Config.Bind<float>(testing, "test 3", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 150, IsAdvanced = true, Browsable = true }));
            PluginConfig.test4 = Config.Bind<float>(testing, "test 4", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 140, IsAdvanced = true, Browsable = true }));
            PluginConfig.test5 = Config.Bind<float>(testing, "test 5", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 130, IsAdvanced = true, Browsable = true }));
            PluginConfig.test6 = Config.Bind<float>(testing, "test 6", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 120, IsAdvanced = true, Browsable = true }));
            PluginConfig.test7 = Config.Bind<float>(testing, "test 7", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 110, IsAdvanced = true, Browsable = true }));
            PluginConfig.test8 = Config.Bind<float>(testing, "test 8", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true, Browsable = true }));
            PluginConfig.test9 = Config.Bind<float>(testing, "test 9", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 90, IsAdvanced = true, Browsable = true }));
            PluginConfig.test10 = Config.Bind<float>(testing, "test 10", 1f, new ConfigDescription("", new AcceptableValueRange<float>(-5000f, 5000f), new ConfigurationManagerAttributes { Order = 80, IsAdvanced = true, Browsable = true }));
            PluginConfig.AddZone = Config.Bind(testing, "Create Debug Zone", new KeyboardShortcut(KeyCode.None), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 70, IsAdvanced = true, Browsable = true }));
            PluginConfig.TargetZone = Config.Bind<string>(testing, "TargetZone", "", new ConfigDescription("DebugZone", null, new ConfigurationManagerAttributes { Order = 65, IsAdvanced = true, Browsable = true }));
            PluginConfig.AddEffectType = Config.Bind<string>(testing, "Effect Type", "", new ConfigDescription("HeavyBleeding, LightBleeding, Fracture, removeHP, addHP.", null, new ConfigurationManagerAttributes { Order = 60, IsAdvanced = true, Browsable = true }));
            PluginConfig.AddEffectBodyPart = Config.Bind<int>(testing, "Body Part Index", 1, new ConfigDescription("Head = 0, Chest = 1, Stomach = 2, Letft Arm, Right Arm, Left Leg, Right Leg, Common (whole body)", null, new ConfigurationManagerAttributes { Order = 50, IsAdvanced = true, Browsable = true }));
            PluginConfig.AddEffectKeybind = Config.Bind(testing, "Add Effect Keybind", new KeyboardShortcut(KeyCode.None), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 40, IsAdvanced = true, Browsable = true }));
            PluginConfig.ZoneDebug = Config.Bind<bool>(testing, "Enable Zone Debug", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 30, IsAdvanced = true, Browsable = true }));
            PluginConfig.EnableBallisticsLogging = Config.Bind<bool>(testing, "Enable Ballistics Logging", false, new ConfigDescription("Enables Logging For Debug And Dev", null, new ConfigurationManagerAttributes { Order = 20, IsAdvanced = true, Browsable = true }));
            PluginConfig.EnableLogging = Config.Bind<bool>(testing, "Enable Logging", false, new ConfigDescription("Enables Logging For Debug And Dev", null, new ConfigurationManagerAttributes { Order = 10, IsAdvanced = true, Browsable = true }));

            PluginConfig.RecoilIntensity = Config.Bind<float>(recoilSettings, "Recoil Intensity", 1.3f, new ConfigDescription("Changes The Overall Intenisty Of Recoil. This Will Increase/Decrease Horizontal Recoil, Dispersion, Vertical Recoil. Does Not Affect Recoil Climb Much, Mostly Spread And Visual.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 50, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.VertMulti = Config.Bind<float>(recoilSettings, "Vertical Recoil Multi.", 1.0f, new ConfigDescription("Up/Down. Will Also Increase Recoil Climb.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 40, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.HorzMulti = Config.Bind<float>(recoilSettings, "Horizontal Recoil Multi", 1.0f, new ConfigDescription("Forward/Back. Will Also Increase Weapon Shake While Firing.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 30, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.DispMulti = Config.Bind<float>(recoilSettings, "Dispersion Recoil Multi", 1.0f, new ConfigDescription("Spread. Will Also Increase S-Pattern Size.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 20, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.CamMulti = Config.Bind<float>(recoilSettings, "Camera Recoil Multi", 1.1f, new ConfigDescription("Visual Camera Recoil.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 10, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.EnableAngle = Config.Bind<bool>(recoilSettings, "Enable Recoil Angle", ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Weapons Will Recoil At Different Angles, And Weight Out Front Will Make The Angle More Steep. If Disabled All Recoil Will Be At 90 Degrees.", null, new ConfigurationManagerAttributes { Order = 3, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.RecoilAngleMulti = Config.Bind<float>(recoilSettings, "Recoil Angle Multi", 1.0f, new ConfigDescription("Multiplier For Recoil Angle, Lower = Steeper Angle.", new AcceptableValueRange<float>(0.8f, 1.2f), new ConfigurationManagerAttributes { Order = 2, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ConvergenceMulti = Config.Bind<float>(recoilSettings, "Convergence Multi", 1.0f, new ConfigDescription("AKA Auto-Compensation. Higher = Snappier Recoil, Faster Reset And Tighter Recoil Pattern.", new AcceptableValueRange<float>(0f, 40f), new ConfigurationManagerAttributes { Order = 1, Browsable = ServerConfig.recoil_attachment_overhaul }));

            PluginConfig.AfterRecoilRandomness = Config.Bind<float>(advancedRecoilSettings, "Reset Recoil Randomness Multi", 1f, new ConfigDescription("Higher = More Deviation From Point Of Aim After Firing", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { Order = 140, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.RecoilRandomness = Config.Bind<float>(advancedRecoilSettings, "Recoil Randomness", 2.8f, new ConfigDescription("Higher = Recoil Bounces Around More, More Erratic Recoil Pattern", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { Order = 135, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.CamReturn = Config.Bind<float>(advancedRecoilSettings, "Camera Recoil Speed", 0.07f, new ConfigDescription("Higher = More Faster Camera Recoil", new AcceptableValueRange<float>(0f, 0.5f), new ConfigurationManagerAttributes { Order = 132, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.CamWiggle = Config.Bind<float>(advancedRecoilSettings, "Camera Recoil Wiggle", 0.81f, new ConfigDescription("Higher = More Camera Wiggle", new AcceptableValueRange<float>(0f, 0.9f), new ConfigurationManagerAttributes { Order = 130, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.EnableAdditionalRec = Config.Bind<bool>(advancedRecoilSettings, "Enable Additional Visual Recoil", ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Enables Additonal Visual Recoil Elements. Makes The Weapon Visually Move More In New Directions While Firing, Doesn't Have A Significant Effect On Spread.", null, new ConfigurationManagerAttributes { Order = 120, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.VisRecoilMulti = Config.Bind<float>(advancedRecoilSettings, "Visual Recoil Multi", 1f, new ConfigDescription("Multi For All Of The Mod's Visual Recoil Elements, Makes The Weapon Vibrate More While Firing. Visual Recoil Is Affected By Weapon Stats.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 110, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.EnableHybridRecoil = Config.Bind<bool>(advancedRecoilSettings, "Enable Hybrid Recoil System", false, new ConfigDescription("Combines Steady Recoil Climb With Auto-Compensation. If You Do Not Attempt To Control Recoil, Auto-Compensation Will Decrease Resulting In More Muzzle Flip. If You Control The Recoil, Auto-Comp Increases And Muzzle Flip Decreases.", null, new ConfigurationManagerAttributes { Order = 100, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.HybridForAll = Config.Bind<bool>(advancedRecoilSettings, "Enable Hybrid Recoil For All Weapons", false, new ConfigDescription("By Default This Hybrid System Is Only Enabled For Pistols And Stockless/Folded Stocked Weapons.", null, new ConfigurationManagerAttributes { Order = 90, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.EnableHybridReset = Config.Bind<bool>(advancedRecoilSettings, "Enable Recoil Reset For Hybrid Recoil", false, new ConfigDescription("Enables Recoil Reset For Pistols And Stockless/Folded Stocked Weapons That Are Using Hybrid Recoil, If The Other Reset Options Are Enabled.", null, new ConfigurationManagerAttributes { Order = 90, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.PlayerControlMulti = Config.Bind<float>(advancedRecoilSettings, "Player Control Strength.", 100f, new ConfigDescription("How Quickly The Weapon Responds To Mouse Input If Using The Hybrid Recoil System.", new AcceptableValueRange<float>(0f, 200f), new ConfigurationManagerAttributes { Order = 85, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ResetVertical = Config.Bind<bool>(advancedRecoilSettings, "Enable Vertical Reset", true, new ConfigDescription("Enables Weapon Reseting Back To Original Vertical Position.", null, new ConfigurationManagerAttributes { Order = 80, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ResetHorizontal = Config.Bind<bool>(advancedRecoilSettings, "Enable Horizontal Reset", false, new ConfigDescription("Enables Weapon Reseting Back To Original Horizontal Position.", null, new ConfigurationManagerAttributes { Order = 70, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ResetSpeed = Config.Bind<float>(advancedRecoilSettings, "Reset Speed", 0.0025f, new ConfigDescription("How Fast The Weapon's Vertical Position Resets After Firing. Weapon's Convergence Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 60, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ResetSensitivity = Config.Bind<float>(advancedRecoilSettings, "Reset Sensitvity", 0.14f, new ConfigDescription("The Amount Of Mouse Movement Needed After Firing Needed To Cancel Reseting Back To Weapon's Original Position. Lower = Less Movement Needed.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 50, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.NewPOASensitivity = Config.Bind<float>(advancedRecoilSettings, "Reset Position Shift Sensitvity", 0.5f, new ConfigDescription("Multi For The Amount Of Mouse Movement Needed While Firing To Change The Position To Where Aim Will Reset After Firing. Lower = Less Movement Needed.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 45, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.RecoilSmoothness = Config.Bind<float>(advancedRecoilSettings, "Recoil Smoothness", 0.03f, new ConfigDescription("How Fast Recoil Moves Weapon While Firing, Higher Value Increases Smoothness.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 40, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.RecoilClimbFactor = Config.Bind<float>(advancedRecoilSettings, "Recoil Climb Multi.", 0.3f, new ConfigDescription("Multiplier For How Much Non-Pistols Climbs Vertically Per Shot. Weapon's Vertical Recoil Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 30, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.PistolRecClimbFactor = Config.Bind<float>(advancedRecoilSettings, "Pistol Recoil Climb Multi", 0.03f, new ConfigDescription("Multiplier For How Much Pistols Vertically Per Shot. Weapon's Vertical Recoil Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 29, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.RecoilClimbLimit = Config.Bind<float>(advancedRecoilSettings, "Recoil Climb Limit", 7f, new ConfigDescription("How Far Recoil Can Climb.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { Order = 25, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.RecoilDispersionFactor = Config.Bind<float>(advancedRecoilSettings, "S-Pattern Multi.", 0.06f, new ConfigDescription("Increases The Size The Classic S Pattern. Weapon's Dispersion Stat Increases This.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 20, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.RecoilDispersionSpeed = Config.Bind<float>(advancedRecoilSettings, "S-Pattern Speed Multi", 2f, new ConfigDescription("Increases The Speed At Which Recoil Makes The Classic S Pattern.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 10, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ResetTime = Config.Bind<float>(advancedRecoilSettings, "Reset Delay", 0.14f, new ConfigDescription("The Time In Seconds That Has To Be Elapsed Before Firing Is Considered Over, Recoil Will Not Reset Until It Is Over.", new AcceptableValueRange<float>(0.01f, 0.5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 4, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.EnableCrank = Config.Bind<bool>(advancedRecoilSettings, "Rearward Recoil", ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Makes Recoil Go Towards Player's Shoulder Instead Of Forward.", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.HandsDampingMulti = Config.Bind<float>(advancedRecoilSettings, "Rearward Recoil Wiggle Multi", 1f, new ConfigDescription("The Amount Of Rearward Wiggle After Firing.", new AcceptableValueRange<float>(0.1f, 1.5f), new ConfigurationManagerAttributes { Order = 2, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.RecoilDampingMulti = Config.Bind<float>(advancedRecoilSettings, "Vertical Recoil Wiggle Multi", 1f, new ConfigDescription("The Amount Of Vertical Wiggle After Firing.", new AcceptableValueRange<float>(0.1f, 1.5f), new ConfigurationManagerAttributes { Order = 1, Browsable = ServerConfig.recoil_attachment_overhaul }));

            PluginConfig.EnableMaterialSpeed = Config.Bind<bool>(moveSettings, "Enable Ground Material Speed Modifier", ServerConfig.movement_changes, new ConfigDescription("Enables Movement Speed Being Affected By Ground Material (Concrete, Grass, Metal, Glass Etc.)", null, new ConfigurationManagerAttributes { Order = 20, Browsable = ServerConfig.movement_changes }));
            PluginConfig.EnableSlopeSpeed = Config.Bind<bool>(moveSettings, "Enable Ground Slope Speed Modifier", false, new ConfigDescription("Enables Slopes Slowing Down Movement. Can Cause Random Speed Slowdowns In Some Small Spots Due To BSG's Bad Map Geometry.", null, new ConfigurationManagerAttributes { Order = 10, Browsable = ServerConfig.movement_changes }));

            PluginConfig.DeviceVolume = Config.Bind<float>(zoneSettings, "Device Volume", 0.9f, new ConfigDescription("Volume Modifier For Geiger And Gas Analyser.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 1, Browsable = ServerConfig.enable_hazard_zones }));
            PluginConfig.GasMaskBreathVolume = Config.Bind<float>(zoneSettings, "Gas Mask Breath Volume", 0.8f, new ConfigDescription("Volume Modifier For Gas Mask SFX.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { Order = 2, Browsable = ServerConfig.enable_hazard_zones }));

            PluginConfig.EnableMedNotes = Config.Bind<bool>(healthSettings, "Medical Notifications", ServerConfig.med_changes, new ConfigDescription("Enables Notifications For Medical Status Effects, Healing Etc..", null, new ConfigurationManagerAttributes { Order = 130, Browsable = ServerConfig.med_changes }));
            PluginConfig.ResourceRateChanges = Config.Bind<bool>(healthSettings, "Enable Hydration/Energy Loss Rate Changes", ServerConfig.med_changes, new ConfigDescription("Enables Changes To How Hydration And Energy Loss Rates Are Calculated. They Are Increased By Injuries, Drug Use, Sprinting And Weight.", null, new ConfigurationManagerAttributes { Order = 120, Browsable = ServerConfig.med_changes }));
            PluginConfig.HydrationRateMulti = Config.Bind<float>(healthSettings, "Hydration Drain Rate Multi.", 0.5f, new ConfigDescription("Lower = Less Drain", new AcceptableValueRange<float>(0.1f, 1.5f), new ConfigurationManagerAttributes { Order = 110, Browsable = ServerConfig.med_changes }));
            PluginConfig.EnergyRateMulti = Config.Bind<float>(healthSettings, "Energy Drain Rate Multi.", 0.3f, new ConfigDescription("Lower = Less Drain", new AcceptableValueRange<float>(0.1f, 1.5f), new ConfigurationManagerAttributes { Order = 100, Browsable = ServerConfig.med_changes }));
            PluginConfig.EnableTrnqtEffect = Config.Bind<bool>(healthSettings, "Enable Tourniquet Effect", ServerConfig.med_changes, new ConfigDescription("Tourniquet Will Drain HP Of The Limb They Are Applied To.", null, new ConfigurationManagerAttributes { Order = 90, Browsable = ServerConfig.med_changes }));
            PluginConfig.GearBlocksEat = Config.Bind<bool>(healthSettings, "Gear Blocks Consumption", ServerConfig.med_changes, new ConfigDescription("Gear Blocks Eating & Drinking. This Includes Some Masks & NVGs & Faceshields That Are Toggled On.", null, new ConfigurationManagerAttributes { Order = 80, Browsable = ServerConfig.med_changes }));
            PluginConfig.GearBlocksHeal = Config.Bind<bool>(healthSettings, "Gear Blocks Healing", false, new ConfigDescription("Gear Blocks Use Of Meds If The Wound Is Covered By It.", null, new ConfigurationManagerAttributes { Order = 70, Browsable = ServerConfig.med_changes }));
            PluginConfig.EnableAdrenaline = Config.Bind<bool>(healthSettings, "Adrenaline", ServerConfig.med_changes, new ConfigDescription("If The Player Is Shot or Shot At They Will Get A Painkiller Effect, As Well As Tunnel Vision and Tremors. The Duration And Strength Of These Effects Are Determined By The Stress Resistence Skill.", null, new ConfigurationManagerAttributes { Order = 55, Browsable = ServerConfig.med_changes }));
            PluginConfig.DropGearKeybind = Config.Bind(healthSettings, "Remove Gear Keybind (Double Press)", new KeyboardShortcut(KeyCode.P), new ConfigDescription("Removes Any Gear That Is Blocking The Healing Of A Wound, It's A Double Press Like Bag Keybind Is.", null, new ConfigurationManagerAttributes { Order = 50, Browsable = ServerConfig.med_changes }));

            PluginConfig.EnableFSPatch = Config.Bind<bool>(miscSettings, "Enable Faceshield Patch", ServerConfig.enable_stances, new ConfigDescription("Faceshields Block ADS Unless The Specfic Stock/Weapon/Faceshield Allows It.", null, new ConfigurationManagerAttributes { Order = 4, Browsable = ServerConfig.enable_stances }));
            PluginConfig.EnableNVGPatch = Config.Bind<bool>(miscSettings, "Enable NVG ADS Patch", ServerConfig.enable_stances, new ConfigDescription("Magnified Optics Block ADS When Using NVGs.", null, new ConfigurationManagerAttributes { Order = 5, Browsable = ServerConfig.enable_stances }));
            PluginConfig.EnableMouseSensPenalty = Config.Bind<bool>(miscSettings, "Enable Weight Mouse Sensitivity Penalty", ServerConfig.gear_weight, new ConfigDescription("Instead Of Using Gear Mouse Sens Penalty Stats, It Is Calculated Based On The Gear + Content's Weight As Modified By The Comfort Stat.", null, new ConfigurationManagerAttributes { Order = 20, Browsable = ServerConfig.gear_weight }));
            PluginConfig.EnableZeroShift = Config.Bind<bool>(miscSettings, "Enable Zero Shift", ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Sights Simulate Losing Zero While Firing. The Reticle Has A Chance To Move Off Target. The Chance Is Determined By The Scope And Its Mount's Accuracy Stat, And The Weapon's Recoil. High Quality Scopes And Mounts Won't Lose Zero. SCAR-H Has Worse Zero-Shift.", null, new ConfigurationManagerAttributes { Order = 30, Browsable = ServerConfig.recoil_attachment_overhaul }));

            PluginConfig.DragModifier = Config.Bind<float>(ballSettings, "Ballistic Coefficient Modifier", 1.25f, new ConfigDescription("Determines The Amount Of Drag On Projectiles. Higher Value = Slower Flight Time And More Drop.", new AcceptableValueRange<float>(0.5f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 150, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.GlobalDamageModifier = Config.Bind<float>(ballSettings, "Global Damage Modifier", 1f, new ConfigDescription("Lower = Less Damage Received (Except Head) For Bots And Player.", new AcceptableValueRange<float>(0.1f, 2f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 140, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.EnablePlateChanges = Config.Bind<bool>(ballSettings, "Enable Armor Plate Hitbox Changes", ServerConfig.realistic_ballistics, new ConfigDescription("Reduces The Size Of Armor Plate Hitboxes To Be Closer To Real Life, And Closer To How They Were When First Implemented.", null, new ConfigurationManagerAttributes { Order = 130, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.EnableBodyHitZones = Config.Bind<bool>(ballSettings, "Enable Body Hit Zones", ServerConfig.realistic_ballistics, new ConfigDescription("Divides Body Into A, C and D Hit Zones Like On IPSC Targets. In Addtion, There Are Upper Arm, Forearm, Thigh, Calf, Neck, Spine And Heart Hit Zones. Each Zone Modifies Damage And Bleed Chance. ", null, new ConfigurationManagerAttributes { Order = 120, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.EnableHitSounds = Config.Bind<bool>(ballSettings, "Enable Hit Sounds", ServerConfig.realistic_ballistics, new ConfigDescription("Enables Additional Sounds To Be Played When Hitting The New Body Zones And Armor Hit Sounds By Material.", null, new ConfigurationManagerAttributes { Order = 110, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.FleshHitSoundMulti = Config.Bind<float>(ballSettings, "Flesh Hit Sound Multi", 1f, new ConfigDescription("Raises/Lowers New Hit Sounds Volume.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 100, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.ArmorCloseHitSoundMulti = Config.Bind<float>(ballSettings, "Close Armor Hit Sound Multi", 1f, new ConfigDescription("Raises/Lowers New Hit Sounds Volume.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 90, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.ArmorFarHitSoundMulti = Config.Bind<float>(ballSettings, "Distant Armor Hit Sound Mutli", 1f, new ConfigDescription("Raises/Lowers New Hit Sounds Volume.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 80, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.EnableRagdollFix = Config.Bind<bool>(ballSettings, "Enable Ragdoll Fix (Experimental)", false, new ConfigDescription("Requiures Restart. Enables Fix For Ragdolls Flying Into The Stratosphere.", null, new ConfigurationManagerAttributes { Order = 70, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.RagdollForceModifier = Config.Bind<float>(ballSettings, "Ragdoll Force Modifier", 1f, new ConfigDescription("Requires Ragdoll Fix To Be Enabled.", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { IsAdvanced = true, Order = 60, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.DisarmBaseChance = Config.Bind<float>(ballSettings, "Disarm Base Chance.", 1f, new ConfigDescription("The Base Chance To Be Disarmed. 1 = 1% Chance. This Value Is Increased By The Bullet's Kinetic Energy, Reduced By Armor Armor If Hit, And Doubled If Forearm Is Hit.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, IsAdvanced = true, Order = 50, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.FallBaseChance = Config.Bind<float>(ballSettings, "Fall Base Chance", 20f, new ConfigDescription("The Base Chance To Toggle Prone If Shot In Leg. 1 = 1% Chance. This Value Is Increased By The Bullet's Kinetic Energy And Doubled If Calf Is Hit.", new AcceptableValueRange<float>(0f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, IsAdvanced = true, Order = 40, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.CanFellBot = Config.Bind<bool>(ballSettings, "Enable Bot Knockdown", ServerConfig.realistic_ballistics, new ConfigDescription("If Hit In The Leg And The Leg Has/Will Have 0 HP, There Is A Chance That Prone Will Be Toggled. Chance Is Modified By Bullet Kinetic EnergyAnd Doubled If Calf Is Hit.", null, new ConfigurationManagerAttributes { Order = 30, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.CanFellPlayer = Config.Bind<bool>(ballSettings, "Enable Player Knockdown", ServerConfig.realistic_ballistics, new ConfigDescription("If Hit In The Leg And The Leg Has/Will Have 0 HP, There Is A Chance That Prone Will Be Toggled. Chance Is Modified By Bullet Kinetic Energy And Doubled If Calf Is Hit.", null, new ConfigurationManagerAttributes { Order = 20, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.CanDisarmBot = Config.Bind<bool>(ballSettings, "Can Disarm Bot.", false, new ConfigDescription("If Hit In The Arms, There Is A Chance That The Currently Equipped Weapon Will Be Dropped. Chance Is Modified By Bullet Kinetic Energy And Reduced If Hit Arm Armor, And Doubled If Forearm Is Hit. WARNING: Disarmed Bots Will Become Passive And Not Attack Player, So This Is Disabled By Default.", null, new ConfigurationManagerAttributes { Order = 10, Browsable = ServerConfig.realistic_ballistics }));
            PluginConfig.CanDisarmPlayer = Config.Bind<bool>(ballSettings, "Can Disarm Player", ServerConfig.realistic_ballistics, new ConfigDescription("If Hit In The Arms, There Is A Chance That The Currently Equipped Weapon Will Be Dropped. Chance Is Modified By Bullet Kinetic Energy And Reduced If Hit Arm Armor, And Doubled If Forearm Is Hit.", null, new ConfigurationManagerAttributes { Order = 1, Browsable = ServerConfig.realistic_ballistics }));

            PluginConfig.EnableAmmoStats = Config.Bind<bool>(statSettings, "Display Ammo Stats", ServerConfig.realistic_ballistics, new ConfigDescription("Requiures Restart.", null, new ConfigurationManagerAttributes { Order = 11, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ShowBalance = Config.Bind<bool>(statSettings, "Show Balance Stat", ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 5, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ShowCamRecoil = Config.Bind<bool>(statSettings, "Show Camera Recoil Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 4, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ShowDispersion = Config.Bind<bool>(statSettings, "Show Dispersion Stat", false, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 3, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ShowRecoilAngle = Config.Bind<bool>(statSettings, "Show Recoil Angle Stat", ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use..", null, new ConfigurationManagerAttributes { Order = 2, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ShowSemiROF = Config.Bind<bool>(statSettings, "Show Semi Auto ROF Stat", ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Requiures Restart. Warning: Showing Too Many Stats On Weapons With Lots Of Slots Makes The Inspect Menu UI Difficult To Use.", null, new ConfigurationManagerAttributes { Order = 1, Browsable = ServerConfig.recoil_attachment_overhaul }));

            PluginConfig.EnableMuzzleEffects = Config.Bind<bool>(waponSettings, "Enable Muzzle Effects.", ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Enanbes Changes To Muzzle Flash, Smoke, Etc. And Makes Their Intensity Dependent On Caliber, Weapon Condition, Attachments Etc.", null, new ConfigurationManagerAttributes { Order = 40, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.SwayIntensity = Config.Bind<float>(waponSettings, "Sway Intensity.", 1.1f, new ConfigDescription("Changes The Intensity Of Aim Sway.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { Order = 30, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.ProceduralIntensity = Config.Bind<float>(waponSettings, "Procedural Intensity.", 1.05f, new ConfigDescription("Changes The Intensity Of Procedural Animations, Including Sway, Weapon Movement, And Weapon Inertia.", new AcceptableValueRange<float>(0f, 3f), new ConfigurationManagerAttributes { Order = 20, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.DuraMalfThreshold = Config.Bind<float>(waponSettings, "Malfunction Durability Threshold", 98f, new ConfigDescription("Malfunction Changes Must Be Enabled On The Server (Config App) And 'Enable Malfunctions Changes' Must Be True. Malfunction Chance Is Significantly Reduced Until This Durability Threshold Is Exceeded.", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { Order = 10, Browsable = ServerConfig.malf_changes }));
            PluginConfig.IncreaseCOI = Config.Bind<bool>(waponSettings, "Enable Increased Inaccuracy", ServerConfig.recoil_attachment_overhaul, new ConfigDescription("Requires Restart. Increases The Innacuracy Of All Weapons So That MOA/Accuracy Is A More Important Stat.", null, new ConfigurationManagerAttributes { Order = 1, Browsable = ServerConfig.recoil_attachment_overhaul }));

            PluginConfig.DryVolumeMulti = Config.Bind<float>(deafSettings, "Headset Base Volume Reduction Multi", 1f, new ConfigDescription("Multi For How Much Headsets Reduce Audio Volume By, Not Including Gain", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 100, IsAdvanced = false, Browsable = ServerConfig.headset_changes }));
            PluginConfig.HeadsetThreshold = Config.Bind<float>(deafSettings, "Headset Cutoff Threshold Offset", -5f, new ConfigDescription("Threshold For How Loud Something Has To Be To Reduce Volume. Offset reduces or increases value. Lower Offset = More Sensitive. Offset Value of -5 Will Make It More Sensitive, A Value Of 5 Less.", new AcceptableValueRange<float>(-35f, -1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 90, IsAdvanced = false, Browsable = ServerConfig.headset_changes }));
            PluginConfig.HeadsetAttack = Config.Bind<float>(deafSettings, "Headset Attack", 1f, new ConfigDescription("How Quickly The Headset Will Start Reducing Volume. Lower = Faster.", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 80, IsAdvanced = false, Browsable = ServerConfig.headset_changes }));
            PluginConfig.HeadsetAmbientMulti = Config.Bind<float>(deafSettings, "Headset Ambient Modifier", 5f, new ConfigDescription("Adjusts The Ambient Volume Reduction From Headsets. Headset Gain Also Affects Ambient Volume. Higher = Louder.", new AcceptableValueRange<float>(-20f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 70, Browsable = ServerConfig.headset_changes }));
            PluginConfig.SharedMovementVolume = Config.Bind<float>(deafSettings, "Shared Movement Volume Multi", 1f, new ConfigDescription("Multiplier For Player + NPC Sprint Volume. Has To Be Shared Due To BSG Jank.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 60, IsAdvanced = false, Browsable = ServerConfig.headset_changes }));
            PluginConfig.NPCMovementVolume = Config.Bind<float>(deafSettings, "NPC Movement Volume Multi", 1f, new ConfigDescription("Multiplier For NPC Movement Volume. Includes Walking And Equipment Rattle.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 50, IsAdvanced = false, Browsable = ServerConfig.headset_changes }));
            PluginConfig.PlayerMovementVolume = Config.Bind<float>(deafSettings, "Player Movement Volume Multi", 1f, new ConfigDescription("Multiplier For Player Movment Volume.  Includes Walking And Equipment Rattle.", new AcceptableValueRange<float>(0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 40, IsAdvanced = false, Browsable = ServerConfig.headset_changes }));
            PluginConfig.ADSVolume = Config.Bind<float>(deafSettings, "ADS Volume Multi", 1f, new ConfigDescription("ADS Volume.", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = false, Browsable = ServerConfig.headset_changes }));
            PluginConfig.GunshotVolume = Config.Bind<float>(deafSettings, "Gunshot Volume", -5f, new ConfigDescription("Offset For Volume Of Gunshots When Not Using Headsets. Lower = Quieter. Use Gain Cutoff For Headsets", new AcceptableValueRange<float>(-50f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = false, Browsable = ServerConfig.headset_changes }));
            PluginConfig.RealTimeGain = Config.Bind<float>(deafSettings, "Headset Gain", 9f, new ConfigDescription("WARNING: BE CAREFUL INCREASING THIS TOO HIGH! IT MAY DAMAGE YOUR HEARING! Adjusts The Gain Of Equipped Headsets In Real Time, Acts Just Like The Volume Control On IRL Ear Defenders.", new AcceptableValueRange<float>(0f, 30f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, Browsable = ServerConfig.headset_changes }));
            PluginConfig.GainCutoff = Config.Bind<float>(deafSettings, "Headset Gain Cutoff Multi", 0.75f, new ConfigDescription("How Much Headset Gain Is Reduced By While Firing. 0.75 = 25% Reduction.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, Browsable = ServerConfig.headset_changes }));
            PluginConfig.DeafenResetDelay = Config.Bind<float>(deafSettings, "Deafen Reset Delay", 0.5f, new ConfigDescription("How Long It Takes For Headset Gain To Be Restored Or Deafening Effects To Start Reseting", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, Browsable = ServerConfig.headset_changes }));
            PluginConfig.DecGain = Config.Bind(deafSettings, "Reduce Gain Keybind", new KeyboardShortcut(KeyCode.KeypadMinus), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 9, Browsable = ServerConfig.headset_changes }));
            PluginConfig.IncGain = Config.Bind(deafSettings, "Increase Gain Keybind", new KeyboardShortcut(KeyCode.KeypadPlus), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 8, Browsable = ServerConfig.headset_changes }));
            PluginConfig.DeafRate = Config.Bind<float>(deafSettings, "Deafen Rate", 0.008f, new ConfigDescription("How Quickly Player Gets Deafened. Higher = Faster.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7, IsAdvanced = true, Browsable = ServerConfig.headset_changes }));
            PluginConfig.DeafReset = Config.Bind<float>(deafSettings, "Deafen Reset Rate.", 0.065f, new ConfigDescription("How Quickly Player Regains Hearing. Higher = Faster.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true, Browsable = ServerConfig.headset_changes }));
            PluginConfig.VigRate = Config.Bind<float>(deafSettings, "Tunnel Effect Rate", 0.02f, new ConfigDescription("How Quickly Player Gets Tunnel Vission. Higher = Faster", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true, Browsable = ServerConfig.headset_changes }));
            PluginConfig.VigReset = Config.Bind<float>(deafSettings, "Tunnel Effect Reset Rate.", 0.035f, new ConfigDescription("How Quickly Player Recovers From Tunnel Vision. Higher = Faster", new AcceptableValueRange<float>(0f, 2f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true, Browsable = ServerConfig.headset_changes }));

            PluginConfig.PistolGlobalAimSpeedModifier = Config.Bind<float>(speed, "Pistol Aim Speed Multi.", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 17, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalAimSpeedModifier = Config.Bind<float>(speed, "Aim Speed Multi.", 1.4f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 16, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalReloadSpeedMulti = Config.Bind<float>(speed, "Magazine Reload Speed Multi", 1.125f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 15, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalFixSpeedMulti = Config.Bind<float>(speed, "Malfunction Fix Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 14, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalUBGLReloadMulti = Config.Bind<float>(speed, "UBGL Reload Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 13, IsAdvanced = true, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.RechamberPistolSpeedMulti = Config.Bind<float>(speed, "Pistol Rechamber Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalRechamberSpeedMulti = Config.Bind<float>(speed, "Rechamber Speed Multi", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalBoltSpeedMulti = Config.Bind<float>(speed, "Bolt Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalShotgunRackSpeedFactor = Config.Bind<float>(speed, "Shotgun Rack Speed Multi", 1.0f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 9, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalCheckChamberSpeedMulti = Config.Bind<float>(speed, "Chamber Check Speed Multi", 1.25f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 8, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalCheckChamberShotgunSpeedMulti = Config.Bind<float>(speed, "Shotgun Chamber Check Speed Multi", 1.25f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7, IsAdvanced = true, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalCheckChamberPistolSpeedMulti = Config.Bind<float>(speed, "Pistol Chamber Check Speed Multi", 1.25f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalCheckAmmoPistolSpeedMulti = Config.Bind<float>(speed, "Pistol Check Ammo Multi", 1.25f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.GlobalCheckAmmoMulti = Config.Bind<float>(speed, "Check Ammo Multi.", 1.3f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.QuickReloadSpeedMulti = Config.Bind<float>(speed, "Quick Reload Multi", 1.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, Browsable = ServerConfig.recoil_attachment_overhaul }));
            PluginConfig.InternalMagReloadMulti = Config.Bind<float>(speed, "Internal Magazine Reload", 1.15f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 10.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, Browsable = ServerConfig.recoil_attachment_overhaul }));

            PluginConfig.RememberStance = Config.Bind<bool>(weapAimAndPos, "Remember Stance", true, new ConfigDescription("Remember Stance After Actions (Using Items).", null, new ConfigurationManagerAttributes { Order = 260, Browsable = ServerConfig.enable_stances }));
            PluginConfig.BlockFiring = Config.Bind<bool>(weapAimAndPos, "Block Shooting While In Stance", false, new ConfigDescription("Blocks Firing While In A Stance, Will Cancel Stance If Attempting To Fire.", null, new ConfigurationManagerAttributes { Order = 250, Browsable = ServerConfig.enable_stances }));
            PluginConfig.EnableSprintPenalty = Config.Bind<bool>(weapAimAndPos, "Enable Sprint Aim Penalties", ServerConfig.movement_changes, new ConfigDescription("ADS Out Of Sprint Has A Short Delay, Reduced Aim Speed And Increased Sway. The Longer You Sprint The Bigger The Penalty.", null, new ConfigurationManagerAttributes { Order = 240, Browsable = ServerConfig.enable_stances }));
            PluginConfig.EnableTacSprint = Config.Bind<bool>(weapAimAndPos, "Enable High Ready Sprint Animation", ServerConfig.enable_stances, new ConfigDescription("Enables Usage Of High Ready Sprint Animation When Sprinting From High Ready Position.", null, new ConfigurationManagerAttributes { Order = 230, Browsable = ServerConfig.enable_stances }));
            PluginConfig.EnableAltPistol = Config.Bind<bool>(weapAimAndPos, "Enable Alternative Pistol Position And ADS", ServerConfig.enable_stances, new ConfigDescription("Pistol Will Be Held Centered And In A Compressed Stance. ADS Will Be Animated.", null, new ConfigurationManagerAttributes { Order = 229, Browsable = ServerConfig.enable_stances }));
            PluginConfig.EnableAltRifle = Config.Bind<bool>(weapAimAndPos, "Enable Alternative Rifle Position And ADS (WIP)", false, new ConfigDescription("Rfile Will Move Closer To Camera When Aiming, Leading To Smoother ADS From Stances. Also Standardizes All Rifle Positions. Ignores 'Rifle Position' Settings.", null, new ConfigurationManagerAttributes { Order = 220, Browsable = ServerConfig.enable_stances }));
            PluginConfig.EnableIdleStamDrain = Config.Bind<bool>(weapAimAndPos, "Enable Idle Arm Stamina Drain", ServerConfig.enable_stances, new ConfigDescription("Arm Stamina Will Drain When Not In A Stance (High And Low Ready, Short-Stocking).", null, new ConfigurationManagerAttributes { Order = 210, Browsable = ServerConfig.enable_stances }));
            PluginConfig.EnableStanceStamChanges = Config.Bind<bool>(weapAimAndPos, "Enable Stance Stamina And Movement Effects", ServerConfig.enable_stances, new ConfigDescription("Enabled Stances And Mounting To Affect Stamina And Movement Speed. Stamina Drain May Not Work Correctly If Disabled. High + Low Ready, Short-Stocking And Pistol Idle Will Regenerate Stamina Faster And Optionally Idle With Rifles Drains Stamina. High Ready Has Faster Sprint Speed And Sprint Accel, Low Ready Has Faster Sprint Accel. Arm Stamina Won't Drain Regular Stamina If It Reaches 0.", null, new ConfigurationManagerAttributes { Order = 183, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimReload = Config.Bind<bool>(weapAimAndPos, "Allow Reload From Active Aim", false, new ConfigDescription("Allows Reload From Magazine While In Active Aim With Speed Bonus.", null, new ConfigurationManagerAttributes { Order = 190, Browsable = ServerConfig.enable_stances }));
            PluginConfig.EnableMountUI = Config.Bind<bool>(weapAimAndPos, "Enable Mounting UI", ServerConfig.enable_stances, new ConfigDescription("If Enabled, An Icon On Screen Will Indicate If Player Is Bracing, Mounting And What Side Of Cover They Are On.", null, new ConfigurationManagerAttributes { Order = 179, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LeftShoulderOffset = Config.Bind<float>(weapAimAndPos, "Left Shoulder Offset", -0.13f, new ConfigDescription("", new AcceptableValueRange<float>(-0.2f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 153, Browsable = ServerConfig.enable_stances }));
            PluginConfig.WeapOffsetX = Config.Bind<float>(weapAimAndPos, "Rifle Position X-Axis", -0.04f, new ConfigDescription("Adjusts The Starting Position Of Rifle On Screen If Alt Rifle Is Disabled", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 152, Browsable = ServerConfig.enable_stances }));
            PluginConfig.WeapOffsetY = Config.Bind<float>(weapAimAndPos, "Rifle Position Y-Axis", -0.015f, new ConfigDescription("Adjusts The Starting Position Of Rifle On Screen If Alt Rifle Is Disabled", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 151, Browsable = ServerConfig.enable_stances }));
            PluginConfig.WeapOffsetZ = Config.Bind<float>(weapAimAndPos, "Rifle Position Z-Axis", 0f, new ConfigDescription("Adjusts The Starting Position Of Rifle On Screen If Alt Rifle Is Disabled", new AcceptableValueRange<float>(-0.1f, 0.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 150, Browsable = ServerConfig.enable_stances }));
            PluginConfig.StanceRotationSpeedMulti = Config.Bind<float>(weapAimAndPos, "Stance Rotation Speed Multi", 1f, new ConfigDescription("Adjusts The Speed Of Stance Rotation Changes.", new AcceptableValueRange<float>(0.1f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 146, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.StanceTransitionSpeedMulti = Config.Bind<float>(weapAimAndPos, "Stance Transition Speed.", 15.0f, new ConfigDescription("Adjusts The Position Change Speed Between Stances", new AcceptableValueRange<float>(1f, 35f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.CycleStancesKeybind = Config.Bind(stanceBinds, "Cycle Stances Keybind", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Cycles Between High, Low Ready and Short-Stocking. Double Click Returns To Idle.", null, new ConfigurationManagerAttributes { Order = 80, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimKeybind = Config.Bind(stanceBinds, "Active Aim Keybind", new KeyboardShortcut(KeyCode.Mouse4), new ConfigDescription("Cants The Weapon Sideways, Improving Hipfire Accuracy.", null, new ConfigurationManagerAttributes { Order = 90, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ToggleActiveAim = Config.Bind<bool>(stanceBinds, "Use Toggle For Active Aim", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 100, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyKeybind = Config.Bind(stanceBinds, "High Ready Keybind", new KeyboardShortcut(KeyCode.Mouse3, new[] { KeyCode.LeftAlt }), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 110, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyKeybind = Config.Bind(stanceBinds, "Low Ready Keybind", new KeyboardShortcut(KeyCode.Mouse3, new[] { KeyCode.LeftControl }), new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 120, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockKeybind = Config.Bind(stanceBinds, "Short-Stock Keybind", new KeyboardShortcut(KeyCode.J), new ConfigDescription("Tucks The Weapon's Stock Under Player's Arm, Shortening The Overall Length Of The Wweapon To Prevent Muzzle Being Pushed Away From Target.", null, new ConfigurationManagerAttributes { Order = 130, Browsable = ServerConfig.enable_stances }));
            PluginConfig.MountKeybind = Config.Bind(stanceBinds, "Mounting Keybind", new KeyboardShortcut(KeyCode.M), new ConfigDescription("Snaps To Cover To Improve Weapon Stability And Recoil, Toggle Only.", null, new ConfigurationManagerAttributes { Order = 140, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PatrolKeybind = Config.Bind(stanceBinds, "Patrol/Neutral Stance Keybind", new KeyboardShortcut(KeyCode.K), new ConfigDescription("Puts The Weapon In A Neutral Position, Improving Arm Stam Regen And Walk Speed. For Maximum Larping.", null, new ConfigurationManagerAttributes { Order = 155, Browsable = ServerConfig.enable_stances }));
            PluginConfig.MeleeKeybind = Config.Bind(stanceBinds, "Melee Keybind", new KeyboardShortcut(KeyCode.None), new ConfigDescription("Strike With Muzzle Or Bayonet Of Equipped Weapon.", null, new ConfigurationManagerAttributes { Order = 150, Browsable = ServerConfig.enable_stances }));
            PluginConfig.UseMouseWheelStance = Config.Bind<bool>(stanceBinds, "Enable Mouse Wheel Stance Switching", ServerConfig.enable_stances, new ConfigDescription("Switches Between High And Low Ready Via Mouse Wheel.", null, new ConfigurationManagerAttributes { Order = 160, Browsable = ServerConfig.enable_stances }));
            PluginConfig.UseMouseWheelPlusKey = Config.Bind<bool>(stanceBinds, "Require Key + Mouse Wheel", ServerConfig.enable_stances, new ConfigDescription("Require Keybind + Mouse Wheel To Change Stance.", null, new ConfigurationManagerAttributes { Order = 170, Browsable = ServerConfig.enable_stances }));
            PluginConfig.StanceWheelComboKeyBind = Config.Bind(stanceBinds, "Keybind To Use With Mouse Wheel", new KeyboardShortcut(KeyCode.LeftControl), new ConfigDescription("Key Used In Combination With Mouse Wheel If Enabled ", null, new ConfigurationManagerAttributes { Order = 180, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ThirdPersonRotationSpeed = Config.Bind<float>(thirdPerson, "Third Person Rotation Speed Multi", 1.5f, new ConfigDescription("Speed Of Stance Rotation Change In Third Person.", new AcceptableValueRange<float>(0.1f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1000, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ThirdPersonPositionSpeed = Config.Bind<float>(thirdPerson, "Third Person Position Speed Multi", 1.0f, new ConfigDescription("Speed Of Stance Position Change In Third Person.", new AcceptableValueRange<float>(0.1f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1100, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.PistolThirdPersonPositionX = Config.Bind<float>(thirdPerson, "Pistol Third Person Position X-Axis", -0.03f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 260, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolThirdPersonPositionY = Config.Bind<float>(thirdPerson, "Pistol Third Person Position Y-Axis", 0.04f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 250, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolThirdPersonPositionZ = Config.Bind<float>(thirdPerson, "Pistol Third Person Position Z-Axis", -0.05f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 240, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolThirdPersonRotationX = Config.Bind<float>(thirdPerson, "Pistol Third Person Rotation X-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 230, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolThirdPersonRotationY = Config.Bind<float>(thirdPerson, "Pistol Third Person Rotation Y-Axis", -15f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 220, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolThirdPersonRotationZ = Config.Bind<float>(thirdPerson, "Pistol Third Person Rotation Z-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 210, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ShortStockThirdPersonPositionX = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Position X-Axis", 0.03f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 200, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockThirdPersonPositionY = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Position Y-Axis", 0.065f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 190, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockThirdPersonPositionZ = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Position Z-Axis", -0.075f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 180, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockThirdPersonRotationX = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Rotation X-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 170, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockThirdPersonRotationY = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Rotation Y-Axis", -15f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 160, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockThirdPersonRotationZ = Config.Bind<float>(thirdPerson, "Short-Stock Third Person Rotation Z-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 150, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ActiveThirdPersonPositionX = Config.Bind<float>(thirdPerson, "Active Aim Third Person Position X-Axis", -0.02f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 140, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveThirdPersonPositionY = Config.Bind<float>(thirdPerson, "Active Aim Third Person Position Y-Axis", -0.02f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 130, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveThirdPersonPositionZ = Config.Bind<float>(thirdPerson, "Active Aim Third Person Position Z-Axis", 0.02f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 120, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveThirdPersonRotationX = Config.Bind<float>(thirdPerson, "Active Aim Third Person Rotation X-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveThirdPersonRotationY = Config.Bind<float>(thirdPerson, "Active Aim Third Person Rotation Y-Axis", -35f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 100, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveThirdPersonRotationZ = Config.Bind<float>(thirdPerson, "Active Aim Third Person Rotation Z-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 90, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.HighReadyThirdPersonPositionX = Config.Bind<float>(thirdPerson, "High Ready Third Person Position X-Axis",  0.02f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 80, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyThirdPersonPositionY = Config.Bind<float>(thirdPerson, "High Ready Third Person Position Y-Axis", 0.05f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 70, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyThirdPersonPositionZ = Config.Bind<float>(thirdPerson, "High Ready Third Person Position Z-Axis",  -0.045f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 60, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyThirdPersonRotationX = Config.Bind<float>(thirdPerson, "High Ready Third Person Rotation X-Axis", -8f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 50, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyThirdPersonRotationY = Config.Bind<float>(thirdPerson, "High Ready Third Person Rotation Y-Axis", -25f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 40, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyThirdPersonRotationZ = Config.Bind<float>(thirdPerson, "High Ready Third Person Rotation Z-Axis", -0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.LowReadyThirdPersonPositionX = Config.Bind<float>(thirdPerson, "Low Ready Third Person Position X-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 20, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyThirdPersonPositionY = Config.Bind<float>(thirdPerson, "Low Ready Third Person Position Y-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyThirdPersonPositionZ = Config.Bind<float>(thirdPerson, "Low Ready Third Person Position Z-Axis", 0f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 9, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyThirdPersonRotationX = Config.Bind<float>(thirdPerson, "Low Ready Third Person Rotation X-Axis", 24f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 8, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyThirdPersonRotationY = Config.Bind<float>(thirdPerson, "Low Ready Third Person Rotation Y-Axis", 10f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 7, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyThirdPersonRotationZ = Config.Bind<float>(thirdPerson, "Low Ready Third Person Rotation Z-Axis", -1f, new ConfigDescription("", new AcceptableValueRange<float>(-1000, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ActiveAimAdditionalRotationSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Additonal Rotation Speed Multi.", 2.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimResetRotationSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Reset Rotation Speed Multi.", 4.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 145, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimRotationMulti = Config.Bind<float>(activeAim, "Active Aim Rotation Speed Multi.", 2f, new ConfigDescription("", new AcceptableValueRange<float>(0.0f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 144, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Speed Multi", 15f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 143, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimResetSpeedMulti = Config.Bind<float>(activeAim, "Active Aim Reset Speed Multi", 11.5f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 142, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ActiveAimOffsetX = Config.Bind<float>(activeAim, "Active Aim Position X-Axis", -0.02f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 135, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimOffsetY = Config.Bind<float>(activeAim, "Active Aim Position Y-Axis", 0.008f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 134, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimOffsetZ = Config.Bind<float>(activeAim, "Active Aim Position Z-Axis", -0.008f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 133, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ActiveAimRotationX = Config.Bind<float>(activeAim, "Active Aim Rotation X-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 122, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimRotationY = Config.Bind<float>(activeAim, "Active Aim Rotation Y-Axis", -35.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 121, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimRotationZ = Config.Bind<float>(activeAim, "Active Aim Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 120, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ActiveAimAdditionalRotationX = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation X-Axis", 0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 111, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimAdditionalRotationY = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation Y-Axis", -35f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimAdditionalRotationZ = Config.Bind<float>(activeAim, "Active Aiming Additional Rotation Z-Axis", 0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 110, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ActiveAimResetRotationX = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation X-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 102, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimResetRotationY = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation Y-Axis.", 20.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 101, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ActiveAimResetRotationZ = Config.Bind<float>(activeAim, "Active Aiming Reset Rotation Z-Axis", -0.25f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 100, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.HighReadyAdditionalRotationSpeedMulti = Config.Bind<float>(highReady, "High Ready Additonal Rotation Speed Multi.", 1.5f, new ConfigDescription("How Fast The Weapon Rotates Going Out Of Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 94, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyResetRotationMulti = Config.Bind<float>(highReady, "High Ready Reset Rotation Speed Multi.", 2f, new ConfigDescription("How Fast The Weapon Rotates Going Out Of Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 93, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyRotationMulti = Config.Bind<float>(highReady, "High Ready Rotation Speed Multi.", 3f, new ConfigDescription("How Fast The Weapon Rotates Going Into Stance.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 92, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyResetSpeedMulti = Config.Bind<float>(highReady, "High Ready Reset Speed Multi", 13f, new ConfigDescription("How Fast The Weapon Moves Going Out Of Stance", new AcceptableValueRange<float>(1f, 100.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 91, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadySpeedMulti = Config.Bind<float>(highReady, "High Ready Speed Multi", 10.5f, new ConfigDescription("How Fast The Weapon Moves Going Into Stance", new AcceptableValueRange<float>(1f, 100.1f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 90, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.HighReadyOffsetX = Config.Bind<float>(highReady, "High Ready Position X-Axis", 0.005f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 85, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyOffsetY = Config.Bind<float>(highReady, "High Ready Position Y-Axis", 0.035f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 84, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyOffsetZ = Config.Bind<float>(highReady, "High Ready Position Z-Axis", -0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 83, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.HighReadyRotationX = Config.Bind<float>(highReady, "High Ready Rotation X-Axis", -8.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 72, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyRotationY = Config.Bind<float>(highReady, "High Ready Rotation Y-Axis", -20.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 71, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyRotationZ = Config.Bind<float>(highReady, "High Ready Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 70, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.HighReadyAdditionalRotationX = Config.Bind<float>(highReady, "High Ready Additional Rotation X-Axis", -5.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 69, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyAdditionalRotationY = Config.Bind<float>(highReady, "High Ready Additiona Rotation Y-Axis", -5f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 68, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyAdditionalRotationZ = Config.Bind<float>(highReady, "High Ready Additional Rotation Z-Axis", -1f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 67, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.HighReadyResetRotationX = Config.Bind<float>(highReady, "High Ready Reset Rotation X-Axis", -0.4f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 66, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyResetRotationY = Config.Bind<float>(highReady, "High Ready Reset Rotation Y-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 65, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.HighReadyResetRotationZ = Config.Bind<float>(highReady, "High Ready Reset Rotation Z-Axis", 0.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 64, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.LowReadyAdditionalRotationSpeedMulti = Config.Bind<float>(lowReady, "Low Ready Additonal Rotation Speed Multi", 1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 64, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyResetRotationMulti = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Speed Multi", 2.7f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 63, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyRotationMulti = Config.Bind<float>(lowReady, "Low Ready Rotation Speed Multi", 2f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 62, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadySpeedMulti = Config.Bind<float>(lowReady, "Low Ready Speed Multi.", 14f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 61, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyResetSpeedMulti = Config.Bind<float>(lowReady, "Low Ready Reset Speed Multi", 8.5f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 60, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.LowReadyOffsetX = Config.Bind<float>(lowReady, "Low Ready Position X-Axis", 0f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 55, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyOffsetY = Config.Bind<float>(lowReady, "Low Ready Position Y-Axis", -0.01f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 54, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyOffsetZ = Config.Bind<float>(lowReady, "Low Ready Position Z-Axis", 0.0f, new ConfigDescription("Weapon Position When In Stance..", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 53, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.LowReadyRotationX = Config.Bind<float>(lowReady, "Low Ready Rotation X-Axis", 8f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 42, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyRotationY = Config.Bind<float>(lowReady, "Low Ready Rotation Y-Axis", -5.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 41, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyRotationZ = Config.Bind<float>(lowReady, "Low Ready Rotation Z-Axis", -1.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 40, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.LowReadyAdditionalRotationX = Config.Bind<float>(lowReady, "Low Ready Additional Rotation X-Axis", 12.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 39, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyAdditionalRotationY = Config.Bind<float>(lowReady, "Low Ready Additional Rotation Y-Axis", -1f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 38, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyAdditionalRotationZ = Config.Bind<float>(lowReady, "Low Ready Additional Rotation Z-Axis", 0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 37, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.LowReadyResetRotationX = Config.Bind<float>(lowReady, "Low Ready Reset Rotation X-Axis", -1.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 36, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyResetRotationY = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Y-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.LowReadyResetRotationZ = Config.Bind<float>(lowReady, "Low Ready Reset Rotation Z-Axis", 0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.PistolAdditionalRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Additional Rotation Speed Multi", 0.1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolResetRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Reset Rotation Speed Multi", 2f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolRotationSpeedMulti = Config.Bind<float>(pistol, "Pistol Rotation Speed Multi", 1f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.0f, 20f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 33, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolPosSpeedMulti = Config.Bind<float>(pistol, "Pistol Position Speed Multi", 6.0f, new ConfigDescription("", new AcceptableValueRange<float>(1.0f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 32, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolPosResetSpeedMulti = Config.Bind<float>(pistol, "Pistol Position Reset Speed Multi", 16.0f, new ConfigDescription("", new AcceptableValueRange<float>(1.0f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.PistolOffsetX = Config.Bind<float>(pistol, "Pistol Position X-Axis.", 0f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 25, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolOffsetY = Config.Bind<float>(pistol, "Pistol Position Y-Axis.", 0.04f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 24, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolOffsetZ = Config.Bind<float>(pistol, "Pistol Position Z-Axis.", -0.015f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 23, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.PistolRotationX = Config.Bind<float>(pistol, "Pistol Rotation X-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolRotationY = Config.Bind<float>(pistol, "Pistol Rotation Y-Axis", -5f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolRotationZ = Config.Bind<float>(pistol, "Pistol Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.PistolAdditionalRotationX = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation X-Axis.", 0.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolAdditionalRotationY = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation Y-Axis.", 0.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolAdditionalRotationZ = Config.Bind<float>(pistol, "Pistol Ready Additional Rotation Z-Axis.", 0.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.PistolResetRotationX = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation X-Axis", -1f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolResetRotationY = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation Y-Axis", 0.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.PistolResetRotationZ = Config.Bind<float>(pistol, "Pistol Ready Reset Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ShortStockAdditionalRotationSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Additional Rotation Speed Multi", 2.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 35, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockResetRotationSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Reset Rotation Speed Multi", 2.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 34, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockRotationMulti = Config.Bind<float>(shortStock, "Short-Stock Rotation Speed Multi", 2.0f, new ConfigDescription("How Fast The Weapon Rotates.", new AcceptableValueRange<float>(0.1f, 5f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 33, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Position Speed Multi.", 6.0f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 32, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockResetSpeedMulti = Config.Bind<float>(shortStock, "Short-Stock Position Reset Speed Mult", 7.25f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 100.0f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 30, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ShortStockOffsetX = Config.Bind<float>(shortStock, "Short-Stock Position X-Axis", 0.02f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 25, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockOffsetY = Config.Bind<float>(shortStock, "Short-Stock Position Y-Axis", 0.1f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 24, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockOffsetZ = Config.Bind<float>(shortStock, "Short-Stock Position Z-Axis", -0.025f, new ConfigDescription("Weapon Position When In Stance.", new AcceptableValueRange<float>(-10f, 10f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 23, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ShortStockRotationX = Config.Bind<float>(shortStock, "Short-Stock Rotation X-Axis", 0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 12, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockRotationY = Config.Bind<float>(shortStock, "Short-Stock Rotation Y-Axis", -15.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 11, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockRotationZ = Config.Bind<float>(shortStock, "Short-Stock Rotation Z-Axis", 0.0f, new ConfigDescription("Weapon Rotation When In Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 10, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ShortStockAdditionalRotationX = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation X-Axis.", -3.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 6, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockAdditionalRotationY = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation Y-Axis.", -15.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 5, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockAdditionalRotationZ = Config.Bind<float>(shortStock, "Short-Stock Ready Additional Rotation Z-Axis.", 1.0f, new ConfigDescription("Additional Seperate Weapon Rotation When Going Into Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 4, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));

            PluginConfig.ShortStockResetRotationX = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation X-Axis", -1.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 3, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockResetRotationY = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation Y-Axis", 2f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 2, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
            PluginConfig.ShortStockResetRotationZ = Config.Bind<float>(shortStock, "Short-Stock Ready Reset Rotation Z-Axis", 0.5f, new ConfigDescription("Weapon Rotation When Going Out Of Stance.", new AcceptableValueRange<float>(-1000f, 1000f), new ConfigurationManagerAttributes { ShowRangeAsPercent = false, Order = 1, IsAdvanced = true, Browsable = ServerConfig.enable_stances }));
        }
    }
}



