using DarkRift;
using DarkRift.Server;
using DVMP.DTO.ServerSave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DVServer
{

    public enum SERVER_STATUS
    {
        INITIALIZING,
        READY,
        SHUTDOWN
    }

    public static class ServerManager
    {
        static List<IPluginSave> _RegisteredPlugins = new List<IPluginSave>();
        static ISaveManager SaveManager;
        static long _ServerStatus = 0;
        static Timer Timer;

        public static SendMode Unreliable =>   SendMode.Unreliable; //SendMode.Unreliable;//
        internal static void Init()
        {
            var tmp = JsonConvert.SerializeObject(_RegisteredPlugins);
        }

        public static void RegisterPlugin(IPluginSave plugin)
        {
            _RegisteredPlugins.Add(plugin);
        }
        public static void RegisterSaveManager(ISaveManager saveManager)
        {
            SaveManager = saveManager;
        }

        internal static void Start()
        {
            Timer = new Timer(SaveTimeCallBack, null, 1000, 60000 * 5);
        }

        private static void SaveTimeCallBack(object state)
        {
            Save();
        }

        internal static void SetServerStatus(SERVER_STATUS newVal)
        {
            Interlocked.Exchange(ref _ServerStatus, (long)newVal);
        }

        public static bool ServerIsReady() => Interlocked.Read(ref _ServerStatus) == (long)SERVER_STATUS.READY;

        internal static void Save(bool force = false)
        {
            try
            {
                SaveManager?.Save(_RegisteredPlugins, force);
                Console.WriteLine("Saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save: {ex.Message}");
            }
        }

        internal static void Load()
        {
            SaveManager?.Load(_RegisteredPlugins);
        }

        public static void SaveObject(string filename, object dataObj)
        {
            if (dataObj != null)
                File.WriteAllText(filename,JsonConvert.SerializeObject(dataObj));
        
        }
        public static T LoadObject<T>(string data)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(data);
            }
            catch (Exception)
            {
            }
            return default;
        }
    }
}
