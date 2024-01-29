using MelonLoader;

namespace FavGirl
{
    // ASSEMBLY PREFS
    internal static class MelonBuildInfo
    {
        public const string Name = "FavGirl";
        public const string Description = "Adds the ability to 'favorite' a girl and/or elfin to lock in their visuals.";
        public const string Author = "RobotLucca";
        public const string Company = null;
        public const string Version = "2.3.8";
        public const string DownloadLink = null;
    }

    // MELONLOADER CLASS
    public class FavGirlMelon : MelonMod
    {
        public static FavGirlMelon instance { get; private set; }

        public override void OnApplicationStart() // Runs after Game Initialization.
        {
            instance = this;
            FavSave.Load();
        }
    }
}
