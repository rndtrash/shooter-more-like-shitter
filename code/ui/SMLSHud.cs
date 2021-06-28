
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Threading.Tasks;

[Library]
public partial class SMLSHud : HudEntity<RootPanel>
{
	public static SMLSHud Instance;

	public SMLSHud()
	{
		if ( !IsClient )
			return;

		Instance = this;

		RootPanel.StyleSheet.Load( "/ui/SMLSHud.scss" );

		RootPanel.AddChild<Vitals>();
		RootPanel.AddChild<Ammo>();

		RootPanel.AddChild<NameTags>();
		RootPanel.AddChild<DamageIndicator>();
		RootPanel.AddChild<HitIndicator>();

		RootPanel.AddChild<InventoryBar>();
		RootPanel.AddChild<PickupFeed>();

		RootPanel.AddChild<ChatBox>();
		RootPanel.AddChild<KillFeed>();
		RootPanel.AddChild<Scoreboard>();
		RootPanel.AddChild<VoiceList>();

		RootPanel.AddChild<StartScreen>();
	}

	private void SwitchPanelsToState( SMLSGame.State gameState )
	{
		//
	}

	[ClientRpc]
	public void OnPlayerDied( string victim, string attacker = null )
	{
		Host.AssertClient();
	}

	[ClientRpc]
	public void ShowDeathScreen( string attackerName )
	{
		Host.AssertClient();
	}


	[ClientRpc]
	public static void OnGameStateChange( SMLSGame.State gameState )
	{
		Instance.SwitchPanelsToState( gameState );
	}
}
