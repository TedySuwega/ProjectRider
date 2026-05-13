using Godot;

namespace ProjectRider.States;

public abstract partial class PlayerState : RefCounted
{
	protected Player _player;

	public virtual bool LocksVisuals => false;

	public PlayerState(Player player)
	{
		_player = player;
	}

	// Dipanggil saat masuk ke state ini (seperti _Ready)
	public virtual void Enter() { }

	// Dipanggil setiap frame (seperti _PhysicsProcess), sebelum MoveAndSlide
	public virtual void Update(double delta) { }

	// Dipanggil setelah MoveAndSlide (misalnya deteksi mendarat)
	public virtual void PhysicsPostMove(double delta) { }

	// Dipanggil sebelum pindah ke state lain
	public virtual void Exit() { }
}
