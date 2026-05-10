using Godot;
using System;
using ProjectRider.Forms;
using ProjectRider.Extensions; 

public partial class Player : CharacterBody2D
{
	[Export] public AnimatedSprite2D PlayerVisuals;

	[ExportGroup("Form Resources")]
	[Export] public BaseForm HumanData;
	[Export] public BaseForm HeroData;
	[Export] public BaseForm CurrentForm;

	private Vector2 _velocity;
	private float _dashSpeedMultiplier = 1.0f;
	private float _coyoteTimer = 0.0f; 
	private float _wallJumpInputLockTimer = 0.0f;

	private int _comboCount = 0; // Pukulan ke berapa (0, 1, 2)
	private float _comboTimer = 0.0f; // Sisa waktu untuk lanjut combo
	private const float COMBO_WINDOW = 0.8f; // Toleransi waktu antar pencetan Z

	private bool _isAttacking = false;
	private bool _isHenshin = false;
	private bool _isDashing = false;
	private bool _isSliding = false;
	private bool _isCrawling = false;
	private bool _isWallSliding = false;

	public override void _Ready()
	{
		if (HumanData != null)
		{
			CurrentForm = HumanData;
			GD.Print($"Game Start: {CurrentForm.FormName} Activated.");
		}
		else
		{
			GD.PrintErr("Error: HumanData belum ditarik ke Inspector!");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CurrentForm == null) return;

		_velocity = Velocity;
		ApplyGravity(delta);
		HandleWallMovement(delta);

		if (IsOnFloor()) _coyoteTimer = 0.15f;
		else _coyoteTimer -= (float)delta;

		if (!_isAttacking && !_isHenshin)
		{
			HandleCrawl();   
			HandleJump();    
			HandleMovement(delta);
			HandleSlide();
			HandleAttack();
			HandleHenshin();
		}
		else
		{
			_velocity.X = Mathf.MoveToward(_velocity.X, 0, CurrentForm.Speed);
		}

		Velocity = _velocity;
		MoveAndSlide();
		UpdateVisuals();
	}

	private void ApplyGravity(double delta)
	{
		if (!IsOnFloor())
		{
			// _velocity += GetGravity() * (float)delta;
			float gravity = (float)GetGravity().Y;
		
			// Jika sedang jatuh, gravitasi lebih berat (Fast Fall)
			if (_velocity.Y > 0) gravity *= 1.5f; 
			
			_velocity.Y += gravity * (float)delta;
		}
	}

	private void HandleJump()
	{
		if (Input.IsActionJustPressed("jump") && _coyoteTimer > 0)
		{
			_velocity.Y = CurrentForm.JumpVelocity;
			_coyoteTimer = 0;

			// State Interrupt: Jika sedang slide lalu lompat, slide berhenti
			if (_isSliding) _isSliding = false;
		}

		if (Input.IsActionJustReleased("jump") && _velocity.Y < -50)
		{
			_velocity.Y *= 0.5f; 
		}
	}

	private void HandleCrawl()
	{
		_isCrawling = Input.IsActionPressed("duck") && IsOnFloor() && CurrentForm == HumanData;
	}

	private void HandleMovement(double delta)
	{
		// --- TAMBAHKAN INI DI AWAL FUNGSI ---
		// Kurangi timer setiap frame
		_wallJumpInputLockTimer -= (float)delta;

		float directionX = Input.GetAxis("move_left", "move_right");
		float targetSpeed = CurrentForm.Speed;

		// Jika input masih dikunci, paksa directionX jadi 0
		if (_wallJumpInputLockTimer > 0)
		{
			directionX = 0;
		}
		// -------------------------------------

		if (_isCrawling) targetSpeed = CurrentForm.CrawlSpeed;
		// Pakai RunMultiplier dari BaseForm
		else if (Input.IsActionPressed("run") && IsOnFloor()) targetSpeed *= CurrentForm.RunMultiplier;

		if (Input.IsActionJustPressed("dash") && !_isDashing && CurrentForm == HeroData && _velocity.X != 0)
			PerformDash();

		float finalSpeed = targetSpeed * _dashSpeedMultiplier;
		_velocity.X = directionX != 0 ? directionX * finalSpeed : Mathf.MoveToward(_velocity.X, 0, finalSpeed);
	}

