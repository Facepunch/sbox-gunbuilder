using Sandbox;

public sealed class LookAtLight : Component
{
	[Property] public GameObject Target { get; set; }

	protected override void OnUpdate()
	{
		GameObject.WorldRotation = Rotation.LookAt( (Target.WorldPosition - WorldPosition).Normal, Vector3.Up );
	}
}
