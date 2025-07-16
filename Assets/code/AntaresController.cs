using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//hola

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
    }
    void Update()
    {

        var targets = HexapodTrajectory.CalcularTrayectoria(d, al, n, w, rs, ra, c, k);

        for (int i = 0; i < 6; i++)
        {
            var angleModifier = -1f;
            if (i < 3)
            {
                angleModifier = 1f;
            }
            Vector3 basePos = mountPoints[i];
            Vector3 target = targets[i];
            Vector3 angles = HexapodKinematics.InverseKinematics(basePos, target, L0, L1, L2);
            Debug.Log("Angles: " + angles);

            //cooxa
            var coxaBody = coxas[i].GetComponent<ArticulationBody>();
            var coxaDrive = coxaBody.xDrive;
            coxaDrive.target = angles.x * angleModifier;
            coxaBody.xDrive = coxaDrive;
            // Debug.Log("Coxa " + i + " target: " + angles.x * angleModifier);

            //femur
            var femurBody = femurs[i].GetComponent<ArticulationBody>();
            var femurDrive = femurBody.xDrive;
            femurDrive.target = angles.y * angleModifier;
            femurBody.xDrive = femurDrive;
            // Debug.Log("Femur " + i + " target: " + angles.y * angleModifier);
            //tibia 
            var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
            var tibiaDrive = tibiaBody.xDrive;
            tibiaDrive.target = angles.z * angleModifier;
            tibiaBody.xDrive = tibiaDrive;
            // Debug.Log("Tibia " + i + " target: " + angles.z * angleModifier);
        }
        k+=60*Mathf.PI/1000*Time.deltaTime;
        if (k>60*Mathf.PI) k = 0;
    }
}
