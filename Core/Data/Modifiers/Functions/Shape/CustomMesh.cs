using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class CustomMesh : ModifierActionBase
    {
        #region Constructors

        public CustomMesh() => SetupModifier(new string[]
        {
            "0", // 0: vertice count
            "0", // 1: triangle count
            "0", // 2: normal count
            "0", // 3: tangent count
            "0", // 4: colors count
        });

        #endregion

        #region Values

        public override string Name => "customMesh";

        public override ModifierCategoryType Category => ModifierCategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            Cache cache;
            if (modifier.TryGetResult(out cache) && cache.meshFilter)
            {
                var mesh = cache.meshFilter.mesh;
                int valueIndex = 0;

                var vertexCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                valueIndex++;
                var verticesChanged = cache.vertices.Length != vertexCount;
                if (verticesChanged)
                    cache.vertices = new Vector3[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    var x = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var y = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var z = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var vertex = new Vector3(x, y, z);
                    if (cache.vertices[i] == vertex)
                        continue;

                    cache.vertices[i] = new Vector3(x, y, z);
                    verticesChanged = true;
                }
                if (verticesChanged)
                    mesh.vertices = cache.vertices;

                var triangleCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                valueIndex++;
                var trianglesChanged = cache.triangles.Length != triangleCount;
                if (trianglesChanged)
                    cache.triangles = new int[triangleCount];
                for (int i = 0; i < triangleCount; i++)
                {
                    var triangle = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                    valueIndex++;
                    if (cache.triangles[i] == triangle)
                        continue;
                    cache.triangles[i] = triangle;
                    trianglesChanged = true;
                }
                if (trianglesChanged)
                    mesh.triangles = cache.triangles;

                var normalCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                valueIndex++;
                var normalsChanged = cache.normals.Length != normalCount;
                if (normalsChanged)
                    cache.normals = new Vector3[normalCount];
                for (int i = 0; i < normalCount; i++)
                {
                    var x = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var y = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var z = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var normal = new Vector3(x, y, z);
                    if (cache.normals[i] == normal)
                        continue;
                    cache.normals[i] = normal;
                    normalsChanged = true;
                }
                if (normalsChanged)
                    mesh.normals = cache.normals;

                var tangentCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                valueIndex++;
                var tangentsChanged = cache.tangents.Length != tangentCount;
                if (tangentsChanged)
                    cache.tangents = new Vector4[tangentCount];
                for (int i = 0; i < tangentCount; i++)
                {
                    var x = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var y = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var z = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var w = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var tangent = new Vector4(x, y, z, w);
                    if (cache.tangents[i] == tangent)
                        continue;
                    cache.tangents[i] = tangent;
                    tangentsChanged = true;
                }
                if (tangentsChanged)
                    mesh.tangents = cache.tangents;

                var colorCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                valueIndex++;
                var colorsChanged = cache.colors.Length != colorCount;
                if (colorsChanged)
                    cache.colors = new Color[colorCount];
                for (int i = 0; i < colorCount; i++)
                {
                    var color = RTColors.HexToColor(modifier.GetValue(valueIndex, modifierLoop.variables));
                    valueIndex++;
                    if (cache.colors[i] == color)
                        continue;
                    cache.colors[i] = color;
                    colorsChanged = true;
                }
                if (colorsChanged)
                    mesh.colors = cache.colors;

                if (verticesChanged)
                    mesh.RecalculateBounds();
            }
            else
            {
                if (beatmapObject.IsSpecialShape || !runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                    return;

                cache = new Cache();
                var mesh = new Mesh();
                int valueIndex = 0;

                var vertexCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                valueIndex++;
                cache.vertices = new Vector3[vertexCount];
                for (int i = 0; i < vertexCount; i++)
                {
                    var x = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var y = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var z = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    cache.vertices[i] = new Vector3(x, y, z);
                }
                mesh.vertices = cache.vertices;

                var triangleCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                valueIndex++;
                cache.triangles = new int[triangleCount];
                for (int i = 0; i < triangleCount; i++)
                {
                    cache.triangles[i] = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                    valueIndex++;
                }
                mesh.triangles = cache.triangles;

                var normalCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                valueIndex++;
                cache.normals = new Vector3[normalCount];
                for (int i = 0; i < normalCount; i++)
                {
                    var x = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var y = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var z = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    cache.normals[i] = new Vector3(x, y, z);
                }
                mesh.normals = cache.normals;

                var tangentCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                valueIndex++;
                cache.tangents = new Vector4[tangentCount];
                for (int i = 0; i < tangentCount; i++)
                {
                    var x = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var y = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var z = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    var w = modifier.GetFloat(valueIndex, 0f, modifierLoop.variables);
                    valueIndex++;
                    cache.tangents[i] = new Vector4(x, y, z, w);
                }
                mesh.tangents = cache.tangents;

                var colorCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
                valueIndex++;
                cache.colors = new Color[colorCount];
                for (int i = 0; i < colorCount; i++)
                {
                    cache.colors[i] = RTColors.HexToColor(modifier.GetValue(valueIndex, modifierLoop.variables));
                    valueIndex++;
                }
                mesh.colors = cache.colors;

                mesh.RecalculateBounds();
                var meshFilter = runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>();
                meshFilter.mesh = mesh;
                cache.meshFilter = meshFilter;
                modifier.Result = cache;
                runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            var modifierLoop = reference.GetModifierLoop();
            if (!modifierLoop)
                return;

            int valueIndex = 0;

            var vertexCountIndex = valueIndex;
            var vertexCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
            valueIndex++;
            for (int i = 0; i < vertexCount; i++)
            {
                var startIndex = valueIndex;
                var label = modifierCard.LabelGenerator($"- Vertex {i}");
                modifierCard.DeleteGenerator(modifier, reference, label.transform, () =>
                {
                    for (int i = 0; i < 3; i++)
                        modifier.values.RemoveAt(startIndex);
                    modifier.values[vertexCountIndex] = (vertexCount - 3).ToString();
                });

                modifierCard.SingleGenerator(modifier, reference, "X", valueIndex);
                valueIndex++;
                modifierCard.SingleGenerator(modifier, reference, "Y", valueIndex);
                valueIndex++;
                modifierCard.SingleGenerator(modifier, reference, "Z", valueIndex);
                valueIndex++;
            }
            // Add
            {
                var _valueIndex = valueIndex;
                modifierCard.AddGenerator(modifier, reference, "Add Vertex", () =>
                {
                    for (int i = 0; i < 3; i++)
                        modifier.values.Insert(_valueIndex, "0");
                    modifier.values[vertexCountIndex] = (vertexCount + 1).ToString();
                }).AddComponent<LayoutElement>().minWidth = 304f;
            }
            var triangleCountIndex = valueIndex;
            var triangleCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
            valueIndex++;
            int tri = 0;
            for (int i = 0; i < triangleCount; i++)
            {
                var startIndex = valueIndex;
                if (i % 3 == 0)
                {
                    var label = modifierCard.LabelGenerator($"- Triangle {tri}");
                    modifierCard.DeleteGenerator(modifier, reference, label.transform, () =>
                    {
                        modifier.values.RemoveAt(startIndex);
                        modifier.values[triangleCountIndex] = (triangleCount - 1).ToString();
                    });
                    tri++;
                }

                modifierCard.IntegerGenerator(modifier, reference, "Point", valueIndex);
                valueIndex++;
            }
            // Add
            {
                var _valueIndex = valueIndex;
                modifierCard.AddGenerator(modifier, reference, "Add Triangle", () =>
                {
                    var c = triangleCount % 3;
                    // if triangleCount = 3, then c = 0
                    // if triangleCount = 4, then c = 1
                    // c = 1
                    // c - 3 = -2

                    for (int i = 0; i < -(c - 3); i++)
                        modifier.values.Insert(_valueIndex, "0");
                    modifier.values[triangleCountIndex] = (triangleCount + 3).ToString();
                }).AddComponent<LayoutElement>().minWidth = 304f;
            }
            var normalCountIndex = valueIndex;
            var normalCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
            valueIndex++;
            for (int i = 0; i < normalCount; i++)
            {
                var startIndex = valueIndex;
                var label = modifierCard.LabelGenerator($"- Normal {i}");
                modifierCard.DeleteGenerator(modifier, reference, label.transform, () =>
                {
                    for (int i = 0; i < 3; i++)
                        modifier.values.RemoveAt(startIndex);
                    modifier.values[normalCountIndex] = (normalCount - 3).ToString();
                });

                modifierCard.SingleGenerator(modifier, reference, "Normal X", valueIndex);
                valueIndex++;
                modifierCard.SingleGenerator(modifier, reference, "Normal Y", valueIndex);
                valueIndex++;
                modifierCard.SingleGenerator(modifier, reference, "Normal Z", valueIndex);
                valueIndex++;
            }
            // Add
            {
                var _valueIndex = valueIndex;
                modifierCard.AddGenerator(modifier, reference, "Add Normal", () =>
                {
                    for (int i = 0; i < 3; i++)
                        modifier.values.Insert(_valueIndex, "0");
                    modifier.values[normalCountIndex] = (normalCount + 1).ToString();
                }).AddComponent<LayoutElement>().minWidth = 304f;
            }
            var tangentCountIndex = valueIndex;
            var tangentCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
            valueIndex++;
            for (int i = 0; i < tangentCount; i++)
            {
                var startIndex = valueIndex;
                var label = modifierCard.LabelGenerator($"- Tangent {i}");
                modifierCard.DeleteGenerator(modifier, reference, label.transform, () =>
                {
                    for (int i = 0; i < 4; i++)
                        modifier.values.RemoveAt(startIndex);
                    modifier.values[tangentCountIndex] = (tangentCount - 4).ToString();
                });

                modifierCard.SingleGenerator(modifier, reference, "Tangent X", valueIndex);
                valueIndex++;
                modifierCard.SingleGenerator(modifier, reference, "Tangent Y", valueIndex);
                valueIndex++;
                modifierCard.SingleGenerator(modifier, reference, "Tangent Z", valueIndex);
                valueIndex++;
                modifierCard.SingleGenerator(modifier, reference, "Tangent W", valueIndex);
                valueIndex++;
            }
            // Add
            {
                var _valueIndex = valueIndex;
                modifierCard.AddGenerator(modifier, reference, "Add Tangent", () =>
                {
                    for (int i = 0; i < 4; i++)
                        modifier.values.Insert(_valueIndex, "0");
                    modifier.values[tangentCountIndex] = (tangentCount + 1).ToString();
                }).AddComponent<LayoutElement>().minWidth = 304f;
            }
            var colorCountIndex = valueIndex;
            var colorCount = modifier.GetInt(valueIndex, 0, modifierLoop.variables);
            valueIndex++;
            for (int i = 0; i < colorCount; i++)
            {
                var startIndex = valueIndex;
                var label = modifierCard.LabelGenerator($"- Color {i}");
                modifierCard.DeleteGenerator(modifier, reference, label.transform, () =>
                {
                    modifier.values.RemoveAt(startIndex);
                    modifier.values[colorCountIndex] = (colorCount - 1).ToString();
                });

                var hexCode = modifierCard.StringGenerator(modifier, reference, "Primary Hex Code", valueIndex).transform.Find("Input").GetComponent<InputField>();
                valueIndex++;
                var _valueIndex = valueIndex;
                EditorContextMenu.AddContextMenu(hexCode.gameObject,
                    EditorContextMenu.GetEditorColorFunctions(hexCode, () => modifier.GetValue(_valueIndex)));
            }
            // Add
            {
                var _valueIndex = valueIndex;
                modifierCard.AddGenerator(modifier, reference, "Add Color", () =>
                {
                    modifier.values.Insert(_valueIndex, RTColors.WHITE_HEX_CODE);
                    modifier.values[colorCountIndex] = (colorCount + 1).ToString();
                }).AddComponent<LayoutElement>().minWidth = 304f;
            }
        }

        #endregion

        #region Sub Classes

        public class Cache : Exists
        {
            public MeshFilter meshFilter;

            public Vector3[] vertices;

            public int[] triangles;

            public Vector3[] normals;

            public Vector4[] tangents;

            public Color[] colors;
        }

        #endregion
    }
}
