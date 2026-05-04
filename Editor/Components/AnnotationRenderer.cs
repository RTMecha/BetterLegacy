using System;

using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Components
{
    public class AnnotationRenderer : MonoBehaviour
    {
        Camera current;

        readonly int mode = GL.QUADS;
        readonly float thicknessMultiply = 0.01f;
        readonly float distanceMultiply = 1f;
        readonly float angleOffset = 90f;

        static Material lineMaterial;
        static void CreateLineMaterial()
        {
            if (lineMaterial)
                return;

            var shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
        }

        void OnRenderObject()
        {
            current = Camera.current;
            if (current != RTLevel.Cameras.UI)
                return;

            CreateLineMaterial();
            // Apply the line material
            lineMaterial.SetPass(0);

            GL.PushMatrix();
            // Set transformation matrix for drawing to
            // match our transform
            GL.MultMatrix(transform.localToWorldMatrix);

            // Draw lines
            GL.Begin(mode);
            if (RTMarkerEditor.inst.currentStroke)
                RenderAnnotationStroke(null, RTMarkerEditor.inst.currentStroke);

            var time = AudioManager.inst.CurrentAudioSource.time;
            for (int i = 0; i < GameData.Current.data.markers.Count; i++)
            {
                var marker = GameData.Current.data.markers[i];
                RenderAnnotationStrokes(marker, time);
            }

            GL.End();
            GL.PopMatrix();
        }

        void RenderAnnotationStrokes(Marker marker, float time)
        {
            if (!marker || !RTMarkerEditor.inst.IsInMarkerArea(marker, time))
                return;

            for (int i = 0; i < marker.annotations.Count; i++)
                RenderAnnotationStroke(marker, marker.annotations[i]);
        }

        void RenderAnnotationStroke(Marker marker, Annotation annotation)
        {
            if (!annotation)
                return;

            var color = !string.IsNullOrEmpty(annotation.hexColor) ? RTColors.HexToColor(annotation.hexColor) : RTMarkerEditor.inst.GetColor(annotation.color);
            color.a *= annotation.opacity;
            color.a *= EditorConfig.Instance.AnnotationOpacity.Value;
            if (marker && !marker.VisibleOnLayer(EditorTimeline.inst.Layer))
                color.a *= EditorConfig.Instance.AnnotationOtherLayerOpacity.Value;
            for (int p = 0; p < annotation.points.Count - 1; p++)
            {
                GL.Color(color);
                Quad(annotation.thickness, annotation.points[p], annotation.points[p + 1], annotation.fixedCamera);
            }
        }

        void Quad(float thickness, Vector2 pos1, Vector2 pos2, bool fixedCamera)
        {
            // base
            var bl = new Vector2(-thickness * thicknessMultiply, 0f);
            var br = new Vector2(thickness * thicknessMultiply, 0f);
            var tr = new Vector2(thickness * thicknessMultiply, RTMath.Distance(pos1, pos2) * distanceMultiply);
            var tl = new Vector2(-thickness * thicknessMultiply, RTMath.Distance(pos1, pos2) * distanceMultiply);

            var angle = ((float)(180.0 / Math.PI) * Mathf.Atan2(pos1.y - pos2.y, pos1.x - pos2.x)) + angleOffset;
            bl = (Vector2)RTMath.Rotate(bl, angle) + pos1;
            br = (Vector2)RTMath.Rotate(br, angle) + pos1;
            tr = (Vector2)RTMath.Rotate(tr, angle) + pos1;
            tl = (Vector2)RTMath.Rotate(tl, angle) + pos1;

            if (fixedCamera)
            {
                var pos = RTLevel.Cameras.FG.transform.position;
                var rot = RTLevel.Cameras.FG.transform.eulerAngles.z;
                var zoom = RTLevel.Cameras.FG.orthographicSize / 20f;

                bl = RTMath.Move(RTMath.Rotate(bl * zoom, rot), pos);
                br = RTMath.Move(RTMath.Rotate(br * zoom, rot), pos);
                tr = RTMath.Move(RTMath.Rotate(tr * zoom, rot), pos);
                tl = RTMath.Move(RTMath.Rotate(tl * zoom, rot), pos);
            }

            GL.Vertex(bl);
            GL.Vertex(br);
            GL.Vertex(tr);
            GL.Vertex(tl);
        }
    }
}
