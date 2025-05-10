using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

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
        public static string ShapesPath => $"{RTFile.BepInExAssetsPath}Shapes/";

        /// <summary>
        /// The path of the shapes setup file.
        /// </summary>
        public static string ShapesSetup => $"{ShapesPath}setup.lss";

        public bool loadedShapes;
        public Transform shapeParent;

        /// <summary>
        /// Exists due to the regular shape collision being messed up.
        /// </summary>
        public static Vector2[] thinTrianglePoints = new Vector2[]
        {
            new Vector2(0f, 0.5774f),
            new Vector2(-0.4908f, -0.2855f),
            new Vector2(-0.4482f, -0.2588f),
            new Vector2(0.4482f, -0.2588f),
            new Vector2(0f, 0.5175f),
            new Vector2(-0.4482f, -0.2588f),
            new Vector2(-0.5001f, -0.2949f),
            new Vector2(0.4866f, -0.2847f),
        };

        /// <summary>
        /// Initializes ShapeManager.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(ShapeManager), SystemManager.inst.transform).AddComponent<ShapeManager>();

        void Awake()
        {
            inst = this;

            if (!RTFile.FileExists(RTFile.GetAsset($"shapes{FileFormat.JSON.Dot()}")))
            {
                System.Windows.Forms.MessageBox.Show("Shapes Setup file does not exist.\nYou may run into issues with playing the game from here on, so it is recommended to\ndownload the proper assets from Github and place them into the appropriate folders.", "Error!");
                return;
            }

            Load();
        }

        public void Load()
        {
            Shapes2D.Clear();
            Shapes3D.Clear();

            var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.GetAsset($"shapes{FileFormat.JSON.Dot()}")));

            for (int i = 0; i < jn["types"].Count; i++)
            {
                Sprite groupIcon = null;
                if (jn["types"][i]["icon"] != null)
                    groupIcon = SpriteHelper.StringToSprite(jn["types"][i]["icon"]);

                var shapeGroup = new ShapeGroup(jn["types"][i]["name"], i, groupIcon);

                for (int j = 0; j < jn["types"][i]["options"].Count; j++)
                {
                    var sjn = jn["types"][i]["options"][j];

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

                    if (sjn["icon"] != null)
                        shape.icon = SpriteHelper.StringToSprite(sjn["icon"]);

                    shapeGroup.Add(shape);
                }

                Shapes2D.Add(shapeGroup);
            }

            jn = JSON.Parse(RTFile.ReadFromFile(RTFile.GetAsset($"shapes_bg{FileFormat.JSON.Dot()}")));

            for (int i = 0; i < jn["types"].Count; i++)
            {
                Sprite groupIcon = null;
                if (jn["types"][i]["icon"] != null)
                    groupIcon = SpriteHelper.StringToSprite(jn["types"][i]["icon"]);

                var shapeGroup = new ShapeGroup(jn["types"][i]["name"], i, groupIcon);

                for (int j = 0; j < jn["types"][i]["options"].Count; j++)
                {
                    var sjn = jn["types"][i]["options"][j];

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

                    if (sjn["icon"] != null)
                        shape.icon = SpriteHelper.StringToSprite(sjn["icon"]);

                    shapeGroup.Add(shape);
                }

                Shapes3D.Add(shapeGroup);
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
                    RTFile.CreateDirectory(fullPath);

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
                            sjn["tris"][k] = mesh.triangles[k].ToString();
                    }
                    else
                        sjn["p"] = i == 4 ? 1 : i == 6 ? 2 : 3;

                    RTFile.WriteToFile(RTFile.CombinePaths(fullPath, $"data{FileFormat.LSSH.Dot()}"), jn.ToString());
                }
            }

            RTFile.WriteToFile(RTFile.ApplicationDirectory + $"beatmaps/shapes/setup{FileFormat.LSS.Dot()}", jn.ToString(3));
        }

        /// <summary>
        /// Initializes the ObjectManager prefabs.
        /// </summary>
        public void SetupShapes()
        {
            loadedShapes = false;

            var parent = new GameObject("Shape Parent");
            parent.SetActive(false);
            shapeParent = parent.transform;

            for (int i = 0; i < Shapes2D.Count; i++)
            {
                var shapeGroup = Shapes2D[i];
                for (int j = 0; j < shapeGroup.Count; j++)
                {
                    var shape = shapeGroup[j];
                    var type = shape.type;
                    var option = shape.option;

                    // Adds new shape group
                    if (ObjectManager.inst.objectPrefabs.Count < type + 1)
                        ObjectManager.inst.objectPrefabs.Add(new ObjectManager.ObjectPrefabHolder() { options = new List<GameObject>() });

                    // Creates new shape object
                    if (ObjectManager.inst.objectPrefabs[type].options.Count < option + 1 && shape.mesh)
                    {
                        var gameObject = ObjectManager.inst.objectPrefabs[1].options[0].Duplicate(shapeParent, shape.name);

                        gameObject.transform.GetChild(0).GetComponent<MeshFilter>().mesh = shape.mesh;

                        if (shape.name == "triangle_outline_thin")
                        {
                            var polygonCollider = gameObject.transform.GetChild(0).GetComponent<PolygonCollider2D>();
                            polygonCollider.points = thinTrianglePoints.Copy();
                            polygonCollider.pathCount = 1;

                            ObjectManager.inst.objectPrefabs[type].options.Add(gameObject);
                            continue;
                        }

                        CoreHelper.CreateCollider(gameObject.transform.GetChild(0).GetComponent<PolygonCollider2D>(), shape.mesh);
                        ObjectManager.inst.objectPrefabs[type].options.Add(gameObject);
                    }

                    // Creates image shape object
                    if (type != 6)
                        continue;

                    // Clear to remove original image object.
                    // Originally I just kept the original "mesh" object and just got rid of the rigidbody it has (for some reason, wtf)
                    // The rigidbody removal code didn't work until I changed the check. Then it started to work BUT it started to crash as well. ('Attempt to access invalid address.' crash)
                    // So instead, we clear the Image group options and add our own. I guess this makes the image object 100% the same as in the arcade now.
                    ObjectManager.inst.objectPrefabs[6].options.Clear();
                    var imageMesh = new GameObject("mesh");
                    imageMesh.layer = 8;
                    var imageObject = new GameObject("object");
                    imageObject.layer = 8;
                    imageObject.transform.SetParent(imageMesh.transform);

                    imageObject.AddComponent<SpriteRenderer>();

                    ObjectManager.inst.objectPrefabs[6].options.Add(imageMesh);
                }
            }

            for (int i = 0; i < ObjectManager.inst.objectPrefabs.Count; i++)
            {
                var objectPrefab = ObjectManager.inst.objectPrefabs[i];
                for (int j = 0; j < objectPrefab.options.Count; j++)
                {
                    var gameObject = objectPrefab.options[j];
                    if (gameObject)
                    {
                        var collider = gameObject.GetComponentInChildren<Collider2D>();
                        if (collider)
                            collider.enabled = false;
                        gameObject.SetActive(false);
                    }
                }
            }

            loadedShapes = true;
        }

        /// <summary>
        /// List of 2D shapes.
        /// </summary>
        public List<ShapeGroup> Shapes2D { get; set; } = new List<ShapeGroup>();

        /// <summary>
        /// List of 3D shapes.
        /// </summary>
        public List<ShapeGroup> Shapes3D { get; set; } = new List<ShapeGroup>();

        public Shape GetShape(int shape, int shapeOption) => Shapes2D[Mathf.Clamp(shape, 0, Shapes2D.Count - 1)].GetShape(shapeOption);

        public Shape GetShape3D(int shape, int shapeOption) => Shapes3D[Mathf.Clamp(shape, 0, Shapes3D.Count - 1)].GetShape(shapeOption);
    }
}
