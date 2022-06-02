using RWCustom;
using UnityEngine;

namespace SmallEel;

public static class Hooks
{
    public static void Enable()
    {
        On.Player.Update += Player_Update;
    }

    public static void Disable()
    {
        On.Player.Update -= Player_Update;
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
