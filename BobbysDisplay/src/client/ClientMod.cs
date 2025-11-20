// #define DEBUG

using BobbysDisplay.Shared;
using JimmysUnityUtilities;
using LogicAPI.Client;
using LogicAPI.Data;
using LogicWorld.ClientCode.Resizing;
using LogicWorld.Rendering.Components;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.BinaryStuff;
using LogicWorld.SharedCode.Components;
using UnityEngine;
using System;
using EccsLogicWorldAPI.Client.Injectors;
using EccsLogicWorldAPI.Shared;
using EccsLogicWorldAPI.Shared.AccessHelper;
using LogicLog;
using LogicWorld;
using LogicWorld.Interfaces;
using LogicWorld.Physics;
using LogicWorld.Players;
using LogicWorld.Rendering.Chunks;
using LogicWorld.UI;
using IDisplayData = BobbysDisplay.Shared.IDisplayData;
using Types = EccsLogicWorldAPI.Shared.AccessHelper.Types;

namespace BobbysDisplay.Client
{
    public class Client : ClientMod
    {
        public new static readonly ILogicLogger Logger = LogicLogger.For("BobbysDisplay");

        protected override void Initialize()
        {
#if DEBUG
            Logger.Info("Client.Initialize()");
#endif

            RawPacketHandlerInjector.addPacketHandler(new DisplayUpdateHandler());
            var fpiType = Types.findInAssembly(typeof(SceneAndNetworkManager), "LogicWorld.FirstPersonInteraction");
            var method = Methods.getPrivateStatic(fpiType, "OnRun");
            var harmonyInstance = HarmonyAtRuntime.getHarmonyInstance("BobbysDisplay");
            HarmonyAtRuntime.patch(harmonyInstance, method, Methods.getPrivateStatic(typeof(Client), "OnRun"));
            var method2 = Methods.getPrivateStatic(typeof(ChairMenu), "OnRun");
            HarmonyAtRuntime.patch(harmonyInstance, method2, Methods.getPrivateStatic(typeof(Client), "OnRun"));
        }

        private static ComponentAddress _currentlyLookingAt = ComponentAddress.Empty;

        private static void SendRelease(ComponentAddress address)
        {
            var packet = new TouchscreenReleasePacket
            {
                Component = address,
            };
            Instances.SendData.Send(packet);
        }

        private static void StopLooking()
        {
            if (_currentlyLookingAt == ComponentAddress.Empty) return;

            SendRelease(_currentlyLookingAt);
            _currentlyLookingAt = ComponentAddress.Empty;
        }

        private static void LookAt(ComponentAddress address, int x, int y)
        {
            if (_currentlyLookingAt != ComponentAddress.Empty && _currentlyLookingAt != address)
            {
                SendRelease(_currentlyLookingAt);
            }

            _currentlyLookingAt = address;
            var packet = new TouchscreenUpdatePacket
            {
                Component = address,
                X = x,
                Y = y,
            };
            Instances.SendData.Send(packet);
        }

        private static void OnRun()
        {
            HitInfo hitInfo = PlayerCaster.CameraCast(Masks.Structure, 100.0f);
            if (!hitInfo.HitComponent)
            {
                StopLooking();
                return;
            }

            var component = Instances.MainWorld.Renderer.Entities.GetClientCode(hitInfo.cAddress) as Display;
            if (component == null)
            {
                StopLooking();
                return;
            }

            var fixedRelativePoint = component.Component.ToLocalSpace(hitInfo.WorldPoint);
            if (!fixedRelativePoint.y.IsPrettyCloseTo(0.35f))
            {
                StopLooking();
                return;
            }

            fixedRelativePoint *= Consts.PixelsPerTile;
            fixedRelativePoint += Vector3.one * 2.0f;
            var x = (int)fixedRelativePoint.x;
            var y = (int)fixedRelativePoint.z;
            LookAt(component.Address, x, y);
        }
    }


    public class Display : ComponentClientCode<IDisplayData>, IResizableX, IResizableZ
    {
        private int _previousSizeX;

        public int SizeX
        {
            get => Data.SizeX;
            set => Data.SizeX = value;
        }

        public int MinX => Consts.MinSize;
        public int MaxX => Consts.MaxSize;
        public float GridIntervalX => 1f;

        private int _previousSizeZ;

        public int SizeZ
        {
            get => Data.SizeZ;
            set => Data.SizeZ = value;
        }

        public int MinZ => Consts.MinSize;
        public int MaxZ => Consts.MaxSize;
        public float GridIntervalZ => 1f;

        private byte[] _pixelData;
        private Texture2D _texture;
        private bool _sendToTexture;

        protected override void SetDataDefaultValues()
        {
#if DEBUG
            Client.Logger.Info("Display.SetDataDefaultValues()");
#endif
            Data.SizeX = Consts.MinSize;
            Data.SizeZ = Consts.MinSize;
            Data.PixelData = new byte[Consts.InitialPixelDataLength];
        }

