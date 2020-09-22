using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;


public class behaviour : MonoBehaviour
{
    public delegate void gameProgressionDelegate();
    public static event gameProgressionDelegate gameStartEvent;
    public static event gameProgressionDelegate gamePauseEvent;
    public static event gameProgressionDelegate gameResumeEvent;
    public static event gameProgressionDelegate gameSucceedEvent;
    public static event gameProgressionDelegate gameFailEvent;
    private static int instances = 0;
    public float playerSpeed = 0.4f;
    public float boostRate = 0.4f;
    //public Material RedArrow;
    //public Material GreenArrow;
    public GameObject nest;
    public GameObject compass;
    public GameObject PlayerText;
    public GameObject TimerText;
    public GameObject gameProgressionPanel;
    public Brightness BrightnessRef;
    private static float secondsCount = 0;
    private static float rawSecondsCount = 0;
    private static int minuteCount = 0;
    private static float lastLevelSec = 0;
    private static LevelPerformance perf = new LevelPerformance(0, 0, 0, 0, 0);
    private float actualBoostRate;
    private float playerTextDisplayTime;
    private float angleToPosition;
    private int currentState;
    private int collectedFood;
    private int carryingFood;
    private bool showText;
    private bool timerOn;
    private bool inputLock;
    private bool hasFood;
    private bool antEyeRunning;
    private bool gameOver;
    private bool stageClear;
    private GameObject physicalNarrator;
    private GameObject laserPointer;
    private ItemCupcake cupcake;
    private AudioSource narratorAudio;
    private Button leftButton;
    private Button rightButton;
    private TextMeshProUGUI TMPPlayer;
    private TextMeshProUGUI TMPTimer;
    private TextMeshProUGUI TMPPanelHeader;
    private TextMeshProUGUI TMPPanelContent;
    private TextMeshProUGUI TMPPanelButtonL;
    private TextMeshProUGUI TMPPanelButtonR;
    private Rigidbody playerRigidBody;
    private OVRPlayerController controller;
    private leaderboard leaderboard;

    // State info (deprecated)
    // 0 SearchFreelyForFood
    // 1 Food found and bring it back home
    // 2 Get another one
    // 3 Return home before Kidnapped Robot
    // 4 After Kidnapped Robot get back home exp 1
    // 5 Get more food using vector
    // 6 Return home using vector
    // 7 After Kidnapped Robot get back home exp 2
    // 8 Get more food using vector
    // 9 Return home using vector
    // 10 After Kidnapped Robot get back home exp 3


    // Use this for initialization

    void Awake()
    {
        instances++;
        if(!laserPointer) laserPointer = GameObject.Find("LaserPointer");
        if(!physicalNarrator) physicalNarrator = GameObject.Find("Physical Narrator");
        if(!gameProgressionPanel) gameProgressionPanel = GameObject.Find("GPPanel");
        if(!PlayerText) PlayerText = GameObject.Find("PlayerText");
        if(!TimerText) TimerText = GameObject.Find("TimerText");
        if(!nest) nest = GameObject.Find("Home");
        leftButton = gameProgressionPanel.transform.Find("ButtonLeft").GetComponent<Button>();
        rightButton = gameProgressionPanel.transform.Find("ButtonRight").GetComponent<Button>();
        leaderboard = GameObject.Find("Leaderboard").GetComponent<leaderboard>();
    }

