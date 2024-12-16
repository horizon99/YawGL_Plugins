Option Strict On
Option Explicit On
Imports System.Text
Imports System.Reflection
Imports Newtonsoft.Json.Linq
Imports ProjectWingmanPlugin.YawVR_Game_Engine.Plugin
Imports YawGLAPI

Public Class MemoryHook
    '/// Per Game Settings - Change for Each Game ///
    Private Const _ProcessName As String = "ProjectWingman-Win64-Shipping" 'Process_Name without the (".exe") for this game

    'Used by the plugin main loop to Process a MemoryHook.
    Public Function Process_MemoryHook(_inputName As String, _inputAddrs As UInt64()) As Single

        If IsNothing(_inputAddrs) = False Then
            Try
                Dim myValue As Single
                myValue = ProcessOffsets(_inputAddrs)

                ' This is to prevent "wild" values outside of a mission
                Select Case _inputName
                    Case "Speed"
                        If myValue > 1000 Then myValue = 0
                    Case "Yaw", "Roll"
                        If myValue > 180 Or myValue < -180 Then myValue = 0
                    Case "Pitch"
                        If myValue > 180 Or myValue < -180 Then myValue = 0
                        myValue = -myValue
                End Select
                Return myValue
            Catch
                Return 0
            End Try
        Else
            Return 0
        End If
    End Function

    Private Function ProcessOffsets(Offset() As UInt64) As Single
        Try
            Dim _BaseAddr As UInt64 = GetProcessBaseAddress(_ProcessName)
            Dim nboffsets As Integer
            nboffsets = Offset.Count

            For cnt As Integer = 0 To nboffsets - 2
                _BaseAddr = ReadInt64(_ProcessName, _BaseAddr + Offset(cnt))
            Next
            Return CSng(ReadSingle(_ProcessName, _BaseAddr + Offset(nboffsets - 1)))

        Catch ex As Exception
            Return 0
        End Try

    End Function

    Private Declare Function ReadProcessMemory Lib "kernel32" Alias "ReadProcessMemory" (ByVal hProcess As Integer, ByVal lpBaseAddress As UInt64, ByVal lpBuffer() As Byte, ByVal nSize As Integer, ByRef lpNumberOfBytesWritten As Integer) As UInteger
    Private Declare Function OpenProcess Lib "kernel32" Alias "OpenProcess" (ByVal dwDesiredAccess As Integer, ByVal bInheritHandle As Integer, ByVal dwProcessId As Integer) As Integer
    Private Declare Function CloseHandle Lib "kernel32" Alias "CloseHandle" (ByVal hObject As Integer) As Integer
    Private Const To_Deg As Double = 180.0 / Math.PI

    'memory hook function #1
    Private Function ReadSingle(hProcess As String, dwAddress As UInt64) As Single
        Try
            Dim proc As Process = Process.GetProcessesByName(hProcess)(0)
            Dim winhandle As Integer = CInt(CType(OpenProcess(&H1F0FFF, 1, proc.Id), IntPtr))
            Dim buffer As Byte() = New Byte(3) {}
            buffer.ToArray()
            ReadProcessMemory(winhandle, dwAddress, buffer, 4, 0)
            Dim MySingle As Single = BitConverter.ToSingle(buffer, 0)
            Return MySingle
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    'memory hook function #2
    Private Function ReadInt32(hProcess As String, dwAddress As UInt64) As UInt32
        Try
            Dim proc As Process = Process.GetProcessesByName(hProcess)(0)
            Dim winhandle As Integer = CInt(CType(OpenProcess(&H1F0FFF, 1, proc.Id), IntPtr))
            Dim buffer As Byte() = New Byte(3) {}
            buffer.ToArray()
            ReadProcessMemory(winhandle, dwAddress, buffer, 4, 0)
            Dim MyInt32 As UInteger = BitConverter.ToUInt32(buffer, 0)
            Return MyInt32
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    'memory hook function #2 for 64bit games
    Private Function ReadInt64(hProcess As String, dwAddress As UInt64) As UInt64
        Try
            Dim proc As Process = Process.GetProcessesByName(hProcess)(0)
            Dim winhandle As Integer = CInt(CType(OpenProcess(&H1F0FFF, 1, proc.Id), IntPtr))
            Dim buffer As Byte() = New Byte(8) {}
            buffer.ToArray()
            ReadProcessMemory(winhandle, dwAddress, buffer, 8, 0)
            'Dim MyInt64 As UInt64 = BitConverter.ToUInt64(buffer, 0)
            Dim MyInt64 As UInt64 = BitConverter.ToUInt64(buffer, 0)
            Return MyInt64
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    'memory hook function #3
    Private Function ReadDouble(hProcess As String, dwAddress As UInt64) As Double
        Try
            Dim proc As Process = Process.GetProcessesByName(hProcess)(0)
            Dim winhandle As Integer = CInt(CType(OpenProcess(&H1F0FFF, 1, proc.Id), IntPtr))
            Dim buffer As Byte() = New Byte(7) {}
            buffer.ToArray()
            ReadProcessMemory(winhandle, dwAddress, buffer, 8, 0)
            Dim MyDouble As Double = BitConverter.ToDouble(buffer, 0)
            Return MyDouble
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Function GetProcessBaseAddress(hProcess As String) As UInt64
        Try
            Dim proc As Process = Process.GetProcessesByName(hProcess)(0)
            Dim BaseAddress As IntPtr
            BaseAddress = proc.MainModule.BaseAddress
            Dim BaseAddress64 = BaseAddress.ToInt64

            Return Convert.ToUInt64(BaseAddress64)

        Catch ex As Exception
            Return Nothing
        End Try
    End Function

End Class