using Godot;
using WOLF3DSim;

namespace WOLF3D
{
    /// <summary>
    /// Assets takes the bytes extracted from VSwap and creates the corresponding Godot objects for them to be used throughout the game.
    /// </summary>
    public class Assets
    {
        //Tom Hall's Doom Bible and also tweets from John Carmack state that the walls in Wolfenstein 3D were always eight feet thick. The wall textures are 64x64 pixels, which means that the ratio is 8 pixels per foot.
        //However, VR uses the metric system, where 1 game unit is 1 meter in real space. One foot equals 0.3048 meters.
        //Now unless I am a complete failure at basic math (quite possible) this means that to scale Wolfenstein 3D correctly in VR, one pixel must equal 0.0381 in game units, and a Wolfenstein 3D wall must be 2.4384 game units thick.
        public static readonly float PixelSize = 0.0381f;
        public static readonly float WallSize = 2.4384f;

        public VSwap VSwap { get; set; }
        public ImageTexture[] Textures { get; set; }

        public Assets(VSwap vswap)
        {
            Load(vswap);
        }

        public Assets Load(VSwap vswap)
        {
            VSwap = vswap;
            Textures = new ImageTexture[VSwap.SoundPage];
            for (uint i = 0; i < Textures.Length; i++)
                if (VSwap.Pages[i] != null)
                {
                    Godot.Image image = new Image(); ;
                    image.CreateFromData(64, 64, false, Image.Format.Rgba8, VSwap.Pages[i]);
                    Textures[i] = new ImageTexture();
                    Textures[i].CreateFromImage(image, (int)Texture.FlagsEnum.ConvertToLinear);
                }
            return this;
        }

        public static readonly SpatialMaterial WallMaterial = new SpatialMaterial()
        {
            FlagsUnshaded = true,
            FlagsDoNotReceiveShadows = true,
            FlagsDisableAmbientLight = true,
            ParamsSpecularMode = SpatialMaterial.SpecularMode.Disabled,
            ParamsCullMode = SpatialMaterial.CullMode.Disabled
        };
    }
}
