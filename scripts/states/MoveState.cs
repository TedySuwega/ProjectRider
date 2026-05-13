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
		_player.ApplyGravity(delta);
		_player.HandleWallMovement(delta);
		_player.HandleCrawl();

		if (Input.IsActionJustPressed("jump") && _player.CanCoyoteJump())
		{
			_player.ChangeState(new JumpState(_player));
			return;
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
		_player.HandleHenshin();
	}
}
