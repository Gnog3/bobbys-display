// #define DEBUG
namespace BobbysDisplay.Shared
{
    public interface IDisplayData
    {
        int SizeX { get; set; }
        int SizeZ { get; set; }
        byte[] PixelData { get; set; }
    }
}