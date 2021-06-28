using Sandbox;
using System;
using System.Linq;

public partial class SMLSBasePlayer : Player
{
	public enum PlayerTeam
	{
		Spectator,
		TeamAlpha,
		TeamBeta,
		FFA
	}
	[Net]
	public PlayerTeam Team { get; set; }

	public bool IsFriendly()
	{
		var localP = Local.Pawn as SMLSBasePlayer;
		if ( localP == null )
			return false;
		if ( localP.Team == PlayerTeam.FFA )
			return localP == this;
		else
			return localP.Team == Team;
	}
}

public partial class FPSPlayer : SMLSBasePlayer
{

	TimeSince timeSinceDropped;

	public bool SupressPickupNotices { get; private set; }

	public FPSPlayer()
	{
		Inventory = new DmInventory( this );
	}

	public override void Respawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );
		//SetModel( "models/smls.player.vmdl" );

		using ( Prediction.Off() )
		{
			Controller = new SMLSController();
			Animator = new TPoseAnimator();
			Camera = new InGameCamera()
			{
				FieldOfView = 120.0f
			};
		}

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		ClearAmmo();

		SupressPickupNotices = true;

		Inventory.Add( new Pistol(), true );
		Inventory.Add( new Shotgun() );
		Inventory.Add( new SMG() );
		Inventory.Add( new Crossbow() );
		Inventory.Add( new SniperRifle() );

		GiveAmmo( AmmoType.Pistol );
		GiveAmmo( AmmoType.Buckshot );
		GiveAmmo( AmmoType.Crossbow );

		SupressPickupNotices = false;
		Health = 100;

		base.Respawn();

		// TODO: color the player depending on his color
		using ( Prediction.Off() )
		{
			ColorPlayerRPC( this );
		}
	}

	public override void OnKilled()
	{
		base.OnKilled();

		Inventory.DropActive();
		Inventory.DeleteContents();

		BecomeRagdollOnClient( LastDamage.Force, GetHitboxBone( LastDamage.HitboxIndex ) );

		Controller = null;
		Camera = new SpectateRagdollCamera();

		EnableAllCollisions = false;
		EnableDrawing = false;
	}


	public override void Simulate( Client cl )
	{
		//if ( cl.NetworkIdent == 1 )
		//	return;

		base.Simulate( cl );

		//
		// Input requested a weapon switch
		//
		if ( Input.ActiveChild != null )
		{
			ActiveChild = Input.ActiveChild;
		}

		if ( LifeState != LifeState.Alive )
			return;

		TickPlayerUse();

		if ( Input.Pressed( InputButton.View ) )
		{
			if ( Camera is ThirdPersonCamera )
			{
				Camera = new InGameCamera();
			}
			else
			{
				Camera = new ThirdPersonCamera();
			}
		}

		if ( Input.Pressed( InputButton.Drop ) )
		{
			var dropped = Inventory.DropActive();
			if ( dropped != null )
			{
				if ( dropped.PhysicsGroup != null )
				{
					dropped.PhysicsGroup.Velocity = Velocity + (EyeRot.Forward + EyeRot.Up) * 300;
				}

				timeSinceDropped = 0;
				SwitchToBestWeapon();
			}
		}

		SimulateActiveChild( cl, ActiveChild );

		//
		// If the current weapon is out of ammo and we last fired it over half a second ago
		// lets try to switch to a better wepaon
		//
		if ( ActiveChild is BaseDmWeapon weapon && !weapon.IsUsable() && weapon.TimeSincePrimaryAttack > 0.5f && weapon.TimeSinceSecondaryAttack > 0.5f )
		{
			SwitchToBestWeapon();
		}
	}

	public void SwitchToBestWeapon()
	{
		var best = Children.Select( x => x as BaseDmWeapon )
			.Where( x => x.IsValid() && x.IsUsable() )
			.OrderByDescending( x => x.BucketWeight )
			.FirstOrDefault();

		if ( best == null ) return;

		ActiveChild = best;
	}

	public override void StartTouch( Entity other )
	{
		if ( timeSinceDropped < 1 ) return;

		base.StartTouch( other );
	}

	public override void PostCameraSetup( ref CameraSetup setup )
	{
		base.PostCameraSetup( ref setup );
	}

	DamageInfo LastDamage;

	public override void TakeDamage( DamageInfo info )
	{
		LastDamage = info;

		// hack - hitbox 0 is head
		// we should be able to get this from somewhere
		if ( info.HitboxIndex == 0 )
		{
			info.Damage *= 2.0f;
		}

		base.TakeDamage( info );

		if ( info.Attacker is FPSPlayer attacker && attacker != this )
		{
			// Note - sending this only to the attacker!
			attacker.DidDamage( To.Single( attacker ), info.Position, info.Damage, Health.LerpInverse( 100, 0 ) );

			TookDamage( To.Single( this ), info.Weapon.IsValid() ? info.Weapon.Position : info.Attacker.Position );
		}
	}

	[ClientRpc]
	public void DidDamage( Vector3 pos, float amount, float healthinv )
	{
		Sound.FromScreen( "dm.ui_attacker" )
			.SetPitch( 1 + healthinv * 1 );

		HitIndicator.Current?.OnHit( pos, amount );
	}

	[ClientRpc]
	public void TookDamage( Vector3 pos )
	{
		//DebugOverlay.Sphere( pos, 5.0f, Color.Red, false, 50.0f );

		DamageIndicator.Current?.OnHit( pos );
	}

	[ClientRpc]
	public void ColorPlayerRPC( SMLSBasePlayer e )
	{
		e.RenderColor = Settings.Instance.GetPlayerColor( e );
	}

	[ClientCmd( "smls_newcolor_rgb" )]
	public static void NewColorRGB( int r, int g, int b ) // FIXME: doesn't work with bytes
	{
		using ( Prediction.Off() )
		{
			(Local.Pawn as SMLSBasePlayer).RenderColor = new Color32( (byte)r, (byte)g, (byte)b );
		}
	}

	[ClientCmd( "smls_newcolor_hsv" )]
	public static void NewColorHSV( float h, float s, float v )
	{
		using ( Prediction.Off() )
		{
			(Local.Pawn as SMLSBasePlayer).RenderColor = new Etc.HSV( h, s, v ).ToColor();
		}
	}
}
