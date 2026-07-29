using System.Linq;

using UnityEngine;

using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class TranslateShape : ModifierActionBase
    {
        #region Constructors

        public TranslateShape(bool is3D)
        {
            this.is3D = is3D;
            Name = "translateShape";
            if (is3D)
                Name += "3D";
            if (is3D)
                SetupModifier("0", "0", "0", "0", "1", "1", "1", "0", "0", "0");
            else
                SetupModifier("0", "0", "0", "1", "1", "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool is3D;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            if (is3D)
            {
                var pos = new Vector3(modifier.GetFloat(1, 0f, modifierLoop.variables), modifier.GetFloat(2, 0f, modifierLoop.variables), modifier.GetFloat(3, 0f, modifierLoop.variables));
                var sca = new Vector3(modifier.GetFloat(4, 0f, modifierLoop.variables), modifier.GetFloat(5, 0f, modifierLoop.variables), modifier.GetFloat(6, 0f, modifierLoop.variables));
                var rot = new Vector3(modifier.GetFloat(7, 0f, modifierLoop.variables), modifier.GetFloat(8, 0f, modifierLoop.variables), modifier.GetFloat(9, 0f, modifierLoop.variables));

                if (!modifier.HasResult())
                {
                    var meshFilter = runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>();
                    var collider2D = runtimeObject.visualObject.collider as PolygonCollider2D;
                    var mesh = meshFilter.mesh;

                    var translateShapeCache = new Cache3D
                    {
                        meshFilter = meshFilter,
                        collider2D = collider2D,
                        vertices = mesh?.vertices ?? null,
                        points = collider2D?.points ?? null,

                        pos = pos,
                        sca = sca,
                        rot = rot,
                    };
                    modifier.Result = translateShapeCache;
                    // force translate for first frame
                    translateShapeCache.Translate(pos, sca, rot, true);

                    runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                    return;
                }

                if (modifier.TryGetResult(out Cache3D shapeCache))
                    shapeCache.Translate(pos, sca, rot);
            }
            else
            {
                var pos = new Vector2(modifier.GetFloat(1, 0f, modifierLoop.variables), modifier.GetFloat(2, 0f, modifierLoop.variables));
                var sca = new Vector2(modifier.GetFloat(3, 0f, modifierLoop.variables), modifier.GetFloat(4, 0f, modifierLoop.variables));
                var rot = modifier.GetFloat(5, 0f, modifierLoop.variables);

                if (!modifier.HasResult())
                {
                    var meshFilter = runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>();
                    var collider2D = runtimeObject.visualObject.collider as PolygonCollider2D;
                    var mesh = meshFilter.mesh;

                    var translateShapeCache = new Cache2D
                    {
                        meshFilter = meshFilter,
                        collider2D = collider2D,
                        vertices = mesh?.vertices ?? null,
                        points = collider2D?.points ?? null,

                        pos = pos,
                        sca = sca,
                        rot = rot,
                    };
                    modifier.Result = translateShapeCache;
                    // force translate for first frame
                    translateShapeCache.Translate(pos, sca, rot, true);

                    runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                    return;
                }

                if (modifier.TryGetResult(out Cache2D shapeCache))
                    shapeCache.Translate(pos, sca, rot);
            }
        }

        public override void OnRemoveCache(Modifier modifier)
        {
            if (!modifier.TryGetResult(out Cache cache))
                return;

            if (cache.meshFilter && cache.vertices != null)
                cache.meshFilter.mesh.vertices = cache.vertices;
            if (cache.collider2D && cache.points != null)
                cache.collider2D.points = cache.points;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (is3D)
            {
                modifierCard.SingleGenerator(modifier, reference, "Pos X", 1, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Pos Y", 2, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Pos Z", 3, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Sca X", 4, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Sca Y", 5, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Sca Z", 6, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Rot X", 7, 0f, 15f, 3f);
                modifierCard.SingleGenerator(modifier, reference, "Rot Y", 8, 0f, 15f, 3f);
                modifierCard.SingleGenerator(modifier, reference, "Rot Z", 9, 0f, 15f, 3f);
                return;
            }

            modifierCard.SingleGenerator(modifier, reference, "Pos X", 1, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Pos Y", 2, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Sca X", 3, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Sca Y", 4, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Rot", 5, 0f, 15f, 3f);
        }

        #endregion

        #region Sub Classes

        /// <summary>
        /// Base cache.
        /// </summary>
        public class Cache
        {
            /// <summary>
            /// Cached mesh filter.
            /// </summary>
            public MeshFilter meshFilter;
            /// <summary>
            /// Cached polygon collider.
            /// </summary>
            public PolygonCollider2D collider2D;
            /// <summary>
            /// Original vertices to translate.
            /// </summary>
            public Vector3[] vertices;
            /// <summary>
            /// Original collider points.
            /// </summary>
            public Vector2[] points;
        }

        /// <summary>
        /// Cache for the 2D variant.
        /// </summary>
        public class Cache2D : Cache
        {
            /// <summary>
            /// Translates the mesh.
            /// </summary>
            /// <param name="pos">Position to translate to.</param>
            /// <param name="sca">Scale to translate to.</param>
            /// <param name="rot">Rotation to tranlsate to.</param>
            public void Translate(Vector2 pos, Vector2 sca, float rot, bool forceTranslate = false)
            {
                // don't translate if the cached values are the same as the parameters.
                if (Is(pos, sca, rot) && !forceTranslate)
                {
                    Cache(pos, sca, rot);
                    return;
                }

                Cache(pos, sca, rot);

                if (meshFilter && vertices != null)
                {
                    meshFilter.mesh.vertices = vertices.Select(x => RTMath.Move(RTMath.Rotate(RTMath.Scale(x, sca), rot), pos)).ToArray();
                    meshFilter.mesh.RecalculateBounds();
                }
                if (collider2D && points != null)
                    collider2D.points = points.Select(x => (Vector2)RTMath.Move(RTMath.Rotate(RTMath.Scale(x, sca), rot), pos)).ToArray();
            }

            #region Cache

            void Cache(Vector2 pos, Vector2 sca, float rot)
            {
                this.pos = pos;
                this.sca = sca;
                this.rot = rot;
            }

            /// <summary>
            /// Cached position.
            /// </summary>
            public Vector2 pos;
            /// <summary>
            /// Cached scale.
            /// </summary>
            public Vector2 sca;
            /// <summary>
            /// Cached rotation.
            /// </summary>
            public float rot;

            #endregion

            #region Operators

            public override int GetHashCode() => CoreHelper.CombineHashCodes(pos.x, pos.y, sca.x, sca.y, rot);

            public override bool Equals(object obj) => obj is Cache2D shapeCache && Is(shapeCache.pos, shapeCache.sca, shapeCache.rot);

            /// <summary>
            /// Checks if the cached values are equal to the parameters.
            /// </summary>
            /// <param name="pos">Position.</param>
            /// <param name="sca">Scale.</param>
            /// <param name="rot">Rotation.</param>
            /// <returns>Returns true if the cached values are approximately the same as the passed parameters, otherwise returns false.</returns>
            public bool Is(Vector2 pos, Vector2 sca, float rot) =>
                Mathf.Approximately(pos.x, this.pos.x) && Mathf.Approximately(pos.y, this.pos.y) &&
                Mathf.Approximately(sca.x, this.sca.x) && Mathf.Approximately(sca.y, this.sca.y) &&
                Mathf.Approximately(rot, this.rot);

            #endregion
        }

        /// <summary>
        /// Cache for the 3D variant.
        /// </summary>
        public class Cache3D : Cache
        {
            /// <summary>
            /// Translates the mesh.
            /// </summary>
            /// <param name="pos">Position to translate to.</param>
            /// <param name="sca">Scale to translate to.</param>
            /// <param name="rot">Rotation to tranlsate to.</param>
            public void Translate(Vector3 pos, Vector3 sca, Vector3 rot, bool forceTranslate = false)
            {
                // don't translate if the cached values are the same as the parameters.
                if (Is(pos, sca, rot) && !forceTranslate)
                {
                    Cache(pos, sca, rot);
                    return;
                }

                Cache(pos, sca, rot);

                if (meshFilter && vertices != null)
                {
                    meshFilter.mesh.vertices = vertices.Select(x => RTMath.Move(RTMath.Rotate(RTMath.Scale(x, sca), rot), pos)).ToArray();
                    meshFilter.mesh.RecalculateBounds();
                }
                if (collider2D && points != null)
                    collider2D.points = points.Select(x => (Vector2)RTMath.Move(RTMath.Rotate(RTMath.Scale(x, sca), rot), pos)).ToArray();
            }

            #region Cache

            void Cache(Vector3 pos, Vector3 sca, Vector3 rot)
            {
                this.pos = pos;
                this.sca = sca;
                this.rot = rot;
            }

            /// <summary>
            /// Cached position.
            /// </summary>
            public Vector3 pos;
            /// <summary>
            /// Cached scale.
            /// </summary>
            public Vector3 sca;
            /// <summary>
            /// Cached rotation.
            /// </summary>
            public Vector3 rot;

            #endregion

            #region Operators

            public override int GetHashCode() => CoreHelper.CombineHashCodes(pos.x, pos.y, pos.z, sca.x, sca.y, sca.z, rot.x, rot.y, rot.z);

            public override bool Equals(object obj) => obj is Cache3D shapeCache && Is(shapeCache.pos, shapeCache.sca, shapeCache.rot);

            /// <summary>
            /// Checks if the cached values are equal to the parameters.
            /// </summary>
            /// <param name="pos">Position.</param>
            /// <param name="sca">Scale.</param>
            /// <param name="rot">Rotation.</param>
            /// <returns>Returns true if the cached values are approximately the same as the passed parameters, otherwise returns false.</returns>
            public bool Is(Vector3 pos, Vector3 sca, Vector3 rot) =>
                Mathf.Approximately(pos.x, this.pos.x) && Mathf.Approximately(pos.y, this.pos.y) && Mathf.Approximately(pos.z, this.pos.z) &&
                Mathf.Approximately(sca.x, this.sca.x) && Mathf.Approximately(sca.y, this.sca.y) && Mathf.Approximately(sca.z, this.sca.z) &&
                Mathf.Approximately(rot.x, this.rot.x) && Mathf.Approximately(rot.y, this.rot.y) && Mathf.Approximately(rot.z, this.rot.z);

            #endregion
        }

        #endregion
    }
}
