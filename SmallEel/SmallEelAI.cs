using System.Collections.Generic;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace SmallEel;

public class SmallEelAI : ArtificialIntelligence, IUseARelationshipTracker
{
    public SmallEelAI(AbstractCreature abstractCreature) : base(abstractCreature, abstractCreature.world)
    {
        eel = abstractCreature.realizedCreature as SmallEel;
        eel.ai = this;

        InitAIModules();
        
        MyBehaviour = Behaviour.Idle;

        if (SmallEelPlugin.debugMode)
        {
            tracker.visualize = true;
            pathFinder.visualize = true;
            utilityComparer.visualize = true;
        }
    }

    private void InitAIModules()
    {
        AddModule(new FishPather(this, creature.world, creature));
        pathFinder.stepsPerFrame = 40;

        AddModule(new Tracker(this, 10, 10, -1, 0.35f, 5, 5, 10));
        AddModule(new DenFinder(this, creature));
        AddModule(new RelationshipTracker(this, tracker));

        AddModule(new RainTracker(this));
        AddModule(new ThreatTracker(this, 3));
        AddModule(new PreyTracker(this, 5, 2f, 10f, 250f, 0.5f));

        AddModule(new UtilityComparer(this));
        utilityComparer.AddComparedModule(rainTracker, null, 0.9f, 1f);

        FloatTweener.FloatTween smoother = new FloatTweener.FloatTweenUpAndDown(
            new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Lerp, 0.5f),
            new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Tick, 0.0025f));

        utilityComparer.AddComparedModule(threatTracker, smoother, 1f, 1f);

        smoother = new FloatTweener.FloatTweenBasic(FloatTweener.TweenType.Lerp, 0.15f);

        utilityComparer.AddComparedModule(preyTracker, smoother, 1f, 1.3f);
    }

    public override void Update()
    {
        base.Update();

        UpdateBehaviour();
        WorldCoordinate? dest;

        switch (MyBehaviour)
        {
            default:
            case Behaviour.Idle:
                dest = FindWanderCoordinate(true);
                break;
            case Behaviour.Flee:
                dest = threatTracker.FleeTo(creature.pos, 1, 30, utilityComparer.HighestUtility() > 0.4f);
                break;
            case Behaviour.Hunt:
                dest = preyTracker.MostAttractivePrey?.BestGuessForPosition();
                break;
            case Behaviour.ReturnPreyToDen:
            case Behaviour.EscapeRain:
                dest = denFinder.denPosition;
                break;
        }

        dest = dest is null && MyBehaviour != Behaviour.Idle ? FindWanderCoordinate(true) : dest;

        if (dest is null)
            SmallEelPlugin.Log.LogDebug("dest is null after force-wander");

        dest = dest is null ? denFinder.GetDenPosition() : dest;

        if (dest is null)
        {
            SmallEelPlugin.textManager.Write(creature.ID.ToString(), $"{MyBehaviour} : LOST : energy{eel.Energy}", eel.BaseColor);
            return;
        }
        
        creature.abstractAI.SetDestination(dest.Value);

        if (eel.room is not null)
            SmallEelPlugin.nodeManager.Draw(creature.ID.ToString(), Color.red, eel.room, dest.Value.TileDefined ? dest.Value.Tile : eel.room.exitAndDenIndex[dest.Value.abstractNode]);
        SmallEelPlugin.textManager.Write(creature.ID.ToString(), $"{MyBehaviour} : {dest} : {(eel.room is null ? "null" : eel.room == eel.room.game.cameras[0].room)} : energy{eel.Energy}", eel.BaseColor);
    }
    
    private void UpdateBehaviour()
    {
        AIModule highestModule = utilityComparer.HighestUtilityModule();

        MyBehaviour = highestModule switch
        {
            RainTracker => Behaviour.EscapeRain,
            ThreatTracker => Behaviour.Flee,
            PreyTracker => Behaviour.Hunt,
            null => Behaviour.Idle,
            _ => MyBehaviour
        };

        if (utilityComparer.HighestUtility() < 0.1f)
        {
            MyBehaviour = Behaviour.Idle;
        }

        bool hasPrey = eel.grasps[0]?.grabbed is Creature;

        if (hasPrey && (MyBehaviour != Behaviour.Flee || utilityComparer.HighestUtility() < 0.3f))
        {
            MyBehaviour = Behaviour.ReturnPreyToDen;
        }
        else if (hasPrey && MyBehaviour == Behaviour.Flee && utilityComparer.HighestUtility() > 0.75f)
        {
            eel.ReleaseGrasp(0);
        }
    }

    private WorldCoordinate? FindWanderCoordinate(bool considerTerrainProx)
    {
        IntVector2 tile = eel.abstractCreature.pos.Tile;
        const int maxTileDist = 3;
        const float dirPersistence = 0.995f;
        List<IntVector2> currAccessDisplacement = new ();
        List<IntVector2> prevAccessDisplacement = new ();

        for (int i = 1; i <= maxTileDist; i++)
        {
            for (int j = 0; j < Custom.eightDirections.Length; j++)
            {
                IntVector2 dest = tile + Custom.eightDirections[j] * i;

                WorldCoordinate destWC = new WorldCoordinate(eel.room.abstractRoom.index, dest.x, dest.y, -1);
                AItile aiTile = creature.Room.realizedRoom.aimap.getAItile(dest);

                if (aiTile.acc != AItile.Accessibility.Solid && (aiTile.terrainProximity > 3 || !considerTerrainProx)
                    && pathFinder.CoordinateReachableAndGetbackable(destWC))
                {
                    currAccessDisplacement.Add(Custom.eightDirections[j]);
                }
            }

            if (currAccessDisplacement.Count == 0 && prevAccessDisplacement.Count > 0)
            {
                if (!(prevAccessDisplacement.Contains(wanderDir) && Random.value < dirPersistence))
                {
                    wanderDir = prevAccessDisplacement[Random.Range(0, prevAccessDisplacement.Count)];
                }
                
                return eel.abstractCreature.pos + wanderDir * (i - 1);
            }
            else if (currAccessDisplacement.Count > 0 && i == maxTileDist)
            {
                if (!(currAccessDisplacement.Contains(wanderDir) && Random.value < dirPersistence))
                {
                    wanderDir = currAccessDisplacement[Random.Range(0, currAccessDisplacement.Count)];
                }

                return eel.abstractCreature.pos + wanderDir * i;
            }

            prevAccessDisplacement.Clear();
            prevAccessDisplacement.AddRange(currAccessDisplacement);
            currAccessDisplacement.Clear();
        }

        // if we've failed and this call considered terrain proximity, try again but without considering terrain proximity
        if (considerTerrainProx)
        {
            return FindWanderCoordinate(false);
        }

        return null;
    }

    public bool DoIWantToShockCreature(AbstractCreature absCreature)
    {
        CreatureTemplate template = StaticWorld.GetCreatureTemplate(EnumExt_SmallEel.SmallEel);
        return template.CreatureRelationship(absCreature.creatureTemplate).type is
            CreatureTemplate.Relationship.Type.Afraid or
            CreatureTemplate.Relationship.Type.Attacks or
            CreatureTemplate.Relationship.Type.Eats or
            CreatureTemplate.Relationship.Type.AgressiveRival;
    }

    AIModule IUseARelationshipTracker.ModuleToTrackRelationship(CreatureTemplate.Relationship relationship)
        => relationship.type switch
        {
            CreatureTemplate.Relationship.Type.Eats           => preyTracker,
            CreatureTemplate.Relationship.Type.Afraid         => threatTracker,
            CreatureTemplate.Relationship.Type.AgressiveRival => threatTracker,
            CreatureTemplate.Relationship.Type.Uncomfortable  => threatTracker,
            CreatureTemplate.Relationship.Type.StayOutOfWay   => threatTracker,
            _ => null
        };

    CreatureTemplate.Relationship IUseARelationshipTracker.UpdateDynamicRelationship(RelationshipTracker.DynamicRelationship dRelation)
    {
        return dRelation.state is SmallEelTrackerState ? dRelation.currentRelationship : default;
    }

    RelationshipTracker.TrackedCreatureState IUseARelationshipTracker.CreateTrackedCreatureState(RelationshipTracker.DynamicRelationship rel)
    {
        return new SmallEelTrackerState();
    }
    

    private readonly SmallEel eel;
    
    public Behaviour MyBehaviour { get; private set; }
    private IntVector2 wanderDir;

    public AbstractCreature TargetCreature
    {
        get
        {
            if (utilityComparer.HighestUtilityModule() == preyTracker)
                return preyTracker.currentPrey?.critRep?.representedCreature;
            if (utilityComparer.HighestUtilityModule() == threatTracker)
                return threatTracker.mostThreateningCreature?.representedCreature;
            return null;
        }
    }


    public enum Behaviour
    {
        Idle,
        Flee,
        Hunt,
        ReturnPreyToDen,
        EscapeRain,
    }


    private class SmallEelTrackerState : RelationshipTracker.TrackedCreatureState
    {
    }
    
}
