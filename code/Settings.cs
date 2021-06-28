using Sandbox;

public class Settings
{
	public static Settings Instance
	{
		get
		{
			Host.AssertClient();
			if ( instance == null )
				instance = new Settings();
			return instance;
		}
	}

	public float AllyColor = 240.0f;
	public float EnemyColor = 0.0f;

	private static Settings instance;

	public Color GetPlayerColor( SMLSBasePlayer player )
	{
		if ( player.Team == SMLSBasePlayer.PlayerTeam.Spectator )
			return Color.White;
		var p = Local.Pawn as SMLSBasePlayer;

		if ( player.Team != SMLSBasePlayer.PlayerTeam.FFA )
			return new Etc.HSV( player.Team == p.Team ? AllyColor : EnemyColor, 1.0f, 1.0f ).ToColor();
		else
			return new Etc.HSV( player == p ? AllyColor : EnemyColor, 1.0f, 1.0f ).ToColor();
	}
}
