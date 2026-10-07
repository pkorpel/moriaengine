using System;

namespace PerCederberg.Grammatica.Parser {

	public class RecursionTooDeepException : Exception {
		public RecursionTooDeepException(string s) : base(s) {

		}

		public RecursionTooDeepException(string s, Exception e) : base(s, e) {
		}
	}
}
