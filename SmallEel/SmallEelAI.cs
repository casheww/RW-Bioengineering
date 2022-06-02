using RWCustom;
using UnityEngine;

namespace SmallEel;

public class SmallEelAI : ArtificialIntelligence, IUseARelationshipTracker
{
    public SmallEelAI(AbstractCreature abstractCreature) : base(abstractCreature, abstractCreature.world)
    {
        _eel = abstractCreature.realizedCreature as SmallEel;
        _eel.ai = this;
        AddModule(new FishPather(this, abstractCreature.world, abstractCreature));
        pathFinder.stepsPerFrame = 40;
        
        AddModule(new Tracker(this, 10, 10, -1, 0.5f, 5, 5, 20));

        AddModule(new RainTracker(this));
        AddModule(new DenFinder(this, abstractCreature));
        AddModule(new ThreatTracker(this, 3));
        AddModule(new PreyTracker(this, 5, 1f, 10f, 150f, 0.05f));
        AddModule(new InjuryTracker(this, 0.6f));
        
        AddModule(new RelationshipTracker(this, tracker));
        
        AddModule(new UtilityComparer(this));
        utilityComparer.AddComparedModule(rainTracker, null, 1f, 1.1f);
        utilityComparer.AddComparedModule(threatTracker, null, 1f, 1.1f);
        utilityComparer.AddComparedModule(preyTracker, null, 0.8f, 1.1f);
        utilityComparer.AddComparedModule(injuryTracker, null, 0.7f, 1.1f);

        MyBehaviour = Behaviour.Idle;
        _idleBearing = Mathf.FloorToInt(Random.value * 360f);
    }

    public override void Update()
    {
        base.Update();

        AIModule highestModule = utilityComparer.HighestUtilityModule();
        float highestModuleVal = utilityComparer.HighestUtility();

        if (highestModule != null)
        {
            MyBehaviour = highestModule switch
            {
                RainTracker => Behaviour.EscapeRain,
                ThreatTracker => Behaviour.Flee,
                PreyTracker => Behaviour.Hunt,
                InjuryTracker => Behaviour.Injured,
                _ => MyBehaviour
            };
        }

        if (highestModuleVal < 0.1f)
            MyBehaviour = Behaviour.Idle;

        WorldCoordinate? dest = null;

        switch (MyBehaviour)
        {
            default:
            case Behaviour.Idle:
                dest = FindWanderCoordinate();
                break;
            case Behaviour.Flee:
            {
                dest = threatTracker.FleeTo(creature.pos, 1, 30, highestModuleVal > 0.4f);
                break;
            }
            case Behaviour.Hunt:
                creature.abstractAI.SetDestination(preyTracker.MostAttractivePrey.BestGuessForPosition());
                break;
            case Behaviour.EscapeRain:
            case Behaviour.Injured:
                break;
        }

        if ((dest == null || _lost) && denFinder.GetDenPosition() != null)
            dest = denFinder.GetDenPosition().Value;
        
        SmallEelPlugin.textManager.Write(creature.ID.ToString(), $"{MyBehaviour} : {dest} : shock{_eel.shockCooldown}", _eel.BaseColor);
        
        if (dest == null) return;
        creature.abstractAI.SetDestination(dest.Value);
    }

    private WorldCoordinate FindWanderCoordinate()
    {
        float bearingWander = Mathf.Sin(Random.value * Mathf.PI * 2f);
        _idleBearing += Mathf.RoundToInt(bearingWander * idleWanderMod);
        
        if (_idleBearing < 0) _idleBearing = Mathf.FloorToInt(360 + _idleBearing % 360);
        else if (_idleBearing >= 360) _idleBearing = Mathf.FloorToInt(_idleBearing % 360);
        
        IntVector2 relativeDest = IntVector2.FromVector2(Custom.DegToVec(_idleBearing) * 25f);
        WorldCoordinate dest = WorldCoordinate.AddIntVector(creature.pos, relativeDest);

        int retries = wanderDirRetries;
        while (creature.Room.realizedRoom.aimap.getAItile(dest).acc == AItile.Accessibility.Solid)
        {
            if (retries <= 0)
            {
                _lost = true;
                break;
            }

            IntVector2 iv = relativeDest;

            for (int i = 5; i >= 0; i--)
            {
                iv = IntVector2.FromVector2(iv.ToVector2() * (i / 5f));
                
                IntVector2 newRelativeDest =
                    IntVector2.FromVector2(Custom.RotateAroundOrigo(iv.ToVector2(), 360f / (wanderDirRetries + 1)));
                dest = WorldCoordinate.AddIntVector(creature.pos, newRelativeDest);

                if (creature.Room.realizedRoom.aimap.getAItile(dest).acc == AItile.Accessibility.Solid)
                    break;
            }
            
            retries--;
        }

        return dest;
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
    private const float idleWanderMod = 15f;
    private const int wanderDirRetries = 5;
    private bool _lost;
    
    public Behaviour MyBehaviour { get; private set; }

    public AbstractCreature TargetCreature
    {
        get
        {
            if (utilityComparer.HighestUtilityModule() == preyTracker)
                return preyTracker.currentPrey.critRep.representedCreature;
            if (utilityComparer.HighestUtilityModule() == threatTracker)
                return threatTracker.mostThreateningCreature.representedCreature;
            return null;
        }
    }


    public enum Behaviour
    {
        Idle,
        Flee,
        Hunt,
        EscapeRain,
        Injured
    }


    private class SmallEelTrackerState : RelationshipTracker.TrackedCreatureState
    {
    }
    
}
