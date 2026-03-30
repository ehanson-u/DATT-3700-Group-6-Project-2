using UnityEngine;
 // Ensure 'Sentis' package is installed in Package Manager
using System.Linq;
using System.Collections.Generic;
using Unity.InferenceEngine;

public class SentisSpeaker : MonoBehaviour
{
    [Header("Sentis Assets")]
    public ModelAsset customVoiceModel; // Drag your .onnx here
    public AudioSource audioSource;

    private Worker engine; // Replacement for IWorker
    private Dictionary<char, int> phonemeMap = new Dictionary<char, int>();

    void Start()
    {
        // 1. Load and compile the model
        Model model = ModelLoader.Load(customVoiceModel);
        
        // 2. Create the Worker (Sentis 2.1 syntax)
        // This automatically chooses the best backend (GPU/CPU)
        engine = new Worker(model, BackendType.GPUCompute);

        // Simple mock phoneme map - Replace with your actual config logic
        string symbols = " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!?.";
        for (int i = 0; i < symbols.Length; i++) phonemeMap[symbols[i]] = i;
    }

    public void Speak(string text)
    {
        // 3. Convert text to IDs
        int[] ids = text.Select(c => phonemeMap.ContainsKey(c) ? phonemeMap[c] : 0).ToArray();

        // 4. Create Input Tensor (New Generic Syntax)
        using Tensor<int> inputTensor = new Tensor<int>(new TensorShape(1, ids.Length), ids);

        // 5. Run the AI
        engine.Schedule(inputTensor);

        // 6. Extract Audio (New PeekOutput Syntax)
        Tensor<float> outputTensor = engine.PeekOutput() as Tensor<float>;
        float[] audioData = outputTensor.DownloadToArray();

        PlayAudio(audioData);
    }

    private void PlayAudio(float[] data)
    {
        AudioClip clip = AudioClip.Create("TTS", data.Length, 1, 22050, false);
        clip.SetData(data, 0);
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void OnDestroy() => engine?.Dispose();
}