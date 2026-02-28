using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using KamiToolKit;
using MateriaQol.RetrieveAllMateriaFromGearPiece;

namespace MateriaQoL;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    internal static IContextMenu ContextMenu { get; private set; } = null!;

    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;

    [PluginService]
    internal static IChatGui ChatGui { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    [PluginService]
    internal static ICondition Condition { get; private set; } = null!;

    private readonly RetrieveAllMateriaFromGearPiece retrieveAllMateriaFromGearPiece;

    public Plugin()
    {
        KamiToolKitLibrary.Initialize(PluginInterface);
        retrieveAllMateriaFromGearPiece = new RetrieveAllMateriaFromGearPiece();
    }

    public void Dispose()
    {
        retrieveAllMateriaFromGearPiece.Dispose();
        KamiToolKitLibrary.Dispose();
    }
}
