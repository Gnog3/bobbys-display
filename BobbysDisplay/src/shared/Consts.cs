// #define DEBUG
namespace BobbysDisplay.Shared
{
    public class Consts
    {
        private const int MaxResolution = 1024;

        public const int PixelsPerTile = 4;
        public const int MinSize = 16;
        public const int MaxSize = MaxResolution / PixelsPerTile;
        public const int BytesPerPixel = 3;
        public const int InitialResolution = MinSize * PixelsPerTile;

        public const int InitialPixelDataLength =
            MinSize * MinSize * PixelsPerTile * PixelsPerTile * BytesPerPixel;


        public const float InputBlockScaleX =
            InputMatrixSize + PositionColumns + ColorColumns + 1 + OutputColumns - 0.1f;

        public const float InputBlockScaleY = 5f / 6f;
        public const float InputBlockScaleZ = 10.0f - 0.1f;

        public const int InputMatrixSize = 8;
        public const int ControlPegs = 6;
        public const int PositionPegs = 10;
        public const int PositionColumns = 2 * 3; // 2 axis * 3 positions
        public const int ColorPegs = 8;
        public const int ColorColumns = 3;

        public const int InputPegs =
            0
            + InputMatrixSize * InputMatrixSize
            + PositionPegs * PositionColumns
            + ColorPegs * ColorColumns
            + ControlPegs;

        public const int OutputColumns = 2;
        public const int OutputPegs = PositionPegs * OutputColumns + 1;
        public const int PegTouchscreen = PositionPegs * OutputColumns;
        public const int PegTouchscreenPosition = 0;
        
        public const int PegMatrix = 0;
        public const int PegTarget = PegMatrix + InputMatrixSize * InputMatrixSize;
        public const int PegSource = PegTarget + PositionPegs * 2;
        public const int PegSize = PegSource + PositionPegs * 2;
        public const int PegColor = PegSize + PositionPegs * 2;
        private const int PegControl = PegColor + ColorPegs * ColorColumns;
        public const int PegSetMatrix = PegControl;
        public const int PegFloodFill = PegSetMatrix + 1;
        public const int PegCopy = PegFloodFill + 1;
        public const int PegRectangle = PegCopy + 1;
        public const int PegBuffer = PegRectangle + 1;
        public const int PegSave = PegBuffer + 1;
    }
}