using System;
using System.IO;

namespace SnowPakTool {

	public abstract class LoadListEntryBase {

		public static int ExpectedMagicBCount => 2;
		public static byte ExpectedMagicAValue => 1;
		public static byte ExpectedMagicBValue => 1;

		public int Index { get; set; }
		public abstract LoadListEntryType Type { get; }
		public virtual int StringsCount => 0;
		public long DependencyEntryOffset { get; set; }
		public long StringsEntryOffset { get; set; }
		public byte[] MagicA { get; set; }
		public byte[] MagicB { get; set; }
		public int[] DependsOn { get; set; }

		public static LoadListEntryBase FromType ( LoadListEntryType type ) {
			switch ( type ) {
				case LoadListEntryType.Start: return new LoadListStartEntry ();
				case LoadListEntryType.Stage: return new LoadListStageEntry ();
				case LoadListEntryType.Asset: return new LoadListAssetEntry ();
				case LoadListEntryType.End: return new LoadListEndEntry ();
				default: throw new NotSupportedException ();
			}
		}


		public virtual bool IsValidStringsCount ( int count ) {
			return count == 0;
		}

		public virtual void WriteType ( Stream stream ) {
			stream.WriteByte ( (byte) Type );
		}

		public virtual void WriteDependencies ( Stream stream ) {
			stream.WriteValue ( DependsOn?.Length ?? 0 );
			stream.WriteByte ( 1 ); //data type?
			if ( DependsOn != null ) {
				stream.WriteValuesArray ( DependsOn );
			}
		}

		public virtual void LoadStrings ( string[] strings ) {
			// Method intentionally left empty.
		}

		public virtual void WriteStrings ( Stream stream ) {
			if ( MagicA != null && MagicA.Length != StringsCount ) throw new NotSupportedException ( $"{nameof ( MagicA )} can only contain {StringsCount} point(s) of magic." );
			stream.WriteValue ( StringsCount );
			stream.WriteValue ( MagicB?.Length ?? ExpectedMagicBCount );
			WriteMagic ( stream , MagicA , StringsCount , ExpectedMagicAValue );
			WriteMagic ( stream , MagicB , ExpectedMagicBCount , ExpectedMagicBValue );
		}

		public override string ToString () {
			return $"[{Index}] {Type} ({DependsOn.Length}) @0x{DependencyEntryOffset:X}/0x{StringsEntryOffset:X}";
		}



		private static void WriteMagic ( Stream stream , byte[] magic , int defaultAmount , byte defaultMagic ) {
			if ( magic == null ) {
				for ( int i = 0; i < defaultAmount; i++ ) {
					stream.WriteByte ( defaultMagic );
				}
			}
			else {
				foreach ( var value in magic ) {
					stream.WriteByte ( value );
				}
			}

		}

	}

}
