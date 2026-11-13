using UnityEngine;
using FootballSim.Inputs;

namespace FootballSim.Net
{
    /// <summary>
    /// Input for a player controlled from across the network. The server keeps the latest frame the client sent: held values
    /// (movement, sprint, aim) stay until replaced, and one-shot actions (pass, shoot, tackle, switch) stay set until the simulation reads them once,
    /// so a press that arrives between two simulation frames is not lost.
    /// </summary>
    public class RemoteInput : IPlayerInput
    {
        InputFrame latest;
        bool pass, shootReleased, tackle, switchPressed;

        /// <summary>Secret the client must put in every packet for this player. Given to the client outside this channel.</summary>
        public uint Token { get; }
        public ushort LastSequence { get; private set; }
        public bool HasSequence { get; private set; }

        public RemoteInput(uint token = 0) { Token = token; }

        /// <summary>Applies a frame unless its sequence number is not newer than the last one applied. Returns whether it was applied.</summary>
        public bool ApplySequenced(InputFrame f, ushort sequence)
        {
            if (HasSequence && !NetCodec.IsNewer(sequence, LastSequence)) return false;
            HasSequence = true; LastSequence = sequence;
            Apply(f);
            return true;
        }

        /// <summary>Number of input frames applied so far.</summary>
        public int Applied { get; private set; }

        public void Apply(InputFrame f)
        {
            latest = f;
            pass |= f.pass; shootReleased |= f.shootReleased; tackle |= f.tackle; switchPressed |= f.switchPressed;
            Applied++;
        }

        public Vector2 Move => latest.move;
        public bool MoveIsWorldSpace => latest.moveIsWorldSpace;
        public bool Sprint => latest.sprint;
        public bool PassPressed { get { var v = pass; pass = false; return v; } }
        public bool ShootHeld => latest.shootHeld;
        public bool ShootReleased { get { var v = shootReleased; shootReleased = false; return v; } }
        public bool TacklePressed { get { var v = tackle; tackle = false; return v; } }
        public bool SwitchPressed { get { var v = switchPressed; switchPressed = false; return v; } }
        public Vector3 AimWorld => latest.aim;
        public float ActionPower => latest.power;
    }
}
