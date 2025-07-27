using UnityEngine;
using System;

public static class Locomotion
{
    public static void Update(ref float Q1, ref float Q2, ref float Q3, ref float E,
                               ref float Ei, ref float LP, ref float L2P, ref float L3P,
                               float T, float CPGXY, float CPGZ, float dt)
    {
        float L1 = 86f, L2 = 74.28f, L3 = 140.85f;

        for (int i = 0; i < 150; i++)
        {
            float Q1p = Mathf.Atan2(CPGXY, T) * Mathf.Rad2Deg;
            float Q2p = (E + 0.34f * Ei) * Sigmoider(CPGZ/6);
            float Eip = E + Ei;
            float Q3p = -Q2 - 90f * Sigmoider(CPGZ/6) + (E + 0.2f * Ei) * Sigmoider(-CPGZ/6);

            float LPp = (L1 + L2P + L3P) * Mathf.Cos(Q1 * Mathf.Deg2Rad);
            float Ep = (T - LP) * (i > 25 ? 1f : 0f);
            float L2Pp = L2 * Mathf.Cos(Q2 * Mathf.Deg2Rad);
            float L3Pp = L3 * Mathf.Cos((Q2 + Q3) * Mathf.Deg2Rad);

            Q1 += (dt / 5f) * (-Q1 + Q1p);
            Q2 += (dt / 5f) * (-Q2 + Q2p);
            Q3 += (dt / 5f) * (-Q3 + Q3p);
            E += (dt / 5f) * (-E + Ep);
            Ei += (dt / 5f) * (-Ei + Eip);

            LP += (dt / 5f) * (-LP + LPp);
            L2P += (dt / 5f) * (-L2P + L2Pp);
            L3P += (dt / 5f) * (-L3P + L3Pp);
        }
    }

    private static float Sigmoider(float x)
    {
        return 1f / (1f + Mathf.Exp(-x));
    }
}
