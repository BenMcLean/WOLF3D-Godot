//
//  DosBoxOPL_Chip.cs
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
using System;

namespace NScumm.Core.Audio.OPL.DosBox
{
    partial class DosBoxOPL
    {
        class Chip
        {
            /// <summary>
            /// Gets the frequency scales for the different multiplications.
            /// </summary>
            /// <value>The freq mul.</value>
            public uint[] FreqMul { get { return freqMul; } }

            /// <summary>
            /// Gets the rates for decay and release for rate of this chip.
            /// </summary>
            /// <value>The linear rates.</value>
            public uint[] LinearRates { get { return linearRates; } }

            /// <summary>
            /// Gets the best match attack rates for the rate of this chip.
            /// </summary>
            /// <value>The attack rates.</value>
            public uint[] AttackRates { get { return attackRates; } }

            public Channel[] Channels { get { return chan; } }

            public byte Reg104 { get { return reg104; } }

            public byte Reg08 { get { return reg08; } }

            public byte RegBD  { get { return regBD; } }

            public sbyte VibratoSign { get { return vibratoSign; } }

            public byte VibratoShift { get { return vibratoShift; } }

            public byte TremoloValue { get { return tremoloValue; } }

            public byte WaveFormMask { get { return waveFormMask; } }

            public sbyte Opl3Active { get { return opl3Active; } }

            public Chip()
            {
                chan = new Channel[18];
                for (int i = 0; i < chan.Length; i++)
                {
                    chan[i] = new Channel(this, i);
                }
            }

            public int ForwardNoise()
            {
                noiseCounter += noiseAdd;
                int count = noiseCounter >> LfoShift;
                noiseCounter &= WaveMask;
                for (; count > 0; --count)
                {
                    //Noise calculation from mame
                    noiseValue ^= (0x800302) & (0 - (noiseValue & 1));
                    noiseValue >>= 1;
                }
                return noiseValue;
            }

            // Update the 0xc0 register for all channels to signal the switch to mono/stereo handlers
            private void UpdateSynths()
            {
                for (var i = 0; i < 18; i++)
                {
                    chan[i].UpdateSynth(this);
                }
            }

            public void WriteReg(uint reg, byte val)
            {
                switch ((reg & 0xf0) >> 4)
                {
                    case 0x00 >> 4:
                        if (reg == 0x01)
                        {
                            waveFormMask = (byte)(((val & 0x20) != 0) ? 0x7 : 0x0);
                        }
                        else if (reg == 0x104)
                        {
                            //Only detect changes in lowest 6 bits
                            if (0 == ((reg104 ^ val) & 0x3f))
                                return;
                            //Always keep the highest bit enabled, for checking > 0x80
                            reg104 = (byte)(0x80 | (val & 0x3f));
                            //Switch synths when changing the 4op combinations
                            UpdateSynths();
                        }
                        else if (reg == 0x105)
                        {
                            //MAME says the real opl3 doesn't reset anything on opl3 disable/enable till the next write in another register
                            if (0 == ((opl3Active ^ val) & 1))
                                return;
                            opl3Active = (sbyte)(((val & 1) != 0) ? 0xff : 0);
                            //Just tupdate the synths now that opl3 most have been enabled
                            //This isn't how the real card handles it but need to switch to stereo generating handlers
                            UpdateSynths();
                        }
                        else if (reg == 0x08)
                        {
                            reg08 = val;
                        }
                        break;
                    case 0x10 >> 4:
                        break;
                    case 0x20 >> 4:
                    case 0x30 >> 4:
                        RegOp(reg, op => op.Write20(this, val));
                        break;
                    case 0x40 >> 4:
                    case 0x50 >> 4:
                        RegOp(reg, op => op.Write40(this, val));
                        break;
                    case 0x60 >> 4:
                    case 0x70 >> 4:
                        RegOp(reg, op => op.Write60(this, val));
                        break;
                    case 0x80 >> 4:
                    case 0x90 >> 4:
                        RegOp(reg, op => op.Write80(this, val));
                        break;
                    case 0xa0 >> 4:
                        RegChan(reg, ch => ch.WriteA0(this, val));
                        break;
                    case 0xb0 >> 4:
                        if (reg == 0xbd)
                        {
                            WriteBD(val);
                        }
                        else
                        {
                            RegChan(reg, ch => ch.WriteB0(this, val));
                        }
                        break;
                    case 0xc0 >> 4:
                        RegChan(reg, ch => ch.WriteC0(this, val));
                        break;
                    case 0xd0 >> 4:
                        break;
                    case 0xe0 >> 4:
                    case 0xf0 >> 4:
                        RegOp(reg, op => op.WriteE0(this, val));
                        break;
                }
            }

