using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SnowTruckConfig.Tests {

	[TestClass]
	public class StringsFile_GetStrings_Tests {

		[TestMethod]
		public void Throws_for_null () {
			Assert.ThrowsException<ArgumentNullException> ( () => StringsFile.GetString ( null , out var str ) );
		}

		[TestMethod]
		public void Returns_empty_for_empty_string () {
			var i = StringsFile.GetString ( "" , out var str );
			Assert.AreEqual ( 0 , i );
			Assert.AreEqual ( "" , str );
		}

		[DataTestMethod]
		//whitespace
		[DataRow ( "  " , "" , 2 )]
		[DataRow ( "\t\t" , "" , 2 )]
		//no quotes, no escapes
		[DataRow ( "zozo" , "zozo" , 4 )]
		[DataRow ( "  zozo" , "zozo" , 6 )]
		[DataRow ( "zozo  " , "zozo" , 4 )]
		[DataRow ( "  zozo  " , "zozo" , 6 )]
		//mismatched quotes
		[DataRow ( "\"" , "" , 1 )]
		[DataRow ( "  \"" , "" , 3 )]
		[DataRow ( "\"  " , "  " , 3 )]
		[DataRow ( "  \" zozo " , " zozo " , 9 )]
		//proper quotes
		[DataRow ( "\"  \"" , "  " , 4 )]
		[DataRow ( "  \"  \"  " , "  " , 6 )]
		[DataRow ( "  \" zozo \"  " , " zozo " , 10 )]
		//incomplete escape
		[DataRow ( @"zozo\" , "zozo" , 5 )]
		//escapes
		[DataRow ( @"zo\\zo" , @"zo\zo" , 6 )]
		[DataRow ( @"zo\""zo" , @"zo""zo" , 6 )]
		[DataRow ( @"zo\nzo" , "zo\nzo" , 6 )]
		[DataRow ( @" ""zo\""zo"" " , @"zo""zo" , 9 )]
		public void Parses_strings ( string line , string expected , int end ) {
			var i = StringsFile.GetString ( line , out var str );
			Assert.AreEqual ( end , i );
			Assert.AreEqual ( expected , str );
		}

	}

}
