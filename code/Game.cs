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

	[ConVar.Replicated( "smls_gamestate" )]
	public static State GameStateConVar { get { return (Game.Current as SMLSGame).GameState; } }

	public State GameState
	{
		set
		{
			if ( gameState != value )
			{

				switch ( value )
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

			}
			gameState = value;
			// FIXME: meh, doesn't work with panel
			//Event.Run( "smls.gamestatechange" );
			StartScreen.OnGameStateChange( gameState );
		}
		get
		{
			return gameState;
		}
	}

	public struct PlayerListDictItem
	{
		public Client Client;
		public bool IsReady;
	}

	protected State gameState = State.WaitingForPlayers;
	protected Dictionary<int, PlayerListDictItem> playerList = new Dictionary<int, PlayerListDictItem>();

	public SMLSGame()
	{
		//
		// Create the HUD entity. This is always broadcast to all clients
		// and will create the UI panels clientside. It's accessible 
		// globally via Hud.Current, so we don't need to store it.
		//
		if ( IsServer )
		{
			new SMLSHud();
			GameState = State.WaitingForPlayers;
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

		playerList.Add( cl.NetworkIdent, new PlayerListDictItem { Client = cl, IsReady = false } ); ;
		StartScreen.OnGameStateChange( To.Single(cl), GameState );
		{
			int[] networkIdents = new int[playerList.Count];
			ulong[] steamIds = new ulong[playerList.Count];
			string[] names = new string[playerList.Count];
			var i = 0;
			foreach ( var p in playerList.Values )
			{
				networkIdents[i] = p.Client.NetworkIdent;
				steamIds[i] = p.Client.SteamId;
				names[i] = p.Client.Name;
				i++;
			}

			StartScreen.SetInitialUsersRPC(networkIdents, steamIds, names);
		}
		StartScreen.AddClientRPC( cl.NetworkIdent, cl.SteamId, cl.Name );
	}

	public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
	{
		base.ClientDisconnect( cl, reason );

		playerList.Remove( cl.NetworkIdent );
		StartScreen.RemoveClientRPC( cl.NetworkIdent );
	}

	[ServerCmd( "smls_ready" )]
	public static void SetReadinessCmd( int networkIdent )
	{
		var game = Current as SMLSGame;
		if ( game.GameState != State.WaitingForPlayers )
			return;

		game.SetReadiness( networkIdent );
	}

	public void SetReadiness( int networkIdent )
	{
		Host.AssertServer();

		if ( !playerList.ContainsKey( networkIdent ) )
			return;

		playerList[networkIdent] = new PlayerListDictItem() { Client = playerList[networkIdent].Client, IsReady = true };
		StartScreen.SetReadinessRPC( networkIdent );
		Log.Info( $"{networkIdent} is ready!" );

		bool isEveryoneReady = true;
		foreach ( var p in playerList )
		{
			if ( !p.Value.IsReady )
			{
				isEveryoneReady = false;
				break;
			}
		}
		if ( isEveryoneReady )
		{
			GameState = State.InGame;
		}
	}

	protected void ResetPlayerList()
	{
		Host.AssertServer();

		playerList = new Dictionary<int, PlayerListDictItem>();
		foreach ( var p in Client.All )
		{
			playerList.Add( p.NetworkIdent, new PlayerListDictItem { Client = p, IsReady = false } );
		}
	}

	public void StartGame()
	{
		Host.AssertServer();

		foreach ( var client in Client.All )
		{
			var player = new SMLSPlayer();
			client.Pawn = player;
			player.Respawn();
		}

		Log.Error( "TODO: start the game" );
	}

	[AdminCmd("smls_forcestart")]
	public static void ForceStartGame()
	{
		(Game.Current as SMLSGame).GameState = State.InGame;
	}
}
