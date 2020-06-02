using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SnowPakTool {

	/// <summary>
	/// pak.load_list reader/writer.
	/// </summary>
	/// <remarks>
	/// Apparently this file is used to specify assets loading order.
	/// 
	/// Each entry represents either an asset file located in one of the PAKs, or a loading stage that seems to be used for grouping.
	/// The stages are named except for the two special stages that represent starting and ending states.
	/// Entries specify which other entries they depend on, thus determining the loading order.
	/// 
	/// There are still some magic values with unknown purpose, but they seem to be safe to ignore for now.
	/// 
	/// It's also unclear how the assets to be included in load_list are selected. For example, not all of the meshes are listed.
	/// </remarks>
	public static class LoadListFile {

		/// <summary>
		/// Reads pak.load_list file.
		/// </summary>
		public static LoadListEntryBase[] ReadEntries ( string location ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			using var stream = File.OpenRead ( location );

			//header
			stream.ReadMagicInt32 ( 1 ); //array length?
			stream.ReadMagicByte ( 1 ); //data type?
			var entriesCount = stream.ReadInt32 ();
			if ( entriesCount < 2 ) throw new InvalidDataException ( "Invalid number of entries." );
			stream.ReadMagicInt32 ( 3 ); //???
			stream.ReadMagicByte ( 1 ); //???

			//types byte array
			var entryTypes = stream.ReadValuesArray<LoadListEntryType> ( entriesCount );
			if ( entryTypes[0] != LoadListEntryType.Start ) throw new InvalidDataException ( "First entry is not a start entry." );
			if ( entryTypes[^1] != LoadListEntryType.End ) throw new InvalidDataException ( "Last entry is not an end entry." );
			if ( !entryTypes.Skip ( 1 ).Take ( entriesCount - 2 ).All ( a => a == LoadListEntryType.Stage || a == LoadListEntryType.Asset ) ) throw new InvalidDataException ( "Unknown entry type." );
			stream.ReadMagicByte ( 1 ); //???

			//order/dependencies
			var entries = new LoadListEntryBase[entriesCount];
			for ( int i = 0; i < entriesCount; i++ ) {
				var entry = entries[i] = LoadListEntryBase.FromType ( entryTypes[i] );
				entry.Index = i;
				entry.DependencyEntryOffset = stream.Position;
				var dependenciesCount = stream.ReadInt32 ();
				stream.ReadMagicByte ( 1 ); //data type?
				entry.DependsOn = stream.ReadValuesArray<int> ( dependenciesCount );
				if ( entry.DependsOn.Any ( a => a < 0 && a >= entriesCount ) ) throw new InvalidDataException ( $"Invalid dependency index for entry {i} @0x{entry.DependencyEntryOffset:X}." );
			}

			stream.ReadMagicByte ( 1 ); //???

			//strings
			for ( int i = 0; i < entriesCount; i++ ) {
				var entry = entries[i];
				entry.StringsEntryOffset = stream.Position;

				var stringsCount = stream.ReadInt32 ();
				var magicBCount = stream.ReadInt32 ();
				entry.MagicA = stream.ReadByteArray ( stringsCount );
				entry.MagicB = stream.ReadByteArray ( magicBCount );

				if ( !entry.IsValidStringsCount ( stringsCount ) ) throw new InvalidDataException ( $"Unexpected strings count ({stringsCount}) for entry {i} @0x{entry.StringsEntryOffset:X}." );
				if ( magicBCount != LoadListEntryBase.ExpectedMagicBCount ) throw new InvalidDataException ( $"Unexpected length of magic array 'B' for entry {i} @0x{entry.StringsEntryOffset:X}." );
				if ( entry.MagicA.Any ( a => a != LoadListEntryBase.ExpectedMagicAValue ) ) throw new InvalidDataException ( $"Unexpected value in magic array 'A' for entry {i} @0x{entry.StringsEntryOffset:X}." );
				if ( entry.MagicB.Any ( a => a != LoadListEntryBase.ExpectedMagicBValue ) ) throw new InvalidDataException ( $"Unexpected value in magic array 'B' for entry {i} @0x{entry.StringsEntryOffset:X}." );

				var strings = stream.ReadLength32StringsArray ( stringsCount );
				entry.LoadStrings ( strings );
			}

			if ( stream.Position != stream.Length ) throw new InvalidDataException ( "Unknown data beyond logical end of load_list file." );

			return entries;
		}

		/// <summary>
		/// Validates that the entry dependencies are in expected order.
		/// </summary>
		public static void ValidateLoadListOrdering ( IReadOnlyList<LoadListEntryBase> entries ) {
			Console.WriteLine ( "\nValidating load list dependencies order:" );
			var lastGroup = -1;
			for ( int i = 0; i < entries.Count; i++ ) {
				var entry = entries[i];
				if ( lastGroup < 0 ) {
					if ( entry.DependsOn.Length > 0 ) {
						Console.WriteLine ( "[0] Start entry has dependencies." );
					}
					lastGroup = i;
					continue;
				}

				var orderedDependsOn = entry.DependsOn.OrderBy ( a => a ).ToList ();
				if ( !entry.DependsOn.SequenceEqual ( orderedDependsOn ) ) {
					Console.WriteLine ( $"[{i}] Dependencies list is not sorted." );
				}

				switch ( entries[i] ) {
					case LoadListAssetEntry _:
						if ( entry.DependsOn.Length == 0 ) {
							Console.WriteLine ( $"[{i}] Asset has no dependencies." );
						}
						if ( entry.DependsOn.Length > 1 ) {
							Console.WriteLine ( $"[{i}] Asset has more than one dependency." );
						}
						break;

					case LoadListEndEntry _:
					case LoadListStageEntry _:
						if ( i - lastGroup > 1 ) {
							if ( !Enumerable.Range ( lastGroup + 1 , i - lastGroup - 1 ).SequenceEqual ( orderedDependsOn ) ) {
								Console.WriteLine ( $"[{i}] Stage doesn't depend on previous assets exactly." );
							}
						}
						else {
							if ( entry.DependsOn.Length == 0 ) {
								Console.WriteLine ( $"[{i}] Stage has no dependencies." );
							}
							if ( entry.DependsOn.Length > 1 ) {
								Console.WriteLine ( $"[{i}] Stage depends on more than immediately preceding stage." );
							}
						}
						lastGroup = i;
						break;
				}
			}
			Console.WriteLine ( "Done." );
		}

		public static void WriteFileNames ( string loadListLocation , IEnumerable<string> initialContainerFiles , IEnumerable<string> sharedContainerFiles , IEnumerable<string> sharedSoundContainerFiles ) {
			using var stream = File.Open ( loadListLocation , FileMode.CreateNew , FileAccess.Write );

			var entries = CreateEntries ( initialContainerFiles , sharedContainerFiles , sharedSoundContainerFiles );
			SetDefaultDependencies ( entries );

			//header
			stream.WriteValue ( 1 ); //array length?
			stream.WriteByte ( 1 ); //data type?
			stream.WriteValue ( entries.Count );
			stream.WriteValue ( 3 ); //???
			stream.WriteByte ( 1 ); //???

			//types byte array
			foreach ( var item in entries ) {
				item.WriteType ( stream );
			}

			stream.WriteByte ( 1 ); //???

			//order/dependencies
			foreach ( var item in entries ) {
				item.WriteDependencies ( stream );
			}

			stream.WriteByte ( 1 ); //???

			//strings
			foreach ( var item in entries ) {
				item.WriteStrings ( stream );
			}
		}

		private static void SetDefaultDependencies ( List<LoadListEntryBase> entries ) {
			var lastGroup = -1;
			for ( int i = 0; i < entries.Count; i++ ) {
				var entry = entries[i];
				switch ( entry ) {

					case LoadListStartEntry _:
						entry.DependsOn = new int[0];
						lastGroup = i;
						break;

					case LoadListAssetEntry _:
					case LoadListEndEntry _:
						entry.DependsOn = new int[] { i - 1 };
						break;

					case LoadListStageEntry _:
						if ( i - lastGroup > 1 ) {
							entry.DependsOn = Enumerable.Range ( lastGroup + 1 , i - lastGroup - 1 ).ToArray ();
						}
						else {
							entry.DependsOn = new int[] { i - 1 };
						}
						lastGroup = i;
						break;
				}
			}
		}

		private static List<LoadListEntryBase> CreateEntries ( IEnumerable<string> initialContainerFiles , IEnumerable<string> sharedContainerFiles , IEnumerable<string> sharedSoundContainerFiles ) {
			var entries = new List<LoadListEntryBase> ();

			entries.Add ( new LoadListStartEntry () );

			entries.Add ( new LoadListStageEntry { Text = "RES3_INIT load" } );
			entries.Add ( new LoadListStageEntry { Text = "SSL_SOURCES_PARSE load" } );

			entries.AddRange ( CreateAssetEntries ( initialContainerFiles , "initial.pak" , @"[ssl_cache]\" , ".spdb" , "spdb" ) );
			entries.AddRange ( CreateAssetEntries ( initialContainerFiles , "initial.pak" , @"[ssl_cache]\" , ".sslbundle" , "sslbundle" ) );
			entries.Add ( new LoadListStageEntry { Text = "SSL_INITIAL load" } );

			entries.AddRange ( CreateAssetEntries ( initialContainerFiles , "initial.pak" , @"[media]\_templates\" , ".xml" , "tpl_loader" ) );
			entries.Add ( new LoadListStageEntry { Text = "TEMPLATES load" } );

			entries.AddRange ( CreateAssetEntries ( initialContainerFiles , "initial.pak" , @"[media]\classes\" , ".xml" , "cls_loader" ) );
			entries.Add ( new LoadListStageEntry { Text = "CLASSES load" } );

			entries.Add ( new LoadListStageEntry { Text = "TEXTURE_PREPARE load" } );
			entries.Add ( new LoadListStageEntry { Text = "TEXTURE load" } );

			entries.AddRange ( CreateAssetEntries ( sharedContainerFiles , "shared.pak" , @"[meshes]\" , null , "mesh_loader" , ExcludeMeshes ) );
			entries.Add ( new LoadListStageEntry { Text = "MESH load" } );

			entries.AddRange ( CreateAssetEntries ( sharedSoundContainerFiles , "shared_sound.pak" , null , ".sound_list" , "sound_loader" ) );
			entries.Add ( new LoadListStageEntry { Text = "SOUND load" } );

			entries.Add ( new LoadListStageEntry { Text = "RES3_PROJECT load" } );
			entries.Add ( new LoadListStageEntry { Text = "PROJECT load" } );
			entries.Add ( new LoadListStageEntry { Text = "DEFAULT load" } );
			entries.Add ( new LoadListStageEntry { Text = "DESC_BLOCK load" } );

			entries.Add ( new LoadListEndEntry () );

			return entries;
		}

		private static IEnumerable<LoadListAssetEntry> CreateAssetEntries ( IEnumerable<string> names , string pakName , string directory , string extension , string loader , Func<string , bool> exclude = null ) {
			return names
				.Where ( name =>
					( directory == null ? Path.GetDirectoryName ( name ).Length == 0 : name.StartsWith ( directory , StringComparison.OrdinalIgnoreCase ) )
					&& ( extension == null ? Path.GetExtension ( name ).Length == 0 : name.EndsWith ( extension , StringComparison.OrdinalIgnoreCase ) )
					&& ( exclude == null || !exclude ( name ) )
				)
				.OrderBy ( a => a )
				.Select ( name => new LoadListAssetEntry {
					PakName = pakName ,
					ExternalName = name ,
					InternalName = LoadListAssetEntry.ExternalNameToInternalName ( name ) ,
					Loader = loader ,
				} );
		}

		/// <summary>
		/// Additional filtering out of the meshes based on the contents of the original pak.load_list.
		/// </summary>
		private static bool ExcludeMeshes ( string name ) {
			if ( name.StartsWith ( @"[meshes]\grass_" , StringComparison.OrdinalIgnoreCase ) ) return true;
			if ( name.StartsWith ( @"[meshes]\plants_" , StringComparison.OrdinalIgnoreCase ) ) return true;
			if ( name.StartsWith ( @"[meshes]\overlays_" , StringComparison.OrdinalIgnoreCase ) ) return true;
			if ( name.StartsWith ( @"[meshes]\models_cargo_unit_" , StringComparison.OrdinalIgnoreCase ) ) return false;
			if ( name.StartsWith ( @"[meshes]\models_" , StringComparison.OrdinalIgnoreCase ) ) return true;
			return false;
		}

	}

}
