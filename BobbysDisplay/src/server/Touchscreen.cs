using BobbysDisplay.Shared;
using LogicWorld.Server;
using LogicWorld.Server.Circuitry;
using LogicWorld.SharedCode.Networking;
using System;

namespace BobbysDisplay.Server
{
    public class TouchscreenUpdateHandler : IPacketHandler
    {
        public Type PacketType => typeof(TouchscreenUpdatePacket);

        public void Handle(object packet, HandlerContext context)
        {
            var p = (TouchscreenUpdatePacket)packet;
            var clientCode = Program.Get<ICircuitryManager>().LookupComponent(p.Component);
            if (clientCode == null)
            {
                Server.Logger.Error("Handling TouchscreenUpdateHandler: Display not found");
                return;
            }
            var display = clientCode as Display;
            if (display == null)
            {
                Server.Logger.Error("Handling TouchscreenUpdateHandler: Component is not a Display");
                return;
            }
            
            display.HandleTouchscreenUpdate(context.Sender, p.X, p.Y);
        }
    }

    public class TouchscreenReleaseHandler : IPacketHandler
    {
        public Type PacketType => typeof(TouchscreenReleasePacket);

        public void Handle(object packet, HandlerContext context)
        {
            var p = (TouchscreenReleasePacket)packet;
            var clientCode = Program.Get<ICircuitryManager>().LookupComponent(p.Component);
            if (clientCode == null)
            {
                Server.Logger.Error("Handling TouchscreenReleaseHandler: Display not found");
                return;
            }
            var display = clientCode as Display;
            if (display == null)
            {
                Server.Logger.Error("Handling TouchscreenReleaseHandler: Component is not a Display");
                return;
            }
            
            display.HandleTouchscreenRelease(context.Sender);
        }
    }
}