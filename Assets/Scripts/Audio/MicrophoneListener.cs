using UnityEngine;
using NaughtyAttributes;

[RequireComponent(typeof(MicrophoneSelector))]
public class MicrophoneListener : MonoBehaviour
{
    [Header("Microphone Settings")]
    [SerializeField] [Range(0f, 4f)] private float sensitivity = 1f;
    [SerializeField] private int sampleRate = 44100;
    
    [Header("Debug")]
    [SerializeField] private bool playMicAudio;
    [ProgressBar("Volume Monitor", 1.0f)]
    [SerializeField] private float currentLoudness;
    
    // Microphone settings
    private MicrophoneSelector _micSelector;
    private string _selectedMicrophone;

    // Clip recording variables
    private AudioClip _audioRecording;
    private float _recordingTime;

    private int _activeRecordings = 0;

    void Start()
    {
        _micSelector = GetComponent<MicrophoneSelector>();
        
        _selectedMicrophone = _micSelector.selectedMicrophone;
    }
    
    /**
     * Returns the starting sample position of the recording.
     * Use this value as the argument in the StopRecording() method.
     */
    public float StartRecording()
    {
        const bool shouldLoop = false;
        const int lengthSec = 3600; // one hour
        
        if (_activeRecordings == 0)
            _audioRecording = Microphone.Start(
                _selectedMicrophone, 
                shouldLoop, 
                lengthSec, 
                sampleRate);
        
        _activeRecordings++;
        
        return Microphone.GetPosition(_selectedMicrophone);
    }

    /**
     * Returns an AudioClip of the microphone's input.
     * Provide the returned value from StartRecording()
     * as the argument.
     */
    public AudioClip StopRecording(int startSample)
    {
        // Get necessary values
        var endSample = Microphone.GetPosition(_selectedMicrophone);
        var sampleCount = endSample - startSample;
        var channels = _audioRecording.channels;
        var frequency = _audioRecording.frequency;
        
        if (--_activeRecordings == 0) Microphone.End(_selectedMicrophone);

        if (endSample - startSample <= 0) return null;
        
        var clipName = startSample + "-" + endSample + "-" + sampleCount;
        var audioClip = AudioClip.Create(clipName, sampleCount, channels, frequency, false);
        
        var data = new float[sampleCount * channels];
        _audioRecording.GetData(data, startSample);
        
        for (int i = 0; i < data.Length; i++)
        {
            data[i] *= sensitivity;
        
            if (data[i] > 1f) data[i] = 1f;
            if (data[i] < -1f) data[i] = -1f;
        }
    
        audioClip.SetData(data, 0);

        return audioClip;
    }
    
}