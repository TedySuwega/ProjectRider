using Godot;
using ProjectRider.Forms.HeroForm;

namespace ProjectRider.States;

public partial class JumpState : PlayerState
{
	private bool _airborne;
	private bool _isDoubleJump;

	public JumpState(Player player, bool isDoubleJump = false) : base(player) 
	{ 
		_isDoubleJump = isDoubleJump;
	}

	public override void Enter()
	{
		_airborne = false;
		
		if (!_isDoubleJump)
		{
			_player.ApplyJumpFromState();
			
			// Set jump count menjadi 1 karena ini lompatan pertama
			_player.JumpCount = 1; 

			var v = _player.WorkingVelocity;
			v.Y = _player.CurrentForm.JumpVelocity;
			_player.WorkingVelocity = v;
			_player.PlayAnimationSafely(_player.CurrentForm.JumpAnim);
		}
		else
		{
			_player.ExecuteDoubleJump();
		}
	}

	public override void Update(double delta)
	{
		if (!_player.IsOnFloor())
			_airborne = true;

		_player.ApplyGravity(delta);
		_player.HandleWallMovement(delta);

		if (Input.IsActionJustReleased("jump"))
			_player.ApplyJumpCut();

		// --- TAMBAHKAN LOGIKA DETEKSI DOUBLE JUMP DI SINI ---
		if (Input.IsActionJustPressed("jump"))
		{
			if (!_player.IsOnFloor() && (_player.CurrentForm == _player.HeroData2 || _player.CurrentForm == _player.HeroData4) && _player.JumpCount == 1)
			{
				_player.ExecuteDoubleJump();
			}
		}
		// ----------------------------------------------------

		_player.ApplyHorizontalLocomotion(delta);
		_player.HandleSlide();
		_player.HandleAttack();
		_player.HandleFormSwitchInput();

		if (Input.IsActionJustPressed("ulti_kick") && _player.CurrentForm is HeroForm)
		{
			_player.ChangeState(new RiderKickState(_player));
			return;
		}
	}

	public override void PhysicsPostMove(double delta)
	{
		if (_airborne && _player.IsOnFloor())
		{
			_player.ResetJumpCount(); // Pastikan di-reset saat mendarat
			_player.ChangeState(new IdleState(_player));
		}
	}
}
