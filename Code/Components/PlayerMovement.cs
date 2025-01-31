public partial class PlayerMovement : Component
{
	[Property] public Hand Hand { get; set; }
	[Property] public GameObject Head { get; set; }

	[Property] public float Speed { get; set; } = 50f;

	protected override void OnUpdate()
	{
		if ( Hand.GetController() is null ) return;

		var inputDir = Hand.GetController().Joystick.Value;
		var headRot = Head.WorldRotation;
		var fwd = new Vector3( inputDir.y, -inputDir.x, 0 ) * headRot;

		Move( fwd );
	}

	void Move( Vector3 direction )
	{
		direction = direction.WithZ( 0 );
		var velocity = direction * Speed * Time.Delta;
		GameObject.WorldPosition += velocity;
	}
}
