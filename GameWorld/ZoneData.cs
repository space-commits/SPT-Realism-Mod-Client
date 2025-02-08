using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System;
using EFT.Game.Spawning;
using UnityEngine.Profiling;
using EFT;
using Comfort.Common;

namespace RealismMod
{
    public enum EZoneType
    {
        Radiation,
        Gas,
        RadAssets,
        GasAssets,
        SafeZone,
        Quest,
        Interactable
    }

    public enum EIneractableType
    {
        None,
        Valve,
        Button
    }

    public enum EIneractableAction
    {
        None,
        On,
        Off
    }

    public enum EInteractableState
    {
        None,
        Random,
        On,
        Off
    }

    public static class Assets
    {
        //hazard assets
        public static AssetBundle GooBarrelBundle { get; set; }
        public static AssetBundle BlueBoxBundle { get; set; }
        public static AssetBundle RedForkLiftBundle { get; set; }
        public static AssetBundle ElectroForkLiftBundle { get; set; }
        public static AssetBundle LabsCrateBundle { get; set; }
        public static AssetBundle LabsCrateSmallBundle { get; set; }
        public static AssetBundle UralBundle { get; set; }
        public static AssetBundle BluePalletBundle { get; set; }
        public static AssetBundle BlueFuelPalletClothBundle { get; set; }
        public static AssetBundle BarrelPileBundle { get; set; }
        public static AssetBundle WhitePlasticPalletBundle { get; set; }
        public static AssetBundle YellowPlasticPalletBundle { get; set; }
        public static AssetBundle MetalFenceBundle { get; set; }
        public static AssetBundle RedContainerBundle { get; set; }
        public static AssetBundle BlueContainerBundle { get; set; }
        public static AssetBundle LabsBarrelPileBundle { get; set; }
        public static AssetBundle RadSign1 { get; set; }
        public static AssetBundle TerraGroupFence { get; set; }
        public static AssetBundle FogBundle { get; set; }
        public static AssetBundle GasBundle { get; set; }
        public static AssetBundle ExplosionBundle { get; set; }
    }

    //alt spawn locations if playe/bot spawns in a hazard zone
    public static class SafeSpawns
    {
        public static IEnumerable<Vector3> CustomsSpawns = new Vector3[]
        {
          new Vector3(436f, 2f, -63f),
          new Vector3(558f, 2f, -99f),
          new Vector3(336f, 1.6f, -17.3f),
          new Vector3(347.4f, 1.6f, -178.6f),
          new Vector3(199f, 1.6f, -203f),
          new Vector3(536.1f, 0.8f, 11.3f),
          new Vector3(372.6f, 1f, 46.1f),
          new Vector3(158.1f, 0f, 193.9f),
          new Vector3(61.1f, 1.6f, -173.8f),
          new Vector3(-168.8f, 1.3f, -67f),
          new Vector3(-228.8f, 1.7f, -220f),
          new Vector3(587.7991f,1.5f,-131.4845f),
          new Vector3(630.0674f,-0.1f,-88.28856f),
          new Vector3(468.2215f,1.6f,-108.095f),
          new Vector3(335.3215f,1.5f,-19.49973f),
          new Vector3(243.4649f,1.5f,-49.10956f),
          new Vector3(153.8941f,-1,165.7909f),
          new Vector3(241.8438f,-0.2f,178.0606f),
          new Vector3(76.565f,1.5f,-51.10367f),
          new Vector3(-176.27f, 1.2f, -77.69751f)
        };

        public static IEnumerable<Vector3> GZSpawns = new Vector3[]
        {
          new Vector3(28.5f, 24.3f, -51.22f),
          new Vector3(30.3f, 23.4f, 147.9f),
          new Vector3(106.1f, 14.3f, 56.55f),
          new Vector3(58.4f, 23.9f, 227.85f),
          new Vector3(148.5946f, 23f, 224.3181f),
          new Vector3(36.00006f, 23f, 322.1284f),
          new Vector3(120.5057f, 25f, 141.3855f),
          new Vector3(126.002f, 24.9f, 110.8803f),
          new Vector3(175.0823f, 17.5f, 28.37658f),
          new Vector3(104.1584f, 17.5f, 176.5322f),
          new Vector3(49.54675f, 23.9f, 158.1759f),
          new Vector3(98.86556f, 21.6f, -66.60643f)
        };

