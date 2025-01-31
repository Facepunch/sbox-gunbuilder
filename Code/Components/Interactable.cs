/// <summary>
/// An interactable object. This can be anything really, a box, a ball, a grenade, a gun.
/// Used in combination with grab points, which dictates positions on the interactable where you can pick it up.
/// </summary>
public partial class Interactable : BaseInteractable
{
	/// <summary>
	/// A shorthand property to get the secondary grab point for this interactable.
	/// We have to be holding a primary grab point to use this grab point.
	/// </summary>
	public IGrabbable SecondaryGrabPoint => heldGrabbables.FirstOrDefault( x => x.Tags.Has( "secondary" ) );

	/// <summary>
	/// The scale of the interactable's mass. Higher means heavier.
	/// </summary>
	[Property] public float MassScale { get; set; } = 1.0f;

	/// <summary>
	/// Can we start interacting with this object?
	/// </summary>
	/// <param name="grabbable"></param>
	/// <param name="hand"></param>
	/// <returns></returns>
	protected override bool CanInteract( IGrabbable grabbable, Hand hand )
	{
		return base.CanInteract( grabbable, hand );
	}

	public void Attach( Attachable attachable, AttachmentPoint attachmentPoint )
	{
		attachable.OnAttach( attachmentPoint );

		OnAttachableAdded( attachable, attachmentPoint );
	}

	/// <summary>
	/// Detaches an attachable from a specific attachemnt point on this interactable.
	/// </summary>
	/// <param name="attachable"></param>
	/// <param name="attachmentPoint"></param>
	public void Detach( Attachable attachable, AttachmentPoint attachmentPoint )
	{
		attachable.OnDetach( attachmentPoint );

		OnAttachableRemoved( attachable, attachmentPoint );
	}

	protected virtual void OnAttachableRemoved( Attachable attachable, AttachmentPoint attachmentPoint )
	{
		//
	}

	protected virtual void OnAttachableAdded( Attachable attachable, AttachmentPoint attachmentPoint )
	{
		//
	}

	/// <summary>
	/// Called when we stop interacting with this object
	/// </summary>
	/// <param name="grabbable"></param>
	/// <param name="hand"></param>
	/// <returns></returns>
	protected override void OnStopInteract( IGrabbable grabbable, Hand hand )
	{
		base.OnStopInteract( grabbable, hand );

		// If we're not holding this interactable anymore, turn its motion back on.
		if ( heldGrabbables.Count <= 0 )
		{
			FreezeMotion( false );
		}
	}

	/// <summary>
	/// Called when we start interactring with this opbject
	/// </summary>
	/// <param name="grabbable"></param>
	/// <param name="hand"></param>
	protected override void OnInteract( IGrabbable grabbable, Hand hand )
	{
		base.OnInteract( grabbable, hand );
		FreezeMotion();
	}

	/// <summary>
	/// Calculates the hold rotation for this interactable.
	/// Can be overriden in child classes.
	/// </summary>
	/// <returns></returns>
	protected virtual Rotation GetHoldRotation()
	{
		var secondaryGrabPoint = heldGrabbables.FirstOrDefault( x => x.Tags.Has( "secondary" ) );
		var targetRotation = PrimaryGrabPoint.Hand.GetHoldRotation( PrimaryGrabPoint );

		// Are we holding from a secondary hold point as well?
		if ( SecondaryGrabPoint.IsValid() )
		{
			var direction = (SecondaryGrabPoint.Hand.WorldPosition - PrimaryGrabPoint.Hand.WorldPosition).Normal;
			targetRotation = Rotation.LookAt( direction, secondaryGrabPoint.Hand.WorldRotation.Up );
		}

		return targetRotation;
	}

	protected void PositionInteractable()
	{
		var velocity = Rigidbody.Velocity;
		var holdPos = PrimaryGrabPoint.Hand.GetHoldPosition( PrimaryGrabPoint );

		var grabPointPos = PrimaryGrabPoint.GameObject.WorldPosition;

		Vector3.SmoothDamp( Rigidbody.WorldPosition, holdPos + ( holdPos - grabPointPos ), ref velocity, CalcVelocityWeight(), Time.Delta );
		Rigidbody.Velocity = velocity;

		var angularVelocity = Rigidbody.AngularVelocity;
		Rotation.SmoothDamp( Rigidbody.WorldRotation, GetHoldRotation(), ref angularVelocity, CalcAngularVelocityWeight(), Time.Delta );
		Rigidbody.AngularVelocity = angularVelocity;
	}

	/// <summary>
	/// Calculate the velocity weight.
	/// </summary>
	/// <returns></returns>
	float CalcVelocityWeight()
	{
		// A higher weight means it'll take longer for this weapon to traverse to your hand.
		// This is in seconds.
		return 0.20f * MassScale;
	}

	/// <summary>
	/// Calculates the angular velocity weight.
	/// </summary>
	/// <returns></returns>
	float CalcAngularVelocityWeight()
	{
		return 0.15f * MassScale;
	}

	protected override void OnFixedUpdate()
	{
		if ( IsHeld )
		{
			PositionInteractable();
		}
	}

	/// <summary>
	/// Clears all interactions. This'll make the player release this weapon from their hands.
	/// </summary>
	internal void ClearAllInteractions()
	{
		var copy = new HashSet<IGrabbable>( heldGrabbables );
		foreach ( var point in copy )
		{
			StopInteract( point );
		}
	}
}
