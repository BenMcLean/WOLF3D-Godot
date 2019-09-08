//
//  OplTimer.cs
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
    class OplTimer
    {
        const double Epsilon = 0.1;

        public double StartTime { get; private set; }

        public double Delay { get; private set; }

        public bool Enabled { get; private set; }

        public bool Overflow { get; set; }

        public bool Masked { get; set; }

        public byte Counter { get; set; }

        //Call update before making any further changes
        public void Update(double time)
        {
            if (!Enabled || Math.Abs(Delay) < Epsilon)
                return;
            double deltaStart = time - StartTime;
            // Only set the overflow flag when not masked
            if (deltaStart >= 0 && !Masked)
                Overflow = true;
        }

        //On a reset make sure the start is in sync with the next cycle
        public void Reset(double time)
        {
            Overflow = false;
            if (Math.Abs(Delay) < Epsilon || !Enabled)
                return;
            double delta = (time - StartTime);
//            double rem = fmod(delta, delay);
            double rem = delta % Delay;
            double next = Delay - rem;
            StartTime = time + next;
        }

        public void Stop()
        {
            Enabled = false;
        }

        public void Start(double time, int scale)
        {
            //Don't enable again
            if (Enabled)
                return;
            Enabled = true;
            Delay = 0.001 * (256 - Counter) * scale;
            StartTime = time + Delay;
        }
    }
    
}
