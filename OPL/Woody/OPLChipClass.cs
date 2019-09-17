//
//  OPLChipClass.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2019 
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
using NScumm.Core.Audio.OPL;

namespace NScumm.Audio.OPL.Woody
{
    /// <summary>
    /// Originally based on ADLIBEMU.C, an AdLib/OPL2 emulation library by Ken Silverman
    /// Copyright(C) 1998-2001 Ken Silverman
    /// Ken Silverman's official web site: "http://www.advsys.net/ken"
    /// This code has been adapted from adplug https://github.com/adplug/adplug
    /// </summary>
    internal sealed class OPLChipClass
    {
        // per-chip variables
        Operator[] op;

        OplType _type;
        int int_samplerate;
        int int_numsamplechannels;
        int int_bytespersample;

        byte status;
        int opl_index;
        byte[] adlibreg = new byte[256];    // adlib register set
        byte[] wave_sel = new byte[22];     // waveform selection

        /// <summary>
        /// vibrato/tremolo increment/counter
        /// </summary>
        int vibtab_pos;
        int vibtab_add;
        int tremtab_pos;
        int tremtab_add;

        /// <summary>
        /// inverse of sampling rate
        /// </summary>
        static double recipsamp;
        /// <summary>
        /// wave form table
        /// </summary>
        public static short[] wavtable = new short[WoodyConstants.WAVEPREC * 3];	// wave form table

        // vibrato/tremolo tables
        static int[] vib_table = new int[WoodyConstants.VIBTAB_SIZE];
        static int[] trem_table = new int[WoodyConstants.TREMTAB_SIZE * 2];

        static int[] vibval_const = new int[WoodyConstants.BLOCKBUF_SIZE];
        static int[] tremval_const = new int[WoodyConstants.BLOCKBUF_SIZE];

        // vibrato value tables (used per-operator)
        static int[] vibval_var1 = new int[WoodyConstants.BLOCKBUF_SIZE];
        static int[] vibval_var2 = new int[WoodyConstants.BLOCKBUF_SIZE];


        // vibrato/tremolo value table pointers
        static Func<int[]> vibval1, vibval2, vibval3, vibval4;
        static Func<int[]> tremval1, tremval2, tremval3, tremval4;

        /// <summary>
        /// where the first entry resides
        /// </summary>
        static int[] wavestart = {
            0,
            WoodyConstants.WAVEPREC>>1,
            0,
            WoodyConstants.WAVEPREC>>2,
            0,
            0,
            0,
            WoodyConstants.WAVEPREC>>3
        };

        // start of the waveform
        static int[] waveform = {
            WoodyConstants.WAVEPREC,
            WoodyConstants.WAVEPREC>>1,
            WoodyConstants.WAVEPREC,
            (WoodyConstants.WAVEPREC*3)>>2,
            0,
            0,
            (WoodyConstants.WAVEPREC*5)>>2,
            WoodyConstants.WAVEPREC<<1
        };

        // length of the waveform as mask
        static int[] wavemask = {
            WoodyConstants.WAVEPREC-1,
            WoodyConstants.WAVEPREC-1,
            (WoodyConstants.WAVEPREC>>1)-1,
            (WoodyConstants.WAVEPREC>>1)-1,
            WoodyConstants.WAVEPREC-1,
            ((WoodyConstants.WAVEPREC*3)>>2)-1,
            WoodyConstants.WAVEPREC>>1,
            WoodyConstants.WAVEPREC-1
        };

        /// <summary>
        /// envelope generator function constants
        /// </summary>
        static double[] attackconst = {
            1/2.82624,
            1/2.25280,
            1/1.88416,
            1/1.59744
        };
        static double[] decrelconst = {
            1/39.28064,
            1/31.41608,
            1/26.17344,
            1/22.44608
        };

        /// <summary>
        /// calculated frequency multiplication values (depend on sampling rate)
        /// </summary>
        static double[] frqmul = new double[16];

        /// <summary>
        /// key scale level lookup table
        /// </summary>
        static readonly double[] kslmul = {
            0.0, 0.5, 0.25, 1.0     // -> 0, 3, 1.5, 6 dB/oct
        };

        // frequency multiplicator lookup table
        static readonly double[] frqmul_tab = {
            0.5,1,2,3,4,5,6,7,8,9,10,10,12,12,15,15
        };

        // key scale levels
        static byte[,] kslev = new byte[8, 16];

        // map a channel number to the register offset of the modulator (=register base)
        static readonly byte[] modulatorbase = {
            0,1,2,
            8,9,10,
            16,17,18
        };

        public static int generator_add;	// should be a chip parameter

        byte[] regbase2modop;
        byte[] regbase2op;

        static int initfirstime;

        private int NumChannels => _type == OplType.Opl3 ? 18 : 9;
        private int MaxOperators => NumChannels * 2;

        private Action<Operator>[] opfuncs = {
            op=>op.Attack(),
            op=>op.Decay(),
            op=>op.Release(),
            op=>op.Sustain(),	// sustain phase (keeping level)
	        op=>op.Release(),	// sustain_nokeep phase (release-style)
	        op=>op.Off()
        };

        public OPLChipClass(OplType type)
        {
            op = new Operator[MaxOperators];
            _type = type;
            if (_type == OplType.Opl3)
            {
                adlibreg = new byte[512];    // adlib register set
                wave_sel = new byte[44];     // waveform selection
                regbase2modop = new byte[]{
                    0,1,2,0,1,2,0,0,3,4,5,3,4,5,0,0,6,7,8,6,7,8,					// first set
                    18,19,20,18,19,20,0,0,21,22,23,21,22,23,0,0,24,25,26,24,25,26	// second set
                };
                regbase2op = new byte[]{
                    0,1,2,9,10,11,0,0,3,4,5,12,13,14,0,0,6,7,8,15,16,17,			// first set
                    18,19,20,27,28,29,0,0,21,22,23,30,31,32,0,0,24,25,26,33,34,35	// second set
                };
            }
            else
            {
                adlibreg = new byte[256];    // adlib register set
                wave_sel = new byte[22];     // waveform selection
                regbase2modop = new byte[]{
                    0,1,2,0,1,2,0,0,3,4,5,3,4,5,0,0,6,7,8,6,7,8
                };
                regbase2op = new byte[]{
                    0,1,2,9,10,11,0,0,3,4,5,12,13,14,0,0,6,7,8,15,16,17
                };
            }
        }

        /// <summary>
        /// Enable an operator
        /// </summary>
        private void EnableOperator(int regbase, Operator op_pt, int act_type)
        {
            // check if this is really an off-on transition
            if (op_pt.act_state == WoodyConstants.OP_ACT_OFF)
            {
                int wselbase = regbase;
                if (wselbase >= WoodyConstants.ARC_SECONDSET) wselbase -= (WoodyConstants.ARC_SECONDSET - 22);    // second set starts at 22

                op_pt.tcount = wavestart[wave_sel[wselbase]] * WoodyConstants.FIXEDPT;

                // start with attack mode
                op_pt.op_state = WoodyConstants.OF_TYPE_ATT;
                op_pt.act_state |= act_type;
            }
        }

        private void ChangeFrequency(int chanbase, int regbase, Operator op_pt)
        {
            // frequency
            var frn = (((adlibreg[WoodyConstants.ARC_KON_BNUM + chanbase]) & 3) << 8) + adlibreg[WoodyConstants.ARC_FREQ_NUM + chanbase];
            // block number/octave
            var oct = (((adlibreg[WoodyConstants.ARC_KON_BNUM + chanbase]) >> 2) & 7);
            op_pt.freq_high = (int)((frn >> 7) & 7);

            // keysplit
            var note_sel = (adlibreg[8] >> 6) & 1;
            op_pt.toff = ((frn >> 9) & (note_sel ^ 1)) | ((frn >> 8) & note_sel);
            op_pt.toff += (oct << 1);

            // envelope scaling (KSR)
            if (0 == (adlibreg[WoodyConstants.ARC_TVS_KSR_MUL + regbase] & 0x10)) op_pt.toff >>= 2;

            // 20+a0+b0:
            op_pt.tinc = (int)((frn << oct) * frqmul[adlibreg[WoodyConstants.ARC_TVS_KSR_MUL + regbase] & 15]);
            // 40+a0+b0:
            double vol_in = (adlibreg[WoodyConstants.ARC_KSL_OUTLEV + regbase] & 63) +
                                    kslmul[adlibreg[WoodyConstants.ARC_KSL_OUTLEV + regbase] >> 6] * kslev[oct, frn >> 6];
            op_pt.vol = Math.Pow(WoodyConstants.FL2, vol_in * -0.125 - 14);

            // operator frequency changed, care about features that depend on it
            ChangeAttackrate(regbase, op_pt);
            ChangeDecayrate(regbase, op_pt);
            ChangeReleaserate(regbase, op_pt);
        }

