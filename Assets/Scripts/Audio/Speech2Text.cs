using UnityEngine;
using Whisper;

public class Speech2Text : MonoBehaviour
{
    public WhisperManager whisper;

    public async System.Threading.Tasks.Task<string> Transcribe(AudioClip aClip)
    {
        if (aClip == null) return "";
    
        var result = await whisper.GetTextAsync(aClip);
        return result?.Result ?? "";
    }
    
}
