// #define DEBUG

using BobbysDisplay.Shared;
using JimmysUnityUtilities;
using LogicWorld.SharedCode.BinaryStuff;

namespace BobbysDisplay.Server
{
    public class DisplayUpdateWriter
    {
        private ByteWriter _d = new ByteWriter();

        public void Matrix(short x, short y, Color24 color, long matrix)
        {
            _d.Write(DisplayUpdateID.MatrixId).Write(x).Write(y).Write(color).Write(matrix);
        }

        public void FloodFill(Color24 color)
        {
            _d.Write(DisplayUpdateID.FloodFillId).Write(color);
        }

        public void Copy(short xT, short yT, short xS, short yS, short width, short height)
        {
            _d.Write(DisplayUpdateID.CopyId).Write(xT).Write(yT).Write(xS).Write(yS).Write(width).Write(height);
        }

        public void Rectangle(short x, short y, short width, short height, Color24 color)
        {
            _d.Write(DisplayUpdateID.RectangleId).Write(x).Write(y).Write(width).Write(height).Write(color);
        }

        public void Buffer()
        {
            _d.Write(DisplayUpdateID.BufferId);
        }

        public byte[] Finish()
        {
            var data = _d.Finish();
            _d.Dispose();
            _d = new ByteWriter();
            return data;
        }
    }
}