using NScumm.Core.Audio.OPL;

namespace WOLF3D.WOLF3DGame.OPL
{
    public interface IAdlibSignaller
    {
        uint IntervalsOf700HzToWait { get; }
        void Init(IOpl opl);
        bool Update(IOpl opl);
        void Silence(IOpl opl);
    }
}
