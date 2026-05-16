using Godot;
using ProjectRider.Forms;

namespace ProjectRider.States;

public partial class HenshinState : PlayerState
{
	private string _henshinAnimation;
	private BaseForm _targetForm; // Tambahkan variabel penampung

	public override bool LocksVisuals => true;

	// MODIFIKASI CONSTRUCTOR: Menerima parameter targetForm
	public HenshinState(Player player, BaseForm targetForm) : base(player)
	{
		_targetForm = targetForm;
	}

	public override void Enter()
	{
		// Validasi jika data resource kosong di Inspector
		if (_targetForm == null)
		{
			_player.ChangeState(GetNextState());
			return;
		}

		// Terapkan form baru ke player
		_player.CurrentForm = _targetForm;
		_henshinAnimation = _player.CurrentForm.HenshinAnim;

		var velocity = _player.WorkingVelocity;
		velocity.X = 0;
		_player.WorkingVelocity = velocity;

		// Jika animasi henshin kosong (misal untuk placeholder Hero 2 & 3), 
		// langsung pindah ke state berikutnya tanpa nunggu animasi selesai
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