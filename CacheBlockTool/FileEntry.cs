using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CacheBlockTool {

	[DebuggerDisplay ( "{InternalName}: {Size} @ {RelativeOffset}" )]
	public class FileEntry {

		/// <summary>
		/// Regex used to parse internal file names.
		/// </summary>
		public static Regex InternalNameRegex { get; } = new Regex ( @"^<(?<ps>[^>]+)>(?:(?<dir>\\.+\\)|:)(?<fn>[^\\]+)$" , RegexOptions.Compiled );


		public static FileEntry FromInternalName ( string name ) {
			if ( name is null ) throw new ArgumentNullException ( nameof ( name ) );
			return new FileEntry {
				InternalName = name ,
				ExternalName = InternalNameToExternalName ( name ) ,
			};
		}

		public static string InternalNameToExternalName ( string name ) {
			var match = InternalNameRegex.Match ( name );
			if ( !match.Success ) throw new ArgumentException ( $"Unexpected internal file name format: '{name}'" );
			var ps = match.Groups["ps"].Value;
			var dir = match.Groups["dir"].Value;
			var fn = match.Groups["fn"].Value;
			if ( ps.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0
						|| dir.IndexOfAny ( IOHelpers.InvalidPathChars ) >= 0
						|| fn.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0 ) throw new ArgumentException ( $"Invalid characters found in internal name: '{name}'" );
			if ( dir.Length == 0 ) dir = "\\";
			return ps + dir + fn;
		}


		public string InternalName { get; private set; }
		public string ExternalName { get; private set; }

		public long RelativeOffset { get; set; }
		public int Size { get; set; }
		public int Zero { get; set; }


		private FileEntry () {
		}

	}

}