    void Start()
    {
        actualBoostRate = boostRate;
        collectedFood = 0;
        carryingFood = 0;
        currentState = 0;
        showText = false;
        inputLock = true;
        hasFood = false;
        antEyeRunning = false;
        timerOn = false;
        gameOver = false;
        stageClear = false;

        playerRigidBody = this.GetComponent<Rigidbody>();
        controller = this.GetComponent<OVRPlayerController>();
        narratorAudio = physicalNarrator.GetComponent<AudioSource>();
        TMPPlayer = PlayerText.GetComponent<TextMeshProUGUI>();
        TMPTimer = TimerText.GetComponent<TextMeshProUGUI>();
        TMPPanelHeader = gameProgressionPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        TMPPanelContent = gameProgressionPanel.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        TMPPanelButtonL = gameProgressionPanel.transform.GetChild(2).GetChild(0).GetComponent<TextMeshProUGUI>();
        TMPPanelButtonR = gameProgressionPanel.transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>();


        controller.Acceleration = playerSpeed;
        laserPointer.SetActive(false);
        gameProgressionPanel.SetActive(false);

        if(GameProgressionHelper.setFirstLevelAsTutorial && GameProgressionHelper.currentLevel == 0)
            StartCoroutine(displayTutorialText());
        else
        {
            StartCoroutine(displayLevelText());
            physicalNarrator.SetActive(false);
            narratorAudio.Stop();
        }

        Debug.Log("Current State: " + currentState);
        //gameObject.GetComponent<WriteInFile>().enabled=true;
    }

    // Register events when script is enabled
    void OnEnable() 
    {
        CupcakeManager.notEnoughCupcakeEvent += onNotEnoughFood;
        CupcakeManager.fullCapacityEvent += onFullCapacity;
        CupcakeManager.attachChangeEvent += onCupckeAttachChanged;
        CupcakeManager.itemDropEvent += onCupcakeDropped;
        CupcakeManager.cupcakeCollectedEvent += onCupcakeCollected;
        CupcakeManager.inappropriateTypeEvent += onGetToWrongNest;
        PredatorBehaviourController.attackEvent += onPredatorAttack;
        leftButton.onClick.AddListener(onLeftButtonClick);
        rightButton.onClick.AddListener(onRightButtonClick);
    }

    // Deregister events when script is disabled
    void OnDisable() 
    {
        CupcakeManager.notEnoughCupcakeEvent -= onNotEnoughFood;
        CupcakeManager.fullCapacityEvent -= onFullCapacity;
        CupcakeManager.attachChangeEvent -= onCupckeAttachChanged;
        CupcakeManager.itemDropEvent -= onCupcakeDropped;
        CupcakeManager.cupcakeCollectedEvent -= onCupcakeCollected;
        CupcakeManager.inappropriateTypeEvent -= onGetToWrongNest;
        PredatorBehaviourController.attackEvent -= onPredatorAttack;
        leftButton.onClick.RemoveListener(onLeftButtonClick);
        rightButton.onClick.RemoveListener(onRightButtonClick);
    }

    // Clear all registered events when the last instance has been destroyed
    void OnDestroy() 
    {
        instances--;
        if(instances <= 0)
        {
            gameStartEvent = null;
            gamePauseEvent = null;
            gameResumeEvent = null;
            gameSucceedEvent = null;
            gameFailEvent = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (timerOn)
        {
            secondsCount += Time.deltaTime;
            rawSecondsCount += Time.deltaTime;

            if (minuteCount > 0)
                TMPTimer.text = minuteCount.ToString("00") + ":" + ((int)secondsCount).ToString("00");
            else
                TMPTimer.text = ((int)secondsCount).ToString();

            if (secondsCount >= 60.0f)
            {
                minuteCount++;
                secondsCount -= 60.0f;
            }
        }

        // Ant Eye image effect
        if (antEyeRunning)
        {

            Vector3 targetDir = nest.transform.position - transform.position;
            targetDir = targetDir.normalized;
            float dot = Vector3.Dot(targetDir, transform.forward);
            angleToPosition = Mathf.Acos(dot) * Mathf.Rad2Deg;

            BrightnessRef.brightness = 1.2f * (1f / angleToPosition);

            //Debug.Log(RenderSettings.ambientLight);
        }

        // Input for Ant Eye and dropping food
        if(!inputLock)
        {
            if(OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0)
            {
                antEyeRunning = true;
            }
            else
            {
                antEyeRunning = false;
                BrightnessRef.brightness = 1f;
            }

            if(hasFood && OVRInput.GetDown(OVRInput.Button.Two))
            {
                cupcake.detach();
                TMPPlayer.color = new Color(0, 0, 0);
                sendPlayerText("You've dropped your food", 3.0f);
            }
        }
        
        actualBoostRate = (hasFood) ? 0 : boostRate;
        controller.Acceleration = playerSpeed * (1.0f + OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) * actualBoostRate);
    }

