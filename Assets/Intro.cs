using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; 
using System.Collections;

public class Intro : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI storyText;
    public GameObject skipPrompt; 

    [Header("Story Settings")]
    [TextArea(5, 10)] 
    public string fullText = "Last night at a lively restaurant called The Meridian, an attempted murder took place. The victim of the crime is a regular and has a peanut allergy that is well known between the servers and chefs. However, that night someone must have deliberately laced his food with nuts in an attempt to kill him. For what reason? That’s for the detective to find out… Take a look at the suspect files to choose a role.";
    
    public float typeSpeed = 0.05f;
    public string nextSceneName = "Face Landmark Detection"; 

    private bool isTypingFinished = false;

    void Start()
    {
        storyText.text = "";
        
        //hiding prompt at the start
        if (skipPrompt != null) 
            skipPrompt.SetActive(false);

        StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        foreach (char c in fullText)
        {
            storyText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        // Once the loop is done, show the prompt
        isTypingFinished = true;
        if (skipPrompt != null) 
            skipPrompt.SetActive(true);
    }

    void Update()
    {
        // space to go to next scene
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}