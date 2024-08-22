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
        public static UnityEngine.Object Ural { get; set; }
        public static UnityEngine.Object BluePallet { get; set; }
        public static UnityEngine.Object BlueFuelPalletCloth { get; set; }
        public static UnityEngine.Object BlueFuelPallet { get; set; }

    }

    public static class MoveDaCube
    {
        public static ConfigEntry<float> ChangeSpeed;
        public static ConfigEntry<KeyboardShortcut> PositiveXKey;
        public static ConfigEntry<KeyboardShortcut> NegativeXKey;
        public static ConfigEntry<KeyboardShortcut> PositiveYKey;
        public static ConfigEntry<KeyboardShortcut> NegativeYKey;
        public static ConfigEntry<KeyboardShortcut> PositiveZKey;
        public static ConfigEntry<KeyboardShortcut> NegativeZKey;
        public static ConfigEntry<KeyboardShortcut> TranslateKey;
        public static ConfigEntry<KeyboardShortcut> ScaleKey;
        public static ConfigEntry<KeyboardShortcut> RotateKey;
        public static ConfigEntry<bool> LockXAndZRotation;
        public static ConfigEntry<string> SelectedObjectName;
        public static ConfigEntry<string> ClosestExfilName;

        private static void DrawerMatchPlayerYRotation(ConfigEntryBase entry)
        {
            if (Utils.GetYourPlayer() == null) return;
            if (TargetInteractableComponent == null) return;
            InitializeButton(TargetInteractableComponent.MatchPlayerYRotation, "Object Faces Camera Direction");
        }

        private static void InitializeButton(Action callable, string buttonName)
        {
            if (GUILayout.Button(buttonName, GUILayout.ExpandWidth(true)))
            {
                callable();
            }
        }
        private static void DrawerUnselectObject(ConfigEntryBase entry)
        {
            if (Utils.GetYourPlayer() == null) return;
            if (TargetInteractableComponent == null) return;
            InitializeButton(() => { UnselectObject(); }, "Unselect Object");
        }
        private static void DrawerSpawnObject(ConfigEntryBase entry)
        {
            if (Utils.GetYourPlayer() == null) return;
            InitializeButton(InteractableComponent.SpawnCube, "Spawn Object");
        }

        private static void DrawerSpawnAsset(ConfigEntryBase entry)
        {
            if (Utils.GetYourPlayer() == null) return;
            InitializeButton(InteractableComponent.SpawnAsset, "Spawn Asset");
        }

        private static void DrawerResetTranslation(ConfigEntryBase entry)
        {
            if (Utils.GetYourPlayer() == null) return;
            if (TargetInteractableComponent == null) return;
            InitializeButton(TargetInteractableComponent.ResetTranslation, "Move Object To Player Feet");
        }

        public static void InitTempBindings(ConfigFile config)
        {
            SelectedObjectName = config.Bind(
              "41.0: Object Control",
              "Selected Object Name",
              ""
          );
            ChangeSpeed = config.Bind(
                "41.0: Object Control",
                "Change Speed",
                3f
            );
            config.Bind(
                "41.0: Object Control",
                "Spawn Object (name required)",
                "",
                new ConfigDescription(
                    "Creates a new zone object",
                    null,
                    new ConfigurationManagerAttributes { CustomDrawer = DrawerSpawnObject }
                )
            );
             config.Bind(
                "41.0: Object Control",
                "Spawn Asset (name required)",
                "",
                new ConfigDescription(
                    "Creates a new zone object",
                    null,
                    new ConfigurationManagerAttributes { CustomDrawer = DrawerSpawnAsset }
                )
            );
            config.Bind(
                "41.1: Object Control",
                "Move Object To Player Feet",
                "",
                new ConfigDescription(
                    "Moves object to player",
                    null,
                    new ConfigurationManagerAttributes { CustomDrawer = DrawerResetTranslation }
                )
            );
            config.Bind(
                "41.1: Object Control",
                "Face Player Camera Direction",
                "",
                new ConfigDescription(
                    "Creates a new zone object",
                    null,
                    new ConfigurationManagerAttributes { CustomDrawer = DrawerMatchPlayerYRotation }
                )
            );
            config.Bind(
                "41.1: Object Control",
                "Unselect Object",
                "",
                new ConfigDescription(
                    "Unselects currently selected object",
                    null,
                    new ConfigurationManagerAttributes { CustomDrawer = DrawerUnselectObject }
                )
            );


            PositiveZKey = config.Bind(
                "20.0: Object Movement Keybinds",
                "1. Forward",
                new KeyboardShortcut(KeyCode.U)
            );
            NegativeZKey = config.Bind(
                "20.0: Object Movement Keybinds",
                "2. Backward",
                new KeyboardShortcut(KeyCode.E)
            );
            NegativeXKey = config.Bind(
                "20.0: Object Movement Keybinds",
                "3. Left",
                new KeyboardShortcut(KeyCode.N)
            );
            PositiveXKey = config.Bind(
                "20.0: Object Movement Keybinds",
                "4. Right",
                new KeyboardShortcut(KeyCode.I)
            );
            PositiveYKey = config.Bind(
                "20.0: Object Movement Keybinds",
                "5. Up",
                new KeyboardShortcut(KeyCode.Y)
            );
            NegativeYKey = config.Bind(
                "20.0: Object Movement Keybinds",
                "6. Down",
                new KeyboardShortcut(KeyCode.L)
            );

            TranslateKey = config.Bind(
                "30.0: Mode Keybinds",
                "Translate",
                new KeyboardShortcut(KeyCode.J)
            );
            ScaleKey = config.Bind(
                "30.0: Mode Keybinds",
                "Scale",
                new KeyboardShortcut(KeyCode.H)
            );
            RotateKey = config.Bind(
                "30.0: Mode Keybinds",
                "Rotate",
                new KeyboardShortcut(KeyCode.Comma)
            );
            LockXAndZRotation = config.Bind(
                "30.0: Mode Keybinds",
                "Lock X And Z Rotation Axes",
                true
            );
        }

        public static InteractableComponent TargetInteractableComponent;
        public static List<InteractableComponent> AllInteractableComponents = new List<InteractableComponent>();
        public static NoteWindow NoteUIPanel;
        public static InventoryScreen InventoryUI;
        public static TasksScreen TasksUI;
        public static List<ExfiltrationPoint> ExfiltrationPointList = new List<ExfiltrationPoint>();

        public static EInputMode Mode = EInputMode.Translate;
        public enum EInputMode
        {
            Translate,
            Scale,
            Rotate
        }

        public static void Update()
        {
            if (Utils.GetYourPlayer() == null) return;
            if (TargetInteractableComponent == null) return;

            if (TranslateKey.Value.IsDown())
            {
                Mode = EInputMode.Translate;
                TargetInteractableComponent.SetColor(Color.green);
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuInstallModFunc);
            }
            if (ScaleKey.Value.IsDown())
            {
                Mode = EInputMode.Scale;
                TargetInteractableComponent.SetColor(Color.blue);
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuInstallModGear);

            }
            if (RotateKey.Value.IsDown())
            {
                Mode = EInputMode.Rotate;
                TargetInteractableComponent.SetColor(Color.red);
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuInstallModVital);
            }

            float delta = Time.deltaTime;
            float speed = ChangeSpeed.Value;

            switch (Mode)//
            {
                case EInputMode.Rotate:
                    {
                        HandleRotation(speed, delta);
                        break;
                    }
                case EInputMode.Translate:
                    {
                        HandleTranslation(speed, delta);
                        break;
                    }
                case EInputMode.Scale:
                    {
                        HandleScaling(speed, delta);
                        break;
                    }
            }
        }

        public static void SelectObject(GameObject obj, bool mute = false)
        {
            if (TargetInteractableComponent != null)
            {
                if (NameFieldEmpty() || NameTaken()) return;
            }

            TargetInteractableComponent = obj.GetComponent<InteractableComponent>();
            Mode = EInputMode.Translate;
            TargetInteractableComponent.SetColor(Color.green);
            SelectedObjectName.Value = TargetInteractableComponent.GetName();

            if (!mute)
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuWeaponAssemble);
            }
        }

        public static void UnselectObject(bool mute = false)
        {
            if (TargetInteractableComponent == null) return;
            if (NameFieldEmpty() || NameTaken()) return;

            string oldName = TargetInteractableComponent.GetName();

            TargetInteractableComponent.SetName(SelectedObjectName.Value);
            TargetInteractableComponent.SetColor(Color.magenta);
            TargetInteractableComponent.LogTransforms();

            TargetInteractableComponent = null;
            SelectedObjectName.Value = "";

            if (!mute)
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuWeaponDisassemble);
            }
        }

        public static bool NameTaken()
        {
            if (SelectedObjectName.Value != TargetInteractableComponent.GetName())
            {
                ConsoleScreen.LogError($"An object by the name of {SelectedObjectName.Value} already exists!");
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
                return true;
            }
            return false;
        }

        public static bool NameFieldEmpty()
        {
            if (SelectedObjectName.Value == "")
            {
                ConsoleScreen.LogError($"Your object has an empty name! SOMETHING WILL EXPLODE AHHH FIX IT NOW (jk just fix it and you'll be fine)");
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
                return true;
            }
            return false;
        }

        public static void HandleScaling(float speed, float delta)
        {
            if (PositiveXKey.Value.IsPressed())
            {
                TargetInteractableComponent.ScaleMe("x", speed * delta);
            }
            if (NegativeXKey.Value.IsPressed())
            {
                TargetInteractableComponent.ScaleMe("x", -(speed * delta));
            }
            if (PositiveYKey.Value.IsPressed())
            {
                TargetInteractableComponent.ScaleMe("y", speed * delta);
            }
            if (NegativeYKey.Value.IsPressed())
            {
                TargetInteractableComponent.ScaleMe("y", -(speed * delta));
            }
            if (PositiveZKey.Value.IsPressed())
            {
                TargetInteractableComponent.ScaleMe("z", speed * delta);
            }
            if (NegativeZKey.Value.IsPressed())
            {
                TargetInteractableComponent.ScaleMe("z", -(speed * delta));
            }
        }

        public static void HandleRotation(float speed, float delta)
        {
            float rotSpeed = speed * 25;

            if (PositiveXKey.Value.IsPressed())
            {
                TargetInteractableComponent.RotateMe("x", rotSpeed * delta);
            }
            if (NegativeXKey.Value.IsPressed())
            {
                TargetInteractableComponent.RotateMe("x", -rotSpeed * delta);
            }
            if (PositiveYKey.Value.IsPressed())
            {
                TargetInteractableComponent.RotateMe("y", rotSpeed * delta);
            }
            if (NegativeYKey.Value.IsPressed())
            {
                TargetInteractableComponent.RotateMe("y", -rotSpeed * delta);
            }
            if (PositiveZKey.Value.IsPressed())
            {
                TargetInteractableComponent.RotateMe("z", rotSpeed * delta);
            }
            if (NegativeZKey.Value.IsPressed())
            {
                TargetInteractableComponent.RotateMe("z", -rotSpeed * delta);
            }
        }

        public static void HandleTranslation(float speed, float delta)
        {
            if (PositiveXKey.Value.IsPressed())
            {
                TargetInteractableComponent.TranslateMe("x", speed * delta);
            }
            if (NegativeXKey.Value.IsPressed())
            {
                TargetInteractableComponent.TranslateMe("x", -(speed * delta));
            }
            if (PositiveYKey.Value.IsPressed())
            {
                TargetInteractableComponent.TranslateMe("y", speed * delta);
            }
            if (NegativeYKey.Value.IsPressed())
            {
                TargetInteractableComponent.TranslateMe("y", -(speed * delta));
            }
            if (PositiveZKey.Value.IsPressed())
            {
                TargetInteractableComponent.TranslateMe("z", speed * delta);
            }
            if (NegativeZKey.Value.IsPressed())
            {
                TargetInteractableComponent.TranslateMe("z", -(speed * delta));
            }
        }

    }

    public class InteractableComponent : InteractableObject
    {
        public List<ActionsTypesClass> Actions = new List<ActionsTypesClass>();
        public static GameObject ParentGameObject { get; private set; }
        private static bool _IsAsset = false;

        public void LogTransforms() 
        {
            Utils.Logger.LogWarning("======Object " + ParentGameObject.name + " ====== ");
            Utils.Logger.LogWarning("pos " + ParentGameObject.transform.position);
            Utils.Logger.LogWarning("rotation " + ParentGameObject.transform.rotation);
        }

        public void Init()
        {
            this.gameObject.layer = LayerMask.NameToLayer("Interactive");
            if (!_IsAsset) ParentGameObject = this.gameObject.transform.parent.gameObject;

            Actions.AddRange(
                new List<ActionsTypesClass>()
                {
                    new ActionsTypesClass
                    {
                        Name = "Select / Unselect",
                        Action = () =>
                        {
                            if (MoveDaCube.TargetInteractableComponent == null)
                            {
                                MoveDaCube.SelectObject(this.gameObject);
                            }
                            else if (MoveDaCube.TargetInteractableComponent == this)
                            {
                                MoveDaCube.UnselectObject();
                            }
                            else
                            {
                                MoveDaCube.UnselectObject(mute:true);
                                MoveDaCube.SelectObject(this.gameObject);
                            }
                        }
                    },
                    new ActionsTypesClass
                    {
                        Name = "Move To Player Feet",
                        Action = ResetTranslation
                    },
                    new ActionsTypesClass
                    {
                        Name = "Reset Scale",
                        Action = ResetScale
                    },
                    new ActionsTypesClass
                    {
                        Name = "Reset Rotation",
                        Action = ResetRotation
                    },
                    new ActionsTypesClass
                    {
                        Name = "Face Camera Direction",
                        Action = MatchPlayerYRotation
                    },
                    new ActionsTypesClass
                    {
                        Name = "Delete",
                        Action = Delete
                    }
                }
            );
        }

        public void ResetRotation()
        {
            ParentGameObject.transform.rotation = Quaternion.identity;
            this.gameObject.transform.rotation = Quaternion.identity;
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.GeneratorTurnOff);
        }

        public void MatchPlayerYRotation()
        {
            Vector3 cameraEulerAngles = Utils.GetYourPlayer().CameraPosition.rotation.eulerAngles;
            Vector3 newObjectEulerAngles = new Vector3(0, cameraEulerAngles.y, 0);
            ParentGameObject.transform.rotation = Quaternion.Euler(newObjectEulerAngles);
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.GeneratorTurnOff);
        }

        public void ResetTranslation()
        {
            ParentGameObject.transform.position = Utils.GetYourPlayer().Transform.position;
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.GeneratorTurnOff);
        }

        public void ResetScale()
        {
            this.gameObject.transform.localScale = new Vector3(1, 1, 1);
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.GeneratorTurnOff);
        }

        public void Delete()
        {
            MoveDaCube.UnselectObject(mute: true);
            MoveDaCube.AllInteractableComponents.Remove(this);
            MoveDaCube.SelectedObjectName.Value = "";

            this.gameObject.transform.position = new Vector3(0, 0, -9999);
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuEscape);
            Destroy(this.gameObject, 3);
        }

        public void TranslateMe(string axis, float amount)
        {
            Vector3 translation = new Vector3(0, 0, 0);

            switch (axis)
            {
                case "x": translation = new Vector3(amount, 0, 0); break;
                case "y": translation = new Vector3(0, amount, 0); break;
                case "z": translation = new Vector3(0, 0, amount); break;
            }

            ParentGameObject.transform.Translate(translation, Space.Self);
        }

        public void ScaleMe(string axis, float amount)
        {
            Vector3 scaleAmount = new Vector3(0, 0, 0);

            switch (axis)
            {
                case "x": scaleAmount = new Vector3(amount, 0, 0); break;
                case "y": scaleAmount = new Vector3(0, amount, 0); break;
                case "z": scaleAmount = new Vector3(0, 0, amount); break;
            }

            this.gameObject.transform.localScale = this.gameObject.transform.localScale + scaleAmount;
        }

        public void RotateMe(string axis, float amount)
        {
            Vector3 rotation = new Vector3(0, 0, 0);

            switch (axis)
            {
                case "x": rotation = new Vector3(0, amount, 0); break;
                case "y": rotation = new Vector3(0, 0, amount); break;
                case "z": rotation = new Vector3(amount, 0, 0); break;
            }

            if (axis == "x")
            {
                ParentGameObject.transform.localRotation = ParentGameObject.transform.localRotation * Quaternion.Euler(rotation);
            }
            else if (!MoveDaCube.LockXAndZRotation.Value)
            {
                this.gameObject.transform.localRotation = this.gameObject.transform.localRotation * Quaternion.Euler(rotation);
            }

        }

        public void SetColor(Color color)
        {
            if (_IsAsset) return;
            this.gameObject.GetComponent<Renderer>().material.color = color;
        }

        public void SetName(string name)
        {
            this.gameObject.name = name;
            ParentGameObject.name = name + "_parent";
        }

        public string GetName()
        {
            return this.gameObject.name;
        }

        public Vector3 GetPosition()
        {
            return ParentGameObject.transform.position;
        }

        public Vector3 GetScale()
        {
            return this.gameObject.transform.localScale;
        }

        public Quaternion GetRotation()
        {
            return this.gameObject.transform.rotation;
        }

        //handle my object spawning in here, and have different methods for spawning assets, loot or cubes
        public static void SpawnCube()
        {
            //settings are his plugin config settings
            if (MoveDaCube.SelectedObjectName.Value == "")
            {
                ConsoleScreen.LogError($"You must give your new zone object a name!");
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
                return;
            }

            GameObject parent = new GameObject();

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.GetComponent<Renderer>().enabled = true;
            cube.transform.parent = parent.transform;
            InteractableComponent interactableComponent = cube.AddComponent<InteractableComponent>();
            interactableComponent.Init();
            interactableComponent.ResetTranslation();
            interactableComponent.MatchPlayerYRotation();
            interactableComponent.SetName(MoveDaCube.SelectedObjectName.Value);

            MoveDaCube.UnselectObject();
            MoveDaCube.SelectObject(cube, mute: true);
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.InsuranceInsured);

            MoveDaCube.AllInteractableComponents.Add(interactableComponent);
        }

        public static object GetFieldValue(string fieldName)
        {
           // Type type = obj.GetType();

            PropertyInfo fieldInfo = typeof(Assets).GetProperty(fieldName, BindingFlags.Public | BindingFlags.Static);
            // return fieldInfo.GetValue(obj);
            return fieldInfo.GetValue(null);
        }

        static List<GameObject> objects = new List<GameObject>();
        static void Test(DamageInfo di)
        {
            Utils.Logger.LogWarning("==================");
            for (int i = 0; i < objects.Count; i++)
            {
                Utils.Logger.LogWarning("name " + objects[i].name);
                Utils.Logger.LogWarning("num " + i + " " + objects[i].transform.position);
                Utils.Logger.LogWarning("num " + i + " " + objects[i].transform.rotation.eulerAngles);
            }
        }

        public static void SpawnAsset() 
        {

            //settings are his plugin config settings
            if (MoveDaCube.SelectedObjectName.Value == "")
            {
                ConsoleScreen.LogError($"You must give your new zone object a name!");
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
                return;
            }

            GameObject parent = new GameObject();

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.GetComponent<Renderer>().enabled = true;
            cube.transform.parent = parent.transform;
            InteractableComponent interactableComponent = cube.AddComponent<InteractableComponent>();
            interactableComponent.Init();
            interactableComponent.ResetTranslation();
            interactableComponent.MatchPlayerYRotation();
            interactableComponent.SetName(Utils.GenId());

            MoveDaCube.UnselectObject();
            MoveDaCube.SelectObject(cube, mute: true);
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.InsuranceInsured);

            MoveDaCube.AllInteractableComponents.Add(interactableComponent);

            var player = Utils.GetYourPlayer().Transform;
            var fieldValue = (UnityEngine.Object)GetFieldValue(PluginConfig.TargetZone.Value);
            GameObject asset = (GameObject)Instantiate(fieldValue, cube.transform.position, cube.transform.rotation);
            asset.transform.parent = parent.transform;
            BallisticCollider collider = asset.GetComponentInChildren<BallisticCollider>();
            collider.OnHitAction += Test;
            objects.Add(asset);

        }

        public static Material GetTransparentMaterial(Color color, float transparency)
        {
            Color transparentColor = new Color(color.r, color.g, color.b, transparency);

            // Create a new material with the Standard Shader
            Material transparentMaterial = new Material(Shader.Find("Standard"));

            // Set the material to use transparency
            transparentMaterial.SetFloat("_Mode", 3); // 3 is the mode for Transparent
            transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMaterial.SetInt("_ZWrite", 0);
            transparentMaterial.DisableKeyword("_ALPHATEST_ON");
            transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
            transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            transparentMaterial.renderQueue = 3000; // Ensure it's rendered after opaque objects
            transparentMaterial.SetFloat("_Glossiness", 0f);


            // Set the color with alpha for transparency
            transparentMaterial.color = transparentColor;

            return transparentMaterial;
        }

        public static Color GetTransparentColor(Color color, float transparency)
        {
            return new Color(color.r, color.g, color.b, transparency);
        }
    }

    internal class GetAvailableActionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(GetActionsClass), x => x.Name == nameof(GetActionsClass.GetAvailableActions) && x.GetParameters()[0].Name == "owner");
        }

        [PatchPrefix]
        public static bool PatchPrefix(object[] __args, ref ActionsReturnClass __result)
        {
            // __args[1] is a GInterface called "interactive", it represents the component that enables interaction
            if (!(__args[1] is InteractableComponent)) return true;
            var customInteractable = __args[1] as InteractableComponent;

            __result = new ActionsReturnClass()
            {
                Actions = customInteractable.Actions
            };
            return false;
        }
    }


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
        public static ManualLogSource Logger;

        public static bool IsReady = false;
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

        public static bool AreFloatsEqual(float a, float b, float epsilon = 0.001f)
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

            System.Random rnd = new System.Random();
            int randNumber = rnd.Next(totalWeight);

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

        public static Player GetPlayerByID(string id)
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
                Utils.IsReady = false;
                Utils.WeaponIsReady = false;
                Utils.IsInHideout = false;
                return false;
            }
            Utils.IsReady = true;
            return true;
        }

        public static void SafelyAddAttributeToList(ItemAttributeClass itemAttribute, Mod __instance)
        {
            if (itemAttribute.Base() != 0f)
            {
                __instance.Attributes.Add(itemAttribute);
            }
        }

        public static string GenId()
        {
            return Guid.NewGuid().ToString();
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
