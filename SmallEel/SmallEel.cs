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
        waterFriction = 0.9f;
        waterRetardationImmunity = 0.1f;
        buoyancy = 0.85f;
        GoThroughFloors = false;

        _oscillationPeriod = 30f;
        _oscillationPosition = 0f;
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

        // decrement cooldown when alive
        if (shockCooldown > 0 && !dead)
            shockCooldown--;
        
        // shock when grabbed - may happen once after death if cooldown was 0 before death
        if (grabbedBy.Count > 0 && shockCooldown <= 0)
            TryShock();

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
        if (room.game.devToolsActive && Input.GetKey(KeyCode.B) && room.game.cameras[0].room == room)
        {
            bodyChunks[0].vel +=
                Custom.DirVec(bodyChunks[0].pos, new Vector2(Input.mousePosition.x, Input.mousePosition.y) + room.game.cameras[0].pos) * 14f;
            Stun(12);
        }
    }

    private void UpdateHealthState()
    {
        if (State is HealthState healthState && healthState.health < 0.2f * Random.value)
        {
            Stun(4);
            if (healthState.health <= 0f && Random.value < 0.2f)
            {
                Die();
            }
        }
    }

    private void Act()
    {
        ai.Update();

        if (Submersion >= 0.3f)
        {
            Swim();

            if (ai.MyBehaviour is SmallEelAI.Behaviour.Flee or SmallEelAI.Behaviour.Hunt)
            {
                float dist = Custom.Dist(mainBodyChunk.pos, ai.TargetCreature.realizedCreature.mainBodyChunk.pos);

                if (dist < ShockRad)
                    TryShock();
            }
            
            if (ai.MyBehaviour is not SmallEelAI.Behaviour.Hunt) return;
                
            Creature prey = ai.TargetCreature.realizedCreature;
            int closestIndex = 0;
            float closestDist = float.MaxValue;

            for (int i = 0; i < prey.bodyChunks.Length; i++)
            {
                float d = Custom.Dist(mainBodyChunk.pos, prey.bodyChunks[i].pos);
                if (d < closestDist)
                {
                    closestIndex = i;
                    closestDist = d;
                }
            }

            if (closestDist < 30f && grasps[0] == null)
            {
                Grab(prey, 0, closestIndex, Grasp.Shareability.CanOnlyShareWithNonExclusive,
                    1f, true, false);
            }
        }
    }

    private void Swim()
    {
        MovementConnection moveConn =
            (ai.pathFinder as FishPather).FollowPath(room.GetWorldCoordinate(mainBodyChunk.pos), true);
        WorldCoordinate overallDest = ai.pathFinder.destination;

        Vector2? dir = null;
        
        if (overallDest.TileDefined && overallDest.room == room.abstractRoom.index &&
            room.VisualContact(room.GetTilePosition(mainBodyChunk.pos), overallDest.Tile))
        {
            dir = Custom.DirVec(mainBodyChunk.pos, room.MiddleOfTile(overallDest.Tile));
        }
        else if (moveConn != null)
        {
            dir = (moveConn.DestTile - moveConn.StartTile).ToVector2().normalized;
        }

        if (dir == null)
        {
            Debug.LogWarning("dir is null");
        }
        else
        {
            mainBodyChunk.vel += dir.Value * Speed;
            Wiggle(dir.Value);
        }
    }

    private void Wiggle(Vector2 dir)
    {
        Vector2 perp = Custom.PerpendicularVector(dir);
        float x = OscillationAmplitude * Mathf.Sin(_oscillationPosition * 2 * Mathf.PI);
        _oscillationPosition = _oscillationPosition >= 1f ? 0f : _oscillationPosition + (1 / _oscillationPeriod);
        mainBodyChunk.vel += perp * x;
    }

    private bool TryShock()
    {
        Debug.Log("shock");
        if (shockCooldown > 0 || Submersion < 0.3f) return false;
        
        room.PlaySound(SoundID.Centipede_Shock, mainBodyChunk.pos);
        room.AddObject(new UnderwaterShock(room, this, this.mainBodyChunk.pos, 14, ShockRad,
            0.2f+ 1.8f * size, this, new Color(0.9f, 0.9f, 0.7f)));

        shockCooldown = bodyChunks.Length * Mathf.FloorToInt(Mathf.Lerp(20f, 80f, 1f / bodyChunks.Length));
        return true;
    }
    
    public override void InitiateGraphicsModule()
    {
        graphicsModule ??= new SmallEelGraphics(this);
    }

    
    public SmallEelAI ai;
    public int shockCooldown;
    public readonly float size;
    private float Speed => ai?.MyBehaviour == SmallEelAI.Behaviour.Idle ? 2f : 3.5f;
    public Color BaseColor { get; private set; }
    private float ShockRad => Mathf.Lerp(250f, 500f, size) + +(Random.value - 0.5f) * 160f;

    private float _oscillationPeriod;
    private float _oscillationPosition;
    private float OscillationAmplitude => ai?.MyBehaviour == SmallEelAI.Behaviour.Hunt ? 7.5f : 15f;

}