        private void ChangeAttackrate(int regbase, Operator op_pt)
        {
            int attackrate = adlibreg[WoodyConstants.ARC_ATTR_DECR + regbase] >> 4;
            if (attackrate != 0)
            {
                double f = Math.Pow(WoodyConstants.FL2, (double)attackrate + (op_pt.toff >> 2) - 1) * attackconst[op_pt.toff & 3] * recipsamp;
                // attack rate coefficients
                op_pt.a0 = 0.0377 * f;
                op_pt.a1 = 10.73 * f + 1;
                op_pt.a2 = -17.57 * f;
                op_pt.a3 = 7.42 * f;

                int step_skip = attackrate * 4 + op_pt.toff;
                int steps = step_skip >> 2;
                op_pt.env_step_a = (1 << (steps <= 12 ? 12 - steps : 0)) - 1;

                int step_num = (step_skip <= 48) ? (4 - (step_skip & 3)) : 0;
                byte[] step_skip_mask = { 0xff, 0xfe, 0xee, 0xba, 0xaa };
                op_pt.env_step_skip_a = step_skip_mask[step_num];

                if (step_skip >= ((_type == OplType.Opl3) ? 60 : 62))
                {
                    op_pt.a0 = 2.0;  // something that triggers an immediate transition to amp:=1.0
                    op_pt.a1 = 0.0;
                    op_pt.a2 = 0.0;
                    op_pt.a3 = 0.0;
                }
            }
            else
            {
                // attack disabled
                op_pt.a0 = 0.0;
                op_pt.a1 = 1.0;
                op_pt.a2 = 0.0;
                op_pt.a3 = 0.0;
                op_pt.env_step_a = 0;
                op_pt.env_step_skip_a = 0;
            }
        }

        private void ChangeDecayrate(int regbase, Operator op_pt)
        {
            int decayrate = adlibreg[WoodyConstants.ARC_ATTR_DECR + regbase] & 15;
            // decaymul should be 1.0 when decayrate==0
            if (decayrate != 0)
            {
                double f = -7.4493 * decrelconst[op_pt.toff & 3] * recipsamp;
                op_pt.decaymul = Math.Pow(WoodyConstants.FL2, f * Math.Pow(WoodyConstants.FL2, decayrate + (op_pt.toff >> 2)));
                int steps = (decayrate * 4 + op_pt.toff) >> 2;
                op_pt.env_step_d = (1 << (steps <= 12 ? 12 - steps : 0)) - 1;
            }
            else
            {
                op_pt.decaymul = 1.0;
                op_pt.env_step_d = 0;
            }
        }

        private void ChangeReleaserate(int regbase, Operator op_pt)
        {
            int releaserate = adlibreg[WoodyConstants.ARC_SUSL_RELR + regbase] & 15;
            // releasemul should be 1.0 when releaserate==0
            if (releaserate != 0)
            {
                double f = -7.4493 * decrelconst[op_pt.toff & 3] * recipsamp;
                op_pt.releasemul = Math.Pow(WoodyConstants.FL2, f * Math.Pow(WoodyConstants.FL2, releaserate + (op_pt.toff >> 2)));
                int steps = (releaserate * 4 + op_pt.toff) >> 2;
                op_pt.env_step_r = (1 << (steps <= 12 ? 12 - steps : 0)) - 1;
            }
            else
            {
                op_pt.releasemul = 1.0;
                op_pt.env_step_r = 0;
            }
        }

        private void ChangeSustainlevel(int regbase, Operator op_pt)
        {
            int sustainlevel = adlibreg[WoodyConstants.ARC_SUSL_RELR + regbase] >> 4;
            // sustainlevel should be 0.0 when sustainlevel==15 (max)
            if (sustainlevel < 15)
            {
                op_pt.sustain_level = Math.Pow(WoodyConstants.FL2, sustainlevel * (-WoodyConstants.FL05));
            }
            else
            {
                op_pt.sustain_level = 0.0;
            }
        }

        private void ChangeWaveform(int regbase, Operator op_pt)
        {
            if (_type == OplType.Opl3)
            {
                if (regbase >= WoodyConstants.ARC_SECONDSET) regbase -= (WoodyConstants.ARC_SECONDSET - 22);  // second set starts at 22
            }
            // waveform selection
            op_pt.cur_wmask = wavemask[wave_sel[regbase]];
            op_pt.cur_wform = waveform[wave_sel[regbase]];
            // (might need to be adapted to waveform type here...)
        }

        private void ChangeKeepsustain(int regbase, Operator op_pt)
        {
            op_pt.sus_keep = (adlibreg[WoodyConstants.ARC_TVS_KSR_MUL + regbase] & 0x20) > 0;
            if (op_pt.op_state == WoodyConstants.OF_TYPE_SUS)
            {
                if (!op_pt.sus_keep) op_pt.op_state = WoodyConstants.OF_TYPE_SUS_NOKEEP;
            }
            else if (op_pt.op_state == WoodyConstants.OF_TYPE_SUS_NOKEEP)
            {
                if (op_pt.sus_keep) op_pt.op_state = WoodyConstants.OF_TYPE_SUS;
            }
        }

        // enable/disable vibrato/tremolo LFO effects
        private void ChangeVibrato(int regbase, Operator op_pt)
        {
            op_pt.vibrato = (adlibreg[WoodyConstants.ARC_TVS_KSR_MUL + regbase] & 0x40) != 0;
            op_pt.tremolo = (adlibreg[WoodyConstants.ARC_TVS_KSR_MUL + regbase] & 0x80) != 0;
        }

        // change amount of self-feedback
        private void ChangeFeedback(int chanbase, Operator op_pt)
        {
            int feedback = adlibreg[WoodyConstants.ARC_FEEDBACK + chanbase] & 14;
            if (feedback != 0) op_pt.mfbi = (int)(Math.Pow(WoodyConstants.FL2, (feedback >> 1) + 8));
            else op_pt.mfbi = 0;
        }

