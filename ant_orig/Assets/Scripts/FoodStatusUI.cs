using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FoodStatusUI : MonoBehaviour
{
    public Image carryingFoodSign;
    public Sprite carryingFoodSprite;
    public Sprite notCarryFoodSprite;
    public TextMeshProUGUI collectedFoodText;
    public TextMeshProUGUI requiredFoodText;
    public TextMeshProUGUI remainingFoodText;
    public float defaultDuration = 2.0f;
    private int collected;
    private int remaining;
    private float foodStatusDisplayTime;
    private bool showStatus = false;
    private ItemCupcake playerCupcake;
    private CanvasGroup canvasGroup;

    void Awake() {
        canvasGroup = this.GetComponent<CanvasGroup>();
    }

    // Start is called before the first frame update
    void Start()
    {
        collected = 0;
        remaining = GameProgressionHelper.foodCountLevels[GameProgressionHelper.currentLevel];
        canvasGroup.alpha = 0;
        carryingFoodSign.sprite = notCarryFoodSprite;
        collectedFoodText.text = "0";
        requiredFoodText.text = GameProgressionHelper.requiredFoodLevels[GameProgressionHelper.currentLevel].ToString();
        remainingFoodText.text = remaining.ToString();
    }

    void OnEnable() 
    {
        CupcakeManager.attachChangeEvent += onCupckeAttachChanged;
        CupcakeManager.itemDropEvent += onCupcakeDropped;
        CupcakeManager.cupcakeCollectedEvent += onCupcakeCollected;
    }

    void OnDisable() 
    {
        CupcakeManager.attachChangeEvent -= onCupckeAttachChanged;
        CupcakeManager.itemDropEvent -= onCupcakeDropped;
        CupcakeManager.cupcakeCollectedEvent -= onCupcakeCollected;
    }

    // Update is called once per frame
    void Update()
    {
        if(OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0)
        {
            displayFoodStatus(defaultDuration);
        }
    }

    public void displayFoodStatus(float duration)
    {
        foodStatusDisplayTime = duration;
        if(!showStatus)
            StartCoroutine(foodStatusLifeTime());
    }

    private void onCupckeAttachChanged(ItemCupcake sender, Transform t)
    {
        if(t.tag == "Player")
        {
            playerCupcake = sender;
            carryingFoodSign.sprite = carryingFoodSprite;
            displayFoodStatus(5.0f);
        }
        else if(sender.Equals(playerCupcake))
        {
            playerCupcake = null;
            carryingFoodSign.sprite = notCarryFoodSprite;
            displayFoodStatus(5.0f);
        }
    }

    private void onCupcakeDropped(ItemCupcake sender, Transform t)
    {
        if(t.tag == "Player")
        {
            playerCupcake = null;
            carryingFoodSign.sprite = notCarryFoodSprite;
            displayFoodStatus(5.0f);
        }
    }

    private void onCupcakeCollected(NestTrigger sender, ItemCupcake item)
    {
        if(item.Equals(playerCupcake))
        {
            collected++;
            collectedFoodText.text = collected.ToString();
        }
        remaining--;
        remainingFoodText.text = remaining.ToString();
        displayFoodStatus(5.0f);
    }

    IEnumerator foodStatusLifeTime()
    {
        canvasGroup.alpha = 1.0f;
        showStatus = true;
        while(foodStatusDisplayTime > 0)
        {
            yield return new WaitForEndOfFrame();
            foodStatusDisplayTime -= Time.deltaTime;
        }
        canvasGroup.alpha = 0;
        showStatus = false;
    }
}
