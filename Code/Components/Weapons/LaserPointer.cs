
/// <summary>
/// A laser pointer .
/// </summary>
public partial class LaserPointerComponent : Component, Component.ExecuteInEditor
{
	/// <summary>
	/// The particle system for the laser line.
	/// </summary>
	[Property] public Sandbox.ParticleSystem LineParticle { get; set; }

	/// <summary>
	/// The particle system for the laser dot.
	/// </summary>
	[Property] public Sandbox.ParticleSystem DotParticle { get; set; }

	/// <summary>
	/// The color of the laser.
	/// </summary>
	[Property] public Color LaserColor { get; set; }

	/// <summary>
	/// Reference to the line particle system instance.
	/// </summary>
	LegacyParticleSystem LineParticleSystem { get; set; }

	/// <summary>
	/// Reference to the dot particle system instance.
	/// </summary>
	LegacyParticleSystem DotParticleSystem { get; set; }

	/// <summary>
	/// Is this thing on? Handles turning on the line and dot.
	/// </summary>
	[Property, MakeDirty] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// A reference to the grab point that we want to respond to input events for.
	/// </summary>
	[Property] public GrabPoint GrabPoint { get; set; }

	/// <summary>
	/// How long has it been since we toggled the laser pointer?
	/// </summary>
	TimeSince TimeSinceToggled { get; set; } = 1;

	/// <summary>
	/// The control point setup for the line particle system.
	/// </summary>
	private List<ParticleControlPoint> LineCPs
	{
		get
		{
			return new()
			{
				new() { StringCP = "0", Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = WorldPosition },
				new() { StringCP = "1", Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = GetTraceEnd() },
				new() { StringCP = "2", Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = new( LaserColor.r, LaserColor.g, LaserColor.b )  },
			};
		}
	}

	/// <summary>
	/// The control point setup for the dot particle system.
	/// </summary>
	private List<ParticleControlPoint> DotCPs
	{
		get
		{
			return new()
			{
				new() { StringCP = "0", Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = GetTraceEnd() },
				new() { StringCP = "2", Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = new( LaserColor.r, LaserColor.g, LaserColor.b )  },
			};
		}
	}

	/// <summary>
	/// How far forward do laser pointers go?
	/// </summary>
	const float Dist = 100000;

	/// <summary>
	/// IsEnabled will call this method.
	/// </summary>
	protected override void OnDirty()
	{
		SetEnabled( IsEnabled );
	}

	/// <summary>
	/// Gets an end position for the trace of the laser pointer.
	/// </summary>
	/// <returns></returns>
	private Vector3 GetTraceEnd()
	{
		var tr = Scene.Trace.Ray( WorldPosition, WorldPosition + ( WorldRotation.Forward * Dist ) ).Run();
		return tr.EndPosition;
	}

	/// <summary>
	/// Turns on/off the laser pointer.
	/// </summary>
	/// <param name="enabled"></param>
	public void SetEnabled( bool enabled )
	{
		if ( enabled )
		{
			LineParticleSystem?.Destroy();
			DotParticleSystem?.Destroy();

			if ( LineParticle is not null )
			{
				LineParticleSystem = Components.Create<LegacyParticleSystem>();
				LineParticleSystem.Flags = ComponentFlags.NotSaved | ComponentFlags.Hidden;
				LineParticleSystem.Particles = LineParticle;
				LineParticleSystem.ControlPoints = LineCPs;
			}

			if ( DotParticle is not null )
			{
				DotParticleSystem = Components.Create<LegacyParticleSystem>();
				DotParticleSystem.Flags = ComponentFlags.NotSaved | ComponentFlags.Hidden;
				DotParticleSystem.Particles = DotParticle;
				DotParticleSystem.ControlPoints = DotCPs;
			}
		}
		else
		{
			LineParticleSystem?.Destroy();
			DotParticleSystem?.Destroy();
		}
	}

	protected override void OnEnabled()
	{
		SetEnabled( IsEnabled );
	}

	protected override void OnDisabled()
	{
		// Mainly for the editor. Since I want to see them there.
		SetEnabled( false );
	}

	protected override void OnUpdate()
	{
		// Make sure we update the control points of both particle systems.
		if ( LineParticleSystem.IsValid() )
			LineParticleSystem.ControlPoints = LineCPs;

		if ( DotParticleSystem.IsValid() )
			DotParticleSystem.ControlPoints = DotCPs;

		// Grab point input parsing.
		if ( GrabPoint.IsValid() && GrabPoint.HeldHand.IsValid() )
		{
			var controller = GrabPoint.HeldHand.GetController();
			if ( controller.JoystickPress.IsPressed && TimeSinceToggled > 0.5f )
			{
				TimeSinceToggled = 0;

				IsEnabled ^= true;
				SetEnabled( IsEnabled );
			}
		}
	}
}
