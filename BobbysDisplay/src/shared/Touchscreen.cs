using LogicAPI.Data;
using LogicAPI.Networking.Packets;
using MessagePack;

namespace BobbysDisplay.Shared
{
    [MessagePackObject(false)]
    public class TouchscreenUpdatePacket : Packet
    {
        [Key(0)] public ComponentAddress Component;
        [Key(1)] public int X;
        [Key(2)] public int Y;
    }

    [MessagePackObject(false)]
    public class TouchscreenReleasePacket : Packet
    {
        [Key(0)] public ComponentAddress Component;
    }
}