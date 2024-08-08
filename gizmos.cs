using BepInEx.Logging;
using EFT;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RealismMod
{
    public class DebugGizmos
    {
        public static bool DrawGizmos = PluginConfig.EnableLogging.Value;

        public class SingleObjects
        {
            public static GameObject Sphere(Vector3 position, float size, Color color, bool temporary = false, float expiretime = 1f)
            {
                if (!DrawGizmos)
                {
                    return null;
                }

                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.GetComponent<Renderer>().material.color = color;
                sphere.GetComponent<Collider>().enabled = false;
                sphere.transform.position = position;
                sphere.transform.localScale = new Vector3(size, size, size);

                if (temporary)
                {
                    TempCoroutine.DestroyAfterDelay(sphere, expiretime);
                }

                return sphere;
            }

            public static GameObject Line(Vector3 startPoint, Vector3 endPoint, Color color, float lineWidth = 0.1f, bool temporary = false, float expiretime = 1f, bool taperLine = false)
            {
                if (!DrawGizmos)
                {
                    return null;
                }

                var lineObject = new GameObject();
                var lineRenderer = lineObject.AddComponent<LineRenderer>();

                // Set the color and width of the line
                lineRenderer.material.color = color;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = taperLine ? lineWidth / 4f : lineWidth;

                // Set the start and end points of the line
                lineRenderer.SetPosition(0, startPoint);
                lineRenderer.SetPosition(1, endPoint);

                if (temporary)
                {
                    TempCoroutine.DestroyAfterDelay(lineObject, expiretime);
                }

                return lineObject;
            }

            public static GameObject Ray(Vector3 startPoint, Vector3 direction, Color color, float length = 0.35f, float lineWidth = 0.1f, bool temporary = false, float expiretime = 1f, bool taperLine = false)
            {
                if (!DrawGizmos)
                {
                    return null;
                }

                var rayObject = new GameObject();
                var lineRenderer = rayObject.AddComponent<LineRenderer>();

                // Set the color and width of the line
                lineRenderer.material.color = color;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = taperLine ? lineWidth / 4f : lineWidth;

                // Set the start and end points of the line to draw a rays
                lineRenderer.SetPosition(0, startPoint);
                lineRenderer.SetPosition(1, startPoint + direction.normalized * length);

                if (temporary)
                {
                    TempCoroutine.DestroyAfterDelay(rayObject, expiretime);
                }

                return rayObject;
            }

           private static UnityEngine.Color GetColor(string colliderName)
            {
                float opacity = PluginConfig.test2.Value;
                if (colliderName.Contains("_chest") || colliderName.Contains("_back") || colliderName.Contains("_side") || colliderName.ToLower() == "left" || colliderName.ToLower() == "right" || colliderName.ToLower() == "top") return Color.magenta;
                if (colliderName.Contains("SpineTopChest") || colliderName.Contains("SAPI_back")) return new UnityEngine.Color(1, 0, 0, opacity);
                if (colliderName.Contains("SpineLowerChest")) return new UnityEngine.Color(0, 1, 0, opacity);
                if (colliderName.Contains("PelvisBack")) return new UnityEngine.Color(0, 0, 1, opacity);
                if (colliderName.Contains("SideChestDown")) return new UnityEngine.Color(0.5f, 1f, 0, opacity);
                if (colliderName.Contains("SideChestUp")) return new UnityEngine.Color(0, 0.5f, 1f, opacity);
                if (colliderName.Contains("HumanSpine2")) return new UnityEngine.Color(1f, 0f, 0.5f, opacity);
                if (colliderName.Contains("HumanSpine3")) return new UnityEngine.Color(1f, 1f, 1f, opacity);
                if (colliderName.Contains("HumanPelvis")) return new UnityEngine.Color(0.5f, 1f, 0.5f, opacity);
                if (colliderName.ToLower().Contains("leg")) return new UnityEngine.Color(0f, 0f, 1f, opacity);
                if (colliderName.ToLower().Contains("arm")) return new UnityEngine.Color(1f, 0f, 0f, opacity);
                if (colliderName.ToLower().Contains("eye")) return new UnityEngine.Color(0f, 1f, 0f, opacity);
                if (colliderName.ToLower().Contains("jaw")) return new UnityEngine.Color(0f, 1f, 0f, opacity);
                if (colliderName.ToLower().Contains("ear")) return new UnityEngine.Color(1f, 1f, 1f, opacity);
                if (colliderName.ToLower().Contains("head")) return new UnityEngine.Color(1f, 0f, 0f, opacity);
                return new UnityEngine.Color(0, 0, 1, opacity);
            }

            public static void VisualizeSphereCollider(SphereCollider sphereCollider, string colliderName)
            {
                // Create a sphere primitive to represent the collider.
                // Create a sphere primitive to represent the collider.
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                // Disable the sphere's collider component.
                UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());

                // Set the sphere's position to match the sphere collider.
                Transform colliderTransform = sphereCollider.transform;
                sphere.transform.position = colliderTransform.TransformPoint(sphereCollider.center);

                // Calculate the correct scale for the sphere. Unity's default sphere has a radius of 0.5 units.
                float actualScale = sphereCollider.radius / 0.5f;
                Vector3 scale = new Vector3(actualScale, actualScale, actualScale);

                // Apply global scale and additional scale factor if needed.
                sphere.transform.localScale = Vector3.Scale(colliderTransform.localScale, scale) * PluginConfig.test1.Value;

                // Set a transparent material to the sphere, so it doesn't obstruct the view.
                Material transparentMaterial = new Material(Shader.Find("Standard"));
                transparentMaterial.color = GetColor(colliderName); // Set to desired semi-transparent color
                sphere.GetComponent<Renderer>().material = transparentMaterial;

                // Parent the sphere to the collider's GameObject to maintain relative positioning.
                sphere.transform.SetParent(colliderTransform, true);
            }

            public static void VisualizeBoxCollider(BoxCollider boxCollider, string colliderName)
            {
                // Create a cube primitive to represent the collider.
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

                // Disable the cube's collider component.
                UnityEngine.Object.Destroy(cube.GetComponent<Collider>());

                // Set the cube's position and scale to match the box collider.
                Transform colliderTransform = boxCollider.transform;
                cube.transform.position = colliderTransform.TransformPoint(boxCollider.center);
                cube.transform.localScale = Vector3.Scale(colliderTransform.localScale, boxCollider.size) * PluginConfig.test1.Value;

                // Optionally, set the cube's rotation to match the collider's GameObject.
                cube.transform.rotation = colliderTransform.rotation;

                // Set a transparent material to the cube, so it doesn't obstruct the view.
                Material transparentMaterial = new Material(Shader.Find("Transparent/Diffuse"));
                transparentMaterial.color = GetColor(colliderName); // Red semi-transparent
                cube.GetComponent<Renderer>().material = transparentMaterial;

                // Parent the cube to the collider's GameObject to maintain relative positioning.
                cube.transform.SetParent(colliderTransform, true);
            }

            public static void VisualizeCapsuleCollider(CapsuleCollider capsuleCollider, string colliderName)
            {
                // Create a capsule primitive to represent the collider.
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);

                // Disable the capsule's collider component.
                UnityEngine.Object.Destroy(capsule.GetComponent<Collider>());

                // Set the capsule's position to match the capsule collider.
                Transform colliderTransform = capsuleCollider.transform;
                capsule.transform.position = colliderTransform.TransformPoint(capsuleCollider.center);

                // Calculate the correct scale for the capsule.
                float capsuleDefaultHeight = 2.0f; // Default Unity capsule height
                float capsuleDefaultRadius = 0.5f; // Default Unity capsule radius
                float actualScaleHeight = (capsuleCollider.height - 2 * capsuleCollider.radius) / capsuleDefaultHeight;
                float actualScaleRadius = capsuleCollider.radius / capsuleDefaultRadius;

                // Adjust the scale and rotation based on the collider's direction.
                Vector3 scale = Vector3.one;
                Quaternion rotation = Quaternion.identity;
                switch (capsuleCollider.direction)
                {
                    case 0: // x-axis
                        scale = new Vector3(actualScaleHeight, actualScaleRadius, actualScaleRadius);
                        rotation = Quaternion.Euler(0, 0, 90); // Rotate to align with x-axis
                        break;
                    case 1: // y-axis
                        scale = new Vector3(actualScaleRadius, actualScaleHeight, actualScaleRadius);
                        break;
                    case 2: // z-axis
                        scale = new Vector3(actualScaleRadius, actualScaleRadius, actualScaleHeight);
                        rotation = Quaternion.Euler(90, 0, 0); // Rotate to align with z-axis
                        break;
                }

                // Apply the rotation and scale.
                capsule.transform.rotation = colliderTransform.rotation * rotation;
                capsule.transform.localScale = Vector3.Scale(colliderTransform.localScale, scale) * PluginConfig.test2.Value;

                // Set a transparent material to the capsule, so it doesn't obstruct the view.
                Material transparentMaterial = new Material(Shader.Find("Transparent/Diffuse"));
                transparentMaterial.color = GetColor(colliderName); // Red semi-transparent
                capsule.GetComponent<Renderer>().material = transparentMaterial;

                // Parent the capsule to the collider's GameObject to maintain relative positioning.
                capsule.transform.SetParent(colliderTransform, true);
            }
        }


        public class DrawLists
        {
            private static ManualLogSource Logger;
            private Color ColorA;
            private Color ColorB;

            public DrawLists(Color colorA, Color colorB, string LogName = "", bool randomColor = false)
            {
                LogName += "[Drawer]";

                if (randomColor)
                {
                    ColorA = new Color(Random.value, Random.value, Random.value);
                    ColorB = new Color(Random.value, Random.value, Random.value);
                }
                else
                {
                    ColorA = colorA;
                    ColorB = colorB;
                }

                Logger = BepInEx.Logging.Logger.CreateLogSource(LogName);
            }

            /*                public void DrawTempPath(NavMeshPath Path, bool active, Color colorActive, Color colorInActive, float lineSize = 0.05f, float expireTime = 0.5f, bool useDrawerSetColors = false)
                            {
                                if (!DrawGizmos)
                                {
                                    return;
                                }

                                for (int i = 0; i < Path.corners.Length - 1; i++)
                                {
                                    Vector3 corner1 = Path.corners[i];
                                    Vector3 corner2 = Path.corners[i + 1];

                                    Color color;
                                    if (useDrawerSetColors)
                                    {
                                        color = active ? ColorA : ColorB;
                                    }
                                    else
                                    {
                                        color = active ? colorActive : colorInActive;
                                    }

                                    SingleObjects.Line(corner1, corner2, color, lineSize, true, expireTime);
                                }
                            }*/

            public void Draw(List<Vector3> list, bool destroy, float size = 0.1f, bool rays = false, float rayLength = 0.35f)
            {
                if (!DrawGizmos)
                {
                    DestroyDebug();
                    return;
                }
                if (destroy)
                {
                    DestroyDebug();
                }
                else if (list.Count > 0 && DebugObjects == null)
                {
                    Logger.LogWarning($"Drawing {list.Count} Vector3s");

                    DebugObjects = Create(list, size, rays, rayLength);
                }
            }

            public void Draw(Vector3[] array, bool destroy, float size = 0.1f, bool rays = false, float rayLength = 0.35f)
            {
                if (!DrawGizmos)
                {
                    DestroyDebug();
                    return;
                }
                if (destroy)
                {
                    DestroyDebug();
                }
                else if (array.Length > 0 && DebugObjects == null)
                {
                    Logger.LogWarning($"Drawing {array.Length} Vector3s");

                    DebugObjects = Create(array, size, rays, rayLength);
                }
            }

            private GameObject[] Create(List<Vector3> list, float size = 0.1f, bool rays = false, float rayLength = 0.35f)
            {
                List<GameObject> debugObjects = new List<GameObject>();
                foreach (var point in list)
                {
                    if (rays)
                    {
                        size *= Random.Range(0.5f, 1.5f);
                        rayLength *= Random.Range(0.5f, 1.5f);
                        var ray = SingleObjects.Ray(point, Vector3.up, ColorA, rayLength, size);
                        debugObjects.Add(ray);
                    }
                    else
                    {
                        var sphere = SingleObjects.Sphere(point, size, ColorA);
                        debugObjects.Add(sphere);
                    }
                }

                return debugObjects.ToArray();
            }

            private GameObject[] Create(Vector3[] array, float size = 0.1f, bool rays = false, float rayLength = 0.35f)
            {
                List<GameObject> debugObjects = new List<GameObject>();
                foreach (var point in array)
                {
                    if (rays)
                    {
                        size *= Random.Range(0.5f, 1.5f);
                        rayLength *= Random.Range(0.5f, 1.5f);
                        var ray = SingleObjects.Ray(point, Vector3.up, ColorA, rayLength, size);
                        debugObjects.Add(ray);
                    }
                    else
                    {
                        var sphere = SingleObjects.Sphere(point, size, ColorA);
                        debugObjects.Add(sphere);
                    }
                }

                return debugObjects.ToArray();
            }

            private void DestroyDebug()
            {
                if (DebugObjects != null)
                {
                    foreach (var point in DebugObjects)
                    {
                        Object.Destroy(point);
                    }

                    DebugObjects = null;
                }
            }

            private GameObject[] DebugObjects;
        }

        public class Components
        {
            /// <summary>
            /// Creates a line between two game objects and adds a script to update the line's DrawPosition and color every frame.
            /// </summary>
            /// <param name="startObject">The starting game object.</param>
            /// <param name="endObject">The ending game object.</param>
            /// <param name="lineWidth">The width of the line.</param>
            /// <param name="color">The color of the line.</param>
            /// <returns>The game object containing the line renderer.</returns>
            public static GameObject FollowLine(GameObject startObject, GameObject endObject, float lineWidth, Color color)
            {
                var lineObject = new GameObject();
                var lineRenderer = lineObject.AddComponent<LineRenderer>();

                // Set the color and width of the line
                lineRenderer.material.color = color;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;

                // Set the initial start and end points of the line
                lineRenderer.SetPosition(0, startObject.transform.position);
                lineRenderer.SetPosition(1, endObject.transform.position);

                // AddorUpdateColorScheme a script to update the line's DrawPosition and color every frame
                var followLineScript = lineObject.AddComponent<FollowLineScript>();
                followLineScript.startObject = startObject;
                followLineScript.endObject = endObject;
                followLineScript.lineRenderer = lineRenderer;

                return lineObject;
            }

            public class FollowLineScript : MonoBehaviour
            {
                public GameObject startObject;
                public GameObject endObject;
                public LineRenderer lineRenderer;
                public float yOffset = 1f;

                private void Update()
                {
                    lineRenderer.SetPosition(0, startObject.transform.position + new Vector3(0, yOffset, 0));
                    lineRenderer.SetPosition(1, endObject.transform.position + new Vector3(0, yOffset, 0));
                }

                /// <summary>
                /// Sets the color of the line renderer material.
                /// </summary>
                /// <param name="color">The color to set.</param>
                public void SetColor(Color color)
                {
                    lineRenderer.material.color = color;
                }
            }
        }

        internal class TempCoroutine : MonoBehaviour
        {
            /// <summary>
            /// Class to run coroutines on a MonoBehaviour.
            /// </summary>
            internal class TempCoroutineRunner : MonoBehaviour { }

            /// <summary>
            /// Destroys the specified GameObject after a given delay.
            /// </summary>
            /// <param name="obj">The GameObject to be destroyed.</param>
            /// <param name="delay">The delay before the GameObject is destroyed.</param>
            public static void DestroyAfterDelay(GameObject obj, float delay)
            {
                if (obj != null)
                {
                    var runner = new GameObject("TempCoroutineRunner").AddComponent<TempCoroutineRunner>();
                    runner.StartCoroutine(RunDestroyAfterDelay(obj, delay));
                }
            }

            /// <summary>
            /// Runs a coroutine to destroy a GameObject after a delay.
            /// </summary>
            /// <param name="obj">The GameObject to destroy.</param>
            /// <param name="delay">The delay before destroying the GameObject.</param>
            /// <returns>The coroutine.</returns>
            private static IEnumerator RunDestroyAfterDelay(GameObject obj, float delay)
            {
                yield return new WaitForSeconds(delay);
                if (obj != null)
                {
                    Destroy(obj);
                }
                TempCoroutineRunner runner = obj?.GetComponentInParent<TempCoroutineRunner>();
                if (runner != null)
                {
                    Destroy(runner.gameObject);
                }
            }
        }
    }
}

