//
//  WoodyEmulatorOpl.cs
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
using NScumm.Core.Audio.OPL;

namespace NScumm.Audio.OPL.Woody
{
    /// <summary>
    /// This code has been adapted from adplug https://github.com/adplug/adplug
    /// </summary>
    public sealed class WoodyEmulatorOpl : IOpl
    {
        public bool IsStereo => false;

        public WoodyEmulatorOpl(OplType type)
        {
            _type = type;
        }

        public void ReadBuffer(short[] buf, int pos, int samples)
        {
            _opl.AdlibGetSample(buf, pos, samples);
        }

        public void WriteReg(int reg, int val)
        {
            _opl.AdlibWrite(reg, (byte)val);
        }

        public void Init(int rate)
        {
            _opl = new OPLChipClass(_type);
            _opl.AdlibInit(rate, 1, 2);
        }

        private OPLChipClass _opl;
        private readonly OplType _type;
    }
}
