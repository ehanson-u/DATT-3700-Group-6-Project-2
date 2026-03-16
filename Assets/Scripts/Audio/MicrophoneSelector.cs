using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;

public class MicrophoneSelector : MonoBehaviour
{
    [Dropdown("GetAvailableMics")]
    [Tooltip("Select your input device")]
    public string selectedMicrophone;

    private List<string> GetAvailableMics()
    {
        string[] devices = Microphone.devices;
        
        if (devices == null || devices.Length == 0)
            return new List<string> {};
        
        return devices.ToList();
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(selectedMicrophone) && Microphone.devices.Length > 0)
            selectedMicrophone = Microphone.devices[0];
    }
}