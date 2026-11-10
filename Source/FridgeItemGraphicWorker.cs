using System;
using AdaptiveStorage;
using AdaptiveStorage.PrintDatas;
using UnityEngine;
using Verse;

namespace TurboMegaMod
{
    // Each storage cell maps to one half of the north-facing fridge interior.
    // Stable thing IDs keep rows from shuffling when another item is refreshed.
    public sealed class FridgeItemGraphicWorker : ItemGraphicWorker
    {
        public FridgeItemGraphicWorker(ItemGraphic graphic, GraphicsDef definition)
            : base(graphic, definition) { }

        private static int CountAndIndex(ThingCollection items, StorageCell cell, Thing item, out int index)
        {
            int count = 0;
            index = 0;
            for (int i = 0; i < items.Count; i++)
            {
                Thing other = items[i];
                if (!items.ContainsAndAllows(other) || items.StoragePositionAt(i).Index != cell.Index)
                    continue;
                count++;
                if (other.thingIDNumber < item.thingIDNumber)
                    index++;
            }
            return Math.Max(1, count);
        }

        private static Vector2 MaximumSize(int count)
        {
            int columns = Math.Min(5, count);
            int rows = (count + columns - 1) / columns;
            float width = columns == 1 ? 0.95f : columns == 2 ? 0.75f
                : columns == 3 ? 0.60f : columns == 4 ? 0.50f : 0.43f;
            float height = rows == 1 ? 0.50f : rows == 2 ? 0.42f : 0.34f;
            return new Vector2(width, height);
        }

        protected override Vector3 ItemOffsetAt(StorageCell cell, ThingCollection items,
            Rot4 rotation, Thing item, out float stackRotation)
        {
            Vector3 offset = base.ItemOffsetAt(cell, items, rotation, item, out stackRotation);
            if (rotation != Rot4.North || !items.ContainsAndAllows(item))
                return offset;
            int index;
            int count = CountAndIndex(items, cell, item, out index);
            int columns = Math.Min(5, count);
            int rows = (count + columns - 1) / columns;
            int row = index / columns;
            int rowCount = Math.Min(columns, count - row * columns);
            int column = index % columns;
            Vector2 maximum = MaximumSize(count);
            float xSpan = 1.68f - maximum.x;
            float zSpan = 0.52f - maximum.y;
            offset.x = rowCount <= 1 ? 0f : (column / (float)(rowCount - 1) - 0.5f) * xSpan;
            offset.z = rows <= 1 ? 0f : (row / (float)(rows - 1) - 0.5f) * zSpan;
            return offset;
        }

        public override void UpdatePrintData(PrintData data, ThingClass building)
        {
            base.UpdatePrintData(data, building);
            if (building.Rotation != Rot4.North || !building.StoredThings.ContainsAndAllows(data.Thing))
                return;
            int index;
            int count = CountAndIndex(building.StoredThings,
                building.StoredThings.StoragePositionOf(data.Thing), data.Thing, out index);
            data.SetDrawScale(DrawScaleForItem(data.Thing, building), MaximumSize(count));
            Vector2 size = data.RotatedScaledDrawSize;
            Vector3 offset = data.DrawOffset;
            // Include an item's own graphic offset in the final bounds check.
            offset.x = Mathf.Clamp(offset.x, -0.86f + size.x / 2f, 0.86f - size.x / 2f);
            offset.z = Mathf.Clamp(offset.z, -0.30f + size.y / 2f, 0.77f - size.y / 2f);
            data.DrawOffset = offset;
        }
    }
}
