using System;
using System.Collections.Generic;
using System.Linq;

using SimpleJSON;
using InControl;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents player device input.
    /// </summary>
    public class PlayerInput : PlayerActionSet
    {
        #region Constructors

        public PlayerInput(float upStateThreshold, float downStateThreshold, float leftStateThreshold, float rightStateThreshold,
            float lookUpStateThreshold, float lookDownStateThreshold, float lookLeftStateThreshold, float lookRightStateThreshold)
        {
            Up = CreatePlayerAction("Move Up");
            Down = CreatePlayerAction("Move Down");
            Left = CreatePlayerAction("Move Left");
            Right = CreatePlayerAction("Move Right");
            Boost = CreatePlayerAction("Boost");
            Join = CreatePlayerAction("Join");
            Pause = CreatePlayerAction("Pause");
            Escape = CreatePlayerAction("Escape");
            LookUp = CreatePlayerAction("Look Up");
            LookDown = CreatePlayerAction("Look Down");
            LookLeft = CreatePlayerAction("Look Left");
            LookRight = CreatePlayerAction("Look Right");
            Shoot = CreatePlayerAction("Shoot");
            Sneak = CreatePlayerAction("Sneak");
            Sprint = CreatePlayerAction("Sprint");

            Move = CreateTwoAxisPlayerAction(Left, Right, Down, Up);
            Up.StateThreshold = upStateThreshold;
            Down.StateThreshold = downStateThreshold;
            Left.StateThreshold = leftStateThreshold;
            Right.StateThreshold = rightStateThreshold;

            Look = CreateTwoAxisPlayerAction(LookLeft, LookRight, LookDown, LookUp);
            LookUp.StateThreshold = lookUpStateThreshold;
            LookDown.StateThreshold = lookDownStateThreshold;
            LookLeft.StateThreshold = lookLeftStateThreshold;
            LookRight.StateThreshold = lookRightStateThreshold;
        }

        public PlayerInput(float stateThreshold) : this(stateThreshold, stateThreshold, stateThreshold, stateThreshold, stateThreshold, stateThreshold, stateThreshold, stateThreshold) { }

        public PlayerInput() : this(STATE_THRESHOLD) { }

        #endregion

        #region Values

        /// <summary>
        /// State threshold for directional controls.
        /// </summary>
        public const float STATE_THRESHOLD = 0.3f;

        #region Listeners

        /// <summary>
        /// Listens for controller input.
        /// </summary>
        public static PlayerInput controllerListener;
        /// <summary>
        /// Listens for keyboard input.
        /// </summary>
        public static PlayerInput keyboardListener;

        #endregion

        #region Defaults

        /// <summary>
        /// Creates an input based on controller and keyboard.
        /// </summary>
        public static PlayerInput ControllerAndKeyboard
        {
            get
            {
                var input = new PlayerInput();
                input.BindAll();
                return input;
            }
        }
        /// <summary>
        /// Creates an input based on controller.
        /// </summary>
        public static PlayerInput Controller
        {
            get
            {
                var input = new PlayerInput();
                input.BindController();
                return input;
            }
        }
        /// <summary>
        /// Creates an input based on the keyboard.
        /// </summary>
        public static PlayerInput Keyboard
        {
            get
            {
                var input = new PlayerInput();
                input.BindKeyboard();
                return input;
            }
        }
        /// <summary>
        /// Creates an input based on WASD keyboard.
        /// </summary>
        public static PlayerInput KeyboardWASD
        {
            get
            {
                var input = new PlayerInput();
                input.BindKeyboardWASD();
                return input;
            }
        }
        /// <summary>
        /// Creates an input based on arrow keys keyboard.
        /// </summary>
        public static PlayerInput KeyboardTraditional
        {
            get
            {
                var input = new PlayerInput();
                input.BindKeyboardTraditional();
                return input;
            }
        }

        #endregion

        #region Global Bindings

        /// <summary>
        /// The controller binding for <see cref="Shoot"/>.
        /// </summary>
        public static InputControlType ShootControl { get; set; }
        /// <summary>
        /// The keyboard binding for <see cref="Shoot"/>.
        /// </summary>
        public static Key ShootKey { get; set; }

        #endregion

        #region Actions

        /// <summary>
        /// Up control.
        /// </summary>
        public PlayerAction Up { get; set; }
        /// <summary>
        /// Down control.
        /// </summary>
        public PlayerAction Down { get; set; }
        /// <summary>
        /// Left control.
        /// </summary>
        public PlayerAction Left { get; set; }
        /// <summary>
        /// Right control.
        /// </summary>
        public PlayerAction Right { get; set; }
        /// <summary>
        /// Movement control.
        /// </summary>
        public PlayerTwoAxisAction Move { get; set; }
        /// <summary>
        /// Boost control.
        /// </summary>
        public PlayerAction Boost { get; set; }
        /// <summary>
        /// Join session control.
        /// </summary>
        public PlayerAction Join { get; set; }
        /// <summary>
        /// Pause game control.
        /// </summary>
        public PlayerAction Pause { get; set; }
        /// <summary>
        /// Escape control.
        /// </summary>
        public PlayerAction Escape { get; set; }
        /// <summary>
        /// Shoot control.
        /// </summary>
        public PlayerAction Shoot;
        /// <summary>
        /// Sneak control.
        /// </summary>
        public PlayerAction Sneak;
        /// <summary>
        /// Sprint control.
        /// </summary>
        public PlayerAction Sprint;
        /// <summary>
        /// Look up control.
        /// </summary>
        public PlayerAction LookUp { get; set; }
        /// <summary>
        /// Look down control.
        /// </summary>
        public PlayerAction LookDown { get; set; }
        /// <summary>
        /// Look left control.
        /// </summary>
        public PlayerAction LookLeft { get; set; }
        /// <summary>
        /// Look right control.
        /// </summary>
        public PlayerAction LookRight { get; set; }
        /// <summary>
        /// Looking control.
        /// </summary>
        public PlayerTwoAxisAction Look { get; set; }
        /// <summary>
        /// Registered custom actions.
        /// </summary>
        public List<PlayerAction> CustomActions { get; set; } = new List<PlayerAction>();

        #endregion

        #endregion

        #region Functions

        /// <summary>
        /// Sets up <see cref="controllerListener"/> and <see cref="keyboardListener"/> used for local players joining.
        /// </summary>
        public static void SetupListeners()
        {
            controllerListener?.Destroy();
            controllerListener = Controller;
            keyboardListener?.Destroy();
            keyboardListener = Keyboard;
        }

        /// <summary>
        /// Binds both controller and keyboard inputs.
        /// </summary>
        public virtual void BindAll()
        {
            BindController();
            BindKeyboard();
        }

        /// <summary>
        /// Binds controller inputs.
        /// </summary>
        public virtual void BindController()
        {
            Up.AddDefaultBinding(InputControlType.DPadUp);
            Up.AddDefaultBinding(InputControlType.LeftStickUp);
            Down.AddDefaultBinding(InputControlType.DPadDown);
            Down.AddDefaultBinding(InputControlType.LeftStickDown);
            Left.AddDefaultBinding(InputControlType.DPadLeft);
            Left.AddDefaultBinding(InputControlType.LeftStickLeft);
            Right.AddDefaultBinding(InputControlType.DPadRight);
            Right.AddDefaultBinding(InputControlType.LeftStickRight);
            //Boost.AddDefaultBinding(InputControlType.RightTrigger);
            //Boost.AddDefaultBinding(InputControlType.RightBumper);
            Boost.AddDefaultBinding(InputControlType.Action1);
            //Boost.AddDefaultBinding(InputControlType.Action3);
            Join.AddDefaultBinding(InputControlType.Action1);
            Join.AddDefaultBinding(InputControlType.Action2);
            Join.AddDefaultBinding(InputControlType.Action3);
            Join.AddDefaultBinding(InputControlType.Action4);
            Pause.AddDefaultBinding(InputControlType.Command);
            Escape.AddDefaultBinding(InputControlType.Action2);
            //Escape.AddDefaultBinding(InputControlType.Action4);

            LookUp.AddDefaultBinding(InputControlType.RightStickUp);
            LookDown.AddDefaultBinding(InputControlType.RightStickDown);
            LookLeft.AddDefaultBinding(InputControlType.RightStickLeft);
            LookRight.AddDefaultBinding(InputControlType.RightStickRight);
            Shoot.AddDefaultBinding(ShootControl);
            Sneak.AddDefaultBinding(InputControlType.LeftStickButton);
            Sprint.AddDefaultBinding(InputControlType.RightTrigger);
        }

        /// <summary>
        /// Binds keyboard inputs.
        /// </summary>
        public virtual void BindKeyboard()
        {
            Up.AddDefaultBinding(Key.UpArrow);
            Up.AddDefaultBinding(Key.W);
            Down.AddDefaultBinding(Key.DownArrow);
            Down.AddDefaultBinding(Key.S);
            Left.AddDefaultBinding(Key.LeftArrow);
            Left.AddDefaultBinding(Key.A);
            Right.AddDefaultBinding(Key.RightArrow);
            Right.AddDefaultBinding(Key.D);
            Boost.AddDefaultBinding(Key.Space);
            Boost.AddDefaultBinding(Key.Return);
            Boost.AddDefaultBinding(Key.Z);
            Join.AddDefaultBinding(Key.Space);
            Join.AddDefaultBinding(Key.A);
            Join.AddDefaultBinding(Key.S);
            Join.AddDefaultBinding(Key.D);
            Join.AddDefaultBinding(Key.W);
            Join.AddDefaultBinding(Key.LeftArrow);
            Join.AddDefaultBinding(Key.RightArrow);
            Join.AddDefaultBinding(Key.DownArrow);
            Join.AddDefaultBinding(Key.UpArrow);
            Pause.AddDefaultBinding(Key.Escape);
            Escape.AddDefaultBinding(Key.Escape);

            LookUp.AddDefaultBinding(Key.I);
            LookDown.AddDefaultBinding(Key.K);
            LookLeft.AddDefaultBinding(Key.J);
            LookRight.AddDefaultBinding(Key.L);
            Shoot.AddDefaultBinding(ShootKey);
            Sneak.AddDefaultBinding(Key.Shift);
            Sprint.AddDefaultBinding(Key.Control);
        }

        /// <summary>
        /// Binds keyboard inputs based on WASD.
        /// </summary>
        public virtual void BindKeyboardWASD()
        {
            Up.AddDefaultBinding(Key.W);
            Down.AddDefaultBinding(Key.S);
            Left.AddDefaultBinding(Key.A);
            Right.AddDefaultBinding(Key.D);
            Boost.AddDefaultBinding(Key.Space);
            Join.AddDefaultBinding(Key.Space);
            Pause.AddDefaultBinding(Key.Escape);
            Escape.AddDefaultBinding(Key.Escape);

            LookUp.AddDefaultBinding(Key.I);
            LookDown.AddDefaultBinding(Key.K);
            LookLeft.AddDefaultBinding(Key.J);
            LookRight.AddDefaultBinding(Key.L);
            Shoot.AddDefaultBinding(ShootKey);
            Sneak.AddDefaultBinding(Key.Shift);
            Sprint.AddDefaultBinding(Key.Control);
        }

        /// <summary>
        /// Binds keyboard inputs based on traditional.
        /// </summary>
        public virtual void BindKeyboardTraditional()
        {
            Up.AddDefaultBinding(Key.UpArrow);
            Down.AddDefaultBinding(Key.DownArrow);
            Left.AddDefaultBinding(Key.LeftArrow);
            Right.AddDefaultBinding(Key.RightArrow);
            Boost.AddDefaultBinding(Key.Z);
            Join.AddDefaultBinding(Key.LeftArrow);
            Join.AddDefaultBinding(Key.RightArrow);
            Join.AddDefaultBinding(Key.DownArrow);
            Join.AddDefaultBinding(Key.UpArrow);
            Pause.AddDefaultBinding(Key.Escape);
            Escape.AddDefaultBinding(Key.Escape);

            LookUp.AddDefaultBinding(Key.I);
            LookDown.AddDefaultBinding(Key.K);
            LookLeft.AddDefaultBinding(Key.J);
            LookRight.AddDefaultBinding(Key.L);
            Shoot.AddDefaultBinding(ShootKey);
            Sneak.AddDefaultBinding(Key.Shift);
            Sprint.AddDefaultBinding(Key.Control);
        }

        /// <summary>
        /// Clears all bindings.
        /// </summary>
        public virtual void ClearBindings()
        {
            Up.ClearBindings();
            Down.ClearBindings();
            Left.ClearBindings();
            Right.ClearBindings();
            Boost.ClearBindings();
            Join.ClearBindings();
            Pause.ClearBindings();
            Escape.ClearBindings();
            for (int i = 0; i < CustomActions.Count; i++)
                CustomActions[i].ClearBindings();
        }

        /// <summary>
        /// Reads a JSON file.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public void ReadJSON(JSONNode jn)
        {
            ClearBindings();
            for (int i = 0; i < jn["bindings"].Count; i++)
                BindJSON(jn["bindings"][i]);
        }

        void BindJSON(JSONNode jn)
        {
            var name = jn["name"];
            if (Actions.TryFind(x => x.Name == name, out PlayerAction playerAction))
            {
                BindJSON(jn, playerAction);
                return;
            }

            var customAction = CreatePlayerAction(name);
            RegisterCustomAction(customAction);
            BindJSON(jn, customAction);
        }

        void BindJSON(JSONNode jn, PlayerAction playerAction)
        {
            switch (jn["type"].AsInt)
            {
                case 0: {
                        var keys = new Key[jn["keys"].Count];
                        for (int i = 0; i < keys.Length; i++)
                            keys[i] = (Key)Enum.Parse(typeof(Key), jn["keys"][i], true);
                        playerAction.AddDefaultBinding(keys);
                        break;
                    } // keyboard
                case 1: {
                        playerAction.AddDefaultBinding((InputControlType)Enum.Parse(typeof(InputControlType), jn["control"], true));
                        break;
                    } // controller
                case 2: {
                        playerAction.AddDefaultBinding((Mouse)Enum.Parse(typeof(Mouse), jn["control"], true));
                        break;
                    } // mouse
            }
        }

        /// <summary>
        /// Registers a custom action to the input.
        /// </summary>
        /// <param name="name">Name of the action.</param>
        /// <param name="keys">Keys to bind.</param>
        public void RegisterCustomAction(string name, params Key[] keys)
        {
            if (Actions.TryFind(x => x.Name == name, out PlayerAction playerAction))
                return;

            var act = CreatePlayerAction(name);
            act.AddDefaultBinding(keys);
            CustomActions.Add(act);
        }

        /// <summary>
        /// Registers a custom action to the input.
        /// </summary>
        /// <param name="name">Name of the action.</param>
        /// <param name="inputControlType">Controller input to bind.</param>
        public void RegisterCustomAction(string name, InputControlType inputControlType)
        {
            if (Actions.TryFind(x => x.Name == name, out PlayerAction playerAction))
                return;

            var act = CreatePlayerAction(name);
            act.AddDefaultBinding(inputControlType);
            CustomActions.Add(act);
        }

        /// <summary>
        /// Registers a custom action to the input.
        /// </summary>
        /// <param name="playerAction">Player action to add.</param>
        public void RegisterCustomAction(PlayerAction playerAction) => CustomActions.OverwriteAdd((a, index) => a.Name == playerAction.Name, playerAction);

        /// <summary>
        /// Gets a player action.
        /// </summary>
        /// <param name="name">Name of the action to get.</param>
        /// <returns>Returns a found player action.</returns>
        public PlayerAction GetPlayerAction(string name) => Actions.First(x => x.Name == name);

        #endregion

        public static implicit operator bool(PlayerInput input) => input != null;
    }
}
