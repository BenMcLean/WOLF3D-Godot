using NScumm.Core.Audio.OPL;
using System.Collections.Concurrent;
using WOLF3DModel;

namespace WOLF3D.WOLF3DGame.OPL
{
    /// <summary>
    /// id Software Adlib Sound Effect Player by Ben McLean mclean.ben@gmail.com
    /// </summary>
    public class IdAdlPlayer : IAdlibPlayer
    {
        public void Init(IOpl opl) => opl?.WriteReg(1, 32); // go to OPL2 mode
        public float RefreshRate => 140f; // These sound effects play back at 140 Hz.
        public void Silence(IOpl opl) => SetNote(false, opl);
        public bool Note { get; private set; }
        public IdAdlPlayer SetNote(bool value, IOpl opl)
        {
            if (Note = value)
                opl?.WriteReg(Adl.OctavePort, (byte)(Adl.Block | Adl.KeyFlag));
            else
                opl?.WriteReg(Adl.OctavePort, 0);
            return this;
        }
        public IdAdlPlayer SetInstrument(IOpl opl)
        {
            opl.WriteReg(1, 32); // go to OPL2 mode
            for (int i = 0; i < Adl.InstrumentPorts.Count; i++)
                opl?.WriteReg(Adl.InstrumentPorts[i], Adl.Instrument[i]);
            opl?.WriteReg(0xC0, 0); // WOLF3D's code ignores this value in its sound data, always setting it to zero instead.
            return this;
        }
        public static readonly ConcurrentQueue<Adl> IdAdlQueue = new ConcurrentQueue<Adl>();
        public bool Update(IOpl opl)
        {
            if (IdAdlQueue.TryDequeue(out Adl adl)
                && (Adl == null || adl == null || Adl == adl || adl.Priority >= Adl.Priority))
            {
                CurrentNote = 0;
                if (opl != null)
                {
                    SetNote(false, opl); // Must send a signal to stop the previous sound before starting a new sound
                    if ((Adl = adl) != null)
                    {
                        SetInstrument(opl);
                        SetNote(true, opl);
                    }
                }
            }
            if (Adl != null)
            {
                if (Adl.Notes[CurrentNote] == 0)
                    SetNote(false, opl);
                else
                {
                    if (!Note) SetNote(true, opl);
                    opl?.WriteReg(Adl.NotePort, Adl.Notes[CurrentNote]);
                }
                CurrentNote++;
                if (CurrentNote >= Adl.Notes.Length)
                {
                    Adl = null;
                    SetNote(false, opl);
                    return false;
                }
                return true;
            }
            return false;
        }
        public uint CurrentNote = 0;
        public Adl Adl { get; private set; } = null;
    }
}
