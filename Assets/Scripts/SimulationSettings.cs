using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class SimulationSettings : ScriptableObject
{
    [Header("General Settings")]
    public int width = 1920;
    public int height = 1080;
    public int numAgents = 250000;
    // public bool fitToScreen = false; // if true, width and height values are not used
    [Min(1)] 
    public int stepSpeed = 1; // 1 stepSpeed = 50 steps a second
    public SlimeSimulation.SpawnMode spawnMode;

    [Header("Trail Settings")]


    public SlimeSettings slimeSettings;
    public bool useDefaultSlimeSettings;

    [System.Serializable]
    public struct SlimeSettings{
        [Header("Sensor Settings")]
        public float sensorWidth;
        public float sensorAngle;
        public float sensorOffset;
        [Header("Movement Settings")]
        public float moveSpeed;
        public float turnSpeed;
        [Header("Color Settings")]
        public Color color;
    }

    private void OnValidate() { 
        // if (fitToScreen){
        //     width = Screen.width;
        //     height = Screen.height;
        // }

        if (useDefaultSlimeSettings){
            slimeSettings.sensorWidth = 1;
            slimeSettings.sensorAngle = 30;
            slimeSettings.sensorOffset = 30;

            slimeSettings.moveSpeed = 20;
            slimeSettings.turnSpeed = 2;

            slimeSettings.color = Color.white;
        }
    }
}
