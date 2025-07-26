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

    public float L0 = 86.0f;
    public float L1 = 74.28f;
    public float L2 = 140.85f;

    public float d = 40f, al = 60f, n = 20f, w = 1f, rs = 0f, ra = 0f, c = 0f;
    public float k = 0f;

    public float hb = -20f;
    public float wb = 80f;

    private Vector3[] mountPoints;

    private Transform[] coxas;
    private Transform[] femurs;
    private Transform[] tibias;

    public enum ControlMode { InverseKinematics, NeuralCircuit }
    public ControlMode controlMode = ControlMode.InverseKinematics;

    private Sensors sensors;
    private HexapodState neuralState = new HexapodState();
    public float dt = 0.01f; // Simulation timestep for neural circuit
    public float[] RangoOPQ1_offset = new float[] { -30, 0, 30, -30, 0, 30 };

    void Start()
    {
        if (disableGravity)
        {
            var rootBody = GetComponent<ArticulationBody>();
            if (rootBody != null)
                rootBody.useGravity = false;

            foreach (var body in GetComponentsInChildren<ArticulationBody>())
            {
                body.useGravity = false;
            }
        }

        // Reordenar coxas para hacer coincidir el orden MATLAB → Unity
        coxas = new Transform[] { Cooxa4, Cooxa5, Cooxa6, Cooxa1, Cooxa2, Cooxa3 };

        // Coordenadas de montaje en el cuerpo
        /*
        mountPoints = new Vector3[]
        {
            new Vector3(62.77f,  90.45f, transform.position.y),
            new Vector3(86f,     0f,     transform.position.y),
            new Vector3(65.89f, -88.21f, transform.position.y),
            new Vector3(-65.89f, 88.21f, transform.position.y),
            new Vector3(-86f,    0f,     transform.position.y),
            new Vector3(-62.77f, -90.45f, transform.position.y)
        };*/

        mountPoints = new Vector3[]
        {
            new Vector3(62.77f,  90.45f, 123.83f),
            new Vector3(86f,     0f,     123.83f),
            new Vector3(65.89f, -88.21f, 123.83f),
            new Vector3(-65.89f, 88.21f, 123.83f),
            new Vector3(-86f,    0f,     123.83f),
            new Vector3(-62.77f, -90.45f, 123.83f)
        };

        femurs = new Transform[6];
        tibias = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            femurs[i] = coxas[i].GetChild(0);
            tibias[i] = femurs[i].GetChild(0);
        }
        sensors = GetComponent<Sensors>();
    }

    void Update()
    {
        if (controlMode == ControlMode.InverseKinematics)
        {
            var targets = HexapodTrajectory.CalcularTrayectoria(d, al, n, w, rs, ra, c, k, hb, wb);
            for (int i = 0; i < 6; i++)
            {
                var angleModifier = 1f;
                if (i < 3) angleModifier = -1f;
                Vector3 basePos = mountPoints[i];
                Vector3 target = targets[i];
                Debug.Log("Target[" + i + "]: " + target);
                Vector3 angles = HexapodKinematics.InverseKinematics(basePos, target, L0, L1, L2);

                Debug.Log($"Leg {i} Angles -> Coxa: {angles.x:F2}, Femur: {angles.y:F2}, Tibia: {angles.z:F2}");

                //cooxa
                var coxaBody = coxas[i].GetComponent<ArticulationBody>();
                var coxaDrive = coxaBody.xDrive;
                if (i >= 3)
                {
                    angles.x = 180-(((angles.x)+720)%360);
                }
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
            //k = 0;
            k +=60*Mathf.PI/100*Time.deltaTime;
            if (k>60*Mathf.PI) k = 0;
        }
        else if (controlMode == ControlMode.NeuralCircuit)
        {
            // --- Lidar-based neural circuit stimulation ---
            float go = 6, bk = 0,  left = 0, right = 0;
                    /*
                if (sensors != null)
                {
                    var scanData = sensors.GetScanData();
                    int numRays = 20;

                    // Sector izquierdo: del rayo 0 al 9 (0° a 162° aprox)
                    List<int> leftSector = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

                    // Sector derecho: del rayo 10 al 19 (180° a 342° aprox)
                    List<int> rightSector = new List<int> { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };

                    // Cada detección suma 0.1 (10 rayos por sector → máx = 1.0)
                    foreach (int i in leftSector)
                        if (i < scanData.Count && scanData[i].hitDetected)
                            right += 0.1f; // obstáculo a la izquierda → moverse a la derecha

                    foreach (int i in rightSector)
                        if (i < scanData.Count && scanData[i].hitDetected)
                            left += 0.1f; // obstáculo a la derecha → moverse a la izquierda
                }*/
                
            for (int j = 0; j < 50; j++)
            {
                // No spinL/spinR for now
                Stimuli.Update(neuralState, go, bk, 0, 0, left, right, dt);
                CPG.Update(neuralState.CPGs, dt);
                // Use CPG output to update joint angles

                // You may want to tune T, cpgXY, cpgZ as in HexapodSimulation
                    // Piernas impares: 1, 3, 5 => índices 0, 2, 4
                    Locomotion.Update(
                        ref neuralState.Q1[0], ref neuralState.Q2[0], ref neuralState.Q3[0],
                        ref neuralState.E[0], ref neuralState.LP[0], ref neuralState.L2P[0], ref neuralState.L3P[0],
                        90 + 3 * neuralState.CPGs[5] * neuralState.DIR1 * (neuralState.DIR3 - neuralState.DIR4),
                        neuralState.DIR1 * 5 * neuralState.CPGs[5] + RangoOPQ1_offset[0],
                        neuralState.CPGs[8], 1);

                    Locomotion.Update(
                        ref neuralState.Q1[2], ref neuralState.Q2[2], ref neuralState.Q3[2],
                        ref neuralState.E[2], ref neuralState.LP[2], ref neuralState.L2P[2], ref neuralState.L3P[2],
                        90 + 3 * neuralState.CPGs[5] * neuralState.DIR1 * (neuralState.DIR3 + neuralState.DIR4),
                        neuralState.DIR1 * 5 * neuralState.CPGs[5] + RangoOPQ1_offset[2],
                        neuralState.CPGs[8], 1);

                    Locomotion.Update(
                        ref neuralState.Q1[4], ref neuralState.Q2[4], ref neuralState.Q3[4],
                        ref neuralState.E[4], ref neuralState.LP[4], ref neuralState.L2P[4], ref neuralState.L3P[4],
                        90 + 3 * neuralState.CPGs[5] * neuralState.DIR1 * neuralState.DIR3,
                        neuralState.DIR2 * 5 * neuralState.CPGs[5] + RangoOPQ1_offset[4],
                        neuralState.CPGs[8], 1);

                    // Piernas pares: 2, 4, 6 => índices 1, 3, 5
                    Locomotion.Update(
                        ref neuralState.Q1[1], ref neuralState.Q2[1], ref neuralState.Q3[1],
                        ref neuralState.E[1], ref neuralState.LP[1], ref neuralState.L2P[1], ref neuralState.L3P[1],
                        90 + 3 * neuralState.CPGs[6] * neuralState.DIR2 * neuralState.DIR3,
                        neuralState.DIR1 * 5 * neuralState.CPGs[6] + RangoOPQ1_offset[1],
                        neuralState.CPGs[9], 1);

                    Locomotion.Update(
                        ref neuralState.Q1[3], ref neuralState.Q2[3], ref neuralState.Q3[3],
                        ref neuralState.E[3], ref neuralState.LP[3], ref neuralState.L2P[3], ref neuralState.L3P[3],
                        90 + 3 * neuralState.CPGs[6] * neuralState.DIR2 * (neuralState.DIR3 + neuralState.DIR4),
                        neuralState.DIR2 * 5 * neuralState.CPGs[6] + RangoOPQ1_offset[3],
                        neuralState.CPGs[9], 1);

                    Locomotion.Update(
                        ref neuralState.Q1[5], ref neuralState.Q2[5], ref neuralState.Q3[5],
                        ref neuralState.E[5], ref neuralState.LP[5], ref neuralState.L2P[5], ref neuralState.L3P[5],
                        90 + 3 * neuralState.CPGs[6] * neuralState.DIR2 * (neuralState.DIR3 - neuralState.DIR4),
                        neuralState.DIR2 * 5 * neuralState.CPGs[6] + RangoOPQ1_offset[5],
                        neuralState.CPGs[9], 1);



                for (int i = 0; i < 6; i++)
                {
                    var angleModifier = -1f;
                    var angleModifierCOX = -1f;
                    if (i < 3) angleModifier = 1f;
                    // Apply neural circuit joint angles
                    var coxaBody = coxas[i].GetComponent<ArticulationBody>();
                    var coxaDrive = coxaBody.xDrive;
                    coxaDrive.target = neuralState.Q1[i] * angleModifier*angleModifierCOX;
                    coxaBody.xDrive = coxaDrive;
                    var femurBody = femurs[i].GetComponent<ArticulationBody>();
                    var femurDrive = femurBody.xDrive;
                    femurDrive.target = neuralState.Q2[i] * angleModifier;
                    femurBody.xDrive = femurDrive;
                    var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
                    var tibiaDrive = tibiaBody.xDrive;
                    tibiaDrive.target = (-neuralState.Q2[i]-90)*angleModifier ;
                    tibiaBody.xDrive = tibiaDrive;
                }
            }
        }
    }
    private ArticulationDrive ConfigureDrive(float target, float stiffness = 1000f, float damping = 500f, float forceLimit = 100f)
    {
        ArticulationDrive drive = new ArticulationDrive();
        drive.stiffness = stiffness;
        drive.damping = damping;
        drive.forceLimit = forceLimit;
        drive.target = target;
        drive.targetVelocity = 0f; // No se usa cuando stiffness > 0
        return drive;
    }

}
