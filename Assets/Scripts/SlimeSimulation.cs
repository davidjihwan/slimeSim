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

    public enum SpawnMode {random, inwardsFullCircle, outwardsFullCircle, randomFullCircle, 
                           inwardsCircle, outwardsPoint, randomPoint};

    // Seb Lague does a [SerializeField, HideInInspector] here. Has to do with saving, but not sure of its exact purpose
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
        // For agent count > 20 million, it might be worth it to implement in compute shader.
        // Initializing 10 million agents currently takes around 2 seconds.
        
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

            } else if (settings.spawnMode == SpawnMode.inwardsFullCircle){

                // Particles start in a circle
                
                // float rad = ((float)i / settings.numAgents) * 2f * Mathf.PI;
                // float cos = Mathf.Cos(rad);
                // float sin = Mathf.Sin(rad);
                // pos.x = cos * circleRadius + (settings.width / 2);
                // pos.y = sin * circleRadius + (settings.height / 2);

                Vector2 insideCirc = (Random.insideUnitCircle * circleRadius);
                pos.x = insideCirc.x + (settings.width / 2);
                pos.y = insideCirc.y + (settings.height / 2);
                // Particles point inwards
                dir = Vector2.zero - insideCirc;

            } else if (settings.spawnMode == SpawnMode.outwardsFullCircle){

                Vector2 insideCirc = (Random.insideUnitCircle * circleRadius);
                pos.x = insideCirc.x + (settings.width / 2);
                pos.y = insideCirc.y + (settings.height / 2);
                // Particles point outwards
                dir = insideCirc - Vector2.zero;

            } else if (settings.spawnMode == SpawnMode.randomFullCircle){

                // Particles start in a circle
                Vector2 insideCirc = (Random.insideUnitCircle * circleRadius);
                pos.x = insideCirc.x + (settings.width / 2);
                pos.y = insideCirc.y + (settings.height / 2);
                // Random direction
                // Random direction
                dir.x = Random.value - 0.5f;
                dir.y = Random.value - 0.5f;
                
            } else if (settings.spawnMode == SpawnMode.inwardsCircle){
                // Note: shows up a little weird but just due to antialiasing 
                float rad = ((float)i / settings.numAgents) * 2f * Mathf.PI;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                pos.x = cos * circleRadius + (settings.width / 2f);
                pos.y = sin * circleRadius + (settings.height / 2f);

                dir = Vector2.zero - new Vector2(cos, sin);
            // TODO: add outwardsCircle and randomCircle
            } else if (settings.spawnMode == SpawnMode.outwardsPoint){

                // Particles start at the center
                
                // Particles point outwards
                
            } else if (settings.spawnMode == SpawnMode.randomPoint){

                // Particles start at the center
                
                // Random direction 
            
            } else {
                Debug.Log("Unaccounted spawn mode");
                return;
            }

            dir.Normalize();
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

        simCS.SetFloat("sensorAngle", settings.slimeSettings.sensorAngle);
        simCS.SetFloat("sensorWidth", settings.slimeSettings.sensorWidth);
        simCS.SetFloat("moveSpeed", settings.slimeSettings.moveSpeed);
        simCS.SetFloat("turnSpeed", settings.slimeSettings.turnSpeed);
        Vector4 setColor = settings.slimeSettings.color; // color can be implicitly converted to vec4
        simCS.SetVector("color", setColor);

    // float sensorAngle;
    // float sensorWidth;
    // float sensorOffset;
    // float moveSpeed;
    // float turnSpeed;
    // float3 color;

        // Set texture fields
        simCS.SetTexture(simKernel, "Display", displayTexture);
        simCS.SetInt("height", settings.height);
        simCS.SetInt("width", settings.width);


        // TODO: delete, this is just for testing purposes
        // int threadGroupsX = Mathf.CeilToInt(settings.width / 8.0f); 
        // int threadGroupsY = Mathf.CeilToInt(settings.height / 8.0f);
        int threadGroupsX = Mathf.CeilToInt(settings.numAgents / 16f);
        Debug.Log("Thread groups x: " + threadGroupsX);
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
            int threadGroupsX = Mathf.CeilToInt(settings.numAgents / 16f);
            simCS.Dispatch(0, threadGroupsX, 1, 1); 
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
