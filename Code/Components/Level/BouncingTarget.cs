/// <summary>
/// A bouncing target. It bounces from left to right. The distance is predetermined.
/// </summary>
public sealed class BouncingTarget : Component
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
	/// Is this target triggered? (Is it moving?)
	/// </summary>
	[Property] public bool Triggered { get; set; } = true;

	/// <summary>
	/// Converts meters to inches (the engine's unit system).
	/// </summary>
	/// <param name="meters"></param>
	/// <returns></returns>
	float MetersToInches( float meters )
	{
		return (meters * 39.3701f);
	}

	/// <summary>
	/// Flip the distance.
	/// </summary>
	void Flip()
	{
		Distance = -Distance;
	}

	float SmoothDistance = 0;
	protected override void OnUpdate()
	{
		if ( !Triggered ) return;

		SmoothDistance = SmoothDistance.Approach( Distance, Time.Delta * Speed );
		var inches = MetersToInches( SmoothDistance );
		Object.LocalPosition = WorldRotation.Right * inches;

		if ( SmoothDistance.AlmostEqual( Distance ) )
		{
			Flip();
		}
	}
}
