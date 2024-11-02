using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using static RealismMod.Attributes;

namespace RealismMod
{
    //Task extention method: allows easier handling of async tasks in non-async methods, treating them as coroutines instead
    public static class TaskExtensions
    {
        public static IEnumerator AsCoroutine(this Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }
    }

    public static class Utils
    {
        public const string GAMU_ID = "66fd571a05370c3ee1a1c613";
        public const string RAMU_ID = "66fd521442055447e2304fda";
        public const string GAMU_DATA_ID = "670120df4f0c4c37e6be90ae";
        public const string RAMU_DATA_ID = "670120ce354987453daf3d0c";
        public const string HALLOWEEN_TRANSMITTER_ID = "6703082a766cb6d11310094e";


        public static ManualLogSource Logger;
        public static System.Random SystemRandom = new System.Random();

        public static bool PlayerIsReady = false;
        public static bool IsInHideout = false;
        public static bool WeaponIsReady = false;
        public static bool HasRunErgoWeightCalc = false;

        public static string Silencer = "550aa4cd4bdc2dd8348b456c";
        public static string FlashHider = "550aa4bf4bdc2dd6348b456b";
        public static string MuzzleCombo = "550aa4dd4bdc2dc9348b4569";
        public static string Barrel = "555ef6e44bdc2de9068b457e";
        public static string Mount = "55818b224bdc2dde698b456f";
        public static string Receiver = "55818a304bdc2db5418b457d";
        public static string Stock = "55818a594bdc2db9688b456a";
        public static string Charge = "55818a6f4bdc2db9688b456b";
        public static string CompactCollimator = "55818acf4bdc2dde698b456b";
        public static string Collimator = "55818ad54bdc2ddc698b4569";
        public static string AssaultScope = "55818add4bdc2d5b648b456f";
        public static string Scope = "55818ae44bdc2dde698b456c";
        public static string IronSight = "55818ac54bdc2d5b648b456e";
        public static string SpecialScope = "55818aeb4bdc2ddc698b456a";
        public static string AuxiliaryMod = "5a74651486f7744e73386dd1";
        public static string Foregrip = "55818af64bdc2d5b648b4570";
        public static string PistolGrip = "55818a684bdc2ddd698b456d";
        public static string Gasblock = "56ea9461d2720b67698b456f";
        public static string Handguard = "55818a104bdc2db9688b4569";
        public static string Bipod = "55818afb4bdc2dde698b456d";
        public static string Flashlight = "55818b084bdc2d5b648b4571";
        public static string TacticalCombo = "55818b164bdc2ddc698b456c";
        public static string UBGL = "55818b014bdc2ddc698b456b";


        public static bool GetIPlayer(IPlayer x)
        {
            return x.ProfileId == Utils.GetYourPlayer().ProfileId;
        }

        public static async Task LoadLoot(Vector3 position, Quaternion rotation, string templateId)
        {
            Item item = Singleton<ItemFactory>.Instance.CreateItem(Utils.GenId(), templateId, null);
            item.StackObjectsCount = 1;
            item.SpawnedInSession = true;
            ResourceKey[] resources = item.Template.AllResources.ToArray();
            await LoadBundle(resources);
            IPlayer player = Singleton<GameWorld>.Instance.RegisteredPlayers.FirstOrDefault(new Func<IPlayer, bool>(GetIPlayer));
            Singleton<GameWorld>.Instance.ThrowItem(item, player, position, rotation, Vector3.zero, Vector3.zero);

        }

        public static async Task LoadBundle(ResourceKey[] resources)
        {
            await Singleton<PoolManager>.Instance.LoadBundlesAndCreatePools(PoolManager.PoolsCategory.Raid, PoolManager.AssemblyType.Local, resources, JobPriority.Immediate, null, PoolManager.DefaultCancellationToken);
        }

        public static Vector3 ClampVector(Vector3 value, Vector3 min, Vector3 max)
        {
            return new Vector3(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y),
                Mathf.Clamp(value.z, min.z, max.z)
            );
        }

        public static bool AreVector2sEqual(Vector2 a, Vector2 b, float epsilon = 0.001f)
        {
            return Vector2.Distance(a, b) < epsilon;
        }

        public static bool IsGreaterThanOrEqualTo(float a, float b, float epsilon = 0.0001f)
        {
            return IsGreaterThan(a, b, epsilon) || AreFloatsEqual(a, b, epsilon);
        }

        public static bool IsLessThanOrEqualTo(float a, float b, float epsilon = 0.0001f)
        {
            return IsLessThan(a, b, epsilon) || AreFloatsEqual(a, b, epsilon);
        }

        public static bool IsLessThan(float a, float b, float epsilon = 0.0001f)
        {
            return (b - a) > epsilon; 
        }

        public static bool IsGreaterThan(float a, float b, float epsilon = 0.0001f)
        {
            return (a - b) > epsilon;
        }

        public static bool AreFloatsEqual(float a, float b, float epsilon = 0.0001f)
        {
            float difference = Math.Abs(a - b);
            return difference < epsilon;
        }

