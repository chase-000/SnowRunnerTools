using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SnowPakTool;

namespace SnowTruckConfig {

	public static class Program {

		/*
			truck CustomizationCameras --FOV:40 "D:\Games\SnowRunner_backs\mods\.staging\initial-pak\[media]\classes\trucks\ank_mk38.xml"
		*/

		private static readonly Regex __NewTireIdName = new Regex ( @"^(.+?)_X_[0-9A-F]{8}$" , RegexOptions.Compiled );



		public static int Main ( string[] args ) {

			var root = new RootCommand ( typeof ( Program ).Assembly.GetCustomAttribute<AssemblyTitleAttribute> ().Title );
			root.AddLicenseOption ();


			var cmdTruck = new Command ( "truck" , "Trucks configuration" );
			root.Add ( cmdTruck );

			var cmdTruckCustomizationCameras = new Command ( "CustomizationCameras" );
			cmdTruckCustomizationCameras.AddOption ( new Option<int> ( "--FOV" ) { Required = true } );
			cmdTruckCustomizationCameras.AddArgument ( new Argument<FileInfo> ( "targetXml" ).ExistingOnly () );
			cmdTruckCustomizationCameras.Handler = CommandHandler.Create<FileInfo , int> ( DoTruckCustomizationCameras );
			cmdTruck.Add ( cmdTruckCustomizationCameras );


			var cmdExtras = new Command ( "extras" , "Extra configuration" );
			root.Add ( cmdExtras );

			var cmdExtrasRename = new Command ( "rename" , "Rename game objects" );
			cmdExtras.Add ( cmdExtrasRename );

			var cmdExtrasRenameTires = new Command ( "tires" , "Rename tires" );
			cmdExtrasRenameTires.AddOption ( new Option<GameLanguage> ( "--language" ) { Required = true } );
			cmdExtrasRenameTires.AddArgument ( new Argument<DirectoryInfo> ( "directory" , "Path to the directory with mixed contents of initial.pak and initial.pak\\initial.cache_block" ).ExistingOnly () );
			cmdExtrasRenameTires.Handler = CommandHandler.Create<DirectoryInfo , GameLanguage> ( DoExtrasRenameTires );
			cmdExtrasRename.Add ( cmdExtrasRenameTires );


			return root.InvokeWithMiddleware ( args , CommandLineExtensions.MakePrintLicenseResourceMiddleware ( typeof ( Program ) ) );
		}

		private static void DoTruckCustomizationCameras ( FileInfo targetXml , int fov ) {
			var xml = XmlHelpers.ReadFragments ( targetXml.FullName );
			SetCustomizationCamerasFov ( xml , fov );
			XmlHelpers.WriteFragments ( targetXml.FullName , xml.Nodes () );
		}

		private static void SetCustomizationCamerasFov ( XElement xml , int fov ) {
			var positions = xml.Element ( "Truck" ).Element ( "GameData" ).Element ( "CustomizationCameras" ).Elements ( "CameraPos" );
			foreach ( var position in positions ) {
				position.Attribute ( "FOV" ).SetValue ( fov );
			}
		}

