using UnityEngine;
using System.Collections.Generic;

public class HexapodTest : MonoBehaviour
{
    void Start()
    {
        float d = 40f, al = 60f, n = 20f, w = 1f, rs = 0f, ra = 0f, c = 0f, k = 0f;
        float L0 = 86f, L1 = 74.28f, L2 = 140.85f;

        List<Vector3> patas = HexapodTrajectory.CalcularTrayectoria(d, al, n, w, rs, ra, c, k);

        for (int i = 0; i < patas.Count; i++)
        {
            Vector3 basePos = GetMountPoint(i);
            Vector3 target = patas[i];
            Vector3 angles = HexapodKinematics.InverseKinematics(basePos, target, L0, L1, L2);

            Debug.Log($"Pata {i + 1}:\n - Objetivo: {target}\n - Ángulos: θ1={angles.x:F2}, θ2={angles.y:F2}, θ3={angles.z:F2}");

        }
    }

    Vector3 GetMountPoint(int i)
    {
        float baseHeight = 123.83f;
        Vector3[] mount_points = new Vector3[]
        {
            new Vector3(62.77f,  90.45f, baseHeight),
            new Vector3(86f,     0f,     baseHeight),
            new Vector3(65.89f, -88.21f, baseHeight),
            new Vector3(-65.89f, 88.21f, baseHeight),
            new Vector3(-86f,    0f,     baseHeight),
            new Vector3(-62.77f, -90.45f, baseHeight)
        };
        return mount_points[i];
    }
}
