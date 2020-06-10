using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SnowPakTool;

namespace SnowTruckConfig {

	public static class ExtrasRenamer {

		public static Regex RenamedStringIdRegex { get; } = new Regex ( @"^(.+?)_96FD3B99_[0-9A-F]{8}$" , RegexOptions.Compiled );


		/// <summary>
		/// Rename all tires (wheels) to include their friction stats in the name.
		/// </summary>
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

			var strings = GetStrings ( directory , language , out var stringsMap , out var stringsLocation );

			Console.WriteLine ( "Creating new tire names." );
			var newTireNames = tires
				.OrderBy ( a => a.NewId )
				.Select ( a => Pair.From ( a.NewId , GetNewTireName ( stringsMap[a.Id] , a ) ) )
				.ToList ()
				;

			Console.WriteLine ( $"Writing localization strings." );
			AddRenamingStrings ( strings , newTireNames );
			StringsFile.WriteStrings ( stringsLocation , strings , overwrite: true );
			Console.WriteLine ( "Done." );
		}

		/// <summary>
		/// Rename all trucks to include their mass in the name.
		/// </summary>
		public static void RenameTrucks ( DirectoryInfo directory , GameLanguage language ) {
			var directoryLocation = IOHelpers.NormalizeDirectory ( directory.FullName );
			var strings = GetStrings ( directory , language , out var stringsMap , out var stringsLocation );

			Console.WriteLine ( "Processing trucks data." );
			var newTruckNames = new List<KeyValuePair<string , string>> ();
			var trucks = Directory.EnumerateFiles ( Path.Combine ( directoryLocation , @"[media]\classes\trucks" ) , "*.xml" );
			foreach ( var truckXmlLocation in trucks ) {
				var root = XmlHelpers.ReadFragments ( truckXmlLocation );
				var nameIdNode = root.Element ( "Truck" )?.Element ( "GameData" )?.Element ( "UiDesc" )?.Attribute ( "UiName" );
				var nameId = nameIdNode?.Value;
				if ( nameId == null ) continue;
				var originalId = GetOriginalId ( nameId );
				var mass = TruckHelpers.GetMass ( root.Element ( "Truck" ) );
				if ( mass < 100 ) continue;

				var newId = GetNewId ( originalId , truckXmlLocation[directoryLocation.Length..] );
				nameIdNode.Value = newId;
				newTruckNames.Add ( Pair.From ( newId , $"{stringsMap[originalId]} | {mass / 1000:0.#}t" ) );

				XmlHelpers.WriteFragments ( truckXmlLocation , root.Nodes () );
			}

			Console.WriteLine ( $"Writing localization strings." );
			AddRenamingStrings ( strings , newTruckNames );
			StringsFile.WriteStrings ( stringsLocation , strings , overwrite: true );
			Console.WriteLine ( "Done." );
		}

		/// <summary>
		/// Rename all trucks to include their torque and fuel consumption in the name. Both of these look like arbitrary in-game units.
		/// </summary>
		public static void RenameEngines ( DirectoryInfo directory , GameLanguage language ) {
			var directoryLocation = IOHelpers.NormalizeDirectory ( directory.FullName );
			var strings = GetStrings ( directory , language , out var stringsMap , out var stringsLocation );

			Console.WriteLine ( "Processing engines data." );
			var newEngineNames = new List<KeyValuePair<string , string>> ();
			var engines = Directory.EnumerateFiles ( Path.Combine ( directoryLocation , @"[media]\classes\engines" ) , "*.xml" );
			foreach ( var engineXmlLocation in engines ) {
				var root = XmlHelpers.ReadFragments ( engineXmlLocation );
				var engineNodes = root.Elements ( "EngineVariants" ).Elements ( "Engine" ); //ignore templates: no original file has any torque/fuel values there
				foreach ( var engineNode in engineNodes ) {
					var nameIdNode = engineNode.Element ( "GameData" )?.Element ( "UiDesc" )?.Attribute ( "UiName" );
					var nameId = nameIdNode?.Value;
					if ( nameId == null ) continue;
					var originalId = GetOriginalId ( nameId );
					var fuelConsumption = (string) engineNode.Attribute ( "FuelConsumption" ) ?? "?";
					float.TryParse ( (string) engineNode.Attribute ( "Torque" ) , out var torque );

					var newId = GetNewId ( originalId , engineXmlLocation[directoryLocation.Length..] );
					nameIdNode.Value = newId;
					newEngineNames.Add ( Pair.From ( newId , $"{stringsMap[originalId]} | {fuelConsumption}/{torque / 1000:0.#}k" ) );
				}
				XmlHelpers.WriteFragments ( engineXmlLocation , root.Nodes () );
			}

			Console.WriteLine ( $"Writing localization strings." );
			AddRenamingStrings ( strings , newEngineNames );
			StringsFile.WriteStrings ( stringsLocation , strings , overwrite: true );
			Console.WriteLine ( "Done." );
		}




		private static List<KeyValuePair<string , string>> GetStrings ( DirectoryInfo directory , GameLanguage language , out Dictionary<string , string> map , out string location ) {
			Console.WriteLine ( $"Reading localization strings for: {language}." );
			location = Path.Combine ( directory.FullName , @$"[strings]\strings_{language}.str".ToLowerInvariant () );
			var strings = StringsFile.ReadStrings ( location ).ToList ();
			map = strings
				.GroupBy ( a => a.Key ) //there are multiple duplicates
				.ToDictionary ( a => a.Key , a => a.First ().Value )
				;
			return strings;
		}

		private static void AddRenamingStrings ( List<KeyValuePair<string , string>> strings , IEnumerable<KeyValuePair<string , string>> newStrings ) {
			var originalIds = new HashSet<string> ( newStrings.Select ( a => GetOriginalId ( a.Key ) ) );
			strings.RemoveAll ( a => GetOriginalId ( a.Key , out var id ) && originalIds.Contains ( id ) );
			strings.AddRange ( newStrings );
		}

		private static string GetNewId ( string originalId , string unique ) {
			var crc = MiscHelpers.ComputeUtf8Crc32 ( unique );
			return $"{originalId}_96FD3B99_{crc:X8}"; //generate new ids to separate them from originals and to make them unique (there's reuse in original files)
		}

		private static bool GetOriginalId ( string id , out string originalId ) {
			var match = RenamedStringIdRegex.Match ( id );
			if ( match.Success ) {
				originalId = match.Groups[1].Value;
				return true;
			}
			else {
				originalId = id;
				return false;
			}
		}

		private static string GetOriginalId ( string id ) {
			GetOriginalId ( id , out var originalId );
			return originalId;
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

					var originalId = GetOriginalId ( id.Value );
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
			return $"{originalName} | {tire}";
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