        public void AdlibInit(int samplerate, int numchannels, int bytespersample)
        {
            int_samplerate = samplerate;
            int_numsamplechannels = numchannels;
            int_bytespersample = bytespersample;

            generator_add = ((int)(WoodyConstants.INTFREQU * WoodyConstants.FIXEDPT / int_samplerate));


            for (var i = 0; i < op.Length; i++)
            {
                op[i] = new Operator
                {
                    op_state = WoodyConstants.OF_TYPE_OFF,
                    act_state = WoodyConstants.OP_ACT_OFF,
                    amp = 0.0,
                    step_amp = 0.0,
                    vol = 0.0,
                    tcount = 0,
                    tinc = 0,
                    toff = 0,
                    cur_wmask = wavemask[0],
                    cur_wform = waveform[0],
                    freq_high = 0,

                    generator_pos = 0,
                    cur_env_step = 0,
                    env_step_a = 0,
                    env_step_d = 0,
                    env_step_r = 0,
                    step_skip_pos_a = 0,
                    env_step_skip_a = 0
                };
                if (_type == OplType.Opl3)
                {
                    op[i].is_4op = false;
                    op[i].is_4op_attached = false;
                    op[i].left_pan = 1;
                    op[i].right_pan = 1;
                }
            }

            recipsamp = 1.0 / int_samplerate;
            for (var i = 15; i >= 0; i--)
            {
                frqmul[i] = frqmul_tab[i] * WoodyConstants.INTFREQU / WoodyConstants.WAVEPREC * WoodyConstants.FIXEDPT * recipsamp;
            }

            status = 0;
            opl_index = 0;


            // create vibrato table
            vib_table[0] = 8;
            vib_table[1] = 4;
            vib_table[2] = 0;
            vib_table[3] = -4;
            for (var i = 4; i < WoodyConstants.VIBTAB_SIZE; i++) vib_table[i] = vib_table[i - 4] * -1;

            // vibrato at ~6.1 ?? (opl3 docs say 6.1, opl4 docs say 6.0, y8950 docs say 6.4)
            vibtab_add = ((int)(WoodyConstants.VIBTAB_SIZE * WoodyConstants.FIXEDPT_LFO / 8192 * WoodyConstants.INTFREQU / int_samplerate));
            vibtab_pos = 0;

            for (var i = 0; i < WoodyConstants.BLOCKBUF_SIZE; i++) vibval_const[i] = 0;


            // create tremolo table
            int[] trem_table_int = new int[WoodyConstants.TREMTAB_SIZE];
            for (var i = 0; i < 14; i++) trem_table_int[i] = i - 13;        // upwards (13 to 26 -> -0.5/6 to 0)
            for (var i = 14; i < 41; i++) trem_table_int[i] = -i + 14;      // downwards (26 to 0 -> 0 to -1/6)
            for (var i = 41; i < 53; i++) trem_table_int[i] = i - 40 - 26;  // upwards (1 to 12 -> -1/6 to -0.5/6)

            for (var i = 0; i < WoodyConstants.TREMTAB_SIZE; i++)
            {
                // 0.0 .. -26/26*4.8/6 == [0.0 .. -0.8], 4/53 steps == [1 .. 0.57]
                double trem_val1 = trem_table_int[i] * 4.8 / 26.0 / 6.0;                // 4.8db
                double trem_val2 = trem_table_int[i] / 4 * 1.2 / 6.0 / 6.0;       // 1.2db (larger stepping)

                trem_table[i] = (int)(Math.Pow(WoodyConstants.FL2, trem_val1) * WoodyConstants.FIXEDPT);
                trem_table[WoodyConstants.TREMTAB_SIZE + i] = (int)(Math.Pow(WoodyConstants.FL2, trem_val2) * WoodyConstants.FIXEDPT);
            }

            // tremolo at 3.7hz
            tremtab_add = (int)(WoodyConstants.TREMTAB_SIZE * WoodyConstants.TREM_FREQ * WoodyConstants.FIXEDPT_LFO / int_samplerate);
            tremtab_pos = 0;

            for (var i = 0; i < WoodyConstants.BLOCKBUF_SIZE; i++) tremval_const[i] = WoodyConstants.FIXEDPT;

            if (initfirstime == 0)
            {
                initfirstime = 1;

                // create waveform tables
                for (var i = 0; i < (WoodyConstants.WAVEPREC >> 1); i++)
                {
                    wavtable[(i << 1) + WoodyConstants.WAVEPREC] = (short)(16384 * Math.Sin((double)((i << 1)) * Math.PI * 2 / WoodyConstants.WAVEPREC));
                    wavtable[(i << 1) + 1 + WoodyConstants.WAVEPREC] = (short)(16384 * Math.Sin((double)((i << 1) + 1) * Math.PI * 2 / WoodyConstants.WAVEPREC));
                    wavtable[i] = wavtable[(i << 1) + WoodyConstants.WAVEPREC];
                    // alternative: (zero-less)
                    /*			wavtable[(i<<1)  +WoodyConstants.WAVEPREC]	= (Bit16s)(16384*Math.Sin((double)((i<<2)+1)*Math.PI/WoodyConstants.WAVEPREC));
                                wavtable[(i<<1)+1+WoodyConstants.WAVEPREC]	= (Bit16s)(16384*Math.Sin((double)((i<<2)+3)*Math.PI/WoodyConstants.WAVEPREC));
                                wavtable[i]					= wavtable[(i<<1)-1+WoodyConstants.WAVEPREC]; */
                }
                for (var i = 0; i < (WoodyConstants.WAVEPREC >> 3); i++)
                {
                    wavtable[i + (WoodyConstants.WAVEPREC << 1)] = (short)(wavtable[i + (WoodyConstants.WAVEPREC >> 3)] - 16384);
                    wavtable[i + ((WoodyConstants.WAVEPREC * 17) >> 3)] = (short)(wavtable[i + (WoodyConstants.WAVEPREC >> 2)] + 16384);
                }

                // key scale level table verified ([table in book]*8/3)
                kslev[7, 0] = 0; kslev[7, 1] = 24; kslev[7, 2] = 32; kslev[7, 3] = 37;
                kslev[7, 4] = 40; kslev[7, 5] = 43; kslev[7, 6] = 45; kslev[7, 7] = 47;
                kslev[7, 8] = 48;
                for (var i = 9; i < 16; i++) kslev[7, i] = (byte)(i + 41);
                for (var j = 6; j >= 0; j--)
                {
                    for (var i = 0; i < 16; i++)
                    {
                        var oct = kslev[j + 1, i] - 8;
                        if (oct < 0) oct = 0;
                        kslev[j, i] = (byte)oct;
                    }
                }
            }

        }

