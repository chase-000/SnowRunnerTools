using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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

		public static FileEntry FromExternalName ( string name ) {
			if ( name is null ) throw new ArgumentNullException ( nameof ( name ) );
			return new FileEntry {
				ExternalName = name ,
				InternalName = ExternalNameToInternalName ( name ) ,
			};
		}

		public static string InternalNameToExternalName ( string name ) {
			var match = InternalNameRegex.Match ( name );
			if ( !match.Success ) throw new ArgumentException ( $"Unexpected internal file name format: '{name}'" , nameof ( name ) );
			var ps = match.Groups["ps"].Value;
			var dir = match.Groups["dir"].Value;
			var fn = match.Groups["fn"].Value;
			if ( ps.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0
						|| dir.IndexOfAny ( IOHelpers.InvalidPathChars ) >= 0
						|| fn.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0 ) throw new ArgumentException ( $"Invalid characters found in internal name: '{name}'" , nameof ( name ) );
			if ( dir.Length == 0 ) dir = "\\";
			return $"[{ps}]{dir}{fn}";
		}

		public static string ExternalNameToInternalName ( string name ) {
			if ( Path.IsPathRooted ( name ) ) throw new ArgumentException ( "External name can not be rooted." , nameof ( name ) );
			var psSeparatorIndex = name.IndexOf ( '\\' );
			if ( psSeparatorIndex <= 0 ) throw new ArgumentException ( "External name must contain top-level directory." , nameof ( name ) );
			if ( psSeparatorIndex < 3 || name[0] != '[' || name[psSeparatorIndex - 1] != ']' ) throw new ArgumentException ( "External name top-level directory must be in '[name]' format." , nameof ( name ) );
			var remainderLength = name.Length - psSeparatorIndex - 1;
			if ( remainderLength <= 0 ) throw new ArgumentException ( "External name must contain file name." , nameof ( name ) );
			var sb = new StringBuilder ( name.Length + 4 );
			sb.Append ( '<' );
			sb.Append ( name , 1 , psSeparatorIndex - 2 );
			sb.Append ( '>' );
			var fnSeparatorIndex = name.LastIndexOf ( '\\' , name.Length - 1 , remainderLength );
			if ( fnSeparatorIndex < 0 ) {
				sb.Append ( ':' );
				sb.Append ( name , psSeparatorIndex + 1 , name.Length - psSeparatorIndex - 1 );
			}
			else {
				sb.Append ( name , psSeparatorIndex , name.Length - psSeparatorIndex );
			}
			return sb.ToString ();
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
