using Godot;
using System.Threading;
using WOLF3D.WOLF3DGame.OPL;

namespace WOLF3D.WOLF3DGame.Action
{
    public class LoadingRoom : Spatial
    {
        public ARVROrigin ARVROrigin { get; set; }
        public ARVRCamera ARVRCamera { get; set; }
        public ARVRController LeftController { get; set; }
        public ARVRController RightController { get; set; }

        public LoadingRoom(ushort mapNumber = 0)
        {
            Name = "LoadingRoom for map " + mapNumber;
            MapNumber = mapNumber;
            Main.BackgroundColor = Assets.Palette[Assets.Maps[mapNumber].Border];
            AddChild(ARVROrigin = new ARVROrigin());
            ARVROrigin.AddChild(ARVRCamera = new ARVRCamera()
            {
                Current = true,
            });
            ARVROrigin.AddChild(LeftController = new ARVRController()
            {
                ControllerId = 1,
            });
            ARVROrigin.AddChild(RightController = new ARVRController()
            {
                ControllerId = 2,
            });
            if (Assets.LoadingPic is ImageTexture pic && pic != null)
            {
                ARVRCamera.AddChild(new MeshInstance()
                {
                    Mesh = new QuadMesh()
                    {
                        Size = new Vector2(pic.GetWidth() * Assets.PixelWidth, pic.GetHeight() * Assets.PixelHeight),
                    },
                    MaterialOverride = new SpatialMaterial()
                    {
                        AlbedoTexture = pic,
                        FlagsUnshaded = true,
                        FlagsDoNotReceiveShadows = true,
                        FlagsDisableAmbientLight = true,
                        FlagsTransparent = false,
                        ParamsCullMode = SpatialMaterial.CullMode.Back,
                        ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
                    },
                    Transform = new Transform(Basis.Identity, Vector3.Forward * pic.GetWidth() * Assets.PixelWidth),
                });
                SoundBlaster.Song = Assets.AudioT.Songs[Assets.Maps[MapNumber].Song];

                System.Threading.Thread thread = new System.Threading.Thread(new ThreadStart(ThreadProc));
                thread.IsBackground = true;
                thread.Start();
            }
        }

        public ushort MapNumber { get; set; }

        public void ThreadProc()
        {
            Main.Scene = Main.ActionRoom = new ActionRoom()
            {
                MapNumber = MapNumber,
            };
        }
    }
}
