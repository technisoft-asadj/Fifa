namespace FootballSim.Core
{
    /// <summary>
    /// What an ad service has to offer the game. No ad SDK is part of this project: <see cref="Ads.Provider"/> is empty, so nothing about
    /// ads is ever shown. Wiring a real service (which needs the owner's account with it, store policies and a privacy notice) means writing one
    /// class that implements this and assigning it at start-up; the rest of the game already asks only through <see cref="Ads"/>.
    /// </summary>
    public interface IAdProvider
    {
        /// <summary>True when a rewarded video is loaded and can be shown right now.</summary>
        bool RewardedReady { get; }

        /// <summary>Shows a rewarded video; calls <paramref name="done"/> with true only if the player watched it to the end.</summary>
        void ShowRewarded(System.Action<bool> done);
    }

    /// <summary>The game's single point of contact with ads. Optional and never in the way: ads only ever sit behind a button the player chooses to press.</summary>
    public static class Ads
    {
        /// <summary>The ad service in use; null (the default) means there are no ads.</summary>
        public static IAdProvider Provider;

        public static bool RewardedReady { get { try { return Provider != null && Provider.RewardedReady; } catch { return false; } } }

        /// <summary>Asks for a rewarded video. With no provider, or none ready, <paramref name="done"/> gets false at once and nothing is shown.</summary>
        public static void ShowRewarded(System.Action<bool> done)
        {
            if (!RewardedReady) { done?.Invoke(false); return; }
            try { Provider.ShowRewarded(done); } catch { done?.Invoke(false); }
        }
    }
}
