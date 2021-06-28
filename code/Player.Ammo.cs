using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class FPSPlayer
{
	[Net]
	public List<int> Ammo { get; set; } = new(); // todo - networkable dictionaries

	public Dictionary<AmmoType, int> AmmoLimits = new()
	{
		{ AmmoType.None, -1 },
		{ AmmoType.Buckshot, 2 * 50 },
		{ AmmoType.Crossbow, 20 },
		{ AmmoType.Pistol, 200 }
	};

	public void ClearAmmo()
	{
		Ammo.Clear();
	}

	public int AmmoCount( AmmoType type )
	{
		var iType = (int)type;
		if ( Ammo == null ) return 0;
		if ( Ammo.Count <= iType ) return 0;

		return Ammo[(int)type];
	}

	public bool SetAmmo( AmmoType type, int amount )
	{
		var iType = (int)type;
		if ( !Host.IsServer ) return false;
		if ( Ammo == null ) return false;

		while ( Ammo.Count <= iType )
		{
			Ammo.Add( 0 );
		}

		if ( amount > AmmoLimits[type] )
			amount = AmmoLimits[type];
		Ammo[(int)type] = amount;
		return true;
	}

	public bool GiveAmmo( AmmoType type )
	{
		return GiveAmmo( type, AmmoLimits[type] );
	}

	public bool GiveAmmo( AmmoType type, int amount )
	{
		if ( !Host.IsServer ) return false;
		if ( Ammo == null ) return false;

		var newAmount = AmmoCount( type ) + amount;
		if ( newAmount > AmmoLimits[type] )
			newAmount = AmmoLimits[type];
		SetAmmo( type, newAmount );
		return true;
	}

	public int TakeAmmo( AmmoType type, int amount )
	{
		if ( Ammo == null ) return 0;

		var available = AmmoCount( type );
		amount = Math.Min( available, amount );

		SetAmmo( type, available - amount );
		return amount;
	}
}

public enum AmmoType
{
	None,
	Pistol,
	Buckshot,
	Crossbow
}
