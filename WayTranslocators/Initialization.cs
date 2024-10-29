using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace WayTranslocators;

public class Initialization : ModSystem
{
    public ICoreAPI api;
    private readonly Overwrite overwrite = new();
    public static ICoreClientAPI ClientAPI { get; private set; }

    public override void Start(ICoreAPI _api)
    {
        api = _api;
        base.Start(api);

        Debug.LoadLogger(api.Logger);
        Debug.Log($"Running on Version: {Mod.Info.Version}");
        overwrite.OverwriteNativeFunctions(this);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientAPI = api;
        base.StartClientSide(api);
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