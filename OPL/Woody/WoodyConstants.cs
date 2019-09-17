//
//  WoodyConstants.cs
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

namespace NScumm.Audio.OPL.Woody
{
    internal static class WoodyConstants
    {
        public const double FL05 = 0.5;
        public const double FL2 = 2.0;

        public const int FIXEDPT = 0x10000;     // fixed-point calculations using 16+16
        public const int FIXEDPT_LFO = 0x1000000;   // fixed-point calculations using 8+24

        public const int WAVEPREC = 1024;       // waveform precision (10 bits)

        public const double INTFREQU = 14318180.0 / 288.0;      // clocking of the chip


        public const int OF_TYPE_ATT = 0;
        public const int OF_TYPE_DEC = 1;
        public const int OF_TYPE_REL = 2;
        public const int OF_TYPE_SUS = 3;
        public const int OF_TYPE_SUS_NOKEEP = 4;
        public const int OF_TYPE_OFF = 5;

        public const int ARC_CONTROL = 0x00;
        public const int ARC_TVS_KSR_MUL = 0x20;
        public const int ARC_KSL_OUTLEV = 0x40;
        public const int ARC_ATTR_DECR = 0x60;
        public const int ARC_SUSL_RELR = 0x80;
        public const int ARC_FREQ_NUM = 0xa0;
        public const int ARC_KON_BNUM = 0xb0;
        public const int ARC_PERC_MODE = 0xbd;
        public const int ARC_FEEDBACK = 0xc0;
        public const int ARC_WAVE_SEL = 0xe0;

        public const int ARC_SECONDSET = 0x100; // second operator set for OPL3


        public const int OP_ACT_OFF = 0x00;
        public const int OP_ACT_NORMAL = 0x01;  // regular channel activated (bitmasked)
        public const int OP_ACT_PERC = 0x02;    // percussion channel activated (bitmasked)

        public const int BLOCKBUF_SIZE = 512;


        // vibrato constants
        public const int VIBTAB_SIZE = 8;
        public const double VIBFAC = 70 / 50000;        // no braces, integer mul/div

        // tremolo constants and table
        public const int TREMTAB_SIZE = 53;
        public const double TREM_FREQ = 3.7;			// tremolo at 3.7hz
    }
}
