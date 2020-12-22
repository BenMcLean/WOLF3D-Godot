using NScumm.Core.Audio.OPL;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    /// <summary>
    /// id Software Adlib Sound Effect Player by Ben McLean mclean.ben@gmail.com
    /// </summary>
    public class IdAdlPlayer : IMusicPlayer
    {
        public IOpl Opl { get; set; } = null;

        public float RefreshRate => 140f; // These sound effects play back at 140 Hz.

        public bool Note
        {
            get => note;
            set
            {
                if (note = value)
                    Opl?.WriteReg(Adl.OctavePort, (byte)(Adl.Block | Adl.KeyFlag));
                else
                    Opl?.WriteReg(Adl.OctavePort, 0);
            }
        }
        private bool note = false;

        public IdAdlPlayer SetInstrument()
        {
            for (int i = 0; i < Adl.InstrumentPorts.Count; i++)
                Opl?.WriteReg(Adl.InstrumentPorts[i], Adl.Instrument[i]);
            Opl?.WriteReg(0xC0, 0); // WOLF3D's code ignores this value in its sound data, always setting it to zero instead.
            return this;
        }

        public bool Update()
        {
            if (Adl != null)
            {
                if (Adl.Notes[CurrentNote] == 0)
                    Note = false;
                else
                {
                    if (!Note) Note = true;
                    Opl?.WriteReg(Adl.NotePort, Adl.Notes[CurrentNote]);
                }
                CurrentNote++;
                if (CurrentNote >= Adl.Notes.Length)
                {
                    Adl = null;
                    return false;
                }
                return true;
            }
            return false;
        }

        public uint CurrentNote = 0;

        public Adl Adl
        {
            get => adl;
            set
            {
                if (adl == null || value == null || adl == value || value.Priority >= adl.Priority)
                {
                    CurrentNote = 0;
                    if (Opl != null)
                    {
                        Note = false; // Must send a signal to stop the previous sound before starting a new sound
                        if ((adl = value) != null)
                        {
                            SetInstrument();
                            Note = true;
                        }
                    }
                }
            }
        }
        private Adl adl;
    }
}
