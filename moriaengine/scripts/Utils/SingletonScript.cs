
namespace SteamEngine.CompiledScripts {

	/// <summary>Generic-based store for singleton AbstractScript descendants, for easy access in C# scripts.</summary>
	public static class SingletonScript<T> where T : AbstractScript {
		static T instance = FindSingletonInstance();

		public static T Instance {
			get {
				if (instance == null) {
					throw new FatalException("No instance found of class " + typeof(T) + ".");
				}
				return instance;
			}
		}

		private static T FindSingletonInstance() {
			foreach (AbstractScript script in AbstractScript.AllScripts) {
				T castScript = script as T;
				if (castScript != null) {
					if (instance != null) {
						throw new FatalException(typeof(T) + " is not a singleton script class, you can't use it as such.");
					}
					return castScript;
				}
			}
			return null;
		}
	}
}