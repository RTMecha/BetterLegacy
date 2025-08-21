using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTBackgroundObject : Exists, IRTObject, ICustomActivatable
    {
        public RTBackgroundObject(BackgroundObject backgroundObject, List<Renderer> renderers, RTLevelBase parentRuntime)
        {
            this.backgroundObject = backgroundObject;

            ParentRuntime = parentRuntime;
            StartTime = backgroundObject.StartTime;
            KillTime = backgroundObject.StartTime + backgroundObject.SpawnDuration;

            this.renderers = renderers;

            var baseObject = BaseObject;
            if (baseObject)
                top = baseObject.transform.parent;
        }

        public RTLevelBase ParentRuntime { get; set; }

        public float StartTime { get; set; }
        public float KillTime { get; set; }
        public bool Active { get; set; }

        public bool CustomActive { get; set; } = true;

        public BackgroundObject backgroundObject;

        public bool hidden;

        public Transform top;

        public List<Renderer> renderers = new List<Renderer>();

        public GameObject BaseObject => !renderers.IsEmpty() && renderers[0] ? renderers[0].gameObject : null;

        public void UpdateShape(int shape, int shapeOption)
        {
            var paShape = ShapeManager.inst.GetShape3D(shape, shapeOption);
            foreach (var renderer in renderers)
            {
                if (renderer && renderer.gameObject.TryGetComponent(out MeshFilter meshFilter) && paShape.mesh)
                    meshFilter.mesh = paShape.mesh;
            }
        }

        public void SetColor(Color mainColor, Color fadeColor)
        {
            int layer = backgroundObject.iterations - backgroundObject.depth;
            renderers.ForLoop((renderer, i) =>
            {
                if (i == 0)
                {
                    renderer.material.color = mainColor;
                    return;
                }

                if (!renderer.gameObject.activeSelf)
                    renderer.gameObject.SetActive(true);

                float t = 1f / layer * i;

                renderer.material.color = Color.Lerp(Color.Lerp(mainColor, fadeColor, t), fadeColor, t);
            });
        }

        public void Clear()
        {
            if (top)
                CoreHelper.Delete(top.gameObject);

            renderers.Clear();
        }

        public void SetActive(bool active)
        {
            Active = active;

            var gameObject = BaseObject;
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

            var gameObject = BaseObject;

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

            if (CoreConfig.Instance.LDM.Value && renderers.Count > 0)
            {
                renderers[0].material.color = mainColor;
                if (renderers.Count > 1 && renderers[1].gameObject.activeSelf)
                {
                    for (int i = 1; i < renderers.Count; i++)
                        renderers[i].gameObject.SetActive(false);
                }
            }
            else
                SetColor(mainColor, fadeColor);

            if (!reactive)
            {
                backgroundObject.reactiveSize = Vector2.zero;

                gameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + backgroundObject.depth * 10f + backgroundObject.zposition) + backgroundObject.positionOffset;
                gameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) + backgroundObject.scaleOffset;
                gameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset);
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
                    32f + backgroundObject.depth * 10f + backgroundObject.zposition) + backgroundObject.positionOffset;
                gameObject.transform.localScale =
                    new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) +
                    new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y, 0f) + backgroundObject.scaleOffset;
                gameObject.transform.localRotation =
                    Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot) + backgroundObject.rotationOffset);

                return;
            }

            float x = RTLevel.Current.GetSample(backgroundObject.reactivePosSamples.x, backgroundObject.reactivePosIntensity.x);
            float y = RTLevel.Current.GetSample(backgroundObject.reactivePosSamples.y, backgroundObject.reactivePosIntensity.y);
            float z = RTLevel.Current.GetSample(backgroundObject.reactiveZSample, backgroundObject.reactiveZIntensity);

            float rot = RTLevel.Current.GetSample(backgroundObject.reactiveRotSample, backgroundObject.reactiveRotIntensity);

            gameObject.transform.localPosition =
                new Vector3(backgroundObject.pos.x + x,
                backgroundObject.pos.y + y,
                32f + backgroundObject.depth * 10f + z + backgroundObject.zposition) + backgroundObject.positionOffset;
            gameObject.transform.localScale =
                new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale) +
                new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y, 0f) + backgroundObject.scaleOffset;
            gameObject.transform.localRotation = Quaternion.Euler(
                new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y,
                backgroundObject.rot + rot) + backgroundObject.rotationOffset);
        }
    }
}