            public uint WriteAddr(uint port, byte val)
            {
                switch (port & 3)
                {
                    case 0:
                        return val;
                    case 2:
                        if (opl3Active != 0 || (val == 0x05))
                            return (uint)(0x100 | val);
                        else
                            return val;
                }
                return 0;
            }

            public void GenerateBlock2(int total, int pos, short[] output)
            {
                while (total > 0)
                {
                    int samples = ForwardLFO(total);
                    Array.Clear(output, pos, samples);
                    for (var ch = chan[0]; ch.ChannelNum < 9;)
                    {
                        ch = ch.SynthHandler(this, samples, output, pos);
                    }
                    total -= samples;
                    pos += samples;
                }
            }

            public void GenerateBlock3(int total, int pos, short[] output)
            {
                while (total > 0)
                {
                    int samples = ForwardLFO(total);
                    Array.Clear(output, pos, samples * 2);

                    for (var ch = chan[0]; ch.ChannelNum < 18;)
                    {
                        ch = ch.SynthHandler(this, samples, output, pos);
                    }
                    total -= samples;
                    pos += (samples * 2);
                }
            }

            public void Setup(int rate)
            {
                double scale = OPLRATE / (double)rate;

                //Noise counter is run at the same precision as general waves
                noiseAdd = (int)(0.5 + scale * (1 << LfoShift));
                noiseCounter = 0;
                noiseValue = 1; //Make sure it triggers the noise xor the first time
                //The low frequency oscillation counter
                //Every time his overflows vibrato and tremoloindex are increased
                lfoAdd = (int)(0.5 + scale * (1 << LfoShift));
                lfoCounter = 0;
                vibratoIndex = 0;
                tremoloIndex = 0;

                //With higher octave this gets shifted up
                //-1 since the freqCreateTable = *2
                #if WAVE_PRECISION
                double freqScale = ( 1 << 7 ) * scale * ( 1 << ( WAVE_SH - 1 - 10));
                for ( int i = 0; i < 16; i++ ) {
                freqMul[i] = (uint)( 0.5 + freqScale * FreqCreateTable[ i ] );
                }
                #else
                uint freqScale = (uint)(0.5 + scale * (1 << (WaveShift - 1 - 10)));
                for (int i = 0; i < 16; i++)
                {
                    freqMul[i] = freqScale * FreqCreateTable[i];
                }
                #endif

                //-3 since the real envelope takes 8 steps to reach the single value we supply
                for (byte i = 0; i < 76; i++)
                {
                    byte index, shift;
                    EnvelopeSelect(i, out index, out shift);
                    linearRates[i] = (uint)(scale * (EnvelopeIncreaseTable[index] << (RateShift + EnvExtra - shift - 3)));
                }
                //Generate the best matching attack rate
                for (byte i = 0; i < 62; i++)
                {
                    byte index, shift;
                    EnvelopeSelect(i, out index, out shift);
                    //Original amount of samples the attack would take
                    int original = (int)((AttackSamplesTable[index] << shift) / scale);

                    int guessAdd = (int)(scale * (EnvelopeIncreaseTable[index] << (RateShift - shift - 3)));
                    int bestAdd = guessAdd;
                    uint bestDiff = 1 << 30;
                    for (uint passes = 0; passes < 16; passes++)
                    {
                        int volume = EnvMax;
                        int samples = 0;
                        uint count = 0;
                        while (volume > 0 && samples < original * 2)
                        {
                            count = (uint)(count + guessAdd);
                            int change = (int)(count >> RateShift);
                            count &= RateMask;
                            if (change != 0)
                            { // less than 1 %
                                volume += (~volume * change) >> 3;
                            }
                            samples++;

                        }
                        int diff = original - samples;
                        uint lDiff = (uint)Math.Abs(diff);
                        //Init last on first pass
                        if (lDiff < bestDiff)
                        {
                            bestDiff = lDiff;
                            bestAdd = guessAdd;
                            //We hit an exactly matching sample count
                            if (bestDiff == 0)
                                break;
                        }
                        //Linear correction factor, not exactly perfect but seems to work
                        double correct = (original - diff) / (double)original;
                        guessAdd = (int)(guessAdd * correct);
                        //Below our target
                        if (diff < 0)
                        {
                            //Always add one here for rounding, an overshoot will get corrected by another pass decreasing
                            guessAdd++;
                        }
                    }
                    attackRates[i] = (uint)bestAdd;
                    //Keep track of the diffs for some debugging
                    //		attackDiffs[i] = bestDiff;
                }
                for (byte i = 62; i < 76; i++)
                {
                    //This should provide instant volume maximizing
                    attackRates[i] = 8 << RateShift;
                }
                //Setup the channels with the correct four op flags
                //Channels are accessed through a table so they appear linear here
                chan[0].FourMask = 0x00 | (1 << 0);
                chan[1].FourMask = 0x80 | (1 << 0);
                chan[2].FourMask = 0x00 | (1 << 1);
                chan[3].FourMask = 0x80 | (1 << 1);
                chan[4].FourMask = 0x00 | (1 << 2);
                chan[5].FourMask = 0x80 | (1 << 2);

                chan[9].FourMask = 0x00 | (1 << 3);
                chan[10].FourMask = 0x80 | (1 << 3);
                chan[11].FourMask = 0x00 | (1 << 4);
                chan[12].FourMask = 0x80 | (1 << 4);
                chan[13].FourMask = 0x00 | (1 << 5);
                chan[14].FourMask = 0x80 | (1 << 5);

                //mark the percussion channels
                chan[6].FourMask = 0x40;
                chan[7].FourMask = 0x40;
                chan[8].FourMask = 0x40;

                //Clear Everything in opl3 mode
                WriteReg(0x105, 0x1);
                for (uint i = 0; i < 512; i++)
                {
                    if (i == 0x105)
                        continue;
                    WriteReg(i, 0xff);
                    WriteReg(i, 0x0);
                }
                WriteReg(0x105, 0x0);
                //Clear everything in opl2 mode
                for (uint i = 0; i < 255; i++)
                {
                    WriteReg(i, 0xff);
                    WriteReg(i, 0x0);
                }
            }

