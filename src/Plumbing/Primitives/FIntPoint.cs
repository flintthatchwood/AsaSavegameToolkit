using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsaSavegameToolkit.Plumbing.Primitives
{
    public class FIntPoint
    {

        public int X { get; }
        public int Y { get; }

        public FIntPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"IntPoint({X}, {Y})";
    }
}
