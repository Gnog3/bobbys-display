// #define DEBUG

using System.Diagnostics;
using BobbysDisplay.Shared;
using JimmysUnityUtilities;
using LogicAPI.Server;
using LogicAPI.Server.Networking;
using LogicWorld.Server;
using LogicWorld.Server.Circuitry;
using System;
using EccsLogicWorldAPI.Server.Injectors;
using EccsLogicWorldAPI.Shared.AccessHelper;
using LogicAPI.Data;
using LogicAPI.Networking;
using LogicLog;
using Timer = System.Timers.Timer;

namespace BobbysDisplay.Server
{
    public class Server : ServerMod
    {
        public new static readonly ILogicLogger Logger = LogicLogger.For("BobbysDisplay");

        protected override void Initialize()
        {
#if DEBUG
            Logger.Info("Server.Initialize()");
#endif
            RawPacketHandlerInjector.addPacketHandler(new TouchscreenUpdateHandler());
            RawPacketHandlerInjector.addPacketHandler(new TouchscreenReleaseHandler());
        }
    }

    public class Display : LogicComponent<IDisplayData>
    {
        public override bool HasPersistentValues => true;

        private byte[] _pixelData;
        private bool _dirtyMemory;
        private DisplayUpdateWriter _writer;
        private int _previousSizeX;
        private int _previousSizeZ;
        private Timer _timer;
        private NetworkServer _networkServer;
        private Stopwatch _stopwatch;
        private IPlayerManager _playerManager;
        private bool _used;
        private PlayerID _usedBy;
        private int _touchscreenX;
        private int _touchscreenY;

        protected override void SetDataDefaultValues()
        {
            Data.SizeX = Consts.MinSize;
            Data.SizeZ = Consts.MinSize;
            Data.PixelData = new byte[Consts.InitialPixelDataLength];
        }

        protected override void Initialize()
        {
#if DEBUG
            Server.Logger.Info(
                $"Display: Initialize SizeX: {Data.SizeX}, SizeZ: {Data.SizeZ}, Length: {Data.PixelData.Length}");
#endif
            _pixelData = Data.PixelData;
            _dirtyMemory = false;
            _writer = new DisplayUpdateWriter();
            _previousSizeX = Data.SizeX;
            _previousSizeZ = Data.SizeZ;

            var server = (LogicWorld.Server.Server)Program.Get<IServer>();
            var fieldScheduler = Fields.getPrivate(server, "ServerTickScheduler");
            var scheduler = (ServerTickScheduler)Fields.getNonNull(fieldScheduler, server);
            var fieldTimer = Fields.getPrivate(scheduler, "TickInvocationTimer");
            _timer = (Timer)Fields.getNonNull(fieldTimer, scheduler);

            _networkServer = Program.Get<NetworkServer>();
            _stopwatch = new Stopwatch();
            _playerManager = Program.Get<IPlayerManager>();
            _used = false;
        }

        private short GetShort(int index, int bits)
        {
            var result = 0;
            for (var i = 0; i < bits; i++)
            {
                var on = Convert.ToInt32(Inputs[index + i].On);
                result |= on << i;
            }

            return (short)result;
        }

        private void SetShort(int index, int bits, short value)
        {
            for (var i = 0; i < bits; i++)
            {
                Outputs[index + i].On = (value & 1) != 0;
                value >>= 1;
            }
        }

        private long GetMatrix()
        {
            long result = 0;
            for (var i = 0; i < Consts.InputMatrixSize * Consts.InputMatrixSize; i++)
            {
                var on = Convert.ToInt64(Inputs[i].On);
                result |= (on << i);
            }

            return result;
        }

        private Color24 GetColor()
        {
            var r = GetShort(Consts.PegColor, Consts.ColorPegs);
            var g = GetShort(Consts.PegColor + Consts.ColorPegs, Consts.ColorPegs);
            var b = GetShort(Consts.PegColor + Consts.ColorPegs * 2, Consts.ColorPegs);
            return new Color24((byte)r, (byte)g, (byte)b);
        }

        private void PixelDataChanged()
        {
            if (!_dirtyMemory)
            {
                _dirtyMemory = true;
                _stopwatch.Restart();
            }

            QueueLogicUpdate();
        }

