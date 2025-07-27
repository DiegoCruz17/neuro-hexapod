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

    // VARIABLES DE LA RED//
    public float go = 0f;
    public float bk = 0f;
    public float left = 0f;
    public float right = 0f;
    private Vector3[] mountPoints;

    private Transform[] coxas;
    private Transform[] femurs;
    private Transform[] tibias;

    public enum ControlMode { InverseKinematics, NeuralCircuit }
    public ControlMode controlMode = ControlMode.InverseKinematics;

    private Sensors sensors;
    private HexapodState neuralState = new HexapodState();
    public float dt = 0.01f; // Simulation timestep for neural circuit
    public float[] RangoOPQ1_offset = new float[] { 40, 0, -40, -40, 0, 40 };

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
                Vector3 angles = HexapodKinematics.InverseKinematics(basePos, target, L0, L1, L2);

                var coxaBody = coxas[i].GetComponent<ArticulationBody>();
                var coxaDrive = coxaBody.xDrive;
                if (i >= 3)
                {
                    angles.x = 180 - (((angles.x) + 720) % 360);
                }
                coxaDrive.target = angles.x * angleModifier;
                coxaBody.xDrive = coxaDrive;

                var femurBody = femurs[i].GetComponent<ArticulationBody>();
                var femurDrive = femurBody.xDrive;
                femurDrive.target = angles.y * angleModifier;
                femurBody.xDrive = femurDrive;

                var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
                var tibiaDrive = tibiaBody.xDrive;
                tibiaDrive.target = angles.z * angleModifier;
                tibiaBody.xDrive = tibiaDrive;
            }
            k += 60 * Mathf.PI / 100 * Time.deltaTime;
            if (k > 60 * Mathf.PI) k = 0;
        }
        else if (controlMode == ControlMode.NeuralCircuit)
        {
            float go = 0, bk = 0, left = 0, right = 0, D = 4, T = 90;

            for (int j = 0; j < 50; j++)
            {
                Stimuli.Update(neuralState, go, bk, 10, 0, left, right, dt);
                CPG.Update(neuralState.CPGs, dt);

                // 6 patas (0-5)
                Locomotion.Update(ref neuralState.Q1[0], ref neuralState.Q2[0], ref neuralState.Q3[0], ref neuralState.E[0], ref neuralState.Ei[0], ref neuralState.LP[0], ref neuralState.L2P[0], ref neuralState.L3P[0],
                    T + neuralState.CPGs[5] * D * (neuralState.DIR3 - 0.1f * neuralState.DIR4), neuralState.CPGs[5] * D * neuralState.DIR1 + RangoOPQ1_offset[0], 3f * neuralState.CPGs[8]* neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[4], ref neuralState.Q2[4], ref neuralState.Q3[4], ref neuralState.E[4], ref neuralState.Ei[4], ref neuralState.LP[4], ref neuralState.L2P[4], ref neuralState.L3P[4],
                    T - neuralState.CPGs[5] * D * neuralState.DIR3, -neuralState.CPGs[5] * D * neuralState.DIR2 + RangoOPQ1_offset[4], 3f * neuralState.CPGs[8]* neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[2], ref neuralState.Q2[2], ref neuralState.Q3[2], ref neuralState.E[2], ref neuralState.Ei[2], ref neuralState.LP[2], ref neuralState.L2P[2], ref neuralState.L3P[2],
                    T + neuralState.CPGs[5] * D * (neuralState.DIR3 + 0.1f * neuralState.DIR4), neuralState.CPGs[5] * D * neuralState.DIR1 + RangoOPQ1_offset[2], 3f * neuralState.CPGs[8]* neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[3], ref neuralState.Q2[3], ref neuralState.Q3[3], ref neuralState.E[3], ref neuralState.Ei[3], ref neuralState.LP[3], ref neuralState.L2P[3], ref neuralState.L3P[3],
                    T - neuralState.CPGs[6] * D * (neuralState.DIR3 - 0.1f * neuralState.DIR4), -neuralState.CPGs[6] * D * neuralState.DIR2 + RangoOPQ1_offset[3], 3f * neuralState.CPGs[9]* neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[1], ref neuralState.Q2[1], ref neuralState.Q3[1], ref neuralState.E[1], ref neuralState.Ei[1], ref neuralState.LP[1], ref neuralState.L2P[1], ref neuralState.L3P[1],
                    T + neuralState.CPGs[6] * D * neuralState.DIR3, neuralState.CPGs[6] * D * neuralState.DIR1 + RangoOPQ1_offset[1], 3f * neuralState.CPGs[9]* neuralState.MOV, dt);
                
                Locomotion.Update(ref neuralState.Q1[5], ref neuralState.Q2[5], ref neuralState.Q3[5], ref neuralState.E[5], ref neuralState.Ei[5], ref neuralState.LP[5], ref neuralState.L2P[5], ref neuralState.L3P[5],
                    T - neuralState.CPGs[6] * D * (neuralState.DIR3 + 0.1f * neuralState.DIR4), -neuralState.CPGs[6] * D * neuralState.DIR2 + RangoOPQ1_offset[5], 3f * neuralState.CPGs[9]* neuralState.MOV, dt);

                // Aplicar ángulos
                for (int i = 0; i < 6; i++)
                {
                    var angleModifier = -1f;
                    var angleModifierCOX = 1f;
                    if (i < 3) { angleModifier = 1f; angleModifierCOX = -1f; }
                    var coxaBody = coxas[i].GetComponent<ArticulationBody>();
                    var coxaDrive = coxaBody.xDrive;
                    coxaDrive.target = neuralState.Q1[i] * angleModifier * angleModifierCOX;
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
