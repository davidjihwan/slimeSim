using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Heavily based on Sebastian Lague's slime simulation implementation.
public class SlimeSimulation : MonoBehaviour
{
    public ComputeShader simCS;
    public SimulationSettings settings;

    // Kernels are annoying to enum as you have to cast enum to int
    public const int simKernel = 0;

    public enum SpawnMode {random, inwardsCircle, randomCircle, outwardsCircle};

    // Seb Lague does a [SerializeField, HideInInspector] here. Has to do with saving, but not sure.
    protected RenderTexture trailTexture;
    protected RenderTexture diffuseTexture;
    protected RenderTexture displayTexture;

    protected ComputeBuffer agentBuffer;

    public struct Agent {
        public Vector2 position;
        public Vector2 direction; // normalized
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

        // Create and initialize agents array
        // Depending on how long this takes for a large number of agents, it might be worth it to implement in compute shader.
        // Initializing 5 million agents currently takes around 1 second.
        
        Agent[] agents = new Agent[settings.numAgents];
        float circleRadius = settings.height / 2.5f;

        float beforeSec = System.DateTime.Now.Second;
        float beforeMs = System.DateTime.Now.Millisecond;
        for (int i = 0; i < settings.numAgents; i++){
            Vector2 pos = new Vector2();
            Vector2 dir = new Vector2();

            if (settings.spawnMode == SpawnMode.random){
                // Random position
                pos.x = Random.value * settings.width;
                pos.y = Random.value * settings.height;
                // Random direction
                dir.x = Random.value - 0.5f;
                dir.y = Random.value - 0.5f;
                dir.Normalize();
            } else if (settings.spawnMode == SpawnMode.inwardsCircle){
                // Particles start in a circle
                float rad = ((float)i / settings.numAgents) * 2f * Mathf.PI;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                pos.x = cos * circleRadius + (settings.width / 2);
                pos.y = sin * circleRadius + (settings.height / 2);
                // Particles point towards center
                dir = Vector2.zero - new Vector2(cos, sin);
                // TODO: check if these are already normalized
            } else if (settings.spawnMode == SpawnMode.randomCircle){
                // Particles start in a circle
                
                // Random direction
                
            } else if (settings.spawnMode == SpawnMode.outwardsCircle){
                // Particles start at the center
                
                // Particles point outwards
                
            } else {
                Debug.Log("Unaccounted spawn mode");
                return;
            }

            agents[i].position = pos;
            agents[i].direction = dir;

        }
        float afterSec = System.DateTime.Now.Second;
        float afterMs = System.DateTime.Now.Millisecond + ((afterSec - beforeSec) * 1000);
        Debug.Log("Initializing " + settings.numAgents.ToString("#,#") + " agents took " + (afterMs - beforeMs) + " ms / " + (afterMs - beforeMs) / 1000 + " seconds.");

        // Set agents buffer in shader
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Agent));
        agentBuffer = new ComputeBuffer(settings.numAgents, stride);
        agentBuffer.SetData(agents);
        simCS.SetBuffer(simKernel, "agents", agentBuffer); // Remember to return data back to agents after dispatching
        simCS.SetInt("numAgents", settings.numAgents);


        // Set other relevant fields in the compute shader
        simCS.SetTexture(simKernel, "Display", displayTexture);

        // TODO: delete, this is just for testing purposes
        // int threadGroupsX = Mathf.CeilToInt(settings.width / 8.0f); 
        // int threadGroupsY = Mathf.CeilToInt(settings.height / 8.0f);
        int threadGroupsX = Mathf.CeilToInt(settings.numAgents / 16f);
        simCS.Dispatch(0, threadGroupsX, 1, 1); 
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
