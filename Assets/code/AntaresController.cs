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
        mountPoints = new Vector3[]
        {
            new Vector3(62.77f,  90.45f, transform.position.y),
            new Vector3(86f,     0f,     transform.position.y),
            new Vector3(65.89f, -88.21f, transform.position.y),
            new Vector3(-65.89f, 88.21f, transform.position.y),
            new Vector3(-86f,    0f,     transform.position.y),
            new Vector3(-62.77f, -90.45f, transform.position.y)
        };

        femurs = new Transform[6];
        tibias = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            femurs[i] = coxas[i].GetChild(0);
            tibias[i] = femurs[i].GetChild(0);
        }
    }

    void Update()
    {
        var targets = HexapodTrajectory.CalcularTrayectoria(d, al, n, w, rs, ra, c, k);

        for (int i = 0; i < 6; i++)
        {
            Vector3 basePos = mountPoints[i];
            Vector3 target = targets[i];
            if (i == 0)
            {
                Debug.Log("Target pata 1: " + target);
            }
            Vector3 angles = HexapodKinematics.InverseKinematics(basePos, target, L0, L1, L2);

            float theta1 = angles.x;
            float theta2 = angles.y;
            float theta3 = angles.z;

            
            if (i == 0|| i == 1|| i == 2)
            {
                theta1 = theta1;
            }
            if (i == 3)
            {
                theta1 = 180-theta1;
            }

            if (i == 4)
            {
                if (theta1>0)
                {
                    theta1= 180-theta1;
                }
                if (theta1<0)
                {
                    theta1= -180-theta1;
                }
            }

            if (i == 5)
            {
                theta1 = -180-theta1;
            }

            // Imprimir en consola
            int pata = i + 1;
            //if (i == 0)
            //{
            Debug.Log("Pata " + pata + " → Coxa: " + theta1.ToString("F2") +
                      ", Fémur: " + theta2.ToString("F2") +
                      ", Tibia: " + theta3.ToString("F2"));
            //}   
            // Aplicar a articulaciones
            var coxaBody = coxas[i].GetComponent<ArticulationBody>();
            var coxaDrive = coxaBody.xDrive;
            coxaDrive.target = theta1;
            coxaBody.xDrive = coxaDrive;

            var femurBody = femurs[i].GetComponent<ArticulationBody>();
            var femurDrive = femurBody.xDrive;
            femurDrive.target = theta2;
            femurBody.xDrive = femurDrive;

            var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
            var tibiaDrive = tibiaBody.xDrive;
            tibiaDrive.target = theta3;
            tibiaBody.xDrive = tibiaDrive;
        }

        k += 60 * Mathf.PI / 1000 * Time.deltaTime;
        if (k > 60 * Mathf.PI) k = 0;

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
