using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Group of shapes.
    /// </summary>
    public class ShapeGroup : Exists
    {
        public ShapeGroup(string name, int type, Sprite icon)
        {
            this.name = name;
            this.type = type;
            this.icon = icon;
        }

        public string name;
        public ShapeType ShapeType => (ShapeType)type;
        public int type;
        public Sprite icon;

        public List<Shape> shapes = new List<Shape>();

        public Shape this[int index]
        {
            get => shapes[index];
            set => shapes[index] = value;
        }

        public int Count => shapes.Count;

        public void Add(Shape shape) => shapes.Add(shape);

        public Shape GetShape(int index) => shapes[Mathf.Clamp(index, 0, shapes.Count - 1)];

        public bool TryGetShape(int index, out Shape shape) => shapes.TryGetAt(index, out shape);

        public ShapeType GetShapeType() => (ShapeType)type;

        public override string ToString() => $"{name} - {type}";
    }
}
