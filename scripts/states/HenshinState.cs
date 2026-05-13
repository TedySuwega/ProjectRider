using Godot;
using ProjectRider.Forms;

namespace ProjectRider.States;

public partial class HenshinState : PlayerState
{
	private string _henshinAnimation;

	public override bool LocksVisuals => true;

	public HenshinState(Player player) : base(player) { }

	public override void Enter()
	{
		BaseForm nextForm = _player.CurrentForm == _player.HumanData ? _player.HeroData : _player.HumanData;
		if (nextForm == null)
		{
			_player.ChangeState(GetNextState());
			return;
		}

		_player.CurrentForm = nextForm;
		_henshinAnimation = _player.CurrentForm.HenshinAnim;

		var velocity = _player.WorkingVelocity;
		velocity.X = 0;
		_player.WorkingVelocity = velocity;

		if (string.IsNullOrEmpty(_henshinAnimation))
		{
			_player.ChangeState(GetNextState());
			return;
		}

		_player.PlayAnimationSafely(_henshinAnimation);
		_player.PlayerVisuals.Frame = 0;
	}

	public override void Update(double delta)
	{
		_player.ApplyGravity(delta);
		_player.HandleWallMovement(delta);
		BrakeHorizontalMovement();

		if (IsHenshinAnimationFinished())
			_player.ChangeState(GetNextState());
	}

	private void BrakeHorizontalMovement()
	{
		var velocity = _player.WorkingVelocity;
		velocity.X = Mathf.MoveToward(velocity.X, 0, _player.CurrentForm.Speed);
		_player.WorkingVelocity = velocity;
	}

	private bool IsHenshinAnimationFinished()
	{
		if (string.IsNullOrEmpty(_henshinAnimation))
			return true;

		int frameCount = _player.PlayerVisuals.SpriteFrames.GetFrameCount(_henshinAnimation);
		return _player.PlayerVisuals.Frame >= frameCount - 1;
	}

	private PlayerState GetNextState()
	{
		if (!_player.IsOnFloor() || Input.GetAxis("move_left", "move_right") != 0)
			return new MoveState(_player);

		return new IdleState(_player);
	}
}
