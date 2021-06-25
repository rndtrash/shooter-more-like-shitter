using Sandbox;

public class Settings
{
	public static Settings Instance {
		get
		{
			Host.AssertClient();
			if ( instance == null )
				instance = new Settings();
			return instance;
		}
	}

	public float AllyColor = 180.0f;
	public float EnemyColor = 0.0f;

	private static Settings instance;

	public Color GetPlayerColor(SMLSPlayer player)
	{
		if ( player.PlayerTeam == SMLSPlayer.Team.Spectator )
			return Color.White;
		var p = Local.Pawn as SMLSPlayer;

		if ( player.PlayerTeam != SMLSPlayer.Team.FFA )
			return new Etc.HSV( player.PlayerTeam == p.PlayerTeam ? AllyColor : EnemyColor, 1.0f, 1.0f ).ToColor();
		else
			return new Etc.HSV( player == p ? AllyColor : EnemyColor, 1.0f, 1.0f ).ToColor();
	}
}
