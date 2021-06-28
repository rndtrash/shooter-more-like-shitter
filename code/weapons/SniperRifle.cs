
using Sandbox;

[Library( "dm_sniperrifle", Title = "Sniper Rifle" )]
partial class SniperRifle : BaseDmWeapon
{
	public override string ViewModelPath => "weapons/rust_crossbow/v_rust_crossbow.vmdl";
	public override AmmoType AmmoType => AmmoType.None;
	public override int Bucket => 4;

	public float Force = 1.5f;
	public float MaxCharge = 1.5f; // In seconds

	[Net]
	public bool Zoomed { get; set; }
	[Net]
	public bool IsCharging { get; set; } = false;
	[Net]
	public TimeSince Charge { get; set; } = 0.0f;
	[Net]
	public TimeSince Cooldown { get; set; } = 0.0f;
	[Net]
	public Sound ChargeSound { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "weapons/rust_crossbow/rust_crossbow.vmdl" );
	}

	public override bool CanPrimaryAttack()
	{
		return Cooldown > 0.0f && Input.Down( InputButton.Attack1 );
	}

	public override void AttackPrimary()
	{
		if ( !IsServer )
			return;
		using ( Prediction.Off() )
		{
			DebugOverlay.ScreenText( 0, $"{IsCharging} {Charge}" );

			var isAimingAtEnemy = IsAimingAtEnemy();
			
			if ( IsCharging )
			{
				if ( !isAimingAtEnemy )
				{
					SetCooldown();
				}
				if ( Charge >= MaxCharge )
				{
					Log.Info( $"Charged!" );
					//
					// Tell the clients to play the shoot effects
					//
					ShootEffects();

					//
					// Shoot the bullets
					//
					ShootBullet( 0.0f, Force, 100.0f, 10.0f );
					SetCooldown( false );
				}
			}
			else if (isAimingAtEnemy)
			{
				Log.Info( $"Began charging..." );
				Charge = 0;
				ChargeSound = PlaySound( "smls.sniper.charge" );
				IsCharging = true;
			}
		}
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		Zoomed = Input.Down( InputButton.Attack2 );

		DebugOverlay.ScreenText( 1, $"{Cooldown}" );
		if ( Cooldown > 0.0f && Input.Released( InputButton.Attack1 ) && IsCharging && TimeSincePrimaryAttack < MaxCharge )
		{
			Log.Info( "Didn't charge fully!" );
			SetCooldown();
		}
	}

	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		base.PostCameraSetup( ref camSetup );

		if ( Zoomed )
		{
			camSetup.FieldOfView = 20;
		}
	}

	public override void BuildInput( InputBuilder owner )
	{
		if ( Zoomed )
		{
			owner.ViewAngles = Angles.Lerp( owner.OriginalViewAngles, owner.ViewAngles, 0.2f );
		}
	}

	public override bool IsUsable()
	{
		return true;
	}

	public override void ActiveEnd( Entity ent, bool dropped )
	{
		base.ActiveEnd( ent, dropped );

		if ( IsCharging )
			SetCooldown();
	}

	protected void SetCooldown( bool punish = true )
	{
		Cooldown = -Charge * (punish ? 2.5f : 0.5f);
		if ( punish )
			ChargeSound.Stop();
		IsCharging = false;
	}

	protected bool IsAimingAtEnemy()
	{
		var tr = Trace.Ray( Owner.EyePos, Owner.EyePos + Owner.EyeRot.Forward * 5000 )
					.UseHitboxes()
					//.HitLayer( CollisionLayer.Water, !InWater )
					.Ignore( Owner )
					.Ignore( this )
					.Size( 10 )
					.Run();

		var hitE = tr.Entity as FPSPlayer;

		return tr.Hit && hitE != null && !hitE.IsFriendly();
	}
}
