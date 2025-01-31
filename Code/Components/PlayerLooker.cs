public partial class PlayerLooker : Component
{
	[Property] public Hand Hand { get; set; }
	[Property] public GameObject Head { get; set; }

	[Property] public float Angle { get; set; } = 30f;

	TimeSince TimeSinceSnap = 1;

	protected override void OnUpdate()
	{
		if ( Hand.GetController() is null ) return;

		var inputDir = Hand.GetController().Joystick.Value;
		if ( !inputDir.x.AlmostEqual( 0f ) )
		{
			Snap( inputDir.x > 0f ? true : false );
		}
	}

	void Snap( bool isRight )
	{
		if ( TimeSinceSnap < 0.2f ) return;

		var angles = Head.WorldRotation.Angles().WithPitch( 0f );

		var rot = angles.ToRotation();
		var right = isRight ? rot.Right : rot.Left;

		TimeSinceSnap = 0;
		WorldRotation *= Rotation.From( 0, isRight ? -Angle : Angle, 0 );
	}
}
