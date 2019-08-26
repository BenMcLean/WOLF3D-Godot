using Godot;
using WOLF3DSim;

namespace WOLF3D
{
    /// <summary>
    /// Assets takes the bytes extracted from VSwap and creates the corresponding Godot objects for them to be used throughout the game.
    /// </summary>
    public class Assets
    {
        public VSwap VSwap { get; set; }
        public ImageTexture[] Graphics { get; set; }

        public Assets(VSwap vswap)
        {
            Load(vswap);
        }

        public Assets Load(VSwap vswap)
        {
            VSwap = vswap;
            Graphics = new ImageTexture[VSwap.SoundPage];
            for (uint i = 0; i < Graphics.Length; i++)
                if (VSwap.Pages[i] != null)
                {
                    Godot.Image image = new Image(); ;
                    image.CreateFromData(64, 64, false, Image.Format.Rgba8, VSwap.Pages[i]);
                    Graphics[i] = new ImageTexture();
                    Graphics[i].CreateFromImage(image, (int)Texture.FlagsEnum.ConvertToLinear);
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
