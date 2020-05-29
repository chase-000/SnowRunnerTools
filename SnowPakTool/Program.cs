using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SnowPakTool {

	public static class Program {

		/*
			/packcb "D:\Games\SnowRunner_backs\settings\keys\tmp" "D:\Games\SnowRunner_backs\settings\keys\initial.1.cache_block"
			/unpackcb "D:\Games\SnowRunner_backs\settings\keys\initial.cache_block"
			/zippak "D:\Games\SnowRunner\en_us\preload\paks\client\initial" "D:\Games\SnowRunner_backs\__mod\test.pak"
		*/

		public static int Main ( string[] args ) {
			switch ( args.Length > 0 ? args[0] : null ) {

				case "/license":
					PrintLicense ();
					return 0;

				case "/listcb":
					ListCacheBlock ( args );
					return 0;

				case "/unpackcb":
					UnpackCacheBlock ( args );
					return 0;

				case "/packcb":
					PackCacheBlock ( args );
					return 0;

				case "/zippak":
					ZipPakHelper.CreatePak ( args[1] , args[2] );
					return 0;

				case "/llvalidate":
					LoadListTest ( args[1] );
					return 0;

				default:
					PrintHelp ();
					return 1;
			}
		}

		[DebuggerDisplay ( "{Strings[0]} ({Values.Length}) @{OrderEntryOffset,X}/{NamesEntryOffset,X}" )]
		public class LoadListRawEntry {
			public int Index { get; set; }
			public long OrderEntryOffset { get; set; }
			public long NamesEntryOffset { get; set; }
			public byte[] MagicA { get; set; }
			public byte[] MagicB { get; set; }
			public string[] Strings { get; set; }
			public int[] Values { get; set; }

			//public string InternalName { get; set; }
			//public string Extension { get; set; }
			//public string PakName { get; set; }
			//public int MagicA { get; set; }
			//public int MagicB { get; set; }
			//public byte[] MagicC { get; set; }
		}


		private static void LoadListTest ( string loadListLocation ) {
			using var stream = File.OpenRead ( loadListLocation );

			/*
				byte map:
					5: start?
					1: type-1
					2: type-3
					6: end?
			*/

			stream.ReadMagicInt32 ( 1 ); //array length?
			stream.ReadMagicByte ( 1 ); //data type?
			var entriesCount = stream.ReadInt32 ();
			stream.ReadMagicInt32 ( 3 ); //???
			stream.ReadMagicByte ( 1 ); //???
			var entryTypes = stream.ReadByteArray ( entriesCount );

			stream.ReadMagicByte ( 1 ); //???

			var entries = new LoadListRawEntry[entriesCount];
			for ( int i = 0; i < entriesCount; i++ ) {
				var entry = entries[i] = new LoadListRawEntry { Index = i };
				entry.OrderEntryOffset = stream.Position;
				var count = stream.ReadInt32 ();
				stream.ReadMagicByte ( 1 );
				entry.Values = stream.ReadInt32Array ( count );
			}

			stream.ReadMagicByte ( 1 ); //???
			MiscHelpers.Assert ( stream.Position == 0x0000E24E );

			foreach ( var entry in entries ) {
				entry.NamesEntryOffset = stream.Position;
				var stringsCount = stream.ReadInt32 ();
				var magicBCount = stream.ReadInt32 ();
				entry.MagicA = stream.ReadByteArray ( stringsCount );
				entry.MagicB = stream.ReadByteArray ( magicBCount );
				entry.Strings = new string[stringsCount];
				for ( int i = 0; i < stringsCount; i++ ) {
					entry.Strings[i] = stream.ReadLength32String ();
				}
			}
			MiscHelpers.Assert ( stream.Position == stream.Length );

			var extensions = entries.Where ( a => a.Strings.Length == 3 ).GroupBy ( a => a.Strings[1] ).Select ( a => a.Key ).ToList ();
			var paks = entries.Where ( a => a.Strings.Length == 3 ).GroupBy ( a => a.Strings[2] ).Select ( a => a.Key ).ToList ();
			var internalNames = entries.Where ( a => a.Strings.Length == 3 ).Select ( a => a.Strings[0] ).ToList ();

			MiscHelpers.Assert ( entries.All ( entry => entry.Strings.Length == 0 || entry.Strings.Length == 1 || entry.Strings.Length == 3 ) );
			MiscHelpers.Assert ( entries.Count ( entry => entry.Strings.Length == 0 ) == 2 );
			MiscHelpers.Assert ( entries.First ().Strings.Length == 0 );
			MiscHelpers.Assert ( entries.Last ().Strings.Length == 0 );
			MiscHelpers.Assert ( entries.All ( entry => entry.MagicB.Length == 2 ) );
			MiscHelpers.Assert ( entries.All ( entry => entry.MagicA.All ( a => a == 1 ) ) );
			MiscHelpers.Assert ( entries.All ( entry => entry.MagicB.All ( a => a == 1 ) ) );
			MiscHelpers.Assert ( entries.Skip ( 1 ).Take ( entries.Length - 2 ).GroupBy ( a => a.Strings[0] ).Count () == entriesCount - 2 );
		}

		private static void PrintLicense () {
			using var stream = typeof ( Program ).Assembly.GetManifestResourceStream ( $"{nameof ( SnowPakTool )}.LICENSE" );
			using var reader = new StreamReader ( stream );
			Console.WriteLine ( reader.ReadToEnd () );
		}

		private static void ListCacheBlock ( string[] args ) {
			using ( var stream = File.OpenRead ( args[1] ) ) {
				var reader = new CacheBlockReader ( stream );
				Console.WriteLine ( $"File entries: {reader.FileEntries.Length}" );
				Console.WriteLine ( $"Base offset: {reader.BaseOffset}" );
				var i = 0;
				foreach ( var item in reader.FileEntries ) {
					Console.WriteLine ( $"[{i}] {item.InternalName}: {item.Size} byte(s) at {item.RelativeOffset + reader.BaseOffset}" );
					i++;
				}
			}
		}

		private static void UnpackCacheBlock ( string[] args ) {
			var sourceLocation = Path.GetFullPath ( args[1] );
			var sourceDirectory = Path.GetDirectoryName ( sourceLocation );
			var targetDirectory = args.Length >= 3
						? Path.GetFullPath ( args[2] )
						: Path.Combine ( sourceDirectory , Path.GetFileNameWithoutExtension ( sourceLocation ) );
			if ( Directory.Exists ( targetDirectory ) ) throw new IOException ( $"Target directory '{targetDirectory}' already exists." );
			using ( var stream = File.OpenRead ( sourceLocation ) ) {
				var reader = new CacheBlockReader ( stream );
				reader.UnpackAll ( targetDirectory );
			}
		}

		private static void PackCacheBlock ( string[] args ) {
			var sourceDirectory = Path.GetFullPath ( args[1] );
			var targetLocation = args[2];
			var entries = CacheBlockWriter.GetFileEntries ( sourceDirectory ).OrderBy ( a => a.InternalName ).ToList ();
			using ( var stream = File.Open ( targetLocation , FileMode.CreateNew , FileAccess.Write , FileShare.Read ) ) {
				var writer = new CacheBlockWriter ( stream );
				writer.Pack ( sourceDirectory , entries );
			}
		}

		private static void PrintHelp () {
			Console.WriteLine ( "Usage:" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /license" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /listcb file.cache_block" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /unpackcb file.cache_block [directory]" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /packcb directory file.cache_block" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /zippak directory file.pak" );
			Console.WriteLine ( $"  {nameof ( SnowPakTool )} /llvalidate pak.load_list" );
		}

	}

}