    public void increaseState()
    {
        currentState++;
        Debug.Log("Current State: " + currentState);
    }

    public int getCurrentState()
    {
        return currentState;
    }

    public bool isOver()
    {
        return gameOver;
    }

    private void sendPlayerText(string text, float duration)
    {
        TMPPlayer.text = text;
        playerTextDisplayTime = duration;
        if(!showText)
            StartCoroutine(playerTextLifeTime());
    }

    private void displayGameProgressionPanel(bool stageClear)
    {
        if(stageClear)
        {
            displayGameProgressionPanel(stageClear, "Do you want to go to next level?");
        }
        else
        {
            displayGameProgressionPanel(stageClear, "You failed to complete the task");
        }
    }

    private void displayGameProgressionPanel(bool stageClear, string contentText)
    {
        if(stageClear)
        {
            TMPPanelHeader.text = "Stage Clear";
            TMPPanelButtonL.text = "Yes";
            TMPPanelButtonR.text = "No";
        }
        else
        {
            TMPPanelHeader.text = "Game Over";
            TMPPanelButtonL.text = "Retry";
            TMPPanelButtonR.text = "Quit";
        }
        TMPPanelContent.text = contentText;
        gameProgressionPanel.SetActive(true);
        laserPointer.SetActive(true);
    }

    IEnumerator playerTextLifeTime()
    {
        PlayerText.SetActive(true);
        showText = true;
        while(playerTextDisplayTime > 0)
        {
            yield return new WaitForEndOfFrame();
            playerTextDisplayTime -= Time.deltaTime;
        }
        TMPPlayer.color = new Color(0, 0, 0);
        PlayerText.SetActive(false);
        showText = false;
    }

    IEnumerator displayTutorialText()
    {
        // Disabling player movement
        controller.SetMoveScaleMultiplier(0f);
        sendPlayerText("Ant Navigation Challenge", 26.0f);
        yield return new WaitForSeconds(26.0f);
        yield return readyCountdown();
    }

    IEnumerator displayLevelText()
    {
        // Disabling player movement
        controller.SetMoveScaleMultiplier(0f);
        sendPlayerText("This level you are required to collect <color=red>" + GameProgressionHelper.requiredFoodLevels[GameProgressionHelper.currentLevel] + "</color> cupcakes", 6.0f);
        yield return new WaitForSeconds(6.0f);
        yield return readyCountdown();
    }

    IEnumerator readyCountdown()
    {
        TMPPlayer.color = new Color(255, 0, 0);
        sendPlayerText("Get Ready...", 3.0f);
        yield return new WaitForSeconds(3.0f);
        for(int i = 3; i > 0; i--) {
            TMPPlayer.color = new Color(255, 0, 0);
            sendPlayerText(i.ToString(), 1.0f);
            yield return new WaitForSeconds(1.0f);
        }
        TMPPlayer.color = new Color(0, 0, 0);
        // Enabling player movement
        controller.SetMoveScaleMultiplier(1f);
        if(GameProgressionHelper.setFirstLevelAsTutorial && GameProgressionHelper.currentLevel == 0)
        {
            sendPlayerText("Hint:\nUse <color=red>left joystick</color> to move\n And <color=red>left index trigger</color> to speed up", 6.0f);
        }
        if(!GameProgressionHelper.setFirstLevelAsTutorial || GameProgressionHelper.currentLevel > 0)
        {
            inputLock = false;
        }
        TMPTimer.color = new Color(255, 255, 255, 0.5f);
        TimerText.SetActive(true);
        timerOn = true;
        if(gameStartEvent != null)
            gameStartEvent.Invoke();
    }

