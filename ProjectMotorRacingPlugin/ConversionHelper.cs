using System;
using System.Numerics;
using System.Text;

namespace YawVR_Game_Engine.Plugin;

public sealed class ConversionHelper
{
    public static void ConvertVec(ref byte[] data, ref int startIdx, out UDPVec3 vec)
    {
        vec = new();

        vec.x = BitConverter.ToSingle(data, startIdx);
        startIdx += sizeof(float);

        vec.y = BitConverter.ToSingle(data, startIdx);
        startIdx += sizeof(float);

        vec.z = BitConverter.ToSingle(data, startIdx);
        startIdx += sizeof(float);
    }

    public static void ConvertQuat(ref byte[] data, ref int startIdx, out UDPQuat quat)
    {
        quat = new UDPQuat();

        quat.x = BitConverter.ToSingle(data, startIdx);
        startIdx += sizeof(float);

        quat.y = BitConverter.ToSingle(data, startIdx);
        startIdx += sizeof(float);

        quat.z = BitConverter.ToSingle(data, startIdx);
        startIdx += sizeof(float);

        quat.w = BitConverter.ToSingle(data, startIdx);
        startIdx += sizeof(float);
    }

    public static void ConvertFloat(ref byte[] data, ref int startIdx, out float f)
    {
        f = BitConverter.ToSingle(data, startIdx);
        startIdx += sizeof(float);
    }

    public static void ConvertBool(ref byte[] data, ref int startIdx, out bool b)
    {
        byte val = data[startIdx];
        startIdx += sizeof(byte);

        b = val != 0;
    }
    public static void ConvertByte(ref byte[] data, ref int startIdx, out byte b)
    {
        b = data[startIdx];
        startIdx += sizeof(byte);
    }

    public static void ConvertInt32(ref byte[] data, ref int startIdx, out int i)
    {
        i = BitConverter.ToInt32(data, startIdx);
        startIdx += sizeof(int);
    }

    public static void ConvertUInt16(ref byte[] data, ref int startIdx, out ushort i)
    {
        i = BitConverter.ToUInt16(data, startIdx);
        startIdx += sizeof(ushort);
    }

    public static void ConvertUInt32(ref byte[] data, ref int startIdx, out uint i)
    {
        i = BitConverter.ToUInt32(data, startIdx);
        startIdx += sizeof(uint);
    }

    public static void ConvertString(ref byte[] data, ref int startIdx, out string str)
    {
        byte len = 0;
        ConvertByte(ref data, ref startIdx, out len);

        byte[] characters = new byte[len];
        for (int i = 0; i < len; i++)
        {
            ConvertByte(ref data, ref startIdx, out characters[i]);
        }

        str = Encoding.UTF8.GetString(characters);
    }
    public static Vector3 QuatToEulerDeg(float x, float y, float z, float w)
    {
        try
        {
            // roll (rotation autour de X)
            double sinr_cosp = 2.0 * (w * x + y * z);
            double cosr_cosp = 1.0 - 2.0 * (x * x + y * y);
            double roll = Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (rotation autour de Y)
            double sinp = 2.0 * (w * y - z * x);
            double pitch;
            if (Math.Abs(sinp) >= 1.0)
            {
                // angle proche de ±90° → clamp
                pitch = Math.CopySign(Math.PI / 2.0, sinp);
            }
            else
            {
                pitch = Math.Asin(sinp);
            }

            // yaw (rotation autour de Z)
            double siny_cosp = 2.0 * (w * z + x * y);
            double cosy_cosp = 1.0 - 2.0 * (y * y + z * z);
            double yaw = Math.Atan2(siny_cosp, cosy_cosp);

            const double Rad2Deg = 180.0 / Math.PI;
            return new Vector3(
                (float)(pitch * Rad2Deg),  // ou roll ici selon ta convention
                (float)(yaw * Rad2Deg),
                (float)(roll * Rad2Deg)
            );
        }
        catch (Exception e)
        { 
        }
        return new Vector3(0, 0, 0);
    }
}