        public static string GetRandomWeightedKey(Dictionary<string, int> items)
        {
            string result = "";
            int totalWeight = 0;

            foreach (var item in items) 
            {
                totalWeight += item.Value;
            }

            int randNumber = Utils.SystemRandom.Next(totalWeight);

            foreach (var item in items)
            {
                int weight = item.Value;

                if (randNumber >= weight)
                {
                    randNumber -= weight;
                }
                else
                {
                    result = item.Key;
                    break;
                }
            }
            return result;
        }

        public static bool IsConfItemNull(string[] confItemArray, int expectedLength = 0)
        {
            if (confItemArray != null && confItemArray.Length > expectedLength)
            {
                if (confItemArray[0] == "SPTRM")
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ConfItemsIsNullOrInvalid(string[] confItemArray, int length)
        {
            if (confItemArray != null && confItemArray.Length >= length)
            {
                if (confItemArray[0] == "SPTRM") 
                {
                    return false;
                }
            }
            return true;
        }

        public static Player GetYourPlayer() 
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return null;
            return gameWorld.MainPlayer;
        }

        public static Player GetPlayerByProfileId(string id)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld.GetAlivePlayerByProfileID(id);   
        }

        public static bool CheckIsReady()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            SessionResultPanel sessionResultPanel = Singleton<SessionResultPanel>.Instance;

            Player player = gameWorld?.MainPlayer;
            if (player != null)
            {
                Utils.WeaponIsReady = player?.HandsController != null && player?.HandsController?.Item != null && player?.HandsController?.Item is Weapon ? true : false;
                Utils.IsInHideout = player is HideoutPlayer ? true : false;
            }
            else 
            {
                Utils.WeaponIsReady = false;
                Utils.IsInHideout = false;
            }

            if (gameWorld == null || gameWorld.AllAlivePlayersList == null || gameWorld.MainPlayer == null || sessionResultPanel != null)
            {
                Utils.PlayerIsReady = false;
                Utils.WeaponIsReady = false;
                Utils.IsInHideout = false;
                return false;
            }
            Utils.PlayerIsReady = true;
            return true;
        }


        public static void AddAttribute(Item item, ENewItemAttributeId att, float baseValue, string displayValue, bool? lessIsGood = null, string name = null, bool colored = true)
        {
            string attName = name == null ? att.GetName() : name;
            ItemAttributeClass attribute = new ItemAttributeClass(att);
            attribute.Name = attName;
            attribute.Base = () => baseValue;
            attribute.StringValue = () => displayValue;
            if (lessIsGood != null) attribute.LessIsGood = (bool)lessIsGood;
            attribute.DisplayType = () => EItemAttributeDisplayType.Compact;
            attribute.LabelVariations = colored ? EItemAttributeLabelVariations.Colored : EItemAttributeLabelVariations.None;
            Utils.SafelyAddAttributeToList(attribute, item);
        }


        public static void SafelyAddAttributeToList(ItemAttributeClass itemAttribute, Item item)
        {
            if (itemAttribute.Base() != 0f)
            {
                item.Attributes.Add(itemAttribute);
            }
        }

        public static string GenId()
        {
            return MongoID.Generate();
        }

        public static bool IsSight(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Scope] || mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[AssaultScope] || mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[SpecialScope] || mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[CompactCollimator] || mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Collimator] || mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[IronSight];
        }
        public static bool IsMuzzleDevice(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[MuzzleCombo] || mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Silencer] || mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[FlashHider];
        }
        public static bool IsStock(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Stock];
        }
        public static bool IsSilencer(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Silencer];
        }
        public static bool IsMagazine(Mod mod)
        {
            return (mod is MagazineClass);
        }
        public static bool IsFlashHider(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[FlashHider];
        }
        public static bool IsMuzzleCombo(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[MuzzleCombo];
        }
        public static bool IsBarrel(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Barrel];
        }
        public static bool IsMount(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Mount];
        }
        public static bool IsReceiver(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Receiver];
        }
        public static bool IsCharge(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Charge];
        }
        public static bool IsCompactCollimator(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[CompactCollimator];
        }
        public static bool IsCollimator(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Collimator];
        }
        public static bool IsAssaultScope(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[AssaultScope];
        }
        public static bool IsScope(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Scope];
        }
        public static bool IsIronSight(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[IronSight];
        }
        public static bool IsSpecialScope(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[SpecialScope];
        }
        public static bool IsAuxiliaryMod(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[AuxiliaryMod];
        }
        public static bool IsForegrip(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Foregrip];
        }
        public static bool IsPistolGrip(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[PistolGrip];
        }
        public static bool IsGasblock(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Gasblock];
        }
        public static bool IsHandguard(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Handguard];
        }
        public static bool IsBipod(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Bipod];
        }
        public static bool IsFlashlight(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[Flashlight];
        }
        public static bool IsTacticalCombo(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[TacticalCombo];
        }
        public static bool IsUBGL(Mod mod)
        {
            return mod.GetType() == TemplateIdToObjectMappingsClass.TypeTable[UBGL];
        }
    }

  
}
