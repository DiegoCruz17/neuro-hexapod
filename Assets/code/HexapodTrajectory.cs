using UnityEngine;
using System.Collections.Generic;

public static class HexapodTrajectory
{
    public static List<Vector3> CalcularTrayectoria(float d, float al, float n, float w, float rs, float ra, float c, float k)
    {
        float hb = -2f;
        Vector3[] P = new Vector3[]
        {
            new Vector3(-2f, -1.7f, hb),
            new Vector3(-2.5f,   0,  hb),
            new Vector3(-2f,  1.7f, hb),
            new Vector3( 2f, -1.7f, hb),
            new Vector3( 2.5f,   0,  hb),
            new Vector3( 2f,  1.7f, hb)
        };
        for (int i = 0; i < P.Length; i++)
        {
            P[i] *= 0.1f;
        }

        float fp(float kx) => d * Mathf.Sin(kx) + n * Mathf.Sin(kx) * Mathf.Pow(Mathf.Cos(kx), 2);

        float rah = ra * Mathf.PI / 4;
        float rahh = -ra * Mathf.PI / 4;

        Quaternion Rz1 = Quaternion.Euler(0, 0, Mathf.Rad2Deg * (rs + rah));
        Quaternion Rzc = Quaternion.Euler(0, 0, Mathf.Rad2Deg * rs);
        Quaternion Rz2 = Quaternion.Euler(0, 0, Mathf.Rad2Deg * (rs + rahh));

        Vector3 RD = new Vector3(
            c * Mathf.Cos(fp(k) / d),
            fp(k),
            al * Mathf.Cos(k)
        );
        Vector3 BD = new Vector3(
            c * Mathf.Cos(fp(k + Mathf.PI) / d),
            fp(k + Mathf.PI),
            al * Mathf.Cos(k + Mathf.PI)
        );
        Vector3 RI = new Vector3(
            -c * Mathf.Cos(fp(w * k) / d),
            fp(w * k),
            al * Mathf.Cos(k)
        );
        Vector3 BI = new Vector3(
            -c * Mathf.Cos(fp(w * k + Mathf.PI) / d),
            fp(w * k + Mathf.PI),
            al * Mathf.Cos(k + Mathf.PI)
        );

        return new List<Vector3>()
        {
            Rz1 * BI + P[5],
            Rzc * RI + P[4],
            Rz2 * BI + P[3],
            Rz2 * RD + P[2],
            Rzc * BD + P[1],
            Rz1 * RD + P[0]
        };
    }
}
