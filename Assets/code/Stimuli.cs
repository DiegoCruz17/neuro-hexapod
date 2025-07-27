using UnityEngine;
using System;

public static class Stimuli
{
    // Función Naka-Rushton actualizada
    private static float NakaRushton(float x)
    {
        float g = 1f, sigma = 0.5f, n = 2f;
        x = Mathf.Max(0f, x);
        return (g * Mathf.Pow(x, n)) / (Mathf.Pow(x, n) + Mathf.Pow(sigma, n) + Mathf.Epsilon);
    }

    public static void Update(HexapodState s, float go, float bk, float spinL, float spinR, float left, float right, float dt)
    {
        float tau = 10f;

        // === Neuronas intermedias (salida en [0, 1]) ===
        float FW_in = 1f * go - 1f * s.BW - 1f * s.TL - 1f * s.TR;
        float BW_in = 1f * bk - 1f * s.FW - 1f * s.TL - 1f * s.TR;
        float TL_in = 1f * spinL - 1f * s.BW - 1f * s.FW - 1f * s.TR;
        float TR_in = 1f * spinR - 1f * s.BW - 1f * s.FW - 1f * s.TL;
        float L_in  = 1f * left - 1f * s.R;
        float R_in  = 1f * right - 1f * s.L;
        float MOV_in = 5f * (s.FW + s.BW + s.TL + s.TR + s.L + s.R);
        float DIR4_in = 1f * s.TL + 1f * s.TR;

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

        // === Neuronas de dirección (salida en [-1, 1]) ===
        float DIR1_in = s.FW + s.TL - s.BW - s.TR;
        float DIR2_in = s.FW - s.TL - s.BW + s.TR; // Ajuste de signos
        float DIR3_in = s.R - s.L;

        s.DIR1 += (dt / tau) * (-s.DIR1 + (float)Math.Tanh(DIR1_in));
        s.DIR2 += (dt / tau) * (-s.DIR2 + (float)Math.Tanh(DIR2_in));
        s.DIR3 += (dt / tau) * (-s.DIR3 + (float)Math.Tanh(DIR3_in));
    }
}
