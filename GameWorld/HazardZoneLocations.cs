using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;
using EFT.Game.Spawning;
using UnityEngine.Profiling;

namespace RealismMod
{
    public class Position
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Rotation
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Size
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Asset
    {
        public string AssetName { get; set; }
        public string Type { get; set; }
        public int Odds { get; set; }
        public bool RandomizeRotation { get; set; }
        public Position Position { get; set; }
        public Rotation Rotation { get; set; }
    }

    public class HazardLocation
    {
        public string Name { get; set; }
        public float SpawnChance { get; set; }
        public float Strength { get; set; }
        public bool BlockNav { get; set; }
        public Position Position { get; set; }
        public Rotation Rotation { get; set; }
        public Size Size { get; set; }
        public List<Asset> Assets { get; set; }
    }

    public interface ZoneCollection 
    {
       public EZoneType ZoneType { get; }
        public List<HazardLocation> Factory { get; set; }
        public List<HazardLocation> Customs { get; set; }
        public List<HazardLocation> GZ { get; set; }
        public List<HazardLocation> Shoreline { get; set; }
        public List<HazardLocation> Streets { get; set; }
        public List<HazardLocation> Labs { get; set; }
        public List<HazardLocation> Interchange { get; set; }
        public List<HazardLocation> Lighthouse { get; set; }
        public List<HazardLocation> Woods { get; set; }
        public List<HazardLocation> Reserve { get; set; }
    }

    public class RadAssetZones : ZoneCollection
    {
        public EZoneType ZoneType { get; } = EZoneType.RadAssets;

        [JsonProperty("FactoryAssetZones")]
        public List<HazardLocation> Factory { get; set; }

        [JsonProperty("CustomsAssetZones")]
        public List<HazardLocation> Customs { get; set; }

        [JsonProperty("GZAssetZones")]
        public List<HazardLocation> GZ { get; set; }

        [JsonProperty("ShorelineAssetZones")]
        public List<HazardLocation> Shoreline { get; set; }

        [JsonProperty("StreetsAssetZones")]
        public List<HazardLocation> Streets { get; set; }

        [JsonProperty("LabsAssetZones")]
        public List<HazardLocation> Labs { get; set; }

        [JsonProperty("InterchangeAssetZone")]
        public List<HazardLocation> Interchange { get; set; }

        [JsonProperty("LighthouseAssetZones")]
        public List<HazardLocation> Lighthouse { get; set; }

        [JsonProperty("WoodsAssetZones")]
        public List<HazardLocation> Woods { get; set; }

        [JsonProperty("ReserveAssetZones")]
        public List<HazardLocation> Reserve { get; set; }
    }

    public class RadZones: ZoneCollection 
    {
        public EZoneType ZoneType { get; } = EZoneType.Radiation;

        [JsonProperty("FactoryRadZones")]
        public List<HazardLocation> Factory { get; set; }

        [JsonProperty("CustomsRadZones")]
        public List<HazardLocation> Customs { get; set; }

        [JsonProperty("GZRadZones")]
        public List<HazardLocation> GZ { get; set; }

        [JsonProperty("ShorelineRadZones")]
        public List<HazardLocation> Shoreline { get; set; }

        [JsonProperty("StreetsRadZones")]
        public List<HazardLocation> Streets { get; set; }

        [JsonProperty("LabsRadZones")]
        public List<HazardLocation> Labs { get; set; }

        [JsonProperty("InterchangeRadZone")]
        public List<HazardLocation> Interchange { get; set; }

        [JsonProperty("LighthouseRadZones")]
        public List<HazardLocation> Lighthouse { get; set; }

        [JsonProperty("WoodsRadZones")]
        public List<HazardLocation> Woods { get; set; }

        [JsonProperty("ReserveRadZones")]
        public List<HazardLocation> Reserve { get; set; }
    }

    public class GasZones: ZoneCollection
    {
        public EZoneType ZoneType { get; } = EZoneType.Gas;

        [JsonProperty("FactoryGasZones")]
        public List<HazardLocation> Factory { get; set; }

        [JsonProperty("CustomsGasZones")]
        public List<HazardLocation> Customs { get; set; }

        [JsonProperty("GZGasZones")]
        public List<HazardLocation> GZ { get; set; }

        [JsonProperty("ShorelineGasZones")]
        public List<HazardLocation> Shoreline { get; set; }

        [JsonProperty("StreetsGasZones")]
        public List<HazardLocation> Streets { get; set; }

        [JsonProperty("LabsGasZones")]
        public List<HazardLocation> Labs { get; set; }

        [JsonProperty("InterchangeGasZones")]
        public List<HazardLocation> Interchange { get; set; }

        [JsonProperty("LighthouseGasZones")]
        public List<HazardLocation> Lighthouse { get; set; }

        [JsonProperty("WoodsGasZones")]
        public List<HazardLocation> Woods { get; set; }

        [JsonProperty("ReserveGasZones")]
        public List<HazardLocation> Reserve{ get; set; }
    }

    public static class HazardZoneLocations
    {
        public static GasZones GasZoneLocations;
        public static RadZones RadZoneLocations;
        public static RadAssetZones RadAssetZoneLocations;

        private static T DeserializeHazardZones<T>(string file) where T : class
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\data\\zones\\" + file + ".json";
            string jsonString = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public static void InitZones()
        {
            GasZoneLocations = DeserializeHazardZones<GasZones>("gas_zones");
            RadZoneLocations = DeserializeHazardZones<RadZones>("rad_zones");
            RadAssetZoneLocations = DeserializeHazardZones<RadAssetZones>("rad_asset_zones");
        }

        public static List<HazardLocation> GetZones(EZoneType zoneType, string map)
        {
            ZoneCollection zones =
                zoneType == EZoneType.RadAssets ? zones = RadAssetZoneLocations :
                zoneType == EZoneType.Radiation ? zones = RadZoneLocations :
                zones = GasZoneLocations;

            switch (map)
            {
                case "rezervbase":
                    return zones.Reserve;
                case "bigmap":
                    return zones.Customs;
                case "factory4_night":
                case "factory4_day":
                    return zones.Factory;
                case "interchange":
                    return zones.Interchange;
                case "laboratory":
                    return zones.Labs;
                case "shoreline":
                    return zones.Shoreline;
                case "sandbox":
                case "sandbox_high":
                    return zones.GZ;
                case "woods":
                    return zones.Woods;
                case "lighthouse":
                    return zones.Lighthouse;
                case "tarkovstreets":
                    return zones.Streets;
                default:
                    return null;
            }
        }
    }
}
