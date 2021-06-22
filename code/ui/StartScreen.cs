using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;

public partial class StartScreen : Panel
{
	public static StartScreen Instance;

	protected Panel userPanel;
	protected Dictionary<int, Panel> players = new Dictionary<int, Panel>();

	public StartScreen() : base()
	{
		Instance = this;

		StyleSheet.Load( "/ui/StartScreen.scss" );

		var p = Add.Panel( "sscontainer" );

		var l = p.AddChild<Label>( "header" );
		l.Text = "test";

		userPanel = p.Add.Panel( "userpanel" );

		var b = p.Add.Button( "imready", () => { ConsoleSystem.Run( "smls_ready", Local.Client.NetworkIdent ); } );
		b.Text = "I'm ready!"; // TODO: change the color of this button when user is ready
	}

	[Obsolete]
	public void AddClient( int networkIdent, ulong steamId, string name )
	{
		if ( players.ContainsKey( networkIdent ) )
			return;

		var p = userPanel.Add.Panel( "user" );
		p.Add.Image( $"avatar:{steamId}", "pfp" );
		p.Add.Label( $"{name}" );

		players[networkIdent] = p;
	}

	[Obsolete]
	public void RemoveClient( int networkIdent )
	{
		if ( !players.ContainsKey( networkIdent ) )
			return;

		players[networkIdent].Delete();
		players.Remove( networkIdent );
	}

	[Obsolete]
	public void SetReadyClient( int networkIdent, bool isReady )
	{
		if ( !players.ContainsKey( networkIdent ) )
			return;

		players[networkIdent].SetClass( "ready", isReady );
	}

	[ClientRpc]
	public static void AddClientRPC( int networkIdent, ulong steamId, string name )
	{
		Instance.AddClient( networkIdent, steamId, name );
	}

	[ClientRpc]
	public static void RemoveClientRPC( int networkIdent )
	{
		Instance.RemoveClient( networkIdent );
	}

	[ClientRpc]
	public static void SetReadinessRPC( int networkIdent )
	{
		Instance.SetReadyClient( networkIdent, true );
	}

	[ClientRpc]
	public static void OnGameStateChange( SMLSGame.State gameState )
	{
		Instance.SetClass( "hide", gameState != SMLSGame.State.WaitingForPlayers );
	}

	[ClientRpc]
	public static void SetInitialUsersRPC( int[] networkIdents, ulong[] steamIds, string[] names )
	{
		for (var i = 0; i < networkIdents.Length; i++ )
		{
			Instance.AddClient( networkIdents[i], steamIds[i], names[i] );
		}
	}
}
