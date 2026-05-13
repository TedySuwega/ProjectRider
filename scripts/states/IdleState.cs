using Godot;

namespace ProjectRider.States;

public partial class IdleState : PlayerState
{
	public IdleState(Player player) : base(player) { }

	public override void Enter()
	{
		_player.WorkingVelocity = Vector2.Zero;
		_player.PlayAnimationSafely(_player.CurrentForm.IdleAnim);
	}

	public override void Update(double delta)
	{
		_player.ApplyGravity(delta);
		_player.HandleWallMovement(delta);
		_player.HandleCrawl();

		if (!_player.IsOnFloor())
		{
			_player.ChangeState(new MoveState(_player));
			return;
		}

		if (Input.IsActionJustPressed("jump") && _player.CanCoyoteJump())
		{
			_player.ChangeState(new JumpState(_player));
			return;
		}

		if (Input.GetAxis("move_left", "move_right") != 0)
		{
			_player.ChangeState(new MoveState(_player));
			return;
		}

		_player.ApplyHorizontalLocomotion(delta);
		_player.HandleSlide();
		_player.HandleAttack();
		_player.HandleHenshin();
	}
}
