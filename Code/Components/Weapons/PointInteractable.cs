using System.Text.Json.Serialization;

/// <summary>
/// A weapon part. This can be held by a player, and translated on a locked axis specified by the component.
/// </summary>
public partial class PointInteractable : BaseInteractable
{
	/// <summary>
	/// When the completion value changes.
	/// </summary>
	[Property, Group( "Events")] public Action<float, float> OnCompletionValue { get; set; }

	/// <summary>
	/// The model renderer for this interactable.
	/// </summary>
	[Property, Group( "Components" )] public SkinnedModelRenderer Renderer { get; set; }

	/// <summary>
	/// A sound to play when <see cref="CompletionValue"/> hits 0 from another value.
	/// </summary>
	[Property, Group( "Sounds" )] public SoundEvent ZeroSound { get; set; }

	/// <summary>
	/// A sound to play when <see cref="CompletionValue"/> hits 1 from another value.
	/// </summary>
	[Property, Group( "Sounds" )] public SoundEvent OneSound { get; set; }

	/// <summary>
	/// The bone we're targeting.
	/// </summary>
	[Property, Group( "Setup" )] public GameObject BoneGameObject { get; set; }

	/// <summary>
	/// The start gameobject. This'll be used for the hand grab point, so make sure it's accurate!
	/// </summary>
	[Property, Group( "Setup" )] public GameObject Start { get; set; }

	/// <summary>
	/// The ending gameobject. This'll be used for the hand grab point, so make sure it's accurate!
	/// </summary>
	[Property, Group( "Setup" )] public GameObject End { get; set; }

	/// <summary>
	/// Should we reset <see cref="CompletionValue"/> back to 0 if we release the interactable?
	/// </summary>
	[Property, Group( "Configuration" )] public bool ResetOnRelease { get; set; }

	[Property] public Angles AnglesAtOne { get; set; }

	/// <summary>
	/// Quick accessor for the hand.
	/// </summary>
	public Hand Hand => PrimaryGrabPoint?.Hand;

	/// <summary>
	/// We store an initial state for the bone, in local space - so we can maintain it.
	/// </summary>
	public Transform InitialBoneLocalTransform { get; set; }

	/// <summary>
	/// A stored calculation of the distance between the start gameobject and the end gameobject, so we can use it to calculate the completion value.
	/// </summary>
	public float DistanceBetweenStartAndEnd { get; set; }

	private float completionValue = 0f;
	/// <summary>
	/// The completion value of this interaction. 1 being fully fully done, 0 being default state
	/// </summary>
	[Property, JsonIgnore, Range( 0, 1 ), Group( "Debugging" )] public float CompletionValue
	{
		get => completionValue;
		set
		{
			var prev = completionValue;
			completionValue = value;

			if ( !completionValue.Equals( prev ) )
				OnCompletionValueChanged( prev, completionValue );
		}
	}

	/// <summary>
	/// Called when the completion value changes.
	/// </summary>
	/// <param name="prev"></param>
	/// <param name="value"></param>
	void OnCompletionValueChanged( float prev, float value )
	{
		if ( prev != 0 && value == 0 )
		{
			if ( ZeroSound is not null )
				Sound.Play( ZeroSound, WorldPosition );
		}

		if ( prev != 1 && value == 1 )
		{
			if ( OneSound is not null )
				Sound.Play( OneSound, WorldPosition );
		}

		OnCompletionValue?.Invoke( prev, value );
	}

	protected override void OnStart()
	{
		if ( Renderer.IsValid() )
		{
			// Store some initial data
			DistanceBetweenStartAndEnd = Vector3.DistanceBetween( End.WorldPosition, Start.WorldPosition );
			InitialBoneLocalTransform = BoneGameObject.Transform.Local;
		}

		if ( PrimaryGrabPoint is GrabPoint grabPoint )
			grabPoint.OnGrabEndEvent += OnGrabEnd;
	}

	protected override void OnDestroy()
	{
		if ( PrimaryGrabPoint.IsValid() && PrimaryGrabPoint is GrabPoint grabPoint )
		{
			grabPoint.OnGrabEndEvent -= OnGrabEnd;
		}
	}

	void OnGrabEnd()
	{
		// ResetOnRelease only counts if we haven't pulled all the way
		if ( ResetOnRelease )
		{
			if ( !CompletionValue.AlmostEqual( 1 ) )
			{
				CompletionValue = 0;
			}
		}
	}

	/// <summary>
	/// Calculates a local position for the bone, using all of our collected data.
	/// </summary>
	/// <returns></returns>
	protected Vector3 CalcLocalPosition()
	{
		var pos = InitialBoneLocalTransform.Position;
		var direction = (End.LocalPosition - Start.LocalPosition).Normal;
		pos += direction * (DistanceBetweenStartAndEnd * completionValue);

		return pos;
	}

	protected override void OnPreRender()
	{
		// If we're not holding the object, just maintain its local transform. This might not be required really,
		// but it's useful for debugging without VR.
		if ( !Hand.IsValid() )
		{
			BoneGameObject.Transform.Local = new Transform( CalcLocalPosition(), Rotation.Identity, 1 );
			if ( CompletionValue.AlmostEqual( 1f ) )
			{
				BoneGameObject.LocalRotation = AnglesAtOne.ToRotation();
			}
			return;
		}

		// Get the transform of the hand in local space to the Start GameObject.
		var localHand = Start.Transform.World.ToLocal( Hand.Transform.World );
		var handDiffFromStart = localHand.Position.x;
		var clamped = handDiffFromStart.Remap( 0, -DistanceBetweenStartAndEnd, 0, 1, true );
		
		// Make sure our completion value is up to date.
		CompletionValue = clamped;

		// Move the primary grab point. This should always be the hand that's interacting with the interactable.
		PrimaryGrabPoint.GameObject.WorldPosition = Vector3.Lerp( Start.WorldPosition, End.WorldPosition, clamped );
		// Move the bone!
		BoneGameObject.Transform.Local = new Transform( CalcLocalPosition(), Rotation.Identity, 1 );

		if ( CompletionValue.AlmostEqual( 1f ) )
		{
			BoneGameObject.LocalRotation = AnglesAtOne.ToRotation();
		}
	}
}
