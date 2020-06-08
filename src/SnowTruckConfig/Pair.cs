using System.Collections.Generic;

namespace SnowTruckConfig {

	public static class Pair {

		public static KeyValuePair<TKey , TValue> From<TKey, TValue> ( TKey key , TValue value ) {
			return new KeyValuePair<TKey , TValue> ( key , value );
		}

	}

}
