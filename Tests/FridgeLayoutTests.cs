// Runs the production worker source with minimal, non-Unity test doubles.
using System;
using System.Collections.Generic;
using System.Globalization;
using TurboMegaMod;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
    public static class Mathf
    {
        public static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
    }
}
namespace Verse
{
    public enum Rot4 { North, East, South, West }
    public class Thing { public int thingIDNumber; public UnityEngine.Vector2 Size; public UnityEngine.Vector3 GraphicOffset; }
}
namespace AdaptiveStorage
{
    public struct StorageCell { public int Index; }
    public class ItemGraphic { }
    public class GraphicsDef { }
    public class ThingCollection
    {
        public readonly List<Verse.Thing> Items = new List<Verse.Thing>();
        public readonly Dictionary<Verse.Thing, int> Cells = new Dictionary<Verse.Thing, int>();
        public int Count { get { return Items.Count; } }
        public Verse.Thing this[int index] { get { return Items[index]; } }
        public bool ContainsAndAllows(Verse.Thing thing) { return Cells.ContainsKey(thing); }
        public StorageCell StoragePositionAt(int index) { return StoragePositionOf(Items[index]); }
        public StorageCell StoragePositionOf(Verse.Thing item) { return new StorageCell { Index = Cells[item] }; }
    }
    public class ThingClass
    {
        public Verse.Rot4 Rotation;
        public ThingCollection StoredThings = new ThingCollection();
    }
    public class ItemGraphicWorker
    {
        public ItemGraphicWorker(ItemGraphic graphic, GraphicsDef definition) { }
        protected virtual UnityEngine.Vector3 ItemOffsetAt(StorageCell cell, ThingCollection items,
            Verse.Rot4 rotation, Verse.Thing item, out float stackRotation)
        { stackRotation = 0; return new UnityEngine.Vector3(0, 0, 0); }
        protected virtual UnityEngine.Vector2 DrawScaleForItem(Verse.Thing item, ThingClass building)
        { return new UnityEngine.Vector2(0.75f, 0.75f); }
        public virtual void UpdatePrintData(PrintDatas.PrintData data, ThingClass building)
        {
            float angle;
            StorageCell cell = building.StoredThings.StoragePositionOf(data.Thing);
            UnityEngine.Vector3 offset = ItemOffsetAt(cell, building.StoredThings, building.Rotation, data.Thing, out angle);
            offset.z += cell.Index * 0.5f;
            offset.x += data.Thing.GraphicOffset.x;
            offset.z += data.Thing.GraphicOffset.z;
            data.DrawOffset = offset;
            data.SetDrawScale(DrawScaleForItem(data.Thing, building), new UnityEngine.Vector2(0.95f, 0.5f));
        }
    }
}
namespace AdaptiveStorage.PrintDatas
{
    public class PrintData
    {
        public Verse.Thing Thing;
        public UnityEngine.Vector3 DrawOffset;
        public UnityEngine.Vector2 RotatedScaledDrawSize;
        public void SetDrawScale(UnityEngine.Vector2 scale, UnityEngine.Vector2 max)
        {
            float x = Thing.Size.x * scale.x, y = Thing.Size.y * scale.y;
            float factor = Math.Min(1f, Math.Min(max.x / x, max.y / y));
            RotatedScaledDrawSize = new UnityEngine.Vector2(x * factor, y * factor);
        }
    }
}
public static class FridgeLayoutTests
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    public static void Main()
    {
        int cases = 0;
        float fullWidth = 0, fullHeight = 0;
        var worker = new FridgeItemGraphicWorker(new AdaptiveStorage.ItemGraphic(), new AdaptiveStorage.GraphicsDef());
        var shapes = new[] { new UnityEngine.Vector2(1, 1), new UnityEngine.Vector2(2, 1),
                             new UnityEngine.Vector2(1, 2) };
        foreach (var shape in shapes)
        for (int count = 1; count <= 15; count++)
        {
            var building = new AdaptiveStorage.ThingClass { Rotation = Verse.Rot4.North };
            for (int cell = 0; cell < 2; cell++)
            for (int id = count; id >= 1; id--)
            {
                // Descending insertion order exercises ID-based placement.
                var item = new Verse.Thing { thingIDNumber = cell * 100 + id, Size = shape };
                building.StoredThings.Items.Add(item);
                building.StoredThings.Cells[item] = cell;
            }
            float xmin = float.MaxValue, xmax = float.MinValue, zmin = float.MaxValue, zmax = float.MinValue;
            foreach (var item in building.StoredThings.Items)
            {
                var data = new AdaptiveStorage.PrintDatas.PrintData { Thing = item };
                worker.UpdatePrintData(data, building);
                float left = data.DrawOffset.x - data.RotatedScaledDrawSize.x / 2;
                float right = data.DrawOffset.x + data.RotatedScaledDrawSize.x / 2;
                float bottom = data.DrawOffset.z - data.RotatedScaledDrawSize.y / 2;
                float top = data.DrawOffset.z + data.RotatedScaledDrawSize.y / 2;
                Check(left >= -0.861f && right <= 0.861f && bottom >= -0.301f && top <= 0.771f, "Food exceeds fridge interior");
                xmin = Math.Min(xmin, left); xmax = Math.Max(xmax, right);
                zmin = Math.Min(zmin, bottom); zmax = Math.Max(zmax, top);
                if (shape.x == 1 && shape.y == 1)
                    Check(data.RotatedScaledDrawSize.y >= 0.339f, "Icons regress to the undersized layout");
                var repeated = new AdaptiveStorage.PrintDatas.PrintData { Thing = item };
                worker.UpdatePrintData(repeated, building);
                Check(repeated.DrawOffset.x == data.DrawOffset.x && repeated.DrawOffset.z == data.DrawOffset.z, "Unstable position");
                item.GraphicOffset = new UnityEngine.Vector3(4, 0, -4);
                worker.UpdatePrintData(repeated, building);
                Check(repeated.DrawOffset.x + repeated.RotatedScaledDrawSize.x / 2 <= 0.861f
                    && repeated.DrawOffset.z - repeated.RotatedScaledDrawSize.y / 2 >= -0.301f, "Custom graphic offset bypasses bounds");
                item.GraphicOffset = new UnityEngine.Vector3();
                cases++;
            }
            if (count == 15 && shape.x == 1 && shape.y == 1)
            {
                fullWidth = xmax - xmin; fullHeight = zmax - zmin;
                Check(fullWidth > 1.5f && fullHeight > 1f, "Full fridge looks underfilled");
            }
        }
        Console.WriteLine("{\"cases\":" + cases + ",\"fullFoodWidth\":" + fullWidth.ToString(CultureInfo.InvariantCulture)
            + ",\"fullFoodHeight\":" + fullHeight.ToString(CultureInfo.InvariantCulture) + ",\"passed\":true}");
    }
}
