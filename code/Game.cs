using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This is the heart of the gamemode. It's responsible
/// for creating the player and stuff.
/// </summary>
public partial class SMLSGame : Game
{
	public enum State
	{
		WaitingForPlayers,
		InGame,
		Finished
	}

	[ConVar.Replicated("smls_gamestate")]
	public static State GameStateConVar { get { return (Game.Current as SMLSGame).GameState; } }

	public State GameState {
		set
		{
			switch (value)
			{
				case State.WaitingForPlayers:
					ResetPlayerList();
					break;
				case State.InGame:
					StartGame();
					break;
				default:
					Log.Warning( $"GameState {value} is not implemented!" );
					break;
			}

			gameState = value;
			// FIXME: meh, doesn't work with panel
			//Event.Run( "smls.gamestatechange" );
			StartScreen.OnGameStateChange(gameState);
		}
		get
		{
			return gameState;
		}
	}

	protected State gameState = State.WaitingForPlayers;
	protected Dictionary<int, bool> playerList;

	public SMLSGame()
	{

		//
		// Create the HUD entity. This is always broadcast to all clients
		// and will create the UI panels clientside. It's accessible 
		// globally via Hud.Current, so we don't need to store it.
		//
		if ( IsServer )
		{
			ResetPlayerList();
			new SMLSHud();
		}
	}

	public override void PostLevelLoaded()
	{
		base.PostLevelLoaded();

		ItemRespawn.Init();
	}

	public override void ClientJoined( Client cl )
	{
		base.ClientJoined( cl );
		Log.Info( $"{cl.NetworkIdent}" );

		playerList.Add( cl.NetworkIdent, false );
		StartScreen.AddClientRPC( cl.NetworkIdent, cl.SteamId, cl.Name );

		var player = new SMLSPlayer();
		cl.Pawn = player;
		player.Respawn();
	}

	public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
	{
		base.ClientDisconnect( cl, reason );

		playerList.Remove( cl.NetworkIdent );
		StartScreen.RemoveClientRPC( cl.NetworkIdent );
	}

	[ServerCmd("smls_ready")]
	public static void SetReadinessCmd(int networkIdent)
	{
		var game = Current as SMLSGame;
		if ( game.GameState != State.WaitingForPlayers )
			return;

		game.SetReadiness( networkIdent );
	}

	public void SetReadiness(int networkIdent)
	{
		Host.AssertServer();

		if ( !playerList.ContainsKey( networkIdent ) )
			return;

		playerList[networkIdent] = true;
		StartScreen.SetReadinessRPC( networkIdent );
		Log.Info( $"{networkIdent} is ready!" );

		bool isEveryoneReady = true;
		foreach (var p in playerList)
		{
			if (!p.Value)
			{
				isEveryoneReady = false;
				break;
			}
		}
		if (isEveryoneReady)
		{
			GameState = State.InGame;
		}
	}
	
	protected void ResetPlayerList()
	{
		Host.AssertServer();

		playerList = new Dictionary<int, bool>();
		foreach ( var p in Client.All )
		{
			playerList.Add( p.NetworkIdent, false );
		}
	}

	public void StartGame()
	{
		Host.AssertServer();

		Log.Error( "TODO: start the game" );
	}
}
