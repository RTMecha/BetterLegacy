using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;

// Basically, right? I had a dream. Install and Open SFM to understand what i going for.

namespace BetterLegacy.Editor.Managers
{
    public class MotionPathManagerSettings : ManagerSettings
    {
        public MotionPathManagerSettings() { }

        public override Transform Parent => EditorManager.inst.transform.parent;

        public override string ClassName => "[<color=#4CAF50>MotionPathManager</color>] \n";
    }

    /// <summary>
    /// Manages rendering for motion paths.
    /// </summary>
    public class MotionPathManager : BaseManager<MotionPathManager, MotionPathManagerSettings>
    {
        #region Values

        /// <summary>
        /// Dictionary of active motion paths.
        /// </summary>
        public Dictionary<IEditable, MotionPath> activePaths = new Dictionary<IEditable, MotionPath>();

        Transform container;
        Material exteriorMaterial;
        Material interiorMaterial;
        Vector3[] baseVertices;
        int[] baseTriangles;
        Vector2[] baseUVs;
        int builtShape = -1;
        float pulseClock;
        readonly List<Vector3> extVerts = new List<Vector3>();
        readonly List<Vector2> extUVs = new List<Vector2>();
        readonly List<int> extTris = new List<int>();
        readonly List<Vector3> intVerts = new List<Vector3>();
        readonly List<Vector2> intUVs = new List<Vector2>();
        readonly List<int> intTris = new List<int>();
        readonly List<Vector3> samplePos = new List<Vector3>();
        readonly List<float> sampleTime = new List<float>();
        Vector3[] chainLocalPos = new Vector3[8];
        float[] chainLocalRot = new float[8];
        Vector3[] chainLocalScale = new Vector3[8];
        Sequence<Vector3> camPosSequence;
        Sequence<Vector3> camZoomSequence;
        Sequence<Vector3> camRotSequence;
        readonly List<IEditable> toRemove = new List<IEditable>();
        const int MAX_VISIBLE = 2000;
        static int ObjectLayer => RTLevel.UI_LAYER;

        #endregion

        #region Functions

        public override void OnInit()
        {
            container = Creator.NewGameObject("Motion Paths", transform).transform;
            RebuildShape();
        }

        void RebuildShape()
        {
            builtShape = (int)EditorConfig.Instance.MotionPathNodeShape.Value;
            if (!ObjectManager.inst || ObjectManager.inst.objectPrefabs == null || ObjectManager.inst.objectPrefabs.Count == 0)
                return;
            var protoVisual = ObjectManager.inst.objectPrefabs[Mathf.Clamp(builtShape, 0, ObjectManager.inst.objectPrefabs.Count - 1)].options[0].transform.GetChild(0);
            var protoMesh = protoVisual.GetComponent<MeshFilter>().sharedMesh;
            baseVertices = protoMesh.vertices;
            baseTriangles = protoMesh.triangles;
            baseUVs = protoMesh.uv;
            if (baseUVs == null || baseUVs.Length != baseVertices.Length)
                baseUVs = new Vector2[baseVertices.Length];
            if (!exteriorMaterial || !interiorMaterial)
            {
                var protoMaterial = protoVisual.GetComponent<Renderer>().sharedMaterial;
                exteriorMaterial = new Material(protoMaterial);
                interiorMaterial = new Material(protoMaterial);
            }
        }

        /// <summary>
        /// Checks if an editable object has an active motion path.
        /// </summary>
        /// <param name="obj">Editable object.</param>
        /// <returns>Returns <see langword="true"/> if the motion path is active, otherwise returns <see langword="false"/>.</returns>
        public bool IsActive(IEditable obj) => obj != null && activePaths.ContainsKey(obj);

        /// <summary>
        /// Toggles the editable objects' motion path.
        /// </summary>
        /// <param name="obj">Editable object.</param>
        public void Toggle(IEditable obj) => SetActive(obj, !IsActive(obj));

