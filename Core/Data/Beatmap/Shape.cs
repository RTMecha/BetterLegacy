using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Shape data class used for objects in levels.
    /// </summary>
    public class Shape
    {
        public Shape(string name, int type, int option)
        {
            this.name = name;
            this.type = type;
            this.option = option;
            mesh = null;
            icon = null;
            property = Property.RegularObject;
            EditorElement = null;
            Toggle = null;
            GameObject = null;
        }

        public Shape(string name, int type, int option, Mesh mesh, Sprite icon, Property property)
        {
            this.name = name;
            this.type = type;
            this.option = option;
            this.mesh = mesh;
            this.icon = icon;
            this.property = property;
            EditorElement = null;
            Toggle = null;
            GameObject = null;
        }

        #region Values

        #region Main

        /// <summary>
        /// Name of the shape.
        /// </summary>
        public string name;

        /// <summary>
        /// Built-in Legacy name of the shape.
        /// </summary>
        public string internalName;

        /// <summary>
        /// Group type of the shape.
        /// </summary>
        public int type;
        /// <summary>
        /// Group option of the shape.
        /// </summary>
        public int option;

        /// <summary>
        /// The kind of property the shape has.
        /// </summary>
        public Property property;

        /// <summary>
        /// The type of collision.
        /// </summary>
        public CollisionType collision;

        public Vector2Int Vector => new Vector2Int(Type, Option);

        /// <summary>
        /// Clamps the shape type.
        /// </summary>
        public int Type
        {
            get => Mathf.Clamp(type, 0, maxShapes.Length - 1);
            set => type = Mathf.Clamp(value, 0, maxShapes.Length - 1);
        }

        /// <summary>
        /// Clamps the shape option.
        /// </summary>
        public int Option
        {
            get => Mathf.Clamp(option, 0, maxShapes[Type]);
            set => option = Mathf.Clamp(value, 0, maxShapes[Type]);
        }

        #endregion

        #region Object

        /// <summary>
        /// Triangles array.
        /// </summary>
        public int[] tris;

        /// <summary>
        /// Vertices array.
        /// </summary>
        public Vector3[] verts;

        /// <summary>
        /// Collision points list.
        /// </summary>
        public List<Vector2> collisionBounds;

        /// <summary>
        /// Mesh reference.
        /// </summary>
        public Mesh mesh;

        /// <summary>
        /// UI sprite of the shape.
        /// </summary>
        public Sprite icon;

        #region GameObject

        /// <summary>
        /// Editor toggle.
        /// </summary>
        public GameObject EditorElement { get; set; }

        /// <summary>
        /// Editor toggle component.
        /// </summary>
        public Toggle Toggle { get; set; }

        /// <summary>
        /// Game object prefab.
        /// </summary>
        public GameObject GameObject { get; set; }

        #endregion

        #endregion

        public int this[int index]
        {
            get => index switch
            {
                0 => Type,
                1 => Option,
                _ => throw new System.IndexOutOfRangeException("Invalid Shape index!"),
            };
            set
            {
                switch (index)
                {
                    case 0:
                        {
                            Type = value;
                            break;
                        }
                    case 1:
                        {
                            Option = value;
                            break;
                        }
                    default:
                        throw new System.IndexOutOfRangeException("Invalid Shape index!");
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a copy of a shape.
        /// </summary>
        /// <param name="orig">Shape to copy.</param>
        /// <returns>Returns a copied shape.</returns>
        public static Shape DeepCopy(Shape orig) => new Shape(orig.name, orig.type, orig.option, orig.mesh, orig.icon, orig.property);

        /// <summary>
        /// Parses a shape from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed shape.</returns>
        public static Shape Parse(JSONNode jn)
        {
            var shape = new Shape(jn["name"], jn["s"].AsInt, jn["so"].AsInt);
            shape.internalName = jn["internal_name"];

            shape.mesh = new Mesh();
            shape.mesh.name = shape.name;

            var vertices = new Vector3[jn["verts"].Count];
            for (int i = 0; i < jn["verts"].Count; i++)
                vertices[i] = new Vector3(jn["verts"][i]["x"].AsFloat, jn["verts"][i]["y"].AsFloat, jn["verts"][i]["z"].AsFloat);

            shape.mesh.vertices = vertices;
            shape.verts = vertices;

            var triangles = new int[jn["tris"].Count];
            for (int i = 0; i < jn["tris"].Count; i++)
                triangles[i] = jn["tris"][i].AsInt;

            shape.mesh.triangles = triangles;
            shape.tris = triangles;

            if (jn["collision_bounds"] != null)
            {
                shape.collisionBounds = new List<Vector2>();
                for (int i = 0; i < jn["collision_bounds"].Count; i++)
                    shape.collisionBounds.Add(Parser.TryParse(jn["collision_bounds"][i], Vector2.zero));
            }

            if (jn["icon"] != null)
                shape.icon = SpriteHelper.StringToSprite(jn["icon"]);

            shape.property = (Property)jn["p"].AsInt;

            return shape;
        }

        /// <summary>
        /// Copies the shapes' mesh.
        /// </summary>
        /// <returns>Returns a copied mesh.</returns>
        public Mesh CopyMesh() => new Mesh
        {
            name = mesh.name,
            vertices = mesh.vertices.Copy(),
            triangles = mesh.triangles.Copy()
        };

        public void Clamp()
        {
            type = Mathf.Clamp(type, 0, maxShapes.Length - 1);
            option = Mathf.Clamp(option, 0, maxShapes[type]);
        }

        #endregion

        #region Operators

        public static bool operator ==(Shape a, Shape b) => a.type == b.type && a.option == b.option;
        public static bool operator !=(Shape a, Shape b) => !(a == b);

        public static bool operator >(Shape a, Shape b) => a.type > b.type && a.option > b.option;
        public static bool operator <(Shape a, Shape b) => a.type < b.type && a.option < b.option;
        public static bool operator >=(Shape a, Shape b) => a.type >= b.type && a.option >= b.option;
        public static bool operator <=(Shape a, Shape b) => a.type <= b.type && a.option <= b.option;

        public static implicit operator bool(Shape exists) => exists != null;

        public override bool Equals(object obj) => obj is Shape shape && this == shape;

        public override int GetHashCode() => type.GetHashCode() ^ option.GetHashCode();

        public override string ToString() => $"{name}: ({type}, {option})";

        #endregion

        #region Global

        /// <summary>
        /// Maximum modded shapes.
        /// </summary>
        public static int[] maxShapes = new int[]
        {
            6,
            17,
            5,
            3,
            1,
            6,
            1,
            6,
            23
        };

        public static int[] unmoddedMaxShapes = new int[]
        {
            3,
            9,
            4,
            2,
            1,
            6
        };

        public enum Property
        {
            RegularObject = 0,
            TextObject = 1,
            ImageObject = 2,
            PlayerObject = 3,
            PolygonObject = 4,
            ParticlesObject = 5,
        }

        public enum CollisionType
        {
            BoxCollider,
            CircleCollider,
            PolygonCollider,
        }

        #endregion
    }
}
