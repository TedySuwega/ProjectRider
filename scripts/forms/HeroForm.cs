using Godot;

namespace ProjectRider.Forms.HeroForm;

[GlobalClass]
public partial class HeroForm : BaseForm, ICombatant
{
	// [Export] public string AttackAnim { get; set; } = "attack_hero";
	[Export] public string[] HeroCombos = { "attack_combo_1", "attack_combo_2", "attack_combo_3" };
	public string[] ComboAnimations => HeroCombos;
}
