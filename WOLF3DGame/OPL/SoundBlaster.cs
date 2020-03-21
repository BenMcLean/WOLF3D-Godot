using NScumm.Audio.OPL.Woody;
using NScumm.Core.Audio.OPL;
using System.Collections.Concurrent;
using System.Threading;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    public static class SoundBlaster
    {
        public static OplPlayer OplPlayer { get; set; } = new OplPlayer()
        {
            Opl = new WoodyEmulatorOpl(OplType.Opl3),
        };

        private static readonly ConcurrentQueue<object> SoundMessages = new ConcurrentQueue<object>();

        public static Imf[] Song
        {
            get => OplPlayer.ImfPlayer.Song;
            set => SoundMessages.Enqueue(value);
        }

        public static Adl Adl
        {
            get => OplPlayer.AdlPlayer.Adl;
            set => SoundMessages.Enqueue(value);
        }

        private enum SoundMessage
        {
            STOP_MUSIC, STOP_SFX, QUIT
        }

        public static void PlayNotes(float delta)
        {
            while (SoundMessages.TryDequeue(out object soundMessage))
                if (soundMessage is Imf[] imf)
                    OplPlayer.ImfPlayer.Song = imf;
                else if (soundMessage is Adl adl)
                    OplPlayer.AdlPlayer.Adl = adl;
                else if (soundMessage is SoundMessage message)
                    switch (message)
                    {
                        case SoundMessage.STOP_MUSIC:
                            OplPlayer.ImfPlayer.Song = null;
                            break;
                        case SoundMessage.STOP_SFX:
                            OplPlayer.AdlPlayer.Adl = null;
                            break;
                    }
            OplPlayer.ImfPlayer.PlayNotes(delta);
            OplPlayer.AdlPlayer.PlayNotes(delta);
        }
    }
}
