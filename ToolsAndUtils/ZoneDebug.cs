using BepInEx.Configuration;
using Comfort.Common;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static RootMotion.FinalIK.IKSolver;

namespace RealismMod
{
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
        public static ConfigEntry<string> SelectedAssetName;

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
        private static void DrawerLogObjects(ConfigEntryBase entry)
        {
            InitializeButton(() => { LogObjects(); }, "Log Object");
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
            SelectedAssetName = config.Bind(
            "41.0: Object Control",
            "Selected Asset Name",
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
            config.Bind(
               "41.1: Object Control",
               "Log Objects",
               "",
               new ConfigDescription(
                  "Logs Objects",
                  null,
                  new ConfigurationManagerAttributes { CustomDrawer = DrawerLogObjects }
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

        public static void AddComponentToExistingGO(GameObject box, string name)
        {
            GameObject parent = new GameObject();

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = parent.transform;

            parent.name = name + "_parent";
            parent.transform.position = box.transform.position;
            parent.transform.rotation = box.transform.rotation;

            cube.transform.rotation = box.transform.rotation;
            cube.transform.localScale = box.transform.localScale;

            cube.AddComponent<InteractableComponent>();
            InteractableComponent interactableComponent = cube.GetComponent<InteractableComponent>();
            interactableComponent.Init();
            interactableComponent.SetName(name);

            Renderer renderer = interactableComponent.GetComponent<Renderer>();
            renderer.enabled = true;
            renderer.material.color = Color.magenta;

            MoveDaCube.AllInteractableComponents.Add(interactableComponent);
            box.transform.localScale = new Vector3(0, 0, 0); //hide the hazsard zone I used to size this movemable cube
        }

        public static void Update()
        {
            if (Utils.GetYourPlayer() == null) return;
            if (TargetInteractableComponent == null) return;

            if (TranslateKey.Value.IsDown())
            {
                Mode = EInputMode.Translate;
                TargetInteractableComponent.SetColor(new Color(0, 1, 0, 0.1f));
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuInstallModFunc);
            }
            if (ScaleKey.Value.IsDown())
            {
                Mode = EInputMode.Scale;
                TargetInteractableComponent.SetColor(new Color(0, 0, 1, 0.1f));
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuInstallModGear);

            }
            if (RotateKey.Value.IsDown())
            {
                Mode = EInputMode.Rotate;
                TargetInteractableComponent.SetColor(new Color(1, 0, 0, 0.1f));
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

        public static void LogObjects(bool mute = false)
        {
            Utils.Logger.LogWarning("=======================");
            foreach (var zone in AllInteractableComponents)
            {
                Utils.Logger.LogWarning("==");
                Utils.Logger.LogWarning("zone name " + zone.name);
                if (zone.Asset != null) Utils.Logger.LogWarning("asset name " + zone.Asset.name);
                Utils.Logger.LogWarning("\"position\": " + "\"x\":" + zone.transform.position.x + "," + "\"y\":" + zone.transform.position.y + "," + "\"z:\"" + zone.transform.position.z);
                Utils.Logger.LogWarning("\"rotation\": " + "\"x\":" + zone.transform.rotation.eulerAngles.x + "," + "\"y\":" + zone.transform.eulerAngles.y + "," + "\"z:\"" + zone.transform.eulerAngles.z);
                Utils.Logger.LogWarning("\"size\": " + "\"x\":" + zone.transform.localScale.x + "," + "\"y\":" + zone.transform.localScale.y + "," + "\"z:\"" + zone.transform.localScale.z);
                Utils.Logger.LogWarning("==");
            }
        }


        public static void SelectObject(GameObject obj, bool mute = false)
        {
            /*   if (TargetInteractableComponent != null)
               {
                   if (NameFieldEmpty() || NameTaken()) return;
               }
   */
            TargetInteractableComponent = obj.GetComponent<InteractableComponent>();
            Mode = EInputMode.Translate;
            TargetInteractableComponent.SetColor(new Color(0, 1, 0, 0.1f));
            SelectedObjectName.Value = TargetInteractableComponent.GetName();

            if (!mute)
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuWeaponAssemble);
            }
        }

        public static void UnselectObject(bool mute = false)
        {
            if (TargetInteractableComponent == null) return;
            /*if (NameFieldEmpty() || NameTaken()) return;*/

            string oldName = TargetInteractableComponent.GetName();
            TargetInteractableComponent.SetColor(new Color(1, 1, 1, 0.1f));

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
        public GameObject ParentGameObject { get; private set; }
        public GameObject Asset { get; private set; }

        public void Init()
        {
            this.gameObject.layer = LayerMask.NameToLayer("Interactive");
            ParentGameObject = this.gameObject.transform.parent.gameObject;

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
                        Name =  this.transform.parent.name,
                        Action = LogDetails
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

        public void LogDetails()
        {
            var zone = this.transform;
            Utils.Logger.LogWarning("==== " + this.transform.parent.name);
            Utils.Logger.LogWarning("name " + this.transform.parent.name);
            Utils.Logger.LogWarning("\"position\": " + "\"x\":" + zone.transform.position.x + "," + "\"y\":" + zone.transform.position.y + "," + "\"z:\"" + zone.transform.position.z);
            Utils.Logger.LogWarning("\"rotation\": " + "\"x\":" + zone.transform.rotation.eulerAngles.x + "," + "\"y\":" + zone.transform.eulerAngles.y + "," + "\"z:\"" + zone.transform.eulerAngles.z);
            Utils.Logger.LogWarning("\"size\": " + "\"x\":" + zone.transform.localScale.x + "," + "\"y\":" + zone.transform.localScale.y + "," + "\"z:\"" + zone.transform.localScale.z);
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
            Destroy(Asset, 3);
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

            ParentGameObject.transform.localRotation = ParentGameObject.transform.localRotation * Quaternion.Euler(rotation);


            /*if (axis == "x")
            {
                ParentGameObject.transform.localRotation = ParentGameObject.transform.localRotation * Quaternion.Euler(rotation);

            }
            else if (!MoveDaCube.LockXAndZRotation.Value)
            {
                this.gameObject.transform.localRotation = this.gameObject.transform.localRotation * Quaternion.Euler(rotation);
            }*/

        }

        public void SetColor(Color color)
        {
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
            interactableComponent.SetName(MoveDaCube.SelectedObjectName.Value);

            MoveDaCube.UnselectObject();
            MoveDaCube.SelectObject(cube, mute: true);
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.InsuranceInsured);

            MoveDaCube.AllInteractableComponents.Add(interactableComponent);

            var player = Utils.GetYourPlayer().Transform;
            var fieldValue = (UnityEngine.Object)GetFieldValue(MoveDaCube.SelectedAssetName.Value);
            GameObject asset = (GameObject)Instantiate(fieldValue, cube.transform.position, cube.transform.rotation);
            asset.name = MoveDaCube.SelectedAssetName.Value + Utils.GenId();
            asset.transform.parent = parent.transform;
            interactableComponent.Asset = asset;
            BallisticCollider collider = asset.GetComponentInChildren<BallisticCollider>();
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

}