	private async void PerformDash()
	{
		_isDashing = true;
		PlayAnimationSafely(CurrentForm.DashAnim);

		// DINAMIS: Durasi dari animasi, Kecepatan dari Resource
		float duration = PlayerVisuals.GetAnimationDuration(CurrentForm.DashAnim);
		_dashSpeedMultiplier = CurrentForm.DashMultiplier;

		await ToSignal(GetTree().CreateTimer(duration), "timeout");

		_dashSpeedMultiplier = 1.0f;
		_isDashing = false;
	}

	private void HandleSlide()
	{
		if (Input.IsActionJustPressed("slide") && IsOnFloor() && !_isSliding)
		{
			// Cek jika kecepatan cukup untuk slide
			if (Mathf.Abs(_velocity.X) > CurrentForm.Speed * 1.1f) StartSlide();
		}
	}

	private async void StartSlide()
	{
		_isSliding = true;
		PlayAnimationSafely(CurrentForm.SlideAnim);

		// DINAMIS: Durasi dari animasi, Kecepatan dari Resource
		float duration = PlayerVisuals.GetAnimationDuration(CurrentForm.SlideAnim);
		_velocity.X = (PlayerVisuals.FlipH ? -1 : 1) * (CurrentForm.Speed * CurrentForm.SlideMultiplier);

		await ToSignal(GetTree().CreateTimer(duration), "timeout");

		_isSliding = false;
	}

	// private void HandleAttack()
	// {
	// 	if (Input.IsActionJustPressed("attack") && IsOnFloor() && CurrentForm is ICombatant combatForm)
	// 	{
	// 		_isAttacking = true;
	// 		PlayerVisuals.Play(combatForm.AttackAnim);
	// 		PlayerVisuals.Frame = 0;
	// 	}
	// }
	private void HandleAttack()
	{
		// Kurangi timer combo setiap frame
		if (_comboTimer > 0) _comboTimer -= (float)GetProcessDeltaTime();
		else if (!_isAttacking) _comboCount = 0; // Reset combo jika waktu habis dan tidak sedang anim

		if (Input.IsActionJustPressed("attack") && IsOnFloor() && CurrentForm is ICombatant combat)
		{
			// Jika sedang menyerang, kita "simpan" inputnya untuk combo berikutnya
			if (_isAttacking) return;

			StartAttack(combat);
		}
	}

	private void StartAttack(ICombatant combat)
	{
		_isAttacking = true;

		// Ambil animasi berdasarkan urutan combo
		string animName = combat.ComboAnimations[_comboCount];
		PlayerVisuals.Play(animName);
		PlayerVisuals.Frame = 0;

		// Siapkan urutan selanjutnya
		_comboCount = (_comboCount + 1) % combat.ComboAnimations.Length;
		_comboTimer = COMBO_WINDOW; // Beri waktu pemain untuk lanjut pencet
	}

	private void HandleHenshin()
	{
		if (Input.IsActionJustPressed("henshin") && !_isHenshin)
		{
			_isHenshin = true;
			_velocity.X = 0;
			CurrentForm = (CurrentForm == HumanData) ? HeroData : HumanData;

			if (!string.IsNullOrEmpty(CurrentForm.HenshinAnim))
			{
				PlayerVisuals.Play(CurrentForm.HenshinAnim);
				PlayerVisuals.Frame = 0;
			}
			else _isHenshin = false;
		}
	}

	private void HandleWallMovement(double delta)
	{
		// Hanya Hero yang bisa Wall Jump/Slide
		if (CurrentForm != HeroData) 
		{
			_isWallSliding = false;
			return;
		}

		// Syarat Wall Slide: Di udara, menempel dinding, dan menekan arah ke dinding
		bool isTouchingWall = IsOnWallOnly(); // Fungsi bawaan Godot
		float wallDir = GetWallNormal().X; // Normal X: 1 (dinding di kiri), -1 (dinding di kanan)

		if (isTouchingWall && !IsOnFloor() && _velocity.Y > 0)
		{
			_isWallSliding = true;

			PlayerVisuals.FlipH = wallDir < 0;
			// Efek gesekan: Kecepatan jatuh dibatasi
			_velocity.Y = Mathf.Min(_velocity.Y, CurrentForm.WallSlideSpeed);
			
			// Handle Wall Jump
			if (Input.IsActionJustPressed("jump"))
			{
				PerformWallJump(wallDir);
			}
		}
		else
		{
			_isWallSliding = false;
		}
	}

