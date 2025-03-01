using Comfort.Common;
using EFT;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static EFT.Interactive.BetterPropagationGroups;


namespace RealismMod
{
    public class AmbientAudioPlayer : MonoBehaviour
    {
        public Player _Player { get; set; }
        public List<AudioClip> AudioClips = new List<AudioClip>();
        public Transform ParentTransform;
        public bool FollowPlayer = false;
        public float MinTimeBetweenClips = 30f;
        public float MaxTimeBetweenClips = 120;
        public float MinDistance = 48f;
        public float MaxDistance = 60f;
        public float DelayBeforePlayback = 30f;
        public float Volume = 1f;
        private float _elapsedTime = 0f;
        private AudioSource _audioSource;
        private float _randomDistanceFromPlayer;
        private Vector3 _relativePositionFromPlayer;

        void Start()
        {
            Volume *= GameWorldController.GetGameVolumeAsFactor();
            _audioSource = this.gameObject.AddComponent<AudioSource>();
            _audioSource.volume = Volume;
            _audioSource.loop = false;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
            _audioSource.maxDistance = 25f;
            _audioSource.maxDistance = 130f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;

            if (FollowPlayer) transform.SetParent(ParentTransform);

            StartCoroutine(PlayRandomAudio());
        }

        void Update()
        {
            _elapsedTime += Time.deltaTime;

            if (PlayerValues.EnviroType == EnvironmentType.Indoor || PlayerValues.BtrState == EPlayerBtrState.Inside)
            {
                _audioSource.volume = Mathf.MoveTowards(_audioSource.volume, Volume * 0.5f, 0.35f * Time.deltaTime);
            }
            else
            {
                _audioSource.volume = Mathf.MoveTowards(_audioSource.volume, Volume, 0.35f * Time.deltaTime);
            }

            if (FollowPlayer)
            {
                _relativePositionFromPlayer = Quaternion.AngleAxis(0.35f * Time.deltaTime, Vector3.up) * _relativePositionFromPlayer;
                transform.position = ParentTransform.position + _relativePositionFromPlayer;

                /*    transform.position = ParentTransform.position + (transform.position - ParentTransform.position).normalized * _randomDistanceFromPlayer;
                    transform.RotateAround(ParentTransform.position, Vector3.up, 0.35f * Time.deltaTime);*/
                ///transform.position = ParentTransform.position + _positionRelativeToPlayer; 
            }
        }

        private IEnumerator PlayRandomAudio()
        {
            while (true)
            {
                if (Utils.PlayerIsReady && _elapsedTime >= DelayBeforePlayback)
                {
                    AudioClip selectedClip = AudioClips[UnityEngine.Random.Range(0, AudioClips.Count)];

                    _randomDistanceFromPlayer = UnityEngine.Random.Range(MinDistance, MaxDistance);
                    var randomPosition = ParentTransform.position + UnityEngine.Random.onUnitSphere * _randomDistanceFromPlayer;
                    randomPosition.y = Mathf.Clamp(randomPosition.y, ParentTransform.position.y - 25f, ParentTransform.position.y + 25f);
                    transform.position = randomPosition;
                    _relativePositionFromPlayer = transform.position - ParentTransform.position;

                    if (PluginConfig.ZoneDebug.Value)
                    {
                        GameObject visualRepresentation = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        visualRepresentation.name = "AmbientAudioPlayerVisual";
                        visualRepresentation.transform.parent = transform;
                        visualRepresentation.transform.localScale = Vector3.one;
                        visualRepresentation.transform.position = transform.position;
                        visualRepresentation.transform.rotation = ParentTransform.transform.rotation;
                        visualRepresentation.GetComponent<Renderer>().material.color = new UnityEngine.Color(1, 0, 0, 1);
                    }

                    _audioSource.clip = selectedClip;
                    _audioSource.Play();

                    yield return new WaitForSeconds(selectedClip.length);
                    float waitTime = UnityEngine.Random.Range(MinTimeBetweenClips, MaxTimeBetweenClips);
                    yield return new WaitForSeconds(waitTime);
                }
                yield return null;
            }
        }

    }

    public static class AudioController 
    {
        public static void CreateAmbientAudioPlayer(Player player, Transform parentTransform, Dictionary<string, AudioClip> clips, bool followPlayer = false, float minTime = 15f, float maxTime = 90f, float volume = 1f, float minDistance = 45f, float maxDistance = 95f, float minDelayBeforePlayback = 60f)
        {
            GameObject audioGO = new GameObject("AmbientAudioPlayer");
            var audioPlayer = audioGO.AddComponent<AmbientAudioPlayer>();
            audioPlayer.ParentTransform = parentTransform;
            audioPlayer._Player = player;
            audioPlayer.FollowPlayer = followPlayer;
            audioPlayer.MinTimeBetweenClips = minTime;
            audioPlayer.MaxTimeBetweenClips = maxTime;
            audioPlayer.MinDistance = minDistance;
            audioPlayer.MaxDistance = maxDistance;
            audioPlayer.Volume = volume;
            audioPlayer.DelayBeforePlayback = minDelayBeforePlayback;
            foreach (var clip in clips)
            {
                audioPlayer.AudioClips.Add(clip.Value);
            }
        }
    }

