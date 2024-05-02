using InControl;

namespace BetterLegacy.Core.Data.Player
{
    public class FaceController : PlayerActionSet
	{
		public FaceController()
		{
			Up = CreatePlayerAction("Move Up");
			Down = CreatePlayerAction("Move Down");
			Left = CreatePlayerAction("Move Left");
			Right = CreatePlayerAction("Move Right");
			Shoot = CreatePlayerAction("Shoot");
			Sneak = CreatePlayerAction("Sneak");
			Sprint = CreatePlayerAction("Sprint");

			Move = CreateTwoAxisPlayerAction(Left, Right, Down, Up);
			Up.StateThreshold = 0.3f;
			Down.StateThreshold = 0.3f;
			Left.StateThreshold = 0.3f;
			Right.StateThreshold = 0.3f;
		}

		public static InputControlType ShootControl { get; set; }
		public static Key ShootKey { get; set; }

		public static FaceController CreateWithBothBindings()
		{
			var faceController = new FaceController();
			faceController.Up.AddDefaultBinding(InputControlType.RightStickUp);
			faceController.Down.AddDefaultBinding(InputControlType.RightStickDown);
			faceController.Left.AddDefaultBinding(InputControlType.RightStickLeft);
			faceController.Right.AddDefaultBinding(InputControlType.RightStickRight);
			faceController.Up.AddDefaultBinding(Key.I);
			faceController.Down.AddDefaultBinding(Key.K);
			faceController.Left.AddDefaultBinding(Key.J);
			faceController.Right.AddDefaultBinding(Key.L);
			faceController.Shoot.AddDefaultBinding(ShootControl);
			faceController.Shoot.AddDefaultBinding(ShootKey);
			faceController.Sneak.AddDefaultBinding(Key.Shift);
			faceController.Sprint.AddDefaultBinding(Key.Control);
			faceController.Sneak.AddDefaultBinding(InputControlType.LeftStickButton);
			faceController.Sprint.AddDefaultBinding(InputControlType.RightTrigger);

			return faceController;
		}

		public static FaceController CreateWithJoystickBindings()
		{
			var faceController = new FaceController();
			faceController.Up.AddDefaultBinding(InputControlType.RightStickUp);
			faceController.Down.AddDefaultBinding(InputControlType.RightStickDown);
			faceController.Left.AddDefaultBinding(InputControlType.RightStickLeft);
			faceController.Right.AddDefaultBinding(InputControlType.RightStickRight);
			faceController.Shoot.AddDefaultBinding(ShootControl);
			faceController.Sneak.AddDefaultBinding(InputControlType.LeftStickButton);
			faceController.Sprint.AddDefaultBinding(InputControlType.RightTrigger);

			return faceController;
		}

		public static FaceController CreateWithKeyboardBindings()
		{
			var faceController = new FaceController();
			faceController.Up.AddDefaultBinding(Key.I);
			faceController.Down.AddDefaultBinding(Key.K);
			faceController.Left.AddDefaultBinding(Key.J);
			faceController.Right.AddDefaultBinding(Key.L);
			faceController.Shoot.AddDefaultBinding(ShootKey);
			faceController.Sneak.AddDefaultBinding(Key.Shift);
			faceController.Sprint.AddDefaultBinding(Key.Control);
			return faceController;
		}

		public PlayerAction Up;

		public PlayerAction Down;

		public PlayerAction Left;

		public PlayerAction Right;

		public PlayerAction Shoot;

		public PlayerAction Sneak;

		public PlayerAction Sprint;

		public PlayerTwoAxisAction Move;
	}
}
