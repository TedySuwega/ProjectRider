using Godot;
using System;

namespace ProjectRider.States;

public partial class RiderKickState : PlayerState
{
	private float _kickDirX;
	private bool _isDescending = false;
	private float _anticipationTimer = 0.12f; // Efek freeze sekejap di udara sebelum meluncur

	public override bool LocksVisuals => true; // Kunci arah sprite selama menendang

	public RiderKickState(Player player) : base(player) { }

	public override void Enter()
	{
		// Tentukan arah tendangan berdasarkan arah hadap sprite player saat ini
		_kickDirX = _player.GetFacingDirection();

		var velocity = _player.WorkingVelocity;

		if (_player.IsOnFloor())
		{
			// JIKA DI TANAH: Lompat ke atas depan dulu sebagai ancang-ancang
			velocity.Y = _player.CurrentForm.JumpVelocity * 1.1f; // Agak lebih tinggi dari lompat biasa
			velocity.X = _kickDirX * (_player.CurrentForm.Speed * 0.8f);
			_player.WorkingVelocity = velocity;
			
			_player.PlayAnimationSafely(_player.CurrentForm.JumpAnim);
			_isDescending = false;
		}
		else
		{
			// JIKA DI UDARA: Langsung masuk fase freeze ancang-ancang
			velocity = Vector2.Zero;
			_player.WorkingVelocity = velocity;
			
			_player.PlayAnimationSafely(_player.RiderKickAnim);
			_isDescending = true;
		}
	}

	public override void Update(double delta)
	{
		// Fase 1: Jika dipicu dari tanah, tunggu sampai koordinat Y mulai turun (mencapai puncak lompatan)
		if (!_isDescending && !_player.IsOnFloor() && _player.WorkingVelocity.Y >= -50)
		{
			_isDescending = true;
			_player.PlayAnimationSafely(_player.RiderKickAnim);
		}

		// Fase 2: Eksekusi seluncuran menukik 45 derajat
		if (_isDescending)
		{
			var velocity = _player.WorkingVelocity;
			
			if (_anticipationTimer > 0)
			{
				// Kasih jeda freeze sekejap biar kerasa hantaman tenaganya
				_anticipationTimer -= (float)delta;
				velocity = Vector2.Zero;
			}
			else
			{
				// Menghitung vektor kecepatan menggunakan trigonometri berdasarkan sudut derajat
				// Mathf.DegToRad mengubah derajat (misal 50) menjadi radian yang dibutuhkan fungsi Cos dan Sin
				float angleRad = Mathf.DegToRad(_player.RiderKickAngle);
				
				// Menukik: Cos(sudut) untuk Horizontal X, Sin(sudut) untuk Vertikal Y
				velocity.X = _kickDirX * _player.RiderKickSpeed * Mathf.Cos(angleRad);
				velocity.Y = _player.RiderKickSpeed * Mathf.Sin(angleRad); // Positif berarti ke bawah di koordinat Godot
			}
			
			_player.WorkingVelocity = velocity;
		}
		else
		{
			// Jika dipicu dari tanah dan belum mencapai puncak, tetap jalankan gravitasi normal dulu
			_player.ApplyGravity(delta);
		}
	}

	public override void PhysicsPostMove(double delta)
	{
		// Kondisi keluar: Begitu kaki menapak tanah, tendangan selesai!
		if (_isDescending && _anticipationTimer <= 0 && _player.IsOnFloor())
		{
			GD.Print("Rider Kick LANDED! Boom!");
			// Di sini nanti tempat trigger particle ledakan tanah dan damage area
			
			_player.ResetJumpCount();
			_player.ChangeState(new IdleState(_player));
		}
	}
}
