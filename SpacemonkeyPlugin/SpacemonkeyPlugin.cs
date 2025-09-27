using SpacemonkeyPlugin.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Xml;
using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "SpaceMonkey")]
    [ExportMetadata("Version", "1.2")]
    class SpacemonkeyPlugin : Game {
        
        UdpClient udpClient;


        Thread readThread;
        private IPEndPoint remoteIP;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private bool running = false;
        public string PROCESS_NAME => "SpaceMonkeyStart";
        public int STEAM_ID => 0 ;
        public bool PATCH_AVAILABLE => false;
        public string AUTHOR => "Fresh_ch";

        public string Description => Resources.description;
        public Stream Logo => GetStream("logo.png");
        public Stream SmallLogo => GetStream("recent.png");
        public Stream Background => GetStream("wide.png");

        private const int Port = 20777;
        
        public List<Profile_Component> DefaultProfile() {

            return new List<Profile_Component>() {
                 new Profile_Component(7,0,1f,1f,0f,false,true,-1,1f),

                new Profile_Component(1,3, 2,2,0f,false,false,-1,1f), //amp
                new Profile_Component(0,4, 17, 17,0f,true,false,-1,1f), //hz

                new Profile_Component(5,1,0.4f,0.4f,0f,false,false,-1,1f), //pitch_orien
                new Profile_Component(6,2,1,1,0f,false,false,-1,1f), //roll_orien

                new Profile_Component(3,1, 5,5,0f,false,true,-1,1f), //pitch_g
                new Profile_Component(4,2,3,3,0f,false,true,-1,1f), //roll_g
            };
        }
        public LedEffect DefaultLED() {

            return new LedEffect(

                EFFECT_TYPE.KNIGHT_RIDER,
                1,
                new YawColor[] {
                    new YawColor(255, 255, 255),
                    new YawColor(80, 80, 80),
                    new YawColor(255, 255, 0),
                    new YawColor(0, 0, 255),
                },
                1f);
        }

        public void Exit() {
            udpClient.Close();
            udpClient = null;
            running = false;
        }

        public string[] GetInputData() {
            return new string[] {
                //"Speed","RPM","Steer","Force_long","Force_lat","Pitch","Roll","Yaw",
                //"suspen_pos_bl","suspen_pos_br","suspen_pos_fl","suspen_pos_fr",
                //"suspen_vel_bl","suspen_vel_br","suspen_vel_fl","suspen_vel_fr","VelocityX","VelocityY","VelocityZ"
                "Yaw","Pitch","Roll",
                "Yaw_velocity","Pitch_velocity","Roll_velocity",
                "Yaw_accel","Pitch_accel","Roll_accel",
                "Pos_X","Pox_Y","Pos_Z",
                "Local_vel_X","Local_vel_Y","Local_vel_Z",
                "GForce_lateral","GForce_longitudinal","GForce_vertical",
                "Speed",
                "suspen_pos_bl","suspen_pos_br","suspen_pos_fl","suspen_pos_fr",
                "suspen_vel_bl","suspen_vel_br","suspen_vel_fl","suspen_vel_fr",
                "suspen_acc_bl","suspen_acc_br","suspen_acc_fl","suspen_acc_fr",
                "wheel_patch_bl","wheel_patch_br","wheel_patch_fl","wheel_patch_fr",
                "Throttle_input","Steering_input","Brake_input","Clutch_input",
                "Gear",
                "Engine_rate"
            };
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
        }
        public void Init() {
            udpClient = new UdpClient(Port);
            readThread = new Thread(new ThreadStart(ReadFunction));
            running = true;
            readThread.Start();

        }
       
        private void ReadFunction() {
            try {
                while (running) {
                    byte[] rawData = udpClient.Receive(ref remoteIP);

                    float yaw = (float)(ReadSingle(rawData, 8, true) * 57.3);
                    float pitch = (float)(ReadSingle(rawData, 12, true) * 57.3);
                    float roll = (float)(ReadSingle(rawData, 16, true) * 57.3);
                    controller.SetInput(0, yaw);
                    controller.SetInput(1, pitch);
                    controller.SetInput(2, roll);

                    float yaw_velocity = (float)(ReadSingle(rawData, 20, true));
                    float pitch_velocity = (float)(ReadSingle(rawData, 24, true));
                    float roll_velocity = (float)(ReadSingle(rawData, 28, true));
                    controller.SetInput(3, yaw_velocity);
                    controller.SetInput(4, pitch_velocity);
                    controller.SetInput(5, roll_velocity);

                    float yaw_acceleration = (float)(ReadSingle(rawData, 32, true));
                    float pitch_acceleration = (float)(ReadSingle(rawData, 36, true));
                    float roll_acceleration = (float)(ReadSingle(rawData, 40, true));
                    controller.SetInput(6, yaw_acceleration);
                    controller.SetInput(7, pitch_acceleration);
                    controller.SetInput(8, roll_acceleration);

                    float position_x = (float)(ReadSingle(rawData, 44, true));
                    float position_y = (float)(ReadSingle(rawData, 48, true));
                    float position_z = (float)(ReadSingle(rawData, 52, true));
                    controller.SetInput(9, position_x);
                    controller.SetInput(10, position_y);
                    controller.SetInput(11, position_z);

                    float local_velocity_x = (float)(ReadSingle(rawData, 56, true));
                    float local_velocity_y = (float)(ReadSingle(rawData, 60, true));
                    float local_velocity_z = (float)(ReadSingle(rawData, 64, true));
                    controller.SetInput(12, local_velocity_x);
                    controller.SetInput(13, local_velocity_y);
                    controller.SetInput(14, local_velocity_z);

                    float gforce_lateral = (float)(ReadSingle(rawData, 68, true));
                    float gforce_longitudinal = (float)(ReadSingle(rawData, 72, true));
                    float gforce_vertical = (float)(ReadSingle(rawData, 76, true));
                    controller.SetInput(15, gforce_lateral);
                    controller.SetInput(16, gforce_longitudinal);
                    controller.SetInput(17, gforce_vertical);

                    float speed = (float)(ReadSingle(rawData, 80, true));
                    controller.SetInput(18, speed);

                    float susp_pos_bl = (float)ReadSingle(rawData, 84, true);
                    float susp_pos_br = (float)ReadSingle(rawData, 88, true);
                    float susp_pos_fl = (float)ReadSingle(rawData, 92, true);
                    float susp_pos_fr = (float)ReadSingle(rawData, 96, true);
                    float susp_velo_bl = (float)ReadSingle(rawData, 100, true);
                    float susp_velo_br = (float)ReadSingle(rawData, 104, true);
                    float susp_velo_fl = (float)ReadSingle(rawData, 108, true);
                    float susp_velo_fr = (float)ReadSingle(rawData, 112, true);
                    float susp_accel_bl = (float)ReadSingle(rawData, 116, true);
                    float susp_accel_br = (float)ReadSingle(rawData, 120, true);
                    float susp_accel_fl = (float)ReadSingle(rawData, 124, true);
                    float susp_accel_fr = (float)ReadSingle(rawData, 128, true);
                    controller.SetInput(19, susp_pos_bl);
                    controller.SetInput(20, susp_pos_br);
                    controller.SetInput(21, susp_pos_fl);
                    controller.SetInput(22, susp_pos_fr);
                    controller.SetInput(23, susp_velo_bl);
                    controller.SetInput(24, susp_velo_br);
                    controller.SetInput(25, susp_velo_fl);
                    controller.SetInput(26, susp_velo_fr);
                    controller.SetInput(27, susp_accel_bl);
                    controller.SetInput(28, susp_accel_br);
                    controller.SetInput(29, susp_accel_fl);
                    controller.SetInput(30, susp_accel_fr);

                    float wheel_patch_bl = (float)ReadSingle(rawData, 132, true);
                    float wheel_patch_br = (float)ReadSingle(rawData, 136, true);
                    float wheel_patch_fl = (float)ReadSingle(rawData, 140, true);
                    float wheel_patch_fr = (float)ReadSingle(rawData, 144, true);
                    controller.SetInput(31, wheel_patch_bl);
                    controller.SetInput(32, wheel_patch_br);
                    controller.SetInput(33, wheel_patch_fl);
                    controller.SetInput(34, wheel_patch_fr);

                    float throttle_input = (float)(ReadSingle(rawData, 148, true));
                    float steering_input = (float)(ReadSingle(rawData, 152, true));
                    float brake_input = (float)(ReadSingle(rawData, 156, true));
                    float clutch_input = (float)(ReadSingle(rawData, 160, true));
                    controller.SetInput(35, throttle_input);
                    controller.SetInput(36, steering_input);
                    controller.SetInput(37, brake_input);
                    controller.SetInput(38, clutch_input);

                    float gear = (float)(ReadSingle(rawData, 164, true));
                    float engine_rate = (float)(ReadSingle(rawData, 172, true));
                    controller.SetInput(39, gear);
                    controller.SetInput(40, engine_rate);

                    // float pitch = (float)(Math.Asin(-forwardY) * 57.3);
                    // float roll = -(float)(Math.Asin(-rollY) * 57.3);
                    // float yaw = (float)Math.Atan2(forwardY + forwardX, forwardZ) * 57.3f;
                }

            }
            catch (SocketException) {
            }
            catch (ThreadAbortException) { }
        }

      
        public void PatchGame() {
            
        }

        float ReadSingle(byte[] data, int offset, bool littleEndian)
        {
            if (BitConverter.IsLittleEndian != littleEndian)
            {   // other-endian; reverse this portion of the data (4 bytes)
                byte tmp = data[offset];
                data[offset] = data[offset + 3];
                data[offset + 3] = tmp;
                tmp = data[offset + 1];
                data[offset + 1] = data[offset + 2];
                data[offset + 2] = tmp;
            }
            return BitConverter.ToSingle(data, offset);
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
