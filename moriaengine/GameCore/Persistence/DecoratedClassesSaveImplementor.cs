/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;

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
		private string description;

		//the name of the saveableclass displayed in settings dialog
		public string Description {
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
	/// be used for creating of an the \"empty\" instance. This instance will then be populated using the 
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
	/// be used for creating of a \"fully populated\" instance. Note that you can not use any of the other auto-loading
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
		//no params
	}

	/// <summary>Use to mark the instance fields and properties to be automatically saved and loaded.</summary>
	/// <remarks>
	/// Note that any properties decorated by this attribute must be simple, that means no indexers.
	/// Also note that these fields and attributes must be public.
	/// </remarks>
	/// <seealso cref="LoadingInitializerAttribute"/>
	/// <seealso cref="LoadSectionAttribute"/>
	/// <seealso cref="LoadingInitializerAttribute"/>
	/// <seealso cref="LoadingFinalizerAttribute"/>
	/// <seealso cref="LoadLineAttribute"/>
	/// <seealso cref="SaveAttribute"/>
	/// <seealso cref="SaveableClassAttribute"/>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class SaveableDataAttribute : Attribute {
		private string description;

		public string Description {
			get {
				return this.description;
			}
		}

		//no params constructor
		public SaveableDataAttribute() {
		}

		//constructor allowing us to specify the displayed name of the attribute
		public SaveableDataAttribute(string description) {
			this.description = description;
		}
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
		//no params
	}

	/// <summary>Use to mark the method that writes out the save section for instances of this class.</summary>
	/// <remarks>
	/// Use this attribute to decorate the instance method that takes one SaveStream parameter
	/// and writes the needed saving data into it. 
	/// Unless there is an according IBaseClassSaveCoordinator, you actually implement only saving of the \"body\" of the section, 
	/// not it's header, because that is reserved for use by the ObjectSaver class.
	/// Also note that there are certain format restrictions you should (or must) follow. 
	/// For example, the obvious one is that you can't use the format of the header ;)
	/// Basically, you should follow the \"name = value\" way of saving.
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
		//no params
	}

	public abstract class DecoratedClassesSaveImplementor : ISaveImplementor {
		Type handledType;
		string headerName;

		protected DecoratedClassesSaveImplementor(Type handledType, string headerName) {
			this.handledType = handledType;
			this.headerName = headerName;
		}

		public Type HandledType {
			get {
				return this.handledType;
			}
		}

		public string HeaderName {
			get {
				return this.headerName;
			}
		}

		public abstract object LoadSection(PropsSection input);
		public abstract void Save(object objToSave, SaveStream writer);

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "1#")]
		protected void LoadSectionLines(PropsSection ps, object loadedObject) {
			foreach (PropsLine p in ps.PropsLines) {
				try {
					this.LoadLineImpl(loadedObject, ps.Filename, p.Line, p.Name.ToLowerInvariant(), p.Value);
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteWarning(ps.Filename, p.Line, ex);
				}
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
		protected virtual void LoadLineImpl(object loadedObject, string filename, int line, string name, string value) {
			throw new SEException("This should not happen.");
		}
	}

	internal sealed class DecoratedClassesSaveImplementorGenerator : ISteamCSCodeGenerator {
		static List<Type> decoratedClasses = new List<Type>();

		internal static bool AddDecoratedClass(Type t, SaveableClassAttribute ignored) {
			decoratedClasses.Add(t);
			return false;
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static void Bootstrap() {
			ClassManager.RegisterSupplyDecoratedClasses<SaveableClassAttribute>(AddDecoratedClass, false);
		}

		public string FileName {
			get {
				return "DecoratedClassesSaveImplementors.Generated.cs";
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public CodeCompileUnit WriteSources() {
			try {
				CodeCompileUnit codeCompileUnit = new CodeCompileUnit();


				if (decoratedClasses.Count > 0) {
					Logger.WriteDebug("Generating SaveImplementors");

					CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts");
					codeCompileUnit.Namespaces.Add(ns);

					foreach (Type decoratedClass in decoratedClasses) {
						try {
							GeneratedInstance gi = new GeneratedInstance(decoratedClass);
							CodeTypeDeclaration ctd = gi.GetGeneratedType();
							ns.Types.Add(ctd);
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(decoratedClass.Assembly.GetName().Name, decoratedClass.Name, e);
							return null;
						}
					}
					Logger.WriteDebug("Done generating " + decoratedClasses.Count + " SaveImplementors");
				}
				return codeCompileUnit;
			} finally {
				decoratedClasses.Clear();
			}
		}

		public void HandleAssembly(Assembly compiledAssembly) {

		}

		private class GeneratedInstance {
			Type decoratedClass;
			MethodBase loadingInitializer;
			List<FieldInfo> saveableDataFields = new List<FieldInfo>();
			List<PropertyInfo> saveableDataProperties = new List<PropertyInfo>();
			MethodBase loadSection;
			MethodInfo loadLine;
			MethodInfo loadingFinalizer;
			MethodInfo saveMethod;

			internal GeneratedInstance(Type decoratedClass) {
				this.decoratedClass = decoratedClass;
				foreach (MemberInfo mi in decoratedClass.GetMembers(
						BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
					this.HandleLoadingInitializerAttribute(mi);
					this.HandleLoadLineAttribute(mi);
					this.HandleSaveableDataAttribute(mi);
					this.HandleLoadSectionAttribute(mi);
					this.HandleLoadingFinalizerAttribute(mi);
					this.HandleSaveAttribute(mi);
				}

				//now check if we have all attributes we need and if we don't have any evil combination of them
				if (this.loadSection != null) {
					if ((this.loadLine != null) || (this.loadingInitializer != null)) {
						//(saveableDataFields.Count != 0) || (saveableDataProperties.Count != 0)) {
						throw new SEException("Can not use the " + LogStr.Ident("LoadSectionAttribute") + " along with other auto-loading attributes.");
					}
				} else if (this.loadingInitializer == null) {
					throw new SEException("No way to instantiate this class when autoloading.");
				} else if ((this.loadLine == null) && (this.saveableDataFields.Count == 0) && (this.saveableDataProperties.Count == 0)) {
					throw new SEException("No way to load members of this class when autoloading (members not public?).");
				}
				if ((this.saveMethod == null) && (this.saveableDataFields.Count == 0) && (this.saveableDataProperties.Count == 0)) {
					throw new SEException("No way to autosave this class.");
				}
			}

			private void HandleSaveAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(SaveAttribute), false)) {
					if (this.saveMethod != null) {
						throw new SEException("Can not use the " + LogStr.Ident("SaveAttribute") + " on two class members.");
					}
					if ((mi.MemberType & MemberTypes.Method) == MemberTypes.Method) {
						MethodInfo meth = (MethodInfo) mi;
						if (!meth.IsStatic) {
							ParameterInfo[] pars = meth.GetParameters();
							if ((pars.Length == 1) && (pars[0].ParameterType == typeof(SaveStream))) {
								this.saveMethod = meth;
							}
						}
					}
					if (this.saveMethod == null) {
						throw new SEException(LogStr.Ident("SaveAttribute") + " can only be placed on an instance method with one parameter of type SaveStream.");
					}
				}
			}

			private void HandleLoadSectionAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(LoadSectionAttribute), false)) {
					if (this.loadSection != null) {
						throw new SEException("Can not use the " + LogStr.Ident("LoadSectionAttribute") + " on two class members.");
					}
					if ((mi.MemberType & MemberTypes.Constructor) == MemberTypes.Constructor) {
						this.loadSection = (MethodBase) mi;
					} else if ((mi.MemberType & MemberTypes.Method) == MemberTypes.Method) {
						MethodInfo meth = (MethodInfo) mi;
						if (meth.IsStatic) {
							this.loadSection = meth;
						}
					}
					if (this.loadSection != null) {
						ParameterInfo[] pars = this.loadSection.GetParameters();
						if ((pars.Length != 1) || (pars[0].ParameterType != typeof(PropsSection))) {
							throw new SEException(LogStr.Ident("LoadSectionAttribute") + " can only be placed on a callable member with one parameter of type PropsSection.");
						}
					} else {
						throw new SEException(LogStr.Ident("LoadSectionAttribute") + " can only be placed on a constructor or static method.");
					}
				}
			}

			private void HandleSaveableDataAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(SaveableDataAttribute), false)) {
					bool added = false;
					if ((mi.MemberType & MemberTypes.Property) == MemberTypes.Property) {
						PropertyInfo pi = (PropertyInfo) mi;
						MethodInfo[] accessors = pi.GetAccessors();
						if (accessors.Length == 2) {
							if (!accessors[0].IsStatic) {
								this.saveableDataProperties.Add(pi);
								added = true;
							}
						} else {
							throw new SEException(LogStr.Ident("SaveableDataAttribute") + " can only be placed on fields or properties with both setter and getter.");
						}
					} else if ((mi.MemberType & MemberTypes.Field) == MemberTypes.Field) {
						FieldInfo fi = (FieldInfo) mi;
						if (!fi.IsStatic) {
							this.saveableDataFields.Add(fi);
							added = true;
						}
					}
					if (!added) {
						throw new SEException(LogStr.Ident("SaveableDataAttribute") + " can only be placed on instance fields or properties.");
					}
				}
			}

			private void HandleLoadLineAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(LoadLineAttribute), false)) {
					if (this.loadLine != null) {
						throw new SEException("Can not use the " + LogStr.Ident("LoadLineAttribute") + " on two class members.");
					}
					if ((mi.MemberType & MemberTypes.Method) == MemberTypes.Method) {
						MethodInfo meth = (MethodInfo) mi;
						if (!meth.IsStatic) {
							ParameterInfo[] pars = meth.GetParameters();
							if ((pars.Length == 4) && (pars[0].ParameterType == typeof(string)) && (pars[1].ParameterType == typeof(int))
							    && (pars[2].ParameterType == typeof(string)) && (pars[3].ParameterType == typeof(string))) {
								this.loadLine = meth;
							}
						}
					}
					if (this.loadLine == null) {
						throw new SEException(LogStr.Ident("LoadLineAttribute") + " can only be placed on an instance method with four parameters of types string, int, string, string.");
					}
				}
			}

			private void HandleLoadingInitializerAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(LoadingInitializerAttribute), false)) {
					if (this.loadingInitializer != null) {
						throw new SEException("Can not use the " + LogStr.Ident("LoadingInitializerAttribute") + " on two class members.");
					}
					if ((mi.MemberType & MemberTypes.Constructor) == MemberTypes.Constructor) {
						this.loadingInitializer = (MethodBase) mi;
					} else if ((mi.MemberType & MemberTypes.Method) == MemberTypes.Method) {
						MethodInfo meth = (MethodInfo) mi;
						if (meth.IsStatic) {
							this.loadingInitializer = meth;
						}
					}
					if (this.loadingInitializer != null) {
						if (this.loadingInitializer.GetParameters().Length != 0) {
							throw new SEException(LogStr.Ident("LoadingInitializerAttribute") + " can only be placed on a callable member with no parameters.");
						}
					} else {
						throw new SEException(LogStr.Ident("LoadingInitializerAttribute") + " can only be placed on a constructor or static method.");
					}
				}
			}

			private void HandleLoadingFinalizerAttribute(MemberInfo mi) {
				if (mi.IsDefined(typeof(LoadingFinalizerAttribute), false)) {
					if (this.loadingFinalizer != null) {
						throw new SEException("Can not use the " + LogStr.Ident("LoadingFinalizerAttribute") + " on two class members.");
					}
					if ((mi.MemberType & MemberTypes.Method) == MemberTypes.Method) {
						MethodInfo meth = (MethodInfo) mi;
						if ((!meth.IsStatic) && (meth.GetParameters().Length == 0)) {
							this.loadingFinalizer = meth;
						}
					}
					if (this.loadingFinalizer == null) {
						throw new SEException(LogStr.Ident("LoadingFinalizerAttribute") + " can only be placed on an instance method with no parameters.");
					}
				}
			}

			internal CodeTypeDeclaration GetGeneratedType() {
				CodeTypeDeclaration codeTypeDeclatarion = new CodeTypeDeclaration("GeneratedSaveImplementor_" + this.decoratedClass.Name);
				codeTypeDeclatarion.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclatarion.BaseTypes.Add(typeof(DecoratedClassesSaveImplementor));
				codeTypeDeclatarion.IsClass = true;

				codeTypeDeclatarion.Members.Add(this.GenerateConstructor());
				this.GenerateLoadSectionMethod(codeTypeDeclatarion.Members);
				codeTypeDeclatarion.Members.Add(this.GenerateSaveMethod());

				return codeTypeDeclatarion;
			}

			private CodeConstructor GenerateConstructor() {
				CodeConstructor retVal = new CodeConstructor();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Final;

				retVal.BaseConstructorArgs.Add(new CodeTypeOfExpression(this.decoratedClass));
				retVal.BaseConstructorArgs.Add(new CodePrimitiveExpression(this.decoratedClass.Name));

				return retVal;
			}

			private void GenerateLoadSectionMethod(CodeTypeMemberCollection methods) {
				CodeMemberMethod loadSectionMethod = new CodeMemberMethod();
				loadSectionMethod.Attributes = MemberAttributes.Public | MemberAttributes.Override;

				loadSectionMethod.Name = "LoadSection";
				loadSectionMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(PropsSection), "input"));
				loadSectionMethod.ReturnType = new CodeTypeReference(typeof(object));

				//int currentLineNumber = input.headerLine;
				loadSectionMethod.Statements.Add(new CodeVariableDeclarationStatement(
					typeof(int), "currentLineNumber",
					new CodePropertyReferenceExpression(
						new CodeArgumentReferenceExpression("input"),
						"HeaderLine")));

				CodeTryCatchFinallyStatement trycatch = new CodeTryCatchFinallyStatement();
				loadSectionMethod.Statements.Add(trycatch);

				//Memory loadedObject = new Memory();
				CodeExpression createObjectExpression;
				if (this.loadSection != null) {
					if (this.loadSection.IsConstructor) {
						createObjectExpression = new CodeObjectCreateExpression(
							this.decoratedClass, new CodeArgumentReferenceExpression("input"));
					} else {
						createObjectExpression = new CodeMethodInvokeExpression(
							new CodeTypeReferenceExpression(this.decoratedClass),
							this.loadSection.Name,
							new CodeArgumentReferenceExpression("input"));

						Type returnedType = ((MethodInfo) this.loadSection).ReturnType;
						if (this.decoratedClass != returnedType) {
							createObjectExpression = new CodeCastExpression(
								this.decoratedClass,
								createObjectExpression);
						}
					}
				} else if (this.loadingInitializer.IsConstructor) {
					createObjectExpression = new CodeObjectCreateExpression(
						this.decoratedClass);
				} else {
					createObjectExpression = new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(this.decoratedClass),
						this.loadingInitializer.Name);
				}

				trycatch.TryStatements.Add(new CodeVariableDeclarationStatement(
					this.decoratedClass, "loadedObject", createObjectExpression));

				if (this.loadSection == null) {
					foreach (PropertyInfo pi in this.saveableDataProperties) {
						CodeAssignStatement propertyAssignment = new CodeAssignStatement();
						propertyAssignment.Left = new CodePropertyReferenceExpression(
							new CodeVariableReferenceExpression("loadedObject"),
							pi.Name);

						this.GenerateLoadFieldOrProperty(methods, trycatch.TryStatements, propertyAssignment,
							pi.Name, pi.PropertyType);
					}

					foreach (FieldInfo fi in this.saveableDataFields) {
						CodeAssignStatement fieldAssignment = new CodeAssignStatement();
						fieldAssignment.Left = new CodeFieldReferenceExpression(
							new CodeVariableReferenceExpression("loadedObject"),
							fi.Name);
						this.GenerateLoadFieldOrProperty(methods, trycatch.TryStatements, fieldAssignment,
							fi.Name, fi.FieldType);
					}
				}

				//we generate a method that only will call the real LoadLine method, and we call it by a 
				//helper method on DecoratedClassSaveImplementor. The reason is that there's no foreach codedom statement
				if (this.loadLine != null) {
					trycatch.TryStatements.Add(new CodeMethodInvokeExpression(
						new CodeThisReferenceExpression(),
						"LoadSectionLines",
						new CodeArgumentReferenceExpression("input"),
						new CodeVariableReferenceExpression("loadedObject")));
					methods.Add(this.GenerateLoadLineImplMethod());
				}

				if (this.loadingFinalizer != null) {
					trycatch.TryStatements.Add(new CodeMethodInvokeExpression(
						new CodeVariableReferenceExpression("loadedObject"),
						this.loadingFinalizer.Name));
				}

				//return
				trycatch.TryStatements.Add(new CodeMethodReturnStatement(
					new CodeVariableReferenceExpression("loadedObject")));

				//catch (SteamEngine.FatalException ) { throw; }
				trycatch.CatchClauses.Add(new CodeCatchClause("",
					new CodeTypeReference(typeof(FatalException)),
						new CodeThrowExceptionStatement()));

				//} catch (SEException sex) {sex.TryAddFileLineInfo(input.filename, currentLineNumber);throw;}
				trycatch.CatchClauses.Add(new CodeCatchClause("sex",
					new CodeTypeReference(typeof(SEException)),
						new CodeExpressionStatement(
							new CodeMethodInvokeExpression(
								new CodeVariableReferenceExpression("sex"),
								"TryAddFileLineInfo",
								new CodePropertyReferenceExpression(
									new CodeArgumentReferenceExpression("input"),
									"Filename"),
								new CodeVariableReferenceExpression("currentLineNumber"))),
						new CodeThrowExceptionStatement()));

				//throw new SEException(Common.LogStr.FileLine(input.filename, currentLineNumber)+CoreLogger.ErrText(e));
				trycatch.CatchClauses.Add(new CodeCatchClause("e",
					new CodeTypeReference(typeof(Exception)),
					new CodeThrowExceptionStatement(
						new CodeObjectCreateExpression(typeof(SEException),
							new CodePropertyReferenceExpression(
								new CodeArgumentReferenceExpression("input"),
								"Filename"),
							new CodeVariableReferenceExpression("currentLineNumber"),
							new CodeVariableReferenceExpression("e")))));

				methods.Add(loadSectionMethod);
			}

			private CodeMemberMethod GenerateLoadLineImplMethod() {
				CodeMemberMethod loadLineMethod = new CodeMemberMethod();
				loadLineMethod.Attributes = MemberAttributes.Family | MemberAttributes.Override;

				loadLineMethod.Name = "LoadLineImpl";
				loadLineMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "loadedObject"));
				loadLineMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "filename"));
				loadLineMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "line"));
				loadLineMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
				loadLineMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value"));
				loadLineMethod.ReturnType = new CodeTypeReference(typeof(void));

				loadLineMethod.Statements.Add(new CodeVariableDeclarationStatement(
					this.decoratedClass,
					"castLoadedObject",
					new CodeCastExpression(
						this.decoratedClass,
						new CodeArgumentReferenceExpression("loadedObject"))));

				loadLineMethod.Statements.Add(new CodeMethodInvokeExpression(
					new CodeVariableReferenceExpression("castLoadedObject"),
					this.loadLine.Name,
					new CodeArgumentReferenceExpression("filename"),
					new CodeArgumentReferenceExpression("line"),
					new CodeArgumentReferenceExpression("name"),
					new CodeArgumentReferenceExpression("value")));

				return loadLineMethod;
			}

			private void GenerateLoadFieldOrProperty(CodeTypeMemberCollection methods,
					CodeStatementCollection statements, CodeAssignStatement assignment, string name, Type type) {

				string propsLineName = Utility.Uncapitalize(name + "Line");

				statements.Add(new CodeVariableDeclarationStatement(
					typeof(PropsLine), propsLineName,
					new CodeMethodInvokeExpression(
						new CodeArgumentReferenceExpression("input"),
						"TryPopPropsLine",
						new CodePrimitiveExpression(name))));

				CodeConditionStatement ifStatement = new CodeConditionStatement(
					new CodeBinaryOperatorExpression(
						new CodeVariableReferenceExpression(propsLineName),
						CodeBinaryOperatorType.IdentityEquality,
						new CodePrimitiveExpression(null)),
					new CodeExpressionStatement(new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(CoreLogger)),
						"WriteWarning",
						new CodePropertyReferenceExpression(
							new CodeArgumentReferenceExpression("input"),
							"Filename"),
						new CodePropertyReferenceExpression(
							new CodeArgumentReferenceExpression("input"),
							"HeaderLine"),
						new CodePrimitiveExpression("Missing value '" + name + "' in loaded section"))));
				statements.Add(ifStatement);

				ifStatement.FalseStatements.Add(new CodeAssignStatement(
					new CodeVariableReferenceExpression("currentLineNumber"),
					new CodePropertyReferenceExpression(
						new CodeVariableReferenceExpression(propsLineName),
						"Line")));

				if (ObjectSaver.IsSimpleSaveableType(type)) {
					//we directly assign the value to the field/property
					assignment.Right = GeneratedCodeUtil.GenerateSimpleLoadExpression(
						type,
						new CodePropertyReferenceExpression(
							new CodeVariableReferenceExpression(propsLineName),
							"Value"));

					ifStatement.FalseStatements.Add(assignment);

				} else {
					//we create a method for delayed loading of the value

					CodeMemberMethod delayedLoadMethod = this.GenerateDelayedLoadMethod(assignment, name, type);
					methods.Add(delayedLoadMethod);

					//ObjectSaver.Load(value, new LoadObject(LoadSomething_Delayed), filename, line);
					ifStatement.FalseStatements.Add(new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
							new CodeTypeReferenceExpression(typeof(ObjectSaver)), "Load"),
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression(propsLineName),
								"Value"),
							new CodeDelegateCreateExpression(
								new CodeTypeReference(typeof(LoadObjectParam)),
								new CodeThisReferenceExpression(),
								delayedLoadMethod.Name),
							new CodePropertyReferenceExpression(
								new CodeArgumentReferenceExpression("input"),
								"Filename"),
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression(propsLineName),
								"Line"),
							new CodeVariableReferenceExpression("loadedObject")));

					//
				}
			}

			private CodeMemberMethod GenerateDelayedLoadMethod(CodeAssignStatement assignment, string name, Type type) {

				CodeMemberMethod delayedLoadMethod = new CodeMemberMethod();
				delayedLoadMethod.Attributes = MemberAttributes.Private;

				delayedLoadMethod.Name = "DelayedLoad_" + name;
				delayedLoadMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "resolved"));
				delayedLoadMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "filename"));
				delayedLoadMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(int), "line"));
				delayedLoadMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "param"));
				delayedLoadMethod.ReturnType = new CodeTypeReference(typeof(void));

				//Memory loadedObject = (Memory) param;
				delayedLoadMethod.Statements.Add(new CodeVariableDeclarationStatement(
					this.decoratedClass, "loadedObject",
					new CodeCastExpression(this.decoratedClass,
						new CodeArgumentReferenceExpression("param"))));

				assignment.Right = GeneratedCodeUtil.GenerateDelayedLoadExpression(
					type,
					new CodeArgumentReferenceExpression("resolved"));

				delayedLoadMethod.Statements.Add(assignment);

				return delayedLoadMethod;
			}


			private CodeMemberMethod GenerateSaveMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;

				retVal.Name = "Save";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "objToSave"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SaveStream), "writer"));
				retVal.ReturnType = new CodeTypeReference(typeof(void));

				//Memory castObjToSave = (Memory) objToSave;
				retVal.Statements.Add(new CodeVariableDeclarationStatement(
					this.decoratedClass, "castObjToSave",
					new CodeCastExpression(this.decoratedClass,
						new CodeArgumentReferenceExpression("objToSave"))));
				if (this.loadSection == null) {
					//writer.WriteValue("Flags", castObjToSave.Flags);
					foreach (PropertyInfo pi in this.saveableDataProperties) {
						retVal.Statements.Add(new CodeMethodInvokeExpression(
							new CodeArgumentReferenceExpression("writer"),
							"WriteValue",
							new CodePrimitiveExpression(pi.Name),
							new CodePropertyReferenceExpression(
								new CodeVariableReferenceExpression("castObjToSave"),
								pi.Name)));
					}
					foreach (FieldInfo fi in this.saveableDataFields) {
						retVal.Statements.Add(new CodeMethodInvokeExpression(
							new CodeArgumentReferenceExpression("writer"),
							"WriteValue",
							new CodePrimitiveExpression(fi.Name),
							new CodeFieldReferenceExpression(
								new CodeVariableReferenceExpression("castObjToSave"),
								fi.Name)));
					}
				}
				if (this.saveMethod != null) {
					retVal.Statements.Add(new CodeMethodInvokeExpression(
						new CodeVariableReferenceExpression("castObjToSave"),
						this.saveMethod.Name,
						new CodeArgumentReferenceExpression("writer")));
				}

				return retVal;
			}
		}
	}
}