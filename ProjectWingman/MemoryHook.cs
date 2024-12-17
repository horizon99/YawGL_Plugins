using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

public class MemoryHook
{
    // Per Game Settings - Change for Each Game
    private const string _ProcessName = "ProjectWingman-Win64-Shipping"; // Process_Name without the (".exe") for this game

    // Used by the plugin main loop to Process a MemoryHook.
    public float Process_MemoryHook(string _inputName, ulong[] _inputAddrs)
    {
        if (_inputAddrs != null)
        {
            try
            {
                float myValue = ProcessOffsets(_inputAddrs);

                // This is to prevent "wild" values outside of a mission
                switch (_inputName)
                {
                    case "Speed":
                        if (myValue > 1000) myValue = 0;
                        break;
                    case "Yaw":
                    case "Roll":
                        if (myValue > 180 || myValue < -180) myValue = 0;
                        break;
                    case "Pitch":
                        if (myValue > 180 || myValue < -180) myValue = 0;
                        myValue = -myValue;
                        break;
                }
                return myValue;
            }
            catch
            {
                return 0;
            }
        }
        else
        {
            return 0;
        }
    }

    private float ProcessOffsets(ulong[] Offset)
    {
        try
        {
            ulong _BaseAddr = GetProcessBaseAddress(_ProcessName);
            int nboffsets = Offset.Length;

            for (int cnt = 0; cnt < nboffsets - 2; cnt++)
            {
                _BaseAddr = ReadInt64(_ProcessName, _BaseAddr + Offset[cnt]);
            }
            return BitConverter.ToSingle(BitConverter.GetBytes(ReadSingle(_ProcessName, _BaseAddr + Offset[nboffsets - 1])), 0);
        }
        catch
        {
            return 0;
        }
    }

    [DllImport("kernel32.dll")]
    private static extern uint ReadProcessMemory(int hProcess, ulong lpBaseAddress, byte[] lpBuffer, int nSize, ref int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    private static extern int OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(int hObject);

    private const double To_Deg = 180.0 / Math.PI;

    // memory hook function #1
    private float ReadSingle(string hProcess, ulong dwAddress)
    {
        try
        {
            Process proc = Process.GetProcessesByName(hProcess).FirstOrDefault();
            if (proc == null) return 0;
            int winhandle = OpenProcess(0x1F0FFF, true, proc.Id);
            byte[] buffer = new byte[4];
            int bytesRead = 0;
            ReadProcessMemory(winhandle, dwAddress, buffer, buffer.Length, ref bytesRead);
            CloseHandle(winhandle);
            return BitConverter.ToSingle(buffer, 0);
        }
        catch
        {
            return 0;
        }
    }

    // memory hook function #2
    private uint ReadInt32(string hProcess, ulong dwAddress)
    {
        try
        {
            Process proc = Process.GetProcessesByName(hProcess).FirstOrDefault();
            if (proc == null) return 0;
            int winhandle = OpenProcess(0x1F0FFF, true, proc.Id);
            byte[] buffer = new byte[4];
            int bytesRead = 0;
            ReadProcessMemory(winhandle, dwAddress, buffer, buffer.Length, ref bytesRead);
            CloseHandle(winhandle);
            return BitConverter.ToUInt32(buffer, 0);
        }
        catch
        {
            return 0;
        }
    }

    // memory hook function #2 for 64bit games
    private ulong ReadInt64(string hProcess, ulong dwAddress)
    {
        try
        {
            Process proc = Process.GetProcessesByName(hProcess).FirstOrDefault();
            if (proc == null) return 0;
            int winhandle = OpenProcess(0x1F0FFF, true, proc.Id);
            byte[] buffer = new byte[8];
            int bytesRead = 0;
            ReadProcessMemory(winhandle, dwAddress, buffer, buffer.Length, ref bytesRead);
            CloseHandle(winhandle);
            return BitConverter.ToUInt64(buffer, 0);
        }
        catch
        {
            return 0;
        }
    }

    // memory hook function #3
    private double ReadDouble(string hProcess, ulong dwAddress)
    {
        try
        {
            Process proc = Process.GetProcessesByName(hProcess).FirstOrDefault();
            if (proc == null) return 0;
            int winhandle = OpenProcess(0x1F0FFF, true, proc.Id);
            byte[] buffer = new byte[8];
            int bytesRead = 0;
            ReadProcessMemory(winhandle, dwAddress, buffer, buffer.Length, ref bytesRead);
            CloseHandle(winhandle);
            return BitConverter.ToDouble(buffer, 0);
        }
        catch
        {
            return 0;
        }
    }

    private ulong GetProcessBaseAddress(string hProcess)
    {
        try
        {
            Process proc = Process.GetProcessesByName(hProcess).FirstOrDefault();
            if (proc == null) return 0;
            return (ulong)proc.MainModule.BaseAddress.ToInt64();
        }
        catch
        {
            return 0;
        }
    }
}
