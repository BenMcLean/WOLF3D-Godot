using Godot;
using NScumm.Core.Audio.OPL;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    /// <summary>
    /// Plays Adlib sound effects in Godot.
    /// </summary>
    public class AdlPlayer : Node
    {
        public IOpl Opl
        {
            get => opl;
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
            get => adl;
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

        public override void _Process(float delta)
        {
            base._Process(delta);
            PlayNotes(delta);
        }

        public bool Note
        {
            get => note;
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
            for (int i = 0; i < Adl.InstrumentPorts.Count; i++)
                Opl.WriteReg(Adl.InstrumentPorts[i], Adl.Instrument[i]);
            Opl.WriteReg(0xC0, 0); // WOLF3D's code ignores this value in its sound data, always setting it to zero instead.
            return this;
        }

        public AdlPlayer PlayNotes(float delta)
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
