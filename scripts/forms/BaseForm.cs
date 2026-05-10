using Godot;

namespace ProjectRider.Forms;

[GlobalClass]
public partial class BaseForm : Resource
{
	[Export] public float Speed = 100.0f;
	[Export] public float JumpVelocity = -200.0f;
	
	[ExportGroup("Crawl Settings")]
	[Export] public string CrawlAnim = "slide_hero"; // Placeholder pake slide dulu
	[Export] public float CrawlSpeed = 40.0f;   // Jauh lebih lambat dari jalan biasa

	[ExportGroup("Advanced Movement Anims")]
	[Export] public string RunAnim = "run";
	[Export] public string DashAnim = "";
	[Export] public string SlideAnim = "";
	[Export] public string RunToIdleAnim = "";
	[Export] public string WalkTurnAnim = "";
	[Export] public string RunTurnAnim = "";
	[Export] public string IdleAnim = "idle";
	[Export] public string JumpAnim = "jump";
	[Export] public string WalkAnim = "walk";
	[Export] public string HenshinAnim = "";
	
	[Export] public string FormName; // Isi di Inspector masing-masing .tres

	[ExportGroup("Movement Multipliers")]
	[Export] public float RunMultiplier = 1.6f;  // PASTIKAN ADA 'public'
	[Export] public float DashMultiplier = 2.5f;
	[Export] public float SlideMultiplier = 2.2f;

	[ExportGroup("Wall Movement")]
	[Export] public string WallSlideAnim = "wall_slide_hero";
	[Export] public string WallJumpAnim = "wall_jump_hero";
	[Export] public float WallSlideSpeed = 100.0f;
	[Export] public Vector2 WallJumpForce = new Vector2(400, -450);
	
}
