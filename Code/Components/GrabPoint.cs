/// <summary>
/// A grab point. This has to be on an interactable somewhere. This'll be something like a handle on a gun.
/// Or just a point where we can hold something.
/// </summary>
public partial class GrabPoint : Component, IGrabbable
{
	[Property] public Action OnGrabStartEvent { get; set; }
	[Property] public Action OnGrabEndEvent { get; set; }

	/// <summary>
	/// The collider in question
	/// </summary>
	[Property] public Collider Collider { get; set; }

	/// <summary>
	/// The linked interactable
	/// </summary>
	[Property] public BaseInteractable Interactable { get; set; }

	/// <summary>
	/// The primary grab point
	/// </summary>
	[Property] public GrabPoint PrimaryGrabPoint { get; set; }

	/// <summary>
	/// The hand pose that we'll use when a hand is attached to this grab point.
	/// </summary>
	[Property, Title( "Hand Preset" )] public HandPreset Preset { get; set; } = HandPreset.GripWithoutIndexFinger;

	/// <summary>
	/// What's our grabbing input type?
	/// </summary>
	[Property] public GrabInputType GrabInput { get; set; } = GrabInputType.Grip;

	/// <summary>
	/// Is this a secondary grab point? Has to be linked to a primary grab point for it to work.
	/// </summary>
	public bool IsSecondaryGrabPoint => PrimaryGrabPoint.IsValid();

	/// <summary>
	/// Is this a primary grab point?
	/// </summary>
	public bool IsPrimaryGrabPoint => !IsSecondaryGrabPoint;

	/// <summary>
	/// What hand is this grab point currently being held by?
	/// Make this a method at some point so we can control access.
	/// </summary>
	public Hand HeldHand { get; set; }

	public void OnStartGrabbing( Hand hand )
	{
		OnGrabStartEvent?.Invoke();

		NotifyGrabListeners( hand, true );
	}

	public void OnStopGrabbing( Hand hand )
	{
		OnGrabEndEvent?.Invoke();

		NotifyGrabListeners( hand, false );
	}

	void NotifyGrabListeners( Hand hand, bool start )
	{
		foreach ( var comp in Scene.GetAllComponents<IGrabbable.IGrabListener>() )
		{
			if ( start )
			{
				comp.OnGrabStart( Interactable, hand );
			}
			else
			{
				comp.OnGrabEnd( Interactable, hand );
			}
		}
	}

	/// <summary>
	/// Can we stop grabbing?
	/// </summary>
	/// <param name="interactable"></param>
	/// <param name="hand"></param>
	/// <returns></returns>
	public bool CanStopGrabbing( BaseInteractable interactable, Hand hand )
	{
		return true;
	}

	/// <summary>
	/// Can we grab right now?
	/// </summary>
	/// <param name="interactable"></param>
	/// <param name="hand"></param>
	/// <returns></returns>
	public bool CanStartGrabbing( BaseInteractable interactable, Hand hand )
	{
		// TODO: Don't allow holding a secondary grab point if the primary grab point isn't being held already.
		// if ( IsSecondaryGrabPoint ) return PrimaryGrabPoint.IsBeingHeld;
		return true;
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.Cyan.WithAlpha( 0.2f );
		Gizmo.Draw.Model( "models/hands/human_hand_right.vmdl" );
	}

	/// <summary>
	/// Called normally by the hand, will update the pose of a hand to match the data we give it.
	/// </summary>
	/// <param name="hand"></param>
	public virtual void UpdateHandPose( Hand hand )
	{
		hand.ApplyHandPreset( Preset );
	}
}
