using NScumm.Core.Audio.OPL;

namespace WOLF3D.WOLF3DGame.OPL
{
    public interface IAdlibPlayer
    {
        float UntilNextUpdate { get; }
        void Init(IOpl opl);
        bool Update(IOpl opl);
        void Silence(IOpl opl);
    }
}
