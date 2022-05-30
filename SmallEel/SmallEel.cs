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
    }
    
    private void GenerateIVars(out int chunkCount)
    {
        int seed = Random.seed;
        Random.seed = abstractCreature.ID.RandomSeed;
        
        _baseColor = new HSLColor(Random.value, 0.4f, 0.3f).rgb;
        chunkCount = Mathf.FloorToInt(Mathf.Lerp(4f, 9f, Random.value));
        
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
        {
            Shock();
            _shockCooldown = bodyChunks.Length * Mathf.FloorToInt(Mathf.Lerp(20f, 80f, 1f / bodyChunks.Length));
        }

        mainBodyChunk.vel += Vector2.up * 5f;
        
        if (Consious)
            Act();
    }

    private void UpdateDevTools()
    {
        if (room.game.devToolsActive && Input.GetKey(KeyCode.B) && room.game.cameras[0].room == room)
        {
            bodyChunks[0].vel +=
                RWCustom.Custom.DirVec(bodyChunks[0].pos, new Vector2(Input.mousePosition.x, Input.mousePosition.y) + room.game.cameras[0].pos) * 14f;
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
        
    }
    
    private void Shock()
    {
        
    }
    
    public override void InitiateGraphicsModule()
    {
        graphicsModule ??= new SmallEelGraphics(this);
    }


    private Color _baseColor;
    private int _shockCooldown;

}
