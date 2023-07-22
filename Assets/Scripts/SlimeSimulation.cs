using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Heavily based on Sebastian Lague's slime simulation implementation.
public class SlimeSimulation : MonoBehaviour
{
    public ComputeShader simCS;
    public SimulationSettings settings;

    // Kernels are annoying to enum as you have to cast to int
    public const int simKernel = 0;

    public enum SpawnMode {random, inwardsCircle, randomCircle, outwardsCircle};

    // Seb Lague does a [SerializeField, HideInInspector] here. Has to do with saving, but not sure.
    protected RenderTexture trailTexture;
    protected RenderTexture diffuseTexture;
    protected RenderTexture displayTexture;

    protected ComputeBuffer agentBuffer;

    public struct Agent {
        Vector2 position;
        Vector2 direction;
    }

    // Start is called before the first frame update
    void Start(){
        Init();
        GetComponentInChildren<MeshRenderer>().material.mainTexture = displayTexture;
        // Debug.Log("Texture width: " + displayTexture.width + ", texture height: " + displayTexture.height);
    }

    void Init(){
        // Initialize render textures
        initRenderTexture(ref trailTexture);
        initRenderTexture(ref diffuseTexture);
        initRenderTexture(ref displayTexture);

        // Create agents array
        Agent[] agents = new Agent[settings.numAgents];

        // Set agents buffer in shader
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Agent));
        Debug.Log("Stride: " + stride);
        agentBuffer = new ComputeBuffer(settings.numAgents, stride);
        agentBuffer.SetData(agents);
        simCS.SetBuffer(simKernel, "agents", agentBuffer); // Remember to return data back to agents after dispatching

        // Set other relevant fields in the compute shader
        simCS.SetTexture(simKernel, "Result", displayTexture);

        // int threadGroupsX = Mathf.CeilToInt(settings.width / 8.0f); 
        // int threadGroupsY = Mathf.CeilToInt(settings.height / 8.0f);
        // simCS.Dispatch(0, threadGroupsX, threadGroupsY, 1); 
    }

    void initRenderTexture(ref RenderTexture texture){
        // Render texture contents can become "lost" in certain events
        if (texture == null || !texture.IsCreated() || texture.width != settings.width || texture.height != settings.height){            
            // Release render texture if we already have one
            if (texture != null){
                texture.Release();
            }

            // Create new render texture
            texture = new RenderTexture(settings.width, settings.height, 0); 
            texture.enableRandomWrite = true; // Allows the compute shader to modify the texture, anti-aliasing not allowed
            texture.autoGenerateMips = false;
            // Not going to worry about graphicsFormat for now
            texture.Create();
        }
    }

    // Fixed update is called every 0.02 seconds = 50 times a second.
    void FixedUpdate(){
        for (int i = 0; i < settings.stepSpeed; i++){
            // TODO:
            // Simulate();
        }
    }

    void LateUpdate(){
        // TODO: Render to the screen
    }


    void OnDestroy(){
        // Garbage collecter may catch unreleased buffers but unsafe
        agentBuffer.Release();
    }


}
