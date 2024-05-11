using BetterLegacy.Components;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// This class handles everything to do with Custom Shapes.
    /// </summary>
    public class ShapeManager : MonoBehaviour
    {
        public static ShapeManager inst;

        /// <summary>
        /// The path of the shapes folder.
        /// </summary>
        public static string ShapesPath => "beatmaps/shapes/";

        /// <summary>
        /// The path of the shapes setup file.
        /// </summary>
        public static string ShapesSetup => $"{ShapesPath}setup.lss";

        public bool loadedShapes;
        public Transform shapeParent;

        /// <summary>
        /// Inits ShapeManager.
        /// </summary>
        public static void Init()
        {
            var gameObject = new GameObject("ShapeManager");
            gameObject.transform.SetParent(SystemManager.inst.transform);
            gameObject.AddComponent<ShapeManager>();
        }

        void Awake()
        {
            inst = this;

            StoredShapes2D = new Dictionary<Vector2Int, Shape>();
            StoredShapes3D = new Dictionary<Vector2Int, Shape>();

            if (!RTFile.FileExists(RTFile.ApplicationDirectory + ShapesSetup))
            {
                System.Windows.Forms.MessageBox.Show("Shapes Setup file does not exist.\nYou may run into issues with playing the game from here on, so it is recommended to\ndownload the proper assets from Github and place them into the appropriate folders.", "Error!");
                return;
            }

            Shapes2D = new List<List<Shape>>();
            Shapes3D = new List<List<Shape>>();

            var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.ApplicationDirectory + ShapesSetup));
            for (int i = 0; i < jn["type"].Count; i++)
            {
                Shapes2D.Add(new List<Shape>());
                Shapes3D.Add(new List<Shape>());
                for (int j = 0; j < jn["type"][i]["option"].Count; j++)
                {
                    var fullPath = RTFile.ApplicationDirectory + jn["type"][i]["option"][j]["path"];

                    // 2D
                    {
                        var sjn = JSON.Parse(RTFile.ReadFromFile(fullPath + "/data.lssh"));

                        Mesh mesh = null;
                        if (i != 4 && i != 6 && sjn["verts"] != null && sjn["tris"] != null)
                        {
                            mesh = new Mesh();
                            mesh.name = sjn["name"];
                            Vector3[] vertices = new Vector3[sjn["verts"].Count];
                            for (int k = 0; k < sjn["verts"].Count; k++)
                                vertices[k] = new Vector3(sjn["verts"][k]["x"].AsFloat, sjn["verts"][k]["y"].AsFloat, sjn["verts"][k]["z"].AsFloat);

                            int[] triangles = new int[sjn["tris"].Count];
                            for (int k = 0; k < sjn["tris"].Count; k++)
                                triangles[k] = sjn["tris"][k].AsInt;

                            mesh.vertices = vertices;
                            mesh.triangles = triangles;
                        }

                        var shape = new Shape(sjn["name"], i, j, mesh, null, string.IsNullOrEmpty(sjn["p"]) ? Shape.Property.RegularObject : (Shape.Property)sjn["p"].AsInt);

                        if (RTFile.FileExists(fullPath + "/icon.png"))
                        {
                            shape.Icon = SpriteManager.LoadSprite(fullPath + "/icon.png");
                        }

                        if (!StoredShapes2D.ContainsKey(shape.Vector))
                            StoredShapes2D.Add(shape.Vector, shape);

                        Shapes2D[i].Add(shape);
                    }

                    // 3D
                    if (RTFile.FileExists(fullPath + "/bg_data.lssh"))
                    {
                        var sjn = JSON.Parse(RTFile.ReadFromFile(fullPath + "/bg_data.lssh"));

                        Mesh mesh = null;
                        if (i != 4 && i != 6 && sjn["verts"] != null && sjn["tris"] != null)
                        {
                            mesh = new Mesh();
                            mesh.name = sjn["name"];
                            Vector3[] vertices = new Vector3[sjn["verts"].Count];
                            for (int k = 0; k < sjn["verts"].Count; k++)
                                vertices[k] = new Vector3(sjn["verts"][k]["x"].AsFloat, sjn["verts"][k]["y"].AsFloat, sjn["verts"][k]["z"].AsFloat);

                            int[] triangles = new int[sjn["tris"].Count];
                            for (int k = 0; k < sjn["tris"].Count; k++)
                                triangles[k] = sjn["tris"][k].AsInt;

                            mesh.vertices = vertices;
                            mesh.triangles = triangles;
                        }

                        var shape = new Shape(sjn["name"], i, j, mesh, null, string.IsNullOrEmpty(sjn["p"]) ? Shape.Property.RegularObject : (Shape.Property)sjn["p"].AsInt);

                        if (RTFile.FileExists(fullPath + "/icon.png"))
                        {
                            shape.Icon = SpriteManager.LoadSprite(fullPath + "/icon.png");
                        }

                        if (!StoredShapes3D.ContainsKey(shape.Vector))
                            StoredShapes3D.Add(shape.Vector, shape);

                        Shapes3D[i].Add(shape);
                    }
                    else
                    {
                        var shape = new Shape("null", i, j, null, null, i == 4 ? Shape.Property.TextObject : i == 6 ? Shape.Property.ImageObject : Shape.Property.RegularObject);

                        if (RTFile.FileExists(fullPath + "/icon.png"))
                        {
                            shape.Icon = SpriteManager.LoadSprite(fullPath + "/icon.png");
                        }

                        if (!StoredShapes3D.ContainsKey(shape.Vector))
                            StoredShapes3D.Add(shape.Vector, shape);

                        Shapes3D[i].Add(shape);
                    }
                }
            }
        }

        /// <summary>
        /// Saves shapes for cases where a new shape has been added.
        /// </summary>
        public void Save()
        {
            var jn = JSON.Parse("{}");
            for (int i = 0; i < ObjectManager.inst.objectPrefabs.Count; i++)
            {
                for (int j = 0; j < ObjectManager.inst.objectPrefabs[i].options.Count; j++)
                {
                    var name = ObjectManager.inst.objectPrefabs[i].options[j].name;
                    jn["type"][i]["option"][j]["path"] = $"beatmaps/shapes/{name}";

                    var fullPath = $"{RTFile.ApplicationDirectory}beatmaps/shapes/{name}";
                    if (!RTFile.DirectoryExists(fullPath))
                        Directory.CreateDirectory(fullPath);

                    var sjn = JSON.Parse("{}");

                    sjn["name"] = name;
                    sjn["s"] = i;
                    sjn["so"] = j;

                    if (i != 4 && i != 6 && i != 9)
                    {
                        var mesh = ObjectManager.inst.objectPrefabs[i].options[j].transform.GetChild(0).GetComponent<MeshFilter>().mesh;

                        for (int k = 0; k < mesh.vertices.Length; k++)
                        {
                            sjn["verts"][k]["x"] = mesh.vertices[k].x.ToString();
                            sjn["verts"][k]["y"] = mesh.vertices[k].y.ToString();
                            sjn["verts"][k]["z"] = mesh.vertices[k].z.ToString();
                        }

                        for (int k = 0; k < mesh.triangles.Length; k++)
                        {
                            sjn["tris"][k] = mesh.triangles[k].ToString();
                        }
                    }
                    else
                    {
                        sjn["p"] = i == 4 ? 1 : i == 6 ? 2 : 3;
                    }

                    RTFile.WriteToFile(fullPath + "/data.lssh", jn.ToString());
                }
            }

            RTFile.WriteToFile(RTFile.ApplicationDirectory + "beatmaps/shapes/setup.lss", jn.ToString(3));
        }

        /// <summary>
        /// Initializes the ObjectManager prefabs.
        /// </summary>
        public void SetupShapes()
        {
            loadedShapes = false;

            var parent = new GameObject("Shape Parent");
            shapeParent = parent.transform;

            foreach (var shapeKeyValue in StoredShapes2D)
            {
                var type = shapeKeyValue.Key.x;
                var option = shapeKeyValue.Key.y;
                var shape = shapeKeyValue.Value;

                // Adds new shape group
                if (ObjectManager.inst.objectPrefabs.Count < type + 1)
                {
                    var p = new ObjectManager.ObjectPrefabHolder();
                    p.options = new List<GameObject>();
                    ObjectManager.inst.objectPrefabs.Add(p);
                }

                // Creates new shape object
                if (ObjectManager.inst.objectPrefabs[type].options.Count < option + 1 && shape.mesh != null)
                {
                    var gameObject = ObjectManager.inst.objectPrefabs[1].options[0].Duplicate(shapeParent, shape.name);

                    gameObject.transform.GetChild(0).GetComponent<MeshFilter>().mesh = shape.mesh;

                    DestroyImmediate(gameObject.transform.GetChild(0).GetComponent<PolygonCollider2D>());
                    gameObject.transform.GetChild(0).gameObject.AddComponent<RTColliderCreator>();

                    ObjectManager.inst.objectPrefabs[type].options.Add(gameObject);
                }

                // Creates image shape object
                if (type != 6)
                    continue;

                if (CoreHelper.InEditor)
                {
                    DestroyImmediate(ObjectManager.inst.objectPrefabs[6].options[0].transform.GetChild(0).GetComponent<Rigidbody2D>());
                    continue;
                }

                var imageMesh = new GameObject("mesh");
                imageMesh.layer = 8;
                var imageObject = new GameObject("object");
                imageObject.layer = 8;
                imageObject.transform.SetParent(imageMesh.transform);

                imageObject.AddComponent<SpriteRenderer>();

                ObjectManager.inst.objectPrefabs[6].options.Add(imageMesh);
            }

            StartCoroutine(SetupPlayerShapes());
        }

        public IEnumerator SetupPlayerShapes()
        {
            if (ObjectManager.inst.objectPrefabs.Count == 10)
            {
                loadedShapes = true;
                yield break;
            }

            var objectPrefab = new ObjectManager.ObjectPrefabHolder();
            objectPrefab.options = new List<GameObject>();

            while (!GameManager.inst || GameManager.inst.PlayerPrefabs == null || GameManager.inst.PlayerPrefabs.Length < 1 || GameManager.inst.PlayerPrefabs[0] == null)
                yield return null;

            var parent = new GameObject("Player Prefabs");
            parent.SetActive(false);

            for (int i = 0; i < PlayerModel.DefaultModels.Count; i++)
            {
                var gameObject = PlayerManager.SpawnPlayer(PlayerModel.DefaultModels[i], parent.transform, 0, Vector3.zero);
                gameObject.SetActive(false);
                objectPrefab.options.Add(gameObject);

                PlayerManager.PlayerModels.AddSet(PlayerModel.DefaultModels[i].basePart.id, PlayerModel.DefaultModels[i]);
            }

            ObjectManager.inst.objectPrefabs.Add(objectPrefab);

            loadedShapes = true;
            yield break;
        }

        public Dictionary<Vector2Int, Shape> StoredShapes2D { get; set; }
        public Dictionary<Vector2Int, Shape> StoredShapes3D { get; set; }

        public List<List<Shape>> Shapes2D { get; set; }

        public List<List<Shape>> Shapes3D { get; set; }

        public Shape GetShape(int shape, int shapeOption)
        {
            shape = Mathf.Clamp(shape, 0, Shapes2D.Count - 1);
            shapeOption = Mathf.Clamp(shapeOption, 0, Shapes2D[shape].Count - 1);

            return Shapes2D[shape][shapeOption];
        }

        public Shape GetShape3D(int shape, int shapeOption)
        {
            shape = Mathf.Clamp(shape, 0, Shapes3D.Count - 1);
            shapeOption = Mathf.Clamp(shapeOption, 0, Shapes3D[shape].Count - 1);

            return Shapes3D[shape][shapeOption];
        }
    }
}
