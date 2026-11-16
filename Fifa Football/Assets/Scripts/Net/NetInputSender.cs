using UnityEngine;
using FootballSim.Inputs;

namespace FootballSim.Net
{
    /// <summary>
    /// Reads a local <see cref="IPlayerInput"/> (keyboard, touch, a replay) once per frame and sends it to the server through a joined
    /// <see cref="NetClient"/>. This is what turns a client into a player. Nothing is sent before the server's Welcome arrives.
    /// </summary>
    public class NetInputSender : MonoBehaviour
    {
        public NetClient client;
        IPlayerInput source;
        public int Sent { get; private set; }

        public void SetSource(IPlayerInput input) { source = input; }

        void Update()
        {
            if (client == null || source == null || !client.Joined) return;
            // each property is read exactly once per frame: one-shot actions (pass, tackle, switch, shoot release) are consumed by a read
            var frame = new InputFrame
            {
                move = source.Move,
                moveIsWorldSpace = source.MoveIsWorldSpace,
                sprint = source.Sprint,
                pass = source.PassPressed,
                shootHeld = source.ShootHeld,
                shootReleased = source.ShootReleased,
                tackle = source.TacklePressed,
                switchPressed = source.SwitchPressed,
                aim = source.AimWorld,
                power = source.ActionPower,
            };
            client.SendInput(frame);
            Sent++;
        }
    }
}
