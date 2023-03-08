using DarkRift.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Specialized;
using DVMP.DTO.ServerSave;

namespace DVServer
{
    class DedicatedServer
    {
        /// <summary>
        ///     The server instance.
        /// </summary>
        static DarkRiftServer server;

        /// <summary>
        ///     Main entry point of the server which starts a single server.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string[] rawArguments = CommandEngine.ParseArguments(string.Join(" ", args));
            string[] arguments = CommandEngine.GetArguments(rawArguments);
            NameValueCollection variables = CommandEngine.GetFlags(rawArguments);

            string configFile;
            if (arguments.Length == 0)
            {
                configFile = "Resources/Server.config";
            }
            else if (arguments.Length == 1)
            {
                configFile = arguments[0];
            }
            else
            {
                System.Console.Error.WriteLine("Invalid comand line arguments.");
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadKey();
                return;
            }

            ServerSpawnData spawnData;

            try
            {
                spawnData = ServerSpawnData.CreateFromXml(configFile, variables);
            }
            catch (IOException e)
            {
                System.Console.Error.WriteLine("Could not load the config file needed to start (" + e.Message + "). Are you sure it's present and accessible?");
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadKey();
                return;
            }
            catch (XmlConfigurationException e)
            {
                System.Console.Error.WriteLine(e.Message);
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadKey();
                return;
            }
            catch (KeyNotFoundException e)
            {
                System.Console.Error.WriteLine(e.Message);
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadKey();
                return;
            }
            ServerManager.Init();
            server = new DarkRiftServer(spawnData);
            server.StartServer();
            ServerSaveManager manager = new ServerSaveManager();
            ServerManager.RegisterSaveManager(manager);
            foreach (ServerSpawnData.PluginsSettings.PluginSettings plugin in spawnData.Plugins.Plugins)
            {
                try
                {
                    ServerManager.RegisterPlugin((IPluginSave)server.PluginManager.GetPluginByName(plugin.Type));
                    Console.WriteLine($"Registered {plugin.Type} for the save manager");
                }
                catch (InvalidCastException)
                {
                }
            }
            ServerManager.Load();
            ServerManager.Start();
            ServerManager.SetServerStatus(SERVER_STATUS.READY);
            Console.WriteLine("Server ready.");
            new Thread(new ThreadStart(ConsoleLoop)).Start();

            while (true)
            {
                server.DispatcherWaitHandle.WaitOne();
                server.ExecuteDispatcherTasks();
            }
        }

        /// <summary>
        ///     Invoked from another thread to repeatedly execute commands from the console.
        /// </summary>
        static void ConsoleLoop()
        {
            while (true)
            {
                string input = Console.ReadLine();
                if (input == "exit")
                {
                    ServerManager.SetServerStatus(SERVER_STATUS.SHUTDOWN);
                    ServerManager.Save(true);
                    server.Dispose();
                    return;
                }
                if (input == "save")
                {
                    ServerManager.Save(true);
                    continue;
                }
                server.ExecuteCommand(input);
            }
        }
    }

    public class ServerSaveManager : ISaveManager
    {
        string saveLocation = "ServerSave.json";
        string data = "";
        public bool Save(List<IPluginSave> plugins, bool force = false)
        {
            PlayerPlugin.PlayerPlugin playerPlugin = (PlayerPlugin.PlayerPlugin)plugins.First(p => p.Name == "PlayerPlugin");
            if (playerPlugin.playerSpawn.Position == null)
                return false;
            foreach (IPluginSave plugin in plugins)
            {
                data += plugin.SaveData() + "$&$";
            }
            File.WriteAllText(saveLocation, data);
            return true;
        }

        public void Load(List<IPluginSave> plugins)
        {
            try
            {
                data = File.ReadAllText(saveLocation);
                string[] tmp = data.Split('$', '&', '$');
                foreach (IPluginSave plugin in plugins)
                {
                    plugin.LoadData(tmp[plugins.BinarySearch(plugin)]);
                }
                Console.WriteLine("Server Save Data successfully loaded.");
            }
            catch (FileNotFoundException)
            {
            }
        }
    }
}

