using System.Threading.Tasks;
using Godot;
using GodotBeamable.BeamGodot;

namespace GodotBeamable
{
	public partial class beam_test : Node
	{
		[Export]Label label;
		[Export]TextEdit inputAlias;
		[Export]Button updateAlias;
		private BeamGodotContext _beamGodotContext;

		public override async void _Ready()
		{
			updateAlias.ButtonUp += HandleAliasBtnPressed;
			label.Text = "Connecting...";
			
			await Init();
		}

		async Task Init()
		{
			inputAlias.Text = string.Empty;
			inputAlias.Editable = false;
			updateAlias.Disabled = true;
			
			
			_beamGodotContext = BeamGodotContext.Default;
			await _beamGodotContext.OnReady;

			if (_beamGodotContext.TryGetStatFromCache("alias", out var alias))
			{
				inputAlias.Text = alias;
			}
			
			UpdateLabel();
		}

		private void UpdateLabel()
		{
			inputAlias.Editable = true;
			updateAlias.Disabled = false;
			if(_beamGodotContext.TryGetStatFromCache("alias", out string alias))
			{
				label.Text = $"Beamable User ID: {_beamGodotContext.UserId} with alias: {alias}";
			}
			else
			{
				label.Text = $"Beamable User ID: {_beamGodotContext.UserId}";
			}
		}

		private void HandleAliasBtnPressed()
		{
			inputAlias.Editable = false;
			updateAlias.Disabled = true;
			_beamGodotContext.SetPublicStat("alias", inputAlias.Text).Then(response =>
			{
				_beamGodotContext.GetStatsFromServer().Then(unit => UpdateLabel());
			});
		}
	}
}