        protected override void DoLogicUpdate()
        {
#if DEBUG
            Server.Logger.Info("Display: DoLogicUpdate");
#endif
            var justUpdated = false;
            if (Inputs[Consts.PegSetMatrix].On)
            {
                var x = GetShort(Consts.PegTarget, Consts.PositionPegs);
                var y = GetShort(Consts.PegTarget + Consts.PositionPegs, Consts.PositionPegs);
                var color = GetColor();
                var matrix = GetMatrix();
                DisplayUpdateLocal.HandleMatrix(Data, _pixelData, x, y, color, matrix);
                _writer.Matrix(x, y, color, matrix);
                PixelDataChanged();
                justUpdated = true;
            }
            else if (Inputs[Consts.PegFloodFill].On)
            {
                var color = GetColor();
                DisplayUpdateLocal.HandleFloodFill(_pixelData, color);
                _writer.FloodFill(color);
                PixelDataChanged();
                justUpdated = true;
            }
            else if (Inputs[Consts.PegCopy].On)
            {
                var xT = GetShort(Consts.PegTarget, Consts.PositionPegs);
                var yT = GetShort(Consts.PegTarget + Consts.PositionPegs, Consts.PositionPegs);
                var xS = GetShort(Consts.PegSource, Consts.PositionPegs);
                var yS = GetShort(Consts.PegSource + Consts.PositionPegs, Consts.PositionPegs);
                var width = GetShort(Consts.PegSize, Consts.PositionPegs);
                var height = GetShort(Consts.PegSize + Consts.PositionPegs, Consts.PositionPegs);
                DisplayUpdateLocal.HandleCopy(Data, _pixelData, xT, yT, xS, yS, width, height);
                _writer.Copy(xT, yT, xS, yS, width, height);
                PixelDataChanged();
                justUpdated = true;
            }
            else if (Inputs[Consts.PegRectangle].On)
            {
                var x = GetShort(Consts.PegTarget, Consts.PositionPegs);
                var y = GetShort(Consts.PegTarget + Consts.PositionPegs, Consts.PositionPegs);
                var width = GetShort(Consts.PegSize, Consts.PositionPegs);
                var height = GetShort(Consts.PegSize + Consts.PositionPegs, Consts.PositionPegs);
                var color = GetColor();
                DisplayUpdateLocal.HandleRectangle(Data, _pixelData, x, y, width, height, color);
                _writer.Rectangle(x, y, width, height, color);
                PixelDataChanged();
                justUpdated = true;
            }
            
            else if (Inputs[Consts.PegSave].On)
            {
                Data.PixelData = _pixelData;
            }

            if (Inputs[Consts.PegBuffer].On)
            {
                _writer.Buffer();
                PixelDataChanged();
                justUpdated = true;
            }

            var tickRate = _timer.GetInterval().TotalSeconds;
            var sendUpdates = _dirtyMemory && tickRate > 0.0 &&
                              _stopwatch.Elapsed.TotalSeconds >= tickRate;
            if (sendUpdates)
            {
#if DEBUG
                Server.Logger.Info("Display: Send Updates");
#endif
                var packet = new DisplayUpdatePacket()
                {
                    Component = Address,
                    Data = _writer.Finish(),
                };
                _networkServer.Broadcast(packet);

                if (!justUpdated)
                {
                    _dirtyMemory = false;
                    _stopwatch.Reset();
                }
                else
                {
                    _stopwatch.Restart();
                }
            }

            if (_used)
            {
                Outputs[Consts.PegTouchscreen].On = true;
                SetShort(Consts.PegTouchscreenPosition, Consts.PositionPegs, (short)_touchscreenX);
                SetShort(Consts.PegTouchscreenPosition + Consts.PositionPegs, Consts.PositionPegs,
                    (short)_touchscreenY);
            }
            else
            {
                Outputs[Consts.PegTouchscreen].On = false;
                SetShort(Consts.PegTouchscreenPosition, Consts.PositionPegs, 0);
                SetShort(Consts.PegTouchscreenPosition + Consts.PositionPegs, Consts.PositionPegs, 0);
            }
        }

        protected override void OnCustomDataUpdated()
        {
#if DEBUG
            Server.Logger.Info("Display: OnCustomDataUpdated");
#endif
            if (_previousSizeX != Data.SizeX || _previousSizeZ != Data.SizeZ)
            {
#if DEBUG
                Server.Logger.Info(
                    $"Display: OnCustomDataUpdated Size Changed {_previousSizeX}x {_previousSizeZ}z => {Data.SizeX}x{Data.SizeZ}z");
#endif
                _previousSizeX = Data.SizeX;
                _previousSizeZ = Data.SizeZ;
                var newSize = Data.SizeX * Data.SizeZ * Consts.PixelsPerTile * Consts.PixelsPerTile *
                              Consts.BytesPerPixel;
                Data.PixelData = new byte[newSize];
                _pixelData = Data.PixelData;
            }
        }

        protected override void SavePersistentValuesToCustomData()
        {
#if DEBUG
            Server.Logger.Info("Display: SavePersistentValuesToCustomData()");
#endif
            Data.PixelData = _pixelData;
        }

        public void HandleTouchscreenUpdate(Connection connection, int x, int y)
        {
            var playerID = _playerManager.GetPlayerIDFromConnection(connection);
            if (_used && _usedBy != playerID)
            {
                return;
            }

            _used = true;
            _usedBy = playerID;
            var height = Data.SizeZ * Consts.PixelsPerTile;
            _touchscreenX = x;
            _touchscreenY = height - y - 1;
            QueueLogicUpdate();
        }

        public void HandleTouchscreenRelease(Connection connection)
        {
            var playerID = _playerManager.GetPlayerIDFromConnection(connection);
            if (_used && _usedBy != playerID)
            {
                return;
            }

            _used = false;
            QueueLogicUpdate();
        }
    }
}
