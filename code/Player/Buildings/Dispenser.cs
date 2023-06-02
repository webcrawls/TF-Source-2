﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFS2;
[Library( "tf_building_dispenser" )]
[Title( "Dispenser" )]
[Category("Gameplay")]
public partial class Dispenser : TFBuilding
{
	protected virtual List<float> LevelHealing => new() { 10f, 15f, 20f };
	protected virtual List<float> LevelAmmo => new() { 0.2f, 0.3f, 0.4f };
	protected virtual List<int> LevelMetal => new() { 40, 50, 60 };
 	protected virtual Vector3 TriggerMins => new( -70, -70, 0 );
	protected virtual Vector3 TriggerMaxs => new( 70, 70, 50 );
	protected virtual int StartingMetal => 25;
	[Net] public DispenserZone Trigger { get; set; }
	public override void Spawn()
	{
		base.Spawn();

		Trigger = new();
		Trigger.SetParent(this);
		Trigger.StoredMetal = StartingMetal;
		Trigger.SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, TriggerMins, TriggerMaxs );
	}
	public override void SetOwner( TFPlayer owner )
	{
		if ( Game.IsClient ) return;

		base.SetOwner( owner );
		Trigger.Team = owner.Team;
	}
	public override void Tick()
	{
		if( Trigger.Enabled && (IsCarried || IsUpgrading || IsConstructing))
		{
			Trigger.Disable();
		}

		base.Tick();
	}
	public override void TickActive()
	{
		if ( !Trigger.Enabled )
			Trigger.Enable();
	}

	protected override void Debug()
	{
		base.Debug();
		DebugOverlay.Box( Trigger, Color.Yellow.Darken(0.2f) );

		Vector3 pos = Position + Vector3.Up * CollisionBounds.Maxs.z;
		DebugOverlay.Text( $"[DISPENSER]", pos, 13, Color.White );
		DebugOverlay.Text( $"= Trigger: {Trigger}", pos, 14, Color.Yellow );
	}

	public override void SetLevel( int level )
	{
		base.SetLevel( level );

		Trigger.HealingPerSecond = LevelHealing.ElementAtOrDefault( level-1 );
		Trigger.AmmoPercentagePerSecond = LevelAmmo.ElementAtOrDefault( level-1 );
		Trigger.MetalPerInterval = LevelMetal.ElementAtOrDefault( level-1 );
	}
}
