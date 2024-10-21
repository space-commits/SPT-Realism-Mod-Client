using Comfort.Common;
using EFT;
using EFT.UI.Ragfair;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Interactive.BetterPropagationGroups;
using static Val;


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
        private float _gameVolume = 1f;

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

        void Start()
        {
            _gameVolume = Singleton<SharedGameSettingsClass>.Instance.Sound.Settings.OverallVolume.Value * 0.1f;
            _audioSource = GetComponent<AudioSource>();
            _audioSource = this.gameObject.AddComponent<AudioSource>();
            _audioSource.volume = Volume * _gameVolume;
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

            if (PlayerState.EnviroType == EnvironmentType.Indoor || PlayerState.BtrState == EPlayerBtrState.Inside)
            {
                _audioSource.volume = Mathf.MoveTowards(_audioSource.volume, 0.5f, 0.35f * Time.deltaTime);
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
    }

    public static class HeadsetGainController 
    {
        public static void AdjustHeadsetVolume()
        {
            if (Input.GetKeyDown(PluginConfig.IncGain.Value.MainKey) && DeafeningController.HasHeadSet)
            {
                if (PluginConfig.RealTimeGain.Value < 30)
                {
                    PluginConfig.RealTimeGain.Value += 1f;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.DeviceAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }
            if (Input.GetKeyDown(PluginConfig.DecGain.Value.MainKey) && DeafeningController.HasHeadSet)
            {

                if (PluginConfig.RealTimeGain.Value > 0)
                {
                    PluginConfig.RealTimeGain.Value -= 1f;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.DeviceAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }
        }
    }

    public static class AudioController 
    {
        public static void CreateAudioComponent()
        {
            GameObject audioGO = new GameObject("HazardAudioController");
            var audioPlayer = audioGO.AddComponent<AudioControllerComponent>();
        }
    }

    public class AudioControllerComponent: MonoBehaviour
    {
        private Player _player;
        private AudioSource _gasMaskAudioSource;
        private AudioSource _gasAnalyserSource;
        private AudioSource _geigerSource;
        private const float _baseBreathVolume = 0.2f;
        private static float _currentBreathClipLength = 0f;
        private static float _breathTimer = 0f;
        private static float _breathCountdown = 2.5f;
        private static float _coughTimer = 0f;
        private static bool _breathedOut = false;

        void Start()
        {
            _gasMaskAudioSource = this.gameObject.AddComponent<AudioSource>();
            _gasAnalyserSource = this.gameObject.AddComponent<AudioSource>();
            _geigerSource = this.gameObject.AddComponent<AudioSource>();

            AudioSource[] sources = { _gasMaskAudioSource, _gasAnalyserSource , _geigerSource };

            foreach (var source in sources) 
            {
                source.volume = 1f;
                source.loop = false;
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.minDistance = 5f;
                source.maxDistance = 10f;
                source.rolloffMode = AudioRolloffMode.Linear;
            };

            //I know this is scuffed, temporary solution
            DeviceController.GeigerAudioSource = _geigerSource;
            DeviceController.GasAnalyserAudioSource = _gasAnalyserSource;
        }

        void Update()
        {
            if (Utils.IsInHideout || !Utils.PlayerIsReady) return;

            if (_player == null)
            {
                _player = Utils.GetYourPlayer();
                this.gameObject.transform.parent = _player.gameObject.transform;
            }

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
                bool isNotBusy = !_player.Speaker.Busy || !_player.Speaker.Speaking;
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

            DeviceController.DoGasAnalyserAudio(_player);
            DeviceController.DoGeigerAudio(_player);

        }

        private void CoughController() 
        {
            if (Plugin.RealHealthController.DoCoughingAudio) _player.Speaker.Play(EPhraseTrigger.OnBreath, ETagStatus.Dying | ETagStatus.Aware, true, null);
        }

        private float GetBreathVolume() 
        {
            return _baseBreathVolume * (2f - PlayerState.BaseStaminaPerc) * PluginConfig.GasMaskBreathVolume.Value;
        }

        private string GetAudioFromOtherStates()
        {
            if (HazardTracker.TotalToxicity >= 70f || HazardTracker.TotalRadiation >= 85f || Plugin.RealHealthController.IsCoughingInGas)
            {
                return "Dying";
            }
            if (HazardTracker.TotalToxicity >= 50f || PlayerState.BaseStaminaPerc <= 0.55 || HazardTracker.TotalRadiation >= 70f || Plugin.RealHealthController.HasAdrenalineEffect)
            {
                return "BadlyInjured";
            }
            if (HazardTracker.TotalToxicity >= 30f || PlayerState.BaseStaminaPerc <= 0.8f || HazardTracker.TotalRadiation >= 50f)
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
            string healthStatus = _player.HealthStatus.ToString();
            string desiredClip = GetAudioFromOtherStates();
            string clipToUse = ChooseAudioClip(healthStatus, desiredClip);
            string inOut = breathOut ? "out" : "in";
            string clipName = inOut + "_" + clipToUse + rndNumber + ".wav";
            AudioClip audioClip = Plugin.GasMaskAudioClips[clipName];
            _currentBreathClipLength = audioClip.length;
            float playBackVolume = GetBreathVolume();
            _player.SpeechSource.SetLowPassFilterParameters(0.99f, ESoundOcclusionType.Obstruction, 1600, 5000, true); //muffles player voice
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, playBackVolume, EOcclusionTest.None, null, false);
        }
    }
}
