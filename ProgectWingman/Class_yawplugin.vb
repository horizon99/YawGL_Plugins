Option Strict On
Option Explicit On
Imports System.Reflection
Imports System.IO
Imports System.ComponentModel.Composition
Imports System.Threading
Imports Newtonsoft.Json.Linq
Imports YawGLAPI
Imports System.Text.Json.Nodes
Imports Newtonsoft.Json

Namespace YawVR_Game_Engine.Plugin
    <Export(GetType(Game))>
    <ExportMetadata("Name", "Project Wingman")>
    <ExportMetadata("Version", "1.2")>
    Public Class ProjectWingmanPlugin
        Implements Game

        Private running As Boolean = False
        Private controller As IProfileManager
        Private dispatcher As IMainFormDispatcher
        Private jsonObject As JObject
        Private inputAddrs As UInt64()()
        Private inputs As String() = New String(-1) {}

        Public ReadOnly Property STEAM_ID As Integer Implements Game.STEAM_ID
            Get
                Return 895870
            End Get
        End Property

        Public ReadOnly Property PROCESS_NAME As String Implements Game.PROCESS_NAME
            Get
                Return "ProjectWingman-Win64-Shipping"
            End Get
        End Property

        Public ReadOnly Property PATCH_AVAILABLE As Boolean Implements Game.PATCH_AVAILABLE
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property AUTHOR As String Implements Game.AUTHOR
            Get
                Return "Fresh_ch"
            End Get
        End Property

        Public ReadOnly Property Description() As String Implements Game.Description
            Get
                Description = "<p>Anniversary Edition</p><p>NB: this plugin only works when launching the game in VR mode.</p>"
            End Get
        End Property

        Public ReadOnly Property Logo() As IO.Stream Implements Game.Logo
            Get
                Return GetStream("logo.png")
            End Get
        End Property

        Public ReadOnly Property SmallLogo() As Stream Implements Game.SmallLogo
            Get
                Return GetStream("recent.png")
            End Get
        End Property

        Public ReadOnly Property Background As IO.Stream Implements Game.Background
            Get
                Return GetStream("wide.png")
            End Get
        End Property

        Public Function DefaultLED() As LedEffect Implements Game.DefaultLED
            Return New LedEffect()
        End Function

        Public Function DefaultProfile() As List(Of Profile_Component) Implements Game.DefaultProfile
            Dim defProfile As String
            Dim assembly = [GetType]().Assembly
            Dim stream As Stream = assembly.GetManifestResourceStream(assembly.GetName.Name & "." & "DefaultProfile.txt")
            Dim reader As New StreamReader(stream)
            defProfile = reader.ReadToEnd
            Dim MyComponentsList = New List(Of Profile_Component)()
            MyComponentsList = dispatcher.JsonToComponents(defProfile)
            Return MyComponentsList
        End Function

        Public Sub PatchGame() Implements Game.PatchGame
            Return
        End Sub

        Public Function GetInputData() As String() Implements Game.GetInputData
            'Return New String() {"Heading", "PitchAngle", "RollAngle", "AirSpeed"}
            Return inputs
        End Function

        Public Sub Init() Implements Game.Init
            running = True
            Dim readThread As Thread = New Thread(Sub()
                                                      Dim GameData As New MemoryHook
                                                      While running
                                                          For i As Integer = 0 To inputAddrs.Length - 1
                                                              controller.SetInput(i, GameData.Process_MemoryHook(inputs(i), inputAddrs(i)))
                                                          Next
                                                          Thread.Sleep(20)
                                                      End While
                                                  End Sub)
            readThread.Start()
        End Sub

        Public Sub [Exit]() Implements Game.Exit
            running = False
            Thread.Sleep(20)
            Return
        End Sub

        Public Sub SetReferences(ByVal controller As IProfileManager, ByVal dispatcher As IMainFormDispatcher) Implements Game.SetReferences
            Me.controller = controller
            Me.dispatcher = dispatcher
            jsonObject = LoadJsonDocument()
            SetupInputs(jsonObject)
        End Sub

        Public Function GetFeatures() As Dictionary(Of String, ParameterInfo()) Implements Game.GetFeatures
            'Dim MyFeatures As New Dictionary(Of String, System.Reflection.ParameterInfo())
            Return Nothing
        End Function

        Private Function GetStream(ByVal resourceName As String) As Stream
            Dim assembly = [GetType]().Assembly
            Return assembly.GetManifestResourceStream(assembly.GetName.Name & "." & resourceName)
        End Function

        Private Function LoadJsonDocument() As JObject
            Try
                Dim assembly As Assembly = [GetType]().Assembly
                Dim filename As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\YawVR_GameLink\ObjectFiles\projectwingman"
                Dim OffsetsJson As JObject = Nothing
                'pull the file from the repository
                Dim t = dispatcher.GetObjectFile("projectwingman", OffsetsJson)
                'if the repo is not available, try to use the local file
                If IsNothing(OffsetsJson) And System.IO.File.Exists(filename) Then
                    Dim file As New StreamReader(filename)
                    Dim reader As New JsonTextReader(file)
                    OffsetsJson = CType(JToken.ReadFrom(reader), JObject)
                End If
                Return OffsetsJson

            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        Private Sub SetupInputs(ByVal objectFileData As JObject)
            Dim inputs As List(Of String) = New List(Of String)()
            inputAddrs = New UInt64(objectFileData.Properties().Count() - 1)() {}
            Dim counter As Integer = 0

            For Each obj In objectFileData
                inputs.Add($"{obj.Key}")
                Dim offsets = obj.Value("Offsets").ToArray()
                inputAddrs(counter) = New UInt64(offsets.Length - 1) {}

                For i As Integer = 0 To offsets.Length - 1
                    Dim v As String = offsets(i).ToString()
                    inputAddrs(counter)(i) = CType(Integer.Parse(v, System.Globalization.NumberStyles.HexNumber), UInt64)
                Next

                counter += 1
            Next
            Me.inputs = inputs.ToArray()

        End Sub
    End Class
End Namespace