		private static void DoExtrasRenameTires ( DirectoryInfo directory , GameLanguage language ) {
			Console.WriteLine ( "Reading templates." );
			var templates = XmlHelpers.ReadFragments ( Path.Combine ( directory.FullName , @"[media]\_templates\trucks.xml" ) )
				.Element ( "_templates" )
				.Element ( "WheelFriction" )
				.Elements ()
				.Select ( a => new TireInfo ( a.Name.LocalName , a ) )
				.ToDictionary ( a => a.Id )
				;
			Console.WriteLine ( $"{templates.Count} entries read." );

			Console.WriteLine ( "Reading tires and issuing new IDs." );
			var tires = PrepareAndGetTires ( directory , templates );
			Console.WriteLine ( $"{tires.Count} new tire name IDs created." );

			Console.WriteLine ( $"Reading localization strings for: {language}." );
			var stringsLocation = Path.Combine ( directory.FullName , @$"[strings]\strings_{language}.str".ToLowerInvariant () );
			var strings = StringsFile.ReadStrings ( stringsLocation ).ToList ();
			Console.WriteLine ( $"{strings.Count} entries read." );

			Console.WriteLine ( "Creating new tire names." );
			var stringsMap = strings.GroupBy ( a => a.Key ).ToDictionary ( a => a.Key , a => a.First ().Value ); //there are multiple duplicates
			var newStrings = strings
				.Where ( a => !__NewTireIdName.IsMatch ( a.Key ) ) //filter out new ids
				.Concat ( tires
					.OrderBy ( a => a.NewId )
					.Select ( a => new KeyValuePair<string , string> (
						 a.NewId ,
						 GetNewTireName ( stringsMap[a.Id] , a )
					)
				) )
				.ToList ()
				;

			Console.WriteLine ( $"Writing localization strings." );
			StringsFile.WriteStrings ( stringsLocation , newStrings , overwrite: true );
			Console.WriteLine ( "Done." );
		}

		private static List<TireInfo> PrepareAndGetTires ( DirectoryInfo directory , Dictionary<string , TireInfo> templates ) {
			var directoryLocation = IOHelpers.NormalizeDirectory ( directory.FullName );
			var xmlLocations = Directory.EnumerateFiles ( Path.Combine ( directoryLocation , @"[media]\classes\wheels" ) , "*.xml" );
			var newTires = new List<TireInfo> ();

			foreach ( var xmlLocation in xmlLocations ) {
				var crc = MiscHelpers.ComputeUtf8Crc32 ( xmlLocation[directoryLocation.Length..] );
				var root = XmlHelpers.ReadFragments ( xmlLocation );
				var tires = root.Elements ( "TruckWheels" ).Elements ( "TruckTires" ).Elements ( "TruckTire" ).ToList ();
				if ( tires.Count == 0 ) continue;

				foreach ( var tire in tires ) {
					var id = tire.Element ( "GameData" )?.Element ( "UiDesc" )?.Attribute ( "UiName" );
					if ( id == null ) continue;
					var friction = tire.Element ( "WheelFriction" );
					if ( friction == null ) continue;

					var match = __NewTireIdName.Match ( id.Value );
					var originalId = match.Success ? match.Groups[1].Value : id.Value;
					var newId = $"{originalId}_X_{crc:X8}"; //generate new ids to make them unique (there are duplicates in original files)
					id.Value = newId;

					var template = friction.Attribute ( "_template" )?.Value;
					var templateValues = template == null ? null : templates[template];
					newTires.Add ( new TireInfo ( originalId , newId , friction , templateValues ) );
				}
				XmlHelpers.WriteFragments ( xmlLocation , root.Nodes () );
			}

			return newTires;
		}

		private static string GetNewTireName ( string originalName , TireInfo tire ) {
			return $"{originalName} A{tire.Asphalt:0.#}/D{tire.Dirt:0.#}/M{tire.Mud:0.#}";
		}

		private sealed class TireInfo {

			public TireInfo ( string id , string newId , XElement element , TireInfo template ) {
				Id = id;
				NewId = newId;
				Dirt = float.TryParse ( element.Attribute ( "BodyFriction" )?.Value , out var dirt ) ? dirt : template?.Dirt ?? 0;
				Mud = float.TryParse ( element.Attribute ( "SubstanceFriction" )?.Value , out var mud ) ? mud : template?.Mud ?? 0;
				Asphalt = float.TryParse ( element.Attribute ( "BodyFrictionAsphalt" )?.Value , out var asphalt ) ? asphalt : template?.Asphalt ?? 0;
			}

			public TireInfo ( string id , XElement element ) : this ( id , null , element , null ) {
			}

			public string Id { get; }
			public string NewId { get; }
			public float Dirt { get; }
			public float Mud { get; }
			public float Asphalt { get; }

		}

	}

}
