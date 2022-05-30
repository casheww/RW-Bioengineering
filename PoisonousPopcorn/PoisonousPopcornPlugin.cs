using BepInEx;
using UnityEngine;

namespace PoisonousPopcorn;

[BepInPlugin("casheww.poisonous_popcorn", nameof(PoisonousPopcorn), "0.1.0")]
public sealed class PoisonousPopcornPlugin : BaseUnityPlugin
{
    public PoisonousPopcornPlugin()
    {
        Log = Logger;
    }
    
    private void OnEnable()
    {
        On.SeedCob.Open += SeedCob_Open;
    }

    private void OnDisable()
    {
        On.SeedCob.Open -= SeedCob_Open;
    }

    private void SeedCob_Open(On.SeedCob.orig_Open orig, SeedCob self)
    {
        orig(self);

        Vector2 pos = self.AppendagePosition(0, 0);
        
        InsectCoordinator smallInsects = null;
        foreach (UpdatableAndDeletable obj in self.room.updateList)
        {
            if (obj is InsectCoordinator insectCoordinator)
            {
                smallInsects = insectCoordinator;
                break; }
        }

        int cloudCount = (int)Mathf.Lerp(7, 15, Random.value);
        for (int i = 0; i < cloudCount; i++)
        {
            Vector2 vel = new Vector2(Random.value * 2f - 1f, Random.value - 0.7f);
            self.room.AddObject(
                new DeadlyCloud(pos, vel, _color, 1.5f, null, 10, smallInsects, self));
        }
        self.room.PlaySound(SoundID.Puffball_Eplode, self.firstChunk.pos);
    }

    private static readonly Color _color = new Color(0.6f, 0.3f, 0.2f);
    
    public static BepInEx.Logging.ManualLogSource Log { get; private set; }
    public static DebuggingHelpers.DebugNodeManager nodeManager = new DebuggingHelpers.DebugNodeManager();
}