        protected override void Initialize()
        {
#if DEBUG
            Client.Logger.Info(
                $"Display.Initialize(), SizeX: {SizeX}, SizeZ: {SizeZ}, Data.Length: {Data.PixelData.Length}");
#endif
            _pixelData = Data.PixelData;
            _sendToTexture = false;
        }

        private void SetInputPosition(int index, int x, int z)
        {
            if (index > 255)
            {
                throw new Exception("Setting input for index > 255");
            }

            float xConv = x;
            float yConv = -Consts.InputBlockScaleY;
            float zConv = z;
            SetInputPosition((byte)index, new Vector3(xConv, yConv, zConv));
        }

        private void SetOutputPosition(int index, int x, int z)
        {
            if (index > 255)
            {
                throw new Exception("Setting input for index > 255");
            }

            float xConv = x;
            float yConv = -Consts.InputBlockScaleY;
            float zConv = z;
            SetOutputPosition((byte)index, new Vector3(xConv, yConv, zConv));
        }

        protected override void DataUpdate()
        {
#if DEBUG
            Client.Logger.Info("Display.DataUpdate()");
#endif
            _pixelData = Data.PixelData;
            _sendToTexture = true;
            QueueFrameUpdate();
            if (SizeX == _previousSizeX && SizeZ == _previousSizeZ) return;
#if DEBUG
            Client.Logger.Info($"Received size change. {_previousSizeX}x {_previousSizeZ}y => {SizeX}x {SizeZ}y");
#endif
            _previousSizeX = SizeX;
            _previousSizeZ = SizeZ;

            // Panel
            SetBlockScale(0, new Vector3(SizeX, 1f / 3f, SizeZ));

            // Input Block
            SetBlockScale(1, new Vector3(Consts.InputBlockScaleZ, Consts.InputBlockScaleY, Consts.InputBlockScaleX));

            // Display
            SetDecorationPosition(0, new Vector3((SizeX / 2f - 0.5f) * 0.3f, 0.35f * 0.3f, (SizeZ / 2f - 0.5f) * 0.3f));
            SetDecorationScale(0, new Vector3(SizeX * 0.3f, -SizeZ * 0.3f, 1));
            _texture.Resize(SizeX * Consts.PixelsPerTile, SizeZ * Consts.PixelsPerTile);
            
            // Inputs
            var inputIndex = 0;
            var xOffset = 0;
            // Matrix
            for (var z = Consts.InputMatrixSize - 1; z >= 0; z--)
            {
                for (var x = 0; x < Consts.InputMatrixSize; x++)
                {
                    SetInputPosition(inputIndex, x, z);
                    inputIndex++;
                }
            }

            xOffset += Consts.InputMatrixSize;
            // Positions
            for (var x = 0; x < Consts.PositionColumns; x++)
            {
                for (var z = 0; z < Consts.PositionPegs; z++)
                {
                    SetInputPosition(inputIndex, x + xOffset, z);
                    inputIndex++;
                }
            }

            xOffset += Consts.PositionColumns;
            // Colors
            for (var x = 0; x < Consts.ColorColumns; x++)
            {
                for (var z = 0; z < Consts.ColorPegs; z++)
                {
                    SetInputPosition(inputIndex, x + xOffset, z);
                    inputIndex++;
                }
            }

            xOffset += Consts.ColorColumns;
            // ControlPegs
            for (var z = 0; z < Consts.ControlPegs; z++)
            {
                SetInputPosition(inputIndex, xOffset, z);
                inputIndex++;
            }

            xOffset += 1;
            // Output Pegs
            var outputIndex = 0;
            for (var x = 0; x < Consts.OutputColumns; x++)
            {
                for (var z = 0; z < Consts.PositionPegs; z++)
                {
                    SetOutputPosition(outputIndex, x + xOffset, z);
                    outputIndex++;
                }
            }

            SetOutputPosition(Consts.PegTouchscreen, xOffset - 1, Consts.ControlPegs);
        }

        protected override void FrameUpdate()
        {
#if DEBUG
            Client.Logger.Info("FrameUpdate()");
#endif
            if (!_sendToTexture) return;
            _sendToTexture = false;
            
            var textureSizeBytes = _texture.width * _texture.height * Consts.BytesPerPixel;
            if (textureSizeBytes != _pixelData.Length)
            {
                Client.Logger.Warn(
                    $"Display FrameUpdate: texture size mismatch: texture: {textureSizeBytes}, _pixelData: {_pixelData.Length}");
                return;
            }

            _texture.SetPixelData(_pixelData, 0);
            _texture.Apply();
        }