            /// <summary>
            /// Return the maximum amount of samples before and LFO change.
            /// </summary>
            /// <returns>The maximum amount of samples before and LFO change.</returns>
            /// <param name="samples">Samples.</param>
            int ForwardLFO(int samples)
            {
                //Current vibrato value, runs 4x slower than tremolo
                vibratoSign = (sbyte)((VibratoTable[vibratoIndex >> 2]) >> 7);
                vibratoShift = (byte)((VibratoTable[vibratoIndex >> 2] & 7) + vibratoStrength);
                tremoloValue = (byte)(tremoloTable[tremoloIndex] >> tremoloStrength);

                //Check hom many samples there can be done before the value changes
                int todo = LfoMax - lfoCounter;
                int count = (todo + lfoAdd - 1) / lfoAdd;
                if (count > samples)
                {
                    count = samples;
                    lfoCounter += count * lfoAdd;
                }
                else
                {
                    lfoCounter += count * lfoAdd;
                    lfoCounter &= (LfoMax - 1);
                    //Maximum of 7 vibrato value * 4
                    vibratoIndex = (byte)((vibratoIndex + 1) & 31);
                    //Clip tremolo to the the table size
                    if (tremoloIndex + 1 < tremoloTable.Length)
                        ++tremoloIndex;
                    else
                        tremoloIndex = 0;
                }
                return count;
            }

            void RegOp(uint reg, Action<Operator> action)
            {
                var index = ((reg >> 3) & 0x20) | (reg & 0x1f);
                var op = opOffsetTable[index];
                if (op != null)
                {
                    action(op(this));
                }
            }

