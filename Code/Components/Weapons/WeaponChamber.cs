using System.Text.Json.Serialization;

public partial class WeaponChamber : Component, Component.ITriggerListener
{
	/// <summary>
	/// A list of bullets that are in this chamber.
	/// </summary>
	[Property] public Stack<Bullet> Chamber { get; set; } = new();

	/// <summary>
	/// Debugging
	/// </summary>
	[Property, JsonIgnore] public int ChamberCount => Chamber.Count;

	/// <summary>
	/// How many bullets can we put in the chamber? For something like a shotgun
	/// </summary>
	[Property] public int ChamberCapacity { get; set; } = 1;

	/// <summary>
	/// A reference to the bolt of this weapon. If it has one.
	/// </summary>
	[Property] public PointInteractable Bolt { get; set; }

	/// <summary>
	/// A sound to play when we manually chamber the gun.
	/// </summary>
	[Property] public SoundEvent OnChamberSound { get; set; }

	/// <summary>
	/// Are we able to add a bullet into the chamber?
	/// </summary>
	bool CanInsert
	{
		get => Chamber.Count < ChamberCapacity;
	}

	/// <summary>
	/// Try to eject a bullet from the chamber.
	/// </summary>
	/// <returns></returns>
	public Bullet Eject()
	{
		if ( Chamber.TryPop( out var bullet ) )
		{
			Log.Info( $"Ejected {bullet} from chamber" );
			return bullet;
		}
		return null;
	}

	/// <summary>
	/// Spend the first bullet in the weapon's chamber
	/// </summary>
	public void Spend()
	{
		if ( Chamber.FirstOrDefault() is { } bullet ) bullet.IsSpent = true;
	}

	/// <summary>
	/// Try to feed bullets from an ammo source
	/// </summary>
	/// <param name="src"></param>
	/// <returns>How many bullets we fed from this list</returns>
	public int Feed( IAmmoSource src )
	{
		if ( src is null ) return 0;

		int count = 0;

		while ( CanInsert )
		{
			if ( src.Pop() is { } bullets && bullets.FirstOrDefault() is { } bullet )
			{
				Log.Info( $"Pushed {bullet} into {this}" );
				Chamber.Push( bullet );
				count++;
			}
			else
			{
				break;
			}
		}

		return count;
	}

	void ITriggerListener.OnTriggerEnter( Sandbox.Collider other )
	{
		if ( other.GameObject.Root.Components.Get<BulletComponent>() is { } bulletComponent )
		{
			if ( !Bolt.IsValid() || ( Bolt.IsValid() && Bolt.CompletionValue.Equals( 1f ) ) )
			{
				if ( Feed( bulletComponent ) > 0 )
				{
					var interactable = bulletComponent.Components.Get<Interactable>();
					if ( interactable is not null )
					{
						interactable.ClearAllInteractions();
					}

					if ( OnChamberSound is not null )
						Sound.Play( OnChamberSound, WorldPosition );

					other.GameObject.Destroy();
				}
			}
		}
	}
}
