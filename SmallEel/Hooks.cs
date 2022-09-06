using System;
using RWCustom;
using UnityEngine;

namespace SmallEel;

public static class Hooks
{
    public static void Enable()
    {
        On.Player.Update += Player_Update;
        On.ShortcutHandler.SpitOutCreature += ShortcutHandler_SpitOutCreature;
    }

    public static void Disable()
    {
        On.Player.Update -= Player_Update;
    }

    private static void ShortcutHandler_SpitOutCreature(On.ShortcutHandler.orig_SpitOutCreature orig, ShortcutHandler self, ShortcutHandler.ShortCutVessel vessel)
    {
        orig(self, vessel);

        if (vessel.creature is SmallEel eel)
        {
            eel.shortcutPushDir = vessel.room.realizedRoom.ShorcutEntranceHoleDirection(vessel.pos).ToVector2();
            SmallEelPlugin.Log.LogDebug(eel.shortcutPushDir);
        }
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

}
