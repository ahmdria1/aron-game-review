// (c) Ahmed Alamrani. All rights reserved.
using System.Collections.Generic;

namespace Aron.Logic
{
    /// <summary>Shopping cart for Ahmed's grocery shop. Kept in memory only; checkout happens on his website.</summary>
    public class GroceryCart
    {
        public const int MaxPerItem = 99;

        public struct Line
        {
            public string id;
            public int quantity;
        }

        private readonly List<Line> lines = new List<Line>();

        public IReadOnlyList<Line> Lines => lines;
        public bool IsEmpty => lines.Count == 0;

        public int TotalQuantity
        {
            get { int n = 0; foreach (var l in lines) n += l.quantity; return n; }
        }

        public int QuantityOf(string id)
        {
            int i = IndexOf(id);
            return i < 0 ? 0 : lines[i].quantity;
        }

        public void Add(string id, int amount = 1)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;
            Set(id, QuantityOf(id) + amount);
        }

        public void Remove(string id, int amount = 1)
        {
            if (amount <= 0) return;
            Set(id, QuantityOf(id) - amount);
        }

        /// <summary>Sets the count for one product, clamped to 0..<see cref="MaxPerItem"/>. Zero takes it out.</summary>
        public void Set(string id, int quantity)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (quantity > MaxPerItem) quantity = MaxPerItem;
            int i = IndexOf(id);
            if (quantity <= 0)
            {
                if (i >= 0) lines.RemoveAt(i);
                return;
            }
            var line = new Line { id = id, quantity = quantity };
            if (i >= 0) lines[i] = line;
            else lines.Add(line);
        }

        public void Clear() => lines.Clear();

        private int IndexOf(string id)
        {
            for (int i = 0; i < lines.Count; i++) if (lines[i].id == id) return i;
            return -1;
        }
    }
}
