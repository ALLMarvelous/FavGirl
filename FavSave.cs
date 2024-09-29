using MelonLoader;

namespace FavGirl
{
    public class FavSave
    {
        public static MelonPreferences_Category favCategory;
        public static MelonPreferences_Entry<GirlID> favGirl;
        public static MelonPreferences_Entry<ElfinID> favElfin;
        public static MelonPreferences_Entry<bool> conditionalHideScoreDetails;

        public static GirlID FavGirl
        {
            get => favGirl.Value;
            set
            {
                if (Enum.IsDefined(typeof(GirlID), value))
                    favGirl.Value = value;
                else
                    favGirl.Value = GirlID.NONE;
            }
        }

        public static ElfinID FavElfin
        {
            get => favElfin.Value;
            set
            {
                if (Enum.IsDefined(typeof(ElfinID), value))
                    favElfin.Value = value;
                else
                    favElfin.Value = ElfinID.NONE;
            }
        }

        public static void Load()
        {
            var girlValuesStr = "";
            var elfinValuesStr = "";

            foreach (var str in Enum.GetNames(typeof(GirlID))) girlValuesStr += str + "\n";
            girlValuesStr = girlValuesStr.Substring(0, girlValuesStr.Length - 1);

            foreach (var str in Enum.GetNames(typeof(ElfinID))) elfinValuesStr += str + "\n";
            elfinValuesStr = elfinValuesStr.Substring(0, elfinValuesStr.Length - 1);

            favCategory = MelonPreferences.CreateCategory(MelonBuildInfo.Name);
            favCategory.SetFilePath("UserData/FavGirl.cfg");
            favGirl = MelonPreferences.CreateEntry(
                MelonBuildInfo.Name, "favGirl", GirlID.NONE,
                description: "Which girl is currently favorited. Acceptable values:\n" + girlValuesStr
            );
            favElfin = MelonPreferences.CreateEntry(
                MelonBuildInfo.Name, "favElfin", ElfinID.NONE,
                description: "Which elfin is currently favorited. Acceptable values:\n" + elfinValuesStr
            );
            conditionalHideScoreDetails = MelonPreferences.CreateEntry(
                MelonBuildInfo.Name, "conditionalHideScoreDetails", false,
                description:
                "Whether to automatically hide girl/elfin choices when the ability matches the victory screen.\nFor if you want to get vanilla victory screens."
            );

            if (!Enum.IsDefined(typeof(GirlID), favGirl.Value))
            {
                FavGirlMelon.instance.LoggerInstance.Msg($"Favorite girl not defined");
                FavGirl = GirlID.NONE;
            }
            if (!Enum.IsDefined(typeof(ElfinID), favElfin.Value))
            {
                FavGirlMelon.instance.LoggerInstance.Msg($"Favorite elfin not defined");
                FavElfin = ElfinID.NONE;
            }
        }
    }

    // List of valid girls selectable as favorites
    // Girls not present cannot be selected
    public enum GirlID
    {
        NONE = -1,
        RIN_BASS = 0,
        RIN_BAD = 1,
        RIN_SLEEP = 2,
        RIN_BUNNY = 3,
        RIN_XMAS = 13,
        RIN_FOOL = 17,
        BURO_PILOT = 4,
        BURO_IDOL = 5,
        BURO_ZOMBIE = 6,
        BURO_JOKER = 7,
        BURO_SAILOR = 14,
        BURO_BIKER = 24,
        OLA_BOXER = 23,
        MARIJA_VIOLIN = 8,
        MARIJA_MAID = 9,
        MARIJA_MAGIC = 10,
        MARIJA_DEVIL = 11,
        MARIJA_BLACK = 12,
        YUME = 15,
        NEKO = 16,
        REIMU = 18,
        EL_CLEAR = 19,
        MARIJA_SISTER = 20,
        MARISA = 21,
        AMIYA = 22,
        MIKU_HATSUNE = 25,
        // RIN_LEN = 26
        // RACER = 27
        BALLERINA = 28
    }

    // List of valid Elfins selectable as favorites
    // Elfins not present cannot be selected (Currently only newly added ones and Neon Egg)
    // Neon Egg has special mechanics that do not work with sprite replacement
    public enum ElfinID
    {
        NONE = -2,
        UNSELECT = -1,
        MIO = 0,
        ANGELA = 1,
        THANATOS = 2,
        RABOT = 3,
        NURSE = 4,
        WITCH = 5,
        DRAGON = 6,
        LILITH = 7,
        PAIGE = 8,
        SILENCER = 9,
        // NEON_EGG = 10
        BETAGO = 11
    }
}