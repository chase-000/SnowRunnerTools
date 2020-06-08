using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SnowPakTool;

namespace SnowTruckConfig {

	public static class ExtrasRenamer {

		public static Regex RenamedStringIdRegex { get; } = new Regex ( @"^(.+?)_X_[0-9A-F]{8}$" , RegexOptions.Compiled );


		public static void RenameTires ( DirectoryInfo directory , GameLanguage language ) {
			Console.WriteLine ( "Reading global friction templates." );
			var globalFrictionTemplates = XmlHelpers.ReadFragments ( Path.Combine ( directory.FullName , @"[media]\_templates\trucks.xml" ) )
				.Element ( "_templates" )
				.Element ( "WheelFriction" )
				.Elements ()
				.Select ( a => new TireFriction ( a.Name.LocalName , a ) )
				.ToDictionary ( a => a.Id )
				;
			Console.WriteLine ( $"{globalFrictionTemplates.Count} entries read." );

			Console.WriteLine ( "Reading tires and issuing new IDs." );
			var tires = PrepareAndGetTires ( directory , globalFrictionTemplates );
			Console.WriteLine ( $"{tires.Count} new tire name IDs created." );

			Console.WriteLine ( $"Reading localization strings for: {language}." );
			var strings = GetStrings ( directory , language , out var stringsMap , out var stringsLocation );

			Console.WriteLine ( "Creating new tire names." );
			var newTireNames = tires
				.OrderBy ( a => a.NewId )
				.Select ( a => Pair.From ( a.NewId , GetNewTireName ( stringsMap[a.Id] , a ) ) )
				.ToList ()
				;

			Console.WriteLine ( $"Writing localization strings." );
			var newStrings = strings
				.Concat ( newTireNames )
				;
			StringsFile.WriteStrings ( stringsLocation , newStrings , overwrite: true );
			Console.WriteLine ( "Done." );
		}



		private static List<KeyValuePair<string , string>> GetStrings ( DirectoryInfo directory , GameLanguage language , out Dictionary<string , string> map , out string location ) {
			location = Path.Combine ( directory.FullName , @$"[strings]\strings_{language}.str".ToLowerInvariant () );
			var strings = StringsFile.ReadStrings ( location )
				.Where ( a => !RenamedStringIdRegex.IsMatch ( a.Key ) ) //filter out new ids
				.ToList ()
				;
			map = strings
				.GroupBy ( a => a.Key ) //there are multiple duplicates
				.ToDictionary ( a => a.Key , a => a.First ().Value )
				;
			return strings;
		}

		private static string GetNewId ( string originalId , string unique ) {
			var crc = MiscHelpers.ComputeUtf8Crc32 ( unique );
			return $"{originalId}_X_{crc:X8}"; //generate new ids to separate them from originals and to make them unique (there's reuse in original files)
		}

		private static List<TireFriction> PrepareAndGetTires ( DirectoryInfo directory , Dictionary<string , TireFriction> globalFrictionTemplates ) {
			var directoryLocation = IOHelpers.NormalizeDirectory ( directory.FullName );
			var xmlLocations = Directory.EnumerateFiles ( Path.Combine ( directoryLocation , @"[media]\classes\wheels" ) , "*.xml" );
			var newTires = new List<TireFriction> ();

			foreach ( var xmlLocation in xmlLocations ) {
				var root = XmlHelpers.ReadFragments ( xmlLocation );

				var templates = new Dictionary<string , TireFriction> ( globalFrictionTemplates );
				var localTemplateElements = root.Element ( "_templates" )?.Elements ( "TruckTire" ).Elements ().Elements ( "WheelFriction" );
				if ( localTemplateElements != null ) {
					foreach ( var item in localTemplateElements ) {
						var id = item.Parent.Name.LocalName;
						templates[id] = new TireFriction ( id , item , templates[item.Attribute ( "_template" ).Value] );
					}
				}

				var tires = root.Elements ( "TruckWheels" ).Elements ( "TruckTires" ).Elements ( "TruckTire" ).ToList ();
				if ( tires.Count == 0 ) continue;

				foreach ( var tire in tires ) {
					var id = tire.Element ( "GameData" )?.Element ( "UiDesc" )?.Attribute ( "UiName" );
					if ( id == null ) continue;

					var match = RenamedStringIdRegex.Match ( id.Value );
					var originalId = match.Success ? match.Groups[1].Value : id.Value;
					var newId = GetNewId ( originalId , xmlLocation[directoryLocation.Length..] );
					id.Value = newId;

					var localFriction = tire.Element ( "WheelFriction" );
					var templateId = localFriction?.Attribute ( "_template" )?.Value ?? tire?.Attribute ( "_template" )?.Value;
					var template = templateId == null ? null : templates[templateId];
					newTires.Add ( new TireFriction ( originalId , newId , localFriction , template ) );
				}
				XmlHelpers.WriteFragments ( xmlLocation , root.Nodes () );
			}

			return newTires;
		}

		private static string GetNewTireName ( string originalName , TireFriction tire ) {
			return $"{originalName} {tire}";
		}



		private sealed class TireFriction {

			public TireFriction ( string id , string newId , XElement element , TireFriction template ) {
				Id = id;
				NewId = newId;
				Dirt = float.TryParse ( element?.Attribute ( "BodyFriction" )?.Value , out var dirt ) ? dirt : template?.Dirt ?? 0;
				Mud = float.TryParse ( element?.Attribute ( "SubstanceFriction" )?.Value , out var mud ) ? mud : template?.Mud ?? 0;
				Asphalt = float.TryParse ( element?.Attribute ( "BodyFrictionAsphalt" )?.Value , out var asphalt ) ? asphalt : template?.Asphalt ?? 0;
			}

			public TireFriction ( string id , XElement element ) : this ( id , null , element , null ) {
			}

			public TireFriction ( string id , XElement element , TireFriction template ) : this ( id , null , element , template ) {
			}

			public string Id { get; }
			public string NewId { get; }
			public float Dirt { get; }
			public float Mud { get; }
			public float Asphalt { get; }

			public override string ToString () {
				return $"A{Asphalt:0.#}/D{Dirt:0.#}/M{Mud:0.#}";
			}

		}

	}

}
