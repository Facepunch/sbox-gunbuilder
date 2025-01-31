/// <summary>
/// A component that is responsible for allowing the player to "slap" the slide forward with their hand.
/// </summary>
public partial class SlideSlappingSystem : Component, Component.ITriggerListener
{
	/// <summary>
	/// The interactable.
	/// </summary>
	[Property] public PointInteractable PointInteractable { get; set; }

	/// <summary>
	/// The collider that we'll react to for slap events.
	/// </summary>
	[Property] public Collider Slapper { get; set; }

	/// <summary>
	/// The hand object that we're tracking.
	/// </summary>
	[Property] public GameObject Hand { get; set; }

	/// <summary>
	/// Is the slide pulled?
	/// </summary>
	public bool IsPulled => PointInteractable.CompletionValue.AlmostEqual( 1f );

	protected override void OnUpdate()
	{
		if ( !Hand.IsValid() ) return;

		// Slap!
		if ( IsPulled && IsCorrectDirectionAndSpeed() )
		{
			PointInteractable.CompletionValue = 0f;
			Hand = null;
			return;
		}

		// If the hand's too far away, it doesn't exist anymore.
		if ( Hand.WorldPosition.Distance( WorldPosition ) > 64f )
		{
			Hand = null;
		}
	}

	/// <summary>
	/// We want to make sure that we're slapping the slide at a certain speed, and a certain direction.
	/// </summary>
	/// <returns></returns>
	protected bool IsCorrectDirectionAndSpeed()
	{
		var velocity = Hand.Components.Get<Hand>( FindMode.EnabledInSelfAndDescendants )?.Velocity ?? 0f;
		Vector3 relativeVelocity = WorldRotation.Inverse * velocity;
		var relativeDir = relativeVelocity.Normal;
		var howForward = relativeDir.Dot( WorldRotation.Forward );

		// Expose these to variables?
		if ( velocity.Length < 1.2f ) return false;
		if ( howForward > 0.25f && howForward < 0.75f ) return true;
		
		return false;
	}

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( other.Tags.Has( "hands" ) )
		{
			Hand = other.GameObject.Root;
		}
	}
}
