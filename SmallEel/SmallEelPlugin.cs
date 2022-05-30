using System;
using BepInEx;
using UnityEngine;

namespace SmallEel;

[BepInPlugin("casheww.large_mushroom", nameof(SmallEel), "0.1.0")]
public sealed class SmallEelPlugin : BaseUnityPlugin
{
    public SmallEelPlugin()
    {
        Log = Logger;
    }

    private void OnEnable() => Hooks.Enable();

    private void OnDisable() => Hooks.Disable(); 

    public static BepInEx.Logging.ManualLogSource Log { get; private set; }
    
}
