using Godot;
using ProjectRider.Forms;

namespace ProjectRider.States;

public partial class AttackState : PlayerState
{
	private readonly ICombatant _combatForm;
	private bool _queuedNextStrike;

	public override bool LocksVisuals => true;

	public AttackState(Player player) : base(player)
	{
		_combatForm = _player.CurrentForm as ICombatant;
	}

	public override void Enter()
	{
		if (_combatForm == null)
		{
			_player.ChangeState(new IdleState(_player));
			return;
		}

		StartStrike();
	}

	public override void Update(double delta)
	{
		_player.ApplyGravity(delta);
		_player.HandleWallMovement(delta);
		BrakeHorizontalMovement();

		if (Input.IsActionJustPressed("attack"))
			_queuedNextStrike = true;

		if (!IsCurrentAnimationFinished())
			return;

		if (_queuedNextStrike)
		{
			_queuedNextStrike = false;
			StartStrike();
			return;
		}

		_player.ChangeState(GetNextState());
	}

	private void StartStrike()
	{
		_player.ExecuteComboStrike();
	}

	private void BrakeHorizontalMovement()
	{
		var velocity = _player.WorkingVelocity;
		velocity.X = Mathf.MoveToward(velocity.X, 0, _player.CurrentForm.Speed);
		_player.WorkingVelocity = velocity;
	}

	private bool IsCurrentAnimationFinished()
	{
		string currentAnimation = _player.PlayerVisuals.Animation;
		if (string.IsNullOrEmpty(currentAnimation))
			return true;

		int frameCount = _player.PlayerVisuals.SpriteFrames.GetFrameCount(currentAnimation);
		return _player.PlayerVisuals.Frame >= frameCount - 1;
	}

	private PlayerState GetNextState()
	{
		if (!_player.IsOnFloor() || Input.GetAxis("move_left", "move_right") != 0)
			return new MoveState(_player);

		return new IdleState(_player);
	}
}
