using Godot;
using System;
using ProjectRider.Forms;
using ProjectRider.Extensions;
using ProjectRider.States;
using ProjectRider.Forms.HeroForm;

public partial class Player : CharacterBody2D
{
	[Export] public AnimatedSprite2D PlayerVisuals;

	[ExportGroup("Form Resources")]
	[Export] public BaseForm HumanData;
	[Export] public BaseForm HeroData;
	[Export] public BaseForm HeroData2; // TAMBAHKAN INI (Hero 2 - Biru)
	[Export] public BaseForm HeroData3; // TAMBAHKAN INI (Hero 3 - Hijau)
	[Export] public BaseForm HeroData4; // TAMBAHKAN INI (Hero 4 - Kamen Rider Black)
	[Export] public BaseForm CurrentForm;

	[ExportGroup("Rider Kick Settings")]
	[Export] public string RiderKickAnim = "kick_hero"; // Siapkan nama animasinya di Godot
	[Export] public float RiderKickSpeed = 600.0f;       // Kecepatan meluncur menukik
	[Export] public float RiderKickAngle = 45.0f;        // Sudut kemiringan tendangan (dalam derajat)

	// Helper untuk mengecek arah hadap player (kiri/kanan) untuk menentukan arah kick
	public float GetFacingDirection()
	{
		return PlayerVisuals.FlipH ? -1.0f : 1.0f;
	}
	private Vector2 _velocity;
	private float _dashSpeedMultiplier = 1.0f;
	private float _coyoteTimer = 0.0f; 
	private float _wallJumpInputLockTimer = 0.0f;
	private float _ultimateFormTimer = 0.0f;
    private const float ULTIMATE_DURATION = 5.0f; // Bisa kamu adjust ke 60.0f (1 menit) nanti

	private PlayerState _currentState;

	public int JumpCount { get; set; } = 0; // Melacak jumlah lompatan aktif
	private int _comboCount = 0; // Pukulan ke berapa (0, 1, 2)
	private float _comboTimer = 0.0f; // Sisa waktu untuk lanjut combo
	private const float COMBO_WINDOW = 0.8f; // Toleransi waktu antar pencetan Z

	private bool _isDashing = false;
	private bool _isSliding = false;
	private bool _isCrawling = false;
	private bool _isWallSliding = false;
	private BaseForm _nextTargetForm;

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

