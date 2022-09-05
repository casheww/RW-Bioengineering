using RWCustom;
using UnityEngine;

namespace SmallEel;

public class SmallEelAI : ArtificialIntelligence, IUseARelationshipTracker
{
    public SmallEelAI(AbstractCreature abstractCreature) : base(abstractCreature, abstractCreature.world)
    {
        _eel = abstractCreature.realizedCreature as SmallEel;
        _eel.ai = this;

        InitAIModules();
        
        MyBehaviour = Behaviour.Idle;
        _idleBearing = Mathf.FloorToInt(Random.value * 360f);

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
                dest = FindWanderCoordinate();
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

        if (dest is null)
        {
            dest = denFinder.GetDenPosition();
        }

        if (dest is null)
        {
            SmallEelPlugin.textManager.Write(creature.ID.ToString(), $"{MyBehaviour} : LOST : energy{_eel.Energy}", _eel.BaseColor);
            return;
        }
        
        creature.abstractAI.SetDestination(dest.Value);

        if (_eel.room is not null)
            SmallEelPlugin.nodeManager.Draw(creature.ID.ToString(), Color.red, _eel.room, dest.Value.TileDefined ? dest.Value.Tile : _eel.room.exitAndDenIndex[dest.Value.abstractNode]);
        SmallEelPlugin.textManager.Write(creature.ID.ToString(), $"{MyBehaviour} : {dest} : energy{_eel.Energy}", _eel.BaseColor);
    }
    
    private void UpdateBehaviour()
    {
        AIModule highestModule = utilityComparer.HighestUtilityModule();

        if (highestModule != null)
        {
            MyBehaviour = highestModule switch
            {
                RainTracker   => Behaviour.EscapeRain,
                ThreatTracker => Behaviour.Flee,
                PreyTracker   => Behaviour.Hunt,
                _ => MyBehaviour
            };
        }

        bool hasPrey = _eel.grasps[0]?.grabbed is Creature;

        if (hasPrey && (MyBehaviour != Behaviour.Flee || utilityComparer.HighestUtility() < 0.3f))
        {
            MyBehaviour = Behaviour.ReturnPreyToDen;
        }
        else if (hasPrey && MyBehaviour == Behaviour.Flee && utilityComparer.HighestUtility() > 0.75f)
        {
            _eel.ReleaseGrasp(0);
        }
    }

    private WorldCoordinate? FindWanderCoordinate()
    {
        // TODO: use smoothed noise or only change heading every x frames

        const float idleWanderMod = 15f;
        const float lookAheadPixels = 40f;
        const int retries = 10;

        int bearingChange = Mathf.RoundToInt(Random.Range(-idleWanderMod, idleWanderMod));

        for (int i = 0; i < retries; i++)
        {
            _idleBearing += bearingChange;

            if (_idleBearing < 0) _idleBearing = Mathf.FloorToInt(360 + _idleBearing % 360);
            else if (_idleBearing >= 360) _idleBearing = Mathf.FloorToInt(_idleBearing % 360);

            IntVector2 relativeDest = IntVector2.FromVector2(Custom.DegToVec(_idleBearing) * lookAheadPixels);
            WorldCoordinate dest = WorldCoordinate.AddIntVector(creature.pos, relativeDest);
            AItile aiTile = creature.Room.realizedRoom.aimap.getAItile(dest);

            if (aiTile.acc != AItile.Accessibility.Solid && aiTile.terrainProximity > 2
                && pathFinder.CoordinateReachableAndGetbackable(dest))
            {
                return dest;
            }
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
    

    private readonly SmallEel _eel;
    private int _idleBearing;
    
    public Behaviour MyBehaviour { get; private set; }

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
