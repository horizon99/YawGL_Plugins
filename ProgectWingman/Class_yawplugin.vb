Option Strict On
Option Explicit On
Imports System.Reflection
Imports System.IO
Imports System.ComponentModel.Composition
Imports System.Threading
Imports YawGLAPI

Namespace YawVR_Game_Engine.Plugin
    <Export(GetType(Game))>
    <ExportMetadata("Name", "Project Wingman")>
    <ExportMetadata("Version", "1.2")>
    Public Class ProjectWingmanPlugin
        Implements Game

        Private running As Boolean = False
        Private controller As IProfileManager
        Private dispatcher As IMainFormDispatcher
        Private xmlDocument As XDocument

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
                Description = "<p>Anniversary Edition</p>"
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
            MyComponentsList = dispatcher.JsonToComponents(DefProfile)
            Return MyComponentsList
        End Function

        Public Sub PatchGame() Implements Game.PatchGame
            Return
        End Sub

        Public Function GetInputData() As String() Implements Game.GetInputData
            Return New String() {"Heading", "PitchAngle", "RollAngle", "AirSpeed"}
        End Function

        Public Sub Init() Implements Game.Init
            running = True
            Dim readThread As Thread = New Thread(Sub()
                                                      Dim GameData As New MemoryHook
                                                      While running
                                                          GameData.Process_MemoryHook(xmlDocument)
                                                          controller.SetInput(0, GameData.Yaw)
                                                          controller.SetInput(1, GameData.Pitch)
                                                          controller.SetInput(2, GameData.Roll)
                                                          controller.SetInput(3, GameData.Speed)
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
            xmlDocument = LoadXmlDocument()
        End Sub

        Public Function GetFeatures() As Dictionary(Of String, ParameterInfo()) Implements Game.GetFeatures
            'Dim MyFeatures As New Dictionary(Of String, System.Reflection.ParameterInfo())
            Return Nothing
        End Function

        Private Function GetStream(ByVal resourceName As String) As Stream
            Dim assembly = [GetType]().Assembly
            Return assembly.GetManifestResourceStream(assembly.GetName.Name & "." & resourceName)
        End Function

        Private Function LoadXmlDocument() As XDocument
            Try
                Dim assembly As Assembly = [GetType]().Assembly
                Dim filename As String = assembly.GetLoadedModules.First.FullyQualifiedName.Replace(".dll", ".xml")
                Dim OffsetXml As XDocument
                If System.IO.File.Exists(filename) Then
                    ' if present an xml file overloads the file pulled from the repository
                    OffsetXml = XDocument.Load(filename)
                Else
                    'pull the file from the repository
                    Dim fileContent As New Object
                    Dim t = dispatcher.GetObjectFile("projectwingman", fileContent)
                    OffsetXml = XDocument.Parse(fileContent.ToString)
                End If
                Return OffsetXml

            Catch ex As Exception
                Return Nothing
            End Try
        End Function

    End Class
End Namespace
