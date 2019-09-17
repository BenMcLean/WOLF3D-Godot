//
//  Operator.cs
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

namespace NScumm.Audio.OPL.Woody
{

    /// <summary>
    /// For OPL2 all 9 channels consist of two operators each, carrier and modulator.
    /// Channel x has operators x as modulator and operators(9+x) as carrier.
    /// For OPL3 all 18 channels consist either of two operators(2op mode) or four
    /// operators(4op mode) which is determined through register4 of the second
    /// adlib register set.
    /// Only the channels 0,1,2 (first set) and 9,10,11 (second set) can act as
    /// 4op channels.The two additional operators for a channel y come from the
    /// 2op channel y+3 so the operatorss y, (9+y), y+3, (9+y)+3 make up a 4op
    /// channel.
    /// </summary>
    internal sealed class Operator
    {
        /// <summary>
        /// current output/last output (used for feedback)
        /// </summary>
        public int cval, lastcval;
        /// <summary>
        /// time(position in waveform) and time increment
        /// </summary>
        public int tcount, wfpos, tinc;
        public double amp, step_amp;
        public double vol;                     // volume
        public double sustain_level;           // sustain level
        public int mfbi;                    // feedback amount
        public double a0, a1, a2, a3;          // attack rate function coefficients
        public double decaymul, releasemul;    // decay/release rate functions
        public int op_state;                // current state of operator (attack/decay/sustain/release/off)
        public int toff;
        public int freq_high;               // highest three bits of the frequency, used for vibrato calculations
        public int cur_wform;              // start of selected waveform
        public int cur_wmask;               // mask for selected waveform
        public int act_state;               // activity state (regular, percussion)
        public bool sus_keep;                  // keep sustain level when decay finished
        public bool vibrato, tremolo;          // vibrato/tremolo enable bits

        // variables used to provide non-continuous envelopes
        public int generator_pos;           // for non-standard sample rates we need to determine how many samples have passed
        public int cur_env_step;              // current (standardized) sample position
        public int env_step_a, env_step_d, env_step_r;    // number of std samples of one step (for attack/decay/release mode)
        public byte step_skip_pos_a;          // position of 8-cyclic step skipping (always 2^x to check against mask)
        public int env_step_skip_a;            // bitmask that determines if a step is skipped (respective bit is zero then)

        // OPL3
        public bool is_4op, is_4op_attached;   // base of a 4op channel/part of a 4op channel
        public int left_pan, right_pan;		// opl3 stereo panning amount

        public void DisableOperator(int act_type)
        {
            // check if this is really an on-off transition
            if (act_state != WoodyConstants.OP_ACT_OFF)
            {
                act_state &= (~act_type);
                if (act_state == WoodyConstants.OP_ACT_OFF)
                {
                    if (op_state != WoodyConstants.OF_TYPE_OFF) op_state = WoodyConstants.OF_TYPE_REL;
                }
            }
        }

        public void Advance(int vib)
        {
            wfpos = tcount;                       // waveform position

            // advance waveform time
            tcount += tinc;
            tcount += tinc * vib / WoodyConstants.FIXEDPT;

            generator_pos += OPLChipClass.generator_add;
        }

        // output level is sustained, mode changes only when operator is turned off (->release)
        // or when the keep-sustained bit is turned off (->sustain_nokeep)
        public void Output(int modulator, int trem)
        {
            if (op_state != WoodyConstants.OF_TYPE_OFF)
            {
                lastcval = cval;
                var i = ((wfpos + modulator) / WoodyConstants.FIXEDPT);

                // wform: -16384 to 16383 (0x4000)
                // trem :  32768 to 65535 (0x10000)
                // step_amp: 0.0 to 1.0
                // vol  : 1/2^14 to 1/2^29 (/0x4000; /1../0x8000)

                cval = (int)(step_amp * vol * OPLChipClass.wavtable[cur_wform + (i & cur_wmask)] * trem / 16.0);
            }
        }

        // no action, operator is off
        public void Off()
        {
        }

        /// <summary>
        /// output level is sustained, mode changes only when operator is turned off (->release)
        /// or when the keep-sustained bit is turned off (->sustain_nokeep)
        /// </summary>
        public void Sustain()
        {
            int num_steps_add = generator_pos / WoodyConstants.FIXEDPT;  // number of (standardized) samples
            for (var ct = 0; ct < num_steps_add; ct++)
            {
                cur_env_step++;
            }
            generator_pos -= num_steps_add * WoodyConstants.FIXEDPT;
        }

