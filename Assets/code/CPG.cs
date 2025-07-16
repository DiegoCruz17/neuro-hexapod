using UnityEngine;
using System;


public static class CPG
{
    public static void Update(float[] CPGs, float dt)
    {
        float Ao = 100, Bo = 120, Co = 1.5f, Do = 2.7f;
        float a = 1, b = 1;
        float tau1o = 8, tau2o = 16, tau3o = 150, tau4o = 150;

        float u = 2, u2 = 2;

        CPGs[0] += (dt / tau1o) * (-a * CPGs[0] + 
            (Ao * Mathf.Pow(150 - Do * CPGs[1], 2)) / 
            (Mathf.Pow(Bo + b * CPGs[2], 2) + Mathf.Pow(150 - Do * CPGs[1], 2)));

        CPGs[1] += (dt / tau2o) * (-a * CPGs[1] + 
            (Ao * Mathf.Pow(150 - Do * CPGs[0], 2)) / 
            (Mathf.Pow(Bo + b * CPGs[3], 2) + Mathf.Pow(150 - Do * CPGs[0], 2)));

        CPGs[2] += (dt / tau3o) * (-a * CPGs[2] + Co * CPGs[0]);
        CPGs[3] += (dt / tau4o) * (-a * CPGs[3] + Co * CPGs[1]);

        CPGs[4] += (dt / tau4o) * (-a * CPGs[4] + 1.01f * (0.5f * CPGs[0] + 0.5f * CPGs[1]));
        CPGs[5] = Mathf.Clamp(CPGs[5] + (dt / tau4o) * (-a * CPGs[5] + a * CPGs[0] - CPGs[4]), -u, u);
        CPGs[6] = Mathf.Clamp(CPGs[6] + (dt / tau4o) * (-a * CPGs[6] + a * CPGs[1] - CPGs[4]), -u, u);

        CPGs[7] += (dt / tau4o) * (-a * CPGs[7] + 1.01f * (0.5f * CPGs[2] + 0.5f * CPGs[3]));
        CPGs[8] = Mathf.Clamp(CPGs[8] + (dt / (tau4o * 0.5f)) * (-a * CPGs[8] + 1.2f * (CPGs[2] - CPGs[7])), -u2, u2);
        CPGs[9] = Mathf.Clamp(CPGs[9] + (dt / (tau4o * 0.5f)) * (-a * CPGs[9] + 1.2f * (CPGs[3] - CPGs[7])), -u2, u2);
    }
}
