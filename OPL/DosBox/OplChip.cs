//
//  OplChip.cs
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

#define DBOPL_WAVE_EQUALS_WAVE_TABLEMUL

using System;

namespace NScumm.Core.Audio.OPL.DosBox
{

    class OplChip
    {
        //Last selected register
        readonly OplTimer[] timer = new OplTimer[2];

        //Check for it being a write to the timer
        public bool Write(uint reg, byte val)
        {
            switch (reg)
            {
                case 0x02:
                    timer[0].Counter = val;
                    return true;
                case 0x03:
                    timer[1].Counter = val;
                    return true;
                case 0x04:
                    double time = Environment.TickCount / 1000.0;

                    if ((val & 0x80) != 0)
                    {
                        timer[0].Reset(time);
                        timer[1].Reset(time);
                    }
                    else
                    {
                        timer[0].Update(time);
                        timer[1].Update(time);

                        if ((val & 0x1) != 0)
                            timer[0].Start(time, 80);
                        else
                            timer[0].Stop();

                        timer[0].Masked = (val & 0x40) > 0;

                        if (timer[0].Masked)
                            timer[0].Overflow = false;

                        if ((val & 0x2) != 0)
                            timer[1].Start(time, 320);
                        else
                            timer[1].Stop();

                        timer[1].Masked = (val & 0x20) > 0;

                        if (timer[1].Masked)
                            timer[1].Overflow = false;
                    }
                    return true;
            }
            return false;
        }
        //Read the current timer state, will use current double
        public byte Read()
        {
            double time = Environment.TickCount / 1000.0;

            timer[0].Update(time);
            timer[1].Update(time);

            byte ret = 0;
            // Overflow won't be set if a channel is masked
            if (timer[0].Overflow)
            {
                ret |= 0x40;
                ret |= 0x80;
            }
            if (timer[1].Overflow)
            {
                ret |= 0x20;
                ret |= 0x80;
            }
            return ret;
        }
    }
    
}
