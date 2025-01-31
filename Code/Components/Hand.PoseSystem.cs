using Sandbox.VR;
using System.Xml.XPath;

public partial class Hand
{
	/// <summary>
	/// Represents a controller to use when fetching skeletal data (finger curl/splay values)
	/// </summary>
	public enum HandSources
	{
		/// <summary>
		/// The left controller
		/// </summary>
		Left,

		/// <summary>
		/// The right controller
		/// </summary>
		Right
	}

	/// <summary>
	/// Which hand should we use to update the parameters?
	/// </summary>
	[Property] public HandSources HandSource { get; set; } = HandSources.Left;

	[Property] public SkinnedModelRenderer SkinnedModelComponent { get; set; }

	/// <summary>
	/// A preset pose. This is fucking shit, but I don't think it matters for this game.
	/// </summary>
	public enum PresetPose
	{
		None,
		Grip,
		GripNoIndex,
		HoldItem,
		Clamp
	}
	
	private static List<string> AnimGraphNames = new()
	{
		"FingerCurl_Thumb",
		"FingerCurl_Index",
		"FingerCurl_Middle",
		"FingerCurl_Ring",
		"FingerCurl_Pinky"
	};

	/// <summary>
	/// Applies a hand preset for this hand.
	/// </summary>
	/// <param name="preset"></param>
	public void ApplyHandPreset( HandPreset preset = null )
	{
		if ( !Game.IsRunningInVR ) return;

		// Get our controller inputs
		var source = GetController();

		SkinnedModelComponent.Set( "BasePose", 1 );
		SkinnedModelComponent.Set( "bGrab", true );
		SkinnedModelComponent.Set( "GrabMode", 1 );

		for ( FingerValue v = FingerValue.ThumbCurl; v <= FingerValue.PinkyCurl; ++v )
		{
			SkinnedModelComponent.Set( AnimGraphNames[(int)v], source.GetFingerValue( v ) );
		}

		if ( preset is not null )
		{
			preset.Apply( SkinnedModelComponent );
		}
	}

	/// <summary>
	/// Designed to run every Update, will update the pose of the hand.
	/// </summary>
	private void UpdatePose()
	{
		if ( !SkinnedModelComponent.IsValid() )
			return;

		if ( IsHolding() )
		{
			if ( CurrentGrabbable is GrabPoint grabPoint )
			{
				grabPoint.UpdateHandPose( this );
			}
		}
		else
		{
			// Use no preset
			ApplyHandPreset();
		}
	}
}
