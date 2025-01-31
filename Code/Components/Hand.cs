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

	protected IGrabbable FindRemotePickupItem()
	{
		// We're gonna look for an item that the hand is kinda looking at

		var rot = WorldRotation *= Rotation.From( 45, 0, 0 );
		var fwd = rot.Forward;

		var tr = Scene.Trace.Ray( WorldPosition, WorldPosition + fwd * 100000f )
		.IgnoreGameObject( GameObject )
		.Run();

		Gizmo.Draw.Color = Color.Red;

		if ( tr.Hit )
		{
			if ( tr.GameObject.Root.Components.Get<BaseInteractable>( FindMode.EnabledInSelfAndDescendants ) is { } interactable )
			{
				Gizmo.Draw.Color = Color.Green;

				var grabbable = interactable.GrabbableDirectory.FirstOrDefault();
				return grabbable;
			}
		}

		Gizmo.Draw.Line( tr.StartPosition, tr.EndPosition );

		return null;
	}

	protected IGrabbable TryFindGrabbable()
	{
		// Are we already holding one?
		if ( CurrentGrabbable.IsValid() ) return CurrentGrabbable;


		// First we'll look in the directory of stuff close to us
		var directory = HoveredDirectory.Where( x => x.IsValid() )
			.OrderBy( x => x.GameObject.WorldPosition.Distance( WorldPosition ) );

		if ( directory.FirstOrDefault() is {IsValid: true } item )
		{
			return item;
		}

		if ( FindRemotePickupItem() is {  IsValid: true } lookItem )
		{
			return lookItem;
		}


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

	void UpdateRemotePickup()
	{
		var att = Model.GetAttachment( "ui_pointer" ) ?? default;

		var tr = Scene.Trace.Ray( att.Position, att.Position + att.Forward * 100000f )
			.IgnoreGameObject( GameObject )
			.Size( 10f )
			.Run();
		

		Gizmo.Draw.Color = Color.Red;

		if ( tr.Hit )
		{
			if ( tr.GameObject.Root.Components.Get<BaseInteractable>( FindMode.EnabledInSelfAndDescendants ) is { } interactable )
			{
				var grabbable = interactable.GrabbableDirectory.FirstOrDefault();
				if ( IsDown( grabbable.GrabInput ) && interactable.Interact( this, grabbable ) )
				{
					// We did it! Respond?
				}
			}
		}
		Gizmo.Draw.Line( tr.StartPosition, tr.EndPosition );
	}

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
