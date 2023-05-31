﻿using Sandbox;
using System;
using System.Collections.Generic;
using Amper.FPS;
using Editor;
using System.Linq;

namespace TFS2;

/// <summary>
/// Timer entity, used to count down the time for the purposes of a gamemode.
/// </summary>
[Library("tf_round_timer")]
[Title("Round Timer")]
[Description("Timer which ticks down is shown on the HUD")]
[Icon("timer")]
[Category( "Gameplay" )]
[EditorSprite( "materials/editor/tf_logic_setup_time.vmat" )]
[HammerEntity]
public partial class RoundTimer : Entity
{
	public new static List<RoundTimer> All { get; set; } = new();

	/// <summary>
	/// Maximum amount of time this timer can reach.
	/// </summary>
	[Property] public float MaxTimerLength { get; set; } = 600;
	/// <summary>
	/// Start counting down from this value.
	/// </summary>
	[Property] public float StartTime { get; set; } = 300;
	[Property, Net] public float SetupTime { get; set; } = 0;
	/// <summary>
	/// Automatically start this timer when it's created.
	/// </summary>
	[Property] public bool StartActive { get; set; } = true;
	/// <summary>
	/// If true, this timer will not be shown on the timer HUD.
	/// </summary>
	[Property, Net] public bool HideFromHUD { get; set; }
	/// <summary>
	/// Should this timer reset on round restart?
	/// </summary>
	[Property] public bool PlayAnnouncerVoicelines { get; set; } = true;
	[Property] public bool ResetOnRoundStart { get; set; } = true;
	[Net] public TFTeam OwnerTeam { get; set; }

	/// <summary>
	/// Defines if this timer is currently paused? Setting this to true will make timer freeze at it's current value.
	/// </summary>
	[Net] public bool Paused { get; set; }
	/// <summary>
	/// Count time relative to this value.
	/// </summary>
	[Net] public float AbsoluteTime { get; set; }
	/// <summary>
	/// The amount of time since the timer started counting.
	/// </summary>
	[Net] public TimeSince TimeSinceStartedCounting { get; set; }
	[Net] public bool InOvertime { get; set; }
	public bool HasSetup => SetupTime > 0;
	public bool InSetup { get; set; }
	public bool IsVisibleOnHUD => !HideFromHUD;
	public RoundTimer()
	{
		All.Add( this );
		EventDispatcher.Subscribe<RoundActiveEvent>( OnRoundStart, this );
	}

	public override void Spawn()
	{
		base.Spawn();

		SetTime( AbsoluteTime );
		Transmit = TransmitType.Always;
		TimeSinceStartedCounting = 0;
		Paused = true;
		if(HasSetup)
			TFGameRules.Current.HasSetup = true;
	}

	protected override void OnDestroy()
	{
		All.Remove( this );
		base.OnDestroy();
	}

	/// <summary>
	/// Start the timer.
	/// </summary>
	[Input]
	public void Start()
	{
		if ( !Game.IsServer )
			return;

		if ( Paused )
			OnStarted.Fire( this );

		InSetup = false;
		Paused = false;
		TimeSinceStartedCounting = 0;
		TFGameRules.Current.IsInSetup = All.Any(timer => timer.InSetup);
	}
	[Input]
	public void StartSetup()
	{
		if ( !Game.IsServer )
			return;

		if ( SetupTime <= 0 ) return;

		if ( Paused )
			OnSetupStarted.Fire( this );

		InSetup = true;
		Paused = false;
		TimeSinceStartedCounting = 0;
		SetTime(SetupTime);
		TFGameRules.Current.IsInSetup = true;
	}

	[Input]
	public void Restart()
	{
		if ( !Game.IsServer )
			return;

		OnRestarted.Fire( this );

		SetTime( StartTime );
		Start();
	}

	[Input]
	public void Pause()
	{
		if ( !Game.IsServer )
			return;

		if ( !Paused )
			OnPaused.Fire( this );

		AbsoluteTime = GetRemainingTime();
		Paused = true;
	}

	[Input]
	public void Resume()
	{
		if ( !Game.IsServer )
			return;

		if ( Paused )
			OnResume.Fire( this );

		Paused = false;
		TimeSinceStartedCounting = 0;
	}

	[Input]
	public float SetTime( float time )
	{
		if ( MaxTimerLength != 0 && time > MaxTimerLength ) time = MaxTimerLength;

		AbsoluteTime = time;
		lastSecond = AbsoluteTime.FloorToInt();

		return time;
	}

	[Input]
	public void AddTime( float time )
	{
		var addedTime = SetTime( AbsoluteTime + time );
		Log.Info( $"AddTime {addedTime}" );
		OnTimeAdded?.Invoke( addedTime );
	}

	public float GetRemainingTime()
	{
		if ( Paused ) return AbsoluteTime;
		return MathF.Max( 0, AbsoluteTime - TimeSinceStartedCounting );
	}

	public string GetTimeString()
	{
		return TimeSpan.FromSeconds( GetRemainingTime() ).ToString( @"mm\:ss" );
	}

	public bool IsElapsed()
	{
		return GetRemainingTime() == 0;
	}

	int lastSecond = -1;

