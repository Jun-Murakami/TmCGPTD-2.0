using Avalonia.Controls;
using AvaloniaEdit.Rendering;
using System.Collections.Generic;

namespace TmCGPTD.Views
{
    using Pair = KeyValuePair<int, Control>;
    public class ElementGenerator : VisualLineElementGenerator, IComparer<Pair>
    {
        public List<Pair> controls = new List<Pair>();

        /// <summary>
        /// Gets the first interested offset using binary search
        /// </summary>
        /// <returns>The first interested offset.</returns>
        /// <param name="startOffset">Start offset.</param>
        public override int GetFirstInterestedOffset(int startOffset)
        {
            int pos = controls.BinarySearch(new Pair(startOffset, null), this);
            if (pos < 0)
                pos = ~pos;
            if (pos < controls.Count)
                return controls[pos].Key;
            else
                return -1;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            int pos = controls.BinarySearch(new Pair(offset, null), this);
            if (pos >= 0)
                return new InlineObjectElement(0, controls[pos].Value);
            else
                return null;
        }

        int IComparer<Pair>.Compare(Pair x, Pair y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }
}
