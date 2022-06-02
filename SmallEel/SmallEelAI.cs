using RWCustom;
using UnityEngine;

namespace SmallEel;

public class SmallEelAI : ArtificialIntelligence
{
    public SmallEelAI(AbstractCreature abstractCreature, World world) : base(abstractCreature, world)
    {
        _eel = abstractCreature.realizedCreature as SmallEel;
        _eel.ai = this;
        AddModule(new FishPather(this, world, abstractCreature));
        pathFinder.stepsPerFrame = 40;
        
        AddModule(new Tracker(this, 10, 10, -1, 0.5f, 5, 5, 20));

        AddModule(new RainTracker(this));
        AddModule(new DenFinder(this, abstractCreature));
        AddModule(new ThreatTracker(this, 3));
        AddModule(new PreyTracker(this, 5, 1f, 10f, 150f, 0.05f));
        AddModule(new InjuryTracker(this, 0.6f));
        
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
            if (highestModule is RainTracker)
                MyBehaviour = Behaviour.EscapeRain;
            else if (highestModule is ThreatTracker)
                MyBehaviour = Behaviour.Flee;
            else if (highestModule is PreyTracker)
                MyBehaviour = Behaviour.Hunt;
            else if (highestModule is InjuryTracker)
                MyBehaviour = Behaviour.Injured;
        }

        if (highestModuleVal < 0.1f)
            MyBehaviour = Behaviour.Idle;
        
        SmallEelPlugin.textManager.Write(creature.ID.ToString(), MyBehaviour, _eel.BaseColor);

        switch (MyBehaviour)
        {
            default:
            case Behaviour.Idle:
                creature.abstractAI.SetDestination(FindWanderCoordinate());
                break;
            case Behaviour.Flee:
            {
                WorldCoordinate dest = threatTracker.FleeTo(creature.pos, 1, 30, highestModuleVal > 0.4f);
                creature.abstractAI.SetDestination(dest);
                break;
            }
            case Behaviour.Hunt:
                creature.abstractAI.SetDestination(preyTracker.MostAttractivePrey.BestGuessForPosition());
                break;
            case Behaviour.EscapeRain:
            case Behaviour.Injured:
                if (denFinder.GetDenPosition() != null)
                    creature.abstractAI.SetDestination(denFinder.GetDenPosition().Value);
                break;
        }
    }

    private WorldCoordinate FindWanderCoordinate()
    {
        float bearingWander = Mathf.Sin(Random.value * Mathf.PI * 2f);
        _idleBearing += Mathf.RoundToInt(bearingWander * idleWanderMod);
        
        if (_idleBearing < 0) _idleBearing = Mathf.FloorToInt(360 + _idleBearing % 360);
        else if (_idleBearing >= 360) _idleBearing = Mathf.FloorToInt(_idleBearing % 360);
        
        Vector2 dir = Custom.DegToVec(_idleBearing);
        return WorldCoordinate.AddIntVector(creature.pos, IntVector2.FromVector2(dir * 5f));
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


    private readonly SmallEel _eel;
    private int _idleBearing;
    private const float idleWanderMod = 15f;
    
    public Behaviour MyBehaviour { get; private set; }
    
    public enum Behaviour
    {
        Idle,
        Flee,
        Hunt,
        EscapeRain,
        Injured
    }

}
