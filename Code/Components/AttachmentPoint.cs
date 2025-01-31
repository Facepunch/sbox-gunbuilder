using Sandbox;

public partial class AttachmentPoint : Component, Component.ExecuteInEditor, Component.ITriggerListener, Component.ICollisionListener
{
	/// <summary>
	/// The attachment point's interactable. They'll always have one.
	/// Example: A weapon's attachment point, of a magazine chamber.
	/// </summary>
	[Property] public Interactable Interactable { get; set; }

	/// <summary>
	/// Editor: The model that'll show up in the editor as a guide.
	/// </summary>
	[Property, Group( "Editor" )] public Model GuideModel { get; set; }

	/// <summary>
	/// The tags that will be accepted when trying to attach something to this attachment point.
	/// TODO: in the future, have a virtual method, and an Action that can be hooked into for this.
	/// </summary>
	[Property] public TagSet AcceptedTags { get; set; } = new TagSet();

	/// <summary>
	/// Do we have an attachable attached to this attachment point? What a fucking mouthful.
	/// </summary>
	public Attachable CurrentAttachable { get; private set; }

	/// <summary>
	/// How long has it been since we attached/detached something to this attachment point?
	/// </summary>
	TimeSince TimeSinceAttachChanged = 1;

	/// <summary>
	/// The delay between attaching/detaching.
	/// </summary>
	const float AttachmentDelay = 0.4f;

	protected override void DrawGizmos()
	{
		if ( GuideModel is not null )
		{
			Gizmo.Draw.Color = Color.Orange.WithAlpha( 0.4f );
			Gizmo.Draw.Model( GuideModel );
		}
	}

	/// <summary>
	/// Can we attach "attachable" to this attachment point?
	/// </summary>
	/// <param name="attachable"></param>
	/// <returns></returns>
	protected bool CanAttach( Attachable attachable )
	{
		// Artificial delay.
		if ( TimeSinceAttachChanged < AttachmentDelay ) return false;

		// We can only attach if the attachable is facing the same way as our attachment point.
		// Good for stuff like magazines.
		var attachableFwd = attachable.WorldRotation.Forward;
		var thisFwd = WorldRotation.Forward;
		var dot = attachableFwd.Dot( thisFwd );
		if ( dot > 0.8f ) return true;

		return false;
	}

	protected bool CanDetach()
	{
		if ( !CurrentAttachable.IsValid() ) return false;

		// Artificial delay.
		if ( TimeSinceAttachChanged < AttachmentDelay ) return false;

		return true;
	}

	/// <summary>
	/// Tries to detach the current attachable from this attachment point.
	/// </summary>
	public bool TryDetach()
	{
		if ( !CanDetach() ) return false;

		// TODO: make it so the interactable can say no
		Interactable.Detach( CurrentAttachable, this );

		Log.Info( $"Detached our current attachable {CurrentAttachable}" );

		TimeSinceAttachChanged = 0;
		CurrentAttachable = null;

		return true;
	}

	protected override void OnUpdate()
	{
		// TODO: Find out a better way to handle this. This kinda sucks.
		if ( CurrentAttachable.IsValid() )
		{
			CurrentAttachable.WorldPosition = GameObject.WorldPosition;
			CurrentAttachable.WorldRotation = GameObject.WorldRotation;
		}
	}

	/// <summary>
	/// Tries to attach an attachable to this attachment point. Can fail.
	/// </summary>
	/// <param name="attachable"></param>
	public bool TryAttach( Attachable attachable )
	{
		if ( attachable.IsAttached ) return false;

		Log.Info( $"> Try attaching {this} to {attachable}" );

		if ( !CanAttach( attachable ) ) return false;

		// Try to detach what we have already
		if ( CurrentAttachable.IsValid() && !TryDetach() ) return false;

		// TODO: make it so the interactable can say no
		Interactable.Attach( attachable, this );

		TimeSinceAttachChanged = 0;
		CurrentAttachable = attachable;

		return false;
	}

	/// <summary>
	/// A shorthand method that searches a <see cref="GameObject"/> for an <see cref="Attachable"/> and runs <see cref="TryAttach(Attachable)"/>
	/// </summary>
	/// <param name="gameObject"></param>
	private void TryAttachGameObject( GameObject gameObject )
	{
		if ( gameObject.Root.Components.Get<Attachable>( FindMode.EnabledInSelfAndDescendants ) is { } attachable )
		{
			TryAttach( attachable );
		}
	}

	//
	// Try to attach an attachable to this attachment point if we collide with it.
	//
	void ITriggerListener.OnTriggerEnter( Collider other ) => TryAttachGameObject( other.GameObject );
	void ICollisionListener.OnCollisionStart( Collision collision ) => TryAttachGameObject( collision.Other.GameObject );
}