        public void AdlibWrite(int idx, byte val)
        {
            var second_set = idx & 0x100;
            adlibreg[idx] = val;

            switch (idx & 0xf0)
            {
                case WoodyConstants.ARC_CONTROL:
                    // here we check for the second set registers, too:
                    switch (idx)
                    {
                        case 0x02:  // timer1 counter
                        case 0x03:  // timer2 counter
                            break;
                        case 0x04:
                            // IRQ reset, timer mask/start
                            if ((val & 0x80) != 0)
                            {
                                // clear IRQ bits in status register
                                status &= unchecked((byte)(~0x60));
                            }
                            else
                            {
                                status = 0;
                            }
                            break;
                        case 0x04 | WoodyConstants.ARC_SECONDSET:
                            if (_type == OplType.Opl3)
                            {
                                // 4op enable/disable switches for each possible channel
                                op[0].is_4op = (val & 1) > 0;
                                op[3].is_4op_attached = op[0].is_4op;
                                op[1].is_4op = (val & 2) > 0;
                                op[4].is_4op_attached = op[1].is_4op;
                                op[2].is_4op = (val & 4) > 0;
                                op[5].is_4op_attached = op[2].is_4op;
                                op[18].is_4op = (val & 8) > 0;
                                op[21].is_4op_attached = op[18].is_4op;
                                op[19].is_4op = (val & 16) > 0;
                                op[22].is_4op_attached = op[19].is_4op;
                                op[20].is_4op = (val & 32) > 0;
                                op[23].is_4op_attached = op[20].is_4op;
                            }
                            break;
                        case 0x05 | WoodyConstants.ARC_SECONDSET:
                            break;
                        case 0x08:
                            // CSW, note select
                            break;
                    }
                    break;
                case WoodyConstants.ARC_TVS_KSR_MUL:
                case WoodyConstants.ARC_TVS_KSR_MUL + 0x10:
                    {
                        // tremolo/vibrato/sustain keeping enabled; key scale rate; frequency multiplication
                        int num = idx & 7;
                        int @base = (idx - WoodyConstants.ARC_TVS_KSR_MUL) & 0xff;
                        if ((num < 6) && (@base < 22))
                        {
                            int modop = regbase2modop[second_set != 0 ? (@base + 22) : @base];
                            int regbase = @base + second_set;
                            int chanbase = second_set != 0 ? (modop - 18 + WoodyConstants.ARC_SECONDSET) : modop;

                            // change tremolo/vibrato and sustain keeping of this operator
                            var op_ptr = op[modop + ((num < 3) ? 0 : 9)];
                            ChangeKeepsustain(regbase, op_ptr);
                            ChangeVibrato(regbase, op_ptr);

                            // change frequency calculations of this operator as
                            // key scale rate and frequency multiplicator can be changed
                            if (_type == OplType.Opl3)
                            {
                                if (((adlibreg[0x105] & 1) != 0) && op[modop].is_4op_attached)
                                {
                                    // operator uses frequency of channel
                                    ChangeFrequency(chanbase - 3, regbase, op_ptr);
                                }
                                else
                                {
                                    ChangeFrequency(chanbase, regbase, op_ptr);
                                }
                            }
                            else
                            {
                                ChangeFrequency(chanbase, @base, op_ptr);
                            }
                        }
                    }
                    break;
                case WoodyConstants.ARC_KSL_OUTLEV:
                case WoodyConstants.ARC_KSL_OUTLEV + 0x10:
                    {
                        // key scale level; output rate
                        int num = idx & 7;
                        int @base = (idx - WoodyConstants.ARC_KSL_OUTLEV) & 0xff;
                        if ((num < 6) && (@base < 22))
                        {
                            int modop = regbase2modop[second_set != 0 ? (@base + 22) : @base];
                            int chanbase = second_set != 0 ? (modop - 18 + WoodyConstants.ARC_SECONDSET) : modop;

                            // change frequency calculations of this operator as
                            // key scale level and output rate can be changed
                            var op_ptr = op[modop + ((num < 3) ? 0 : 9)];
                            if (_type == OplType.Opl3)
                            {
                                int regbase = @base + second_set;
                                if (((adlibreg[0x105] & 1) != 0) && op[modop].is_4op_attached)
                                {
                                    // operator uses frequency of channel
                                    ChangeFrequency(chanbase - 3, regbase, op_ptr);
                                }
                                else
                                {
                                    ChangeFrequency(chanbase, regbase, op_ptr);
                                }
                            }
                            else
                            {
                                ChangeFrequency(chanbase, @base, op_ptr);
                            }
                        }
                    }
                    break;
                case WoodyConstants.ARC_ATTR_DECR:
                case WoodyConstants.ARC_ATTR_DECR + 0x10:
                    {
                        // attack/decay rates
                        int num = idx & 7;
                        int @base = (idx - WoodyConstants.ARC_ATTR_DECR) & 0xff;
                        if ((num < 6) && (@base < 22))
                        {
                            int regbase = @base + second_set;

                            // change attack rate and decay rate of this operator
                            var op_ptr = op[regbase2op[second_set != 0 ? (@base + 22) : @base]];
                            ChangeAttackrate(regbase, op_ptr);
                            ChangeDecayrate(regbase, op_ptr);
                        }
                    }
                    break;
                case WoodyConstants.ARC_SUSL_RELR:
                case WoodyConstants.ARC_SUSL_RELR + 0x10:
                    {
                        // sustain level; release rate
                        int num = idx & 7;
                        int @base = (idx - WoodyConstants.ARC_SUSL_RELR) & 0xff;
                        if ((num < 6) && (@base < 22))
                        {
                            int regbase = @base + second_set;

                            // change sustain level and release rate of this operator
                            var op_ptr = op[regbase2op[second_set != 0 ? (@base + 22) : @base]];
                            ChangeReleaserate(regbase, op_ptr);
                            ChangeSustainlevel(regbase, op_ptr);
                        }
                    }
                    break;
                case WoodyConstants.ARC_FREQ_NUM:
                    {
                        // 0xa0-0xa8 low8 frequency
                        byte @base = (byte)((idx - WoodyConstants.ARC_FREQ_NUM) & 0xff);
                        if (@base < 9)
                        {
                            int opbase = second_set != 0 ? (@base + 18) : @base;
                            if (_type == OplType.Opl3)
                            {
                                if (((adlibreg[0x105] & 1) != 0) && op[opbase].is_4op_attached) break;
                            }
                            // regbase of modulator:
                            int modbase = modulatorbase[@base] + second_set;

                            int chanbase = @base + second_set;

                            ChangeFrequency(chanbase, modbase, op[opbase]);
                            ChangeFrequency(chanbase, modbase + 3, op[opbase + 9]);
                            if (_type == OplType.Opl3)
                            {
                                // for 4op channels all four operators are modified to the frequency of the channel
                                if (((adlibreg[0x105] & 1) != 0) && op[second_set != 0 ? (@base + 18) : @base].is_4op)
                                {
                                    ChangeFrequency(chanbase, modbase + 8, op[opbase + 3]);
                                    ChangeFrequency(chanbase, modbase + 3 + 8, op[opbase + 3 + 9]);
                                }
                            }
                        }
                    }
                    break;
                case WoodyConstants.ARC_KON_BNUM:
                    {
                        if (idx == WoodyConstants.ARC_PERC_MODE)
                        {
                            if (_type == OplType.Opl3)
                            {
                                if (second_set != 0) return;
                            }
                            if ((val & 0x30) == 0x30)
                            {       // BassDrum active
                                EnableOperator(16, op[6], WoodyConstants.OP_ACT_PERC);
                                ChangeFrequency(6, 16, op[6]);
                                EnableOperator(16 + 3, op[6 + 9], WoodyConstants.OP_ACT_PERC);
                                ChangeFrequency(6, 16 + 3, op[6 + 9]);
                            }
                            else
                            {
                                op[6].DisableOperator(WoodyConstants.OP_ACT_PERC);
                                op[6 + 9].DisableOperator(WoodyConstants.OP_ACT_PERC);
                            }
                            if ((val & 0x28) == 0x28)
                            {       // Snare active
                                EnableOperator(17 + 3, op[16], WoodyConstants.OP_ACT_PERC);
                                ChangeFrequency(7, 17 + 3, op[16]);
                            }
                            else
                            {
                                op[16].DisableOperator(WoodyConstants.OP_ACT_PERC);
                            }
                            if ((val & 0x24) == 0x24)
                            {       // TomTom active
                                EnableOperator(18, op[8], WoodyConstants.OP_ACT_PERC);
                                ChangeFrequency(8, 18, op[8]);
                            }
                            else
                            {
                                op[8].DisableOperator(WoodyConstants.OP_ACT_PERC);
                            }
                            if ((val & 0x22) == 0x22)
                            {       // Cymbal active
                                EnableOperator(18 + 3, op[8 + 9], WoodyConstants.OP_ACT_PERC);
                                ChangeFrequency(8, 18 + 3, op[8 + 9]);
                            }
                            else
                            {
                                op[8 + 9].DisableOperator(WoodyConstants.OP_ACT_PERC);
                            }
                            if ((val & 0x21) == 0x21)
                            {       // Hihat active
                                EnableOperator(17, op[7], WoodyConstants.OP_ACT_PERC);
                                ChangeFrequency(7, 17, op[7]);
                            }
                            else
                            {
                                op[7].DisableOperator(WoodyConstants.OP_ACT_PERC);
                            }

                            break;
                        }
                        // regular 0xb0-0xb8
                        int @base = (idx - WoodyConstants.ARC_KON_BNUM) & 0xff;
                        if (@base < 9)
                        {
                            int opbase = second_set != 0 ? (@base + 18) : @base;
                            if (_type == OplType.Opl3)
                            {
                                if (((adlibreg[0x105] & 1) != 0) && op[opbase].is_4op_attached) break;
                            }
                            // regbase of modulator:
                            int modbase = modulatorbase[@base] + second_set;

                            if ((val & 32) != 0)
                            {
                                // operator switched on
                                EnableOperator(modbase, op[opbase], WoodyConstants.OP_ACT_NORMAL);       // modulator (if 2op)
                                EnableOperator(modbase + 3, op[opbase + 9], WoodyConstants.OP_ACT_NORMAL);   // carrier (if 2op)
                                if (_type == OplType.Opl3)
                                {
                                    // for 4op channels all four operators are switched on
                                    if (((adlibreg[0x105] & 1) != 0) && op[opbase].is_4op)
                                    {
                                        // turn on chan+3 operators as well
                                        EnableOperator(modbase + 8, op[opbase + 3], WoodyConstants.OP_ACT_NORMAL);
                                        EnableOperator(modbase + 3 + 8, op[opbase + 3 + 9], WoodyConstants.OP_ACT_NORMAL);
                                    }
                                }
                            }
                            else
                            {
                                // operator switched off
                                op[opbase].DisableOperator(WoodyConstants.OP_ACT_NORMAL);
                                op[opbase + 9].DisableOperator(WoodyConstants.OP_ACT_NORMAL);
                                if (_type == OplType.Opl3)
                                {
                                    // for 4op channels all four operators are switched off
                                    if (((adlibreg[0x105] & 1) != 0) && op[opbase].is_4op)
                                    {
                                        // turn off chan+3 operators as well
                                        op[opbase + 3].DisableOperator(WoodyConstants.OP_ACT_NORMAL);
                                        op[opbase + 3 + 9].DisableOperator(WoodyConstants.OP_ACT_NORMAL);
                                    }
                                }
                            }

                            int chanbase = @base + second_set;

                            // change frequency calculations of modulator and carrier (2op) as
                            // the frequency of the channel has changed
                            ChangeFrequency(chanbase, modbase, op[opbase]);
                            ChangeFrequency(chanbase, modbase + 3, op[opbase + 9]);
                            if (_type == OplType.Opl3)
                            {
                                // for 4op channels all four operators are modified to the frequency of the channel
                                if (((adlibreg[0x105] & 1) != 0) && op[second_set != 0 ? (@base + 18) : @base].is_4op)
                                {
                                    // change frequency calculations of chan+3 operators as well
                                    ChangeFrequency(chanbase, modbase + 8, op[opbase + 3]);
                                    ChangeFrequency(chanbase, modbase + 3 + 8, op[opbase + 3 + 9]);
                                }
                            }
                        }
                    }
                    break;
                case WoodyConstants.ARC_FEEDBACK:
                    {
                        // 0xc0-0xc8 feedback/modulation type (AM/FM)
                        int @base = (idx - WoodyConstants.ARC_FEEDBACK) & 0xff;
                        if (@base < 9)
                        {
                            int opbase = second_set != 0 ? (@base + 18) : @base;
                            int chanbase = @base + second_set;
                            ChangeFeedback(chanbase, op[opbase]);
                            if (_type == OplType.Opl3)
                            {
                                // OPL3 panning
                                op[opbase].left_pan = ((val & 0x10) >> 4);
                                op[opbase].right_pan = ((val & 0x20) >> 5);
                            }
                        }
                    }
                    break;
                case WoodyConstants.ARC_WAVE_SEL:
                case WoodyConstants.ARC_WAVE_SEL + 0x10:
                    {
                        int num = idx & 7;
                        int @base = (idx - WoodyConstants.ARC_WAVE_SEL) & 0xff;
                        if ((num < 6) && (@base < 22))
                        {
                            if (_type == OplType.Opl3)
                            {
                                int wselbase = second_set != 0 ? (@base + 22) : @base;    // for easier mapping onto wave_sel[]
                                                                                          // change waveform
                                if ((adlibreg[0x105] & 1) != 0) wave_sel[wselbase] = (byte)(val & 7);  // opl3 mode enabled, all waveforms accessible
                                else wave_sel[wselbase] = (byte)(val & 3);
                                var op_ptr = op[regbase2modop[wselbase] + ((num < 3) ? 0 : 9)];
                                ChangeWaveform(wselbase, op_ptr);
                            }
                            else if ((adlibreg[0x01] & 0x20) != 0)
                            {
                                // wave selection enabled, change waveform
                                wave_sel[@base] = (byte)(val & 3);
                                var op_ptr = op[regbase2modop[@base] + ((num < 3) ? 0 : 9)];
                                ChangeWaveform(@base, op_ptr);
                            }
                        }
                    }
                    break;
            }
        }

