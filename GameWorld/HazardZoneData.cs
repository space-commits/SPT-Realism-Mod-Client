using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;
using EFT.Game.Spawning;
using UnityEngine.Profiling;

namespace RealismMod
{
    public static class Assets
    {
        //hazard assets
        public static UnityEngine.Object GooBarrel { get; set; }
        public static UnityEngine.Object BlueBox { get; set; }
        public static UnityEngine.Object RedForkLift { get; set; }
        public static UnityEngine.Object ElectroForkLift { get; set; }
        public static UnityEngine.Object BigForkLift { get; set; }
        public static UnityEngine.Object LabsCrate { get; set; }
        public static UnityEngine.Object LabsCrateSmall { get; set; }
        public static UnityEngine.Object Ural { get; set; }
        public static UnityEngine.Object BluePallet { get; set; }
        public static UnityEngine.Object BlueFuelPalletCloth { get; set; }
        public static UnityEngine.Object BarrelPile { get; set; }
        public static UnityEngine.Object YellowPlasticBarrel { get; set; }
        public static UnityEngine.Object YellowPlasticPallet { get; set; }
        public static UnityEngine.Object LabsSuit1 { get; set; }
        public static UnityEngine.Object LabsSuit2 { get; set; }
    }

    public static class ZoneLoot 
    {
        public static Dictionary<string, int> LowTier = new Dictionary<string, int>
        {
             {"5672cb724bdc2dc2088b456b", 50 }, //geiger
             {"590a3efd86f77437d351a25b", 50 }, //gas analyser
             {"590c595c86f7747884343ad7", 50 }, //gas mask filter
             {"5b432c305acfc40019478128", 50 }, //gp-5 mask    
             {"60363c0c92ec1c31037959f5", 50 }, //gp-7 mask
             {"5d1b3a5d86f774252167ba22", 50 }, //piles of meds
             {"590c2e1186f77425357b6124", 40 }, //toolset
             {"590c645c86f77412b01304d9", 30 }, //diary
             {"590c651286f7741e566b6461", 30 }, //slim diary
             {"5755356824597772cb798962", 50 }, //ai-2
        };

        public static Dictionary<string, int> MidTier = new Dictionary<string, int>
        {
             {"619cbfeb6b8a1b37a54eebfa", 30 }, //cable cutter
             {"63a0b208f444d32d6f03ea1e", 5 }, //sledge hammer
             {"619cbfccbedcde2f5b3f7bdd", 30 }, //pipe wrench
             {"638e0752ab150a5f56238962", 30 }, //intel folder
             {"6389c8fb46b54c634724d847", 15 }, //circuts book
             {"62a0a124de7ac81993580542", 25 }, //topographic
             {"637b6251104668754b72f8f9", 30 }, //blue blood
             {"5fca138c2a7b221b2852a5c6", 30 }, //antidote
             {"5ed51652f6c34d2cc26336a1", 30 }, //mule
             {"5c0e530286f7747fa1419862", 30 }, //propitol
             {"5fca13ca637ee0341a484f46", 30 }, //sj9
             {"637b612fb7afa97bfc3d7005", 30 }, //sj12
             {"5780cf7f2459777de4559322", 10 }, //customs marked,
             {"5d80c60f86f77440373c4ece", 10 }, //rb-bk marked,
             {"5ede7a8229445733cb4c18e2", 10 }, //rb-pkpm,
             {"5d80c62a86f7744036212b3f", 10 }, //rb-vo,
             {"63a3a93f8a56922e82001f5d", 10 }, //abandoned marked
             {"64ccc25f95763a1ae376e447", 10 }, //mysterious marked
             {"64d4b23dc1b37504b41ac2b6", 10 }, //rusted marked
        };

        public static Dictionary<string, int> HighTier = new Dictionary<string, int>
        {
             {"5c94bbff86f7747ee735c08f", 50 }, //labs keys
             {"5c1d0d6d86f7744bb2683e1f", 10 }, //labs yellow
             {"5c1e495a86f7743109743dfb", 10 }, //labs violet
             {"5c1d0c5f86f7744bb2683cf0", 10 }, //labs blue
             {"6389c8c5dbfd5e4b95197e6b", 10 }, //blue folders
             {"5c1d0f4986f7744bb01837fa", 1 }, //labs black
             {"5c1d0dc586f7744baf2e7b79", 1 }, //labs green
             {"5c1d0efb86f7744baf2e7b7b", 1 }, //labs red
             {"5c1e2a1e86f77431ea0ea84c", 30 }, //labs manager
             {"5c1e2d1f86f77431e9280bee", 25 }, // labs weapon testing
             {"5c1f79a086f7746ed066fb8f", 40 }, //labs arsenl key
        };
    }

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

    public class Zone
    {
        public string Name { get; set; }
        public float Strength { get; set; }
        public bool BlockNav { get; set; }
        public Position Position { get; set; }
        public Rotation Rotation { get; set; }
        public Size Size { get; set; }
    }

    public class Loot 
    {
        public string Type { get; set; }
        public int Odds { get; set; }
        public bool RandomizeRotation { get; set; }
        public Position Position { get; set; }
        public Rotation Rotation { get; set; }
    }

    public class HazardLocation
    {
        public float SpawnChance { get; set; }
        public List<Zone> Zones { get; set; }
        public List<Asset> Assets { get; set; }
        public List<Loot> Loot { get; set; }
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

    public static class HazardZoneData
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

        public static void DeserializeZoneData()
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
