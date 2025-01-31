public partial class Weapon
{
	private LegacyParticleSystem CreateParticleSystem( string particle, Vector3 pos, Rotation rot, List<ParticleControlPoint> cps = null, float decay = 5f )
	{
		var gameObject = Scene.CreateObject();
		gameObject.WorldPosition = pos;
		gameObject.WorldRotation = rot;

		var p = gameObject.Components.Create<LegacyParticleSystem>();
		p.Particles = ParticleSystem.Load( particle );
		p.ControlPoints = cps ?? new()
		{
			new() { Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = pos }
		};

		// Clear off in a suitable amount of time.
		// TODO: destroy async util
		// gameObject.DestroyAsync( decay );

		return p;
	}

	private void CreateImpactEffects( GameObject hitObject, Surface surface, Vector3 pos, Vector3 normal )
	{
		var decalPath = Game.Random.FromList( surface.ImpactEffects.BulletDecal, "decals/bullethole.decal" );
		if ( ResourceLibrary.TryGet<DecalDefinition>( decalPath, out var decalResource ) )
		{
			CreateParticleSystem( Game.Random.FromList( surface.ImpactEffects.Bullet ), pos, Rotation.LookAt( -normal ) );

			var decal = Game.Random.FromList( decalResource.Decals );

			var gameObject = Scene.CreateObject();
			gameObject.WorldPosition = pos;
			gameObject.WorldRotation = Rotation.LookAt( -normal );

			// Random rotation
			gameObject.WorldRotation *= Rotation.FromAxis( Vector3.Forward, decal.Rotation.GetValue() );

			var decalRenderer = gameObject.Components.Create<DecalRenderer>();
			decalRenderer.Material = decal.Material;
			decalRenderer.Size = new( decal.Width.GetValue(), decal.Height.GetValue(), decal.Depth.GetValue() );

			// Creates a destruction component to destroy the gameobject after a while
			// TODO: destroy async util
			//gameObject.DestroyAsync( 3f );
		}

		if ( !string.IsNullOrEmpty( surface.Sounds.Bullet ) )
		{
			// TODO: play bullet impact sound
			// hitObject.PlaySound( surface.Sounds.Bullet );
		}
	}

	/// <summary>
	/// Do shoot effects
	/// </summary>
	protected void DoShootEffects()
	{
		if ( ShootSound is not null )
		{
			if ( Sound.Play( ShootSound, MuzzleGameObject.WorldPosition ) is SoundHandle snd )
			{
				snd.ListenLocal = !IsProxy;
			}
		}
	}

	protected void DoTracer( Vector3 startPosition, Vector3 endPosition, float distance, int count )
	{
		var effectPath = "particles/gameplay/guns/trail/trail_smoke.vpcf";

		// For when we have bullet penetration implemented.
		if ( count > 0 )
		{
			effectPath = "particles/gameplay/guns/trail/rico_trail_smoke.vpcf";

			// Project backward
			Vector3 dir = (startPosition - endPosition).Normal;
			var tr = Scene.Trace.Ray( endPosition, startPosition + (dir * 50f) )
				.Radius( 1f )
				.WithoutTags( "weapon" )
				.Run();

			if ( tr.Hit )
			{
				CreateImpactEffects( tr.GameObject, tr.Surface, tr.StartPosition, dir );
			}
		}

		var origin = count == 0 ? MuzzleGameObject.WorldPosition : startPosition;

		// What in tarnation is this 
		CreateParticleSystem( effectPath, origin, Rotation.Identity, new()
		{
			new() { StringCP = "0", Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = origin },
			new() { StringCP = "1", Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = endPosition },
			new() { StringCP = "2", Value = ParticleControlPoint.ControlPointValueInput.Float, FloatValue = distance }
		}, 3f );
	}
}
