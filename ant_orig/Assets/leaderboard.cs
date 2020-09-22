using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;


// Leaderboard script, based on the following source: https://www.grimoirehex.com/unity-3d-local-leaderboard/

public class PlayerInfo
{
    public string name;
    public int level;
    public float time;
    public float angle;
    public float score;

    public PlayerInfo(string name, int level, float time, float angle, float score)
    {
        this.name = name;
        this.level = level;
        this.time = time;
        this.angle = angle;
        this.score = score;
    }
}

public class LevelPerformance
{
    public int collectedFood;
    public float time;
    public float angle;
    public int retry;
    public int stolenTimes;

    public LevelPerformance(int collectedFood, float time, float angle, int retry, int stolenTimes)
    {
        this.collectedFood = collectedFood;
        this.time = time;
        this.angle = angle;
        this.retry = retry;
        this.stolenTimes = stolenTimes;
    }
}

public class leaderboard : MonoBehaviour
{

    //Use TextMeshPro to display the data on the screen
    public TextMeshProUGUI ranks;
    public TextMeshProUGUI names;
    public TextMeshProUGUI levels;
    public TextMeshProUGUI times;
    public TextMeshProUGUI angles;
    public TextMeshProUGUI scores;
    public int cupcakePoints;
    public float[] timeBonusRanges;
    public int[] timeBonusPoints;
    public float[] angleBonusRanges;
    public int[] angleBonusPoints;
    public int[] retryBonusRanges;
    public int[] retryBonusPoints;
    public int[] stolenTimesBonusRanges;
     public int[] stolenTimesBonusPoints;

    public bool showOnStart = false;


    //List To Hold "PlayerInfo" Objects
    private List<PlayerInfo> collectedStats = new List<PlayerInfo>();
    private PlayerInfo latestEntry;
    private string filename = "leaderboardData.csv";


    // Use this for initialization
    public void Start()
    {
        gameObject.SetActive(showOnStart);
    }

    // Shows the leaderboard on-screen for the player
    public void ShowLeaderBoard()
    {
        gameObject.SetActive(true);
    }

    // Hides the leaderboard on-screen for the player
    public void HideLeaderBoard()
    {
        gameObject.SetActive(false);
    }

    // Saves a new complete entry (result) to the csv file
    public void SaveCompleteResult(PlayerInfo info)
    {
        string csvLine = info.name + "," + info.level.ToString() + "," + info.time.ToString() + "," + info.angle.ToString() + "," + info.score.ToString();
        System.IO.File.AppendAllText(filename, csvLine + Environment.NewLine);
    }

    //Sorting the stats by their overall score
    public void SortStatsByScore()
    {
        latestEntry = collectedStats.Last();

        //Bubble sort requires nxn passes for a list of n elements
        for (int i = 1; i < collectedStats.Count; i++)
        {
            //Start at the beginning of the list and compare the score with the one below it
            for (int j = 0; j < collectedStats.Count - i; j++)
            {
                //If The Current Score Is Higher Than The Score Above It , Swap
                if (collectedStats[j].score < collectedStats[j + 1].score)
                {
                    //Temporary variable to hold smaller score
                    PlayerInfo tempInfo = collectedStats[j + 1];

                    // Replace small score with bigger score
                    collectedStats[j + 1] = collectedStats[j];

                    //Set small score closer to the end of the list by placing it at "i" rather than "i-1" 
                    collectedStats[j] = tempInfo;
                }
            }
        }
    }

    //Sorting the stats by the total time
    public void SortStatsByTime(int checkpoint)
    {
        latestEntry = collectedStats.Last();

        //Bubble sort requires nxn passes for a list of n elements
        for (int i = 1; i < collectedStats.Count; i++)
        {
            //Start at the beginning of the list and compare the time with the one below it
            for (int j = 0; j < collectedStats.Count - i; j++)
            {
                //If The Current time Is lower Than The time Above It , Swap
                if (collectedStats[j].time > collectedStats[j + 1].time)
                {
                    //Temporary variable to hold smaller score
                    PlayerInfo tempInfo = collectedStats[j + 1];

                    // Replace short time with longer one
                    collectedStats[j + 1] = collectedStats[j];

                    //Set long time closer to the end of the list by placing it at "i" rather than "i-1" 
                    collectedStats[j] = tempInfo;
                }
            }
        }
    }

    public void UpdateLeaderboardVisual()
    {
        int maxDisplay = Mathf.Min(8, collectedStats.Count);
        int currentIndex;
        int latestIndex = collectedStats.IndexOf(latestEntry);
        bool isTop8 = (latestIndex < maxDisplay);

        for(int i = 0; i < maxDisplay; i++)
        {
            currentIndex = (!isTop8 && i == 7) ? latestIndex : i;

            if (!isTop8 && i == 6)
            {
                ranks.text += "...\n";
                names.text += "...\n";
                levels.text += "...\n";
                times.text += "...\n";
                angles.text += "...\n";
                scores.text += "...\n";
                continue;
            }

            if(collectedStats[currentIndex].Equals(latestEntry))
            {
                ranks.text += "<color=yellow>";
                names.text += "<color=yellow>";
                levels.text += "<color=yellow>";
                times.text += "<color=yellow>";
                angles.text += "<color=yellow>";
                scores.text += "<color=yellow>";
            }

            ranks.text += (currentIndex + 1).ToString() + "\n";
            names.text += collectedStats[currentIndex].name + "\n";
            levels.text += collectedStats[currentIndex].level.ToString() + "\n";
            times.text += formatTime(collectedStats[currentIndex].time) + "\n";
            angles.text += collectedStats[currentIndex].angle.ToString() + "°\n";
            scores.text += collectedStats[currentIndex].score.ToString() + "\n";

            if(collectedStats[currentIndex].Equals(latestEntry))
            {
                ranks.text += "</color>";
                names.text += "</color>";
                levels.text += "</color>";
                times.text += "</color>";
                angles.text += "</color>";
                scores.text += "</color>";
            }
        }
    }