    public class RealismAudioControllerComponent: MonoBehaviour
    {
        private Player _Player;
        private AudioSource _foodPoisoningSfx;
        private AudioSource _gasMaskAudioSource;
        private AudioSource _gasAnalyserSource;
        private AudioSource _geigerAudioSource;
        private AudioSource _toggleDeviceSource;

        private const float GAS_DELAY = 4f;
        private const float RAD_DELAY = 3f;
        private const float GAS_DEVICE_VOLUME = 0.28f;
        private const float GEIGER_VOLUME = 0.32f;
        private const float BASE_BREATH_VOLUME = 0.3f;
        private const float TOGGLE_DEVICE_VOLUME = 0.6f;

        private static float _currentBreathClipLength = 0f;
        private static float _breathTimer = 0f;
        private static float _breathCountdown = 2.5f;
        private static float _coughTimer = 0f;
        private static bool _breathedOut = false;

        private static bool _muteGeiger = false;
        private static bool _muteGasAnalyser = false;
        private static float _currentGasClipLength = 0f;
        private static float _gasDeviceTimer = 0f;
        private static float _currentGeigerClipLength = 0f;
        private static float _geigerDeviceTimer = 0f;

        void Start()
        {
            _gasMaskAudioSource = this.gameObject.AddComponent<AudioSource>();
            _gasAnalyserSource = this.gameObject.AddComponent<AudioSource>();
            _geigerAudioSource = this.gameObject.AddComponent<AudioSource>();
            _toggleDeviceSource = this.gameObject.AddComponent<AudioSource>();
            _foodPoisoningSfx = this.gameObject.AddComponent<AudioSource>();

            SetUpAudio(_gasMaskAudioSource, 1f, 0f);
            SetUpAudio(_gasAnalyserSource, 1f, 1f);
            SetUpAudio(_geigerAudioSource, 1f, 1f);
            SetUpAudio(_toggleDeviceSource, TOGGLE_DEVICE_VOLUME, 0.37f);
            SetUpAudio(_foodPoisoningSfx, 0.65f, 0f);
        }

        void Update()
        {
            if (Utils.IsInHideout || !Utils.PlayerIsReady || _Player == null) return;

            transform.position = _Player.gameObject.transform.position;

            _breathTimer += Time.deltaTime;
            _coughTimer += Time.deltaTime;  

            if (GearController.HasGasMask && _breathCountdown > 0f)
            {
                _breathCountdown -= Time.deltaTime;
            }
            else 
            {
                _breathCountdown = 2.5f;
            }

            if (_breathTimer > _currentBreathClipLength && _breathCountdown <= 0f && GearController.HasGasMask)
            {
                bool isNotBusy = !_Player.Speaker.Busy || !_Player.Speaker.Speaking;
                if (isNotBusy)
                {
                    PlayGasMaskBreathing(_breathedOut);
                    _breathTimer = 0f;
                    _breathedOut = !_breathedOut;
                }
            }

            if (_coughTimer > 5f)
            {
                CoughController();
                _coughTimer = 0f;
            }

            DoGasAnalyserAudio();
            DoGeigerAudio();

        }

        public void RunReInitPlayer()
        {
            _Player = Utils.GetYourPlayer();
            transform.position = _Player.gameObject.transform.position;
        }

        private void SetUpAudio(AudioSource source, float vol = 1f, float spatialBlend = 0f, float minDistance = 5f, float maxDistance = 10f) 
        {
            source.volume = vol * GameWorldController.GetGameVolumeAsFactor();
            source.spatialBlend = spatialBlend; 
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
        }

        public void PlayFoodPoisoningSFX(float vol = 0.5f)
        {
            _foodPoisoningSfx.clip = Plugin.FoodPoisoningSfx.RandomElement().Value;
            _foodPoisoningSfx.volume = vol * GameWorldController.GetGameVolumeAsFactor();
            _foodPoisoningSfx.Play();
        }

        private void CoughController() 
        {
            if (Plugin.RealHealthController.DoCoughingAudio) _Player.Speaker.Play(EPhraseTrigger.OnBreath, ETagStatus.Dying | ETagStatus.Aware, true, null);
        }

