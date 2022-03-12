using DVMultiplayer;
using System;

namespace DarkRift.Server.Unity
{
    public sealed class UnityModManagerWriter : LogWriter
    {
        public override Version Version
        {
            get
            {
                return new Version(1, 1, 0);
            }
        }

        public UnityModManagerWriter(LogWriterLoadData pluginLoadData) : base(pluginLoadData)
        {
        }

        public override void WriteEvent(WriteEventArgs args)
        {
            switch (args.LogType)
            {
                case LogType.Trace:
                case LogType.Info:
                    Main.mod.Logger.Log($"[SERVER] {args.FormattedMessage}");
                    break;
                case LogType.Warning:
                    Main.mod.Logger.Warning($"[SERVER] {args.FormattedMessage}");
                    break;
                case LogType.Error:
                case LogType.Fatal:
                    Main.mod.Logger.Error($"[SERVER] {args.FormattedMessage}");
                    break;
            }
        }
    }
}
