
/// <summary>
/// A classic moving target. This is designed to be moved with input, by setting the distance using some buttons in physical space.
/// </summary>
public sealed class MovingTarget : Component
{
	/// <summary>
	/// The object we're moving.
	/// </summary>
	[Property] public GameObject Object { get; set; }

	/// <summary>
	/// How quick are we?
	/// </summary>
	[Property] public float Speed { get; set; }

	float dist = 0;

	/// <summary>
	/// How far (in meters) can this target go in any direction?
	/// </summary>
	[Property, Title( "Distance (meters)" )] public float Distance
	{
		get => dist;
		set
		{
			if ( dist == value ) return;
			dist = value;
		}
	}

	/// <summary>
	/// Converts meters to inches (the engine's unit system).
	/// </summary>
	/// <param name="meters"></param>
	/// <returns></returns>
	float MetersToInches( float meters )
	{
		return (meters * 39.3701f);
	}

	float SmoothDistance = 0;
	protected override void OnUpdate()
	{
		SmoothDistance = SmoothDistance.Approach( Distance, Time.Delta * Speed );
		var inches = MetersToInches( SmoothDistance );
		Object.LocalPosition = WorldRotation.Forward * inches;
	}
}
