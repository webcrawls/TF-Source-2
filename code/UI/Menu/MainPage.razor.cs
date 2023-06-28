﻿using Sandbox;
using Sandbox.Localization;
using Sandbox.Menu;
using Sandbox.UI;
using System.Threading.Tasks;
using TFS2.UI;

namespace TFS2.Menu;

public partial class MainPage : Panel
{
	ServerList List;
	public MainPage()
	{
		BindClass( "ingame", () => Game.InGame );
	}
	public void OnClickResumeGame()
	{
		Game.Menu.HideMenu();
	}
	public async Task OnClickCreateGame()
	{
		await Game.Menu.CreateLobbyAsync();
	}
	public void OnClickViewLobby()
	{
		this.Navigate( "/lobby" );
	}
	public void OnClickLoadout()
	{
		this.Navigate( "/loadout" );
	}
	public void OnClickJoinGame()
	{
		List.SetClass( "visible", !List.HasClass( "visible" ) );
	}

	public void OnClickSettings()
	{
		this.Navigate( "/settings" );
	}

	public void OnClickQuit()
	{
		if(Game.InGame)
		{
			MenuOverlay.Open<QuitDialog>();
		}
		else
		{
			Game.Menu.Close();
		}
	}

	public void OnClickClassSelection()
	{
		if ( !Game.InGame ) return;

		HudOverlay.Open<ClassSelection>();
	}

	public void OnClickTeamSelection()
	{
		if ( !Game.InGame ) return;

		HudOverlay.Open<TeamSelection>();
	}

	public void OnClickBlog()
	{
		MenuOverlay.Open<BlogView>();
	}
}
