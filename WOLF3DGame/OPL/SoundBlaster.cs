using System;
using System.Collections.Concurrent;
using System.Xml.Linq;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    public static class SoundBlaster
    {
        #region SoundMessages
        public static readonly ConcurrentQueue<object> SoundMessages = new ConcurrentQueue<object>();

        public static AudioT.Song Song
        {
            get => throw new NotImplementedException();
            set
            {
                if (value == null)
                {
                    SoundMessages.Enqueue(SoundMessage.STOP_MUSIC);
                }
                else
                    SoundMessages.Enqueue(value);
            }
        }

        public static Adl Adl
        {
            get => throw new NotImplementedException();
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

        public static void Play(XElement xml)
        {
            if (xml?.Attribute("Sound")?.Value is string sound && !string.IsNullOrWhiteSpace(sound) && Assets.Sound(sound) is Adl adl && adl != null)
                Adl = adl;
        }
        #endregion SoundMessages
    }
}
