using Godot;

namespace RiderProject.Forms;

[GlobalClass]
public partial class HeroForm : BaseForm, ICombatant
{
	[Export] public string AttackAnim { get; set; } = "attack_hero";
}
