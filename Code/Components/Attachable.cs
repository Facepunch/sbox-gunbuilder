/// <summary>
/// An attachable. This is something that can attach TO an attachment point.
/// Example: A key into a door.
/// Example: A magazine into a weapon.
/// </summary>
public sealed class Attachable : Component
{
	/// <summary>
	/// What interactable object does this attachable belong to?
	/// </summary>
	[Property] public Interactable Interactable { get; set; }

	/// <summary>
	/// We need a rigidbody for an attachable, since it'll have a collider, and physics.
	/// </summary>
	[Property] public Rigidbody Rigidbody { get; set; }

	/// <summary>
	/// The collider in question.
	/// </summary>
	[Property] public Collider Collider { get; set; }

	/// <summary>
	/// A sound to play when we attach to a valid AttachmentPoint
	/// </summary>
	[Property] public SoundEvent AttachSound { get; set; }

	/// <summary>
	/// A sound to play when we detach from a valid AttachmentPoint
	/// </summary>
	[Property] public SoundEvent DetachSound { get; set; }

	/// <summary>
	/// The attachment point that this attachable is connected to right now.
	/// </summary>
	AttachmentPoint CurrentAttachmentPoint { get; set; }

	/// <summary>
	/// Is this attached to something already?
	/// </summary>
	public bool IsAttached => CurrentAttachmentPoint.IsValid();

	/// <summary>
	/// Called when we attach to an attachment point
	/// </summary>
	/// <param name="attachmentPoint"></param>
	internal void OnAttach( AttachmentPoint attachmentPoint )
	{
		CurrentAttachmentPoint = attachmentPoint;

		// Clear all interactions since we don't want the player to hold the item anymore.
		Interactable.ClearAllInteractions();

		// Disable motion on our Rigidbody, since we're going to be controlled by an attachment point now.
		Rigidbody.MotionEnabled = false;

		Tags.Set( "attached", true );

		if ( AttachSound is not null )
			Sound.Play( AttachSound, WorldPosition );
	}

	/// <summary>
	/// Try to detach from our current attachemnt point
	/// </summary>
	internal void Detach()
	{
		if ( CurrentAttachmentPoint.TryDetach() )
		{
			Rigidbody.Velocity = 0;
			Rigidbody.AngularVelocity = 0;
		}
	}

	/// <summary>
	/// Called when we detach from an attachment point.
	/// </summary>
	/// <param name="attachmentPoint"></param>
	internal void OnDetach( AttachmentPoint attachmentPoint )
	{
		Tags.Set( "attached", false );

		if ( DetachSound is not null )
			Sound.Play( DetachSound, WorldPosition );

		CurrentAttachmentPoint = null;
	}
}
