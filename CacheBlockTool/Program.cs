using System;
using System.IO;
using System.Linq;

namespace CacheBlockTool {

	public static class Program {

		/*
			/pack "D:\Games\SnowRunner_backs\settings\keys\tmp" "D:\Games\SnowRunner_backs\settings\keys\initial.1.cache_block"
			/unpack "D:\Games\SnowRunner_backs\settings\keys\initial.cache_block"
		*/

		public static int Main ( string[] args ) {
			switch ( args.Length > 0 ? args[0] : null ) {

				case "/list":
					List ( args );
					return 0;

				case "/unpack":
					Unpack ( args );
					return 0;

				case "/pack":
					Pack ( args );
					return 0;

				default:
					PrintHelp ();
					return 1;
			}
		}


		private static void List ( string[] args ) {
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

		private static void Unpack ( string[] args ) {
			var sourceLocation = Path.GetFullPath ( args[1] );
			var sourceDirectory = Path.GetDirectoryName ( sourceLocation );
			var targetDirectory = args.Length >= 3
						? Path.GetFullPath ( args[2] )
						: Path.Combine ( sourceDirectory , Path.GetFileNameWithoutExtension ( sourceLocation ) );
			if ( Directory.Exists ( targetDirectory ) ) throw new IOException ( $"Target directory '{targetDirectory}' already exists." );
			using ( var stream = File.OpenRead ( sourceLocation ) ) {
				var reader = new CacheBlockReader ( stream );
				reader.ExtractAll ( targetDirectory );
			}
		}

		private static void Pack ( string[] args ) {
			var sourceDirectory = Path.GetFullPath ( args[1] );
			if ( !sourceDirectory.EndsWith ( "\\" ) ) sourceDirectory += '\\';
			if ( !Directory.Exists ( sourceDirectory ) ) throw new IOException ( $"Source directory '{sourceDirectory}' does not exist." );
			if ( Directory.EnumerateFiles ( sourceDirectory , "*" ).Any () ) throw new IOException ( $"Source directory '{sourceDirectory}' has files in it. It should only contain directories." );

		}

		private static void PrintHelp () {
			Console.WriteLine ( "Usage:" );
			Console.WriteLine ( $"  {nameof ( CacheBlockTool )} /list file.cache_block" );
			Console.WriteLine ( $"  {nameof ( CacheBlockTool )} /unpack file.cache_block [directory]" );
			Console.WriteLine ( $"  {nameof ( CacheBlockTool )} /pack directory file.cache_block" );
		}

	}

	//public class 

}
