using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

partial class BaseDmWeapon : BaseWeapon, IRespawnableEntity
{
	public virtual AmmoType AmmoType => AmmoType.Pistol;
	public virtual int Bucket => 1;
	public virtual int BucketWeight => 100;
	public virtual int MaxAmmo => 100;
	public virtual float DeployTime => 0.6f;

	[Net, Predicted]
	public TimeSince TimeSinceDeployed { get; set; }


	public PickupTrigger PickupTrigger { get; protected set; }

	public int AvailableAmmo()
	{
		var owner = Owner as FPSPlayer;
		if ( owner == null ) return 0;
		return owner.AmmoCount( AmmoType );
	}

	public override void ActiveStart( Entity ent )
	{
		base.ActiveStart( ent );

		TimeSinceDeployed = 0;
	}

	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );

		PickupTrigger = new PickupTrigger();
		PickupTrigger.Parent = this;
		PickupTrigger.Position = Position;
	}

	public override void Simulate( Client owner )
	{
		if ( TimeSinceDeployed < DeployTime )
			return;

		base.Simulate( owner );
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		//
		// Tell the clients to play the shoot effects
		//
		ShootEffects();

		//
		// ShootBullet is coded in a way where we can have bullets pass through shit
		// or bounce off shit, in which case it'll return multiple results
		//
		foreach ( var tr in TraceBullet( Owner.EyePos, Owner.EyePos + Owner.EyeRot.Forward * 5000 ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			//
			// We turn predictiuon off for this, so aany exploding effects don't get culled etc
			//
			using ( Prediction.Off() )
			{
				var damage = DamageInfo.FromBullet( tr.EndPos, Owner.EyeRot.Forward * 100, 15 )
					.UsingTraceResult( tr )
					.WithAttacker( Owner )
					.WithWeapon( this );

				tr.Entity.TakeDamage( damage );
			}
		}
	}

	[ClientRpc]
	protected virtual void ShootEffects()
	{
		Host.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );

		if ( IsLocalPawn )
		{
			new Sandbox.ScreenShake.Perlin();
		}

		ViewModelEntity?.SetAnimBool( "fire", true );
		CrosshairPanel?.CreateEvent( "fire" );
	}

	/// <summary>
	/// Shoot a single bullet
	/// </summary>
	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize )
	{
		var forward = Owner.EyeRot.Forward;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
		forward = forward.Normal;

		//
		// ShootBullet is coded in a way where we can have bullets pass through shit
		// or bounce off shit, in which case it'll return multiple results
		//
		foreach ( var tr in TraceBullet( Owner.EyePos, Owner.EyePos + forward * 5000, bulletSize ) )
		{
			tr.Surface.DoBulletImpact( tr );

			if ( !IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			//
			// We turn predictiuon off for this, so any exploding effects don't get culled etc
			//
			using ( Prediction.Off() )
			{
				var damageInfo = DamageInfo.FromBullet( tr.EndPos, forward * 100 * force, damage )
					.UsingTraceResult( tr )
					.WithAttacker( Owner )
					.WithWeapon( this );

				tr.Entity.TakeDamage( damageInfo );
			}
		}
	}

	public bool TakeAmmo( int amount )
	{
		var owner = Owner as FPSPlayer;
		if ( owner == null || owner.AmmoCount( AmmoType ) < amount )
			return false;

		owner.TakeAmmo( AmmoType, amount );
		return true;
	}

	[ClientRpc]
	public virtual void DryFire()
	{
		// CLICK
	}

	public override void CreateViewModel()
	{
		Host.AssertClient();

		if ( string.IsNullOrEmpty( ViewModelPath ) )
			return;

		ViewModelEntity = new DmViewModel();
		ViewModelEntity.Position = Position;
		ViewModelEntity.Owner = Owner;
		ViewModelEntity.EnableViewmodelRendering = true;
		ViewModelEntity.SetModel( ViewModelPath );
	}

	public override void CreateHudElements()
	{
		if ( Local.Hud == null ) return;

		CrosshairPanel = new Crosshair();
		CrosshairPanel.Parent = Local.Hud;
		CrosshairPanel.AddClass( ClassInfo.Name );
	}

	public virtual bool IsUsable()
	{
		return AvailableAmmo() > 0;
	}

	public override void OnCarryStart( Entity carrier )
	{
		base.OnCarryStart( carrier );

		if ( PickupTrigger.IsValid() )
		{
			PickupTrigger.EnableTouch = false;
		}
	}

	public override void OnCarryDrop( Entity dropper )
	{
		base.OnCarryDrop( dropper );

		if ( PickupTrigger.IsValid() )
		{
			PickupTrigger.EnableTouch = true;
		}
	}

}
