using Sandbox.VR;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json.Serialization;

public partial class Hand : Component, Component.ITriggerListener
{
	[Property] GameObject ModelGameObject { get; set; }
	[Property] GameObject DummyGameObject { get; set; }

	/// <summary>
	/// Which objects are we hovering our hand over right now?
	/// This doesn't mean HOLDING, it means hovered.
	/// </summary>
	HashSet<IGrabbable> HoveredDirectory = new();

	/// <summary>
	/// The current grab point that this hand is holding. This means the grip is down, and we're actively holding an interactable.
	/// </summary>
	public IGrabbable CurrentGrabbable { get; set; }

	/// <summary>
	/// The input deadzone, so holding ( flDeadzone * 100 ) percent of the grip down means we've got the grip / trigger down.
	/// </summary>
	const float flDeadzone = 0.25f;

	/// <summary>
	/// What the velocity?
	/// </summary>
	public Vector3 Velocity { get; set; }

	/// <summary>
	/// Debug output
	/// </summary>
	[Group( "Data" ), Property, JsonIgnore]
	Component Current => CurrentGrabbable as Component;

	/// <summary>
	/// Is the hand grip down?
	/// </summary>
	/// <returns></returns>
	public bool IsGripDown()
	{
		// For debugging purposes
		if ( !Game.IsRunningInVR ) return Input.Down( "Attack2" );

		var src = GetController();
		if ( src is null ) return false;

		return src.Grip.Value > flDeadzone;
	}

	/// <summary>
	/// Is the hand trigger down?
	/// </summary>
	/// <returns></returns>
	public bool IsTriggerDown()
	{
		// For debugging purposes
		if ( !Game.IsRunningInVR ) return Input.Down( "Attack1" );

		var src = GetController();
		if ( src is null ) return false;

		return src.Trigger.Value > flDeadzone;
	}

	public VRController GetController()
	{
		return HandSource == HandSources.Left ? Input.VR?.LeftHand : Input.VR?.RightHand;
	}

	public bool IsDown( GrabInputType inputType )
	{
		return inputType switch
		{
			GrabInputType.Hover => true,
			GrabInputType.Grip => IsGripDown(),
			GrabInputType.Trigger => IsTriggerDown(),
			_ => false
		};
	}

	/// <summary>
	/// Try to grab a grab point.
	/// </summary>
	/// <param name="grabbable"></param>
	void StartGrabbing( IGrabbable grabbable )
	{
		// If we're already grabbing this thing, don't bother.
		if ( CurrentGrabbable == grabbable ) return;

		// Input type match
		if ( !IsDown( grabbable.GrabInput ) ) return;

		// Only if we succeed to interact with the interactable, take hold of the object.
		if ( grabbable.StartGrabbing( this ) )
		{
			// We did it! Respond?
			CurrentGrabbable = grabbable;
		}
	}

	/// <summary>
	/// Stop grabbing something.
	/// </summary>
	public void StopGrabbing()
	{
		// If we can release the object (which can fail!), clear the current grab point.
		if ( CurrentGrabbable?.StopGrabbing( this ) ?? false )
		{
			HoveredDirectory.Remove( CurrentGrabbable );
			CurrentGrabbable = null;
		}
	}

	private void UpdateTrackedLocation()
	{
		var controller = GetController();
		if ( controller is null ) return;

		var tx = controller.Transform;
		// Bit of a hack, but the alyx controllers have a weird origin that I don't care for.
		tx = tx.Add( Vector3.Forward * -2f, false );

		var prevPosition = Transform.World.Position;
		Transform.World = tx;

		var newPosition = Transform.World.Position;
		Velocity = newPosition - prevPosition;
	}

	protected IGrabbable LookForHandAligned( float size = 1f )
	{
		// We're gonna look for an item that the hand is kinda looking at

		var rot = WorldRotation *= Rotation.From( 45, 0, 0 );
		var fwd = rot.Forward;

		var tr = Scene.Trace.Ray( WorldPosition, WorldPosition + fwd * 100000f )
		.IgnoreGameObject( GameObject )
		.Size( size )
		.Run();

		if ( tr.Hit )
		{
			if ( tr.GameObject.Root.Components.Get<BaseInteractable>( FindMode.EnabledInSelfAndDescendants ) is { } interactable )
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.LineSphere( new Sphere( tr.GameObject.WorldPosition, 2 ), 8 );
				var grabbable = interactable.GrabbableDirectory.FirstOrDefault();
				return grabbable;
			}
		}

