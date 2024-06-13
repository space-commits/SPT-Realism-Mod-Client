using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public static class DeviceController
    {
        public static bool HasGasAnalyser { get; set; } = false;
        public static bool HasGeiger { get; set; } = false;
        private const float GasDelay = 5f;
        private const float RadDelay = 1f;
        private const float BaseDeviceVolume = 0.2f;

        private static float _currentGasClipLength = 0f;
        private static float _gasDeviceTimer = 0f;

        private static float _currentGeigerClipLength = 0f;
        private static float _geigerDeviceTimer = 0f;


        private static float GetGasDelayTime() 
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 4f;
            return GasDelay * (1f - Plugin.RealHealthController.PlayerHazardBridge.GasAmount);
        }

        private static float GeRadDelayTime()
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 1f;
            if (Plugin.RealHealthController.PlayerHazardBridge.RadAmount >= 0.15f) return 0f;
            return RadDelay * (1f - Plugin.RealHealthController.PlayerHazardBridge.RadAmount);
        }

        public static void GasAnalyserAudioController()
        {
            if (HasGasAnalyser && GameWorldController.GameStarted && Utils.IsReady) 
            {
                _gasDeviceTimer += Time.deltaTime;

                if (_gasDeviceTimer > _currentGasClipLength && _gasDeviceTimer >= GetGasDelayTime())
                {
                    Player player = Utils.GetYourPlayer();
                    PlayerHazardBridge bridge = Plugin.RealHealthController.PlayerHazardBridge;
                    if (player != null && bridge != null && bridge.IsInGasZone)
                    {
                        PlayGasAnalyserClips(player, bridge);
                        _gasDeviceTimer = 0f;
                    }
                }
            }
        }

        public static void GeigerAudioController()
        {
            if (HasGeiger)
            {
                _geigerDeviceTimer += Time.deltaTime;

                if (_geigerDeviceTimer > _currentGeigerClipLength && _geigerDeviceTimer >= GeRadDelayTime())
                {
                    Player player = Utils.GetYourPlayer();
                    PlayerHazardBridge bridge = Plugin.RealHealthController.PlayerHazardBridge;
                    if (player != null && bridge != null && bridge.IsInRadZone)
                    {
                        PlayGeigerClips(player, bridge);
                        _geigerDeviceTimer = 0f;
                    }
                }
            }
        }

        public static string GetGasAnalsyerClip(float gasLevel) 
        {
            switch (gasLevel) 
            {
                case <= 0f:
                    return null;
                case <= 0.1f:
                    return "gasBeep1.wav";
                case <= 0.15f:
                    return "gasBeep2.wav";
                case <= 0.2f:
                    return "gasBeep3.wav";
                case <= 0.3f:
                    return "gasBeep4.wav";
                case <= 0.4f:
                    return "gasBeep5.wav";
                case <= 0.5f:
                    return "gasBeep6.wav";
                case > 0.5f:
                    return "gasBeep7.wav";
                default: 
                    return null;
            }
        }

        public static string[] GetGeigerClip(float radLevel)
        {
            switch (radLevel)
            {
                case <= 0f:
                    return null;
                case <= 0.01f:
                    return new string[] { "geiger1.wav", "geiger1_1.wav" };
                case <= 0.025f:
                    return new string[] { "geiger2.wav", "geiger2_1.wav", "geiger1_1.wav", "geiger1.wav" };
                case <= 0.05f:
                    return new string[] { "geiger3.wav", "geiger3_1.wav", "geiger2.wav", "geiger2_1.wav" };
                case <= 0.075f:
                    return new string[] { "geiger4.wav", "geiger4_1.wav", "geiger3.wav", "geiger3_1.wav" };
                case <= 0.1f:
                    return new string[] { "geiger5.wav", "geiger5_1.wav", "geiger5_2.wav", "geiger4.wav", "geiger4_1.wav" };
                case <= 0.15f:
                    return new string[] { "geiger6.wav", "geiger6_1.wav", "geiger6_2.wav", "geiger5.wav", "geiger5_1.wav", "geiger5_2.wav" };
                case <= 0.2f:
                    return new string[] { "geiger7.wav" };
                case <= 0.25f:
                    return new string[] { "geiger8.wav", "geiger7.wav" };
                case <= 0.3f:
                    return new string[] { "geiger9.wav" };
                case > 0.3f:
                    return new string[] { "geiger10.wav", "geiger9.wav" };
                default:
                    return null;
            }
        }

        public static void PlayGasAnalyserClips(Player player, PlayerHazardBridge bridge)
        {
            string clip = GetGasAnalsyerClip(bridge.GasAmount);
            if (clip == null) return;
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentGasClipLength = audioClip.length;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, BaseDeviceVolume, EOcclusionTest.None, null, false);
        }

        public static void PlayGeigerClips(Player player, PlayerHazardBridge bridge)
        {
            string[] clips = GetGeigerClip(bridge.RadAmount);
            if (clips == null) return;
            int rndNumber = UnityEngine.Random.Range(0, clips.Length);
            Utils.Logger.LogWarning("rndNumber " + rndNumber);
            Utils.Logger.LogWarning("clips.Length " + clips.Length);
            Utils.Logger.LogWarning("clip " + clips[rndNumber]);
            string clip = clips[rndNumber];
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentGeigerClipLength = audioClip.length;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, BaseDeviceVolume, EOcclusionTest.None, null, false);
        }
    }
}