        private float GetBreathVolume() 
        {
            float baseVol = BASE_BREATH_VOLUME + GameWorldController.GetHeadsetVolume();
            float modifiers = (2f - PlayerValues.BaseStaminaPerc) * PluginConfig.GasMaskBreathVolume.Value * GameWorldController.GetGameVolumeAsFactor();
            return Mathf.Max(baseVol * modifiers, 0);
        }

        private string GetAudioFromOtherStates()
        {
            if (HazardTracker.TotalToxicity >= 70f || HazardTracker.TotalRadiation >= 85f || Plugin.RealHealthController.IsCoughingInGas)
            {
                return "Dying";
            }
            if (HazardTracker.TotalToxicity >= 50f || PlayerValues.BaseStaminaPerc <= 0.55 || HazardTracker.TotalRadiation >= 70f || Plugin.RealHealthController.HasPositiveAdrenalineEffect)
            {
                return "BadlyInjured";
            }
            if (HazardTracker.TotalToxicity >= 30f || PlayerValues.BaseStaminaPerc <= 0.8f || HazardTracker.TotalRadiation >= 50f)
            {
                return "Injured";
            }
            return "Healthy";
        }

        private string ChooseAudioClip(string healthStatus, string desiredClip)
        {
            if (desiredClip == "Dying" || healthStatus == "Dying")
            {
                return "Dying";
            }
            if (desiredClip == "BadlyInjured" || healthStatus == "BadlyInjured")
            {
                return "BadlyInjured";
            }
            if (desiredClip == "Injured" || healthStatus == "Injured")
            {
                return "Injured";
            }
            return "Healthy";
        }

        public void PlayGasMaskBreathing(bool breathOut)
        {
            int rndNumber = UnityEngine.Random.Range(1, 4);
            string healthStatus = _Player.HealthStatus.ToString();
            string desiredClip = GetAudioFromOtherStates();
            string clipToUse = ChooseAudioClip(healthStatus, desiredClip);
            string inOut = breathOut ? "out" : "in";
            string clipName = inOut + "_" + clipToUse + rndNumber + ".wav";
            AudioClip audioClip = Plugin.GasMaskAudioClips[clipName];
            _currentBreathClipLength = audioClip.length;
            float playBackVolume = GetBreathVolume();
            _Player.SpeechSource.SetLowPassFilterParameters(0.99f, ESoundOcclusionType.Obstruction, 1600, 5000, true); //muffles player voice
            _gasMaskAudioSource.volume = playBackVolume;
            _gasMaskAudioSource.clip = audioClip;
            _gasMaskAudioSource.Play();
        }

        private float GetGasDelayTime()
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 4f;
            return GAS_DELAY * (1f - HazardTracker.BaseTotalToxicityRate);
        }

