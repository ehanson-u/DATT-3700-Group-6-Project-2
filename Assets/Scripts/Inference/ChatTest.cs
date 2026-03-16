using LLMUnity;
using UnityEngine;

public class ChatTest : MonoBehaviour
{
    public LLMAgent agent; // Drag your AI_Character here in the Inspector

    void Start()
    {
        // Send a message to the AI
        _ = agent.Chat("Hello Detective. You wanted to talk to me?", HandleReply);
    }

    // This function is called every time the AI generates a new piece of text
    void HandleReply(string reply)
    {
        Debug.Log("AI says: " + reply);
    }
}