		ChangeState(new IdleState(this));
	}

	public Vector2 WorkingVelocity
	{
		get => _velocity;
		set => _velocity = value;
	}

	public void ChangeState(PlayerState newState)
	{
		_currentState?.Exit();
		_currentState = newState;
		_currentState?.Enter();
	}

	public bool CanCoyoteJump() => IsOnFloor() || _coyoteTimer > 0;

	public void ApplyJumpFromState()
	{
		if (_isSliding) _isSliding = false;
		_coyoteTimer = 0;
	}

	public void ApplyJumpCut()
	{
		if (_velocity.Y < -50)
			_velocity.Y *= 0.5f;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CurrentForm == null) return;

		_velocity = Velocity;
		TickCoyote(delta);
		TickComboTimer(delta);
		TickUltimateFormTimer(delta);
		HandleFormSwitchInput();

		_currentState?.Update(delta);
		Velocity = _velocity;
		MoveAndSlide();
		_velocity = Velocity;
		_currentState?.PhysicsPostMove(delta);
		UpdateVisuals();
	}

	private void TickCoyote(double delta)
	{
		if (IsOnFloor()) _coyoteTimer = 0.15f;
		else _coyoteTimer -= (float)delta;
	}

	private void TickComboTimer(double delta)
	{
		if (_currentState is AttackState)
			return;

		if (_comboTimer > 0)
			_comboTimer -= (float)delta;
		else
			_comboCount = 0;
	}

	public void ApplyGravity(double delta)
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

	public void ResetJumpCount()
	{
		JumpCount = 0;
	}

	public void ExecuteDoubleJump()
	{
		// Berikan daya dorong vertikal baru (bisa disamakan atau dibuat lebih ringan dari lompatan pertama)
		_velocity.Y = CurrentForm.JumpVelocity; 
		JumpCount = 2;

		// Efek Visual opsional: Mainkan kembali animasi jump dari frame 0 agar terlihat nge-snap
		PlayAnimationSafely(CurrentForm.JumpAnim);
		PlayerVisuals.Frame = 0;

		GD.Print("Hero 2 Double Jump Activated: Kinetic Burst!");
	}

	public void HandleCrawl()
	{
		_isCrawling = Input.IsActionPressed("duck") && IsOnFloor() && CurrentForm == HumanData;
	}

	public void ApplyHorizontalLocomotion(double delta)
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

		if (Input.IsActionJustPressed("dash") && !_isDashing && CurrentForm is HeroForm && _velocity.X != 0)
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

	public void HandleSlide()
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

	public void HandleAttack()
	{
		if (Input.IsActionJustPressed("attack") && IsOnFloor() && CurrentForm is ICombatant)
		{
			ChangeState(new AttackState(this));
		}
	}

	public void ExecuteComboStrike()
	{
		if (CurrentForm is not ICombatant combat || combat.ComboAnimations.Length == 0)
			return;

		// Ambil animasi berdasarkan urutan combo
		string animName = combat.ComboAnimations[_comboCount];
		PlayAnimationSafely(animName);
		PlayerVisuals.Frame = 0;

		// Siapkan urutan selanjutnya
		_comboCount = (_comboCount + 1) % combat.ComboAnimations.Length;
		_comboTimer = COMBO_WINDOW; // Beri waktu pemain untuk lanjut pencet
	}

	private void TickUltimateFormTimer(double delta)
    {
        // Hanya kurangi timer jika saat ini sedang menggunakan HeroData4
        if (CurrentForm == HeroData4)
        {
            if (_ultimateFormTimer > 0)
            {
                _ultimateFormTimer -= (float)delta;
            }
            else
            {
                // WAKTU HABIS: Paksa kembali ke HumanData menggunakan HenshinState
                GD.Print("Ultimate Form Time Out! Unhenshin to Human Form.");
                ChangeState(new HenshinState(this, HumanData));
            }
        }
    }

	// public void HandleHenshin()
	// {
	// 	if (Input.IsActionJustPressed("henshin"))
	// 	{
	// 		ChangeState(new HenshinState(this, _nextTargetForm));
	// 	}
	// }
	
	// Fungsi untuk mendeteksi input ganti wujud secara sederhana
    public void HandleFormSwitchInput()
    {
        BaseForm targetForm = null;

        if (Input.IsActionJustPressed("key_1")) targetForm = HumanData;
        if (Input.IsActionJustPressed("key_2")) targetForm = HeroData;  // Hero 1
        if (Input.IsActionJustPressed("key_3")) targetForm = HeroData2; // Hero 2
        if (Input.IsActionJustPressed("key_4")) targetForm = HeroData3; // Hero 3
		if (Input.IsActionJustPressed("key_5")) targetForm = HeroData4; // Hero 4

        // Pastikan target form tidak null, tidak sama dengan form sekarang, 
        // dan tidak sedang dalam keadaan HenshinState
        if (targetForm != null && targetForm != CurrentForm && _currentState is not HenshinState)
        {
			// JIKA BERUBAH KE HERO 4: Set ulang timernya ke 5 detik
            if (targetForm == HeroData4)
            {
                _ultimateFormTimer = ULTIMATE_DURATION;
            }
            // Kita simpan dulu target form-nya ke dalam variabel sementara (atau passing langsung ke State)
            _nextTargetForm = targetForm; 
            ChangeState(new HenshinState(this, targetForm));
        }
    }

	// Fungsi ini bisa dipanggil oleh Script UI Radial Menu kamu nanti saat tombol R1 dilepas
	public void RequestFormFromUI(int formIndex)
	{
		if (_currentState is HenshinState) return;

		BaseForm targetForm = formIndex switch
		{
			1 => HumanData,
			2 => HeroData,
			3 => HeroData2,
			4 => HeroData3,
			5 => HeroData4,
			_ => null
		};

		if (targetForm != null && targetForm != CurrentForm)
		{
			// JIKA BERUBAH KE HERO 4 VIA UI: Set ulang timernya
            if (targetForm == HeroData4)
            {
                _ultimateFormTimer = ULTIMATE_DURATION;
            }
			ChangeState(new HenshinState(this, targetForm));
		}
	}

	public void HandleWallMovement(double delta)
	{
		// Hanya Hero yang bisa Wall Jump/Slide
		if (CurrentForm is not HeroForm)
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
			if (Input.IsActionJustPressed("jump") && (CurrentForm == HeroData2 || CurrentForm == HeroData4))
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
		if (_currentState?.LocksVisuals == true) return;

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

	public void PlayAnimationSafely(string animName)
	{
		if (!string.IsNullOrEmpty(animName) && PlayerVisuals.SpriteFrames.HasAnimation(animName))
		{
			PlayerVisuals.Play(animName);
		}
	}
}