		return null;
	}


	protected IGrabbable LookForHeadAligned( float size = 1f )
	{
		// We're gonna look for an item that the hand is kinda looking at

		var rot = Scene.Camera.WorldRotation;
		var fwd = rot.Forward;

		var tr = Scene.Trace.Ray( Scene.Camera.WorldPosition, Scene.Camera.WorldPosition + fwd * 100000f )
		.IgnoreGameObject( GameObject )
		.Size( size )
		.Run();

		if ( tr.Hit )
		{
			if ( tr.GameObject.Root.Components.Get<BaseInteractable>( FindMode.EnabledInSelfAndDescendants ) is { } interactable )
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.LineSphere( new Sphere( tr.GameObject.WorldPosition, 2 ), 8 );
				var grabbable = interactable.GrabbableDirectory.FirstOrDefault();
				return grabbable;
			}
		}

		return null;
	}

	protected IGrabbable TryFindGrabbable()
	{
		// Are we already holding one?
		if ( CurrentGrabbable.IsValid() ) return CurrentGrabbable;

		// First we'll look in the directory of stuff close to us
		var directory = HoveredDirectory.Where( x => x.IsValid() )
			.OrderBy( x => x.GameObject.WorldPosition.Distance( WorldPosition ) );

		var item = directory.FirstOrDefault();
		if ( item.IsValid() ) return item;

		// Look for something we're directly aiming at with our hand
		item = LookForHandAligned( 1f );
		if ( item.IsValid() ) return item;

		// Look for something we're directly aiming at with our head
		item = LookForHeadAligned( 1f );
		if ( item.IsValid() ) return item;

		// Go outside the box, find something that's a bit off but good enough
		item = LookForHeadAligned( 10f );
		if ( item.IsValid() ) return item;

		// same with the hands
		item = LookForHandAligned( 5f );
		if ( item.IsValid() ) return item;

		return null;
	}

	protected override void OnUpdate()
	{
		UpdateTrackedLocation();
		UpdatePose();

		if ( IsProxy ) return;

		if ( CurrentGrabbable.IsValid() )
		{
			// Auto-detach for hover input type
			if ( CurrentGrabbable.GrabInput == GrabInputType.Hover )
			{
				// Detach!
				if ( CurrentGrabbable.GameObject.WorldPosition.Distance( WorldPosition ) > 3f )
				{
					StopGrabbing();
					return;
				}
			}
		}

		var grabbable = TryFindGrabbable();
		if ( grabbable.IsValid() && IsDown( grabbable.GrabInput ) )
		{
			StartGrabbing( grabbable );
		}
		else
		{
			StopGrabbing();
		}
	}

	[Property] public SkinnedModelRenderer Model { get; set; }

	/// <summary>
	/// Is this hand holding something right now?
	/// </summary>
	/// <returns></returns>
	internal bool IsHolding()
	{
		return CurrentGrabbable.IsValid();
	}
	
	/// <summary>
	/// Attaches the hand model to a grab point.
	/// </summary>
	/// <param name="gameObject"></param>
	internal void AttachModelTo( GameObject gameObject )
	{
		DummyGameObject.SetParent( gameObject, false );
	}

	/// <summary>
	/// Detaches the hand model from the grab point, puts it back on our hand.
	/// </summary>
	internal void DetachModelFrom()
	{
		DummyGameObject.SetParent( ModelGameObject, false );
	}

	// Not sure what purpose this'll really serve soon.
	internal Vector3 GetHoldPosition( IGrabbable grabbable )
	{
		var src = ModelGameObject.WorldPosition;
		return src;
	}

	// Not sure what purpose this'll really serve soon.
	internal Rotation GetHoldRotation( IGrabbable grabbable )
	{
		return ModelGameObject.WorldRotation;
	}

	/// <summary>
	/// Called when we overlap with another trigger in the world.
	/// </summary>
	/// <param name="other"></param>
	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		// Did we find a grab point that'll become eligible to grab?
		if ( other.Components.Get<IGrabbable>( FindMode.EnabledInSelf ) is { } grabbable )
		{
			HoveredDirectory.Add( grabbable );
		}
	}

	void ITriggerListener.OnTriggerExit( Collider other )
	{
		// Did we find a grab point that'll become eligible to grab?
		if ( other.Components.Get<IGrabbable>( FindMode.EnabledInSelf ) is { } grabbable )
		{
			if ( HoveredDirectory.Contains( grabbable ) )
			{
				HoveredDirectory.Remove( grabbable );
			}
		}
	}
}
