using BetterLegacy.Components;
using BetterLegacy.Core.Data.Player;
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

        public static string ShapesPath => "beatmaps/shapes/";
        public static string ShapesSetup => $"{ShapesPath}setup.lss";

        bool gameHasLoaded;
        public bool loadedShapes;

        void Awake()
        {
            inst = this;
        }

        void Start()
        {

        }

        void Update()
        {
            //if (ObjectManager.inst && !gameHasLoaded)
            //{
            //    gameHasLoaded = true;
            //    loadedShapes = false;
            //    Load();
            //}
            //else if (!ObjectManager.inst)
            //{
            //    gameHasLoaded = false;
            //}
        }

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

                    if (i != 4 && i != 6)
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
                        sjn["p"] = i == 4 ? 1 : 2;
                    }

                    RTFile.WriteToFile(fullPath + "/data.lssh", jn.ToString());
                }
            }

            RTFile.WriteToFile(RTFile.ApplicationDirectory + "beatmaps/shapes/setup.lss", jn.ToString(3));
        }

        public void Load()
        {
            if (!RTFile.FileExists(RTFile.ApplicationDirectory + ShapesSetup))
            {
                System.Windows.Forms.MessageBox.Show("Shapes Setup file does not exist.\nYou may run into issues with playing the game from here on, so it is recommended to\ndownload the proper assets from Github and place them into the appropriate folders.", "Error!");
                return;
            }

            loadedShapes = false;

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

                        if (ObjectManager.inst.objectPrefabs.Count < i + 1)
                        {
                            var p = new ObjectManager.ObjectPrefabHolder();
                            p.options = new List<GameObject>();
                            ObjectManager.inst.objectPrefabs.Add(p);
                        }

                        if (ObjectManager.inst.objectPrefabs[i].options.Count < j + 1 && mesh != null)
                        {
                            var gameObject = ObjectManager.inst.objectPrefabs[1].options[0].Duplicate(null, sjn["name"]);

                            gameObject.transform.GetChild(0).GetComponent<MeshFilter>().mesh = mesh;

                            DestroyImmediate(gameObject.transform.GetChild(0).GetComponent<PolygonCollider2D>());
                            gameObject.transform.GetChild(0).gameObject.AddComponent<RTColliderCreator>();

                            ObjectManager.inst.objectPrefabs[i].options.Add(gameObject);
                        }

                        if (i == 6 && EditorManager.inst == null)
                        {
                            var imageMesh = new GameObject("mesh");
                            imageMesh.layer = 8;
                            var imageObject = new GameObject("object");
                            imageObject.layer = 8;
                            imageObject.transform.SetParent(imageMesh.transform);

                            imageObject.AddComponent<SpriteRenderer>();
                            var imageRB = imageObject.AddComponent<Rigidbody2D>();

                            imageRB.angularDrag = 0f;
                            imageRB.bodyType = RigidbodyType2D.Kinematic;
                            imageRB.gravityScale = 0f;
                            imageRB.inertia = 0f;

                            ObjectManager.inst.objectPrefabs[6].options.Add(imageMesh);
                        }

                        var shape = new Shape(sjn["name"], i, j, mesh, null, string.IsNullOrEmpty(sjn["p"]) ? Shape.Property.RegularObject : (Shape.Property)sjn["p"].AsInt);

                        if (RTFile.FileExists(fullPath + "/icon.png"))
                        {
                            shape.Icon = SpriteManager.LoadSprite(fullPath + "/icon.png");
                        }

                        shape.GameObject = ObjectManager.inst.objectPrefabs[i].options[j];

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

                        Shapes3D[i].Add(shape);
                    }
                    else
                    {
                        var shape = new Shape("null", i, j, null, null, i == 4 ? Shape.Property.TextObject : i == 6 ? Shape.Property.ImageObject : Shape.Property.RegularObject);

                        if (RTFile.FileExists(fullPath + "/icon.png"))
                        {
                            shape.Icon = SpriteManager.LoadSprite(fullPath + "/icon.png");
                        }

                        Shapes3D[i].Add(shape);
                    }
                }
            }

            StartCoroutine(SetupPlayerShapes());
        }

        public IEnumerator SetupPlayerShapes()
        {
            if (ObjectManager.inst.objectPrefabs.Count != 10)
            {
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

                    if (!PlayerManager.PlayerModels.ContainsKey(PlayerModel.DefaultModels[i].basePart.id))
                        PlayerManager.PlayerModels.Add(PlayerModel.DefaultModels[i].basePart.id, PlayerModel.DefaultModels[i]);
                    else
                        PlayerManager.PlayerModels[PlayerModel.DefaultModels[i].basePart.id] = PlayerModel.DefaultModels[i];
                }

                ObjectManager.inst.objectPrefabs.Add(objectPrefab);
            }

            loadedShapes = true;
            yield break;
        }

        IEnumerator LoadIcons()
        {
            for (int i = 0; i < Shapes2D.Count; i++)
            {
                for (int j = 0; j < Shapes2D[i].Count; j++)
                {
                    string fullPath = RTFile.ApplicationDirectory + ShapesPath + Shapes2D[i][j].name + "/icon.png";
                    if (RTFile.FileExists(fullPath))
                    {
                        //Debug.Log($"{FunctionsPlugin.className}Setting Icon for {Shapes2D[i][j].name}");
                        yield return Networking.AlephNetworkManager.inst.StartCoroutine(Networking.AlephNetworkManager.DownloadImageTexture("file://" + fullPath, delegate (Texture2D texture2D)
                        {
                            var sprite = SpriteManager.CreateSprite(texture2D);
                            var s = Shapes2D[i][j];
                            s.Icon = sprite;
                            Shapes2D[i][j] = s;

                            if (Shapes3D.Count > i && Shapes3D[i].Count > j)
                            {
                                var s3 = Shapes3D[i][j];
                                s3.Icon = sprite;
                                Shapes3D[i][j] = s3;
                            }
                        }));
                    }
                }
            }

            yield break;
        }

        public List<List<Shape>> Shapes2D { get; set; }

        static List<Shape> shapes;
        public static List<Shape> Shapes
        {
            get => shapes;
            set => shapes = value;
        }

        public List<List<Shape>> Shapes3D { get; set; }

        public Shape GetShape(int shape, int shapeOption)
        {
            if (!Shapes.Has(x => x.Type == shape && x.Option == shapeOption))
                return Shapes[0];

            return Shapes.Find(x => x.Type == shape && x.Option == shapeOption);
        }

        public Shape GetShape3D(int shape, int shapeOption)
        {
            shape = Mathf.Clamp(shape, 0, Shapes3D.Count - 1);
            shapeOption = Mathf.Clamp(shapeOption, 0, Shapes3D[shape].Count - 1);

            return Shapes3D[shape][shapeOption];
        }
    }
}
