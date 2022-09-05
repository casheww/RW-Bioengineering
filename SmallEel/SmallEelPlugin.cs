using BepInEx;

namespace SmallEel;

[BepInDependency("github.notfood.BepInExPartialityWrapper", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin("casheww.small_eel", nameof(SmallEel), "0.1.0")]
public sealed class SmallEelPlugin : BaseUnityPlugin
{
    public SmallEelPlugin()
    {
        Log = Logger;
    }

    private void OnEnable()
    {
        if (debugMode)
            Hooks.Enable();
        
        Fisobs.Core.Content.Register(new SmallEelCritob());
    }

    private void OnDisable()
    {
        if (debugMode)
            Hooks.Disable();
    }

    private void Update()
    {
        textManager.Update();
        nodeManager.Update();
    }

    public static BepInEx.Logging.ManualLogSource Log { get; private set; }
    public static DebuggingHelpers.DebugTextManager textManager = new();
    public static DebuggingHelpers.DebugNodeManager nodeManager = new();

    public const bool debugMode = true;

}
