using Godot;

namespace WOLF3D.WOLF3DGame
{
	public abstract class Room : Spatial
	{
		public bool Paused
		{
			get => paused;
			set
			{
				if (paused = value)
					OnPause();
				else
					OnUnpause();
			}
		}
		private bool paused = true;

		public virtual void OnPause() { }
		public virtual void OnUnpause() { }
		public virtual ARVROrigin ARVROrigin { get; set; }
		public virtual FadeCamera ARVRCamera { get; set; }
		public virtual ARVRController LeftController { get; set; }
		public virtual ARVRController RightController { get; set; }
		public virtual ARVRController Controller(bool left) => left ? LeftController : RightController;
		public virtual ARVRController Controller(int which) => Controller(which == 0);
		public virtual ARVRController OtherController(ARVRController aRVRController) => aRVRController == LeftController ? RightController : LeftController;
		public virtual void Enter()
		{
			ARVRCamera.Current = true;
			NewRoom = null;
			Main.Brightness = 0f;
			FadeProgress = 0f;
			Paused = true;
		}
		public virtual void Exit() { }
		public virtual void FinishedFadeIn()
		{
			Main.Brightness = 1f;
			Paused = false;
		}

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

		public virtual Room NewRoom { get; set; } = null;

		public const float FadeSeconds = 0.5f;
		public float FadeProgress = 0f;

		public virtual void ChangeRoom(Room room)
		{
			Paused = true;
			room.Paused = true;
			FadeProgress = 0f;
			NewRoom = room;
		}

		public virtual void PausedProcess(float delta)
		{
			FadeProgress += delta;
			if (FadeProgress > FadeSeconds)
				if (NewRoom == null)
				{
					FinishedFadeIn();
					return;
				}
				else
				{
					Main.Brightness = 0f;
					Main.Room = NewRoom;
					return;
				}
			Main.Brightness = NewRoom == null ?
				FadeProgress / FadeSeconds
				: (FadeSeconds - FadeProgress) / FadeSeconds;
		}
	}
}
