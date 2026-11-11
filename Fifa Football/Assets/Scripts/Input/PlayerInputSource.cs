using UnityEngine;
using UnityEngine.InputSystem;

namespace FootballSim.Inputs
{
    /// <summary>Abstraction shared by keyboard, gamepad, touch, AI and (later) network input.</summary>
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        /// <summary>True when Move is a world-space XZ direction (AI); false when camera-relative (human).</summary>
        bool MoveIsWorldSpace { get; }
        bool Sprint { get; }
        bool PassPressed { get; }
        bool ShootHeld { get; }
        bool ShootReleased { get; }
        bool TacklePressed { get; }
        bool SwitchPressed { get; }
        /// <summary>World-space aim direction for strikes; zero = use movement/facing.</summary>
        Vector3 AimWorld { get; }
        /// <summary>0..1 power for pass/shot; 0 = let the controller decide (charge / default).</summary>
        float ActionPower { get; }
    }

    /// <summary>Input produced by AI brains. One-shot actions are consumed when read.</summary>
    public class AIInputSource : IPlayerInput
    {
        public Vector2 MoveVector;
        public bool SprintFlag;
        public Vector3 Aim;
        public float Power;
        bool pass, shoot, tackle;

        public Vector2 Move => MoveVector;
        public bool MoveIsWorldSpace => true;
        public bool Sprint => SprintFlag;
        public bool PassPressed { get { var v = pass; pass = false; return v; } }
        public bool ShootHeld => false;
        public bool ShootReleased { get { var v = shoot; shoot = false; return v; } }
        public bool TacklePressed { get { var v = tackle; tackle = false; return v; } }
        public bool SwitchPressed => false;
        public Vector3 AimWorld => Aim;
        public float ActionPower => Power;

        public void QueuePass(Vector3 aim, float power) { Aim = aim; Power = power; pass = true; }
        public void QueueShoot(Vector3 aim, float power) { Aim = aim; Power = power; shoot = true; }
        public void QueueTackle() { tackle = true; }
        public void Clear() { MoveVector = Vector2.zero; SprintFlag = false; Aim = Vector3.zero; Power = 0f; pass = shoot = tackle = false; }
    }

    /// <summary>Keyboard + gamepad implementation using code-defined InputActions.</summary>
    public class PlayerInputSource : MonoBehaviour, IPlayerInput
    {
        InputAction move, sprint, pass, shoot, tackle, switchPlayer;

        /// <summary>Optional on-screen controls merged with keyboard/gamepad.</summary>
        public TouchControls touch;

        public Vector2 Move
        {
            get
            {
                Vector2 kb = move.ReadValue<Vector2>();
                Vector2 t = touch != null ? touch.Move : Vector2.zero;
                return t.sqrMagnitude > kb.sqrMagnitude ? t : kb;
            }
        }
        public bool MoveIsWorldSpace => false;
        public bool Sprint => sprint.IsPressed() | (touch != null && touch.Sprint);
        public bool PassPressed => pass.WasPressedThisFrame() | (touch != null && touch.PassPressed);
        public bool ShootHeld => shoot.IsPressed() | (touch != null && touch.ShootHeld);
        public bool ShootReleased => shoot.WasReleasedThisFrame() | (touch != null && touch.ShootReleased);
        public bool TacklePressed => tackle.WasPressedThisFrame() | (touch != null && touch.TacklePressed);
        public bool SwitchPressed => switchPlayer.WasPressedThisFrame() | (touch != null && touch.SwitchPressed);
        public Vector3 AimWorld => Vector3.zero;
        public float ActionPower => 0f;

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            sprint = new InputAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift"); sprint.AddBinding("<Gamepad>/rightTrigger");
            pass = new InputAction("Pass", InputActionType.Button);
            pass.AddBinding("<Keyboard>/k"); pass.AddBinding("<Gamepad>/buttonSouth");
            shoot = new InputAction("Shoot", InputActionType.Button);
            shoot.AddBinding("<Keyboard>/space"); shoot.AddBinding("<Gamepad>/buttonEast");
            tackle = new InputAction("Tackle", InputActionType.Button);
            tackle.AddBinding("<Keyboard>/j"); tackle.AddBinding("<Gamepad>/buttonWest");
            switchPlayer = new InputAction("Switch", InputActionType.Button);
            switchPlayer.AddBinding("<Keyboard>/tab"); switchPlayer.AddBinding("<Gamepad>/leftShoulder");
        }

        void OnEnable() { move.Enable(); sprint.Enable(); pass.Enable(); shoot.Enable(); tackle.Enable(); switchPlayer.Enable(); }
        void OnDisable() { move.Disable(); sprint.Disable(); pass.Disable(); shoot.Disable(); tackle.Disable(); switchPlayer.Disable(); }
    }
}
