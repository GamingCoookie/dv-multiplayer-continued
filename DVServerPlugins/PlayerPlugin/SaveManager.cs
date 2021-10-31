using DarkRift.Server;
using DVServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerPlugin
{
    public class SaveManager : Plugin,ISaveManager
    {
        public override bool ThreadSafe => true;

        public override Version Version => new Version(1, 0, 0, 0);

        public SaveManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            DVServer.ServerManager.RegisterSaveManager(this);
        }

        public bool Save(List<IPluginSave> plugins,bool force = false)
        {
            if (!force && ClientManager.Count == 0)
                return false;
            foreach (var item in plugins)
            {
                try
                {
                    var dta = item.SaveData();
                    if (dta != null)
                        ServerManager.SaveObject(Path.Combine(ResourceDirectory, item.Name + ".json"), dta);
                }
                catch (Exception ex)
                {
                    Logger.Error($"{item.Name} Save failed {ex}");
                }
            }
            Logger.Info("Data saved");
            return true;
        }

        public void Load(List<IPluginSave> plugins)
        {
            foreach (var item in plugins)
            {
                try
                {
                    var saveFile = Path.Combine(ResourceDirectory, item.Name + ".json");
                    if (!File.Exists(saveFile))
                        continue;
                    var dta = File.ReadAllText(saveFile);

                    if (dta != null && !String.IsNullOrEmpty(dta))
                        item.LoadData(dta);
                }
                catch (Exception ex)
                {
                    Logger.Error($"{item.Name} Load failed {ex}");
                }
            }
            Logger.Info("Data loaded");
        }
    }
}
