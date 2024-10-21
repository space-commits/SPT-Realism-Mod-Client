using EFT.Weather;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public class RealismWeatherController : MonoBehaviour
    {
        private static FieldInfo FogField = AccessTools.Field(typeof(WeatherDebug), "Fog");
        private static FieldInfo LighteningThunderField = AccessTools.Field(typeof(WeatherDebug), "LightningThunderProbability");
        private static FieldInfo RainField = AccessTools.Field(typeof(WeatherDebug), "Rain");
        private static FieldInfo TemperatureField = AccessTools.Field(typeof(WeatherDebug), "Temperature");

        private WeatherController wc;
        public float TargetCloudDensity { get; set; }
        public float TargetWindMagnitude { get; set; }
        public float TargetFog { get; set; }
        public float TargetLighteningThunder { get; set; }
        public float TargetRain { get; set; }
        public Vector2 TargetTopWindDirection { get; set; }
        public WeatherDebug.Direction TargetWindDirection { get; set; }

        private bool ModifyWeather
        { 
            get 
            {
                return (Plugin.ModInfo.IsPreExplosion && GameWorldController.IsRightDateForExp) || GameWorldController.DidExplosionClientSide || GameWorldController.DoMapGasEvent || GameWorldController.DoMapRads;
            } 
        }

        private float _elapsedTime = 0f;
        private float _gasFogTimer = 0f;
        private float _radRainTimer = 0f;
        private float _targetGasStrength = 0.05f;
        private float _targetGasCloudStrength = 0.4f;
        private float _radRainStrength = 0.15f;

        void Awake()
        {
        }

        //do not run on labs or factory
        void Update() 
        {
            if (!GameWorldController.MapWithDynamicWeather || !GameWorldController.GameStarted || !ModifyWeather) return;
            if (wc == null) wc = WeatherController.Instance; //keep trying to get instance
            if (wc != null)
            {
                if (Plugin.ModInfo.IsPreExplosion && !GameWorldController.DidExplosionClientSide && GameWorldController.IsRightDateForExp) DoPreExplosionWeather();
                else if (GameWorldController.DidExplosionClientSide) DoExplosionWeather();
                else if (GameWorldController.DoMapGasEvent) DoMapGasEventWeather();
                else if (GameWorldController.DoMapRads) DoMapRadWeather();

                wc.WeatherDebug.Enabled = true;
                wc.WeatherDebug.CloudDensity = TargetCloudDensity;
                wc.WeatherDebug.WindMagnitude = TargetWindMagnitude;
                wc.WeatherDebug.TopWindDirection = TargetTopWindDirection;
                wc.WeatherDebug.WindDirection = TargetWindDirection;
                FogField.SetValue(wc.WeatherDebug, TargetFog);
                LighteningThunderField.SetValue(wc.WeatherDebug, TargetLighteningThunder);
                RainField.SetValue(wc.WeatherDebug, TargetRain);
            }       
        }

        private void DoMapGasEventWeather() 
        {
            _gasFogTimer += Time.deltaTime;

            if (_gasFogTimer >= 200f)
            {
                _targetGasStrength = UnityEngine.Random.Range(0.02f, 0.08f);
                _targetGasCloudStrength = UnityEngine.Random.Range(0f, 0.6f);
                _gasFogTimer = 0f;
            }

            TargetRain = Mathf.Lerp(TargetRain, 0f, 0.025f * Time.deltaTime);
            TargetFog = Mathf.Lerp(TargetFog, _targetGasStrength, 0.05f * Time.deltaTime);
            TargetCloudDensity = Mathf.Lerp(TargetCloudDensity, _targetGasCloudStrength, 0.05f * Time.deltaTime);
            TargetLighteningThunder = Mathf.Lerp(TargetLighteningThunder, 0f, 0.1f * Time.deltaTime);
            TargetWindMagnitude = Mathf.Lerp(TargetWindMagnitude, -0.1f, 0.05f * Time.deltaTime);
        }

        private void DoMapRadWeather()
        {
            _radRainTimer += Time.deltaTime;

            if (_radRainTimer >= 200f)
            {
                _radRainStrength = Mathf.Max(0, UnityEngine.Random.Range(-0.2f, 0.65f));
                _radRainTimer = 0f;
            }

            float cloudStrength = Mathf.Max(_radRainStrength * 1.25f, 0.25f);

            TargetRain = Mathf.Lerp(TargetRain, _radRainStrength, 0.05f * Time.deltaTime);
            TargetFog = Mathf.Lerp(TargetFog, _radRainStrength * 0.025f, 0.025f * Time.deltaTime);
            TargetCloudDensity = Mathf.Lerp(TargetCloudDensity, cloudStrength, 0.05f * Time.deltaTime);
            TargetWindMagnitude = Mathf.Lerp(TargetWindMagnitude, 0f, 0.05f * Time.deltaTime);
        }

        private void DoExplosionWeather()
        {
            float delay = 200f;
            _elapsedTime += Time.deltaTime;

            TargetWindDirection = WeatherDebug.Direction.South;
            TargetTopWindDirection = Vector2.up;

            if (_elapsedTime >= delay)
            {
                TargetRain = Mathf.Lerp(TargetRain, 2f, 0.025f * Time.deltaTime);
                TargetFog = Mathf.Lerp(TargetFog, 0.075f, 0.025f * Time.deltaTime);
                TargetCloudDensity = Mathf.Lerp(TargetCloudDensity, 1f, 0.025f * Time.deltaTime);
                TargetLighteningThunder = Mathf.Lerp(TargetLighteningThunder, 1f, 0.1f * Time.deltaTime);
                TargetWindMagnitude = Mathf.Lerp(TargetWindMagnitude, 0.1f, 0.05f * Time.deltaTime);
            }
            else if (_elapsedTime >= 10f && _elapsedTime < delay)
            {
                TargetRain = Mathf.Lerp(TargetRain, 0f, 0.05f * Time.deltaTime);
                TargetFog = Mathf.Lerp(TargetFog, 0f, 0.05f * Time.deltaTime);
                TargetCloudDensity = Mathf.Lerp(TargetCloudDensity, -0.75f, 0.25f * Time.deltaTime);
                TargetWindMagnitude = Mathf.Lerp(TargetWindMagnitude, 1.35f, 0.25f * Time.deltaTime);
            }
       
        }

        private void DoPreExplosionWeather()
        {
            TargetCloudDensity = 0.3f;
            TargetFog = 0.01f;
            TargetRain = 0.15f;
            TargetWindMagnitude = 0;
            TargetLighteningThunder = 0;
            TargetWindDirection = WeatherDebug.Direction.East;
            TargetTopWindDirection = Vector2.down;
        }
    }
}
