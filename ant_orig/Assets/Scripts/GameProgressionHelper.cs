public static class GameProgressionHelper
{
    public static readonly bool setFirstLevelAsTutorial = true;
    public static readonly int[] requiredFoodLevels = {3, 2, 2, 3, 4};  //Required collected food count for each level
    public static readonly int[] agentAntLevels = {0, 4, 5, 7, 9};  //Agent ant count for each level
    public static readonly int[] agentPredatorLevels = {0, 0, 2, 3, 4};  //Agent predator count for each level
    public static readonly int[] foodCountLevels = {4, 4, 4, 6, 7};
    public static int currentLevel
    {
        get
        {
            return currentlevel;
        }
    }
    public static int maxFoodCapacity = 1;
    public static PlayerInfo info = new PlayerInfo("Cur. Attempt", 0, 0, 0, 0);

    private static int currentlevel = 0;

    public static int nextLevel()
    {
        if(currentlevel < requiredFoodLevels.Length - 1)
            currentlevel++;
        return currentlevel;
    }

    public static int maxLevel()
    {
        return requiredFoodLevels.Length;
    }

    public static bool isLastLevel()
    {
        return (currentlevel == requiredFoodLevels.Length - 1);
    }
}
