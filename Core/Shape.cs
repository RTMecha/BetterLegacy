using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core
{
    public struct Shape
    {
        public Shape(string name, int type, int option)
        {
            this.name = name;
            this.type = type;
            this.option = option;
            mesh = null;
            Icon = null;
            SpecialProperty = Property.RegularObject;
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
            Icon = icon;
            SpecialProperty = property;
            EditorElement = null;
            Toggle = null;
            GameObject = null;
        }

        public string name;

        public int type;
        public int option;

        public Mesh mesh;
        public Sprite Icon { get; set; }
        public GameObject EditorElement { get; set; }
        public Toggle Toggle { get; set; }
        public GameObject GameObject { get; set; }

        public Vector2Int Vector => new Vector2Int(Type, Option);

        public int Type
        {
            get => Mathf.Clamp(type, 0, maxShapes.Length - 1);
            set => type = Mathf.Clamp(value, 0, maxShapes.Length - 1);
        }

        public int Option
        {
            get => Mathf.Clamp(option, 0, maxShapes[Type]);
            set => option = Mathf.Clamp(value, 0, maxShapes[Type]);
        }

        public enum Property
        {
            RegularObject,
            TextObject,
            ImageObject,
            PlayerObject,
        }

        public Property SpecialProperty { get; set; }

        public int this[int index]
        {
            get
            {
                int result;
                switch (index)
                {
                    case 0:
                        {
                            result = Type;
                            break;
                        }
                    case 1:
                        {
                            result = Option;
                            break;
                        }
                    default:
                        throw new System.IndexOutOfRangeException("Invalid Shape index!");
                }
                return result;
            }
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

        #region Methods

        public static Shape DeepCopy(Shape orig) => new Shape
        {
            name = orig.name,
            type = orig.type,
            option = orig.option,
            mesh = orig.mesh
        };

        public static Shape Parse(JSONNode jn)
        {
            var shape = new Shape(jn["name"], jn["s"].AsInt, jn["so"].AsInt);

            shape.mesh = new Mesh();

            var vertices = new Vector3[jn["verts"].Count];
            for (int i = 0; i < jn["verts"].Count; i++)
                vertices[i] = new Vector3(jn["verts"][i]["x"].AsFloat, jn["verts"][i]["y"].AsFloat, jn["verts"][i]["z"].AsFloat);

            shape.mesh.vertices = vertices;

            var triangles = new int[jn["tris"].Count];
            for (int i = 0; i < jn["tris"].Count; i++)
                triangles[i] = jn["tris"][i].AsInt;

            shape.mesh.triangles = triangles;

            return shape;
        }

        public Mesh CopyMesh() => new Mesh
        {
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
        public static bool operator !=(Shape a, Shape b) => a.type != b.type || a.option != b.option;

        public static bool operator >(Shape a, Shape b) => a.type > b.type && a.option > b.option;
        public static bool operator <(Shape a, Shape b) => a.type < b.type && a.option < b.option;
        public static bool operator >=(Shape a, Shape b) => a.type >= b.type && a.option >= b.option;
        public static bool operator <=(Shape a, Shape b) => a.type <= b.type && a.option <= b.option;

        public static implicit operator bool(Shape exists) => exists != null;

        public override bool Equals(object obj)
        {
            if (obj is Shape)
                return this == (Shape)obj;
            return false;
        }

        public override int GetHashCode() => type.GetHashCode() ^ option.GetHashCode();

        public override string ToString() => $"{name}: ({type}, {option})";

        #endregion

        #region Global Properties

        public static int[] MaxShapes => maxShapes;

        static int[] maxShapes = new int[]
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

        #endregion
    }
}
