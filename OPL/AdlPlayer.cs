using Godot;
using NScumm.Core.Audio.OPL;

namespace OPL
{
    /// <summary>
    /// Plays Adlib sound effects in Godot.
    /// </summary>
    public class AdlPlayer : Node
    {
        public IOpl Opl
        {
            get { return opl; }
            set
            {
                opl = value;
                if (Opl != null) Opl.WriteReg(1, 0x20); // Enable different wave type selections
            }
        }
        private IOpl opl;

        public bool Mute { get; set; } = false;
        public uint CurrentNote = 0;
        private float SinceLastNote = 0f;

        public Adl Adl
        {
            get { return adl; }
            set
            {
                if (!Mute && (adl == null || value == null || value.Priority >= adl.Priority))
                {
                    SinceLastNote = 0f;
                    CurrentNote = 0;
                    if (Opl != null)
                    {
                        if ((adl = value) != null)
                            SetInstrument().PlayNote();
                        else
                            Note = false;
                    }
                }
            }
        }
        private Adl adl;

        public override void _PhysicsProcess(float delta)
        {
            if (Opl != null && Adl != null)
            {
                SinceLastNote += delta;
                while (Adl != null && SinceLastNote >= Adl.Hz)
                {
                    SinceLastNote -= Adl.Hz;
                    PlayNote();
                }
            }
        }

        public bool Note
        {
            get { return note; }
            set
            {
                if (note = value)
                    Opl.WriteReg(Adl.OctavePort, (byte)(Adl.Block | Adl.KeyFlag));
                else
                    Opl.WriteReg(Adl.OctavePort, 0);
            }
        }
        private bool note = false;

        public AdlPlayer SetInstrument()
        {
            for (uint i = 0; i < Adl.InstrumentPorts.Length; i++)
                Opl.WriteReg(Adl.InstrumentPorts[i], Adl.Instrument[i]);
            Opl.WriteReg(0xC0, 0); // Wolf3D's code ignores this value in its sound data, always setting it to zero instead.
            return this;
        }

        public AdlPlayer PlayNote()
        {
            if (Adl.Notes[CurrentNote] == 0)
                Note = false;
            else
            {
                if (!Note) Note = true;
                Opl.WriteReg(Adl.NotePort, Adl.Notes[CurrentNote]);
            }
            CurrentNote++;
            if (CurrentNote >= Adl.Notes.Length)
                Adl = null;
            return this;
        }
    }
}
