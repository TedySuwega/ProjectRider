using Godot;

namespace ProjectRider.Forms.HeroForm;

[GlobalClass]
public partial class HeroForm : BaseForm, ICombatant
{
	[Export] public string AttackAnim { get; set; } = "attack_hero";
}
