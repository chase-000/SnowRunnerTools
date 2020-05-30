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

			stream.ReadMagicInt32 ( 1 ); //array length?
			stream.ReadMagicByte ( 1 ); //data type?
			var entriesCount = stream.ReadInt32 ();
			stream.ReadMagicInt32 ( 3 ); //???
			stream.ReadMagicByte ( 1 ); //???
			var entryTypes = stream.ReadValuesArray<LoadListEntryType> ( entriesCount );

			if ( entriesCount < 2 ) throw new InvalidDataException ( "Invalid number of entries." );
			if ( entryTypes[0] != LoadListEntryType.Start ) throw new InvalidDataException ( "First entry is not a start entry." );
			if ( entryTypes[entriesCount - 1] != LoadListEntryType.End ) throw new InvalidDataException ( "Last entry is not an end entry." );
			if ( !entryTypes.Skip ( 1 ).Take ( entriesCount - 2 ).All ( a => a == LoadListEntryType.Stage || a == LoadListEntryType.Asset ) ) throw new InvalidDataException ( "Unknown entry type." );

			stream.ReadMagicByte ( 1 ); //???

			var entries = new LoadListEntryBase[entriesCount];
			for ( int i = 0; i < entriesCount; i++ ) {
				var entry = entries[i] = LoadListEntryBase.FromType ( entryTypes[i] );
				entry.Index = i;
				entry.OrderEntryOffset = stream.Position;
				var count = stream.ReadInt32 ();
				stream.ReadMagicByte ( 1 ); //data type?
				entry.DependsOn = stream.ReadInt32Array ( count );
				if ( entry.DependsOn.Any ( a => a < 0 && a >= entriesCount ) ) throw new InvalidDataException ( $"Invalid dependency index for entry {i} @0x{entry.OrderEntryOffset:X}." );
			}

			stream.ReadMagicByte ( 1 ); //???

			for ( int i = 0; i < entriesCount; i++ ) {
				var entry = entries[i];
				entry.NamesEntryOffset = stream.Position;

				var stringsCount = stream.ReadInt32 ();
				var magicBCount = stream.ReadInt32 ();
				entry.MagicA = stream.ReadByteArray ( stringsCount );
				entry.MagicB = stream.ReadByteArray ( magicBCount );

				if ( stringsCount != entry.ExpectedStringsCount ) throw new InvalidDataException ( $"Unexpected strings count for entry {i} @0x{entry.NamesEntryOffset:X}." );
				if ( magicBCount != 2 ) throw new InvalidDataException ( $"Unexpected length of magic array 'B' for entry {i} @0x{entry.NamesEntryOffset:X}." );
				if ( entry.MagicA.Any ( a => a != 1 ) ) throw new InvalidDataException ( $"Unknown value in magic array 'A' for entry {i} @0x{entry.NamesEntryOffset:X}." );
				if ( entry.MagicB.Any ( a => a != 1 ) ) throw new InvalidDataException ( $"Unknown value in magic array 'B' for entry {i} @0x{entry.NamesEntryOffset:X}." );

				if ( entry is LoadListStageEntry stage ) {
					stage.Text = stream.ReadLength32String ();
				}
				else if ( entry is LoadListAssetEntry asset ) {
					asset.InternalName = stream.ReadLength32String ();
					asset.Loader = stream.ReadLength32String ();
					asset.PakName = stream.ReadLength32String ();
				}
			}

			if ( stream.Position != stream.Length ) throw new InvalidDataException ( "Data found beyond logical end of file." );

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
		}

	}

}