        /// <summary>
        /// operator in release mode, if output level reaches zero the operator is turned off
        /// </summary>
        public void Release()
        {
            // ??? boundary?
            if (amp > 0.00000001)
            {
                // release phase
                amp *= releasemul;
            }

            int num_steps_add = generator_pos / WoodyConstants.FIXEDPT;  // number of (standardized) samples
            for (var ct = 0; ct < num_steps_add; ct++)
            {
                cur_env_step++;                      // sample counter
                if ((cur_env_step & env_step_r) == 0)
                {
                    if (amp <= 0.00000001)
                    {
                        // release phase finished, turn off this operator
                        amp = 0.0;
                        if (op_state == WoodyConstants.OF_TYPE_REL)
                        {
                            op_state = WoodyConstants.OF_TYPE_OFF;
                        }
                    }
                    step_amp = amp;
                }
            }
            generator_pos -= num_steps_add * WoodyConstants.FIXEDPT;
        }

        /// <summary>
        /// operator in decay mode, if sustain level is reached the output level is either
        /// kept (sustain level keep enabled) or the operator is switched into release mode
        /// </summary>
        public void Decay()
        {
            if (amp > sustain_level)
            {
                // decay phase
                amp *= decaymul;
            }

            int num_steps_add = generator_pos / WoodyConstants.FIXEDPT;  // number of (standardized) samples
            for (var ct = 0; ct < num_steps_add; ct++)
            {
                cur_env_step++;
                if ((cur_env_step & env_step_d) == 0)
                {
                    if (amp <= sustain_level)
                    {
                        // decay phase finished, sustain level reached
                        if (sus_keep)
                        {
                            // keep sustain level (until turned off)
                            op_state = WoodyConstants.OF_TYPE_SUS;
                            amp = sustain_level;
                        }
                        else
                        {
                            // next: release phase
                            op_state = WoodyConstants.OF_TYPE_SUS_NOKEEP;
                        }
                    }
                    step_amp = amp;
                }
            }
            generator_pos -= num_steps_add * WoodyConstants.FIXEDPT;
        }

        /// <summary>
        /// operator in attack mode, if full output level is reached,
        /// the operator is switched into decay mode
        /// </summary>
        public void Attack()
        {
            amp = ((a3 * amp + a2) * amp + a1) * amp + a0;

            int num_steps_add = generator_pos / WoodyConstants.FIXEDPT;      // number of (standardized) samples
            for (var ct = 0; ct < num_steps_add; ct++)
            {
                cur_env_step++;  // next sample
                if ((cur_env_step & env_step_a) == 0)
                {       // check if next step already reached
                    if (amp > 1.0)
                    {
                        // attack phase finished, next: decay
                        op_state = WoodyConstants.OF_TYPE_DEC;
                        amp = 1.0;
                        step_amp = 1.0;
                    }
                    step_skip_pos_a <<= 1;
                    if (step_skip_pos_a == 0) step_skip_pos_a = 1;
                    if (step_skip_pos_a != 0 & env_step_skip_a != 0)
                    {   // check if required to skip next step
                        step_amp = amp;
                    }
                }
            }
            generator_pos -= num_steps_add * WoodyConstants.FIXEDPT;
        }

        private static readonly Random random = new Random();

        public static void AdvanceDrums(Operator op_pt1, int vib1, Operator op_pt2, int vib2, Operator op_pt3, int vib3)
        {
            int c1 = op_pt1.tcount / WoodyConstants.FIXEDPT;
            int c3 = op_pt3.tcount / WoodyConstants.FIXEDPT;
            int phasebit = (((c1 & 0x88) ^ ((c1 << 5) & 0x80)) | ((c3 ^ (c3 << 2)) & 0x20))!=0 ? 0x02 : 0x00;

            int noisebit = random.Next() & 1;

            int snare_phase_bit = (((int)((op_pt1.tcount / WoodyConstants.FIXEDPT) / 0x100)) & 1);

            //Hihat
            int inttm = (phasebit << 8) | (0x34 << (phasebit ^ (noisebit << 1)));
            op_pt1.wfpos = inttm * WoodyConstants.FIXEDPT;                // waveform position
                                                            // advance waveform time
            op_pt1.tcount += op_pt1.tinc;
            op_pt1.tcount += op_pt1.tinc * vib1 / WoodyConstants.FIXEDPT;
            op_pt1.generator_pos += OPLChipClass.generator_add;

            //Snare
            inttm = ((1 + snare_phase_bit) ^ noisebit) << 8;
            op_pt2.wfpos = inttm * WoodyConstants.FIXEDPT;                // waveform position
                                                            // advance waveform time
            op_pt2.tcount += op_pt2.tinc;
            op_pt2.tcount += op_pt2.tinc * vib2 / WoodyConstants.FIXEDPT;
            op_pt2.generator_pos += OPLChipClass.generator_add;

            //Cymbal
            inttm = (1 + phasebit) << 8;
            op_pt3.wfpos = inttm * WoodyConstants.FIXEDPT;                // waveform position
                                                            // advance waveform time
            op_pt3.tcount += op_pt3.tinc;
            op_pt3.tcount += op_pt3.tinc * vib3 / WoodyConstants.FIXEDPT;
            op_pt3.generator_pos += OPLChipClass.generator_add;
        }
    }
}
