using InControl;

namespace BetterLegacy.Components
{
    public class EditorCameraController : PlayerActionSet
    {
        public EditorCameraController()
        {
            Activate = CreatePlayerAction("Activate");

            Up = CreatePlayerAction("Move Up");
            Down = CreatePlayerAction("Move Down");
            Left = CreatePlayerAction("Move Left");
            Right = CreatePlayerAction("Move Right");

            SpeedUp = CreatePlayerAction("Speed Up");
            SlowDown = CreatePlayerAction("Slow Down");

            RotateAdd = CreatePlayerAction("Rotate Add");
            RotateSub = CreatePlayerAction("Rotate Sub");

            ZoomIn = CreatePlayerAction("Zoom In");
            ZoomOut = CreatePlayerAction("Zoom Out");

            RotateUp = CreatePlayerAction("Rotate Up");
            RotateDown = CreatePlayerAction("Rotate Down");
            RotateLeft = CreatePlayerAction("Rotate Left");
            RotateRight = CreatePlayerAction("Rotate Right");

            ResetOffsets = CreatePlayerAction("Reset");

            Rotate = CreateTwoAxisPlayerAction(RotateLeft, RotateRight, RotateDown, RotateUp);
            RotateUp.StateThreshold = 0.3f;
            RotateDown.StateThreshold = 0.3f;
            RotateLeft.StateThreshold = 0.3f;
            RotateRight.StateThreshold = 0.3f;

            Move = CreateTwoAxisPlayerAction(Left, Right, Down, Up);
            Up.StateThreshold = 0.3f;
            Down.StateThreshold = 0.3f;
            Left.StateThreshold = 0.3f;
            Right.StateThreshold = 0.3f;
        }

        public static EditorCameraController Bind()
        {
            var editorCameraController = new EditorCameraController();

            editorCameraController.Activate.AddDefaultBinding(InputControlType.RightStickButton);

            editorCameraController.Up.AddDefaultBinding(InputControlType.LeftStickUp);
            editorCameraController.Down.AddDefaultBinding(InputControlType.LeftStickDown);
            editorCameraController.Left.AddDefaultBinding(InputControlType.LeftStickLeft);
            editorCameraController.Right.AddDefaultBinding(InputControlType.LeftStickRight);

            editorCameraController.SpeedUp.AddDefaultBinding(InputControlType.RightTrigger);
            editorCameraController.SpeedUp.AddDefaultBinding(InputControlType.RightBumper);
            editorCameraController.SlowDown.AddDefaultBinding(InputControlType.LeftTrigger);
            editorCameraController.SlowDown.AddDefaultBinding(InputControlType.LeftBumper);
            
            editorCameraController.RotateAdd.AddDefaultBinding(InputControlType.DPadLeft);
            editorCameraController.RotateSub.AddDefaultBinding(InputControlType.DPadRight);

            editorCameraController.ZoomIn.AddDefaultBinding(InputControlType.DPadUp);
            editorCameraController.ZoomOut.AddDefaultBinding(InputControlType.DPadDown);

            editorCameraController.RotateUp.AddDefaultBinding(InputControlType.RightStickUp);
            editorCameraController.RotateDown.AddDefaultBinding(InputControlType.RightStickDown);
            editorCameraController.RotateLeft.AddDefaultBinding(InputControlType.RightStickLeft);
            editorCameraController.RotateRight.AddDefaultBinding(InputControlType.RightStickRight);

            editorCameraController.ResetOffsets.AddDefaultBinding(InputControlType.LeftStickButton);

            return editorCameraController;
        }

        public PlayerAction Activate;

        public PlayerAction RotateAdd;
        public PlayerAction RotateSub;
        
        public PlayerAction RotateUp;
        public PlayerAction RotateDown;
        public PlayerAction RotateLeft;
        public PlayerAction RotateRight;

        public PlayerTwoAxisAction Rotate;

        public PlayerAction ResetOffsets;

        public PlayerAction ZoomIn;
        public PlayerAction ZoomOut;

        public PlayerAction SpeedUp;
        public PlayerAction SlowDown;

        public PlayerAction Up;
        public PlayerAction Down;
        public PlayerAction Left;
        public PlayerAction Right;

        public PlayerTwoAxisAction Move;
    }
}
