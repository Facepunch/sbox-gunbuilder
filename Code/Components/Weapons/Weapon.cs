public partial class Weapon : Interactable
{
	/// <summary>
	/// Where do the bullets come from?
	/// </summary>
	[Property, Group( "Setup" )] public GameObject MuzzleGameObject { get; set; }

	/// <summary>
	/// Rounds per minute that this weapon can fire.
	/// </summary>
	[Property, Group( "Stats" )] public float RPM { get; set; }

	/// <summary>
	/// How far can this bullet travel?
	/// </summary>
	[Property, Group( "Stats" )] public float MaxRange { get; set; } = 100000;

	/// <summary>
	/// How big (radius) is this bullet?
	/// </summary>
	[Property, Group( "Stats" )] public float BulletSize { get; set; } = 4;

	/// <summary>
	/// What sound should we play when shooting the gun?
	/// </summary>
	[Property, Group( "Sounds" )] public SoundEvent ShootSound { get; set; }

	/// <summary>
	/// What sound should we play when trying to dry fire?
	/// </summary>
	[Property, Group( "Sounds" )] public SoundEvent DryFireSound { get; set; }

	[Property, Group( "Recoil" )] public RangedFloat VerticalRecoil { get; set; } = new( 300, 600 );
	[Property, Group( "Recoil" )] public RangedFloat HorizontalRecoil { get; set; } = new( -200, 200 );
	[Property, Group( "Recoil" )] public float BaseRecoilDecay { get; set; } = 3f;

	/// <summary>
	/// The weapon's chamber. If it has one. It probably.
	/// </summary>
	[Property] public WeaponChamber Chamber { get; set; }

	/// <summary>
	/// The current weapon magazine
	/// </summary>
	public WeaponMagazine Magazine { get; private set; }

	/// <summary>
	/// Optional: Uses slide release system if we have one.
	/// </summary>
	[Property] public SlideReleaseSystem SlideReleaseSystem { get; set; }

	/// <summary>
	/// How long has it been since we've shot this gun?
	/// </summary>
	[Sync] public TimeSince TimeSinceShoot { get; private set; }

	/// <summary>
	/// When we're trying to dry fire, we want to delay playing any sound
	/// </summary>
	[Sync] public TimeUntil TimeUntilNextDryFire { get; private set; }

	/// <summary>
	/// Called when an attachable is added to this weapon. (From <see cref="Interactable"/>)
	/// </summary>
	/// <param name="attachable"></param>
	/// <param name="attachmentPoint"></param>
	protected override void OnAttachableAdded( Attachable attachable, AttachmentPoint attachmentPoint )
	{
		// When an attachable is added to this weapon (an attachment point on it), check to see if it's a magazine
		// and specify to the weapon that this is its ammo reserve.
		if ( attachable.Components.Get<WeaponMagazine>() is { } magazine )
		{
			Magazine = magazine;
		}
	}

	/// <summary>
	/// Called when an attachble is removed from this weapon. From (<see cref="Interactable"/>)
	/// </summary>
	/// <param name="attachable"></param>
	/// <param name="attachmentPoint"></param>
	protected override void OnAttachableRemoved( Attachable attachable, AttachmentPoint attachmentPoint )
	{
		// If the magazine gets detached, we don't have an ammo reserve anymore.
		if ( attachable.Components.Get<WeaponMagazine>() is { } magazine )
		{
			Magazine = null;
		}
	}

	private Vector2 CurrentRecoilAmount { get; set; }

	protected override Rotation GetHoldRotation()
	{
		var original = base.GetHoldRotation();

		original = original * Rotation.From( -CurrentRecoilAmount.y * Time.Delta, CurrentRecoilAmount.x * Time.Delta, 0 );

		return original;
	}

	/// <summary>
	/// If there's a magazine, we'll try to detach it from its attachment point.
	/// </summary>
	protected void TryDetachMagazine()
	{
		Magazine?.Attachable?.Detach();
	}

	public bool TryFeedFromMagazine()
	{
		var ejectedBullets = Chamber.Eject();

		if ( ejectedBullets is not null )
		{
		// 	OnBulletEjected( ejectedBullets );
		}

		var magazine = Magazine;
		// Feed the weapon's chamber from its current magazine.
		if ( Chamber.Feed( magazine ) > 0 )
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// A conversion from RPM (rounds per minute) to a fire rate in seconds.
	/// </summary>
	/// <returns></returns>
	protected float RPMToSeconds()
	{
		return 60 / RPM;
	}

	/// <summary>
	/// The weapon's forward direction, originating normally from the muzzle, straight forward.
	/// </summary>
	protected virtual Ray WeaponRay => new Ray( MuzzleGameObject.WorldPosition, MuzzleGameObject.WorldRotation.Forward );

	/// <summary>
	/// Produces a trace that we can use for weapon shooting.
	/// This is designed in a way where we can override it for a shotgun, and supply multiple shots for pellets.
	/// </summary>
	/// <returns></returns>
	protected virtual IEnumerable<SceneTraceResult> GetShootTrace()
	{
		var tr = Scene.Trace.Ray( WeaponRay, MaxRange )
			.Size( BulletSize )
			.WithoutTags( "trigger" )
			.UseHitboxes()
			.Run();

		yield return tr;
	}

	/// <summary>
	/// Can we shoot this gun?
	/// </summary>
	/// <returns></returns>
	public bool CanShoot()
	{
		if ( SlideReleaseSystem?.IsPulled ?? false )
		{
			TryDryShoot();
			return false;
		}

		// Delay checks
		if ( TimeSinceShoot < RPMToSeconds() )
		{
			return false;
		}

		return true;
	}

	protected override void OnUpdate()
	{
		CurrentRecoilAmount = CurrentRecoilAmount.LerpTo( 0, Time.Delta * CalcRecoilDecay() );

		if ( PrimaryGrabPoint?.Hand is { } hand )
		{
			if ( hand.IsTriggerDown() )
			{
				TryShoot();
			}

			if ( hand.GetController().ButtonB.IsPressed )
			{
				TryDetachMagazine();
			}
		}
	}

	[Rpc.Broadcast]
	private void TryDryShoot()
	{
		if ( !TimeUntilNextDryFire ) return;

		Sound.Play( DryFireSound, WorldPosition );
		TimeUntilNextDryFire = 0.5f;
	}

	private Vector2 CalcRecoil()
	{
		return new Vector2()
		{
			y = Game.Random.Int( VerticalRecoil.Min.CeilToInt(), VerticalRecoil.Max.CeilToInt() ),
			x = Game.Random.Int( HorizontalRecoil.Min.CeilToInt(), HorizontalRecoil.Max.CeilToInt() )
		};
	}

	private float CalcRecoilDecay()
	{
		return BaseRecoilDecay;
	}

	private Bullet GetBullet()
	{
		if ( Chamber?.Eject() is { } bullet )
		{
			return bullet;
		}
		return null;
	}

	[Rpc.Broadcast]
	public void TryShoot()
	{
		if ( !CanShoot() ) return;

		TimeSinceShoot = 0;

		var bullet = GetBullet();
		if ( bullet is null )
		{
			TryDryShoot();
			return;
		}

		// Mark the bullet as spent.
		bullet.IsSpent = true;

		CurrentRecoilAmount += CalcRecoil();

		int count = 0;
		foreach ( var tr in GetShootTrace() )
		{
			foreach ( var damageable in tr.GameObject.Root.Components.GetAll<IDamageable>( FindMode.EnabledInSelfAndDescendants ) )
			{
				damageable.OnDamage( new DamageInfo()
				{
					Damage = 25, Position = tr.EndPosition
				} );
			}

			if ( tr.Hit )
				CreateImpactEffects( tr.GameObject, tr.Surface, tr.EndPosition, tr.Normal );

			DoShootEffects();
			DoTracer( tr.StartPosition, tr.EndPosition, tr.Distance, count );
			count++;
		}

		OnBulletEjected( bullet );

		// If we succeed to shoot, let's feed another bullet into the chamber from the mag.
		if ( !TryFeedFromMagazine() )
		{
			SlideReleaseSystem?.PullManually();
		}
	}


	[Property] public GameObject BulletEjectPrefab { get; set; }
	[Property] public GameObject EjectionPort { get; set; }

	protected void OnBulletEjected( Bullet bullet )
	{
		if ( bullet is null )
		{
			Log.Warning( "Tried to eject an invalid bullet?" );
			return;
		}

		var worldBullet = bullet.CreateInWorld( EjectionPort.WorldPosition, EjectionPort.WorldRotation );

		var rb = worldBullet.Components.Get<Rigidbody>( FindMode.EnabledInSelfAndDescendants );
		if ( rb.IsValid() )
		{
			rb.ApplyForce( EjectionPort.WorldRotation.Right * 1500000 );
			rb.ApplyForce( EjectionPort.WorldRotation.Up * Game.Random.Float( 100000, 200000 ) );

			//rb.AngularVelocity = EjectionPort.WorldRotation.Right * 10;
			rb.AngularVelocity = worldBullet.WorldRotation.Forward * 10;
		}
	}

	internal bool TryEjectFromChamber()
	{
		var ejectedBullet = Chamber.Eject();

		if ( ejectedBullet is not null )
		{
			OnBulletEjected( ejectedBullet );
		}

		return false;
	}
}
