using System;

namespace SteamEngine.Persistence {
	/// <summary>Decorate your class by this attribute if you want it to be automatically loadable and saveable.</summary>
	/// <seealso cref="SaveableDataAttribute"/>
	/// <seealso cref="LoadSectionAttribute"/>
	/// <seealso cref="LoadingInitializerAttribute"/>
	/// <seealso cref="LoadLineAttribute"/>
	/// <seealso cref="SaveAttribute"/>
	/// <seealso cref="LoadingFinalizerAttribute"/>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class SaveableClassAttribute : Attribute {
		private readonly string? description;

		//the name of the saveableclass displayed in settings dialog
		public string? Description {
			get {
				return this.description;
			}
		}

		public SaveableClassAttribute() {
		}

		public SaveableClassAttribute(string description) {
			this.description = description;
		}
	}

	/// <summary>
	/// Use to mark the simple initializer of loaded instances
	/// </summary>
	/// <remarks>
	/// Use this attribute to decorate the constructor or static method of your class which should
	/// be used for creating of an the "empty" instance. This instance will then be populated using the
	/// LoadLine- and/or SaveableData-decorated members. This method or constructor should be public and have no parameters.
	/// </remarks>
	/// <seealso cref="SaveableDataAttribute"/>
	/// <seealso cref="SaveableClassAttribute"/>
	/// <seealso cref="LoadSectionAttribute"/>
	/// <seealso cref="LoadLineAttribute"/>
	/// <seealso cref="SaveAttribute"/>
	/// <seealso cref="LoadingFinalizerAttribute"/>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
	public sealed class LoadingInitializerAttribute : Attribute {
	}


	/// <summary>
	/// Use to mark the simple initializer of loaded instances
	/// </summary>
	/// Use this attribute to decorate the method of your class which should
	/// be called after all other loading methods. This method should be public and have no parameters.
	/// <seealso cref="SaveableDataAttribute"/>
	/// <seealso cref="SaveableClassAttribute"/>
	/// <seealso cref="LoadSectionAttribute"/>
	/// <seealso cref="LoadLineAttribute"/>
	/// <seealso cref="SaveAttribute"/>
	/// <seealso cref="LoadingInitializerAttribute"/>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class LoadingFinalizerAttribute : Attribute {
	}

	/// <summary>Use to mark the initializer that takes whole saved section as it's parameter.</summary>
	/// <remarks>
	/// Use this attribute to decorate the static method or constructor of your class which should
	/// be used for creating of a "fully populated" instance. Note that you can not use any of the other auto-loading
	/// (SaveableData, LoadLine, LoadingInitializer) attributes if you use this one. In other words, this
	/// should implement the whole loading by itself. So, it must be a public (static) member with one
	/// parameter of type PropsSection, returning the loaded instance.
	/// </remarks>
	/// <seealso cref="LoadingInitializerAttribute"/>
	/// <seealso cref="SaveableDataAttribute"/>
	/// <seealso cref="LoadingInitializerAttribute"/>
	/// <seealso cref="LoadingFinalizerAttribute"/>
	/// <seealso cref="LoadLineAttribute"/>
	/// <seealso cref="SaveAttribute"/>
	/// <seealso cref="SaveableClassAttribute"/>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
	public sealed class LoadSectionAttribute : Attribute {
	}

	/// <summary>Use to mark the instance method that is repeatedly called for every line in the loaded section.</summary>
	/// <remarks>
	/// Decorate only public instance methods which have one string parameter and return void.
	/// Note that if there are also any SaveableDataAttribute-decorated members in the class, they
	/// will be loaded first.
	/// </remarks>
	/// <seealso cref="LoadingInitializerAttribute"/>
	/// <seealso cref="SaveableDataAttribute"/>
	/// <seealso cref="LoadSectionAttribute"/>
	/// <seealso cref="LoadingInitializerAttribute"/>
	/// <seealso cref="LoadingFinalizerAttribute"/>
	/// <seealso cref="SaveAttribute"/>
	/// <seealso cref="SaveableClassAttribute"/>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class LoadLineAttribute : Attribute {
	}

	/// <summary>Use to mark the method that writes out the save section for instances of this class.</summary>
	/// <remarks>
	/// Use this attribute to decorate the instance method that takes one SaveStream parameter
	/// and writes the needed saving data into it.
	/// Unless there is an according IBaseClassSaveCoordinator, you actually implement only saving of the "body" of the section,
	/// not it's header, because that is reserved for use by the ObjectSaver class.
	/// Also note that there are certain format restrictions you should (or must) follow.
	/// For example, the obvious one is that you can't use the format of the header ;)
	/// Basically, you should follow the "name = value" way of saving.
	/// If you use SaveableDataAttributes along with this, note that this method will be called before
	/// applying those attributes.
	/// </remarks>
	/// <seealso cref="LoadingInitializerAttribute"/>
	/// <seealso cref="SaveableDataAttribute"/>
	/// <seealso cref="LoadSectionAttribute"/>
	/// <seealso cref="LoadingInitializerAttribute"/>
	/// <seealso cref="LoadingFinalizerAttribute"/>
	/// <seealso cref="LoadLineAttribute"/>
	/// <seealso cref="SaveableClassAttribute"/>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class SaveAttribute : Attribute {
	}
}
