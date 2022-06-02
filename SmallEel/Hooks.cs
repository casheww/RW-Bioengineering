using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace SmallEel;

public static class Hooks
{
    public static void Enable()
    {
        On.RainWorld.ctor += InitCreatureTemplate;
        On.AbstractCreature.Realize += AbstractCreature_Realize;
        On.AbstractCreature.InitiateAI += AbstractCreature_InitiateAI;
        On.Player.Update += Player_Update;
        On.CreatureTemplate.ctor += CreatureTemplate_ctor;
        On.Player.Grabability += Player_Grabability;
    }

    private static void InitCreatureTemplate(On.RainWorld.orig_ctor orig, RainWorld self)
    {
        orig(self);
        
        CreatureTemplate[] temp = new CreatureTemplate[StaticWorld.creatureTemplates.Length + 1];
        for (int i = 0; i < StaticWorld.creatureTemplates.Length; i++)
            temp[i] = StaticWorld.creatureTemplates[i];

        List<TileTypeResistance> tileResistances = new List<TileTypeResistance>()
        {
            new(AItile.Accessibility.Air, 1f, PathCost.Legality.Allowed)
        };
        List<TileConnectionResistance> connectionResistances = new List<TileConnectionResistance>()
        {
            new(MovementConnection.MovementType.Standard, 1f, PathCost.Legality.Allowed),
            new(MovementConnection.MovementType.ShortCut, 1f, PathCost.Legality.Allowed),
            new(MovementConnection.MovementType.OffScreenMovement, 1f, PathCost.Legality.Allowed),
            new(MovementConnection.MovementType.BetweenRooms, 10f, PathCost.Legality.Allowed),
            new(MovementConnection.MovementType.NPCTransportation, 10f, PathCost.Legality.Allowed)
        };

        CreatureTemplate template = new CreatureTemplate(EnumExt_SmallEel.SmallEel, null, tileResistances,
            connectionResistances,
            new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Antagonizes, 0.2f))
        {
            AI = true,
            requireAImap = true,
            doPreBakedPathing = true,
            preBakedPathingAncestor = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Leech),
            grasps = 1,
            canSwim = true,
            waterRelationship = CreatureTemplate.WaterRelationship.WaterOnly
        };

        temp[temp.Length - 1] = template;
        
        StaticWorld.creatureTemplates = temp;
    }

    public static void Disable()
    {
        On.AbstractCreature.Realize -= AbstractCreature_Realize;
        On.AbstractCreature.InitiateAI -= AbstractCreature_InitiateAI;
        On.Player.Update -= Player_Update;
        On.CreatureTemplate.ctor -= CreatureTemplate_ctor;
        On.Player.Grabability -= Player_Grabability;
    }
    
    private static void AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
    {
        if (self.realizedCreature != null || self.creatureTemplate.TopAncestor().type != EnumExt_SmallEel.SmallEel)
        {
            orig(self);
            return;
        }
        
        self.realizedCreature = new SmallEel(self);

        self.InitiateAI();
        foreach (AbstractPhysicalObject.AbstractObjectStick abstractStick in self.stuckObjects)
        {
            if (abstractStick.A.realizedObject == null)
            {
                abstractStick.A.Realize();
            }
            if (abstractStick.B.realizedObject == null)
            {
                abstractStick.B.Realize();
            }
        }
    }
    
    private static void AbstractCreature_InitiateAI(On.AbstractCreature.orig_InitiateAI orig, AbstractCreature self)
    {
        orig(self);

        if (self.creatureTemplate.TopAncestor().type == EnumExt_SmallEel.SmallEel)
            self.abstractAI.RealAI = new SmallEelAI(self, self.world);
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            IntVector2 tilePos = self.room.GetTilePosition(new Vector2(Input.mousePosition.x, Input.mousePosition.y) + self.room.game.cameras[0].pos);
            WorldCoordinate worldCoordinate = new WorldCoordinate(self.abstractCreature.pos.room, tilePos.x, tilePos.y,
                self.abstractCreature.pos.abstractNode);
            
            AbstractCreature absCreature = new AbstractCreature(
                self.room.world,
                StaticWorld.GetCreatureTemplate(EnumExt_SmallEel.SmallEel),
                null,
                worldCoordinate,
                self.room.world.game.GetNewID());
            
            absCreature.RealizeInRoom();
        }
    }

    private static void CreatureTemplate_ctor(On.CreatureTemplate.orig_ctor orig, CreatureTemplate self,
        CreatureTemplate.Type type, CreatureTemplate ancestor, List<TileTypeResistance> tileResistances,
        List<TileConnectionResistance> connectionResistances, CreatureTemplate.Relationship defaultRelationship)
    {
        orig(self, type, ancestor, tileResistances, connectionResistances, defaultRelationship);
        
        if (type == EnumExt_SmallEel.SmallEel)
            self.name = "SmallEel";
    }

    private static int Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        => obj is SmallEel ? (int)Player.ObjectGrabability.Drag : orig(self, obj);

}
