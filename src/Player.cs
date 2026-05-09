using Godot;
using System;
using ProjectRider.Forms;
using ProjectRider.Extensions; // Tambahkan ini agar GetAnimationDuration bisa dipanggil

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

	private void HandleMovement()
	{
		float directionX = Input.GetAxis("move_left", "move_right");
		float targetSpeed = CurrentForm.Speed;

		if (_isCrawling) targetSpeed = CurrentForm.CrawlSpeed;
		// Pakai RunMultiplier dari BaseForm
		else if (Input.IsActionPressed("run") && IsOnFloor()) targetSpeed *= CurrentForm.RunMultiplier;

		if (Input.IsActionJustPressed("dash") && !_isDashing && CurrentForm == HeroData)
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

		if (HandleActionLocks()) return;

		float moveDir = Input.GetAxis("move_left", "move_right");
		float speedMag = Mathf.Abs(_velocity.X);
		bool isSkidding = (moveDir > 0 && _velocity.X < -20) || (moveDir < 0 && _velocity.X > 20);

		if (moveDir != 0 && !isSkidding) 
			PlayerVisuals.FlipH = moveDir < 0;

		string targetAnim = (IsOnFloor(), _isCrawling, isSkidding, _isSliding) switch
		{
			(_, _, _, true)          => CurrentForm.SlideAnim,
			(false, _, _, _)         => CurrentForm.JumpAnim,
			(true, true, _, _)       => CurrentForm.CrawlAnim,
			(true, _, true, _)       => CurrentForm.WalkTurnAnim,
			(true, _, _, _) when _isDashing => CurrentForm.DashAnim,
			(true, _, _, _) when speedMag > CurrentForm.Speed * 1.2f => CurrentForm.RunAnim,
			(true, _, _, _) when speedMag > 1.0f => CurrentForm.WalkAnim,
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
