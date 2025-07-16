using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntaresController : MonoBehaviour
{
    public bool disableGravity = false;
    
    public Transform Cooxa1;
    public Transform Cooxa2;
    public Transform Cooxa3;
    public Transform Cooxa4;
    public Transform Cooxa5;
    public Transform Cooxa6;

    public float L0 = 86f;
    public float L1 = 74.28f;
    public float L2 = 140.85f;

    public float d = 40f, al = 60f, n = 20f, w = 1f, rs = 0f, ra = 0f, c = 0f;
    public float k = 0f;

    private Vector3[] mountPoints;

    private Transform[] coxas;
    private Transform[] femurs;
    private Transform[] tibias;

    public enum ControlMode { InverseKinematics, NeuralCircuit }
    public ControlMode controlMode = ControlMode.InverseKinematics;

    private Sensors sensors;
    private HexapodState neuralState = new HexapodState();
    public float dt = 0.01f; // Simulation timestep for neural circuit

    void Start()
    {
        if (disableGravity)
        {
            // Disable gravity on this object
            var rootBody = GetComponent<ArticulationBody>();
            if (rootBody != null)
                rootBody.useGravity = false;

            // Disable gravity on all children and grandchildren
            foreach (var body in GetComponentsInChildren<ArticulationBody>())
            {
                body.useGravity = false;
            }
        }

        // Initialize mount points with current transform position
        mountPoints = new Vector3[]
        {
            new Vector3(62.77f,  90.45f, transform.position.y),
            new Vector3(86f,     0f,     transform.position.y),
            new Vector3(65.89f, -88.21f, transform.position.y),
            new Vector3(-65.89f, 88.21f, transform.position.y),
            new Vector3(-86f,    0f,     transform.position.y),
            new Vector3(-62.77f, -90.45f, transform.position.y)
        };

        coxas = new Transform[] { Cooxa1, Cooxa2, Cooxa3, Cooxa4, Cooxa5, Cooxa6 };
        femurs = new Transform[6];
        tibias = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            femurs[i] = coxas[i].GetChild(0); //Arreglar esto desde las jerarquias
            tibias[i] = femurs[i].GetChild(0);
        }
        sensors = GetComponent<Sensors>();
    }
    void Update()
    {
        if (controlMode == ControlMode.InverseKinematics)
        {
            var targets = HexapodTrajectory.CalcularTrayectoria(d, al, n, w, rs, ra, c, k);
            for (int i = 0; i < 6; i++)
            {
                var angleModifier = -1f;
                if (i < 3) angleModifier = 1f;
                Vector3 basePos = mountPoints[i];
                Vector3 target = targets[i];
                Vector3 angles = HexapodKinematics.InverseKinematics(basePos, target, L0, L1, L2);
                //cooxa
                var coxaBody = coxas[i].GetComponent<ArticulationBody>();
                var coxaDrive = coxaBody.xDrive;
                coxaDrive.target = angles.x * angleModifier;
                coxaBody.xDrive = coxaDrive;
                //femur
                var femurBody = femurs[i].GetComponent<ArticulationBody>();
                var femurDrive = femurBody.xDrive;
                femurDrive.target = angles.y * angleModifier;
                femurBody.xDrive = femurDrive;
                //tibia 
                var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
                var tibiaDrive = tibiaBody.xDrive;
                tibiaDrive.target = angles.z * angleModifier;
                tibiaBody.xDrive = tibiaDrive;
            }
            k+=60*Mathf.PI/1000*Time.deltaTime;
            if (k>60*Mathf.PI) k = 0;
        }
        else if (controlMode == ControlMode.NeuralCircuit)
        {
            // --- Lidar-based neural circuit stimulation ---
            float go = 0, bk = 0, left = 0, right = 0;
            if (sensors != null)
            {
                var scanData = sensors.GetScanData();
                int numRays = 20;
                float anglePerRay = 360f / numRays;
                // Define area indices (0 = front center, increases clockwise)
                List<int> frontRays = new List<int> { 19, 0, 1, 2 }; // -36° to +36°
                List<int> backRays = new List<int> { 9, 10, 11, 12 }; // 144° to 216°
                List<int> leftRays = new List<int> { 3, 4, 5, 6 };    // 72° to 144°
                List<int> rightRays = new List<int> { 13, 14, 15, 16 }; // -144° to -72°
                // Normalize: each hit adds 0.25 (max 1.0 if all 4 rays hit)
                foreach (int i in frontRays) if (i < scanData.Count && scanData[i].hitDetected) bk += 0.25f;
                foreach (int i in backRays)  if (i < scanData.Count && scanData[i].hitDetected) go += 0.25f;
                foreach (int i in leftRays)  if (i < scanData.Count && scanData[i].hitDetected) right += 0.25f;
                foreach (int i in rightRays) if (i < scanData.Count && scanData[i].hitDetected) left += 0.25f;
            }
            for (int j = 0; j < 50; j++){
                // No spinL/spinR for now
                Stimuli.Update(neuralState, go, bk, 0, 0, left, right, dt);
                CPG.Update(neuralState.CPGs, dt);
                // Use CPG output to update joint angles
                for (int i = 0; i < 6; i++)
                {
                    // You may want to tune T, cpgXY, cpgZ as in HexapodSimulation
                    float cpgXY = (i % 2 == 0 ? neuralState.CPGs[5] : neuralState.CPGs[6]) * neuralState.DIR1 * 5;
                    float T = 130 + 3 * cpgXY * neuralState.DIR1;
                    float cpgZ = (i % 2 == 0 ? neuralState.CPGs[8] : neuralState.CPGs[9]);
                    Locomotion.Update(ref neuralState.Q1[i], ref neuralState.Q2[i], ref neuralState.Q3[i],
                        ref neuralState.E[i], ref neuralState.LP[i], ref neuralState.L2P[i], ref neuralState.L3P[i],
                        T, cpgXY, cpgZ, dt);
                    var angleModifier = 1f;
                    if (i < 3) angleModifier = -1f;
                    // Apply neural circuit joint angles
                    var coxaBody = coxas[i].GetComponent<ArticulationBody>();
                    var coxaDrive = coxaBody.xDrive;
                    coxaDrive.target = neuralState.Q1[i] * angleModifier;
                    coxaBody.xDrive = coxaDrive;
                    var femurBody = femurs[i].GetComponent<ArticulationBody>();
                    var femurDrive = femurBody.xDrive;
                    femurDrive.target = neuralState.Q2[i] * angleModifier;
                    femurBody.xDrive = femurDrive;
                    var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
                    var tibiaDrive = tibiaBody.xDrive;
                    tibiaDrive.target = neuralState.Q3[i] * angleModifier;
                    tibiaBody.xDrive = tibiaDrive;
                }
            }
        }
    }
}
