using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime.Objects.Visual;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTBackgroundObject : Exists, IRTObject, ICustomActivatable
    {
        public RTBackgroundObject(BackgroundObject backgroundObject, List<VisualFadeObject> visualFadeObjects, RTLevelBase parentRuntime)
        {
            this.backgroundObject = backgroundObject;

            ParentRuntime = parentRuntime;
            StartTime = backgroundObject.StartTime;
            KillTime = backgroundObject.StartTime + backgroundObject.SpawnDuration;

            this.visualFadeObjects = visualFadeObjects;
            showFade = CoreConfig.Instance.ShowBackgroundFade.Value;

            var baseObject = !visualFadeObjects.IsEmpty() && visualFadeObjects[0] ? visualFadeObjects[0].gameObject : null;
            if (!baseObject)
                return;

            gameObject = baseObject.transform.parent.gameObject;
            top = gameObject.transform.parent;
        }

        public RTLevelBase ParentRuntime { get; set; }

        public float StartTime { get; set; }
        public float KillTime { get; set; }
        public bool Active { get; set; }

        public bool CustomActive { get; set; } = true;

        public BackgroundObject backgroundObject;

        public bool hidden;

        public GameObject gameObject;

        public Transform top;

        public List<VisualFadeObject> visualFadeObjects = new List<VisualFadeObject>();

        public GameObject BaseObject => !visualFadeObjects.IsEmpty() && visualFadeObjects[0] ? visualFadeObjects[0].gameObject : null;

        public int depthOffset;
        public int Depth => backgroundObject.depth + Mathf.Clamp(depthOffset, 0, backgroundObject.iterations - 1);

        Color mainColor;
        Color fadeColor;
        int fadeCount = -1;
        bool showFade = true;

        public void ReactiveDepth(int sample, float intensity, int offset, bool inverse)
        {
            var reactive = (int)RTLevel.Current.GetSample(sample, intensity) + offset;
            depthOffset = Mathf.Clamp(inverse ? -(reactive - (backgroundObject.iterations - 1)) : reactive, 0, int.MaxValue);
            visualFadeObjects.ForLoop((visualFadeObject, i) => visualFadeObject.SetActive(i == 0 || i < backgroundObject.iterations - Depth));
        }

        public void SetDepthOffset(int depthOffset)
        {
            this.depthOffset = Mathf.Clamp(depthOffset, 0, int.MaxValue);
            visualFadeObjects.ForLoop((visualFadeObject, i) => visualFadeObject.SetActive(i == 0 || i < backgroundObject.iterations - Depth));
        }

        public void UpdateShape(int shape, int shapeOption, bool flat = false)
        {
            var shapeType = (ShapeType)shape;
            if (shapeType == ShapeType.Polygon)
            {
                var polygon = backgroundObject.Polygon;
                if (!polygon)
                    return;

                foreach (var visualFadeObject in visualFadeObjects)
                {
                    if (visualFadeObject && visualFadeObject.meshFilter)
                        VGShapes.RoundedRingMesh(visualFadeObject.meshFilter, null,
                            polygon.Radius,
                            polygon.Sides,
                            polygon.Roundness,
                            polygon.Thickness,
                            polygon.ThicknessOffset,
                            polygon.ThicknessScale,
                            polygon.Angle,
                            polygon.ThicknessRotation);
                }
                return;
            }

            var paShape = flat ? ShapeManager.inst.GetShape(shape, shapeOption) : ShapeManager.inst.GetShape3D(shape, shapeOption);
            foreach (var visualFadeObject in visualFadeObjects)
            {
                if (visualFadeObject && visualFadeObject.meshFilter && paShape.mesh)
                    visualFadeObject.meshFilter.mesh = paShape.mesh;
            }
        }

        public void SetColor(Color mainColor, Color fadeColor)
        {
            int fadeCount = backgroundObject.iterations - Depth;

            if (this.mainColor == mainColor && this.fadeColor == fadeColor && this.fadeCount == fadeCount)
                return;

            this.mainColor = mainColor;
            this.fadeColor = fadeColor;
            this.fadeCount = fadeCount;
            visualFadeObjects.ForLoop((visualFadeObject, i) =>
            {
                if (!visualFadeObject.Active)
                    return;

                if (i == 0)
                {
                    visualFadeObject.SetColor(mainColor);
                    return;
                }

                float t = 1f / fadeCount * i;
                visualFadeObject.SetColor(Color.Lerp(Color.Lerp(mainColor, fadeColor, t), fadeColor, t));
            });
        }

        public void Clear()
        {
            if (top)
                CoreHelper.Delete(top.gameObject);

            visualFadeObjects.Clear();
        }

        public void SetActive(bool active)
        {
            Active = active;

            if (gameObject)
                gameObject.SetActive(active);
        }

        public void SetCustomActive(bool active)
        {
            CustomActive = active;
            if (backgroundObject)
                backgroundObject.Enabled = active;
        }

        public void Interpolate(float time)
        {
            var enabled = backgroundObject.Enabled && RTLevel.Current.eventEngine.bgActive;
            if (backgroundObject.active && top && top.gameObject.activeSelf != enabled)
                top.gameObject.SetActive(enabled);

            if (hidden)
                top.gameObject.SetActive(false);

            if (!backgroundObject.active || !enabled || hidden || !gameObject)
                return;

            var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

            Color mainColor =
                RTColors.ChangeColorHSV(beatmapTheme.GetBGColor(backgroundObject.color), backgroundObject.hue, backgroundObject.saturation, backgroundObject.value);

            var reactive = backgroundObject.IsReactive;

            if (reactive)
                mainColor =
                    RTMath.Lerp(mainColor,
                        RTColors.ChangeColorHSV(
                            beatmapTheme.GetBGColor(backgroundObject.reactiveCol),
                            backgroundObject.hue,
                            backgroundObject.saturation,
                            backgroundObject.value),
                        RTLevel.Current.GetSample(backgroundObject.reactiveColSample, backgroundObject.reactiveColIntensity));

            mainColor.a = 1f;

            var fadeColor =
                RTColors.ChangeColorHSV(beatmapTheme.GetBGColor(backgroundObject.fadeColor), backgroundObject.fadeHue, backgroundObject.fadeSaturation, backgroundObject.fadeValue);

            if (RTColors.ColorMatch(fadeColor, beatmapTheme.backgroundColor, 0.05f))
                fadeColor = ThemeManager.inst.bgColorToLerp;
            fadeColor.a = 1f;

            if (showFade != CoreConfig.Instance.ShowBackgroundFade.Value)
            {
                showFade = CoreConfig.Instance.ShowBackgroundFade.Value;
                for (int i = 1; i < visualFadeObjects.Count; i++)
                    visualFadeObjects[i].SetActive(showFade);
            }

            if (showFade)
                SetColor(mainColor, fadeColor);
            else if (!visualFadeObjects.IsEmpty())
                visualFadeObjects[0].SetColor(mainColor);

            if (!reactive)
            {
                backgroundObject.reactiveSize = Vector2.zero;

                gameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + Depth * 10f + backgroundObject.zposition) + backgroundObject.positionOffset + backgroundObject.fullTransform.position;
                gameObject.transform.localScale = RTMath.Scale(new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) + backgroundObject.scaleOffset, backgroundObject.fullTransform.scale);
                gameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset + backgroundObject.fullTransform.rotation);
                return;
            }

            backgroundObject.reactiveSize = backgroundObject.reactiveType switch
            {
                BackgroundObject.ReactiveType.Bass => new Vector2(RTLevel.Current.sampleLow, RTLevel.Current.sampleLow) * backgroundObject.reactiveScale,
                BackgroundObject.ReactiveType.Mids => new Vector2(RTLevel.Current.sampleMid, RTLevel.Current.sampleMid) * backgroundObject.reactiveScale,
                BackgroundObject.ReactiveType.Treble => new Vector2(RTLevel.Current.sampleHigh, RTLevel.Current.sampleHigh) * backgroundObject.reactiveScale,
                BackgroundObject.ReactiveType.Custom => new Vector2(RTLevel.Current.GetSample(backgroundObject.reactiveScaSamples.x, backgroundObject.reactiveScaIntensity.x), RTLevel.Current.GetSample(backgroundObject.reactiveScaSamples.y, backgroundObject.reactiveScaIntensity.y)) * backgroundObject.reactiveScale,
                _ => Vector2.zero,
            };

            if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
            {
                gameObject.transform.localPosition =
                    new Vector3(backgroundObject.pos.x,
                    backgroundObject.pos.y,
                    32f + Depth * 10f + backgroundObject.zposition) + backgroundObject.positionOffset + backgroundObject.fullTransform.position;
                gameObject.transform.localScale =
                    RTMath.Scale(new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) +
                    new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y, 0f) + backgroundObject.scaleOffset, backgroundObject.fullTransform.scale);
                gameObject.transform.localRotation =
                    Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset + backgroundObject.fullTransform.rotation);

                return;
            }

            float x = RTLevel.Current.GetSample(backgroundObject.reactivePosSamples.x, backgroundObject.reactivePosIntensity.x);
            float y = RTLevel.Current.GetSample(backgroundObject.reactivePosSamples.y, backgroundObject.reactivePosIntensity.y);
            float z = RTLevel.Current.GetSample(backgroundObject.reactiveZSample, backgroundObject.reactiveZIntensity);

            float rot = RTLevel.Current.GetSample(backgroundObject.reactiveRotSample, backgroundObject.reactiveRotIntensity);

            gameObject.transform.localPosition =
                new Vector3(backgroundObject.pos.x + x,
                backgroundObject.pos.y + y,
                32f + Depth * 10f + z + backgroundObject.zposition) + backgroundObject.positionOffset + backgroundObject.fullTransform.position;
            gameObject.transform.localScale =
                RTMath.Scale(new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) +
                new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y, 0f) + backgroundObject.scaleOffset, backgroundObject.fullTransform.scale);
            gameObject.transform.localRotation = Quaternion.Euler(
                new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y,
                backgroundObject.rot + rot) + backgroundObject.rotationOffset + backgroundObject.fullTransform.rotation);
        }

        public override string ToString() => backgroundObject?.ToString() ?? string.Empty;
    }
}
