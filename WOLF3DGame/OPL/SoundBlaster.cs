using NScumm.Audio.OPL.Woody;
using NScumm.Core.Audio.OPL;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    public static class SoundBlaster
    {
        public static OplPlayer OplPlayer { get; set; } = new OplPlayer()
        {
            Opl = new WoodyEmulatorOpl(OplType.Opl2),
        };

        public static void NewOPL()
        {
            OplPlayer.Opl = new WoodyEmulatorOpl(OplType.Opl2);
        }

        public static readonly ConcurrentQueue<object> SoundMessages = new ConcurrentQueue<object>();

        public static Imf[] Song
        {
            get => OplPlayer.ImfPlayer.Song;
            set
            {
                if (value == null)
                {
                    SoundMessages.Enqueue(SoundMessage.STOP_MUSIC);
                    MusicOff();
                }
                else
                    SoundMessages.Enqueue(value);
            }
        }

        public static Adl Adl
        {
            get => OplPlayer.AdlPlayer.Adl;
            set
            {
                if (value == null)
                    SoundMessages.Enqueue(SoundMessage.STOP_SFX);
                else
                    SoundMessages.Enqueue(value);
            }
        }

        public enum SoundMessage
        {
            STOP_MUSIC, STOP_SFX, QUIT
        }

        public static System.Threading.Thread Thread { get; set; } = null;

        public static void Start()
        {
            /*
            if (Thread == null)
            {
                Thread = new System.Threading.Thread(new ThreadStart(ThreadProc));
                Thread.Start();
            }
            */
        }

        public static void Stop()
        {
            SoundMessages.Enqueue(SoundMessage.QUIT);
            Thread.Join();
            Thread = null;
        }

        private static void ThreadProc()
        {
            bool quit = false;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!quit)
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
                            case SoundMessage.QUIT:
                                quit = true;
                                break;
                        }
                Thread.Sleep(1);
                stopwatch.Stop();
                OplPlayer.ImfPlayer.PlaySeconds(stopwatch.ElapsedMilliseconds * Imf.Hz);
                stopwatch.Restart();
                OplPlayer.FillBuffer();
            }
            stopwatch.Stop();
        }

        public static void PlayNotes(float delta)
        {
            //OplPlayer.ImfPlayer.PlayNotes(delta);
            OplPlayer.AdlPlayer.PlayNotes(delta);
        }

        public static void MusicOff() => OplPlayer?.ImfPlayer?.MusicOff();
    }
}
