using UnityEngine;

public static class HexapodKinematics
{
    public static Vector3 InverseKinematics(Vector3 basePos, Vector3 target, float L0, float L1, float L2)
    {
        // L0,L1,L2 son las longitudes de los segmentos de la cadena cinemática
        // basePos es la posición de la base del hexápodo
        // target es la posición del objetivo
        float dx = target.x - basePos.x;
        float dy = target.y - basePos.y;
        float dz = target.z - basePos.z;

        float theta1 = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

        Matrix4x4 Rz = RotationZ(-theta1);

        Vector3 globalVector = new Vector3(dx, dy, dz);
        Vector3 local = Rz.MultiplyVector(globalVector);

        float x_local = local.x - L0;
        float z_local = local.z;

        float r = Mathf.Sqrt(x_local * x_local + z_local * z_local);
        float D = ((r * r) - (L1 * L1) - (L2 * L2)) / (2 * L1 * L2);
        D = Mathf.Clamp(D, -1f, 1f);

        Debug.Log($"Rz Matrix:\n" +
          $"| {Rz.m00:F2} {Rz.m01:F2} {Rz.m02:F2} {Rz.m03:F2} |\n" +
          $"| {Rz.m10:F2} {Rz.m11:F2} {Rz.m12:F2} {Rz.m13:F2} |\n" +
          $"| {Rz.m20:F2} {Rz.m21:F2} {Rz.m22:F2} {Rz.m23:F2} |\n" +
          $"| {Rz.m30:F2} {Rz.m31:F2} {Rz.m32:F2} {Rz.m33:F2} |");


        //Debug.Log($"Kinematic invs Angles -> local_1: {local.x:F2}, z_local: {z_local:F2}, L1: {L1:F2}");

        float theta3 = Mathf.Acos(D) * Mathf.Rad2Deg;

        float alpha = Mathf.Atan2(z_local, x_local) * Mathf.Rad2Deg;
        float beta = Mathf.Atan2(L2 * Mathf.Sin(-theta3 * Mathf.Deg2Rad), L1 + L2 * Mathf.Cos(-theta3 * Mathf.Deg2Rad)) * Mathf.Rad2Deg;

        float theta2 = -(alpha - beta);

        return new Vector3(theta1, theta2, theta3);
    }

    private static Matrix4x4 RotationZ(float theta)
    {
        float rad = theta * Mathf.Deg2Rad;
        return new Matrix4x4(
            new Vector4(Mathf.Cos(rad), Mathf.Sin(rad), 0, 0),
            new Vector4(-Mathf.Sin(rad),  Mathf.Cos(rad), 0, 0),
            new Vector4(0,              0,              1, 0),
            new Vector4(0,              0,              0, 1)
        );
    }
}