	private void PerformWallJump(float wallNormalX)
	{
		_isWallSliding = false;
	
		// Zig-Zag Force (Sesuaikan nilainya di BaseForm)
		_velocity.X = wallNormalX * CurrentForm.WallJumpForce.X;
		_velocity.Y = CurrentForm.WallJumpForce.Y;

		// --- TAMBAHKAN INI: Kunci input selama 0.1 - 0.15 detik ---
		// Waktu yang sangat singkat tapi cukup untuk mematikan input arah pemain
		_wallJumpInputLockTimer = 0.15f; 
		// ----------------------------------------------------------

		PlayAnimationSafely(CurrentForm.WallJumpAnim);
	}

	private void UpdateVisuals()
	{
		if (PlayerVisuals == null || CurrentForm == null) return;

		if (HandleActionLocks()) return;

		float moveDir = Input.GetAxis("move_left", "move_right");
		float speedMag = Mathf.Abs(_velocity.X);
		bool isSkidding = (moveDir > 0 && _velocity.X < -20) || (moveDir < 0 && _velocity.X > 20);

		if (_isWallSliding)
		{
			// Saat wall slide, paksa hadap menjauh dari normal dinding
			// GetWallNormal().X bernilai 1 jika dinding di kiri, maka hadap kanan (FlipH = true)
			PlayerVisuals.FlipH = GetWallNormal().X < 0;
		}
		else if (moveDir != 0 && !isSkidding) 
		{
			// Logika jalan biasa
			PlayerVisuals.FlipH = moveDir < 0;
		}

		string targetAnim = (IsOnFloor(), _isCrawling, isSkidding, _isSliding, _isWallSliding) switch
		{
			
			(_, _, _, _, true)         => CurrentForm.WallSlideAnim,
			(_, _, _, true, _)          => CurrentForm.SlideAnim,
			(false, _, _, _, _) => _velocity.Y < 0 ? CurrentForm.JumpAnim : CurrentForm.FallAnim,
			(true, true, _, _, _)       => CurrentForm.CrawlAnim,
			(true, _, true, _, _)       => CurrentForm.WalkTurnAnim,
			(true, _, _, _, _) when _isDashing => CurrentForm.DashAnim,
			(true, _, _, _, _) when speedMag > CurrentForm.Speed * 1.2f => CurrentForm.RunAnim,
			(true, _, _, _, _) when speedMag > 1.0f => CurrentForm.WalkAnim,
			_ => (PlayerVisuals.Animation == CurrentForm.RunAnim) ? CurrentForm.RunToIdleAnim : CurrentForm.IdleAnim
		};

		PlayAnimationSafely(targetAnim);
	}

	private bool HandleActionLocks()
	{
		if (_isHenshin && !string.IsNullOrEmpty(CurrentForm.HenshinAnim))
		{
			if (PlayerVisuals.Frame >= PlayerVisuals.SpriteFrames.GetFrameCount(CurrentForm.HenshinAnim) - 1)
				_isHenshin = false;
			return true;
		}

		// if (_isAttacking && CurrentForm is ICombatant combatForm)
		// {
		// 	if (PlayerVisuals.Frame >= PlayerVisuals.SpriteFrames.GetFrameCount(combatForm.AttackAnim) - 1)
		// 		_isAttacking = false;
		// 	return true;
		// }
		if (_isAttacking && CurrentForm is ICombatant combatForm)
		{
			// Cek apakah animasi yang sekarang diputar sudah selesai
			if (PlayerVisuals.Frame >= PlayerVisuals.SpriteFrames.GetFrameCount(PlayerVisuals.Animation) - 1)
			{
				_isAttacking = false;

				// Sedikit trick: Jika setelah animasi selesai timer masih ada, 
				// jangan reset comboCount agar bisa lanjut ke pukulan berikutnya.
			}
			return true;
		}

		return false;
	}

	private void PlayAnimationSafely(string animName)
	{
		if (!string.IsNullOrEmpty(animName) && PlayerVisuals.SpriteFrames.HasAnimation(animName))
		{
			PlayerVisuals.Play(animName);
		}
	}
}
