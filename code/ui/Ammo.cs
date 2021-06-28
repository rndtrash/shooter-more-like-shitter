
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

public class Ammo : Panel
{
	public Label Weapon;
	public Label Inventory;

	public Ammo()
	{
		Weapon = Add.Label( "100", "weapon" );
		Inventory = Add.Label( "100", "inventory" );
	}

	public override void Tick()
	{
		var player = Local.Pawn as FPSPlayer;
		if ( player == null ) return;

		var weapon = player.ActiveChild as BaseDmWeapon;
		SetClass( "active", weapon != null );

		if ( weapon == null ) return;

		Weapon.Text = $"{weapon.AvailableAmmo()}";

		Inventory.Text = $" / {player.AmmoLimits[weapon.AmmoType]}";
		Inventory.SetClass( "active", weapon.IsUsable() );
	}
}
