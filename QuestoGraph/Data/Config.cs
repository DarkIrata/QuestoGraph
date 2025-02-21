using Dalamud.Configuration;

namespace QuestoGraph.Data
{
    [Serializable]
    internal class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
    }
}
