using Godot;

namespace ProjectRider.Extensions;

public static class AnimatedSpriteExtensions
{
	/// <summary>
	/// Menghitung durasi animasi dalam detik berdasarkan jumlah frame dan FPS.
	/// </summary>
	public static float GetAnimationDuration(this AnimatedSprite2D sprite, string animName)
	{
		if (string.IsNullOrEmpty(animName) || !sprite.SpriteFrames.HasAnimation(animName))
			return 0f;

		float frameCount = sprite.SpriteFrames.GetFrameCount(animName);
		float fps = (float)sprite.SpriteFrames.GetAnimationSpeed(animName);

		return fps > 0 ? frameCount / fps : 0f;
	}
}
