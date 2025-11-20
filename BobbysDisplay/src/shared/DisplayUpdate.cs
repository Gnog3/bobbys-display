// #define DEBUG

using JimmysUnityUtilities;
using LogicAPI.Data;
using LogicAPI.Networking.Packets;
using MessagePack;

namespace BobbysDisplay.Shared
{
    public static class DisplayUpdateID
    {
        public const byte MatrixId = 0;
        public const byte FloodFillId = 1;
        public const byte CopyId = 2;
        public const byte RectangleId = 3;
        public const byte BufferId = 4;
    }

    [MessagePackObject(false)]
    public class DisplayUpdatePacket : Packet
    {
        [Key(0)] public ComponentAddress Component;
        [Key(1)] public byte[] Data;
    }

    public static class DisplayUpdateLocal
    {
        public static void HandleMatrix(IDisplayData displayData, byte[] pixelData, short x, short y, Color24 color,
            long matrix)
        {
            var screenWidth = displayData.SizeX * Consts.PixelsPerTile;
            var screenHeight = displayData.SizeZ * Consts.PixelsPerTile;

            for (var yZ = 0; yZ < Consts.InputMatrixSize; yZ++)
            {
                for (var xZ = 0; xZ < Consts.InputMatrixSize; xZ++)
                {
                    if ((matrix & 1) != 0)
                    {
                        var targetX = x + xZ;
                        var targetY = y + yZ;
                        if (targetX < screenWidth && targetY < screenHeight)
                        {
                            var targetByte = (targetY * screenWidth + targetX) * Consts.BytesPerPixel;
                            pixelData[targetByte] = color.r;
                            pixelData[targetByte + 1] = color.g;
                            pixelData[targetByte + 2] = color.b;
                        }
                    }

                    matrix >>= 1;
                }
            }
        }

        public static void HandleFloodFill(byte[] pixelData, Color24 color)
        {
            for (var i = 0; i < pixelData.Length; i += 3)
            {
                pixelData[i] = color.r;
                pixelData[i + 1] = color.g;
                pixelData[i + 2] = color.b;
            }
        }

        public static void HandleCopy(IDisplayData displayData, byte[] pixelData, short xT, short yT, short xS,
            short yS, short width, short height)
        {
            var screenWidth = displayData.SizeX * Consts.PixelsPerTile;
            var screenHeight = displayData.SizeZ * Consts.PixelsPerTile;
            var oldData = (byte[])pixelData.Clone();

            for (var yZ = 0; yZ < height; yZ++)
            {
                for (var xZ = 0; xZ < width; xZ++)
                {
                    var sourceX = xS + xZ;
                    var sourceY = yS + yZ;
                    var targetX = xT + xZ;
                    var targetY = yT + yZ;
                    if (sourceX >= screenWidth || sourceY >= screenHeight || targetX >= screenWidth ||
                        targetY >= screenHeight) continue;

                    var sourceByte = (sourceY * screenWidth + sourceX) * Consts.BytesPerPixel;
                    var targetByte = (targetY * screenWidth + targetX) * Consts.BytesPerPixel;
                    pixelData[targetByte] = oldData[sourceByte];
                    pixelData[targetByte + 1] = oldData[sourceByte + 1];
                    pixelData[targetByte + 2] = oldData[sourceByte + 2];
                }
            }
        }

        public static void HandleRectangle(IDisplayData displayData, byte[] pixelData, short x, short y, short width,
            short height, Color24 color)
        {
            var screenWidth = displayData.SizeX * Consts.PixelsPerTile;
            var screenHeight = displayData.SizeZ * Consts.PixelsPerTile;
            
            for (var yZ = 0; yZ < height; yZ++)
            {
                for (var xZ = 0; xZ < width; xZ++)
                {
                    var targetX = x + xZ;
                    var targetY = y + yZ;
                    if (targetX >= screenWidth || targetY >= screenHeight) continue;

                    var targetByte = (targetY * screenWidth + targetX) * Consts.BytesPerPixel;

                    pixelData[targetByte] = color.r;
                    pixelData[targetByte + 1] = color.g;
                    pixelData[targetByte + 2] = color.b;
                }
            }
        }
    }
}