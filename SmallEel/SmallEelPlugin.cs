using BepInEx;

namespace SmallEel;

[BepInPlugin("casheww.small_eel", nameof(SmallEel), "0.1.0")]
public sealed class SmallEelPlugin : BaseUnityPlugin
{
    public SmallEelPlugin()
    {
        Log = Logger;
    }

    private void OnEnable()
    {
        Hooks.Enable();
        Fisobs.Core.Content.Register(new SmallEelCritob());
    }

    private void OnDisable() => Hooks.Disable();

    private void Update()
    {
        textManager.Update();
    }

    public static BepInEx.Logging.ManualLogSource Log { get; private set; }
    public static DebuggingHelpers.DebugTextManager textManager = new ();

}