    IEnumerator carryingFoodText()
    {
        sendPlayerText("Now bring the food to your nest", 3.0f);
        yield return new WaitForSeconds(3.0f);
        sendPlayerText("Hint:\nHold <color=red>right hand trigger</color> to see food status", 8.0f);
    }

    IEnumerator dropFoodText()
    {
        sendPlayerText("You can not speed up while you're carrying food", 4.0f);
        yield return new WaitForSeconds(4.0f);
        sendPlayerText("Hint:\nYou can press <color=red>B button</color> to drop your food", 8.0f);
    }

    IEnumerator findMoreFoodText()
    {
        sendPlayerText("Well done!", 2.0f);
        yield return new WaitForSeconds(2);
        sendPlayerText("You need to find more!", 3.0f);
    }

    IEnumerator lastFoodText()
    {
        sendPlayerText("Almost there!", 2.0f);
        yield return new WaitForSeconds(2);
        sendPlayerText("You need to find one more piece!", 3.0f);
    }

    IEnumerator challengeAudio()
    {
        timerOn = false;
        controller.SetMoveScaleMultiplier(0f);
        playerRigidBody.freezeRotation = true;
        
        narratorAudio.clip = Resources.Load<AudioClip>("Narration 4");
        narratorAudio.Play();
        yield return new WaitForSeconds(20);
        yield return countdownTimerText();
        StartCoroutine(displayChallengeResult());
        narratorAudio.clip = Resources.Load<AudioClip>("Narration 5");
        narratorAudio.Play();
    }

    IEnumerator countdownTimerText()
    {
        PlayerText.SetActive(true);
        for(int i = 5; i > 0; i--) {
            TMPPlayer.text = i.ToString();
            yield return new WaitForSeconds(1);
        }
        PlayerText.SetActive(false);
    }


    IEnumerator displayChallengeResult()
    {
        Vector3 targetDir = nest.transform.position - transform.position;
        targetDir = targetDir.normalized;
        float dot = Vector3.Dot(targetDir, transform.forward);
        angleToPosition = Mathf.Acos(dot) * Mathf.Rad2Deg;
        Debug.Log(angleToPosition);
        Debug.Log("Nest Position " + nest.transform.position);
        Debug.Log("Player position " + transform.position);
        Debug.Log("Target Direction " + targetDir);

        //Rounding angle to 2 decimal place
        angleToPosition = Mathf.Round(angleToPosition * 100.0f) / 100.0f;
        perf.angle = angleToPosition;
        GameProgressionHelper.info.angle = angleToPosition;
        if (angleToPosition < 45)
        {
            sendPlayerText("YOU WERE OUT BY " + "<color=green>" + angleToPosition + "</color> DEGREES", 6.0f);
        }
        else if (angleToPosition <= 90)
        {
            sendPlayerText("YOU WERE OUT BY " + "<color=yellow>" + angleToPosition + "</color> DEGREES", 6.0f);
        }
        else
        {
            sendPlayerText("YOU WERE OUT BY " + "<color=red>" + angleToPosition + "</color> DEGREES", 6.0f);
        }
        yield return new WaitForSeconds(6);
        sendPlayerText("<color=red>ANT EYE</color>", 4.0f);
        yield return new WaitForSeconds(4);
        sendPlayerText("Hint:\nHold <color=red>right index trigger</color> and look around slowly!", 8.0f);
        inputLock = false;
        yield return new WaitForSeconds(8);

        timerOn = true;
        controller.SetMoveScaleMultiplier(1f);
        playerRigidBody.freezeRotation = false;
        if(gameResumeEvent != null) 
            gameResumeEvent.Invoke();

        yield return carryingFoodText();
    }

