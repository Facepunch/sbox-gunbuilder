using System.Text.Json.Serialization;

/// <summary>
/// An ammo source.
/// </summary>
public interface IAmmoSource
{
	public IEnumerable<Bullet> Pop( int amount = 1 );
}

public sealed class WeaponMagazine : Component, IAmmoSource, Component.ITriggerListener
{
	/// <summary>
	/// The interactable that belongs to this weapon magazine. It'll always exist.
	/// </summary>
	[RequireComponent] public Interactable Interactable { get; set; }

	/// <summary>
	/// The attachable that belongs to this weapon magazine. It'll also always exist.
	/// </summary>
	[RequireComponent] public Attachable Attachable { get; set; }

	/// <summary>
	/// The model renderer for this mag.
	/// </summary>
	[Property] public SkinnedModelRenderer Renderer { get; set; }

	/// <summary>
	/// A list of the weapon's bullets.
	/// </summary>
	[Property] public Stack<Bullet> Bullets { get; set; }

	/// <summary>
	/// Debugging
	/// </summary>
	[Property, JsonIgnore] public int BulletCount => Bullets.Count;

	/// <summary>
	/// What's the ammo capacity for this gun?
	/// </summary>
	[Property] public int BulletCapacity { get; set; } = 30;

	/// <summary>
	/// How many bullets by default?
	/// </summary>
	[Property] public int DefaultBulletCapacity { get; set; } = 0;

	/// <summary>
	/// Which bullet caliber fits in here?
	/// </summary>
	[Property] public BulletCaliber Caliber { get; set; }

	/// <summary>
	/// Mainly for development - but negates needing ammo in the magazine to fire a gun.
	/// </summary>
	[Property] public bool IsInfiniteAmmo { get; set; } = false;

	[Property] public int AmmoBodygroup { get; set; } = 0;

	/// <summary>
	/// Called when inserting a bullet manually into the mag.
	/// </summary>
	[Property] public SoundEvent OnChamberSound { get; set; }

	/// <summary>
	/// Does this magazine have any ammo?
	/// </summary>
	public bool HasAmmo
	{
		get => Bullets.Count > 0;
	}

	[Property, Group( "Bullet Setup" )] public Bullet BulletPrefab { get; set; }

	protected override void OnStart()
	{
		if ( BulletPrefab is null ) return;

		// Push the bullet capacity into the gun
		for ( int i = 0; i < DefaultBulletCapacity; i++ )
		{
			Push( BulletPrefab with { IsSpent = false } );
		}
	}

	void UpdateMesh()
	{
		Renderer?.SetBodyGroup( AmmoBodygroup, Bullets.Count > 0 ? 0 : 1 );
	}

	/// <summary>
	/// Adds X bullet(s) to the top of the pile.
	/// </summary>
	/// <param name="bullets"></param>
	public int Push( params Bullet[] bullets )
	{
		// Can't add more bullets if full!
		if ( Bullets.Count >= BulletCapacity ) return 0;

		int pushed = 0;
		foreach ( var bullet in bullets )
		{
			if ( bullet.Caliber == Caliber )
			{
				pushed++;
				Bullets.Push( bullet );
			}
		}

		if ( pushed > 0 ) UpdateMesh();

		return pushed;
	}

	/// <summary>
	/// Pops X bullet(s) out of the magazine and returns them.
	/// </summary>
	/// <param name="amount"></param>
	/// <returns></returns>
	public IEnumerable<Bullet> Pop( int amount = 1 )
	{
		var popped = new List<Bullet>();

		for ( int i = 0; i < amount; i++ )
		{
			if ( Bullets.TryPop( out var bullet ) )
			{
				popped.Add( bullet );
			}
			else break;
		}

		if ( amount > 0 ) UpdateMesh();

		return popped;
	}

	/// <summary>
	/// Checks to see if this magazine is compatible with the bullet.
	/// </summary>
	/// <param name="bullet"></param>
	/// <returns></returns>
	public bool IsCompatibleBullet( Bullet bullet )
	{
		return Caliber == bullet.Caliber;
	}

	void ITriggerListener.OnTriggerEnter( Sandbox.Collider other )
	{
		if ( other.GameObject.Root.Components.Get<BulletComponent>() is { } bulletComponent )
		{
			if ( Push( bulletComponent.Bullet ) > 0 )
			{
				var interactable = bulletComponent.Components.Get<Interactable>();
				if ( interactable is not null )
				{
					interactable.ClearAllInteractions();
				}

				if ( OnChamberSound is not null )
					Sound.Play( OnChamberSound, WorldPosition );

				other.GameObject.Root.Destroy();
			}
		}
	}
}