    public void LoadLeaderBoardStats()
    {
        ClearPrefs();
        string[] lines = System.IO.File.ReadAllLines(filename);

        //Results stored in csv file in chronological order (last line will be the current attempt)
        for (int i = 0; i < lines.Length; i++)
        {
            String[] lineData = lines[i].Split(',');
            //Name, time (in seconds) and size of angle (in degrees) they were off by when looking for their home
            String name = lineData[0];
            int level = int.Parse(lineData[1]);
            float time = float.Parse(lineData[2]);
            float angle = float.Parse(lineData[3]);
            //Score is calculated from time and angle (the less the better, for both)
            float score = int.Parse(lineData[4]);
            PlayerInfo loadedInfo = new PlayerInfo(name, level, time, angle, score);
            collectedStats.Add(loadedInfo);
        }

        //Sort stats by score (and then display them on the screen)
        SortStatsByScore();

        //Update LeaderBoard on screen
        UpdateLeaderboardVisual();
    }

    public void ClearPrefs()
    {
        //Use This To Delete All Names And Scores From The LeaderBoard
        PlayerPrefs.DeleteAll();
        collectedStats.Clear();

        //Clear Current Displayed LeaderBoard
        ranks.text = "";
        names.text = "";
        levels.text = "";
        times.text = "";
        angles.text = "";
        scores.text = "";
    }

    public int calculateLevelScore(LevelPerformance perf)
    {
        int levelScore = 0;

        levelScore += cupcakePoints * perf.collectedFood;
        //Debug.Log("Cupcake pts: " + levelScore);

        for(int i = 0; i < timeBonusRanges.Length; i++)
        {
            float lowerBound = (i == 0) ? 0 : timeBonusRanges[i - 1];
            float upperBound = timeBonusRanges[i];
            if(perf.time - lowerBound <= (upperBound - lowerBound))
            {
                levelScore += timeBonusPoints[i];
                //Debug.Log("Time bonus: " + timeBonusPoints[i]);
                break;
            }
        }
        if(GameProgressionHelper.setFirstLevelAsTutorial && GameProgressionHelper.currentLevel == 0)
        {
            for(int i = 0; i < angleBonusRanges.Length; i++)
            {
                float lowerBound = (i == 0) ? 0 : angleBonusRanges[i - 1];
                float upperBound = angleBonusRanges[i];
                if(perf.angle - lowerBound <= (upperBound - lowerBound))
                {
                    levelScore += angleBonusPoints[i];
                    //Debug.Log("Angle bonus: " + angleBonusPoints[i]);
                    break;
                }
            }
        }
        if(!GameProgressionHelper.setFirstLevelAsTutorial || GameProgressionHelper.currentLevel != 0)
        {
            for(int i = 0; i < retryBonusRanges.Length; i++)
            {
                int lowerBound = (i == 0) ? 0 : retryBonusRanges[i - 1];
                int upperBound = retryBonusRanges[i];
                if(perf.retry - lowerBound <= (upperBound - lowerBound))
                {
                    levelScore += retryBonusPoints[i];
                    //Debug.Log("Retry bonus: " + retryBonusPoints[i]);
                    break;
                }
            }
            for(int i = 0; i < stolenTimesBonusRanges.Length; i++)
            {
                int lowerBound = (i == 0) ? 0 : stolenTimesBonusRanges[i - 1];
                int upperBound = stolenTimesBonusRanges[i];
                if(perf.stolenTimes - lowerBound <= (upperBound - lowerBound))
                {
                    levelScore += stolenTimesBonusPoints[i];
                    //Debug.Log("Stolen times bonus: " + stolenTimesBonusPoints[i]);
                    break;
                }
            }
        }

        return levelScore;
    } 

    private string formatTime(float rawSeconds)
    {
        int min = (int)(rawSeconds / 60.0f);
        int sec = (int)(rawSeconds % 60.0f);
        string format = min.ToString("00") + ":" + sec.ToString("00");
        return format;
    }

    void OnApplicationQuit()
    {
        string[] lines = System.IO.File.ReadAllLines(filename);
        int deleteLines = 0;

        //If last char of last line is a comma (ie: line is not complete), delete last line
        if (lines.Last().Last() == ',')
            deleteLines = 1;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < (lines.Length - deleteLines); i++)
            sb.AppendLine(lines[i].Replace("Cur. Attempt", "Prev. Attempt"));

        System.IO.File.WriteAllText(filename, sb.ToString());
    }
}
