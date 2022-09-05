using System.Linq;
using RWCustom;
using UnityEngine;

namespace SmallEel;

public class SmallEel : Creature
{
    public SmallEel(AbstractCreature abstractCreature) : base(abstractCreature, abstractCreature.world)
    {
        GenerateIVars(out size, out int chunkCount);
        
        bodyChunks = new BodyChunk[chunkCount];
        bodyChunkConnections = new BodyChunkConnection[bodyChunks.Length - 1];
        
        for (int i = 0; i < bodyChunks.Length; i++)
        {
            bodyChunks[i] = new BodyChunk(this, i, Vector2.zero, 10f, 0.05f);
        }
        for (int i = 0; i < bodyChunkConnections.Length; i++)
        {
            bodyChunkConnections[i] = new BodyChunkConnection(bodyChunks[i], bodyChunks[i + 1],
                Mathf.Max(bodyChunks[i].rad, bodyChunks[i + 1].rad), BodyChunkConnection.Type.Normal, 1f, -1f);
        }

        airFriction = 0.99f;
        gravity = 0.9f;
        bounce = 0.1f;
        surfaceFriction = 0.1f;
        collisionLayer = 1;
        waterFriction = 0.8f;
        waterRetardationImmunity = 0.1f;
        buoyancy = 0.7f;
        GoThroughFloors = false;

        oscillationPeriod = 30f;
        oscillationPosition = 0f;
    }
    
    private void GenerateIVars(out float size, out int chunkCount)
    {
        int seed = Random.seed;
        Random.seed = abstractCreature.ID.RandomSeed;
        
        BaseColor = new HSLColor(Random.value, 0.25f, 0.2f).rgb;

        size = Random.value;      // TODO: load size from abstractCreature.spawnData
        chunkCount = Mathf.FloorToInt(Mathf.Lerp(8f, 15f, size));
        
        Random.seed = seed;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        
        UpdateDevTools();
        UpdateHealthState();
        UpdateShortcuts();

        // decrement cooldown when alive
        if (!dead)
            Energy = Mathf.Clamp01(Energy + Random.Range(0.001f, 0.01f));

        // shock when grabbed - may happen once after death if charge was stored
        if (grabbedBy.Count > 0)
        {
            if (dead && Energy >= 0.1f || !dead && Energy >= 0.7f)
                Shock();
        }

        if (Consious)
            Act();

        if (grasps[0] != null)
        {
            BodyChunk bc = grasps[0].grabbedChunk;
            Vector2 v = Custom.DirVec(bc.pos, mainBodyChunk.pos) * Custom.Dist(bc.pos, mainBodyChunk.pos);
            bc.vel += v * 0.9f;
            mainBodyChunk.vel -= v * 0.1f;
        }
    }

    private void UpdateDevTools()
    {
        if (room?.game?.devToolsActive is not null && room.game.devToolsActive && Input.GetKey(KeyCode.B) && room.game.cameras[0].room == room)
        {
            foreach (BodyChunk bc in bodyChunks)
            {
                bc.vel +=
                    Custom.DirVec(bc.pos, new Vector2(Input.mousePosition.x, Input.mousePosition.y) + room.game.cameras[0].pos) * 20f;
            }

            Stun(12);
        }
    }

    private void UpdateHealthState()
    {
        if (State is not HealthState healthState) return;
        
        if (Submersion <= 0f)
            healthState.health -= 1f / 200f;
            
        if (healthState.health < 0.15f * Random.value)
        {
            Stun(4);
            if (healthState.health <= 0f && Random.value < 0.2f)
            {
                Die();
            }
        }
    }

