using Godot;

namespace ProjectRider.States;

public partial class MoveState : PlayerState
{
	public MoveState(Player player) : base(player) { }

	public override void Enter()
	{
		_player.PlayAnimationSafely(_player.CurrentForm.WalkAnim);
	}

	public override void Update(double delta)
	{
		if (_player.IsOnFloor())
		{
			_player.ResetJumpCount();
		}
		else if (_player.JumpCount == 0 && !_player.CanCoyoteJump())
		{
			_player.JumpCount = 1;
		}

		_player.ApplyGravity(delta);
		_player.HandleWallMovement(delta);
		_player.HandleCrawl();

		if (Input.IsActionJustPressed("jump"))
		{
			if (_player.CanCoyoteJump())
			{
				_player.ChangeState(new JumpState(_player));
				return;
			}
			else if (!_player.IsOnFloor() && _player.CurrentForm == _player.HeroData2 && _player.JumpCount == 1)
			{
				_player.ChangeState(new JumpState(_player, true));
				return;
			}
		}

		float directionX = Input.GetAxis("move_left", "move_right");
		if (directionX == 0f && _player.IsOnFloor())
		{
			_player.ChangeState(new IdleState(_player));
			return;
		}

		_player.ApplyHorizontalLocomotion(delta);
		_player.HandleSlide();
		_player.HandleAttack();
		_player.HandleFormSwitchInput();
	}
}
