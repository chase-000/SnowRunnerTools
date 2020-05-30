using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SnowPakTool {

	public class LoadListAssetEntry : LoadListEntryBase {

		/// <summary>
		/// Regex used to parse internal file names.
		/// </summary>
		/// <remarks>
		/// This format differs slightly from the one in <see cref="CacheBlockFileFileEntry"/> in that it doesn't use ':' in place of an empty directory name.
		/// </remarks>
		public static Regex InternalNameRegex { get; } = new Regex ( @"^(?:<(?<ps>[^>]+)>(?<dir>\\(?:.+\\)?))?(?<fn>[^\\]+)$" , RegexOptions.Compiled );

		/// <summary>
		/// Regex used to parse external file names.
		/// </summary>
		public static Regex ExternalNameRegex { get; } = new Regex ( @"^(?:\[(?<ps>[^\]]+)\](?<dir>\\(?:.+\\)?))?(?<fn>[^\\]+)$" , RegexOptions.Compiled );



		/// <summary>
		/// Converts file name from the internal name format that uses angled brackets into the external name format with square brackets.
		/// </summary>
		public static string InternalNameToExternalName ( string name , out string ps ) {
			ps = null;
			var match = InternalNameRegex.Match ( name );
			if ( !match.Success ) throw new ArgumentException ( $"Unexpected internal file name format: '{name}'" , nameof ( name ) );
			var psValue = match.Groups["ps"].Value;
			var dir = match.Groups["dir"].Value;
			var fn = match.Groups["fn"].Value;
			if ( psValue.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0
						|| dir.IndexOfAny ( IOHelpers.InvalidPathChars ) >= 0
						|| fn.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0 ) throw new ArgumentException ( $"Invalid characters found in internal name: '{name}'" , nameof ( name ) );

			if ( psValue.Length > 0 ) {
				ps = psValue;
				return $"[{psValue}]{dir}{fn}";
			}
			else {
				return dir + fn;
			}
		}

		/// <summary>
		/// Converts file name from the external (file system) name format into the internal one.
		/// </summary>
		public static string ExternalNameToInternalName ( string name ) {
			var match = ExternalNameRegex.Match ( name );
			if ( !match.Success ) throw new ArgumentException ( $"Unexpected external file name format: '{name}'" , nameof ( name ) );
			var ps = match.Groups["ps"].Value;
			var dir = match.Groups["dir"].Value;
			var fn = match.Groups["fn"].Value;
			var sb = new StringBuilder ( name.Length + 4 );
			if ( ps.Length > 0 ) {
				sb.Append ( '<' );
				sb.Append ( ps );
				sb.Append ( '>' );
			}
			if ( dir.Length > 0 ) {
				sb.Append ( dir );
			}
			sb.Append ( fn );
			return sb.ToString ();
		}


		public override LoadListEntryType Type => LoadListEntryType.Asset;
		public override int ExpectedStringsCount => 3;
		public string InternalName { get; set; }
		public string InternalNamePs { get; set; }
		public string ExternalName { get; set; }
		public string Loader { get; set; }
		public string PakName { get; set; }


		public override void WriteStrings ( Stream stream ) {
			base.WriteStrings ( stream );
			stream.WriteLength32String ( InternalName );
			stream.WriteLength32String ( Loader );
			stream.WriteLength32String ( PakName );
		}

		public override string ToString () {
			return $"[{Index}] [{PakName}] {InternalName} ({Loader}) ({DependsOn?.Length ?? 0}) @0x{DependencyEntryOffset:X}/0x{StringsEntryOffset:X}";
		}

	}

}
