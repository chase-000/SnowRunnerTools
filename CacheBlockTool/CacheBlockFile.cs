using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CacheBlockTool {
	public abstract class CacheBlockFile {

		/// <summary>
		/// File header signature. Could be that it's actually structured somehow.
		/// </summary>
		public static byte[] Signature { get; } = new byte[] {
			0x31 , 0x53 , 0x45 , 0x52 , 0x63 , 0x61 , 0x63 , 0x68 , 0x65 , 0x5F , 0x62 , 0x6C , 0x6F , 0x63 , 0x6B , 0x00 ,
			0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 ,
			0x00 , 0x00 , 0x00 , 0x00 , 0x53 , 0x33 , 0x44 , 0x52 , 0x45 , 0x53 , 0x4F , 0x55 , 0x52 , 0x43 , 0x45 , 0x20 ,
			0x20 , 0x20 , 0x20 , 0x20 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 ,
		};
		/*
			00000000:  31 53 45 52-63 61 63 68-65 5F 62 6C-6F 63 6B 00  1SERcache_block
			00000010:  00 00 00 00-00 00 00 00-00 00 00 00-00 00 00 00
			00000020:  00 00 00 00-53 33 44 52-45 53 4F 55-52 43 45 20      S3DRESOURCE
			00000030:  20 20 20 20-00 00 00 00-00 00 00 00-00 00 00 00
		*/

		/// <summary>
		/// Encoding used for file names. Could be some other encoding entirely, could be a multibyte one, but works so far.
		/// </summary>
		public static Encoding Encoding { get; } = Encoding.GetEncoding ( 437 );

		/// <summary>
		/// Regex used to parse internal file names.
		/// </summary>
		public static Regex InternalNameRegex { get; } = new Regex ( @"^<(?<ps>[^>]+)>(?:(?<dir>\\.+\\)|:)(?<fn>[^\\]+)$" , RegexOptions.Compiled );



		protected CacheBlockFile ( Stream stream ) {
			if ( stream == null ) throw new ArgumentNullException ( nameof ( stream ) );
			Stream = stream;
		}


		public Stream Stream { get; }
		public FileEntry[] FileEntries { get; protected set; }
		public long BaseOffset { get; protected set; }


		public static string InternalNameToExternalName ( string name ) {
			var match = InternalNameRegex.Match ( name );
			if ( !match.Success ) throw new InvalidDataException ( $"Unexpected internal file name format: '{name}'" );
			var ps = match.Groups["ps"].Value;
			var dir = match.Groups["dir"].Value;
			var fn = match.Groups["fn"].Value;
			if ( ps.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0
						|| dir.IndexOfAny ( IOHelpers.InvalidPathChars ) >= 0
						|| fn.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0 ) throw new InvalidDataException ( $"Invalid characters found in internal name: '{name}'" );
			if ( dir.Length == 0 ) dir = "\\";
			return ps + dir + fn;
		}

	}

}
