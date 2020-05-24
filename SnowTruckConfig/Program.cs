using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SnowTruckConfig {

	public static class Program {

		/*
			/CustomizationCameras /FOV:40 "D:\Games\SnowRunner_backs\mods\.staging\initial-pak\[media]\classes\trucks\ank_mk38.xml"
		*/

		public static int Main ( string[] args ) {
			switch ( args.FirstOrDefault () ) {

				case "/license":
					PrintLicense ();
					return 0;

				case "/CustomizationCameras":
					return DoCustomizationCameras ( args.Skip ( 1 ).ToArray () );

				default:
					return PrintHelp ();
			}
		}

		private static void PrintLicense () {
			using var stream = typeof ( Program ).Assembly.GetManifestResourceStream ( $"{nameof ( SnowTruckConfig )}.LICENSE" );
			using var reader = new StreamReader ( stream );
			Console.WriteLine ( reader.ReadToEnd () );
		}

		private static int PrintHelp () {
			Console.WriteLine ( "Usage:" );
			Console.WriteLine ( $"  {nameof ( SnowTruckConfig )} /license" );
			Console.WriteLine ( $"  {nameof ( SnowTruckConfig )} /CustomizationCameras switches file.xml" );
			Console.WriteLine ( $"    switches: /FOV:value" );
			return 1;
		}

		private static int DoCustomizationCameras ( string[] args ) {
			if ( args.Length < 2 ) return PrintHelp ();
			return DoCustomizationCameras ( args.Take ( args.Length - 1 ) , args.Last () );
		}

		private static int DoCustomizationCameras ( IEnumerable<string> switches , string xmlLocation ) {
			var xml = XmlHelpers.ReadFragments ( xmlLocation );
			foreach ( var item in switches ) {
				ApplySwitch ( item , xml );
			}
			XmlHelpers.WriteFragments ( xmlLocation , xml.Nodes () );
			return 0;
		}

		private static readonly Dictionary<string , Action<string , XElement>> __CustomizationCamerasSwitches = new Dictionary<string , Action<string , XElement>> {
			["FOV"] = SetCustomizationCamerasFov ,
		};

		private static void SetCustomizationCamerasFov ( string value , XElement xml ) {
			var positions = xml.Element ( "Truck" ).Element ( "GameData" ).Element ( "CustomizationCameras" ).Elements ( "CameraPos" );
			foreach ( var position in positions ) {
				position.Attribute ( "FOV" ).SetValue ( value );
			}
		}

		private static void ApplySwitch ( string sw , XElement xml ) {
			if ( !ParseSwitch ( sw , out var switchName , out var value )
							|| !__CustomizationCamerasSwitches.TryGetValue ( switchName , out var action ) ) {
				Console.WriteLine ( $"Skipping unknown switch: {sw}" );
				return;
			}
			action ( value , xml );
		}

		private static bool ParseSwitch ( string sw , out string name , out string value ) {
			name = null;
			value = null;
			if ( sw.Length < 2 || sw[0] != '/' ) return false;
			var index = sw.IndexOf ( ':' , 1 );
			if ( index < 0 ) {
				name = sw.Substring ( 1 );
			}
			else {
				name = sw.Substring ( 1 , index - 1 );
				value = sw.Substring ( index + 1 );
			}
			return true;
		}

	}

}
