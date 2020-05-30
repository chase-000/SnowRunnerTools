using System;
using System.Text.RegularExpressions;

namespace SnowPakTool {

	public class LoadListAssetEntry : LoadListEntryBase {

		private string m_InternalName;


		/// <summary>
		/// Regex used to parse internal file names.
		/// </summary>
		/// <remarks>
		/// This format differs slightly from the one in <see cref="CacheBlockFileFileEntry"/> in that it doesn't use ':' in place of an empty directory name.
		/// </remarks>
		public static Regex InternalNameRegex { get; } = new Regex ( @"^(?:<(?<ps>[^>]+)>(?<dir>\\(?:.+\\)?))?(?<fn>[^\\]+)$" , RegexOptions.Compiled );

		/// <summary>
		/// Converts file name from the internal name format that uses angled brackets into the external name format with square brackets.
		/// </summary>
		public static string InternalNameToExternalName ( string name ) {
			var match = InternalNameRegex.Match ( name );
			if ( !match.Success ) throw new ArgumentException ( $"Unexpected internal file name format: '{name}'" , nameof ( name ) );
			var ps = match.Groups["ps"].Value;
			var dir = match.Groups["dir"].Value;
			var fn = match.Groups["fn"].Value;
			if ( ps.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0
						|| dir.IndexOfAny ( IOHelpers.InvalidPathChars ) >= 0
						|| fn.IndexOfAny ( IOHelpers.InvalidNameChars ) >= 0 ) throw new ArgumentException ( $"Invalid characters found in internal name: '{name}'" , nameof ( name ) );
			return ps.Length > 0 ? $"[{ps}]{dir}{fn}" : dir + fn;
		}



		public override LoadListEntryType Type => LoadListEntryType.Asset;

		public override int ExpectedStringsCount => 3;

		public string InternalName {
			get => m_InternalName;
			set {
				ExternalName = InternalNameToExternalName ( value );
				m_InternalName = value;
			}
		}

		public string ExternalName { get; private set; }

		public string Loader { get; set; }

		public string PakName { get; set; }

		public override string ToString () {
			return $"[{Index}] [{PakName}] {InternalName} ({Loader}) ({DependsOn.Length}) @0x{OrderEntryOffset:X}/0x{NamesEntryOffset:X}";
		}

	}

}
