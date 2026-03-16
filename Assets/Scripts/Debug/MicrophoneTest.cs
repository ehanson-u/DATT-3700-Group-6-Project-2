using TMPro;
using UnityEngine;

public class MicrophoneTest : MonoBehaviour
{
    [SerializeField] private MicrophoneListener listener;
    [SerializeField] private TextMeshProUGUI buttonText;
    
    private bool _testing = false;

    public void MicTest()
    {
        _testing = !_testing;
        
        if (_testing)
        {
            buttonText.text = "Complete Test";
        }
        else
        {
            buttonText.text = "Test Microphone";
        }
    }
    
}
