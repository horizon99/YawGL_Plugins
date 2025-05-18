using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using YawGLAPI;

namespace YawVR_Game_Engine.Plugin
{
    [Export(typeof(Game))]
    [ExportMetadata("Name", "Project Wingman")]
    [ExportMetadata("Version", "1.4")]
    public class ProjectWingmanPlugin : Game
    {
        private bool running = false;
        private IProfileManager controller;
        private IMainFormDispatcher dispatcher;
        private JObject jsonObject;
        private ulong[][] inputAddrs;

        private string[] inputs = [];

        public int STEAM_ID => 895870;

        public string PROCESS_NAME => "ProjectWingman-Win64-Shipping";

        public bool PATCH_AVAILABLE => false;

        public string AUTHOR => "Fresh_ch";

        public string Description => GetString("description.html");

        public Stream Logo => GetStream("logo.png");

        public Stream SmallLogo => GetStream("recent.png");

        public Stream Background => GetStream("wide.png");

        private string defProfile => GetString("Default.yawglprofile");

        public LedEffect DefaultLED() => new LedEffect();

        public List<Profile_Component> DefaultProfile() =>  dispatcher.JsonToComponents(defProfile);

        public void PatchGame()
        {
            
        }

        public string[] GetInputData()
        {
            return inputs;
        }

        public void Init()
        {
            running = true;
            var readThread = new Thread(() =>
            {
                var GameData = new MemoryHook();
                while (running)
                {
                    for (int i = 0; i < inputAddrs.Length; i++)
                    {
                        controller.SetInput(i, GameData.Process_MemoryHook(inputs[i], inputAddrs[i]));
                    }
                    Thread.Sleep(20);
                }
            });
            readThread.Start();
        }

        public void Exit()
        {
            running = false;
            Thread.Sleep(20);
            return;
        }

        public void SetReferences(IProfileManager controller, IMainFormDispatcher dispatcher)
        {
            this.controller = controller;
            this.dispatcher = dispatcher;
            jsonObject = LoadJsonDocument();
            SetupInputs(jsonObject);
        }

        public Dictionary<string, ParameterInfo[]> GetFeatures()
        {
            return null;
        }

        public Type GetConfigBody()
        {
            return null;
        }

        private JObject LoadJsonDocument()
        {
            try
            {
                var assembly = GetType().Assembly;
                var filename = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\YawVR_GameLink\\ObjectFiles\\projectwingman";
                JObject offsetsJson = null;
                var t = dispatcher.GetObjectFile("projectwingman", out  offsetsJson);
                if (offsetsJson == null && File.Exists(filename))
                {
                    using var file = new StreamReader(filename);
                    using var reader = new JsonTextReader(file);
                    offsetsJson = (JObject)JToken.ReadFrom(reader);
                }
                return offsetsJson;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void SetupInputs(JObject objectFileData)
        {
            var inputs = new List<string>();
            inputAddrs = new ulong[objectFileData.Properties().Count()][];
            int counter = 0;

            foreach (var obj in objectFileData)
            {
                inputs.Add($"{obj.Key}");
                var offsets = obj.Value["Offsets"].ToArray();
                inputAddrs[counter] = new ulong[offsets.Length];

                for (int i = 0; i < offsets.Length; i++)
                {
                    var v = offsets[i].ToString();
                    inputAddrs[counter][i] = ulong.Parse(v, System.Globalization.NumberStyles.HexNumber);
                }

                counter++;
            }
            this.inputs = inputs.ToArray();
        }

        Stream GetStream(string resourceName)
        {
            var assembly = GetType().Assembly;
            var rr = assembly.GetManifestResourceNames();

            string fullResourceName = $"{assembly.GetName().Name}.Resources.{resourceName}";

            if (!rr.Contains(fullResourceName))
            {
                dispatcher.ShowNotification(NotificationType.ERROR, "Resource not found - " + fullResourceName);
            }



            return assembly.GetManifestResourceStream(fullResourceName);
        }

        private string GetString(string resourceName)
        {

            var result = string.Empty;
            try
            {
                using var stream = GetStream(resourceName);

                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    result = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                dispatcher.ShowNotification(NotificationType.ERROR, "Error loading resource - " + e.Message);
            }


            return result;
        }
    }
}