            void RegChan(uint reg, Action<Channel> action)
            {
                var ch = chanOffsetTable[((reg >> 4) & 0x10) | (reg & 0xf)]; 
                if (ch != null)
                {
                    action(ch(this));
                }
            }

            void WriteBD(byte val)
            {
                byte change = (byte)(regBD ^ val);
                if (change == 0)
                    return;
                regBD = val;
                //TODO could do this with shift and xor?
                vibratoStrength = (byte)(((val & 0x40) != 0) ? 0x00 : 0x01);
                tremoloStrength = (byte)(((val & 0x80) != 0) ? 0x00 : 0x02);
                if ((val & 0x20) != 0)
                {
                    //Drum was just enabled, make sure channel 6 has the right synth
                    if ((change & 0x20) != 0)
                    {
                        var mode = (opl3Active != 0) ? SynthMode.Sm3Percussion : SynthMode.Sm2Percussion;
                        chan[6].SynthMode = mode;
                    }
                    //Bass Drum
                    if ((val & 0x10) != 0)
                    {
                        chan[6].Ops[0].KeyOn(0x2);
                        chan[6].Ops[1].KeyOn(0x2);
                    }
                    else
                    {
                        chan[6].Ops[0].KeyOff(0x2);
                        chan[6].Ops[1].KeyOff(0x2);
                    }
                    //Hi-Hat
                    if ((val & 0x1) != 0)
                    {
                        chan[7].Ops[0].KeyOn(0x2);
                    }
                    else
                    {
                        chan[7].Ops[0].KeyOff(0x2);
                    }
                    //Snare
                    if ((val & 0x8) != 0)
                    {
                        chan[7].Ops[1].KeyOn(0x2);
                    }
                    else
                    {
                        chan[7].Ops[1].KeyOff(0x2);
                    }
                    //Tom-Tom
                    if ((val & 0x4) != 0)
                    {
                        chan[8].Ops[0].KeyOn(0x2);
                    }
                    else
                    {
                        chan[8].Ops[0].KeyOff(0x2);
                    }
                    //Top Cymbal
                    if ((val & 0x2) != 0)
                    {
                        chan[8].Ops[1].KeyOn(0x2);
                    }
                    else
                    {
                        chan[8].Ops[1].KeyOff(0x2);
                    }
                    //Toggle keyoffs when we turn off the percussion
                }
                else if ((change & 0x20) != 0)
                {
                    //Trigger a reset to setup the original synth handler
                    //This makes it call
                    chan[6].UpdateSynth(this);
                    chan[6].Ops[0].KeyOff(0x2);
                    chan[6].Ops[1].KeyOff(0x2);
                    chan[7].Ops[0].KeyOff(0x2);
                    chan[7].Ops[1].KeyOff(0x2);
                    chan[8].Ops[0].KeyOff(0x2);
                    chan[8].Ops[1].KeyOff(0x2);
                }
            }

            //This is used as the base counter for vibrato and tremolo
            int lfoCounter;
            int lfoAdd;

            int noiseCounter;
            int noiseAdd;
            int noiseValue;

            uint[] freqMul = new uint[16];
            uint[] linearRates = new uint[76];
            uint[] attackRates = new uint[76];

            //18 channels with 2 operators each
            readonly Channel[] chan;

            byte reg104;
            byte reg08;
            byte regBD;
            byte vibratoIndex;
            byte tremoloIndex;
            sbyte vibratoSign;
            byte vibratoShift;
            byte tremoloValue;
            byte vibratoStrength;
            byte tremoloStrength;
            /// <summary>
            /// Mask for allowed wave forms.
            /// </summary>
            byte waveFormMask;
            //0 or -1 when enabled
            sbyte opl3Active;

            //The lower bits are the shift of the operator vibrato value
            //The highest bit is right shifted to generate -1 or 0 for negation
            //So taking the highest input value of 7 this gives 3, 7, 3, 0, -3, -7, -3, 0
            static readonly sbyte[] VibratoTable =
                {
                    1 - 0x00, 0 - 0x00, 1 - 0x00, 30 - 0x00,
                    1 - 0x80, 0 - 0x80, 1 - 0x80, 30 - 0x80
                };
        }
    }
}

