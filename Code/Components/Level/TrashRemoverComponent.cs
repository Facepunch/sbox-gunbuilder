/// <summary>
/// A quick and nasty component that removes interactables if they collide with it.
/// </summary>
public sealed class TrashRemoverComponent : Component, Component.ICollisionListener
{
	/// <summary>
	/// What sound should we play when something is deleted?
	/// </summary>
	[Property] public SoundEvent TrashSound { get; set; }

	/// <summary>
	/// Plays the trash sound.
	/// </summary>
	void PlaySound()
	{
		if ( TrashSound is not null )
		{
			Sound.Play( TrashSound, WorldPosition );
		}
	}

	/// <summary>
	/// Called when something collides with us.
	/// </summary>
	/// <param name="other"></param>
	void ICollisionListener.OnCollisionStart( Sandbox.Collision other )
	{
		if ( other.Other.GameObject.Root.Components.Get<Interactable>() is { } interactable )
		{
			// Clear all interactions
			interactable.ClearAllInteractions();
			
			// Delete the object
			interactable.GameObject.Destroy();

			PlaySound();
		}
	}
}
