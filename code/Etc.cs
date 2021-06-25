using System;

public static class Etc
{
	public readonly struct HSV
	{
		/// <summary>
		/// Hue in degrees (0 - 360)
		/// </summary>
		public readonly float H;
		public readonly float S;
		public readonly float V;

		public HSV(float h, float s, float v)
		{
			while (h < 0) { h += 360.0f; }
			while (h > 360) { h -= 360.0f; }
			H = h;
			S = s;
			V = v;
		}

		public static HSV FromRadians(float h, float s, float v)
		{
			return new HSV(h * 180 / (float) Math.PI, s, v);
		}

		public Color ToColor()
		{
			int hi = Convert.ToInt32( Math.Floor( H / 60 ) ) % 6;
			var f = H / 60 - (float)Math.Floor( H / 60 );

			var v = V;
			var p = v * (1 - S);
			var q = v * (1 - f * S);
			var t = v * (1 - (1 - f) * S);

			if ( hi == 0 )
				return new Color( v, t, p );
			else if ( hi == 1 )
				return new Color( q, v, p );
			else if ( hi == 2 )
				return new Color( p, v, t );
			else if ( hi == 3 )
				return new Color( p, q, v );
			else if ( hi == 4 )
				return new Color( t, p, v );
			else
				return new Color( v, p, q );
		}
	}
}