        public static IEnumerable<Vector3> InterchangeSpawns = new Vector3[]
        {
          new Vector3(-45.7f, 27.5f, 12.1f),
          new Vector3(0.33f, 27.5f, -199.1f),
          new Vector3(-165.7f, 22.2f, -307.5f),
          new Vector3(-161.6f, 21.7f, 199.9f),
          new Vector3(34.6f, 27.5f, 112.7f),

        };

        public static IEnumerable<Vector3> ReserveSpawns = new Vector3[]
        {
          new Vector3(195.2f, -6.5f, -142.3f),
          new Vector3(-61.8f, -6.5f, -42.6f),
          new Vector3(-137.1f, -5.5f, -8.8f),
          new Vector3(-172f, -5.7f, 40.5f),
          new Vector3(-116.4f, -5.7f, 99.3f),
          new Vector3(-72.5f, -10.3f, 29.8f),
          new Vector3(56.4f, -6.12f, -5.1f),
          new Vector3(-11f, 19.4f, 170f),
          new Vector3(-144.3f, -8.9f, -23.8f),

        };

        public static IEnumerable<Vector3> ShorelineSpawns = new Vector3[]
        {
          new Vector3(271.3f, -59.1f, 299.4f),
          new Vector3(354f, -55.8f, 158.2f),
          new Vector3(52.2f, -46.2f, 138.3f),
          new Vector3(-243.1f, -40.7f, 135.7f),
          new Vector3(-168.6f, -40.9f, 172.6f),
          new Vector3(-358.9f, -4.6f, -95.5f),
          new Vector3(-267f, -4.6f, -159.7f),
          new Vector3(-190f, -15.4f, -294.9f),
          new Vector3(-658.1f, -26.4f, -155.7f),
          new Vector3(-471.5f, -25.4f, 272f),
          new Vector3(-185.1f, -62.2f, 437.6f),
          new Vector3(-394f, -63.2f, 543.3f)
        };

        public static IEnumerable<Vector3> WoodsSpawns = new Vector3[]
        {
          new Vector3(-168.7f, -1.4f, 243f),
          new Vector3(-191.7f, -1.4f, 199f),
          new Vector3(110.7f, -3.4f, 8f),
          new Vector3(-27.6f, -3.1f, 41.2f),
          new Vector3(-2.2f, -1.2f, -86.4f),
          new Vector3(-507f, 15.5f, -419.3f),
          new Vector3(-448f, 15.5f, -373f),
          new Vector3(-87f, 8.9f, -670f),
          new Vector3(170.6f, 23.7f, -726f),
          new Vector3(243.3f, 24.9f, -713f),
          new Vector3(262.5f, 23.2f, -418.4f),
          new Vector3(220.4f, -4.2f, 40f)
        };

        public static IEnumerable<Vector3> LighthouseSpawns = new Vector3[]
        {
          new Vector3(41.5f, 5.1f, -665.7f),
          new Vector3(-149.7f, 5.1f, -743f),
          new Vector3(-185f, 5.1f, -621.4f),
          new Vector3(-68f, 19.7f, -125f),
          new Vector3(-70f, 27.3f, 127.2f)
        };

        public static IEnumerable<Vector3> SteetsSpawns = new Vector3[]
        {
          new Vector3(-86.4f, 0.2f, 410.2f),
          new Vector3(-96.6f, 2.6f, 297.5f),
          new Vector3(-158.7f, 2.6f, 281.4f),
          new Vector3(205.2f, 0.9f, 212.3f),
          new Vector3(249.6f, 3.8f, 347.16f),
          new Vector3(42.4f, 0.75f, -64.36f),

        };