    private void UpdateShortcuts()
    {
        if (CurrentMovement is null) return;

        if ((CurrentMovement.type == MovementConnection.MovementType.ShortCut || !CurrentMovement.destinationCoord.TileDefined)
            && room.GetTile(mainBodyChunk.pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
        {
            enteringShortCut = new IntVector2?(room.GetTilePosition(mainBodyChunk.pos));

            foreach (BodyChunk bc in bodyChunks)
            {
                bc.vel *= 0.05f;
            }
        }

        WorldCoordinate dest = ai.pathFinder.destination;

        if (enteringShortCut is null && dest.NodeDefined && room.abstractRoom.index == dest.room)
        {
            Vector2 middleOfNode = room.MiddleOfTile(room.LocalCoordinateOfNode(dest.abstractNode));

            Vector2 dir = middleOfNode - mainBodyChunk.pos;

            if (Custom.DistLess(mainBodyChunk.pos, middleOfNode, 40f))
            {
                SmallEelPlugin.Log.LogDebug($"eel approaching shortcut");

                foreach (BodyChunk bc in bodyChunks)
                {
                    bc.vel += middleOfNode - bc.pos;
                    SmallEelPlugin.Log.LogDebug(bc.vel);
                }
            }
        }
    }

    private void Act()
    {
        ai.Update();

        if (Submersion > 0f)
        {
            Swim(CurrentMovement, ai.pathFinder.destination);

            if (ai.TargetCreature?.realizedCreature is null) return;

            if (ai.MyBehaviour is SmallEelAI.Behaviour.Flee or SmallEelAI.Behaviour.Hunt)
            {
                float dist = Custom.Dist(mainBodyChunk.pos, ai.TargetCreature.realizedCreature.mainBodyChunk.pos);

                if (dist < ShockRad && CanIKillCreature(ai.TargetCreature.realizedCreature) && Energy >= 0.7f)
                    Shock();
            }
            
            if (ai.MyBehaviour is SmallEelAI.Behaviour.Hunt)
            {
                Creature prey = ai.TargetCreature.realizedCreature;
                int closestIndex = GetClosestChunkIndex(prey, out float closestDist);

                if (closestDist < 20f && grasps[0] == null)
                {
                    Grab(prey, 0, closestIndex, Grasp.Shareability.CanOnlyShareWithNonExclusive,
                        1f, true, false);
                }
            }
        }
    }

    private int GetClosestChunkIndex(Creature creature, out float closestDist)
    {
        int closestIndex = 0;
        closestDist = float.MaxValue;

        for (int i = 0; i < creature.bodyChunks.Length; i++)
        {
            float d = Custom.Dist(mainBodyChunk.pos, creature.bodyChunks[i].pos);
            if (d < closestDist)
            {
                closestIndex = i;
                closestDist = d;
            }
        }

        return closestIndex;
    }

    private void Swim(MovementConnection moveConn, WorldCoordinate destination)
    {
        Vector2? dir = null;
        
        if (destination.TileDefined && destination.room == room.abstractRoom.index &&
            room.VisualContact(room.GetTilePosition(mainBodyChunk.pos), destination.Tile))
        {
            dir = Custom.DirVec(mainBodyChunk.pos, room.MiddleOfTile(destination.Tile));
        }
        else if (moveConn != null)
        {
            dir = (moveConn.DestTile - moveConn.StartTile).ToVector2().normalized;
        }
        
        if (dir != null)
        {
            mainBodyChunk.vel += dir.Value * Speed;

            for (int i = 1; i < bodyChunks.Length; i++)
            {
                bodyChunks[i].vel += Custom.DirVec(bodyChunks[i].pos, bodyChunks[i - 1].pos) * Speed * 0.4f;
            }

            Wiggle(dir.Value);
        }
    }

    private void Wiggle(Vector2 dir)
    {
        Vector2 perp = Custom.PerpendicularVector(dir);
        float x = OscillationAmplitude * Mathf.Sin(oscillationPosition * 2 * Mathf.PI);
        oscillationPosition = oscillationPosition >= 1f ? 0f : oscillationPosition + (1 / oscillationPeriod);
        mainBodyChunk.vel += perp * x;
    }

    private void Shock()
    {
        room.PlaySound(SoundID.Centipede_Shock, mainBodyChunk.pos);

        int sparkCount = (int)Mathf.Lerp(2f, 5f, Random.value);
        for (int i = 0; i < sparkCount; i++)
        {
            room.AddObject(new Spark(mainBodyChunk.pos, Custom.RNV() * Mathf.Lerp(4f, 16f, Random.value),
                SparkColor, null, 5, 12));
        }
        
        float effectiveEnergy = TotalMass * Energy;
        
        foreach (Grasp g in grabbedBy)
        {
            if (CanIKillCreature(g.grabber))
            {
                g.grabber.Die();
                room.AddObject(new CreatureSpasmer(g.grabber, true, (int)Mathf.Lerp(50f, 100f, size)));
            }
            else
            {
                g.grabber.Stun((int)Custom.LerpMap(g.grabber.TotalMass, 0f, effectiveEnergy, 250f, 25f));
                room.AddObject(new CreatureSpasmer(g.grabber, true, g.grabber.stun));
                g.grabber.LoseAllGrasps();
            }
        }
        
        if (Submersion > 0.15f)
            UnderwaterShock(effectiveEnergy);

        Energy = 0f;
    }

    private bool CanIKillCreature(Creature target)
        => TotalMass * 1.2f * Energy > target.TotalMass * target.Template.damageRestistances[5, 0] * 0.2f;

    private void UnderwaterShock(float e)
    {
        room.PlaySound(SoundID.Centipede_Shock, mainBodyChunk.pos);
        room.AddObject(new UnderwaterShock(room, this, mainBodyChunk.pos, 14, ShockRad, 
            0.2f + 1.9f * e, this, SparkColor));
    }
    
    public override void Stun(int st)
    {
        LoseAllGrasps();
        base.Stun(st);
    }
    
    public override void InitiateGraphicsModule()
    {
        graphicsModule ??= new SmallEelGraphics(this);
    }


    public SmallEelAI ai;
    private MovementConnection CurrentMovement
    {
        get
        {
            if (ai?.pathFinder is null || room is null) return null;

            return (ai.pathFinder as FishPather).FollowPath(room.GetWorldCoordinate(mainBodyChunk.pos), true);
        }
    }

    public float Energy { get; private set; }
    
    public readonly float size;
    private float Speed
        => ai.MyBehaviour switch
        {
            SmallEelAI.Behaviour.Idle => 10f,
            SmallEelAI.Behaviour.Flee => 80f,
            SmallEelAI.Behaviour.Hunt => 20f,
            SmallEelAI.Behaviour.ReturnPreyToDen => 20f,
            SmallEelAI.Behaviour.EscapeRain => 50f,
            _ => 10f
        };

    public Color BaseColor { get; private set; }

    private Color SparkColor
    {
        get
        {
            float r = Mathf.Lerp(0.8f, 0.95f, Random.value);
            float g = Mathf.Lerp(0.8f, 0.95f, Random.value);
            float b = Mathf.Lerp(0.65f, 0.75f, Random.value);
            return new Color(r, g, b);
        }
    }
    private float ShockRad => Mathf.Lerp(100f, 250f, size) + (Random.value - 0.5f) * 100f;

    private float oscillationPeriod;
    private float oscillationPosition;
    private float OscillationAmplitude => ai?.MyBehaviour == SmallEelAI.Behaviour.Hunt ? 5f : 10f;

}
