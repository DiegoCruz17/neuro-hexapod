using UnityEngine;
using System;

public static class Locomotion
{
    public static void Update(ref float Q1, ref float Q2, ref float Q3, ref float E,
                              ref float LP, ref float L2P, ref float L3P,
                              float T, float CPGXY, float CPGZ, float dt)
    {
        float L1 = 86, L2 = 74.28f, L3 = 140.85f;

        for (int i = 0; i < 50; i++)
        {
            float Q1p = CPGXY;
            float Q2p = (Q2 * 0.35f + 5.5f * E) * (float)Math.Tanh(Math.Pow(CPGZ, 3) / 9.0f) + CPGZ;
            float Q3p = -Q2p - 90;

            float LPp = (L1 + L2P) * Mathf.Cos(Q1 * Mathf.Deg2Rad);
            float Ep = (T - LP) * (i > 25 ? 1f : 0f);
            float L2Pp = L2 * Mathf.Cos(Q2 * Mathf.Deg2Rad);
            float L3Pp = L3 * Mathf.Cos((Q2 + Q3) * Mathf.Deg2Rad);

            Q1 += (dt / 5f) * (-Q1 + Q1p);
            Q2 += (dt / 30f) * (-Q2 + (float)Math.Tanh(Q2p / 60f) * 180f);
            Q3 += (dt / 30f) * (-Q3 + Q3p);
            E  += (dt / 5f) * (-E + Ep);

            LP  += (dt / 5f) * (-LP + LPp);
            L2P += (dt / 5f) * (-L2P + L2Pp);
            L3P += (dt / 5f) * (-L3P + L3Pp);
        }
    }
}