        /// <summary>
        /// Sets the active state of the editable objects' motion path.
        /// </summary>
        /// <param name="obj">Editable object.</param>
        /// <param name="active">Active state to set.</param>
        public void SetActive(IEditable obj, bool active)
        {
            if (obj == null)
                return;
            if (active)
            {
                if (!activePaths.ContainsKey(obj))
                    activePaths[obj] = new MotionPath(obj);
                return;
            }
            if (activePaths.TryGetValue(obj, out var path))
            {
                path.Destroy();
                activePaths.Remove(obj);
            }
        }

        public override void OnTick()
        {
            if (activePaths.Count == 0 || !GameData.Current)
                return;
            var config = EditorConfig.Instance;
            if (baseVertices == null || (int)config.MotionPathNodeShape.Value != builtShape)
                RebuildShape();
            if (baseVertices == null)
                return;
            exteriorMaterial.color = config.MotionPathNodeColorExterior.Value;
            interiorMaterial.color = config.MotionPathNodeColorInterior.Value;
            var fgCamera = RTLevel.Cameras.FG;
            float frontZ = (fgCamera ? fgCamera.transform.position.z : -10f) + 0.5f;
            var audioSource = AudioManager.inst ? AudioManager.inst.CurrentAudioSource : null;
            float now = audioSource ? audioSource.time : 0f;
            if (audioSource && audioSource.isPlaying)
                pulseClock = now;
            else
                pulseClock += Time.unscaledDeltaTime;
            float maxSeek = config.MotionPathMaxSeekTime.Value;
            float baseSize = config.MotionPathNodeBaseSize.Value;
            float largeMult = config.MotionPathNodeLargeSizeMultiplier.Value;
            float shrink = config.MotionPathNodeShrinkTime.Value;
            float interval = config.MotionPathPlaybackInterval.Value;
            float interiorMult = config.MotionPathNodeInteriorMultiplier.Value;
            float nodePerFrame = config.MotionPathNodePerFrame.Value;
            var easeFunc = Ease.GetEaseFunction(config.MotionPathNodeAnimationEasing.Value);
            BuildCameraSequences();
            toRemove.Clear();
            foreach (var kvp in activePaths)
            {
                var path = kvp.Value;
                if (!IsObjectValid(path.reference))
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }
                RenderPath(path, now, pulseClock, maxSeek, baseSize, largeMult, shrink, interval, interiorMult, nodePerFrame, easeFunc, frontZ);
            }
            for (int i = 0; i < toRemove.Count; i++)
                SetActive(toRemove[i], false);
        }

