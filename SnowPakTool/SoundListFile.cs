using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnowPakTool {

	/// <summary>
	/// sound.sound_list reader/writer.
	/// </summary>
	public static class SoundListFile {

		/// <summary>
		/// Reads sound.sound_list file.
		/// </summary>
		public static IEnumerable<string> ReadEntries ( string location , bool keepInternal = false ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			return Generator ( location , keepInternal );

			static IEnumerable<string> Generator ( string location , bool keepInternal ) {
				using var stream = File.OpenRead ( location );
				var count = stream.ReadValue<long> ();
				for ( long i = 0; i < count; i++ ) {
					var length = stream.ReadValue<long> ();
					if ( length > int.MaxValue ) throw new NotSupportedException ( $"Name length exceeds int32 range." );
					var name = stream.ReadString ( (int) length );
					if ( !keepInternal ) {
						name = LoadListAssetEntry.InternalNameToExternalName ( name , out _ ); //let's assume for now the format is the same (and not the cache_block one)
					}
					yield return name;
				}
			}
		}

		/// <summary>
		/// Writes sound.sound_list file.
		/// </summary>
		public static void WriteEntries ( string location , IReadOnlyCollection<string> names ) {
			if ( location is null ) throw new ArgumentNullException ( nameof ( location ) );
			if ( names is null ) throw new ArgumentNullException ( nameof ( names ) );

			using var stream = File.Open ( location , FileMode.CreateNew , FileAccess.Write , FileShare.Read );
			stream.WriteValue<long> ( names.Count );
			foreach ( var name in names ) {
				var internalName = name.Length > 0 && name[0] == '<' ? name : LoadListAssetEntry.ExternalNameToInternalName ( name );
				var bytes = MiscHelpers.Encoding.GetBytes ( internalName );
				stream.WriteValue<long> ( bytes.Length );
				stream.Write ( bytes , 0 , bytes.Length );
			}
		}

	}

}