    IEnumerator displayFinishedText()
    {
        GameProgressionHelper.info.level = GameProgressionHelper.currentLevel + 1;
        GameProgressionHelper.info.time = rawSecondsCount;
        leaderboard.SaveCompleteResult(GameProgressionHelper.info);

        physicalNarrator.transform.position = new Vector3(0.63f, -27.284f, 1.055f);
        physicalNarrator.transform.rotation = Quaternion.Euler(-12.529f, 231.681f, 5.573f);
        physicalNarrator.SetActive(true);
        narratorAudio.clip = Resources.Load<AudioClip>("Narration 6");
        narratorAudio.Play();
        sendPlayerText("Well done!", 3.0f);
        yield return new WaitForSeconds(3);
        sendPlayerText("You have completed\n <color=red>the Ant Navigation Challenge!</color>", 5.0f);
        yield return new WaitForSeconds(5);
        sendPlayerText("Want to know more?", 3.0f);
        yield return new WaitForSeconds(3);
        sendPlayerText("Ask us a question!", 3.0f);
        yield return new WaitForSeconds(3);
        leaderboard.LoadLeaderBoardStats();
        leaderboard.ShowLeaderBoard();
    }

    IEnumerator displayFinishedTextFailed()
    {
        GameProgressionHelper.info.level = GameProgressionHelper.currentLevel;
        GameProgressionHelper.info.time = rawSecondsCount;
        leaderboard.SaveCompleteResult(GameProgressionHelper.info);

        sendPlayerText("Thank you for playing\n <color=red>the Ant Navigation Challenge!</color>", 6.0f);
        yield return new WaitForSeconds(6);
        sendPlayerText("Want to know more?", 3.0f);
        yield return new WaitForSeconds(3);
        sendPlayerText("Ask us a question!", 3.0f);
        yield return new WaitForSeconds(3);
        leaderboard.LoadLeaderBoardStats();
        leaderboard.ShowLeaderBoard();
    }

    public GameObject getFoodObject()
    {
        return (cupcake) ? cupcake.gameObject : null;
    }

    private void onFullCapacity(ItemCupcake sender, Transform attach)
    {
        if(attach.tag == "Player")
        {
            TMPPlayer.color = new Color(0, 0, 0);
            sendPlayerText("You can not carry more than <color=red>" + GameProgressionHelper.maxFoodCapacity + "</color> cupcakes", 3.0f);
        }
    }

    private void onCupckeAttachChanged(ItemCupcake sender, Transform currentAttach)
    {
        if(currentAttach.tag == "Player")
        {
            cupcake = sender;
            hasFood = true;
            carryingFood++;

            //Display hints for tutorial
            if(GameProgressionHelper.setFirstLevelAsTutorial && GameProgressionHelper.currentLevel == 0)
            {
                if (currentState == 0 && inputLock)
                {
                    StartCoroutine(challengeAudio());
                    if(gamePauseEvent != null)
                        gamePauseEvent.Invoke();
                }
                if (currentState == 1)
                {
                    StartCoroutine(dropFoodText());
                }
            }
        }
        else if(sender.Equals(cupcake))
        {
            cupcake = null;
            carryingFood = 0;
            hasFood = false;
            perf.stolenTimes++;

            TMPPlayer.color = new Color(255.0f, 0f, 0f);
            sendPlayerText("Your food has been stolen!", 3.0f);
        }
    }

    private void onCupcakeDropped(ItemCupcake sender, Transform t)
    {
        if(t.tag == "Player")
        {
            cupcake = null;
            carryingFood = 0;
            hasFood = false;
        }
    }

