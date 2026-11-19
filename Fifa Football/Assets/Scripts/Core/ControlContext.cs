namespace FootballSim.Core
{
    /// <summary>
    /// Which situation the player in control is in, and what the touch buttons should say and how prominent each should be. The buttons
    /// keep doing what they always did (shoot, pass, tackle); only the wording and emphasis follow the play, so the right button stands out.
    /// </summary>
    public enum ControlContext { Loose, Attack, Defend, Keeper }

    public struct ButtonLook
    {
        public string label;
        /// <summary>Size multiplier on the button (1 = normal).</summary>
        public float size;
        /// <summary>Opacity multiplier (1 = normal); a button that is not useful right now is dimmed, never disabled.</summary>
        public float alpha;
    }

    public struct ControlLook
    {
        public ButtonLook shoot, pass, tackle;
    }

    public static class ControlContextRules
    {
        /// <summary>The ball counts as controlled by a team when it last touched it and the ball is not flying fast (a shot or long pass in the air is nobody's).</summary>
        public const float InFlightSpeed = 14f;

        /// <summary>
        /// The situation for the controlled player. <paramref name="possessionTeam"/> is the team of the last toucher (-1 for nobody);
        /// <paramref name="ballSpeed"/> in m/s.
        /// </summary>
        public static ControlContext Decide(bool controlledIsKeeper, int controlledTeam, int possessionTeam, float ballSpeed)
        {
            if (controlledIsKeeper) return ControlContext.Keeper;
            if (possessionTeam < 0 || ballSpeed > InFlightSpeed) return ControlContext.Loose;
            return possessionTeam == controlledTeam ? ControlContext.Attack : ControlContext.Defend;
        }

        static ButtonLook B(string label, float size = 1f, float alpha = 1f) { return new ButtonLook { label = label, size = size, alpha = alpha }; }

        public static ControlLook Look(ControlContext c)
        {
            switch (c)
            {
                case ControlContext.Attack:  // with the ball: shoot and pass lead, the tackle button is not useful
                    return new ControlLook { shoot = B("SHOOT", 1.1f), pass = B("PASS", 1.1f), tackle = B("TACKLE", 0.8f, 0.55f) };
                case ControlContext.Defend:  // without the ball: win it back
                    return new ControlLook { shoot = B("CLEAR", 0.9f, 0.8f), pass = B("PASS", 0.85f, 0.55f), tackle = B("TACKLE", 1.25f) };
                case ControlContext.Keeper:  // the keeper's tackle button is the dive, shoot kicks the ball out
                    return new ControlLook { shoot = B("KICK", 1f), pass = B("PASS", 1f), tackle = B("DIVE", 1.25f) };
                default:                     // loose ball: everything as normal
                    return new ControlLook { shoot = B("SHOOT"), pass = B("PASS"), tackle = B("TACKLE") };
            }
        }
    }
}