        private int AdlibRegRead(int port)
        {
            if (_type == OplType.Opl3)
            {
                // opl3-detection routines require ret&6 to be zero
                if ((port & 1) == 0)
                {
                    return status;
                }
                return 0x00;
            }
            // opl2-detection routines require ret&6 to be 6
            if ((port & 1) == 0)
            {
                return status | 6;
            }
            return 0xff;
        }

        private void AdlibWriteIndex(int port, byte val)
        {
            opl_index = val;
            if (_type == OplType.Opl3)
            {
                if ((port & 3) != 0)
                {
                    // possibly second set
                    if (((adlibreg[0x105] & 1) != 0) || (opl_index == 5)) opl_index |= WoodyConstants.ARC_SECONDSET;
                }
            }
        }

        private static short Clipit8(int ival)
        {
            ival /= 256;
            ival += 128;
            if (ival < 256)
            {
                if (ival >= 0)
                {
                    return (sbyte)ival;
                }
                return 0;
            }
            return 255;
        }

        private static short Clipit16(int ival)
        {
            if (ival < 32768)
            {
                if (ival > -32769)
                {
                    return (short)ival;
                }
                return -32768;
            }
            return 32767;
        }

        public void AdlibGetSample(short[] data, int pos, int numsamples)
        {
            int i, endsamples;
            int cptr = 0;
            int sndptr = 0;
            int sndptr1 = 0;

            var outbufl = new int[WoodyConstants.BLOCKBUF_SIZE];
            // second output buffer (right channel for opl3 stereo)
            var outbufr = new int[WoodyConstants.BLOCKBUF_SIZE];


            // vibrato/tremolo lookup tables (global, to possibly be used by all operators)
            int[] vib_lut = new int[WoodyConstants.BLOCKBUF_SIZE];
            int[] trem_lut = new int[WoodyConstants.BLOCKBUF_SIZE];

            int samples_to_process = numsamples;

            for (var cursmp = 0; cursmp < samples_to_process; cursmp += endsamples)
            {
                endsamples = samples_to_process - cursmp;
                if (endsamples > WoodyConstants.BLOCKBUF_SIZE) endsamples = WoodyConstants.BLOCKBUF_SIZE;

                Array.Clear(outbufl, 0, endsamples);
                if (_type == OplType.Opl3)
                {
                    // clear second output buffer (opl3 stereo)
                    if ((adlibreg[0x105] & 1) != 0) Array.Clear(outbufr, 0, endsamples);
                }

                // calculate vibrato/tremolo lookup tables
                int vib_tshift = ((adlibreg[WoodyConstants.ARC_PERC_MODE] & 0x40) == 0) ? 1 : 0;    // 14cents/7cents switching
                for (i = 0; i < endsamples; i++)
                {
                    // cycle through vibrato table
                    vibtab_pos += vibtab_add;
                    if (vibtab_pos / WoodyConstants.FIXEDPT_LFO >= WoodyConstants.VIBTAB_SIZE) vibtab_pos -= WoodyConstants.VIBTAB_SIZE * WoodyConstants.FIXEDPT_LFO;
                    vib_lut[i] = vib_table[vibtab_pos / WoodyConstants.FIXEDPT_LFO] >> vib_tshift;     // 14cents (14/100 of a semitone) or 7cents

                    // cycle through tremolo table
                    tremtab_pos += tremtab_add;
                    if (tremtab_pos / WoodyConstants.FIXEDPT_LFO >= WoodyConstants.TREMTAB_SIZE) tremtab_pos -= WoodyConstants.TREMTAB_SIZE * WoodyConstants.FIXEDPT_LFO;
                    if ((adlibreg[WoodyConstants.ARC_PERC_MODE] & 0x80) != 0) trem_lut[i] = trem_table[tremtab_pos / WoodyConstants.FIXEDPT_LFO];
                    else trem_lut[i] = trem_table[WoodyConstants.TREMTAB_SIZE + tremtab_pos / WoodyConstants.FIXEDPT_LFO];
                }

                if ((adlibreg[WoodyConstants.ARC_PERC_MODE] & 0x20) != 0)
                {
                    //BassDrum
                    cptr = 6;
                    if ((adlibreg[WoodyConstants.ARC_FEEDBACK + 6] & 1) != 0)
                    {
                        // additive synthesis
                        if (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF)
                        {
                            if (op[cptr + 9].vibrato)
                            {
                                vibval1 = () => vibval_var1;
                                for (i = 0; i < endsamples; i++)
                                    vibval1()[i] = (int)((vib_lut[i] * op[cptr + 9].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                            }
                            else vibval1 = () => vibval_const;
                            if (op[cptr + 9].tremolo) tremval1 = () => trem_lut;   // tremolo enabled, use table
                            else tremval1 = () => tremval_const;

                            // calculate channel output
                            for (i = 0; i < endsamples; i++)
                            {
                                op[cptr + 9].Advance(vibval1()[i]);
                                opfuncs[op[cptr + 9].op_state](op[cptr + 9]);
                                op[cptr + 9].Output(0, tremval1()[i]);

                                int chanval = op[cptr + 9].cval * 2;
                                ChanVal(outbufl, outbufr, cptr, i, chanval);
                            }
                        }
                    }
                    else
                    {
                        // frequency modulation
                        if ((op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF) || (op[cptr].op_state != WoodyConstants.OF_TYPE_OFF))
                        {
                            if ((op[cptr].vibrato) && (op[cptr].op_state != WoodyConstants.OF_TYPE_OFF))
                            {
                                vibval1 = () => vibval_var1;
                                for (i = 0; i < endsamples; i++)
                                    vibval1()[i] = (int)((vib_lut[i] * op[cptr].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                            }
                            else vibval1 = () => vibval_const;
                            if ((op[cptr + 9].vibrato) && (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                            {
                                vibval2 = () => vibval_var2;
                                for (i = 0; i < endsamples; i++)
                                    vibval2()[i] = (int)((vib_lut[i] * op[cptr + 9].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                            }
                            else vibval2 = () => vibval_const;
                            if (op[cptr].tremolo) tremval1 = () => trem_lut;   // tremolo enabled, use table
                            else tremval1 = () => tremval_const;
                            if (op[cptr + 9].tremolo) tremval2 = () => trem_lut;   // tremolo enabled, use table
                            else tremval2 = () => tremval_const;

                            // calculate channel output
                            for (i = 0; i < endsamples; i++)
                            {
                                op[cptr].Advance(vibval1()[i]);
                                opfuncs[op[cptr].op_state](op[cptr]);
                                op[cptr].Output((op[cptr].lastcval + op[cptr].cval) * op[cptr].mfbi / 2, tremval1()[i]);

                                op[cptr + 9].Advance(vibval2()[i]);
                                opfuncs[op[cptr + 9].op_state](op[cptr + 9]);
                                op[cptr + 9].Output(op[cptr].cval * WoodyConstants.FIXEDPT, tremval2()[i]);

                                int chanval = op[cptr + 9].cval * 2;
                                ChanVal(outbufl, outbufr, cptr, i, chanval);

                            }
                        }
                    }

                    //TomTom (j=8)
                    if (op[8].op_state != WoodyConstants.OF_TYPE_OFF)
                    {
                        cptr = 8;
                        if (op[cptr].vibrato)
                        {
                            vibval3 = () => vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                vibval3()[i] = (int)((vib_lut[i] * op[cptr].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                        }
                        else vibval3 = () => vibval_const;

                        if (op[cptr].tremolo) tremval3 = () => trem_lut;   // tremolo enabled, use table
                        else tremval3 = () => tremval_const;

                        // calculate channel output
                        for (i = 0; i < endsamples; i++)
                        {
                            op[cptr].Advance(vibval3()[i]);
                            opfuncs[op[cptr].op_state](op[cptr]);        //TomTom
                            op[cptr].Output(0, tremval3()[i]);
                            int chanval = op[cptr].cval * 2;
                            ChanVal(outbufl, outbufr, cptr, i, chanval);

                        }
                    }

                    //Snare/Hihat (j=7), Cymbal (j=8)
                    if ((op[7].op_state != WoodyConstants.OF_TYPE_OFF) || (op[16].op_state != WoodyConstants.OF_TYPE_OFF) ||
                        (op[17].op_state != WoodyConstants.OF_TYPE_OFF))
                    {
                        cptr = 7;
                        if ((op[cptr].vibrato) && (op[cptr].op_state != WoodyConstants.OF_TYPE_OFF))
                        {
                            vibval1 = () => vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                vibval1()[i] = (int)((vib_lut[i] * op[cptr].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                        }
                        else vibval1 = () => vibval_const;
                        if ((op[cptr + 9].vibrato) && (op[cptr + 9].op_state == WoodyConstants.OF_TYPE_OFF))
                        {
                            vibval2 = () => vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                vibval2()[i] = (int)((vib_lut[i] * op[cptr + 9].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                        }
                        else vibval2 = () => vibval_const;

                        if (op[cptr].tremolo) tremval1 = () => trem_lut;   // tremolo enabled, use table
                        else tremval1 = () => tremval_const;
                        if (op[cptr + 9].tremolo) tremval2 = () => trem_lut;   // tremolo enabled, use table
                        else tremval2 = () => tremval_const;

                        cptr = 8;
                        if ((op[cptr + 9].vibrato) && (op[cptr + 9].op_state == WoodyConstants.OF_TYPE_OFF))
                        {
                            vibval4 = () => vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                vibval4()[i] = (int)((vib_lut[i] * op[cptr + 9].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                        }
                        else vibval4 = () => vibval_const;

                        if (op[cptr + 9].tremolo) tremval4 = () => trem_lut;   // tremolo enabled, use table
                        else tremval4 = () => tremval_const;

                        // calculate channel output
                        for (i = 0; i < endsamples; i++)
                        {
                            Operator.AdvanceDrums(op[7], vibval1()[i], op[7 + 9], vibval2()[i], op[8 + 9], vibval4()[i]);

                            opfuncs[op[7].op_state](op[7]);            //Hihat
                            op[7].Output(0, tremval1()[i]);

                            opfuncs[op[7 + 9].op_state](op[7 + 9]);        //Snare
                            op[7 + 9].Output(0, tremval2()[i]);

                            opfuncs[op[8 + 9].op_state](op[8 + 9]);        //Cymbal
                            op[8 + 9].Output(0, tremval4()[i]);

                            int chanval = (op[7].cval + op[7 + 9].cval + op[8 + 9].cval) * 2;
                            ChanVal(outbufl, outbufr, cptr, i, chanval);
                        }
                    }
                }

                int max_channel = NumChannels;
                if (_type == OplType.Opl3)
                {
                    if ((adlibreg[0x105] & 1) == 0) max_channel = NumChannels / 2;
                }
                for (var cur_ch = max_channel - 1; cur_ch >= 0; cur_ch--)
                {
                    // skip drum/percussion operators
                    if (((adlibreg[WoodyConstants.ARC_PERC_MODE] & 0x20) != 0) && (cur_ch >= 6) && (cur_ch < 9)) continue;

                    int k = cur_ch;
                    if (_type == OplType.Opl3)
                    {
                        if (cur_ch < 9)
                        {
                            cptr = cur_ch;
                        }
                        else
                        {
                            cptr = cur_ch + 9; // second set is operator18-operator35
                            k += (-9 + 256);        // second set uses registers 0x100 onwards
                        }
                        // check if this operator is part of a 4-op
                        if (((adlibreg[0x105] & 1) != 0) && op[cptr].is_4op_attached) continue;
                    }
                    else
                    {
                        cptr = cur_ch;
                    }
                    // check for FM/AM
                    if ((adlibreg[WoodyConstants.ARC_FEEDBACK + k] & 1) != 0)
                    {
                        if (_type == OplType.Opl3)
                        {
                            if (((adlibreg[0x105] & 1) != 0) && op[cptr].is_4op)
                            {
                                if ((adlibreg[WoodyConstants.ARC_FEEDBACK + k + 3] & 1) != 0)
                                {
                                    // AM-AM-style synthesis (op1[fb] + (op2 * op3) + op4)
                                    if (op[cptr].op_state != WoodyConstants.OF_TYPE_OFF)
                                    {
                                        if (op[cptr].vibrato)
                                        {
                                            vibval1 = () => vibval_var1;
                                            for (i = 0; i < endsamples; i++)
                                                vibval1()[i] = (int)((vib_lut[i] * op[cptr].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                                        }
                                        else vibval1 = () => vibval_const;
                                        if (op[cptr].tremolo) tremval1 = () => trem_lut;    // tremolo enabled, use table
                                        else tremval1 = () => tremval_const;

                                        // calculate channel output
                                        for (i = 0; i < endsamples; i++)
                                        {
                                            op[cptr].Advance(vibval1()[i]);
                                            opfuncs[op[cptr].op_state](op[cptr]);
                                            op[cptr].Output((op[cptr].lastcval + op[cptr].cval) * op[cptr].mfbi / 2, tremval1()[i]);

                                            int chanval = op[cptr].cval;
                                            ChanVal(outbufl, outbufr, cptr, i, chanval);
                                        }
                                    }

                                    if ((op[cptr + 3].op_state != WoodyConstants.OF_TYPE_OFF) || (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                                    {
                                        if ((op[cptr + 9].vibrato) && (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                                        {
                                            vibval1 = () => vibval_var1;
                                            for (i = 0; i < endsamples; i++)
                                                vibval1()[i] = (int)((vib_lut[i] * op[cptr + 9].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                                        }
                                        else vibval1 = () => vibval_const;
                                        if (op[cptr + 9].tremolo) tremval1 = () => trem_lut;    // tremolo enabled, use table
                                        else tremval1 = () => tremval_const;
                                        if (op[cptr + 3].tremolo) tremval2 = () => trem_lut;    // tremolo enabled, use table
                                        else tremval2 = () => tremval_const;

                                        // calculate channel output
                                        for (i = 0; i < endsamples; i++)
                                        {
                                            op[cptr + 9].Advance(vibval1()[i]);
                                            opfuncs[op[cptr + 9].op_state](op[cptr + 9]);
                                            op[cptr + 9].Output(0, tremval1()[i]);

                                            op[cptr + 3].Advance(0);
                                            opfuncs[op[cptr + 3].op_state](op[cptr + 3]);
                                            op[cptr + 3].Output(op[cptr + 9].cval * WoodyConstants.FIXEDPT, tremval2()[i]);

                                            int chanval = op[cptr + 3].cval;
                                            ChanVal(outbufl, outbufr, cptr, i, chanval);
                                        }
                                    }

                                    if (op[cptr + 3 + 9].op_state != WoodyConstants.OF_TYPE_OFF)
                                    {
                                        if (op[cptr + 3 + 9].tremolo) tremval1 = () => trem_lut;    // tremolo enabled, use table
                                        else tremval1 = () => tremval_const;

                                        // calculate channel output
                                        for (i = 0; i < endsamples; i++)
                                        {
                                            op[cptr + 3 + 9].Advance(0);
                                            opfuncs[op[cptr + 3 + 9].op_state](op[cptr + 3 + 9]);
                                            op[cptr + 3 + 9].Output(0, tremval1()[i]);

                                            int chanval = op[cptr + 3 + 9].cval;
                                            ChanVal(outbufl, outbufr, cptr, i, chanval);
                                        }
                                    }
                                }
                                else
                                {
                                    // AM-FM-style synthesis (op1[fb] + (op2 * op3 * op4))
                                    if (op[cptr].op_state != WoodyConstants.OF_TYPE_OFF)
                                    {
                                        if (op[cptr].vibrato)
                                        {
                                            vibval1 = () => vibval_var1;
                                            for (i = 0; i < endsamples; i++)
                                                vibval1()[i] = (int)((vib_lut[i] * op[cptr].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                                        }
                                        else vibval1 = () => vibval_const;
                                        if (op[cptr].tremolo) tremval1 = () => trem_lut;    // tremolo enabled, use table
                                        else tremval1 = () => tremval_const;

                                        // calculate channel output
                                        for (i = 0; i < endsamples; i++)
                                        {
                                            op[cptr].Advance(vibval1()[i]);
                                            opfuncs[op[cptr].op_state](op[cptr]);
                                            op[cptr].Output((op[cptr].lastcval + op[cptr].cval) * op[cptr].mfbi / 2, tremval1()[i]);

                                            int chanval = op[cptr].cval;
                                            ChanVal(outbufl, outbufr, cptr, i, chanval);
                                        }
                                    }

                                    if ((op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF) || (op[cptr + 3].op_state != WoodyConstants.OF_TYPE_OFF) || (op[cptr + 3 + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                                    {
                                        if ((op[cptr + 9].vibrato) && (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                                        {
                                            vibval1 = () => vibval_var1;
                                            for (i = 0; i < endsamples; i++)
                                                vibval1()[i] = (int)((vib_lut[i] * op[cptr + 9].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                                        }
                                        else vibval1 = () => vibval_const;
                                        if (op[cptr + 9].tremolo) tremval1 = () => trem_lut;    // tremolo enabled, use table
                                        else tremval1 = () => tremval_const;
                                        if (op[cptr + 3].tremolo) tremval2 = () => trem_lut;    // tremolo enabled, use table
                                        else tremval2 = () => tremval_const;
                                        if (op[cptr + 3 + 9].tremolo) tremval3 = () => trem_lut;    // tremolo enabled, use table
                                        else tremval3 = () => tremval_const;

                                        // calculate channel output
                                        for (i = 0; i < endsamples; i++)
                                        {
                                            op[cptr + 9].Advance(vibval1()[i]);
                                            opfuncs[op[cptr + 9].op_state](op[cptr + 9]);
                                            op[cptr + 9].Output(0, tremval1()[i]);

                                            op[cptr + 3].Advance(0);
                                            opfuncs[op[cptr + 3].op_state](op[cptr + 3]);
                                            op[cptr + 3].Output(op[cptr + 9].cval * WoodyConstants.FIXEDPT, tremval2()[i]);

                                            op[cptr + 3 + 9].Advance(0);
                                            opfuncs[op[cptr + 3 + 9].op_state](op[cptr + 3 + 9]);
                                            op[cptr + 3 + 9].Output(op[cptr + 3].cval * WoodyConstants.FIXEDPT, tremval3()[i]);

                                            int chanval = op[cptr + 3 + 9].cval;
                                            ChanVal(outbufl, outbufr, cptr, i, chanval);
                                        }
                                    }
                                }
                                continue;
                            }
                        }
                        // 2op additive synthesis
                        if ((op[cptr + 9].op_state == WoodyConstants.OF_TYPE_OFF) && (op[cptr].op_state == WoodyConstants.OF_TYPE_OFF)) continue;
                        if ((op[cptr].vibrato) && (op[cptr].op_state != WoodyConstants.OF_TYPE_OFF))
                        {
                            vibval1 = () => vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                vibval1()[i] = (int)((vib_lut[i] * op[cptr].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                        }
                        else vibval1 = () => vibval_const;
                        if ((op[cptr + 9].vibrato) && (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                        {
                            vibval2 = () => vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                vibval2()[i] = (int)((vib_lut[i] * op[cptr + 9].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                        }
                        else vibval2 = () => vibval_const;
                        if (op[cptr].tremolo) tremval1 = () => trem_lut;   // tremolo enabled, use table
                        else tremval1 = () => tremval_const;
                        if (op[cptr + 9].tremolo) tremval2 = () => trem_lut;   // tremolo enabled, use table
                        else tremval2 = () => tremval_const;

                        // calculate channel output
                        for (i = 0; i < endsamples; i++)
                        {
                            // carrier1
                            op[cptr].Advance(vibval1()[i]);
                            opfuncs[op[cptr].op_state](op[cptr]);
                            op[cptr].Output((op[cptr].lastcval + op[cptr].cval) * op[cptr].mfbi / 2, tremval1()[i]);

                            // carrier2
                            op[cptr + 9].Advance(vibval2()[i]);
                            opfuncs[op[cptr + 9].op_state](op[cptr + 9]);
                            op[cptr + 9].Output(0, tremval2()[i]);

                            int chanval = op[cptr + 9].cval + op[cptr].cval;
                            ChanVal(outbufl, outbufr, cptr, i, chanval);

                        }
                    }
                    else
                    {
                        if (_type == OplType.Opl3)
                        {
                            if (((adlibreg[0x105] & 1) != 0) && op[cptr].is_4op)
                            {
                                if ((adlibreg[WoodyConstants.ARC_FEEDBACK + k + 3] & 1) != 0)
                                {
                                    // FM-AM-style synthesis ((op1[fb] * op2) + (op3 * op4))
                                    if ((op[cptr + 0].op_state != WoodyConstants.OF_TYPE_OFF) || (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                                    {
                                        if ((op[cptr + 0].vibrato) && (op[cptr + 0].op_state != WoodyConstants.OF_TYPE_OFF))
                                        {
                                            vibval1 = () => vibval_var1;
                                            for (i = 0; i < endsamples; i++)
                                                vibval1()[i] = (int)((vib_lut[i] * op[cptr + 0].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                                        }
                                        else vibval1 = () => vibval_const;
                                        if ((op[cptr + 9].vibrato) && (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                                        {
                                            vibval2 = () => vibval_var2;
                                            for (i = 0; i < endsamples; i++)
                                                vibval2()[i] = (int)((vib_lut[i] * op[cptr + 9].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                                        }
                                        else vibval2 = () => vibval_const;
                                        if (op[cptr + 0].tremolo) tremval1 = () => trem_lut;   // tremolo enabled, use table
                                        else tremval1 = () => tremval_const;
                                        if (op[cptr + 9].tremolo) tremval2 = () => trem_lut;   // tremolo enabled, use table
                                        else tremval2 = () => tremval_const;

                                        // calculate channel output
                                        for (i = 0; i < endsamples; i++)
                                        {
                                            op[cptr + 0].Advance(vibval1()[i]);
                                            opfuncs[op[cptr + 0].op_state](op[cptr + 0]);
                                            op[cptr + 0].Output((op[cptr + 0].lastcval + op[cptr + 0].cval) * op[cptr + 0].mfbi / 2, tremval1()[i]);

                                            op[cptr + 9].Advance(vibval2()[i]);
                                            opfuncs[op[cptr + 9].op_state](op[cptr + 9]);
                                            op[cptr + 9].Output(op[cptr + 0].cval * WoodyConstants.FIXEDPT, tremval2()[i]);

                                            int chanval = op[cptr + 9].cval;
                                            ChanVal(outbufl, outbufr, cptr, i, chanval);


                                        }
                                    }

                                    if ((op[cptr + 3].op_state != WoodyConstants.OF_TYPE_OFF) || (op[cptr + 3 + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                                    {
                                        if (op[cptr + 3].tremolo) tremval1 = () => trem_lut;   // tremolo enabled, use table
                                        else tremval1 = () => tremval_const;
                                        if (op[cptr + 3 + 9].tremolo) tremval2 = () => trem_lut;   // tremolo enabled, use table
                                        else tremval2 = () => tremval_const;

                                        // calculate channel output
                                        for (i = 0; i < endsamples; i++)
                                        {
                                            op[cptr + 3].Advance(0);
                                            opfuncs[op[cptr + 3].op_state](op[cptr + 3]);
                                            op[cptr + 3].Output(0, tremval1()[i]);

                                            op[cptr + 3 + 9].Advance(0);
                                            opfuncs[op[cptr + 3 + 9].op_state](op[cptr + 3 + 9]);
                                            op[cptr + 3 + 9].Output(op[cptr + 3].cval * WoodyConstants.FIXEDPT, tremval2()[i]);

                                            int chanval = op[cptr + 3 + 9].cval;
                                            ChanVal(outbufl, outbufr, cptr, i, chanval);


                                        }
                                    }

                                }
                                else
                                {
                                    // FM-FM-style synthesis (op1[fb] * op2 * op3 * op4)
                                    if ((op[cptr + 0].op_state != WoodyConstants.OF_TYPE_OFF) || (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF) ||
                                        (op[cptr + 3].op_state != WoodyConstants.OF_TYPE_OFF) || (op[cptr + 3 + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                                    {
                                        if ((op[cptr + 0].vibrato) && (op[cptr + 0].op_state != WoodyConstants.OF_TYPE_OFF))
                                        {
                                            vibval1 = () => vibval_var1;
                                            for (i = 0; i < endsamples; i++)
                                                vibval1()[i] = (int)((vib_lut[i] * op[cptr + 0].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                                        }
                                        else vibval1 = () => vibval_const;
                                        if ((op[cptr + 9].vibrato) && (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                                        {
                                            vibval2 = () => vibval_var2;
                                            for (i = 0; i < endsamples; i++)
                                                vibval2()[i] = (int)((vib_lut[i] * op[cptr + 9].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                                        }
                                        else vibval2 = () => vibval_const;
                                        if (op[cptr + 0].tremolo) tremval1 = () => trem_lut;   // tremolo enabled, use table
                                        else tremval1 = () => tremval_const;
                                        if (op[cptr + 9].tremolo) tremval2 = () => trem_lut;   // tremolo enabled, use table
                                        else tremval2 = () => tremval_const;
                                        if (op[cptr + 3].tremolo) tremval3 = () => trem_lut;   // tremolo enabled, use table
                                        else tremval3 = () => tremval_const;
                                        if (op[cptr + 3 + 9].tremolo) tremval4 = () => trem_lut;   // tremolo enabled, use table
                                        else tremval4 = () => tremval_const;

                                        // calculate channel output
                                        for (i = 0; i < endsamples; i++)
                                        {
                                            op[cptr + 0].Advance(vibval1()[i]);
                                            opfuncs[op[cptr + 0].op_state](op[cptr + 0]);
                                            op[cptr + 0].Output((op[cptr + 0].lastcval + op[cptr + 0].cval) * op[cptr + 0].mfbi / 2, tremval1()[i]);

                                            op[cptr + 9].Advance(vibval2()[i]);
                                            opfuncs[op[cptr + 9].op_state](op[cptr + 9]);
                                            op[cptr + 9].Output(op[cptr + 0].cval * WoodyConstants.FIXEDPT, tremval2()[i]);

                                            op[cptr + 3].Advance(0);
                                            opfuncs[op[cptr + 3].op_state](op[cptr + 3]);
                                            op[cptr + 3].Output(op[cptr + 9].cval * WoodyConstants.FIXEDPT, tremval3()[i]);

                                            op[cptr + 3 + 9].Advance(0);
                                            opfuncs[op[cptr + 3 + 9].op_state](op[cptr + 3 + 9]);
                                            op[cptr + 3 + 9].Output(op[cptr + 3].cval * WoodyConstants.FIXEDPT, tremval4()[i]);

                                            int chanval = op[cptr + 3 + 9].cval;
                                            ChanVal(outbufl, outbufr, cptr, i, chanval);


                                        }
                                    }
                                }
                                continue;
                            }
                        }
                        // 2op frequency modulation
                        if ((op[cptr + 9].op_state == WoodyConstants.OF_TYPE_OFF) && (op[cptr].op_state == WoodyConstants.OF_TYPE_OFF)) continue;
                        if ((op[cptr].vibrato) && (op[cptr].op_state != WoodyConstants.OF_TYPE_OFF))
                        {
                            vibval1 = () => vibval_var1;
                            for (i = 0; i < endsamples; i++)
                                vibval1()[i] = (int)((vib_lut[i] * op[cptr].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                        }
                        else vibval1 = () => vibval_const;
                        if ((op[cptr + 9].vibrato) && (op[cptr + 9].op_state != WoodyConstants.OF_TYPE_OFF))
                        {
                            vibval2 = () => vibval_var2;
                            for (i = 0; i < endsamples; i++)
                                vibval2()[i] = (int)((vib_lut[i] * op[cptr + 9].freq_high / 8) * WoodyConstants.FIXEDPT * WoodyConstants.VIBFAC);
                        }
                        else vibval2 = () => vibval_const;
                        if (op[cptr].tremolo) tremval1 = () => trem_lut;   // tremolo enabled, use table
                        else tremval1 = () => tremval_const;
                        if (op[cptr + 9].tremolo) tremval2 = () => trem_lut;   // tremolo enabled, use table
                        else tremval2 = () => tremval_const;

                        // calculate channel output
                        for (i = 0; i < endsamples; i++)
                        {
                            // modulator
                            op[cptr].Advance(vibval1()[i]);
                            opfuncs[op[cptr].op_state](op[cptr]);
                            op[cptr].Output((op[cptr].lastcval + op[cptr].cval) * op[cptr].mfbi / 2, tremval1()[i]);

                            // carrier
                            op[cptr + 9].Advance(vibval2()[i]);
                            opfuncs[op[cptr + 9].op_state](op[cptr + 9]);
                            op[cptr + 9].Output(op[cptr].cval * WoodyConstants.FIXEDPT, tremval2()[i]);

                            int chanval = op[cptr + 9].cval;
                            ChanVal(outbufl, outbufr, cptr, i, chanval);
                        }
                    }
                }

                if (_type == OplType.Opl3)
                {
                    if ((adlibreg[0x105] & 1) != 0)
                    {
                        if (int_numsamplechannels == 1)
                        {
                            if (int_bytespersample == 1)
                            {
                                for (i = 0; i < endsamples; i++)
                                {
                                    data[pos + sndptr1++] = Clipit8((outbufl[i] + outbufr[i]) / 2);
                                }
                            }
                            else
                            {
                                for (i = 0; i < endsamples; i++)
                                {
                                    data[pos + sndptr++] = Clipit16((outbufl[i] + outbufr[i]) / 2);
                                }
                            }
                        }
                        else
                        {
                            if (int_bytespersample == 1)
                            {
                                for (i = 0; i < endsamples; i++)
                                {
                                    data[pos + sndptr1++] = Clipit8(outbufl[i]);
                                    data[pos + sndptr1++] = Clipit8(outbufr[i]);
                                }
                            }
                            else
                            {
                                for (i = 0; i < endsamples; i++)
                                {
                                    data[pos + sndptr++] = Clipit16(outbufl[i]);
                                    data[pos + sndptr++] = Clipit16(outbufr[i]);
                                }
                            }
                        }
                        continue;
                    }
                }

                if (int_numsamplechannels == 1)
                {
                    if (int_bytespersample == 1)
                    {
                        for (i = 0; i < endsamples; i++)
                        {
                            data[pos + sndptr1++] = Clipit8(outbufl[i]);
                        }
                    }
                    else
                    {
                        for (i = 0; i < endsamples; i++)
                        {
                            data[pos + sndptr++] = Clipit16(outbufl[i]);
                        }
                    }
                }
                else
                {
                    if (int_bytespersample == 1)
                    {
                        for (i = 0; i < endsamples; i++)
                        {
                            data[pos + sndptr1++] = Clipit8(outbufl[i]);
                            data[pos + sndptr1++] = Clipit8(outbufl[i]);
                        }
                    }
                    else
                    {
                        for (i = 0; i < endsamples; i++)
                        {
                            data[pos + sndptr++] = Clipit16(outbufl[i]);
                            data[pos + sndptr++] = Clipit16(outbufl[i]);
                        }
                    }
                }
            }
        }

        // be careful with this
        // uses cptr and chanval, outputs into outbufl(/outbufr)
        // for opl3 check if opl3-mode is enabled (which uses stereo panning)
        private void ChanVal(int[] outbufl, int[] outbufr, int cptr, int i, int chanval)
        {
            if (_type == OplType.Opl3)
            {
                if ((adlibreg[0x105] & 1) != 0)
                {
                    outbufl[i] += chanval * op[cptr].left_pan;
                    outbufr[i] += chanval * op[cptr].right_pan;
                }
                else
                {
                    outbufl[i] += chanval;
                }
                return;
            }
            outbufl[i] += chanval;
        }
    }
}
