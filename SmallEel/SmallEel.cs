using RWCustom;
using UnityEngine;

namespace SmallEel;

public class SmallEel : Creature
{
    public SmallEel(AbstractCreature abstractCreature) : base(abstractCreature, abstractCreature.world)
    {
        GenerateIVars(out int chunkCount);
        
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
        _oscillationAmplitude = 40f;
    }
    
    private void GenerateIVars(out int chunkCount)
    {
        int seed = Random.seed;
        Random.seed = abstractCreature.ID.RandomSeed;
        
        BaseColor = new HSLColor(Random.value, 0.4f, 0.3f).rgb;

        float size = Random.value;      // TODO: load size from abstractCreature.spawnData
        chunkCount = Mathf.FloorToInt(Mathf.Lerp(5f, 15f, size));
        
        Random.seed = seed;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        
        UpdateDevTools();
        UpdateHealthState();

        // decrement cooldown when alive
        if (_shockCooldown > 0 && !dead)
            _shockCooldown--;
        
        // shock when grabbed - may happen after death if cooldown was 0 before death
        if (grabbedBy.Count > 0 && _shockCooldown <= 0)
            TryShock();

        if (Consious)
            Act();
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
            Swim();
        else
        {
            
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
        float x = _oscillationAmplitude * Mathf.Sin(_oscillationPosition * 2 * Mathf.PI);
        _oscillationPosition = _oscillationAmplitude >= 1f ? 0f : _oscillationAmplitude + (1 / _oscillationPeriod);
        mainBodyChunk.vel += perp * x;
    }

    private bool TryShock()
    {
        if (_shockCooldown > 0) return false;
        
        Debug.Log("EEL SHOCK");
        
        _shockCooldown = bodyChunks.Length * Mathf.FloorToInt(Mathf.Lerp(20f, 80f, 1f / bodyChunks.Length));
        return true;
    }
    
    public override void InitiateGraphicsModule()
    {
        graphicsModule ??= new SmallEelGraphics(this);
    }

    
    public SmallEelAI ai;
    public Color BaseColor { get; private set; }
    private int _shockCooldown;

    private const float maxAccel = 0.05f;
    private float Speed => ai?.MyBehaviour == SmallEelAI.Behaviour.Idle ? 2f : 3.5f;
    
    private float _oscillationPeriod;
    private float _oscillationPosition;
    private float _oscillationAmplitude;

}