	[GameEvent.Tick.Server]
	protected virtual void Tick()
	{
		if ( Paused )
		{
			if(Game.IsServer && InOvertime && !CheckOvertime())
			{
				StopOvertime();
			}
			else
				return;
		}

		float timeLeft = GetRemainingTime();
		int second = timeLeft.FloorToInt();

		if(InSetup)
		{
			if ( second != lastSecond )
			{
				lastSecond = second;
			}

			if ( PlayAnnouncerVoicelines )
			{
				int secondsRemaining = GetRemainingTime().FloorToInt();
				PlayAnnouncerTimeVoiceLine( secondsRemaining, true );
			}

			if ( timeLeft == 0 )
			{
				SetTime( StartTime );
				Start();
				OnSetupEnded.Fire( this );

				SDKGame.PlaySoundToAll( "ambience.siren", SoundBroadcastChannel.Ambience );
				SDKGame.PlaySoundToAll( "announcer.round_start", SoundBroadcastChannel.Announcer );
			}
		}
		else
		{
			if ( second != lastSecond )
			{
				//
				// Outputs
				//
				switch ( second )
				{
					case 300: On5MinRemain.Fire( this ); break;
					case 240: On4MinRemain.Fire( this ); break;
					case 180: On3MinRemain.Fire( this ); break;
					case 120: On2MinRemain.Fire( this ); break;
					case 60: On1MinRemain.Fire( this ); break;
					case 30: On30SecRemain.Fire( this ); break;
					case 20: On20SecRemain.Fire( this ); break;
					case 10: On10SecRemain.Fire( this ); break;
					case 5: On5SecRemain.Fire( this ); break;
					case 4: On4SecRemain.Fire( this ); break;
					case 3: On3SecRemain.Fire( this ); break;
					case 2: On2SecRemain.Fire( this ); break;
					case 1: On1SecRemain.Fire( this ); break;
				}

				lastSecond = second;
			}

			if ( PlayAnnouncerVoicelines )
			{
				int secondsRemaining = GetRemainingTime().FloorToInt();
				PlayAnnouncerTimeVoiceLine( secondsRemaining );
			}

			if ( timeLeft == 0 )
			{
				if(CheckOvertime())
				{
					StartOvertime();
				}
				else
				{
					Pause();
					OnFinished.Fire( this );
				}
			}
		}
	}

	const string OVERTIME_SOUND = "announcer.overtime";
	public void StartOvertime()
	{
		// Dont call Pause() to avoid firing OnPaused output
		InOvertime = true;
		AbsoluteTime = GetRemainingTime();
		Paused = true;

		TFGameRules.PlaySoundToAll( OVERTIME_SOUND, SoundBroadcastChannel.Announcer );
	}
	public void StopOvertime()
	{
		// Dont call Resume() to avoid firing OnResumed output
		InOvertime = false;
		Paused = false;
		TimeSinceStartedCounting = 0;
	}
	public bool CheckOvertime()
	{
		return GetRemainingTime() < 1 && Entity.All.OfType<IRoundTimerBlocker>().Any( blocker => blocker.ShouldBlock() );
	}

	public void PlayAnnouncerTimeVoiceLine( int second, bool begins = false )
	{
		var time = "";
		switch ( second )
		{
			case 300: time = "5min"; break;
			case 60: time = "1min"; break;
			case 30: time = "30sec"; break;
			case 10: time = "10sec"; break;
			case 5: time = "5sec"; break;
			case 4: time = "4sec"; break;
			case 3: time = "3sec"; break;
			case 2: time = "2sec"; break;
			case 1: time = "1sec"; break;
		}

		// no time value
		if ( string.IsNullOrEmpty( time ) ) return;

		//
		// Compute announcer voice line
		//
		var sound = "announcer.";

		// beings or ends
		if ( begins ) sound += "begins.";
		else sound += "ends.";

		sound += time;

		SDKGame.PlaySoundToAll( sound, SoundBroadcastChannel.Announcer );
	}

	public virtual void OnRoundStart( RoundActiveEvent args )
	{
		if(HasSetup)
		{
			StartSetup();
			return;
		}

		if ( ResetOnRoundStart )
			SetTime( StartTime );
		if ( StartActive )
			Start();
	}
	public event Action<float> OnTimeAdded;
	protected Output On5MinRemain { get; set; }
	protected Output On4MinRemain { get; set; }
	protected Output On3MinRemain { get; set; }
	protected Output On2MinRemain { get; set; }
	protected Output On1MinRemain { get; set; }
	protected Output On30SecRemain { get; set; }
	protected Output On20SecRemain { get; set; }
	protected Output On10SecRemain { get; set; }
	protected Output On5SecRemain { get; set; }
	protected Output On4SecRemain { get; set; }
	protected Output On3SecRemain { get; set; }
	protected Output On2SecRemain { get; set; }
	protected Output On1SecRemain { get; set; }

	protected Output OnRestarted { get; set; }
	protected Output OnPaused { get; set; }
	protected Output OnResume { get; set; }
	protected Output OnStarted { get; set; }
	protected Output OnFinished { get; set; }
	protected Output OnSetupStarted { get; set; }
	protected Output OnSetupEnded { get; set; }
}