        private float GeRadDelayTime()
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 1f;
            if (HazardTracker.BaseTotalRadiationRate >= 0.14f) return 0f;
            float radRate = HazardTracker.BaseTotalRadiationRate;
            float delay = RAD_DELAY * (1f - Mathf.Pow(radRate, 0.35f));
            return delay;
        }

        private void PlayToggleSfx(string clip)
        {
            _toggleDeviceSource.clip = Plugin.DeviceAudioClips[clip];
            _toggleDeviceSource.volume = (TOGGLE_DEVICE_VOLUME + GameWorldController.GetHeadsetVolume()) * GameWorldController.GetGameVolumeAsFactor();
            _toggleDeviceSource.Play();
        }

        private void DoDetectors() 
        {
            if (GearController.HasGasAnalyser && GameWorldController.GameStarted && Utils.PlayerIsReady)
            {
                DoGasAnalyserAudio();
                DoGeigerAudio();
            }
        }

        public void DoGasAnalyserAudio()
        {
            if (GearController.HasGasAnalyser)
            {
                _gasDeviceTimer += Time.deltaTime;

                if (Input.GetKeyDown(PluginConfig.MuteGasAnalyserKey.Value.MainKey) && PluginConfig.MuteGasAnalyserKey.Value.Modifiers.All(Input.GetKey))
                {
                    _muteGasAnalyser = !_muteGasAnalyser;
                    PlayToggleSfx("switch_off.wav");
                }

                if (_gasDeviceTimer > _currentGasClipLength && _gasDeviceTimer >= GetGasDelayTime())
                {
                    PlayerZoneBridge bridge = Plugin.RealHealthController.PlayerHazardBridge;
                    if (_Player != null && bridge != null && HazardTracker.BaseTotalToxicityRate > 0)
                    {
                        PlayGasAnalyserClips(_Player);
                        _gasDeviceTimer = 0f;
                    }
                }
            }
        }

        public void DoGeigerAudio()
        {
            if (GearController.HasGeiger)
            {
                _geigerDeviceTimer += Time.deltaTime;

                if (Input.GetKeyDown(PluginConfig.MuteGeigerKey.Value.MainKey) && PluginConfig.MuteGeigerKey.Value.Modifiers.All(Input.GetKey))
                {
                    _muteGeiger = !_muteGeiger;
                    PlayToggleSfx("switch_off.wav");
                }

                if (_geigerDeviceTimer > _currentGeigerClipLength && _geigerDeviceTimer >= GeRadDelayTime())
                {
                    PlayerZoneBridge bridge = Plugin.RealHealthController.PlayerHazardBridge;
                    if (_Player != null && bridge != null && HazardTracker.BaseTotalRadiationRate > 0)
                    {
                        PlayGeigerClips(_Player);
                        _geigerDeviceTimer = 0f;
                    }
                }
            }
        }

        public string GetGasAnalsyerClip(float gasLevel, out float volumeModi)
        {
            switch (gasLevel)
            {
                case <= 0.01f:
                    volumeModi = 1f;
                    return null;
                case <= 0.02f:
                    volumeModi = 1.1f;
                    return "gasBeep1.wav";
                case <= 0.04f:
                    volumeModi = 1.1f;
                    return "gasBeep2.wav";
                case <= 0.08f:
                    volumeModi = 1.1f;
                    return "gasBeep3.wav";
                case <= 0.12f:
                    volumeModi = 1f;
                    return "gasBeep4.wav";
                case <= 0.16f:
                    volumeModi = 1f;
                    return "gasBeep5.wav";
                case <= 0.19f:
                    volumeModi = 1f;
                    return "gasBeep6.wav";
                case > 0.19f:
                    volumeModi = 0.8f;
                    return "gasBeep7.wav";
                default:
                    volumeModi = 1f;
                    return null;
            }
        }

        public string[] GetGeigerClip(float radLevel)
        {
            switch (radLevel)
            {
                case <= 0.015f:
                    return new string[] { "geiger1.wav", "geiger1_1.wav", "geiger1_2.wav", "geiger1_3.wav" };
                case <= 0.03f:
                    return new string[] { "geiger2.wav", "geiger2_1.wav", "geiger2_2.wav", "geiger2_3.wav" };
                case <= 0.06f:
                    return new string[] { "geiger3.wav", "geiger3_1.wav", "geiger3_2.wav", "geiger3_3.wav" };
                case <= 0.1f:
                    return new string[] { "geiger4.wav", "geiger4_1.wav", "geiger4_2.wav", "geiger4_3.wav" };
                case <= 0.14f:
                    return new string[] { "geiger5.wav", "geiger5_1.wav", "geiger5_2.wav", "geiger5_3.wav" };
                case <= 0.2f:
                    return new string[] { "geiger6.wav", "geiger6_1.wav", "geiger6_2.wav", "geiger6_3.wav" };
                case > 0.2f:
                    return new string[] { "geiger7.wav", "geiger7_1.wav", "geiger7_2.wav", "geiger7_3.wav" };
                default:
                    return null;
            }
        }

        private float GetDeviceVolume(float baseVol, float additionalModi = 1f)
        {
            baseVol += GameWorldController.GetHeadsetVolume();
            float modifiers = PluginConfig.DeviceVolume.Value * additionalModi * GameWorldController.GetGameVolumeAsFactor();
            return Mathf.Max(baseVol * modifiers, 0);
        }

        public void PlayGasAnalyserClips(Player player)
        {
            float volumeModi = 1f;
            string clip = GetGasAnalsyerClip(HazardTracker.BaseTotalToxicityRate, out volumeModi);
            if (clip == null) return;
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentGasClipLength = audioClip.length;
            float volume = _muteGasAnalyser ? 0f : GetDeviceVolume(GEIGER_VOLUME, volumeModi);

            _gasAnalyserSource.volume = volume;
            _gasAnalyserSource.clip = audioClip;
            _gasAnalyserSource.Play();
        }

        public void PlayGeigerClips(Player player)
        {
            string[] clips = GetGeigerClip(HazardTracker.BaseTotalRadiationRate);
            if (clips == null) return;
            int rndNumber = UnityEngine.Random.Range(0, clips.Length);
            string clip = clips[rndNumber];
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentGeigerClipLength = audioClip.length;
            float volume = _muteGeiger ? 0f : GetDeviceVolume(GEIGER_VOLUME);

            _geigerAudioSource.volume = volume;
            _geigerAudioSource.clip = audioClip;
            _geigerAudioSource.Play();
        }
    }
}
