using RWCustom;
using UnityEngine;

namespace SmallEel;

public class SmallEelGraphics : GraphicsModule
{
    public SmallEelGraphics(PhysicalObject ow) : base(ow, false)
    {
        _eel = ow as SmallEel;
        bodyParts = new BodyPart[BodyChunks.Length];
        for (int i = 0; i < bodyParts.Length; i++)
        {
            bodyParts[i] = new BodyPart(this);
        }
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[BodyChunks.Length * 2];
        for (int i = 0; i < tris.Length; i++)
        {
            tris[i] = new TriangleMesh.Triangle(i, i + 1, i + 2);
        }
        TriangleMesh mesh = new TriangleMesh("Futile_White", tris, false);
        sLeaser.sprites = new FSprite[] {mesh};
        
        base.InitiateSprites(sLeaser, rCam);
        AddToContainer(sLeaser, rCam, null);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        
        if (culled) return;

        TriangleMesh mesh = sLeaser.sprites[0] as TriangleMesh;

        for (int i = 0; i < BodyChunks.Length + 1; i++)
        {
            BodyChunk firstChunk = BodyChunks[i < BodyChunks.Length - 1 ? i : i - 2];
            BodyChunk secondChunk = BodyChunks[i < BodyChunks.Length - 1 ? i + 1 : i - 1];
            
            Vector2 firstPos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker);
            Vector2 secondPos = Vector2.Lerp(secondChunk.lastPos, secondChunk.pos, timeStacker);
            
            float chunkSeparation = Vector2.Distance(firstPos, secondPos);
            Vector2 dir = Custom.DirVec(secondPos, firstPos);
            Vector2 perp = Custom.PerpendicularVector(dir);
            Vector2 mid = firstPos + dir * chunkSeparation / 2f;
            
            mesh.MoveVertice(2 * i, mid + perp * firstChunk.rad - camPos);
            mesh.MoveVertice(2 * i + 1, mid - perp * firstChunk.rad - camPos);
        }
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        base.ApplyPalette(sLeaser, rCam, palette);

        sLeaser.sprites[0].color = _eel.BaseColor;
    }

    private readonly SmallEel _eel;
    private BodyChunk[] BodyChunks => _eel.bodyChunks;

}
