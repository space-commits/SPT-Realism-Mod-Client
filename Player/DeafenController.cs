using Comfort.Common;
using Prism.Utils;
using RealismMod.Health;
using UnityEngine;

namespace RealismMod
{


    public static class HeadsetGainController
    {
        public static void IncreaseGain() 
        {
            if (Input.GetKeyDown(PluginConfig.IncGain.Value.MainKey) && DeafenController.HasHeadSet)
            {
                if (PluginConfig.HeadsetGain.Value < DeafenController.MaxGain)
                {
                    PluginConfig.HeadsetGain.Value += 1;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.RealismAudioController.DeviceAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }
        }

        public static void DecreaseGain() 
        {
            if (Input.GetKeyDown(PluginConfig.DecGain.Value.MainKey) && DeafenController.HasHeadSet)
            {
                if (PluginConfig.HeadsetGain.Value > DeafenController.MinGain)
                {
                    PluginConfig.HeadsetGain.Value -= 1;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), Plugin.RealismAudioController.DeviceAudioClips["beep.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }
        }

        public static void AdjustHeadsetVolume()
        {
            IncreaseGain();
            DecreaseGain();
        }
    }

    public static class DeafenController
    {
        public const float AmbientOutVolume = 0f; //-0.91515f
        public const float AmbientInVolume = 0f; //-80f
        public const float WeaponMuffleBase = -70f;
        public const float BaseMainVolume = 0f;
        public const float LightHelmetDeafReduction = 0.9f;
        public const float HeavyHelmetDeafreduction = 0.8f;
        public const float MinHeadsetProtection = 0.8f;
        public const float MaxHeadsetProtection = 0.25f;
        public const float HeadsetUpperProtection = 26f;
        public const float HeadestLowerProtection = 16f;
        public const float VignetteFactor = 0.16f;
        public const int MaxGain = 15;
        public const int MinGain = -10;
        public const float MaxMuffle = 15f;
        public const float MaxDeafen = 30;
        public const float MaxVignette = 2.4f;

        public static float EarProtectionFactor { get; set; } = 1f;
        public static float AmmoDeafFactor { get; set; } = 1f;
        public static float GunDeafFactor { get; set; } = 1f;
        public static float BotFiringDeafFactor { get; set; } = 1f;
        public static float ExplosionDeafFactor { get; set; } = 1f;
        public static bool HasHeadSet { get; set; } = false;
        public static float HeadSetGain { get; set; } = PluginConfig.HeadsetGain.Value;
        public static bool IsBotFiring { get; set; } = false;
        public static bool GrenadeExploded { get; set; } = false;
        public static float BotTimer { get; set; } = 0f;
        public static float GrenadeTimer { get; set; } = 0f;

        private static float _mainVolumeReduction = 0f;
        private static float _mainVolumeReductionTarget = 0f;
        private static float _maxShootVolumeReduction = 0f;
        private static float _maxBotVolumeReduction = 0f;
        private static float _maxShootMuffle = 0f;
        private static float _maxBotShootMuffle = 0f;
        private static float _muffleTarget = WeaponMuffleBase;
        private static float _gunShotMuffle = WeaponMuffleBase;
        private static float _vignetteAmount = 0f;
        private static float _vignetteTarget = 0f;
        private static float _maxShootVignette = 0f; //relative to the specific gun
        private static float _maxBotShootVignette = 0f;
        private static float _maxExplosionVignette = 0f;

        public static float EnvironmentFactor
        {
            get
            {
                return PlayerState.EnviroType == EnvironmentType.Indoor ? 1.4f : 1f;
            }
        }

        public static void DoLogging()
        {
            //Utils.Logger.LogWarning($"_mainVolumeReduction {_mainVolumeReduction}, _gunShotMuffle {_gunShotMuffle}, _vignetteAmount {_vignetteAmount}");
            //Utils.Logger.LogWarning($"_maxShootVolumeReduction {_maxShootVolumeReduction}, _mainVolumeReductionTarget {_mainVolumeReductionTarget}, _mainVolumeReduction {_mainVolumeReduction}");
            //Utils.Logger.LogWarning($"_maxShootMuffle {_maxShootMuffle}, _muffleTarget {_muffleTarget}, _gunShotMuffle {_gunShotMuffle}");
            // Utils.Logger.LogWarning($"_maxShootVignette {_maxShootVignette}, _vignetteTarget {_vignetteTarget}, _vignetteTarget {_vignetteTarget}");
        }


        public static void IncreaseDeafeningShooting()
        {
            float factor = AmmoDeafFactor * GunDeafFactor * EarProtectionFactor * EnvironmentFactor;
            float newMaxDeafen = factor * 2f;
            float newMaxMuffle = factor;
            float newMaxVignette = factor * 0.28f;

            //volume
            _mainVolumeReductionTarget = _mainVolumeReductionTarget < newMaxDeafen ? _mainVolumeReductionTarget + factor : _mainVolumeReductionTarget;
            _maxShootVolumeReduction = Mathf.Max(newMaxDeafen, _maxShootVolumeReduction);
            //muffle
            _muffleTarget = _muffleTarget < newMaxMuffle ? _muffleTarget + (factor * 2f) : _muffleTarget;
            _maxShootMuffle = Mathf.Max(newMaxMuffle, _maxShootMuffle);
            //vignete
            _vignetteTarget = _vignetteTarget < newMaxVignette ? _vignetteTarget + (factor * VignetteFactor * EarProtectionFactor) : _vignetteTarget;//vignette needs to be reduced more
            _maxShootVignette = Mathf.Max(newMaxVignette, _maxShootVignette);
        }

        public static void IncreaseDeafeningShotAt()
        {
            float factor = BotFiringDeafFactor * EnvironmentFactor;
            //volume
            _mainVolumeReductionTarget += factor * 0.5f;
            _maxBotVolumeReduction = Mathf.Max(factor, _maxBotVolumeReduction);
            //muffle
            _muffleTarget += factor;
            _maxBotShootMuffle = Mathf.Max(factor, _maxBotShootMuffle);
            //vignete
            _vignetteTarget += factor * 0.09f;
            _maxBotShootVignette = Mathf.Max(factor * 0.15f, _maxBotShootVignette);
        }

        public static void IncreaseDeafeningExplosion()
        {
            float factor = ExplosionDeafFactor * EnvironmentFactor;
            //volume
            _mainVolumeReductionTarget += factor * 0.2f; //0.2
            //muffle
            _muffleTarget += factor * 0.9f; //0.9
            //vignete
            _vignetteTarget += factor * 0.025f; // 0.025
            _maxExplosionVignette = Mathf.Max(factor * 0.035f, _maxExplosionVignette); //0.035
        }

        public static void SetAudio(float mainVolume)
        {
            Singleton<BetterAudio>.Instance.Master.SetFloat("InGame", mainVolume + _mainVolumeReduction); //main volume
            Singleton<BetterAudio>.Instance.Master.SetFloat("Tinnitus1", _gunShotMuffle); //higher = more muffled (reverby with headset) gunshots and all weapon sounds

            //ambient
            if (PluginConfig.EnableAmbientChanges.Value) 
            {
                float ambientBase = HasHeadSet ? 10f : 5f;
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOutVolume", AmbientOutVolume + ambientBase + PluginConfig.OutdoorAmbientMulti.Value - 7f); //outdoors
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientInVolume", AmbientInVolume + ambientBase + PluginConfig.IndoorAmbientMulti.Value - 15f); //indoor audio seems bugged if it's too high relative to outdoor
            }
        }

        public static void DoVignette()
        {
            ScreenEffectsController.PrismEffects.SetVignetteStrength(_vignetteAmount);
        }

        public static void DoTimers()
        {
            if (IsBotFiring)
            {
                BotTimer += Time.deltaTime;
                if (BotTimer >= 0.5f)
                {
                    IsBotFiring = false;
                    BotTimer = 0f;
                }
            }

            if (GrenadeExploded)
            {
                GrenadeTimer += Time.deltaTime;
                if (GrenadeTimer >= 25f)
                {
                    GrenadeExploded = false;
                    GrenadeTimer = 0f;
                }
            }
        }

        public static void ResetMaxValues()
        {
            if (Utils.AreFloatsEqual(_mainVolumeReduction, 0f, 2f))
            {
                _maxShootVolumeReduction = 0f;
                _maxBotVolumeReduction = 0f;
            }

            if (Utils.AreFloatsEqual(_gunShotMuffle, WeaponMuffleBase, 5f))
            {
                _maxShootMuffle = 0f;
                _maxBotShootMuffle = 0f;
            }

            if (Utils.AreFloatsEqual(_vignetteAmount, 0f, 0.1f)) 
            {
                if(ScreenEffectsController.PrismEffects.useVignette) ScreenEffectsController.PrismEffects.useVignette = false;
                _maxShootVignette = 0f;
                _maxBotShootVignette = 0f;
                _maxExplosionVignette = 0f;
            }
        }

        public static void ResetValues() 
        {
            _mainVolumeReductionTarget = 0f;
            _muffleTarget = WeaponMuffleBase;
            _vignetteTarget = 0f;

            _mainVolumeReduction = Mathf.Lerp(_mainVolumeReduction, BaseMainVolume, 0.0009f); //0.0009f
            _gunShotMuffle = Mathf.Lerp(_gunShotMuffle, WeaponMuffleBase, 0.0014f);//0.0014f
            _vignetteAmount = Mathf.Lerp(_vignetteAmount, 0f, 0.01f);//0.01f
        }

        public static void SetValues()
        {

            float maxVolumeReduction = Mathf.Min(MaxDeafen, _maxShootVolumeReduction + _maxBotVolumeReduction);
            _mainVolumeReduction = Mathf.Lerp(_mainVolumeReduction, Mathf.Max(-_mainVolumeReductionTarget, -maxVolumeReduction), 0.01f); //0.01f
            float maxMuffle = Mathf.Min(MaxMuffle, _maxShootMuffle + _maxBotShootMuffle);
            _gunShotMuffle = Mathf.Lerp(_gunShotMuffle, Mathf.Min(_muffleTarget, maxMuffle), 0.035f);

            float maxVignette = Mathf.Min(MaxVignette, _maxShootVignette + _maxBotShootVignette + _maxExplosionVignette);
            _vignetteAmount = Mathf.Lerp(_vignetteAmount, Mathf.Min(_vignetteTarget, maxVignette), 0.1f);

            ScreenEffectsController.PrismEffects.useVignette = true;
            ScreenEffectsController.PrismEffects.vignetteStart = 1.5f;
            ScreenEffectsController.PrismEffects.vignetteEnd = 0.1f;
        }

        public static void DoDeafening()
        {
            float baseMainVolume = 0f;
            if (IsBotFiring || GrenadeExploded || ShootController.IsFiringDeafen)
            {
                if (HasHeadSet)
                {
                    baseMainVolume = Mathf.Min(PluginConfig.HeadsetNoiseReduction.Value, PluginConfig.HeadsetGain.Value - 5f);
                }

                SetValues();
            }
            else
            {
                if (HasHeadSet)
                {
                    baseMainVolume = PluginConfig.HeadsetGain.Value;
                }

                ResetValues();
                ResetMaxValues();
            }

            SetAudio(baseMainVolume);
            DoVignette();
            DoTimers();

            if (PluginConfig.EnableGeneralLogging.Value) DoLogging();
        }
    }
}
