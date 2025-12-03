using ProjectMotorRacingPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Threading;
using System.Threading.Tasks;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Project Motor Racing")]
    [ExportMetadata("Version", "0.1")]
    public class ProjectMotorRacingPlugin : Game {


        private bool stop = false;
        private Thread readthread;

        public string PROCESS_NAME => "ProjectMotorRacingGame";
        public int STEAM_ID => 299970;
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "Fresh_ch";
        public string Description => Resources.description;
        public Stream Logo =>  GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");


        static IPEndPoint m_endPoint = null;
        static IPAddress m_multicastGroup = null;
        static UdpClient m_udpClient = null;
        static bool m_multiCast = true;
        static int m_defaultPort = 7576;
        static string m_defaultMulticastGroup = "224.0.0.150";

        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private string _DefaultProfile => Resources.profile;
        private int _PlayerId;

        public LedEffect DefaultLED() {
            return new LedEffect(

           EFFECT_TYPE.KNIGHT_RIDER,
           3,
           new YawColor[] {
                new YawColor(66, 135, 245),
                 new YawColor(80,80,80),
                new YawColor(128, 3, 117),
                new YawColor(110, 201, 12),
                },
           36f);
        }

        public List<Profile_Component> DefaultProfile() => dispatcher.JsonToComponents(_DefaultProfile);

        public void PatchGame()
        {
            return;
        }
        public void Exit() {
            stop = true;
            m_udpClient?.DropMulticastGroup(m_multicastGroup);
            m_udpClient?.Close();
            m_udpClient?.Dispose();
        }

        public string[] GetInputData() {
            return new string[] {

                "Yaw","Pitch","Roll","Surge","Heave","Sway","Angular Vel X","Angular Vel Y","Angular Vel Z","RPM","Speed","SideSlip","Suspension FL","Suspension FR","Suspension RL","Suspension RR"
            };
        }
        public void SetReferences(IProfileManager controller,IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {
            Console.WriteLine("Project Motor Racing INIT");
            stop = false;

            m_udpClient = new UdpClient(m_defaultPort);
            m_multicastGroup = IPAddress.Parse(m_defaultMulticastGroup);
            m_endPoint = new IPEndPoint(m_multicastGroup, m_defaultPort);
            m_udpClient.JoinMulticastGroup(m_multicastGroup);

            readthread = new  Thread(async () => await ReadFunctionAsync());
            readthread.Start();
        }

        private async Task ReadFunctionAsync() {

            while (!stop) {
                try
                {
                    var packet = await m_udpClient.ReceiveAsync();
                    byte[] data = packet.Buffer;
                    DecodePacket(ref data);
                }
                catch (Exception e)
                {

                }
            }
        }

        private void DecodePacket(ref byte[] data)
        {
            byte packetType = data[0];
            if (packetType == (byte)UDPPacketType.RaceInfo)
            {
                // UDPRaceInfo p = UDPRaceInfo.decode(ref data, 1);
            }
            else if (packetType == (byte)UDPPacketType.ParticipantRaceState)
            {
                UDPParticipantRaceState p = UDPParticipantRaceState.decode(ref data, 1);
                if (p.m_isPlayer == true)
                {
                    // We must know who is the player so we only take their telemetry
                    _PlayerId = p.m_vehicleId;
                }
            }
            else if (packetType == (byte)UDPPacketType.ParticipantVehicleTelemetry)
            {
                UDPVehicleTelemetry p = UDPVehicleTelemetry.decode(ref data, 1);
                if (p.m_vehicleId == _PlayerId)
                {
                    Vector3 PitchYawRoll = ConversionHelper.QuatToEulerDeg(p.m_chassis.m_quat.y, p.m_chassis.m_quat.x, p.m_chassis.m_quat.z, p.m_chassis.m_quat.w);
                    controller.SetInput(0, -PitchYawRoll.Z); // Yaw
                    controller.SetInput(1, -PitchYawRoll.Y); // Pitch
                    controller.SetInput(2, -PitchYawRoll.X); // Roll
                    controller.SetInput(3, -p.m_chassis.m_accelerationLS.x);// Surge?
                    controller.SetInput(4, p.m_chassis.m_accelerationLS.y);// Heave?
                    controller.SetInput(5, p.m_chassis.m_accelerationLS.z);// Sway?
                    controller.SetInput(6, p.m_chassis.m_angularVelocityLS.x);
                    controller.SetInput(7, p.m_chassis.m_angularVelocityLS.y);
                    controller.SetInput(8, p.m_chassis.m_angularVelocityLS.z); // useful for limited yaw?
                    controller.SetInput(9, p.m_drivetrain.m_engineRPM);
                    controller.SetInput(10, p.m_chassis.m_overallSpeed);
                    controller.SetInput(11, p.m_chassis.m_sideslip);
                    controller.SetInput(12, p.m_wheels[0].SpringStrain); // FL
                    controller.SetInput(13, p.m_wheels[1].SpringStrain); // FR
                    controller.SetInput(14, p.m_wheels[2].SpringStrain); // RL
                    controller.SetInput(15, p.m_wheels[3].SpringStrain); // RR

                }
            }
        }


        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }


        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
        }
        Stream GetStream(string resourceName)
        {
            var assembly = GetType().Assembly;
            var rr = assembly.GetManifestResourceNames();
            string fullResourceName = $"{assembly.GetName().Name}.Resources.{resourceName}";
            return assembly.GetManifestResourceStream(fullResourceName);
        }

        public Type GetConfigBody()
        {
            return null;
        }
    }
}
