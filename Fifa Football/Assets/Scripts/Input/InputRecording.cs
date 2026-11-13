using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Inputs
{
    /// <summary>Everything one player's input said during one frame: the unit a network client would send, or a replay would play back.</summary>
    public struct InputFrame
    {
        public Vector2 move;
        public bool moveIsWorldSpace, sprint, pass, shootHeld, shootReleased, tackle, switchPressed;
        public Vector3 aim;
        public float power;
    }

    /// <summary>Passes reads through to another input and writes down what was read, per frame, from <c>startFrame</c> on.</summary>
    public class RecordingInput : IPlayerInput
    {
        readonly IPlayerInput inner;
        readonly int startFrame;
        public readonly List<InputFrame> Frames = new List<InputFrame>();

        public RecordingInput(IPlayerInput inner, int startFrame) { this.inner = inner; this.startFrame = startFrame; }

        InputFrame Current
        {
            get
            {
                int i = Mathf.Max(0, Time.frameCount - startFrame);
                while (Frames.Count <= i) Frames.Add(default);
                return Frames[i];
            }
        }
        void Store(InputFrame f) { Frames[Mathf.Max(0, Time.frameCount - startFrame)] = f; }

        public Vector2 Move { get { var v = inner.Move; var f = Current; f.move = v; Store(f); return v; } }
        public bool MoveIsWorldSpace { get { var v = inner.MoveIsWorldSpace; var f = Current; f.moveIsWorldSpace = v; Store(f); return v; } }
        public bool Sprint { get { var v = inner.Sprint; var f = Current; f.sprint = v; Store(f); return v; } }
        public bool PassPressed { get { var v = inner.PassPressed; var f = Current; f.pass |= v; Store(f); return v; } }
        public bool ShootHeld { get { var v = inner.ShootHeld; var f = Current; f.shootHeld = v; Store(f); return v; } }
        public bool ShootReleased { get { var v = inner.ShootReleased; var f = Current; f.shootReleased |= v; Store(f); return v; } }
        public bool TacklePressed { get { var v = inner.TacklePressed; var f = Current; f.tackle |= v; Store(f); return v; } }
        public bool SwitchPressed { get { var v = inner.SwitchPressed; var f = Current; f.switchPressed |= v; Store(f); return v; } }
        public Vector3 AimWorld { get { var v = inner.AimWorld; var f = Current; f.aim = v; Store(f); return v; } }
        public float ActionPower { get { var v = inner.ActionPower; var f = Current; f.power = v; Store(f); return v; } }
    }

    /// <summary>Plays recorded frames back. One-shot actions are consumed by the first read in a frame, like AI input.</summary>
    public class ReplayInput : IPlayerInput
    {
        readonly IList<InputFrame> frames;
        readonly int startFrame;
        int passFrame = -1, shootFrame = -1, tackleFrame = -1, switchFrame = -1;

        public ReplayInput(IList<InputFrame> frames, int startFrame) { this.frames = frames; this.startFrame = startFrame; }

        int Index => Mathf.Max(0, Time.frameCount - startFrame);
        InputFrame F => Index < frames.Count ? frames[Index] : default;
        bool Once(bool value, ref int consumedAt) { if (!value || consumedAt == Index) return false; consumedAt = Index; return true; }

        public Vector2 Move => F.move;
        public bool MoveIsWorldSpace => F.moveIsWorldSpace;
        public bool Sprint => F.sprint;
        public bool PassPressed => Once(F.pass, ref passFrame);
        public bool ShootHeld => F.shootHeld;
        public bool ShootReleased => Once(F.shootReleased, ref shootFrame);
        public bool TacklePressed => Once(F.tackle, ref tackleFrame);
        public bool SwitchPressed => Once(F.switchPressed, ref switchFrame);
        public Vector3 AimWorld => F.aim;
        public float ActionPower => F.power;
    }
}
