using System.Collections.Generic;
using Fisobs.Creatures;
using Fisobs.Properties;
using Fisobs.Sandbox;

namespace SmallEel;

public class SmallEelCritob : Critob
{
    public SmallEelCritob() : base(EnumExt_SmallEel.SmallEel)
    {
        RegisterUnlock(KillScore.Configurable(4), EnumExt_SmallEel.SmallEelUnlock, MultiplayerUnlocks.SandboxUnlockID.SeaLeech);
    }
    
    public override Creature GetRealizedCreature(AbstractCreature acrit)
        => new SmallEel(acrit);
    
    public override ArtificialIntelligence GetRealizedAI(AbstractCreature acrit)
        => new SmallEelAI(acrit);

    public override IEnumerable<CreatureTemplate> GetTemplates()
    {
        CreatureTemplate ct = new CreatureFormula(this, "Small Eel")
        {
            DefaultRelationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, 0.5f),
            HasAI = true,
            Pathing = PreBakedPathing.Ancestral(CreatureTemplate.Type.Leech),
            TileResistances = new TileResist {Air = new PathCost(1f, PathCost.Legality.Allowed)},
            ConnectionResistances = new ConnectionResist
            {
                Standard = new PathCost(1f, PathCost.Legality.Allowed),
                ShortCut = new PathCost(1.2f, PathCost.Legality.Allowed),
                OffScreenMovement = new PathCost(3f, PathCost.Legality.Allowed),
                BetweenRooms = new PathCost(40f, PathCost.Legality.Allowed),
                NPCTransportation = new PathCost(40f, PathCost.Legality.Allowed)
            },
            DamageResistances = new AttackResist {Base = 1f, Electric = 100f, Water = 50f, Blunt = 20f},
            StunResistances = new AttackResist {Base = 1f, Electric = 100f, Water = 50f},
            InstantDeathDamage = 5f
        }.IntoTemplate();

        ct.bodySize = 1f;
        ct.stowFoodInDen = true;
        ct.grasps = 1;
        ct.visualRadius = 800f;
        ct.waterVision = 1f;
        ct.throughSurfaceVision = 0.6f;
        ct.dangerousToPlayer = 0.6f;
        ct.canSwim = true;
        ct.waterRelationship = CreatureTemplate.WaterRelationship.WaterOnly;

        yield return ct;
    }

    public override void EstablishRelationships()
    {
        Relationships rel = new Relationships(EnumExt_SmallEel.SmallEel);
        
        rel.Eats(CreatureTemplate.Type.Leech, 1.0f);
        rel.Eats(CreatureTemplate.Type.Snail, 0.9f);
        rel.Eats(CreatureTemplate.Type.VultureGrub, 0.9f);
        rel.Eats(CreatureTemplate.Type.SmallCentipede, 0.8f);
        rel.Eats(CreatureTemplate.Type.Hazer, 0.8f);
        rel.Eats(CreatureTemplate.Type.Slugcat, 0.7f);
        rel.Eats(CreatureTemplate.Type.CicadaA, 0.7f);
        rel.Eats(CreatureTemplate.Type.CicadaB, 0.7f);
        rel.Eats(CreatureTemplate.Type.SeaLeech, 0.7f);
        rel.Eats(CreatureTemplate.Type.Fly, 0.2f);
        
        rel.Fears(CreatureTemplate.Type.BigEel, 1.0f);
        rel.Fears(CreatureTemplate.Type.Salamander, 0.9f);
        rel.Fears(CreatureTemplate.Type.DaddyLongLegs, 0.9f);
        rel.Fears(CreatureTemplate.Type.BrotherLongLegs, 0.8f);
        rel.Fears(CreatureTemplate.Type.KingVulture, 0.7f);
        rel.Fears(CreatureTemplate.Type.Vulture, 0.6f);
        rel.Fears(CreatureTemplate.Type.TentaclePlant, 0.6f);
        rel.Fears(CreatureTemplate.Type.PoleMimic, 0.5f);
        rel.Fears(CreatureTemplate.Type.LizardTemplate, 0.5f);
        rel.Fears(CreatureTemplate.Type.JetFish, 0.2f);

        rel.Rivals(CreatureTemplate.Type.RedCentipede, 0.7f);
        rel.Rivals(CreatureTemplate.Type.Centipede, 0.4f);

    }

    public override ItemProperties Properties(PhysicalObject forObject)
        => forObject is SmallEel eel ? new SmallEelProperties(eel) : null;

    private class SmallEelProperties : ItemProperties
    {
        public SmallEelProperties(SmallEel eel)
            => _eel = eel;

        public override void Grabability(Player player, ref Player.ObjectGrabability grabability)
            => grabability = _eel.State.alive
                ? Player.ObjectGrabability.CantGrab
                : Player.ObjectGrabability.TwoHands;
        
        private readonly SmallEel _eel;
    }
    
}
