Option Strict On
Option Explicit On
Imports System.Text
Imports System.Reflection
Imports ProjectWingmanPlugin.YawVR_Game_Engine.Plugin
Imports YawGLAPI

Public Class MemoryHook
    '/// Per Game Settings - Change for Each Game ///
    Public Yaw As Single
    Public Pitch As Single
    Public Roll As Single
    Public Speed As Single
    Private Const _ProcessName As String = "ProjectWingman-Win64-Shipping" 'Process_Name without the (".exe") for this game

    '***********Offset for Project Wingman official (non beta) version as of 2023
    'Private Const _MemHook_Yaw As UInt64 = &H4FC
    'Private Const _MemHook_Pitch As UInt64 = &H4F8
    'Private Const _MemHook_Roll As UInt64 = &H4F4
    'Private Const _MemHook_Speed As UInt64 = &H45C
    'Private Const _BasePtrAddr As UInt64 = &H6C20000 ' Base pointer 
    'Private Const _Offset0 As UInt64 = &H0
    'Private Const _Offset1 As UInt64 = &H110
    'Private Const _Offset2 As UInt64 = &H9C0

    '******************Offsets for Project Wingman Anniversary Edition (beta branch)
    'Private Const _MemHook_Speed As UInt64 = &H5EC
    'Private Const _BasePtrAddr As UInt64 = &H8E10040 ' Base pointer 
    'Private Const _Offset0 As UInt64 = &H0
    'Private Const _Offset1 As UInt64 = &H20
    'Private Const _Offset2 As UInt64 = &H750

    'Private Const _MemHook_Yaw As UInt64 = &H4F0
    'Private Const _MemHook_Pitch As UInt64 = &H4EC
    'Private Const _MemHook_Roll As UInt64 = &H4E8
    'Private Const _BasePtrAddr2 As UInt64 = &H8E0E2A0 ' Base pointer 
    'Private Const _Offset10 As UInt64 = &H90
    'Private Const _Offset11 As UInt64 = &H3B0

    'Private Const _BasePtrAddr As UInt64 = &H8E10040 ' Base pointer 
    'Private Const _Offset0 As UInt64 = &H0
    'Private Const _Offset1 As UInt64 = &H20
    'Private Const _Offset2 As UInt64 = &H750

    'Offsets for Project Wingman Anniversary Edition (Main branch - Update 12.2024)
    Private Const _MemHook_Yaw As UInt64 = &H544
    Private Const _MemHook_Pitch As UInt64 = &H540
    Private Const _MemHook_Roll As UInt64 = &H53C
    Private Const _MemHook_Speed As UInt64 = &H6B4
    Private Const _Offset0 As UInt64 = &H30
    Private Const _Offset1 As UInt64 = &HC70
    Private Const _Offset2 As UInt64 = &H7D0

    Private _BaseAddr As UInt64 'This is the memory offset
    Private _BasePtrAddr As UInt64 = &H95AC140 ' Base pointer 
    Private _PitchOffsets() As XAttribute
    Private _RollOffsets() As XAttribute
    Private _YawOffsets() As XAttribute
    Private _SpeedOffsets() As XAttribute

    'Used by the plugin main loop to Process a MemoryHook.
    Public Sub Process_MemoryHook(xmlDocument As XDocument)

        If IsNothing(xmlDocument) Then
            Try
                ' Can't use xml file. Use values hardcoded above instead.
                _BaseAddr = GetProcessBaseAddress(_ProcessName)
                _BaseAddr = ReadInt64(_ProcessName, _BaseAddr + _BasePtrAddr)
                _BaseAddr = ReadInt64(_ProcessName, _BaseAddr + _Offset0)
                _BaseAddr = ReadInt64(_ProcessName, _BaseAddr + _Offset1)
                _BaseAddr = ReadInt64(_ProcessName, _BaseAddr + _Offset2)
                Speed = CSng(ReadSingle(_ProcessName, _BaseAddr + _MemHook_Speed))
                Yaw = CSng(ReadSingle(_ProcessName, _BaseAddr + _MemHook_Yaw))
                Pitch = CSng(ReadSingle(_ProcessName, _BaseAddr + _MemHook_Pitch)) * -1
                Roll = CSng(ReadSingle(_ProcessName, _BaseAddr + _MemHook_Roll)) * -1
            Catch
                Yaw = 0
                Pitch = 0
                Roll = 0
                Speed = 0
            End Try

        Else
            ReadXmlOffsets(xmlDocument)
            Yaw = ProcessXmlOffsets(_YawOffsets)
            Pitch = ProcessXmlOffsets(_PitchOffsets)
            Roll = ProcessXmlOffsets(_RollOffsets)
            Speed = ProcessXmlOffsets(_SpeedOffsets)
        End If

        ' This is to prevent "wild" values outside of a mission
        If Speed > 1000 Then Speed = 0
        If Yaw > 180 Or Yaw < -180 Then Yaw = 0
        If Pitch > 180 Or Pitch < -180 Then Pitch = 0
        If Roll > 180 Or Roll < -180 Then Roll = 0

    End Sub

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

    Private Function ProcessXmlOffsets(Offset() As XAttribute) As Single
        Try
            _BaseAddr = GetProcessBaseAddress(_ProcessName)
            Dim nboffsets As Integer
            nboffsets = CInt(Offset(0).Value)
            _BasePtrAddr = CType("&H" & Offset(1).Value, UInt64)
            _BaseAddr = ReadInt64(_ProcessName, _BaseAddr + _BasePtrAddr)

            For cnt As Integer = 2 To nboffsets - 1
                _BaseAddr = ReadInt64(_ProcessName, _BaseAddr + CType("&H" & Offset(cnt).Value, UInt64))
            Next
            Return CSng(ReadSingle(_ProcessName, _BaseAddr + CType("&H" & Offset(nboffsets).Value, UInt64)))

        Catch ex As Exception
            Return 0
        End Try

    End Function

    Private Sub ReadXmlOffsets(OffsetXml As XDocument)
        Try
            Dim contents As XElement = OffsetXml.Elements("pointers").First
            _PitchOffsets = contents.Elements("Pitch").First.Attributes.ToArray
            _RollOffsets = contents.Elements("Roll").First.Attributes.ToArray
            _YawOffsets = contents.Elements("Yaw").First.Attributes.ToArray
            _SpeedOffsets = contents.Elements("Speed").First.Attributes.ToArray
        Catch ex As Exception
        End Try
    End Sub
End Class