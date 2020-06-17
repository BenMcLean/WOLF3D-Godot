using Godot;

namespace WOLF3D.WOLF3DGame
{
    public abstract class Room : Spatial
    {
        public virtual bool IsPaused() => false;
        public virtual ARVROrigin ARVROrigin { get; set; }
        public virtual ARVRCamera ARVRCamera { get; set; }
        public virtual ARVRController LeftController { get; set; }
        public virtual ARVRController RightController { get; set; }
        public virtual ARVRController Controller(bool left) => left ? LeftController : RightController;
        public virtual ARVRController Controller(int which) => Controller(which == 0);
        public virtual ARVRController OtherController(ARVRController aRVRController) => aRVRController == LeftController ? RightController : LeftController;
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
