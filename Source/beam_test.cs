using System.Threading.Tasks;
using Godot;
using GodotBeamable.BeamGodot;
using Newtonsoft.Json;

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
			// updateAlias.Connect("pressed", this, nameof(HandleAliasBtnPressed));
			label.Text = "Connecting...";
			
			await Init();
		}

		async Task Init()
		{
			// inputAlias.Readonly = true;
			updateAlias.Disabled = true;
			_beamGodotContext = await BeamGodotContext.GetOrCreate();
			if (_beamGodotContext.TryGetStatFromCache("alias", out var alias))
			{
				inputAlias.Text = alias;
			}
			UpdateLabel();
			var test = await _beamGodotContext.Requester.GetManifest();

			const string leaderboardID = "leaderboards.New_LeaderboardContent";

			// TODO fix update score, displaying works
			await _beamGodotContext.Requester.UpdateBoardScore(leaderboardID, _beamGodotContext.UserId, 11.0);

			// GD.Print(JsonConvert.SerializeObject(test));
			var leaderboards = await _beamGodotContext.Requester.GetBoardScores(leaderboardID, 0, 50);
			GD.Print(JsonConvert.SerializeObject(leaderboards));
		}

		private void UpdateLabel()
		{
			// inputAlias.Readonly = false;
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
			// inputAlias.Readonly = true;
			updateAlias.Disabled = true;
			_beamGodotContext.SetPublicStat("alias", inputAlias.Text).Then(response =>
			{
				_beamGodotContext.GetStatsFromServer().Then(unit => UpdateLabel());
			});
		}
	}
}



