using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOLF3D.WOLF3DGame
{
    public static class Settings
    {
        public static byte Episode { get; set; } = 0;
        public static byte Difficulty { get; set; } = 0;

        public enum VRModeEnum
        {
            ROOMSCALE, FIVEDOF
        }

        public static VRModeEnum VRMode = VRModeEnum.ROOMSCALE;
        public static bool Roomscale => VRMode == VRModeEnum.ROOMSCALE;
        public static bool FiveDOF => VRMode == VRModeEnum.FIVEDOF;
    }
}
