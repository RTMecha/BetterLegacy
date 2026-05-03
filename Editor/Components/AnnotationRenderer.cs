using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Components
{
    public class AnnotationRenderer : MonoBehaviour
    {
        //public int lineCount = 100;
        //public float radius = 3f;
        Camera current;

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
            GL.Begin(GL.LINES);
            var time = AudioManager.inst.CurrentAudioSource.time;
            for (int i = 0; i < GameData.Current.data.markers.Count; i++)
            {
                var marker = GameData.Current.data.markers[i];
                if (!RTMarkerEditor.inst.IsInMarkerArea(marker, time))
                    continue;

                for (int j = 0; j < marker.annotations.Count; j++)
                {
                    var annotation = marker.annotations[j];
                    var color = !string.IsNullOrEmpty(annotation.hexColor) ? RTColors.HexToColor(annotation.hexColor) : RTMarkerEditor.inst.GetColor(annotation.color);
                    color.a *= annotation.opacity;
                    for (int p = 0; p < annotation.points.Count - 1; p++)
                    {
                        GL.Color(color);
                        GL.Vertex(annotation.points[p]);
                        GL.Vertex(annotation.points[p + 1]);
                    }
                }
            }

            //for (int i = 0; i < lineCount; ++i)
            //{
            //    float a = i / (float)lineCount;
            //    float angle = a * Mathf.PI * 2;
            //    // Vertex colors change from red to green
            //    GL.Color(new Color(a, 1 - a, 0, 0.8F));
            //    // One vertex at transform position
            //    GL.Vertex3(0, 0, 0);
            //    // Another vertex at edge of circle
            //    GL.Vertex3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            //}
            GL.End();
            GL.PopMatrix();
        }
    }
}
