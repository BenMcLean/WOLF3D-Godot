using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOLF3D.WOLF3DGame
{
    public abstract class Room : Spatial
    {
        public virtual bool IsPaused() => false;
        public virtual ARVROrigin ARVROrigin { get; set; }
        public virtual ARVRCamera ARVRCamera { get; set; }
        public virtual ARVRController LeftController { get; set; }
        public virtual ARVRController RightController { get; set; }
        public virtual void Enter()
        {
            ARVRCamera.Current = true;
        }
        public virtual void Exit() { }

        public static bool IsVRButton(int buttonIndex)
        {
            switch (buttonIndex)
            {
                case (int)JoystickList.VrGrip:
                case (int)JoystickList.VrPad:
                case (int)JoystickList.VrAnalogGrip:
                case (int)JoystickList.VrTrigger:
                case (int)JoystickList.OculusAx:
                case (int)JoystickList.OculusBy:
                case (int)JoystickList.OculusMenu:
                    return true;
                default:
                    return false;
            }
        }
    }
}
