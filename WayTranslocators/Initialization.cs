using System.Collections.Generic;
using Vintagestory.API.Common;

namespace WayTranslocators;

public class Initialization : ModSystem
{
    public ICoreAPI api;
    private readonly Overwrite overwrite = new();

    public override void Start(ICoreAPI _api)
    {
        api = _api;
        base.Start(api);

        Debug.LoadLogger(api.Logger);
        Debug.Log($"Running on Version: {Mod.Info.Version}");
        overwrite.OverwriteNativeFunctions(this);
    }
}

public class Debug
{
    static private ILogger logger;

    static public void LoadLogger(ILogger _logger) => logger = _logger;
    static public void Log(string message)
    {
        logger?.Log(EnumLogType.Notification, $"[WayTranslocator] {message}");
    }
}