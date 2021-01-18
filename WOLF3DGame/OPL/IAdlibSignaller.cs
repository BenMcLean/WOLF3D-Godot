using NScumm.Core.Audio.OPL;

namespace WOLF3D.WOLF3DGame.OPL
{
    public interface IAdlibSignaller
    {
        void Init(IOpl opl);
        /// <returns>The number of 700 Hz intervals to wait until calling Update again</returns>
        uint Update(IOpl opl);
        void Silence(IOpl opl);
    }
}
