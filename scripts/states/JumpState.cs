using Godot;

namespace ProjectRider.States;

public partial class JumpState : PlayerState
{
	private bool _airborne;

	public JumpState(Player player) : base(player) { }

	public override void Enter()
	{
		_airborne = false;
		_player.ApplyJumpFromState();
		var v = _player.WorkingVelocity;
		v.Y = _player.CurrentForm.JumpVelocity;
		_player.WorkingVelocity = v;
		_player.PlayAnimationSafely(_player.CurrentForm.JumpAnim);
	}

	public override void Update(double delta)
	{
		if (!_player.IsOnFloor())
			_airborne = true;

		_player.ApplyGravity(delta);
		_player.HandleWallMovement(delta);

		if (Input.IsActionJustReleased("jump"))
			_player.ApplyJumpCut();

		_player.ApplyHorizontalLocomotion(delta);
		_player.HandleSlide();
		_player.HandleAttack();
		_player.HandleFormSwitchInput();
	}

	public override void PhysicsPostMove(double delta)
	{
		if (_airborne && _player.IsOnFloor())
			_player.ChangeState(new IdleState(_player));
	}
}