        protected override IDecoration[] GenerateDecorations(Transform parentToCreateDecorationsUnder)
        {
#if DEBUG
            Client.Logger.Info("GenerateDecorations()");
#endif
            if (_texture == null)
            {
#if DEBUG
                Client.Logger.Info("GenerateDecorations() texture is null, generating texture");
#endif
                _texture = new Texture2D(Data.SizeX * Consts.PixelsPerTile, Data.SizeZ * Consts.PixelsPerTile,
                    TextureFormat.RGB24, true)
                {
                    filterMode = FilterMode.Point,
                };
                _sendToTexture = true;
            }
            else
            {
#if DEBUG
                Client.Logger.Info("GenerateDecorations() texture is not null");
#endif
            }

            var material = new Material(Shader.Find("Unlit/Texture"))
            {
                mainTexture = _texture
            };
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            gameObject.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            gameObject.GetComponent<Renderer>().material = material;
            gameObject.transform.SetParent(parentToCreateDecorationsUnder);
            return new IDecoration[1]
            {
                new Decoration
                {
                    LocalPosition = new Vector3(-0.5f, 0.0f, -0.5f) * 0.3f,
                    LocalRotation = Quaternion.Euler(90f, 0f, 0f),
                    DecorationObject = gameObject,
                    AutoSetupColliders = true,
                    IncludeInModels = true
                }
            };
        }

        public void HandleMatrix(short x, short y, Color24 color, long matrix)
        {
#if DEBUG
            Client.Logger.Info($"Display.HandleMatrix() {x} {y} {color} {matrix}");
#endif
            DisplayUpdateLocal.HandleMatrix(Data, _pixelData, x, y, color, matrix);
        }

        public void HandleFloodFill(Color24 color)
        {
#if DEBUG
            Client.Logger.Info("Display.HandleFloodFill()");
#endif
            DisplayUpdateLocal.HandleFloodFill(_pixelData, color);
        }

        public void HandleCopy(short xT, short yT, short xS, short yS, short width, short height)
        {
#if DEBUG
            Client.Logger.Info("Display.HandleCopy()");
#endif
            DisplayUpdateLocal.HandleCopy(Data, _pixelData, xT, yT, xS, yS, width, height);
        }

        public void HandleRectangle(short x, short y, short width, short height, Color24 color)
        {
#if DEBUG
            Client.Logger.Info("Display.HandleRectangle()");
#endif
            DisplayUpdateLocal.HandleRectangle(Data, _pixelData, x, y, width, height, color);
        }

        public void HandleBuffer()
        {
#if DEBUG
            Client.Logger.Info("Display.HandleBuffer()");
#endif
            _sendToTexture = true;
        }
    }

    public class DisplayPrefabGenerator : DynamicPrefabGenerator<int>
    {
        protected override int GetIdentifierFor(ComponentData componentData)
        {
            return 0;
        }

        protected override Prefab GeneratePrefabFor(int _)
        {
            var inputs = new ComponentInput[Consts.InputPegs];
            for (var i = 0; i < inputs.Length; i++)
            {
                inputs[i] = new ComponentInput
                {
                    Rotation = new Vector3(180f, 0f, 0f),
                };
            }

            var outputs = new ComponentOutput[Consts.OutputPegs];
            for (var i = 0; i < outputs.Length; i++)
            {
                outputs[i] = new ComponentOutput
                {
                    Rotation = new Vector3(180f, 0f, 0f),
                };
            }

            var blocks = new[]
            {
                new Block
                {
                    Position = new Vector3(-0.5f, 0f, -0.5f),
                    MeshName = "OriginCube",
                    RawColor = Color24.Black
                },
                new Block
                {
                    Position = new Vector3(-0.43f, -0.02f, -0.45f),
                    Rotation = new Vector3(180f, 270f, 0f),
                    MeshName = "OriginCube",
                    ColliderData = new ColliderData
                    {
                        Transform = new ColliderTransform
                        {
                            LocalScale = new Vector3(1f, 0.4f, 1f),
                            LocalPosition = new Vector3(0f, 0.6f, 0f)
                        }
                    }
                }
            };
            return new Prefab
            {
                Blocks = blocks,
                Inputs = inputs,
                Outputs = outputs,
            };
        }

        public override (int inputCount, int outputCount) GetDefaultPegCounts()
        {
            return (Consts.InputPegs, Consts.OutputPegs);
        }
    }

    public class DisplayPlacingRulesGenerator : DynamicPlacingRulesGenerator<(int, int)>
    {
        protected override (int, int) GetIdentifierFor(ComponentData componentData)
        {
            var m = new CustomDataManager<IDisplayData>();
            var result = m.TryDeserializeData(componentData.CustomData);
            return result ? (m.Data.SizeX, m.Data.SizeZ) : (Consts.MinSize, Consts.MinSize);
        }

        protected override PlacingRules GeneratePlacingRulesFor((int, int) dimensions)
        {
            return PlacingRules.FlippablePanelOfSize(dimensions.Item1, dimensions.Item2);
        }
    }
}