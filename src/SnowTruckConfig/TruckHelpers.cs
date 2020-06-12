using System.Linq;
using System.Xml.Linq;

namespace SnowTruckConfig {

	public static class TruckHelpers {

		public static (float minX, float minY, float minZ, float maxX, float maxY, float maxZ) GetExtents ( XElement truck ) {
			var positions = truck.Descendants ().Attributes ( "Pos" ).Select ( a => ParsePos ( (string) a ) ).ToList ();
			return (
				positions.Min ( a => a.X ),
				positions.Min ( a => a.Y ),
				positions.Min ( a => a.Z ),
				positions.Max ( a => a.X ),
				positions.Max ( a => a.Y ),
				positions.Max ( a => a.Z )
			);
		}

		public static (float X, float Y, float Z) ParsePos ( string value ) {
			if ( !string.IsNullOrEmpty ( value ) ) {
				var xStart = value.IndexOf ( '(' ) + 1;
				if ( xStart > 0 ) {
					var yStart = value.IndexOf ( ';' , xStart ) + 1;
					if ( yStart > 0 && float.TryParse ( value[xStart..( yStart - 1 )] , out var x ) ) {
						var zStart = value.IndexOf ( ';' , yStart ) + 1;
						if ( zStart > 0 && float.TryParse ( value[yStart..( zStart - 1 )] , out var y ) ) {
							var zEnd = value.IndexOf ( ')' , zStart );
							if ( zStart >= 0 && float.TryParse ( value[zStart..zEnd] , out var z ) ) {
								return (x, y, z);
							}
						}
					}
				}
			}
			return (0, 0, 0);
		}

		public static float GetMass ( XElement truck ) {
			var massNodes = truck.Element ( "PhysicsModel" )?.Descendants ( "Body" ).Attributes ( "Mass" );
			if ( massNodes == null ) return 0;
			var mass = massNodes.Select ( a => float.TryParse ( a.Value , out var f ) ? f : 0 ).Sum ();
			return mass;
		}

	}

}