        public static IEnumerable<Vector3> FactorySpawns = new Vector3[]
        {
          new Vector3(29.8f, 0.7f, 8.3f),
          new Vector3(-21.1f, 0.6f, 23.5f),
          new Vector3(30.4f, 0.7f, 14.8f),
          new Vector3(15.1f, 5f, 35.9f),

        };

        public static IEnumerable<Vector3> LabsSpawns = new Vector3[]
        {
          new Vector3(-179f, 0.5f, -322.6f),
          new Vector3(-144f, 0.5f, -386.9f),
          new Vector3(-204.1818f, 0.5f, -391.2065f),
          new Vector3(-153.6401f, 0.5f, -395.4791f),
          new Vector3(-256.8109f, 0.5f, -308.5962f),
          new Vector3(-242.5417f, 0.5f, -297.6328f),
          new Vector3(-196.8698f, 0.5f, -323.7413f),
          new Vector3(-126.1994f, 0.5f, -382.8971f)
        };
    }

    public static class DynamicRadZoneLoot 
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
             {"590c645c86f77412b01304d9", 20 }, //diary
             {"590c651286f7741e566b6461", 20 }, //slim diary
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
             {"66fd57171f981640e667fbe2", 50 }, //rad zample
             {"66fd588956f73c4f38dd07ae", 40 } //tox zample
        };

        public static Dictionary<string, int> HighTier = new Dictionary<string, int>
        {
             {"5c94bbff86f7747ee735c08f", 50 }, //labs keys
             {"5c1d0d6d86f7744bb2683e1f", 10 }, //labs yellow
             {"5c1e495a86f7743109743dfb", 10 }, //labs violet
             {"5c1d0c5f86f7744bb2683cf0", 10 }, //labs blue
             {"6389c8c5dbfd5e4b95197e6b", 10 }, //blue folders
             {"5c1d0f4986f7744bb01837fa", 2 }, //labs black
             {"5c1d0dc586f7744baf2e7b79", 2 }, //labs green
             {"5c1d0efb86f7744baf2e7b7b", 2 }, //labs red
             {"5c1e2a1e86f77431ea0ea84c", 30 }, //labs manager
             {"5c1e2d1f86f77431e9280bee", 25 }, // labs weapon testing
             {"5c1f79a086f7746ed066fb8f", 40 }, //labs arsenl key
             {"66fd57171f981640e667fbe2", 100 }, //rad zample
             {"66fd588956f73c4f38dd07ae", 40 }, //tox zample
             {"6389c7f115805221fb410466", 10 } //far forward gps
        };
    }

    public interface ZoneCollection
    {
        public EZoneType ZoneType { get; set; }
        public List<HazardGroup> Factory { get; set; }
        public List<HazardGroup> Customs { get; set; }
        public List<HazardGroup> GZ { get; set; }
        public List<HazardGroup> Shoreline { get; set; }
        public List<HazardGroup> Streets { get; set; }
        public List<HazardGroup> Labs { get; set; }
        public List<HazardGroup> Interchange { get; set; }
        public List<HazardGroup> Lighthouse { get; set; }
        public List<HazardGroup> Woods { get; set; }
        public List<HazardGroup> Reserve { get; set; }
    }

    public class HazardGroup
    {
        public bool IsTriggered { get; set; }
        public float SpawnChance { get; set; }
        public string QuestToEnable { get; set; }
        public string QuestToBlock { get; set; }
        public InteractableGroup InteractableGroup { get; set; }
        public List<Zone> Zones { get; set; }
        public List<Asset> Assets { get; set; }
        public List<Loot> Loot { get; set; }
        public List<string> AudioFiles { get; set; }
    }

    public class Zone
    {
        public string Name { get; set; }
        public float Strength { get; set; }
        public bool UsesDistanceFalloff { get; set; }
        public bool BlockNav { get; set; }
        public Analysable Analysable { get; set; }
        public InteractableSubZone Interactable { get; set; }
        public string AudioFile { get; set; }
        public Position Position { get; set; }
        public Rotation Rotation { get; set; }
        public Size Size { get; set; }
        public bool UseVisual { get; set; } = true;
        public float VisParticleRate { get; set; } = 40f;
        public float VisOpacityModi { get; set; } = 1f;
        public float VisSpeedModi { get; set; } = 1f;
        public float VisZoneSizeMulti { get; set; } = 0.85f;
        public bool VisUsePhysics { get; set; } = false;
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

    public class Loot
    {
        public string Type { get; set; }
        public Dictionary<string, int> LootOverride { get; set; }
        public int Odds { get; set; }
        public bool RandomizeRotation { get; set; }
        public Position Position { get; set; }
        public Rotation Rotation { get; set; }
    }

    public class Analysable 
    {
        public bool NoRequirement { get; set; }
        public string[] EnabledBy { get; set; }
        public string[] DisabledBy { get; set; }
    }

    public class InteractableZoneTargets
    {
        public string[] ZoneNames { get; set; }
        public string InteractableName { get; set; } //the interactable responsible for affecting it partially
        public float PartialCompletionModifier { get; set; } //modifer for when the primary interactable is triggered
        public float FullCompletiionModifer { get; set; } = 0f;
        public EInteractableState DesiredEndState { get; set; } = 0f;
    }

    public class InteractableGroup
    {
        public string[] EnabledBy { get; set; }
        public string[] DisabledBy { get; set; }
        public InteractableZoneTargets[] TargtetZones { get; set; }
    }

    public class InteractableSubZone
    {
        public EIneractableType InteractionType { get; set; }
        public int CompletionStep { get; set; } = 0; //what step this interactable is if order of completion is needed
        public EInteractableState StartingState { get; set; } = EInteractableState.On;
        public string[] TargeObjects { get; set; }
        public EIneractableAction[] InteractionAction { get; set; }
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

    public class UserZones : ZoneCollection
    {
        public EZoneType ZoneType { get; set; } = EZoneType.SafeZone;

        [JsonProperty("FactoryUserZones")]
        public List<HazardGroup> Factory { get; set; }

        [JsonProperty("CustomsUserZones")]
        public List<HazardGroup> Customs { get; set; }

        [JsonProperty("GZUserZones")]
        public List<HazardGroup> GZ { get; set; }

        [JsonProperty("ShorelineUserZones")]
        public List<HazardGroup> Shoreline { get; set; }

        [JsonProperty("StreetsUserZones")]
        public List<HazardGroup> Streets { get; set; }

        [JsonProperty("LabsUserZones")]
        public List<HazardGroup> Labs { get; set; }

        [JsonProperty("InterchangeUserZone")]
        public List<HazardGroup> Interchange { get; set; }

        [JsonProperty("LighthouseUserZones")]
        public List<HazardGroup> Lighthouse { get; set; }

        [JsonProperty("WoodsUserZones")]
        public List<HazardGroup> Woods { get; set; }

        [JsonProperty("ReserveUserZones")]
        public List<HazardGroup> Reserve { get; set; }
    }

    public class SafeZones : ZoneCollection
    {
        public EZoneType ZoneType { get; set; } = EZoneType.SafeZone;

        [JsonProperty("FactorySafeZones")]
        public List<HazardGroup> Factory { get; set; }

        [JsonProperty("CustomsSafeZones")]
        public List<HazardGroup> Customs { get; set; }

        [JsonProperty("GZSafeZones")]
        public List<HazardGroup> GZ { get; set; }

        [JsonProperty("ShorelineSafeZones")]
        public List<HazardGroup> Shoreline { get; set; }

        [JsonProperty("StreetsSafeZones")]
        public List<HazardGroup> Streets { get; set; }

        [JsonProperty("LabsSafeZones")]
        public List<HazardGroup> Labs { get; set; }

        [JsonProperty("InterchangeSafeZone")]
        public List<HazardGroup> Interchange { get; set; }

        [JsonProperty("LighthouseSafeZones")]
        public List<HazardGroup> Lighthouse { get; set; }

        [JsonProperty("WoodsSafeZones")]
        public List<HazardGroup> Woods { get; set; }

        [JsonProperty("ReserveSafeZones")]
        public List<HazardGroup> Reserve { get; set; }
    }

    public class RadAssetZones : ZoneCollection
    {
        public EZoneType ZoneType { get; set; } = EZoneType.RadAssets;

        [JsonProperty("FactoryAssetZones")]
        public List<HazardGroup> Factory { get; set; }

        [JsonProperty("CustomsAssetZones")]
        public List<HazardGroup> Customs { get; set; }

        [JsonProperty("GZAssetZones")]
        public List<HazardGroup> GZ { get; set; }

        [JsonProperty("ShorelineAssetZones")]
        public List<HazardGroup> Shoreline { get; set; }

        [JsonProperty("StreetsAssetZones")]
        public List<HazardGroup> Streets { get; set; }

        [JsonProperty("LabsAssetZones")]
        public List<HazardGroup> Labs { get; set; }

        [JsonProperty("InterchangeAssetZone")]
        public List<HazardGroup> Interchange { get; set; }

        [JsonProperty("LighthouseAssetZones")]
        public List<HazardGroup> Lighthouse { get; set; }

        [JsonProperty("WoodsAssetZones")]
        public List<HazardGroup> Woods { get; set; }

        [JsonProperty("ReserveAssetZones")]
        public List<HazardGroup> Reserve { get; set; }
    }

    public class RadZones: ZoneCollection 
    {
        public EZoneType ZoneType { get; set; } = EZoneType.Radiation;

        [JsonProperty("FactoryRadZones")]
        public List<HazardGroup> Factory { get; set; }

        [JsonProperty("CustomsRadZones")]
        public List<HazardGroup> Customs { get; set; }

        [JsonProperty("GZRadZones")]
        public List<HazardGroup> GZ { get; set; }

        [JsonProperty("ShorelineRadZones")]
        public List<HazardGroup> Shoreline { get; set; }

        [JsonProperty("StreetsRadZones")]
        public List<HazardGroup> Streets { get; set; }

        [JsonProperty("LabsRadZones")]
        public List<HazardGroup> Labs { get; set; }

        [JsonProperty("InterchangeRadZones")]
        public List<HazardGroup> Interchange { get; set; }

        [JsonProperty("LighthouseRadZones")]
        public List<HazardGroup> Lighthouse { get; set; }

        [JsonProperty("WoodsRadZones")]
        public List<HazardGroup> Woods { get; set; }

        [JsonProperty("ReserveRadZones")]
        public List<HazardGroup> Reserve { get; set; }
    }

    public class GasAssetZones : ZoneCollection
    {
        public EZoneType ZoneType { get; set; } = EZoneType.GasAssets;

        [JsonProperty("FactoryAssetZones")]
        public List<HazardGroup> Factory { get; set; }

        [JsonProperty("CustomsAssetZones")]
        public List<HazardGroup> Customs { get; set; }

        [JsonProperty("GZAssetZones")]
        public List<HazardGroup> GZ { get; set; }

        [JsonProperty("ShorelineAssetZones")]
        public List<HazardGroup> Shoreline { get; set; }

        [JsonProperty("StreetsAssetZones")]
        public List<HazardGroup> Streets { get; set; }

        [JsonProperty("LabsAssetZones")]
        public List<HazardGroup> Labs { get; set; }

        [JsonProperty("InterchangeAssetZone")]
        public List<HazardGroup> Interchange { get; set; }

        [JsonProperty("LighthouseAssetZones")]
        public List<HazardGroup> Lighthouse { get; set; }

        [JsonProperty("WoodsAssetZones")]
        public List<HazardGroup> Woods { get; set; }

        [JsonProperty("ReserveAssetZones")]
        public List<HazardGroup> Reserve { get; set; }
    }

    public class GasZones: ZoneCollection
    {
        public EZoneType ZoneType { get; set; } = EZoneType.Gas;

        [JsonProperty("FactoryGasZones")]
        public List<HazardGroup> Factory { get; set; }

        [JsonProperty("CustomsGasZones")]
        public List<HazardGroup> Customs { get; set; }

        [JsonProperty("GZGasZones")]
        public List<HazardGroup> GZ { get; set; }

        [JsonProperty("ShorelineGasZones")]
        public List<HazardGroup> Shoreline { get; set; }

        [JsonProperty("StreetsGasZones")]
        public List<HazardGroup> Streets { get; set; }

        [JsonProperty("LabsGasZones")]
        public List<HazardGroup> Labs { get; set; }

        [JsonProperty("InterchangeGas")]
        public List<HazardGroup> Interchange { get; set; }

        [JsonProperty("LighthouseGasZones")]
        public List<HazardGroup> Lighthouse { get; set; }

        [JsonProperty("WoodsGasZones")]
        public List<HazardGroup> Woods { get; set; }

        [JsonProperty("ReserveGasZones")]
        public List<HazardGroup> Reserve{ get; set; }
    }

    public class QuestZones : ZoneCollection
    {
        public EZoneType ZoneType { get; set; } = EZoneType.Quest;

        [JsonProperty("FactoryQuestZones")]
        public List<HazardGroup> Factory { get; set; }

        [JsonProperty("CustomsQuestZones")]
        public List<HazardGroup> Customs { get; set; }

        [JsonProperty("GZQuestZones")]
        public List<HazardGroup> GZ { get; set; }

        [JsonProperty("ShorelineQuestZones")]
        public List<HazardGroup> Shoreline { get; set; }

        [JsonProperty("StreetsQuestZones")]
        public List<HazardGroup> Streets { get; set; }

        [JsonProperty("LabsQuestZones")]
        public List<HazardGroup> Labs { get; set; }

        [JsonProperty("InterchangeQuestZones")]
        public List<HazardGroup> Interchange { get; set; }

        [JsonProperty("LighthouseQuestZones")]
        public List<HazardGroup> Lighthouse { get; set; }

        [JsonProperty("WoodsQuestZones")]
        public List<HazardGroup> Woods { get; set; }

        [JsonProperty("ReserveQuestZones")]
        public List<HazardGroup> Reserve { get; set; }
    }

    public class InteractionZones : ZoneCollection
    {
        public EZoneType ZoneType { get; set; } = EZoneType.Interactable;

        [JsonProperty("FactoryZones")]
        public List<HazardGroup> Factory { get; set; }

        [JsonProperty("CustomsZones")]
        public List<HazardGroup> Customs { get; set; }

        [JsonProperty("GZZones")]
        public List<HazardGroup> GZ { get; set; }

        [JsonProperty("ShorelineZones")]
        public List<HazardGroup> Shoreline { get; set; }

        [JsonProperty("StreetsZones")]
        public List<HazardGroup> Streets { get; set; }

        [JsonProperty("LabsZones")]
        public List<HazardGroup> Labs { get; set; }

        [JsonProperty("InterchangeZones")]
        public List<HazardGroup> Interchange { get; set; }

        [JsonProperty("LighthouseZones")]
        public List<HazardGroup> Lighthouse { get; set; }

        [JsonProperty("WoodsZones")]
        public List<HazardGroup> Woods { get; set; }

        [JsonProperty("ReserveZones")]
        public List<HazardGroup> Reserve { get; set; }
    }

    public static class ZoneData
    {
        public static SafeZones SafeZoneLocations;
        public static GasZones GasZoneLocations;
        public static GasAssetZones GasAssetZoneLocations;
        public static RadZones RadZoneLocations;
        public static RadAssetZones RadAssetZoneLocations;
        public static QuestZones QuestZoneLocations;
        public static InteractionZones InteractionLocations;

        private static T DeserializeHazardZones<T>(string file) where T : class
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\BepInEx\\plugins\\Realism\\data\\zones\\" + file + ".json";
            string jsonString = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        private static void MergeData(ZoneCollection original, ZoneCollection toBeMerged) 
        {
            if (original == null || toBeMerged == null) return;

            original.Factory.AddRange(toBeMerged.Factory);
            original.Customs.AddRange(toBeMerged.Customs);
            original.GZ.AddRange(toBeMerged.GZ);
            original.Shoreline.AddRange(toBeMerged.Shoreline);
            original.Streets.AddRange(toBeMerged.Streets);
            original.Labs.AddRange(toBeMerged.Labs);
            original.Interchange.AddRange(toBeMerged.Interchange);
            original.Lighthouse.AddRange(toBeMerged.Lighthouse);
            original.Woods.AddRange(toBeMerged.Woods);
            original.Reserve.AddRange(toBeMerged.Reserve);
        }

        public static void DeserializeZoneData()
        {
            SafeZoneLocations = DeserializeHazardZones<SafeZones>("safe_zones");
            var userSafeZones = DeserializeHazardZones<UserZones>("user_safe_zones");
            MergeData(SafeZoneLocations, userSafeZones);

            InteractionLocations = DeserializeHazardZones<InteractionZones>("interactable_zones");

            QuestZoneLocations = DeserializeHazardZones<QuestZones>("quest_zones");
            var userQuestZones = DeserializeHazardZones<UserZones>("user_quest_zones");
            MergeData(QuestZoneLocations, userQuestZones);

            GasZoneLocations = DeserializeHazardZones<GasZones>("gas_zones");
            var userGasZones = DeserializeHazardZones<UserZones>("user_gas_zones");
            MergeData(GasZoneLocations, userGasZones);
      
            GasAssetZoneLocations = DeserializeHazardZones<GasAssetZones>("gas_asset_zones");
            var userGasAssetZones = DeserializeHazardZones<UserZones>("user_gas_asset_zones");
            MergeData(GasAssetZoneLocations, userGasAssetZones);
   
            RadZoneLocations = DeserializeHazardZones<RadZones>("rad_zones");
            var userRadZones = DeserializeHazardZones<UserZones>("user_rad_zones");
            MergeData(RadZoneLocations, userRadZones);
  
            RadAssetZoneLocations = DeserializeHazardZones<RadAssetZones>("rad_asset_zones");
            var userRadAssetZones = DeserializeHazardZones<UserZones>("user_rad_asset_zones");
            MergeData(RadAssetZoneLocations, userRadAssetZones);
        }

        public static List<HazardGroup> GetZones(EZoneType zoneType, string map)
        {
            ZoneCollection zones =
                zoneType == EZoneType.RadAssets ? RadAssetZoneLocations :
                zoneType == EZoneType.Radiation ? RadZoneLocations :
                zoneType == EZoneType.GasAssets ? GasAssetZoneLocations :
                zoneType == EZoneType.Gas ? GasZoneLocations :
                zoneType == EZoneType.Quest ? QuestZoneLocations :
                zoneType == EZoneType.Interactable ? InteractionLocations :
                SafeZoneLocations;

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

        public static IEnumerable<Vector3> GetSafeSpawns()
        {
            string map = Singleton<GameWorld>.Instance.MainPlayer.Location;

            switch (map)
            {
                case "rezervbase":
                    return SafeSpawns.ReserveSpawns;
                case "bigmap":
                    return SafeSpawns.CustomsSpawns;
                case "factory4_night":
                case "factory4_day":
                    return SafeSpawns.FactorySpawns;
                case "interchange":
                    return SafeSpawns.InterchangeSpawns;
                case "laboratory":
                    return SafeSpawns.LabsSpawns;
                case "shoreline":
                    return SafeSpawns.ShorelineSpawns;
                case "sandbox":
                case "sandbox_high":
                    return SafeSpawns.GZSpawns;
                case "woods":
                    return SafeSpawns.WoodsSpawns;
                case "lighthouse":
                    return SafeSpawns.LighthouseSpawns;
                case "tarkovstreets":
                    return SafeSpawns.SteetsSpawns;
                default:
                    return null;
            }
        }
    }
}
