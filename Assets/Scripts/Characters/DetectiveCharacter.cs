using UnityEngine;
using TMPro;
using LLMUnity;

public class DetectiveCharacter : MonoBehaviour
{
    [Header("LLM Components")]
    public LLMAgent agent;

    [Header("UI Elements")]
    public TMP_InputField inputField;
    public TextMeshProUGUI outputField;

    private string _currentEmotion;
    private float _emotionCertainty;

    private string[] _visibleObjects;

    void Start()
    {
        inputField.onSubmit.AddListener(OnInputFieldSubmit);

        string init = "This is a system prompt to tell you to start questioning the user. Please talk to them like you and them are real. Remember to use eye dialect to represent a boston accent.";
        _ = agent.Chat(init, HandleReply, HandleReplyCompleted);
    }

    void OnInputFieldSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        _ = agent.Chat(text, HandleReply, HandleReplyCompleted);

        inputField.text = "";
        inputField.ActivateInputField();
    }

    public void UpdateEmotion(string emotion, float certainty)
    {
        _currentEmotion = emotion;
        _emotionCertainty = certainty;
    }

    public void UpdateObjectsInView(string[] objects)
    {
        _visibleObjects = objects;
    }

    void HandleReply(string reply)
    {
        outputField.SetText(reply);
    }

    void HandleReplyCompleted()
    {
        Debug.Log("AI has finished speaking.");
    }
}