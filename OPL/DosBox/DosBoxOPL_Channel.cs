//
//  DosBoxOPL_Channel.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace NScumm.Core.Audio.OPL.DosBox
{
    partial class DosBoxOPL
    {
        class Channel
        {
            //Shifts for the values contained in chandata variable
            public const int ShiftKslBase = 16;
            public const int ShiftKeyCode = 24;

            public Chip Chip { get; private set; }

            public int ChannelNum { get; private set; }

            public Operator[] Ops { get; private set; }

            public SynthMode SynthMode { get; set; }

            public byte FourMask { get; set; }

            public void WriteA0(Chip chip, byte val)
            {
                byte fourOp = (byte)(chip.Reg104 & chip.Opl3Active & FourMask);
                //Don't handle writes to silent fourop channels
                if (fourOp > 0x80)
                    return;
                int change = (chanData ^ val) & 0xff;
                if (change != 0)
                {
                    chanData ^= change;
                    UpdateFrequency(chip, fourOp);
                }
            }

            public void WriteB0(Chip chip, byte val)
            {
                byte fourOp = (byte)(chip.Reg104 & chip.Opl3Active & FourMask);
                //Don't handle writes to silent fourop channels
                if (fourOp > 0x80)
                    return;
                int change = ((chanData ^ (val << 8)) & 0x1f00);
                if (change != 0)
                {
                    chanData ^= change;
                    UpdateFrequency(chip, fourOp);
                }
                //Check for a change in the keyon/off state
                if (0 == ((val ^ regB0) & 0x20))
                    return;
                regB0 = val;
                if ((val & 0x20) != 0)
                {
                    Op(0).KeyOn(0x1);
                    Op(1).KeyOn(0x1);
                    if ((fourOp & 0x3f) != 0)
                    {
                        Chip.Channels[ChannelNum + 1].Op(0).KeyOn(1);
                        Chip.Channels[ChannelNum + 1].Op(1).KeyOn(1);
                    }
                }
                else
                {
                    Op(0).KeyOff(0x1);
                    Op(1).KeyOff(0x1);
                    if ((fourOp & 0x3f) != 0)
                    {
                        Chip.Channels[ChannelNum + 1].Op(0).KeyOff(1);
                        Chip.Channels[ChannelNum + 1].Op(1).KeyOff(1);
                    }
                }
            }

            public void WriteC0(Chip chip, byte val)
            {
                byte change = (byte)(val ^ regC0);
                if (change == 0)
                    return;
                regC0 = val;
                feedback = (byte)((regC0 >> 1) & 7);
                if (feedback != 0)
                {
                    //We shift the input to the right 10 bit wave index value
                    feedback = (byte)(9 - feedback);
                }
                else
                {
                    feedback = 31;
                }
                UpdateSynth(chip);
            }

            public void UpdateSynth(Chip chip)
            {
                //Select the new synth mode
                if (chip.Opl3Active != 0)
                {
                    //4-op mode enabled for this channel
                    if (((chip.Reg104 & FourMask) & 0x3f) != 0)
                    {
                        Channel chan0, chan1;
                        //Check if it's the 2nd channel in a 4-op
                        if (0 == (FourMask & 0x80))
                        {
                            chan0 = this;
                            chan1 = Chip.Channels[ChannelNum + 1];
                        }
                        else
                        {
                            chan0 = Chip.Channels[ChannelNum - 1];
                            chan1 = this;
                        }

                        byte synth = (byte)(((chan0.regC0 & 1) << 0) | ((chan1.regC0 & 1) << 1));
                        switch (synth)
                        {
                            case 0:
                                chan0.SynthMode = SynthMode.Sm3FMFM;
                                break;
                            case 1:
                                chan0.SynthMode = SynthMode.Sm3AMFM;
                                break;
                            case 2:
                                chan0.SynthMode = SynthMode.Sm3FMAM;
                                break;
                            case 3:
                                chan0.SynthMode = SynthMode.Sm3AMAM;
                                break;
                        }
                        //Disable updating percussion channels
                    }
                    else if (((FourMask & 0x40) != 0) && ((chip.RegBD & 0x20) != 0))
                    {

                        //Regular dual op, am or fm
                    }
                    else if ((regC0 & 1) != 0)
                    {
                        SynthMode = SynthMode.Sm3AM;
                    }
                    else
                    {
                        SynthMode = SynthMode.Sm3FM;
                    }
                    maskLeft = (sbyte)((regC0 & 0x10) != 0 ? -1 : 0);
                    maskRight = (sbyte)((regC0 & 0x20) != 0 ? -1 : 0);
                    //opl2 active
                }
                else
                {
                    //Disable updating percussion channels
                    if (((FourMask & 0x40) != 0) && ((chip.RegBD & 0x20) != 0))
                    {
                        //Regular dual op, am or fm
                    }
                    else if ((regC0 & 1) != 0)
                    {
                        SynthMode = SynthMode.Sm2AM;
                    }
                    else
                    {
                        SynthMode = SynthMode.Sm2FM;
                    }
                }
            }

            public Channel SynthHandler(Chip chip, int samples, short[] output, int pos)
            {
                return BlockTemplate(SynthMode, chip, samples, output, pos);
            }

            public Channel(Chip chip, int index)
            {
                Chip = chip;
                ChannelNum = index;

                old = new short[2];
                Ops = new Operator[2];
                for (int i = 0; i < Ops.Length; i++)
                {
                    Ops[i] = new Operator();
                }

                maskLeft = -1;
                maskRight = -1;
                feedback = 31;
                SynthMode = SynthMode.Sm2FM;
            }

            /// <summary>
            /// Generate blocks of data in specific modes.
            /// </summary>
            /// <returns>The template.</returns>
            /// <param name="mode">Mode.</param>
            /// <param name="chip">Chip.</param>
            /// <param name="samples">Samples.</param>
            /// <param name="output">Output.</param>
            /// <param name="pos">Position.</param>
            Channel BlockTemplate(SynthMode mode, Chip chip, int samples, short[] output, int pos)
            {
                switch (mode)
                {
                    case SynthMode.Sm2AM:
                    case SynthMode.Sm3AM:
                        if (Op(0).Silent() && Op(1).Silent())
                        {
                            old[0] = old[1] = 0;
                            return Chip.Channels[ChannelNum + 1];
                        }
                        break;
                    case SynthMode.Sm2FM:
                    case SynthMode.Sm3FM:
                        if (Op(1).Silent())
                        {
                            old[0] = old[1] = 0;
                            return Chip.Channels[ChannelNum + 1];
                        }
                        break;
                    case SynthMode.Sm3FMFM:
                        if (Op(3).Silent())
                        {
                            old[0] = old[1] = 0;
                            return Chip.Channels[ChannelNum + 2];
                        }
                        break;
                    case SynthMode.Sm3AMFM:
                        if (Op(0).Silent() && Op(3).Silent())
                        {
                            old[0] = old[1] = 0;
                            return Chip.Channels[ChannelNum + 2];
                        }
                        break;
                    case SynthMode.Sm3FMAM:
                        if (Op(1).Silent() && Op(3).Silent())
                        {
                            old[0] = old[1] = 0;
                            return Chip.Channels[ChannelNum + 2];
                        }
                        break;
                    case SynthMode.Sm3AMAM:
                        if (Op(0).Silent() && Op(2).Silent() && Op(3).Silent())
                        {
                            old[0] = old[1] = 0;
                            return Chip.Channels[ChannelNum + 2];
                        }
                        break;
                    case SynthMode.Sm2Percussion:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                    case SynthMode.Sm3Percussion:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                    case SynthMode.Sm4Start:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                    case SynthMode.Sm6Start:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                }
                //Init the operators with the the current vibrato and tremolo values
                Op(0).Prepare(chip);
                Op(1).Prepare(chip);
                if (mode > SynthMode.Sm4Start)
                {
                    Op(2).Prepare(chip);
                    Op(3).Prepare(chip);
                }
                if (mode > SynthMode.Sm6Start)
                {
                    Op(4).Prepare(chip);
                    Op(5).Prepare(chip);
                }
                for (int i = 0; i < samples; i++)
                {
                    //Early out for percussion handlers
                    if (mode == SynthMode.Sm2Percussion)
                    {
                        GeneratePercussion(false, chip, output, pos + i);
                        continue;   //Prevent some unitialized value bitching
                    }
                    if (mode == SynthMode.Sm3Percussion)
                    {
                        GeneratePercussion(true, chip, output, pos + i * 2);
                        continue;   //Prevent some unitialized value bitching
                    }

                    //Do unsigned shift so we can shift out all bits but still stay in 10 bit range otherwise
                    int mod = (old[0] + old[1]) >> feedback;
                    old[0] = old[1];
                    old[1] = Op(0).GetSample(mod);
                    short sample = 0;
                    short out0 = old[0];
                    if (mode == SynthMode.Sm2AM || mode == SynthMode.Sm3AM)
                    {
                        sample = (short)(out0 + Op(1).GetSample(0));
                    }
                    else if (mode == SynthMode.Sm2FM || mode == SynthMode.Sm3FM)
                    {
                        sample = Op(1).GetSample(out0);
                    }
                    else if (mode == SynthMode.Sm3FMFM)
                    {
                        int next = Op(1).GetSample(out0);
                        next = Op(2).GetSample(next);
                        sample = Op(3).GetSample(next);
                    }
                    else if (mode == SynthMode.Sm3AMFM)
                    {
                        sample = out0;
                        int next = Op(1).GetSample(0);
                        next = Op(2).GetSample(next);
                        sample += Op(3).GetSample(next);
                    }
                    else if (mode == SynthMode.Sm3FMAM)
                    {
                        sample = Op(1).GetSample(out0);
                        int next = Op(2).GetSample(0);
                        sample += Op(3).GetSample(next);
                    }
                    else if (mode == SynthMode.Sm3AMAM)
                    {
                        sample = out0;
                        int next = Op(1).GetSample(0);
                        sample += Op(2).GetSample(next);
                        sample += Op(3).GetSample(0);
                    }
                    switch (mode)
                    {
                        case SynthMode.Sm2AM:
                        case SynthMode.Sm2FM:
                            output[pos + i] += sample;
                            break;
                        case SynthMode.Sm3AM:
                        case SynthMode.Sm3FM:
                        case SynthMode.Sm3FMFM:
                        case SynthMode.Sm3AMFM:
                        case SynthMode.Sm3FMAM:
                        case SynthMode.Sm3AMAM:
                            output[pos + i * 2 + 0] += (short)(sample & maskLeft);
                            output[pos + i * 2 + 1] += (short)(sample & maskRight);
                            break;
                        case SynthMode.Sm2Percussion:
                                // This case was not handled in the DOSBox code either
                                // thus we leave this blank.
                                // TODO: Consider checking this.
                            break;
                        case SynthMode.Sm3Percussion:
                                // This case was not handled in the DOSBox code either
                                // thus we leave this blank.
                                // TODO: Consider checking this.
                            break;
                        case SynthMode.Sm4Start:
                                // This case was not handled in the DOSBox code either
                                // thus we leave this blank.
                                // TODO: Consider checking this.
                            break;
                        case SynthMode.Sm6Start:
                                // This case was not handled in the DOSBox code either
                                // thus we leave this blank.
                                // TODO: Consider checking this.
                            break;
                    }
                }
                switch (mode)
                {
                    case SynthMode.Sm2AM:
                    case SynthMode.Sm2FM:
                    case SynthMode.Sm3AM:
                    case SynthMode.Sm3FM:
                        return Chip.Channels[ChannelNum + 1];
                    case SynthMode.Sm3FMFM:
                    case SynthMode.Sm3AMFM:
                    case SynthMode.Sm3FMAM:
                    case SynthMode.Sm3AMAM:
                        return Chip.Channels[ChannelNum + 2];
                    case SynthMode.Sm2Percussion:
                    case SynthMode.Sm3Percussion:
                        return Chip.Channels[ChannelNum + 3];
                    case SynthMode.Sm4Start:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                    case SynthMode.Sm6Start:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                }
                return null;
            }

            Operator Op(int index)
            {
                return Chip.Channels[ChannelNum + (index >> 1)].Ops[index & 1];
            }

            /// <summary>
            /// Forward the channel data to the operators of the channel.
            /// </summary>
            /// <param name="chip">Chip.</param>
            /// <param name="data">Data.</param>
            void SetChanData(Chip chip, int data)
            {
                int change = chanData ^ data;
                chanData = data;
                Op(0).ChanData = data;
                Op(1).ChanData = data;
                //Since a frequency update triggered this, always update frequency
                Op(0).UpdateFrequency();
                Op(1).UpdateFrequency();
                if ((change & (0xff << ShiftKslBase)) != 0)
                {
                    Op(0).UpdateAttenuation();
                    Op(1).UpdateAttenuation();
                }
                if ((change & (0xff << ShiftKeyCode)) != 0)
                {
                    Op(0).UpdateRates(chip);
                    Op(1).UpdateRates(chip);
                }
            }

            /// <summary>
            /// Change in the chandata, check for new values and if we have to forward to operators.
            /// </summary>
            /// <param name="chip">Chip.</param>
            /// <param name="fourOp">Four op.</param>
            void UpdateFrequency(Chip chip, byte fourOp)
            {
                //Extrace the frequency bits
                int data = chanData & 0xffff;
                int kslBase = kslTable[data >> 6];
                int keyCode = (data & 0x1c00) >> 9;
                if ((chip.Reg08 & 0x40) != 0)
                {
                    keyCode |= (data & 0x100) >> 8;  /* notesel == 1 */
                }
                else
                {
                    keyCode |= (data & 0x200) >> 9;  /* notesel == 0 */
                }
                //Add the keycode and ksl into the highest bits of chanData
                data |= (keyCode << ShiftKeyCode) | (kslBase << ShiftKslBase);
                SetChanData(chip, data);
                if ((fourOp & 0x3f) != 0)
                {
                    Chip.Channels[ChannelNum + 1].SetChanData(chip, data);
                }
            }

            // call this for the first channel
            void GeneratePercussion(bool opl3Mode, Chip chip, short[] output, int pos)
            {
                Channel chan = this;

                //BassDrum
                int mod = (old[0] + old[1]) >> feedback;
                old[0] = old[1];
                old[1] = Op(0).GetSample(mod);

                //When bassdrum is in AM mode first operator is ignored
                if ((chan.regC0 & 1) != 0)
                {
                    mod = 0;
                }
                else
                {
                    mod = old[0];
                }
                short sample = Op(1).GetSample(mod);


                //Precalculate stuff used by other outputs
                int noiseBit = chip.ForwardNoise() & 0x1;
                int c2 = Op(2).ForwardWave();
                int c5 = Op(5).ForwardWave();
                int phaseBit = (((c2 & 0x88) ^ ((c2 << 5) & 0x80)) | ((c5 ^ (c5 << 2)) & 0x20)) != 0 ? 0x02 : 0x00;

                //Hi-Hat
                uint hhVol = Op(2).ForwardVolume();
                if (!EnvSilent((int)hhVol))
                {
                    var hhIndex = ((phaseBit << 8) | (0x34 << (byte)(phaseBit ^ (noiseBit << 1))));
                    sample += Op(2).GetWave(hhIndex, hhVol);
                }
                //Snare Drum
                uint sdVol = Op(3).ForwardVolume();
                if (!EnvSilent((int)sdVol))
                {
                    int sdIndex = (int)((0x100 + (c2 & 0x100)) ^ (noiseBit << 8));
                    sample += Op(3).GetWave(sdIndex, sdVol);
                }
                //Tom-tom
                sample += Op(4).GetSample(0);

                //Top-Cymbal
                uint tcVol = Op(5).ForwardVolume();
                if (!EnvSilent((int)tcVol))
                {
                    var tcIndex = ((1 + phaseBit) << 8);
                    sample += Op(5).GetWave(tcIndex, tcVol);
                }
                sample <<= 1;
                if (opl3Mode)
                {
                    output[pos] += sample;
                    output[pos + 1] += sample;
                }
                else
                {
                    output[pos] += sample;
                }
            }

            /// <summary>
            /// Frequency/octave and derived values.
            /// </summary>
            int chanData;

            /// <summary>
            /// Old data for feedback.
            /// </summary>
            readonly short[] old;

            /// <summary>
            /// Feedback shift.
            /// </summary>
            byte feedback;

            /// <summary>
            /// Register values to check for changes.
            /// </summary>
            byte regB0;

            /// <summary>
            /// This should correspond with reg104, bit 6 indicates a Percussion channel, bit 7 indicates a silent channel
            /// </summary>
            byte regC0;

            /// <summary>
            /// Sign extended values for both channel's panning.
            /// </summary>
            sbyte maskLeft;

            /// <summary>
            /// Sign extended values for both channel's panning
            /// </summary>
            sbyte maskRight;
        }
    }
}

