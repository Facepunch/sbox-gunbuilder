/// <summary>
/// A flip up target. They'll ideally flip down when shot, then spring back up in a set time (by another system).
/// </summary>
public sealed class FlipUpTarget : Component, Component.IDamageable
{
	/// <summary>
	/// The target in question.
	/// </summary>
	[Property] public GameObject Target { get; set; }

	/// <summary>
	/// A sound to play when triggering the target.
	/// </summary>
	[Property] public SoundEvent TriggerSound { get; set; }

	/// <summary>
	/// How quick?
	/// </summary>
	[Property] public float Speed { get; set; } = 10;

	private bool triggered = false;
	
	/// <summary>
	/// Is this target active? (Triggered = upright)
	/// </summary>
	[Property] public bool Triggered
	{
		get => triggered;
		set
		{
			if ( triggered == value ) return;

			triggered = value;
			TimeSinceChanged = 0;
			
			if ( TriggerSound is not null )
			{
				Sound.Play( TriggerSound, WorldPosition );
			}
		}
	}

	private TimeSince TimeSinceChanged = 1;

	float SmoothPitch = 180;
	protected override void OnUpdate()
	{
		SmoothPitch = SmoothPitch.Approach( Triggered ? 90 : 180, Time.Delta * Speed );
		Target.LocalRotation = Rotation.From( SmoothPitch, 0, 0 );

		if ( TimeSinceChanged > 1f && !Triggered ) Triggered = true;
	}

	void IDamageable.OnDamage( in DamageInfo damage )
	{
		if ( Triggered )
		{
			Triggered = false;
		}
	}
}