        void RenderPath(MotionPath path, float now, float pulseClock, float maxSeek, float baseSize, float largeMult, float shrink, float interval, float interiorMult, float nodePerFrame, EaseFunction easeFunc, float frontZ)
        {
            var lifetime = (ILifetime)path.reference;
            float startTime = lifetime.StartTime;
            float killTime = startTime + lifetime.SpawnDuration;
            float winStart = Mathf.Max(startTime, now - maxSeek);
            float winEnd = Mathf.Min(killTime, now + maxSeek);
            if (winEnd <= winStart)
            {
                path.SetRenderersEnabled(false);
                return;
            }
            float dt = 1f / Mathf.Max(nodePerFrame * 60f, 0.001f);
            int total = Mathf.CeilToInt((winEnd - winStart) / dt) + 1;
            int step = Mathf.Max(1, Mathf.CeilToInt(total / (float)MAX_VISIBLE));
            var beatmapChain = path.reference is BeatmapObject bo ? bo.GetParentChain() : null;
            samplePos.Clear();
            sampleTime.Clear();
            for (int i = 0; i < total; i += step)
            {
                float t = Mathf.Min(winStart + i * dt, winEnd);
                sampleTime.Add(t);
                samplePos.Add(beatmapChain != null ? SampleWorldTransform(beatmapChain, t).position : SampleCenter(path.reference, t));
            }
            int count = samplePos.Count;
            if (count == 0)
            {
                path.SetRenderersEnabled(false);
                return;
            }
            float interiorZ = frontZ - 0.02f;
            extVerts.Clear(); extUVs.Clear(); extTris.Clear();
            intVerts.Clear(); intUVs.Clear(); intTris.Clear();
            for (int i = 0; i < count; i++)
            {
                var position = samplePos[i];
                Vector3 dir =
                    i < count - 1 ? samplePos[i + 1] - position :
                    i > 0 ? position - samplePos[i - 1] :
                    Vector3.up;
                float angle = dir.sqrMagnitude > 0.0000001f ? Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f : 0f;
                var rotation = Quaternion.Euler(0f, 0f, angle);
                float phase = Mathf.Repeat(pulseClock - sampleTime[i], interval);
                float size = baseSize;
                if (phase < shrink)
                {
                    float k01 = easeFunc(Mathf.Clamp01(phase / shrink));
                    size = Mathf.Lerp(baseSize * largeMult, baseSize, k01);
                }
                AppendNode(extVerts, extUVs, extTris, rotation, new Vector3(position.x, position.y, frontZ), size);
                AppendNode(intVerts, intUVs, intTris, rotation, new Vector3(position.x, position.y, interiorZ), size * interiorMult);
            }
            path.EnsureRenderers(container, exteriorMaterial, interiorMaterial, ObjectLayer);
            path.ApplyMeshes(extVerts, extUVs, extTris, intVerts, intUVs, intTris);
        }
        void AppendNode(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Quaternion rotation, Vector3 position, float size)
        {
            int offset = verts.Count;
            for (int i = 0; i < baseVertices.Length; i++)
            {
                verts.Add(position + rotation * (baseVertices[i] * size));
                uvs.Add(baseUVs[i]);
            }
            for (int i = 0; i < baseTriangles.Length; i++)
                tris.Add(offset + baseTriangles[i]);
        }
        Vector3 SampleCenter(IEditable obj, float time)
        {
            switch (obj)
            {
                case BeatmapObject beatmapObject:
                    return SampleWorldTransform(beatmapObject, time).position;
                case PrefabObject prefabObject:
                    return ComposeParentable(prefabObject, new Vector3(prefabObject.events[0].values[0], prefabObject.events[0].values[1], 0f), time);
                case BackgroundObject backgroundObject:
                    return backgroundObject.pos;
                default:
                    return Vector3.zero;
            }
        }
        WorldTransform SampleWorldTransform(BeatmapObject self, float time) => SampleWorldTransform(self.GetParentChain(), time);
        WorldTransform SampleWorldTransform(List<BeatmapObject> chain, float time)
        {
            int n = chain.Count;
            if (chainLocalPos.Length < n)
            {
                chainLocalPos = new Vector3[n];
                chainLocalRot = new float[n];
                chainLocalScale = new Vector3[n];
            }
            var localPos = chainLocalPos;
            var localRot = chainLocalRot;
            var localScale = chainLocalScale;
            bool animatePos = true, animateSca = true, animateRot = true;
            float posDelay = 0f, scaDelay = 0f, rotDelay = 0f;
            float posAdded = 0f, scaAdded = 0f, rotAdded = 0f;
            float posParallax = 1f, scaParallax = 1f, rotParallax = 1f;
            for (int i = 0; i < n; i++)
            {
                var o = chain[i];
                float timeOffset = o.StartTime;
                if (o.GetParentAdditive(0)) posAdded += o.GetParentOffset(0);
                if (o.GetParentAdditive(1)) scaAdded += o.GetParentOffset(1);
                if (o.GetParentAdditive(2)) rotAdded += o.GetParentOffset(2);
                var seq = o.cachedSequences;
                localPos[i] = animatePos
                    ? ScaleXY(seq != null && seq.PositionSequence != null
                        ? EvalSequence(seq.PositionSequence, time - timeOffset - (posDelay + posAdded))
                        : new Vector3(o.Interpolate(0, 0, time - timeOffset - (posDelay + posAdded)), o.Interpolate(0, 1, time - timeOffset - (posDelay + posAdded)), 0f), posParallax)
                    : Vector3.zero;
                localScale[i] = animateSca
                    ? ScaleXY1(seq != null && seq.ScaleSequence != null
                        ? EvalSequence(seq.ScaleSequence, time - timeOffset - (scaDelay + scaAdded))
                        : new Vector3(o.Interpolate(1, 0, time - timeOffset - (scaDelay + scaAdded)), o.Interpolate(1, 1, time - timeOffset - (scaDelay + scaAdded)), 1f), scaParallax)
                    : Vector3.one;
                localRot[i] = animateRot
                    ? (seq != null && seq.RotationSequence != null
                        ? EvalSequence(seq.RotationSequence, time - timeOffset - (rotDelay + rotAdded)).z
                        : o.Interpolate(2, 0, time - timeOffset - (rotDelay + rotAdded))) * rotParallax
                    : 0f;
                posDelay = o.GetParentOffset(0); scaDelay = o.GetParentOffset(1); rotDelay = o.GetParentOffset(2);
                posParallax = o.parallaxSettings[0]; scaParallax = o.parallaxSettings[1]; rotParallax = o.parallaxSettings[2];
                animatePos = o.GetParentType(0); animateSca = o.GetParentType(1); animateRot = o.GetParentType(2);
            }
            var pos = localPos[n - 1];
            float rot = localRot[n - 1];
            var scale = localScale[n - 1];
            for (int i = n - 2; i >= 0; i--)
            {
                pos = RTMath.Move(pos, RTMath.Rotate(RTMath.Scale(localPos[i], scale), rot));
                rot += localRot[i];
                scale = RTMath.Scale(scale, localScale[i]);
            }

            // The topmost object may be parented to the camera, whose motion comes from event keyframes.
            // Compose it as the outermost parent (scale -> rotate -> move), mirroring UpdateCameraParent.
            var top = chain[n - 1];
            if (top.Parent == BeatmapObject.CAMERA_PARENT)
                ApplyCameraParent(top, time, ref pos, ref rot, ref scale);

            return new WorldTransform { position = pos, rotation = rot, scale = scale };
        }
        void ApplyCameraParent(BeatmapObject top, float time, ref Vector3 pos, ref float rot, ref Vector3 scale)
        {
            var camPos = camPosSequence != null ? EvalSequence(camPosSequence, time) : Vector3.zero;
            float zoom = camZoomSequence != null ? EvalSequence(camZoomSequence, time).x : 20f;
            if (zoom == 0f || float.IsNaN(zoom))
                zoom = 20f;
            float camRot = camRotSequence != null ? EvalSequence(camRotSequence, time).x : 0f;
            float camOrthoZoom = zoom / 20f - 1f;

            if (top.GetParentType(1))
            {
                float off = top.parallaxSettings[1];
                float zoomFactor = camOrthoZoom * off + 1f;
                pos = new Vector3(pos.x * zoomFactor, pos.y * zoomFactor, pos.z);
                scale = new Vector3(scale.x * zoomFactor, scale.y * zoomFactor, scale.z * (off + 1f));
            }
            if (top.GetParentType(2))
            {
                float cr = camRot * top.parallaxSettings[2];
                pos = RTMath.Rotate(pos, cr);
                rot += cr;
            }
            if (top.GetParentType(0))
            {
                float off = top.parallaxSettings[0];
                pos = RTMath.Move(pos, new Vector3(camPos.x * off, camPos.y * off, 0f));
            }
        }
        void BuildCameraSequences()
        {
            var events = GameData.Current ? GameData.Current.events : null;
            camPosSequence = BuildEventSequence(events != null && events.Count > 0 ? events[0] : null);
            camZoomSequence = BuildEventSequence(events != null && events.Count > 1 ? events[1] : null);
            camRotSequence = BuildEventSequence(events != null && events.Count > 2 ? events[2] : null);
        }
        static Sequence<Vector3> BuildEventSequence(List<EventKeyframe> eventKeyframes)
        {
            var keyframes = new List<IKeyframe<Vector3>>();
            if (eventKeyframes != null)
            {
                var currentValue = Vector3.zero;
                foreach (var eventKeyframe in eventKeyframes)
                {
                    var value = new Vector3(
                        eventKeyframe.values.Length > 0 ? eventKeyframe.values[0] : 0f,
                        eventKeyframe.values.Length > 1 ? eventKeyframe.values[1] : 0f,
                        eventKeyframe.values.Length > 2 ? eventKeyframe.values[2] : 0f);
                    currentValue = eventKeyframe.relative ? currentValue + value : value;
                    keyframes.Add(new Vector3Keyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve), eventKeyframe.relative));
                }
            }
            if (keyframes.Count == 0)
                keyframes.Add(new Vector3Keyframe(0f, Vector3.zero, Ease.GetEaseFunction(Easing.Linear), false));
            return new Sequence<Vector3>(keyframes);
        }
        static Vector3 ScaleXY(Vector3 v, float parallax) => new Vector3(v.x * parallax, v.y * parallax, 0f);
        static Vector3 ScaleXY1(Vector3 v, float parallax) => new Vector3(v.x * parallax, v.y * parallax, 1f);
        static Vector3 EvalSequence(Sequence<Vector3> sequence, float time)
        {
            var keyframes = sequence.keyframes;
            int n = keyframes.Length;
            if (n == 0)
                return Vector3.zero;
            var first = keyframes[0];
            if (n == 1 || time < first.Time)
                return first.Interpolate(first, 1f);
            var last = keyframes[n - 1];
            if (time >= last.Time)
                return last.Interpolate(last, 0f);
            int index = SearchKeyframe(keyframes, time);
            var current = keyframes[index];
            var next = keyframes[Mathf.Min(index + 1, n - 1)];
            float t = current.Time == next.Time ? 1f : Mathf.InverseLerp(current.Time, next.Time, time);
            return current.Interpolate(next, t);
        }
        static int SearchKeyframe(IKeyframe<Vector3>[] keyframes, float time)
        {
            int low = 0;
            int high = keyframes.Length - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                float midTime = keyframes[mid].Time;
                if (time < midTime)
                    high = mid - 1;
                else if (time > midTime)
                    low = mid + 1;
                else
                    return mid;
            }
            return Mathf.Max(0, low - 1);
        }
        Vector3 ComposeParentable(IParentable parentable, Vector3 local, float time)
        {
            if (!GameData.Current)
                return local;
            var parent = parentable.CachedParent ? parentable.CachedParent : GameData.Current.beatmapObjects.Find(x => x.id == parentable.Parent);
            if (!parent)
                return local;
            string parentType = parentable.ParentType;
            float posDelay = parentable.ParentOffsets != null && parentable.ParentOffsets.Length > 0 ? parentable.ParentOffsets[0] : 0f;
            var pt = SampleWorldTransform(parent, time - posDelay);
            bool inheritPosition = parentType == null || parentType.Length < 1 || parentType[0] == '1';
            bool inheritScale = parentType == null || parentType.Length < 2 || parentType[1] == '1';
            bool inheritRotation = parentType == null || parentType.Length < 3 || parentType[2] == '1';
            var pos = local;
            if (inheritScale)
                pos = RTMath.Scale(pos, pt.scale);
            if (inheritRotation)
                pos = RTMath.Rotate(pos, pt.rotation);
            if (inheritPosition)
                pos = RTMath.Move(pos, pt.position);
            return pos;
        }
        static bool IsObjectValid(IEditable obj)
        {
            if (!GameData.Current)
                return false;
            return obj switch
            {
                BeatmapObject beatmapObject => GameData.Current.beatmapObjects.Contains(beatmapObject),
                PrefabObject prefabObject => GameData.Current.prefabObjects.Contains(prefabObject),
                BackgroundObject backgroundObject => GameData.Current.backgroundObjects.Contains(backgroundObject),
                _ => false,
            };
        }

        #endregion

        #region Sub Classes

        struct WorldTransform
        {
            public Vector3 position;
            public float rotation;
            public Vector3 scale;
        }

        /// <summary>
        /// Represents an editable objects' motion path.
        /// </summary>
        public class MotionPath
        {
            public MotionPath(IEditable reference) => this.reference = reference;

            #region Values

            /// <summary>
            /// Editable object reference.
            /// </summary>
            public readonly IEditable reference;
            MeshFilter exteriorFilter;
            MeshFilter interiorFilter;
            MeshRenderer exteriorRenderer;
            MeshRenderer interiorRenderer;
            Mesh exteriorMesh;
            Mesh interiorMesh;

            #endregion

            #region Functions

            /// <summary>
            /// Ensures the renderer exists.
            /// </summary>
            /// <param name="parent">Parent to use.</param>
            /// <param name="exteriorMaterial">Material to use for the exterior.</param>
            /// <param name="interiorMaterial">Material to use for the interior.</param>
            /// <param name="layer">Layer to set.</param>
            public void EnsureRenderers(Transform parent, Material exteriorMaterial, Material interiorMaterial, int layer)
            {
                if (exteriorFilter)
                {
                    SetRenderersEnabled(true);
                    return;
                }
                exteriorMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                interiorMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                var exterior = Creator.NewGameObject("Motion Path (Exterior)", parent);
                exterior.layer = layer;
                exteriorFilter = exterior.AddComponent<MeshFilter>();
                exteriorFilter.mesh = exteriorMesh;
                exteriorRenderer = exterior.AddComponent<MeshRenderer>();
                exteriorRenderer.sharedMaterial = exteriorMaterial;
                exteriorRenderer.enabled = true;
                var interior = Creator.NewGameObject("Motion Path (Interior)", parent);
                interior.layer = layer;
                interiorFilter = interior.AddComponent<MeshFilter>();
                interiorFilter.mesh = interiorMesh;
                interiorRenderer = interior.AddComponent<MeshRenderer>();
                interiorRenderer.sharedMaterial = interiorMaterial;
                interiorRenderer.enabled = true;
            }

            /// <summary>
            /// Applies meshes to interior and exterior meshes.
            /// </summary>
            public void ApplyMeshes(List<Vector3> extVerts, List<Vector2> extUVs, List<int> extTris, List<Vector3> intVerts, List<Vector2> intUVs, List<int> intTris)
            {
                SetRenderersEnabled(true);
                UploadMesh(exteriorMesh, extVerts, extUVs, extTris);
                UploadMesh(interiorMesh, intVerts, intUVs, intTris);
            }
            
            static void UploadMesh(Mesh mesh, List<Vector3> verts, List<Vector2> uvs, List<int> tris)
            {
                mesh.Clear();
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0, true);
                mesh.RecalculateBounds();
            }

            /// <summary>
            /// Sets the renderers active state.
            /// </summary>
            /// <param name="enabled">Active state to set.</param>
            public void SetRenderersEnabled(bool enabled)
            {
                if (exteriorRenderer)
                    exteriorRenderer.enabled = enabled;
                if (interiorRenderer)
                    interiorRenderer.enabled = enabled;
            }

            /// <summary>
            /// Destroys the motion path.
            /// </summary>
            public void Destroy()
            {
                if (exteriorFilter)
                    Object.Destroy(exteriorFilter.gameObject);
                if (interiorFilter)
                    Object.Destroy(interiorFilter.gameObject);
                if (exteriorMesh)
                    Object.Destroy(exteriorMesh);
                if (interiorMesh)
                    Object.Destroy(interiorMesh);
                exteriorFilter = null;
                interiorFilter = null;
                exteriorRenderer = null;
                interiorRenderer = null;
                exteriorMesh = null;
                interiorMesh = null;
            }

            #endregion
        }

        #endregion
    }
}
