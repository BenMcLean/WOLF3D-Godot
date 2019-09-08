//
//  DosBoxOPL_Operator.cs
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

//Use 8 handlers based on a small logatirmic wavetabe and an exponential table for volume
#define WAVE_HANDLER
//Use a logarithmic wavetable with an exponential table for volume
#define WAVE_TABLELOG
//Use a linear wavetable with a multiply table for volume
#define WAVE_TABLEMUL

//Select the type of wave generator routine
#define DBOPL_WAVE_EQUALS_WAVE_TABLEMUL

using System;

namespace NScumm.Core.Audio.OPL.DosBox
{
    partial class DosBoxOPL
    {
        const double OPLRATE = (14318180.0 / 288.0);
        const int TremoloTableLength = 52;

        //Try to use most precision for frequencies
        //Else try to keep different waves in synch
        //#define WAVE_PRECISION;
        //Wave bits available in the top of the 32bit range
        //Original adlib uses 10.10, we use 10.22
        const int WaveBits = 10;

        const int WaveShift = (32 - WaveBits);
        const int WaveMask = ((1 << WaveShift) - 1);

        //Use the same accuracy as the waves
        const int LfoShift = (WaveShift - 10);
        //LFO is controlled by our tremolo 256 sample limit
        const int LfoMax = (256 << (LfoShift));

        //Maximum amount of attenuation bits
        //Envelope goes to 511, 9 bits

        //Uses the value directly
        public const int EnvBits = 9;

        //Limits of the envelope with those bits and when the envelope goes silent
        public const int EnvMin = 0;
        public const int EnvExtra = (EnvBits - 9);
        public const int EnvMax = (511 << EnvExtra);
        public const int EnvLimit = ((12 * 256) >> (3 - EnvExtra));

        public static bool EnvSilent(int x)
        {
            return x >= EnvLimit;
        }

        //Attack/decay/release rate counter shift
        const int RateShift = 24;
        const int RateMask = ((1 << RateShift) - 1);

        //Has to fit within 16bit lookuptable
        const int MulShift = 16;

        delegate int VolumeHandler();

        delegate Channel SynthHandler(Chip chip,uint samples,int[] output,int pos);

        #if ( DBOPL_WAVE_EQUALS_WAVE_HANDLER ) || ( DBOPL_WAVE_EQUALS_WAVE_TABLELOG )
        static ushort[] ExpTable = new ushort[256];
        #endif

        #if DBOPL_WAVE_EQUALS_WAVE_HANDLER
        static ushort[] SinTable = new ushort[512];
        #endif

        static ushort[] mulTable = new ushort[384];

        //Layout of the waveform table in 512 entry intervals
        //With overlapping waves we reduce the table to half it's size

        //  |    |//\\|____|WAV7|//__|/\  |____|/\/\|
        //  |\\//|    |    |WAV7|    |  \/|    |    |
        //  |06  |0126|17  |7   |3   |4   |4 5 |5   |

        //6 is just 0 shifted and masked

        static short[] waveTable = new short[8 * 512];
        //Distance into WaveTable the wave starts
        static readonly ushort[] WaveBaseTable =
            {
                0x000, 0x200, 0x200, 0x800,
                0xa00, 0xc00, 0x100, 0x400,
            };
        //Where to start the counter on at keyon
        static readonly ushort[] WaveStartTable =
            {
                512, 0, 0, 0,
                0, 512, 512, 256,
            };
        //Mask the counter with this
        static readonly ushort[] WaveMaskTable =
            {
                1023, 1023, 511, 511,
                1023, 1023, 512, 1023,
            };

        static byte[] kslTable = new byte[8 * 16];

        //How much to substract from the base value for the final attenuation
        static readonly byte[] KslCreateTable =
            {
                //0 will always be be lower than 7 * 8
                64, 32, 24, 19,
                16, 12, 11, 10,
                8,  6,  5,  4,
                3,  2,  1,  0,
            };

        static byte[] tremoloTable = new byte[ TremoloTableLength ];
        //Start of a channel behind the chip struct start
        static Func<Chip,Channel>[] chanOffsetTable = new Func<Chip,Channel>[32];
        //Start of an operator behind the chip struct start
        static Func<Chip,Operator>[] opOffsetTable = new Func<Chip,Operator>[64];

        static byte M(double x)
        {
            return (byte)(x * 2);
        }

        static readonly byte[] FreqCreateTable =
            {
                M(0.5), M(1), M(2), M(3), M(4), M(5), M(6), M(7),
                M(8), M(9), M(10), M(10), M(12), M(12), M(15), M(15)
            };

        //Generate a table index and table shift value using input value from a selected rate
        static void EnvelopeSelect(byte val, out byte index, out byte shift)
        {
            if (val < 13 * 4)
            {               //Rate 0 - 12
                shift = (byte)(12 - (val >> 2));
                index = (byte)(val & 3);
            }
            else if (val < 15 * 4)
            {        //rate 13 - 14
                shift = 0;
                index = (byte)(val - 12 * 4);
            }
            else
            {                            //rate 15 and up
                shift = 0;
                index = 12;
            }
        }

        //On a real opl these values take 8 samples to reach and are based upon larger tables
        static readonly byte[] EnvelopeIncreaseTable =
            {
                4,  5,  6,  7,
                8, 10, 12, 14,
                16, 20, 24, 28,
                32,
            };

        //We're not including the highest attack rate, that gets a special value
        static readonly byte[] AttackSamplesTable =
            {
                69, 55, 46, 40,
                35, 29, 23, 20,
                19, 15, 11, 10,
                9
            };

        //Different synth modes that can generate blocks of data
        enum SynthMode
        {
            Sm2AM,
            Sm2FM,
            Sm3AM,
            Sm3FM,
            Sm4Start,
            Sm3FMFM,
            Sm3AMFM,
            Sm3FMAM,
            Sm3AMAM,
            Sm6Start,
            Sm2Percussion,
            Sm3Percussion
        }
    }
}