    private void onCupcakeCollected(NestTrigger sender, ItemCupcake item) 
    {
        if (item.Equals(cupcake))
        {
            collectedFood += carryingFood;
            increaseState();

            if(collectedFood >= GameProgressionHelper.requiredFoodLevels[GameProgressionHelper.currentLevel])
            {
                timerOn = false;
                antEyeRunning = false;
                inputLock = true;
                BrightnessRef.brightness = 1f;
                // Disabling player movement
                controller.SetMoveScaleMultiplier(0f);

                //compass.GetComponent<Compass>().setTarget(food.transform);
                //compass.GetComponent<Renderer>().material = RedArrow;

                //Calculate score when each level pass
                perf.collectedFood = collectedFood;
                perf.time = rawSecondsCount - lastLevelSec;
                GameProgressionHelper.info.score += leaderboard.calculateLevelScore(perf);

                //Reinitialize value for next level
                lastLevelSec = rawSecondsCount;
                perf.retry = 0;
                perf.stolenTimes = 0;

                if(GameProgressionHelper.isLastLevel())
                    StartCoroutine(displayFinishedText());
                else
                {
                    displayGameProgressionPanel(true);
                }

                // Game progression settings
                gameOver = true;
                stageClear = true;
                if(gameSucceedEvent != null)
                    gameSucceedEvent.Invoke();
            }

            // Display food status text
            if(collectedFood < GameProgressionHelper.requiredFoodLevels[GameProgressionHelper.currentLevel] - 1)
            {
                StartCoroutine(findMoreFoodText());
            }
            else if(collectedFood == GameProgressionHelper.requiredFoodLevels[GameProgressionHelper.currentLevel] - 1)
            {
                StartCoroutine(lastFoodText());
            }

            //Tutorial audio
            if(GameProgressionHelper.setFirstLevelAsTutorial && GameProgressionHelper.currentLevel == 0)
            {
                if (currentState == 1)
                {
                    narratorAudio.clip = Resources.Load<AudioClip>("Narration 2");
                    narratorAudio.Play();
                }
                if (currentState == 2)
                {
                    narratorAudio.clip = Resources.Load<AudioClip>("Narration 3");
                    narratorAudio.Play();
                }
            }
        }
    }

    private void onGetToWrongNest(NestTrigger sender, ItemCupcake item)
    {
        if(item.Equals(cupcake))
        {
            sendPlayerText("You get to a wrong nest and\nYour food will not be collected", 3.0f);
        }
    }

    // Stage failed when there is not enough food to meet requirement
    private void onNotEnoughFood()
    {
        if(!gameOver)
        {
            // Stopping timer
            timerOn = false;

            // Disbling Ant Eye and other player input
            antEyeRunning = false;
            inputLock = true;
            BrightnessRef.brightness = 1f;

            // Disabling player movement
            controller.SetMoveScaleMultiplier(0f);

            displayGameProgressionPanel(false, "There is not enough food to collect");

            // Game progression settings
            gameOver = true;
            stageClear = false;
            if(gameFailEvent != null)
                gameFailEvent.Invoke();
        }
    }

    // Stage failed when player is caught by a predator
    private void onPredatorAttack(Transform target)
    {
        if(!gameOver && target.Equals(transform))
        {
            // Stopping timer
            timerOn = false;

            // Disbling Ant Eye and other player input
            antEyeRunning = false;
            inputLock = true;
            BrightnessRef.brightness = 1f;

            // Disabling player movement
            controller.SetMoveScaleMultiplier(0f);

            // Drop food when player is carrying it;
            if(hasFood)
            {
                cupcake.detach();
            }

            displayGameProgressionPanel(false, "You have been caught by predator");

            // Game progression settings
            gameOver = true;
            stageClear = false;
            if(gameFailEvent != null)
                gameFailEvent.Invoke();
        }
    }

    // When player clicks the left button of game progression panel
    private void onLeftButtonClick()
    {
        laserPointer.SetActive(false);
        gameProgressionPanel.SetActive(false);
        if(stageClear) 
            GameProgressionHelper.nextLevel();
        else
            perf.retry++;
        SceneManager.LoadScene(0);
    }

    // When player clicks the right button of game progression panel
    private void onRightButtonClick()
    {
        laserPointer.SetActive(false);
        gameProgressionPanel.SetActive(false);
        if(stageClear)
            StartCoroutine(displayFinishedText());
        else
            StartCoroutine(displayFinishedTextFailed());
    }
}