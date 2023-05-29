﻿using Sandbox;
using System;
using System.Collections.Generic;

namespace TFS2;

public partial class Cart
{
	bool wasMoving = false;
	bool wasRolling = false;
	protected virtual void DoMovement()
	{
		if ( !CanMove() )
        {
            StopAllSound();
            return;
        }

		bool isRolling = false;
		if ( CanPush() )
		{
			wasRolling = false;

			float maxSpeed = 0f;
			switch ( GetCapRate() )
			{
				case 0:
					break;
				case 1:
					maxSpeed = Level1Speed;
					break;
				case 2:
					maxSpeed = Level2Speed;
					break;
				default:
					maxSpeed = Level3Speed;
					break;
			}

			CurrentSpeed += Acceleration * Time.Delta;
			CurrentSpeed = MathF.Min( maxSpeed, CurrentSpeed );

			TimeSincePush = 0f;
		}
		else if ( CanRollforward() )
		{
			isRolling = true;
			wasRolling = true;
			float maxSpeed = Level3Speed;

			CurrentSpeed += Acceleration * Time.Delta;
			CurrentSpeed = MathF.Min( maxSpeed, CurrentSpeed );
		}
		else if ( CanRollback() )
		{
			isRolling = true;
			wasRolling = true;

			float minSpeed = -BackwardsSpeed;
			CurrentSpeed -= Acceleration * Time.Delta;
			CurrentSpeed = MathF.Max( minSpeed, CurrentSpeed );
		}
		else
		{
			wasRolling = false;

			float minSpeed = 0f;
			CurrentSpeed -= Acceleration * Time.Delta;
			CurrentSpeed = MathF.Max( minSpeed, CurrentSpeed );
		}

        SetRollingSoundState(isRolling);

		if ( isRolling && !wasRolling )
			OnStartRolling.Fire( this );
		else if ( !isRolling && wasRolling )
			OnStopRolling.Fire( this );

		if ( CurrentSpeed < 0f && CurrentIndex == 0f && CurrentFraction <= 0f )
		{
			CurrentSpeed = 0f;
			return;
		}

		if ( CurrentSpeed == 0f )
		{
			if ( wasMoving )
			{
				StopMoveSounds();
				wasMoving = false;
			}

			return;
		}

		bool isMovingReverse = CurrentSpeed < 0f;

		if ( isMovingReverse )
		{
			float remainingDistance = MathF.Abs( CurrentSpeed ) * Time.Delta;
			while ( remainingDistance > 0.01f )
			{
				remainingDistance = DoMoveBackwards( remainingDistance );
			}
		}
		else
		{
			float remainingDistance = CurrentSpeed * Time.Delta;
			while ( remainingDistance > 0.01f )
			{
				remainingDistance = DoMoveForwards( remainingDistance );
			}
		}

		var newpos = GetPathPosition();
		Vector3 dir = isMovingReverse ? newpos - Position : Position - newpos;

		/*
		// Push player away from the cart
		var forwardTrace = GetEntitiesBBox( dir + Vector3.Down * 4 );
		foreach(var ent in forwardTrace)
		{
			if ( ent == this ) return;

			Log.Info( $"Traced ent: {ent}" );
			ent.Velocity += dir.WithZ(0);
		}
		*/

		Velocity = dir;
		Rotation = Rotation.LookAt( dir );
		Position = newpos;

        if ( !wasMoving )
		{
			StartMoveSounds();
			wasMoving = true;
		}
		else
		{
			MoveSounds();
		}
	}

	protected virtual float DoMoveForwards( float distance )
	{
		float usedDistance = MathF.Min( distance * Time.Delta, distance );
		CurrentFraction += GetSpeedFraction( usedDistance );

		if ( CurrentFraction >= 1 )
		{
			CurrentIndex++;
			CurrentFraction -= 1;
			OnNodeChanged( CurrentNode );

			if ( NextNode == null )
			{
				var nextPath = CurrentNode.GetNextPath();
				if ( nextPath != null )
				{
					Path = nextPath;
					ResetPath();
					return -2;
				}

				FinishMoving();
				return -1;
			}
		}

		return distance - usedDistance;
	}

	protected virtual float DoMoveBackwards( float distance )
	{
		distance = MathF.Abs( distance );
		float usedDistance = MathF.Min( distance * Time.Delta, distance );
		CurrentFraction -= MathF.Abs( GetSpeedFraction( usedDistance ) );

		if ( CurrentFraction < 0 )
		{
			if ( PreviousNode == null )
			{
				StopMoveSounds();
				return 0;
			}

			CurrentIndex--;
			CurrentFraction += 1;
			OnNodeChanged( CurrentNode );
		}

		return distance - usedDistance;
	}

	public virtual IEnumerable<Entity> GetEntitiesBBox(Vector3 offset)
	{
		var bounds = CollisionBounds;
		bounds = bounds.Translate( Position );
		bounds = bounds.Translate( offset );

		return Entity.FindInBox( bounds );
	}

	protected virtual void FinishMoving()
	{
		IsAtEnd = true;
		StopMoveSounds();
		OnReachEnd.Fire( this );
		Log.Info( $"Reached end at: {CurrentIndex + 1}" );
		if ( EnablePhysicsAtEnd )
			EnablePhysics();
	}
}
