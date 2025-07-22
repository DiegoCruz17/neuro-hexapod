using UnityEngine;
using System;

public static class Stimuli
{
    private static float NakaRushton(float x)
    {
        float g = 3f, sigma = 0.5f, n = 2f;
        x = Mathf.Max(0f, x);
        return (g * Mathf.Pow(x, n)) / (Mathf.Pow(x, n) + Mathf.Pow(sigma, n) + Mathf.Epsilon);
    }

    public static void Update(HexapodState s, float go, float bk, float spinL, float spinR, float left, float right, float dt)
    {
        float tau = 10f;

        float FW_in = 6 * go - bk - spinL - spinR;
        float BW_in = 6 * bk - go - spinL - spinR;
        float TL_in = 6 * spinL - go - bk - spinR;
        float TR_in = 6 * spinR - go - bk - spinL;
        float L_in  = 6 * left - s.R;
        float R_in  = 6 * right - s.L;
        float MOV_in = 5 * (s.FW + s.BW + s.TL + s.TR + s.L + s.R);
        float DIR4_in = s.TL + s.TR;

        float FW_target = NakaRushton(FW_in);
        float BW_target = NakaRushton(BW_in);
        float TL_target = NakaRushton(TL_in);
        float TR_target = NakaRushton(TR_in);
        float L_target  = NakaRushton(L_in);
        float R_target  = NakaRushton(R_in);
        float MOV_target = NakaRushton(MOV_in);
        float DIR4_target = NakaRushton(DIR4_in);

        s.FW += (dt / tau) * (-s.FW + FW_target);
        s.BW += (dt / tau) * (-s.BW + BW_target);
        s.TL += (dt / tau) * (-s.TL + TL_target);
        s.TR += (dt / tau) * (-s.TR + TR_target);
        s.L  += (dt / tau) * (-s.L + L_target);
        s.R  += (dt / tau) * (-s.R + R_target);
        s.MOV += (dt / tau) * (-s.MOV + MOV_target);
        s.DIR4 += (dt / tau) * (-s.DIR4 + DIR4_target);

        float DIR1_in = s.FW + s.TL - s.BW - s.TR;
        float DIR2_in = -s.FW + s.TL + s.BW - s.TR;
        float DIR3_in = s.R - s.L;

        s.DIR1 += (dt / tau) * (-s.DIR1 + (float)Math.Tanh(DIR1_in));
        s.DIR2 += (dt / tau) * (-s.DIR2 + (float)Math.Tanh(DIR2_in));
        s.DIR3 += (dt / tau) * (-s.DIR3 + (float)Math.Tanh(DIR3_in));
    }
}
