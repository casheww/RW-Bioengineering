using UnityEngine;

namespace PoisonousPopcorn;

public class DeadlyCloud : SporeCloud
{
    public DeadlyCloud(Vector2 pos, Vector2 vel, Color color, float size, AbstractCreature killTag,
        int checkInsectsDelay, InsectCoordinator smallInsects, SeedCob cob, float strength = 1.4f)
        : base(pos, vel, color, size, killTag, checkInsectsDelay, smallInsects)
    {
        _cob = cob;
        _strength = strength;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);

        foreach (AbstractCreature absCreature in room.abstractRoom.creatures)
        {
            Creature realCreature = absCreature.realizedCreature;

            if (realCreature is InsectoidCreature ||
                realCreature.dead ||
                absCreature.creatureTemplate.type is CreatureTemplate.Type.Spider or CreatureTemplate.Type.Fly)
                continue;
            
            PoisonousPopcornPlugin.nodeManager.Draw($"cloud{Random.value:F3}", Color.cyan, room, room.GetTilePosition(pos), frames: 2);

            float resistance = Mathf.Clamp01(realCreature.TotalMass / _strength);
            float dist = Mathf.Clamp01(RWCustom.Custom.Dist(pos, realCreature.firstChunk.pos) / (rad * 20f));
            float prob = Mathf.Cos(dist * Mathf.PI / 2f) * (1 - dist) / 40f;
            if (Random.value < prob)
                realCreature.Die();
            else if (Random.value < prob)
                realCreature.Violence(_cob.firstChunk, null, realCreature.firstChunk,
                    null, Creature.DamageType.Explosion, 0.7f, 0.2f);
        }
    }


    private SeedCob _cob;
    private float _strength;

}
