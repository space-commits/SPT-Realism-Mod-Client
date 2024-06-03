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
        private const float _baseDeviceVolume = 0.2f;
        private const float delay = 5f;
        private static float _currentDeviceClipLength = 0f;
        private static float _deviceTimer = 0f;

        private static float GetDelayTime() 
        {
            if (Plugin.RealHealthController.PlayerHazardBridge == null) return 4f;
            return delay * (1f - Plugin.RealHealthController.PlayerHazardBridge.GasAmount);
        }

        public static void DeviceAudioController()
        {
            if (GearController.HasGasAnalyser) 
            {
                _deviceTimer += Time.deltaTime;

                if (_deviceTimer > _currentDeviceClipLength && _deviceTimer >= GetDelayTime())
                {
                    Player player = Utils.GetYourPlayer();
                    PlayerHazardBridge bridge = Plugin.RealHealthController.PlayerHazardBridge;
                    if (bridge != null && bridge.IsInGasZone)
                    {
                        PlayGasAnalyserClips(player, bridge);
                        _deviceTimer = 0f;
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

        public static void PlayGasAnalyserClips(Player player, PlayerHazardBridge bridge)
        {

            string clip = GetGasAnalsyerClip(bridge.GasAmount);
            if (clip == null) return;
            AudioClip audioClip = Plugin.DeviceAudioClips[clip];
            _currentDeviceClipLength = audioClip.length;
            Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), audioClip, 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, _baseDeviceVolume, EOcclusionTest.None, null, false);
        }
    }
}
