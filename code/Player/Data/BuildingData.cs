﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFS2;
[GameResource( "TF:S2 Building Data", "tfbuild", "Team Fortress: Source 2 building definitions", Icon = "build_circle", IconBgColor = "#ff6861", IconFgColor = "#0e0e0e" )]
public class BuildingData : GameResource
{
	/// <summary>
	/// All registered Buildings.
	/// </summary>
	public static IReadOnlyList<BuildingData> All => _all;
	private static List<BuildingData> _all = new();
	public static BuildingData Get(string name) => _all.FirstOrDefault(building => building.ResourceName== name);

	/// <summary>
	/// Title of the weapon that will be displayed to the client.
	/// </summary>
	public string Title { get; set; }
	/// <summary>
	/// Engine entity classname.
	/// </summary>
	public string EngineClass { get; set; }
	public int BuildCost { get; set; } = 100;
	public float BuildTime { get; set; } = 10f;
	public int UpgradeCost { get; set; } = 200;
	public float UpgradeTime { get; set; } = 1.2f;
	public int MaxCount { get; set; } = 1;
	public List<BuildingLevelData> Levels { get; set; } = new();
	[HideInEditor]
	public int LevelCount => Levels.Count;
	[Category( "BBox" )]
	public Vector3 Mins { get; set; } = new( -20, -20, 0 );
	[Category( "BBox" )]
	public Vector3 Maxs { get; set; } = new( 20, 20, 55 );
	[Category( "BBox" )]
	public Vector3 PlacementMins { get; set; }
	[Category( "BBox" )]
	public Vector3 PlacementMaxs { get; set; }
	[HideInEditor]
	public BBox BBox => new( Mins, Maxs );

	[HideInEditor]
	public BBox PlacementBBox => PlacementMins != default ? new( PlacementMins, PlacementMaxs ) : BBox;
	/// <summary>
	/// The name of this building displayed in the UI
	/// </summary>
	[Title("UI Name")]
	[Category("UI")]
	public string UIName { get; set; }
	/// <summary>
	/// Should the UI panel for this building be bigger? (ex: sentry)
	/// </summary>
	[Category( "UI" )]
	public bool BigPanel { get; set; }
	/// <summary>
	/// Icon used for things like the PDA
	/// </summary>
	[Category( "UI" )]
	[ResourceType( "png" )]
	public string BlueprintIcon { get; set; }
	[Category("UI")]
	[ResourceType( "vmdl" )]
	public string BlueprintModel { get; set; }
	/// <summary>
	/// Toggle this bodygroup when placing building
	/// </summary>
	[Category( "UI" )]
	public string BlueprintBodyGroup { get; set; }
	[Category("Sounds")]
	[ResourceType("sound")]
	public string DestroyedSound { get; set; }
	[Category( "Sounds" )]
	[ResourceType( "sound" )]
	public string BuiltVO { get; set; }
	[Category( "Sounds" )]
	[ResourceType( "sound" )]
	public string DestroyedVO { get; set; }
	[Category( "Sounds" )]
	[ResourceType( "sound" )]
	public string SappedVO { get; set; }
	/// <summary>
	/// Creates an instance of this weapon.
	/// </summary>
	/// <returns></returns>
	public TFBuilding CreateInstance()
	{
		if ( string.IsNullOrEmpty( EngineClass ) )
			return null;

		if(Levels.Count == 0)
		{
			Log.Warning( $"Tried to create instance of data with no levels!" );
			return null;
		}

		var building = TypeLibrary.Create<TFBuilding>( EngineClass, false );
		if(building == null)
		{
			Log.Error( $"Tried to create building with invalid engine class {EngineClass}!" );
			return null;
		}
		building.Initialize( this );

		return building;
	}
	public string GetUIIcon(int level)
	{
		return Levels.Take(level).LastOrDefault( lvl => !string.IsNullOrEmpty( lvl.GameplayIcon ) ).GameplayIcon;
	}
	protected override void PostLoad()
	{
		if ( Levels.Count == 0 )
		{
			Log.Error( $"Tried to load building {this} with no levels!" );
			return;
		}

		Precache.Add( BlueprintModel );
		Precache.Add( BlueprintIcon );

		foreach ( var level in Levels )
		{
			Precache.Add( level.Model );
			Precache.Add( level.DeployModel );
		}

		// Add this asset to the registry.
		_all.Add( this );
	}
}

public struct BuildingLevelData
{
	public float MaxHealth { get; set; }
	/// <summary>
	/// Model used while the building is active at this level
	/// </summary>
	[ResourceType( "vmdl" )]
	public string Model { get; set; }
	/// <summary>
	/// Model used while this level is being constructed / upgraded to
	/// </summary>
	[ResourceType( "vmdl" )]
	public string DeployModel { get; set; }

	[ResourceType( "png" )]
	public string GameplayIcon { get; set; }
}