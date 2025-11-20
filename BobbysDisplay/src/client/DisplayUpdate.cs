// #define DEBUG

using BobbysDisplay.Shared;
using LogicWorld.Interfaces;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.SharedCode.Networking;
using System;

namespace BobbysDisplay.Client
{
    public static class DisplayUpdateReader
    {
        public static void Read(byte[] data, Display display)
        {
            var d = new MemoryByteReader(data);
            while (d.ReadPosition != d.DataLength)
            {
                var id = d.ReadByte();

                switch (id)
                {
                    case DisplayUpdateID.MatrixId:
                        display.HandleMatrix(d.ReadInt16(), d.ReadInt16(), d.ReadColor24(), d.ReadInt64());
                        break;
                    case DisplayUpdateID.FloodFillId:
                        display.HandleFloodFill(d.ReadColor24());
                        break;
                    case DisplayUpdateID.CopyId:
                        display.HandleCopy(d.ReadInt16(), d.ReadInt16(), d.ReadInt16(), d.ReadInt16(), d.ReadInt16(),
                            d.ReadInt16());
                        break;
                    case DisplayUpdateID.RectangleId:
                        display.HandleRectangle(d.ReadInt16(), d.ReadInt16(), d.ReadInt16(), d.ReadInt16(),
                            d.ReadColor24());
                        break;
                    case DisplayUpdateID.BufferId:
                        display.HandleBuffer();
                        break;
                }
            }
        }
    }

    public class DisplayUpdateHandler : IPacketHandler
    {
        public Type PacketType => typeof(DisplayUpdatePacket);

        public void Handle(object packet, HandlerContext context)
        {
#if DEBUG
            Client.Logger.Info("Received DisplayUpdate packet");
#endif
            var p = (DisplayUpdatePacket)packet;
#if DEBUG
            Client.Logger.Info($"DisplayUpdate packet: component: {p.Component}, length: {p.Data.Length}");
#endif
            var clientCode = Instances.MainWorld.Renderer.Entities.GetClientCode(p.Component);
            if (clientCode == null)
            {
                Client.Logger.Error("Handling DisplayUpdatePacket: Display not found");
                return;
            }

            var display = clientCode as Display;
            if (display == null)
            {
                Client.Logger.Error("Handling DisplayUpdatePacket: Component is not a Display");
                return;
            }

            DisplayUpdateReader.Read(p.Data, display);
            display.QueueFrameUpdate();
        }
    }
}
