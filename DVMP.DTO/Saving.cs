using System.Collections.Generic;

namespace DVMP.DTO.ServerSave
{
    public interface IPluginSave
    {
        string Name { get; }
        string SaveData();
        void LoadData(string data);
    }

    public interface ISaveManager
    {
        bool Save(List<IPluginSave> plugins, bool force = false);
        void Load(List<IPluginSave> plugins);
    }
}