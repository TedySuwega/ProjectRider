using Godot;
using System;
using RiderProject.Forms;

public partial class Player : CharacterBody2D
{
	[Export] public AnimatedSprite2D PlayerVisuals;

	[ExportGroup("Form Resources")]
	[Export] public BaseForm HumanData;
	[Export] public BaseForm HeroData;
	[Export] public BaseForm CurrentForm;

	// Movement Variables
	private Vector2 _velocity;
	private float _dashSpeedMultiplier = 1.0f;
	private float _coyoteTimer = 0.0f; 
	
	// Status Flags
	private bool _isAttacking = false;
	private bool _isHenshin = false;
	private bool _isDashing = false;
	private bool _isSliding = false;
	private bool _isCrawling = false;

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

		// Coyote Time
		if (IsOnFloor()) _coyoteTimer = 0.15f;
		else _coyoteTimer -= (float)delta;

		if (!_isAttacking && !_isHenshin)
		{
			HandleCrawl();   
			HandleJump();    
			HandleMovement();
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
			_velocity += GetGravity() * (float)delta;
	}

	private void HandleJump()
	{
		if (Input.IsActionJustPressed("jump") && _coyoteTimer > 0)
		{
			_velocity.Y = CurrentForm.JumpVelocity;
			_coyoteTimer = 0;
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

	private void HandleMovement()
	{
		float directionX = Input.GetAxis("move_left", "move_right");
		float targetSpeed = CurrentForm.Speed;

		if (_isCrawling) targetSpeed = CurrentForm.CrawlSpeed;
		else if (Input.IsActionPressed("run") && IsOnFloor()) targetSpeed *= 1.6f;

		if (Input.IsActionJustPressed("dash") && !_isDashing && CurrentForm == HeroData)
			PerformDash();

		float finalSpeed = targetSpeed * _dashSpeedMultiplier;
		_velocity.X = directionX != 0 ? directionX * finalSpeed : Mathf.MoveToward(_velocity.X, 0, finalSpeed);
	}

	private async void PerformDash()
	{
		_isDashing = true;
		_dashSpeedMultiplier = 2.5f;
		await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
		_dashSpeedMultiplier = 1.0f;
		_isDashing = false;
	}

	private void HandleSlide()
	{
		if (Input.IsActionJustPressed("slide") && IsOnFloor() && !_isSliding)
		{
			if (Mathf.Abs(_velocity.X) > CurrentForm.Speed * 1.1f) StartSlide();
		}
	}

	private async void StartSlide()
	{
		_isSliding = true;
		_velocity.X = (PlayerVisuals.FlipH ? -1 : 1) * (CurrentForm.Speed * 2.0f);
		PlayAnimationSafely(CurrentForm.SlideAnim);
		await ToSignal(GetTree().CreateTimer(0.4f), "timeout");
		_isSliding = false;
	}

	private void HandleAttack()
	{
		if (Input.IsActionJustPressed("attack") && IsOnFloor() && CurrentForm is ICombatant combatForm)
		{
			_isAttacking = true;
			PlayerVisuals.Play(combatForm.AttackAnim);
			PlayerVisuals.Frame = 0;
		}
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

	private void UpdateVisuals()
	{
		if (PlayerVisuals == null || CurrentForm == null) return;

		// 1. Prioritas: Kunci animasi Henshin/Attack
		if (HandleActionLocks()) return;

		float moveDir = Input.GetAxis("move_left", "move_right");
		float speedMag = Mathf.Abs(_velocity.X);
		bool isSkidding = (moveDir > 0 && _velocity.X < -20) || (moveDir < 0 && _velocity.X > 20);

		// 2. Facing Logic
		if (moveDir != 0 && !isSkidding) 
			PlayerVisuals.FlipH = moveDir < 0;

		// 3. Switch Expression untuk menentukan Target Animasi
		// Tuple: (IsOnFloor, isCrawling, isSkidding, isSliding)
		string targetAnim = (IsOnFloor(), _isCrawling, isSkidding, _isSliding) switch
		{
			(_, _, _, true)          => CurrentForm.SlideAnim,      // Sedang Sliding aktif
			(false, _, _, _)         => CurrentForm.JumpAnim,       // Sedang melompat
			(true, true, _, _)       => CurrentForm.CrawlAnim,      // Merangkak (Human Only)
			(true, _, true, _)       => CurrentForm.WalkTurnAnim,   // Ngerem mendadak
			(true, _, _, _) when _isDashing => CurrentForm.DashAnim, // Dash aktif
			(true, _, _, _) when speedMag > CurrentForm.Speed * 1.2f => CurrentForm.RunAnim, // Lari kencang
			(true, _, _, _) when speedMag > 1.0f => CurrentForm.WalkAnim, // Jalan biasa
			_ => (PlayerVisuals.Animation == CurrentForm.RunAnim) ? CurrentForm.RunToIdleAnim : CurrentForm.IdleAnim
		};

		PlayAnimationSafely(targetAnim);
	}

	// Fungsi pembantu untuk mengecek apakah animasi yang mengunci state sudah selesai
	private bool HandleActionLocks()
	{
		if (_isHenshin && !string.IsNullOrEmpty(CurrentForm.HenshinAnim))
		{
			if (PlayerVisuals.Frame >= PlayerVisuals.SpriteFrames.GetFrameCount(CurrentForm.HenshinAnim) - 1)
				_isHenshin = false;
			return true;
		}

		if (_isAttacking && CurrentForm is ICombatant combatForm)
		{
			if (PlayerVisuals.Frame >= PlayerVisuals.SpriteFrames.GetFrameCount(combatForm.AttackAnim) - 1)
				_isAttacking = false;
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
