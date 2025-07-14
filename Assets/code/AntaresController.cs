using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntaresController : MonoBehaviour
{
    public Transform Cooxa1;
    public Transform Cooxa2;
    public Transform Cooxa3;
    public Transform Cooxa4;
    public Transform Cooxa5;
    public Transform Cooxa6;

    public float L0 = 86f;
    public float L1 = 74.28f;
    public float L2 = 140.85f;

    public float d = 40f, al = 60f, n = 20f, w = 1f, rs = 0f, ra = 0f, c = 0f, k = 0f;

    private Vector3[] mountPoints = new Vector3[]
    {
        new Vector3(62.77f,  90.45f, 123.83f),
        new Vector3(86f,     0f,     123.83f),
        new Vector3(65.89f, -88.21f, 123.83f),
        new Vector3(-65.89f, 88.21f, 123.83f),
        new Vector3(-86f,    0f,     123.83f),
        new Vector3(-62.77f, -90.45f, 123.83f)
    };

    private Transform[] coxas;
    private Transform[] femurs;
    private Transform[] tibias;

    void Start()
    {
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
            Vector3 basePos = mountPoints[i];
            Vector3 target = targets[i];
            Vector3 angles = HexapodKinematics.InverseKinematics(basePos, target, L0, L1, L2);

            // Aplicar ángulos: θ1 a coxa (Y), θ2 a femur (Z), θ3 a tibia (Z)
            // Ajustar ejes según sea necesario para coincidir con la orientación de tu modelo
            coxas[i].localRotation = Quaternion.Euler(0, angles.x, 0);
            femurs[i].localRotation = Quaternion.Euler(0, 0, angles.y);
            tibias[i].localRotation = Quaternion.Euler(0, 0, angles.z);
        }
    }
}
