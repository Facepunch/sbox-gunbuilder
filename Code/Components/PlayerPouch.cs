/// <summary>
/// A pouch on the player. It can hold interactables.
/// </summary>
public partial class PlayerPouch : Component, Component.ITriggerListener, IGrabbable.IGrabListener
{
	/// <summary>
	/// The player in question.
	/// </summary>
	[Property] public Player Player { get; set; }

	/// <summary>
	/// The list of objects held in this pouch.
	/// </summary>
	[Property] public BaseInteractable HeldInteractable { get; set; }

	/// <summary>
	/// How thick is our radius? TODO: derive from collider
	/// </summary>
	[Property] public float PouchRadius { get; set; } = 4;

	/// <summary>
	/// How quick does the position lerp?
	/// </summary>
	[Property] public float LerpSpeed { get; set; } = 10f;

	/// <summary>
	/// Called when anything in the world has started being grabbed.
	/// </summary>
	/// <param name="interactable"></param>
	/// <param name="hand"></param>
	void IGrabbable.IGrabListener.OnGrabStart( BaseInteractable interactable, Hand hand )
	{
		if ( interactable == HeldInteractable )
		{
			HeldInteractable = null;
		}
	}

	/// <summary>
	/// Called when anything in the world has stopped being grabbed.
	/// </summary>
	/// <param name="interactable"></param>
	/// <param name="hand"></param>
	void IGrabbable.IGrabListener.OnGrabEnd( BaseInteractable interactable, Hand hand )
	{
		// Can't insert new.
		if ( HeldInteractable.IsValid() )
		{
			return;
		}

		// Did we drop something in the vicinity of the pouch?
		if ( interactable.WorldPosition.Distance( WorldPosition ) < PouchRadius )
		{
			HeldInteractable = interactable;
			HeldInteractable.FreezeMotion();
		}
	}

	protected override void OnUpdate()
	{
		if ( HeldInteractable.IsValid() )
		{
			HeldInteractable.WorldPosition = HeldInteractable.WorldPosition.LerpTo( WorldPosition, Time.Delta * LerpSpeed );
			HeldInteractable.WorldRotation = Rotation.Lerp( HeldInteractable.WorldRotation, WorldRotation, Time.Delta * LerpSpeed );
		}
	}
}
