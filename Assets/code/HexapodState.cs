public class HexapodState
{
    public float[] CPGs = new float[10];
    public float[] Q1 = new float[6];
    public float[] Q2 = new float[6];
    public float[] Q3 = new float[6];
    public float[] E = new float[6];
    public float[] LP = new float[6];
    public float[] L2P = new float[6];
    public float[] L3P = new float[6];

    public float DIR1, DIR2, DIR3, DIR4;
    public float FW, BW, TL, TR, L, R, MOV;
     public float[] RangoOPQ1_offset = new float[] { 10, 0, -10, 10, 0, -10 };
}
