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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.Regions;

namespace SteamEngine.LScript {

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Lazy_Expression : OpNode, IOpNodeHolder {
		//accepts STRING, SimpleExpression, 
		private string name;
		protected OpNode[] args;
		private string formatString;
		private object[] results;
		private bool mustEval;
		private bool noArgs;
		private string classOrNamespaceName = "";
		private bool isClass;

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			return Construct(parent, code, false);
		}

		internal static OpNode Construct(IOpNodeHolder parent, Node code, bool mustEval) {
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			string filename = LScriptMain.GetParentScriptHolder(parent).filename;
			OpNode_Lazy_Expression constructed = new OpNode_Lazy_Expression(
				parent, filename, line, column, code);

			constructed.mustEval = mustEval;
			//LScript.DisplayTree(code);

			if (IsType(code, StrictConstants.STRING)) {
				string identifier = ((Token) code).GetImage();
				//some "keywords"
				if (StringComparer.OrdinalIgnoreCase.Equals(identifier, "args")) { //true for case insensitive
					/*args*/
					return new OpNode_GetArgs(parent, filename, line, column, code);
				}
				if (StringComparer.OrdinalIgnoreCase.Equals(identifier, "this")) { //true for case insensitive
					/*this*/
					return new OpNode_This(parent, filename, line, column, code);
				}
				if (StringComparer.OrdinalIgnoreCase.Equals(identifier, "argvcount")) { //true for case insensitive
					/*argvcount*/
					return new OpNode_ArgvCount(parent, filename, line, column, code);
				}
				if (StringComparer.OrdinalIgnoreCase.Equals(identifier, "true")) { //true for case insensitive
					/*true*/
					return OpNode_Object.Construct(parent, true);
				}
				if (StringComparer.OrdinalIgnoreCase.Equals(identifier, "false")) { //true for case insensitive
					/*false*/
					return OpNode_Object.Construct(parent, false);
				}
				if (StringComparer.OrdinalIgnoreCase.Equals(identifier, "null")) { //true for case insensitive
					/*null*/
					return OpNode_Object.Construct(parent, (object) null);
				}


				//Console.WriteLine("identifier: "+identifier+", token "+code);
				constructed.name = identifier;
				constructed.args = new OpNode[0];
				constructed.noArgs = true;
				return constructed;
			}
			if (IsType(code, StrictConstants.SIMPLE_EXPRESSION)) {
				constructed.name = LScriptMain.GetFirstTokenString(code);
				int current = 1;
				Node caller = code.GetChildAt(current);
				if (IsType(caller, StrictConstants.CALLER)) {
					constructed.GetArgsFrom(caller);
					current++;
				}

				List<OpNode> indexersList = new List<OpNode>();
				while (IsType(code.GetChildAt(current), StrictConstants.INDEXER)) {
					Production indexer = (Production) code.GetChildAt(current);
					//"identifier[...] = ..." - then the assignment "belongs" to the indexer
					if (IsAssigner(code.GetChildAt(current + 1))) {
						indexer.AddChild(((Production) code).PopChildAt(current + 1));
						//this is last one, because assigner being next means there are no nodes anymore
					}
					indexersList.Add(OpNode_Lazy_Indexer.Construct(constructed, indexer));
					current++;
				}

				Node assigner = code.GetChildAt(current);
				if (IsAssigner(assigner)) {
					if (constructed.args == null) {
						constructed.GetArgsFrom(assigner);
					} else {
						throw new InterpreterException("This expression is invalid : 'identifier(...) = ... '",
							LScriptMain.startLine + assigner.GetStartLine(), assigner.GetStartColumn(),
							filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
						//this could also (maybe?) be a parser error, but I wasn`t yet able to make the grammar that way :)
						//for the scripter is it irrelevant - it`s anyway an error at "compile" time.
					}
				} else if (constructed.args == null) {
					constructed.args = new OpNode[0];
				}
				if (indexersList.Count > 0) {
					OpNode[] chain = new OpNode[indexersList.Count + 1];
					chain[0] = constructed;
					for (int i = 0, n = indexersList.Count; i < n; i++) {
						chain[i + 1] = indexersList[i];
					}
					return OpNode_Lazy_ExpressionChain.ConstructFromArray(parent, code, chain);
				}
				return constructed;
			}
			throw new SEException("Passed bad Node type construct. This should not happen.");
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		protected void GetArgsFrom(Node caller) {
			//caller / assigner
			Node arg = caller.GetChildAt(1); //ArgsList or just one expression
			//skipped the caller`s first token - "(" / "=" / " "
			if (IsType(arg, StrictConstants.RIGHT_PAREN)) {
				this.args = new OpNode[0];
				return;
			}
			if (IsType(arg, StrictConstants.ARGS_LIST)) {
                List<OpNode> argsList = new List<OpNode>();
				//ArrayList stringList = new ArrayList();
				StringBuilder sb = new StringBuilder();
				for (int i = 0, n = arg.GetChildCount(); i < n; i += 2) { //step 2 - skipping argsseparators
					sb.Append("{" + i / 2 + "}");
					if (i + 1 < n) {
						Node separator = arg.GetChildAt(i + 1);
						sb.Append(LScriptMain.GetString(separator));
					}
					Node node = arg.GetChildAt(i);
					argsList.Add(LScriptMain.CompileNode(this, node));
				}
                this.args = argsList.ToArray();

				object[] stringTokens = new object[arg.GetChildCount()];
				((Production) arg).children.CopyTo(stringTokens);
				this.formatString = sb.ToString();
			} else {
				OpNode compiled = LScriptMain.CompileNode(this, arg);
				this.args = new[] { compiled };
				this.formatString = "{0}";
			}
		}

		protected OpNode_Lazy_Expression(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.args, oldNode);
			if (index < 0) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
			this.args[index] = newNode;
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal override object Run(ScriptVars vars) {
			//Console.WriteLine("OpNode_Lazy_Expression.Run on "+vars.self);
			//Console.WriteLine("Running as lazyexpression: "+LScript.GetString(origNode));

			Commands.AuthorizeCommandThrow(Globals.Src, this.name);

			bool memberNameMatched = false;
			bool skillKeyMatched = false;

			bool tryInstance = true; //are the members only static or also instance
			bool haveBaseInstance = true; //is here some base instance/type?
			Type seType = null;

			OpNode finalOpNode;
			MemberResolver resolver = null;

			Type type = null;
			if (vars.self == null) {
				haveBaseInstance = false;
			}

			//ArrayList matches = new ArrayList(1); //list of possible OpNodes. only the first one is really used.

			//return
			if (StringComparer.OrdinalIgnoreCase.Equals(this.name, "return")) {
				OpNode newNode;
				if (this.args.Length == 1) {
					newNode = new OpNode_Return(this.parent, this.filename, this.line, this.column, this.OrigNode, this.args[0]);
					this.SetNewParentToArgs((IOpNodeHolder) newNode);
				} else if (this.args.Length > 1) {
					newNode = new OpNode_Return_String(this.parent, this.filename, this.line, this.column, this.OrigNode, this.args, this.formatString);
					this.SetNewParentToArgs((IOpNodeHolder) newNode);
				} else { //args.Length == 0
					//Console.WriteLine("OpNode_Object.Construct from return");
					OpNode nullNode = OpNode_Object.Construct(null, (object) null);
					newNode = new OpNode_Return(this.parent, this.filename, this.line, this.column, this.OrigNode, nullNode);
				}
				this.ReplaceSelf(newNode);

				object oSelf = vars.self;
				vars.self = vars.defaultObject;
				try {
					return newNode.Run(vars);
				} finally {
					vars.self = oSelf;
				}
			}

			//scripts
			AbstractScript ad = AbstractScript.GetByDefname(this.name);
			if (ad != null) {
				finalOpNode = OpNode_Object.Construct(this.parent, ad);
				goto runit;
			}

			//self is namespace
			if (vars.self is NameRef) {
				this.classOrNamespaceName += ((NameRef) vars.self).name;
				if (vars.self is NameSpaceRef) {
					this.isClass = false;
				} else { //vars.self is ClassNameRef
					this.isClass = true;
				}
			}

			if (haveBaseInstance) {
				if (!string.IsNullOrEmpty(this.classOrNamespaceName)) {
					//noArgs
					if (!this.isClass) {
						string className = this.classOrNamespaceName + "." + this.name;
						type = Type.GetType(className, false, true);
						if (type == null) {
							throw new NameRefException(this.line, this.column, this.filename,
								new NameSpaceRef(className), this.ParentScriptHolder.GetDecoratedName());
							//in fact we dunno if it`s a _valid_ namespace name, but there seems to be no way to confirm that
						}
						if (this.noArgs) {//it`s just reference to class, so that the next in chain is a static member, or is it wrong
							throw new NameRefException(this.line, this.column,
								this.filename, new ClassNameRef(className), this.ParentScriptHolder.GetDecoratedName());
						} //it`s a constructor/member with same name
						tryInstance = false;
						//Console.WriteLine("tryInstance = false: {0} ({1}) at {2}:{3}", type, className, this.line, this.column);
					} else {
						ClassNameRef classRef = (ClassNameRef) vars.self;
						type = Type.GetType(classRef.name, false, true);
						if (type == null) {
							type = ClassManager.GetType(classRef.name);
						}
						if (type == null) {
							throw new SEException("We have not found class named " + classRef.name + ", thought we were supposed to. This should not happen.");
						}
						//Console.WriteLine("Type.GetType: {0} ({1}) at {2}:{3}", type, classRef.name, this.line, this.column);
						tryInstance = false;//we look for a static member
					}
				} else if (vars.self == vars.defaultObject) {
					seType = ClassManager.GetType(this.name);//some SteamEngine class from our ClassManager class
					type = vars.self.GetType();
				} else {
					type = vars.self.GetType();
				}
			}
			//I am namespace name ("system")
			if (StringComparer.OrdinalIgnoreCase.Equals(this.name, "system")) {
				throw new NameRefException(this.line, this.column, this.filename,
					new NameSpaceRef(this.name), this.ParentScriptHolder.GetDecoratedName());
				//this exception being thrown doesnt really mean something is wrong. 
				//It may be also harmlessly caught by superior ExpressionChain. but if there is no such present, it becomes really an error
			}

			//arg/local
			if (this.args.Length == 0) {
				OpNode_Lazy_ExpressionChain chainAsParent = this.parent as OpNode_Lazy_ExpressionChain;
				if ((chainAsParent != null) && chainAsParent.GetChildIndex(this) > 0) {
					//we can only resolve as local variable if we're the leftmost string
				} else if (this.ParentScriptHolder.ContainsLocalVarName(this.name)) {
					finalOpNode = new OpNode_GetArg(this.parent, this.filename, this.line, this.column, this.OrigNode, this.name);
					goto runit;
				}
			}

			resolver = MemberResolver.GetInstance(
				vars, this.parent, this.name, this.args, this.line, this.column, this.filename);
			MemberDescriptor desc = null;
			//methods/properties /fields/constructors

			//Console.WriteLine("resolving "+name);
			if (haveBaseInstance) {
				if ((vars.defaultObject is Thing) &&
						((StringComparer.OrdinalIgnoreCase.Equals(this.name, "item")) ||
						(StringComparer.OrdinalIgnoreCase.Equals(this.name, "itemnewbie")) ||
						(StringComparer.OrdinalIgnoreCase.Equals(this.name, "sell")) ||
						(StringComparer.OrdinalIgnoreCase.Equals(this.name, "buy")))) {
					int argsLength = this.args.Length;
					if ((argsLength == 1) || (argsLength == 2)) {
						resolver.RunArgs();
						if ((resolver.results[0] is IThingFactory) && ((argsLength == 1) ||
								((argsLength == 2) && (ConvertTools.IsNumberType(resolver.results[1].GetType()))))) {
							OpNode amountNode;
							if (argsLength == 1) {
								amountNode = OpNode_Object.Construct(this, (uint) 1);
							} else {
								amountNode = this.args[1];
							}
							bool newbie = (StringComparer.OrdinalIgnoreCase.Equals(this.name, "itemnewbie"));
							finalOpNode = new OpNode_TemplateItem(this.parent, this.filename, this.line, this.column, this.OrigNode,
								newbie, this.args[0], amountNode);
							goto runit;
						}
					}
				}
				MemberTypes typesToResolve = (tryInstance ? MemberTypes.All : MemberTypes.Constructor);
				memberNameMatched |= resolver.Resolve(type, BindingFlags.Instance, typesToResolve, out desc);
				if (!this.ResolveAsClassMember(desc, out finalOpNode)) {//in other words, if desc was null
					memberNameMatched |= resolver.Resolve(type, BindingFlags.Static, MemberTypes.All, out desc);
					if (this.ResolveAsClassMember(desc, out finalOpNode)) {
						goto runit;
					}
				} else {
					goto runit;
				}
			}
			if (seType != null) {//try to resolve as constructor
				memberNameMatched |= resolver.Resolve(seType, BindingFlags.Static, MemberTypes.Constructor, out desc);
				if (this.ResolveAsClassMember(desc, out finalOpNode)) {
					goto runit;
				}

				throw new NameRefException(this.line, this.column, this.filename,
					new ClassNameRef(this.name), this.ParentScriptHolder.GetDecoratedName());
			}

			memberNameMatched |= resolver.Resolve(typeof(IntrinsicMethods), BindingFlags.Static, MemberTypes.Method | MemberTypes.Property, out desc);
			if (this.ResolveAsClassMember(desc, out finalOpNode)) {
				goto runit;
			}

			//var
			if (this.args.Length == 0) {
				if (Globals.Instance.HasTag(TagKey.Acquire(this.name))) {
					finalOpNode = new OpNode_GetVar(this.parent, this.filename, this.line, this.column, this.OrigNode, this.name);
					goto runit;
				}
			}
			//function
			ScriptHolder function = ScriptHolder.GetFunction(this.name);
			if (function != null) {
				finalOpNode = new OpNode_Function(this.parent, this.filename, this.line, this.column, this.OrigNode,
					function, this.args, this.formatString);
				goto runit;
			}

			//skillkey
			if (vars.self is AbstractCharacter) {
				AbstractSkillDef sd = AbstractSkillDef.GetByKey(this.name);
				if (sd != null) {
					skillKeyMatched = true;
					if (this.args.Length == 0) {
						finalOpNode = new OpNode_SkillKey_Get(this.parent, this.filename, this.line, this.column, this.OrigNode,
							sd.Id);
						goto runit;
					}
					if (this.args.Length == 1) {
						resolver.RunArgs();
						try {
							Convert.ToInt32(resolver.results[0], CultureInfo.InvariantCulture);
							finalOpNode = new OpNode_SkillKey_Set(this.parent, this.filename, this.line, this.column, this.OrigNode,
								sd.Id, this.args[0]);
							goto runit;
						} catch {//if an exception happens here, it means we can't construct this, so we can ignore the exception
						}
					}
				}
			}

			if (this.args.Length == 0) {
				//constant (defnames)
				Constant con = Constant.GetByName(this.name);
				if (con != null) {
					finalOpNode = new OpNode_Constant(this.parent, this.filename, this.line, this.column, this.OrigNode, con);
					goto runit;
				}

				//regions
				StaticRegion reg = StaticRegion.GetByDefname(this.name);
				if (reg != null) {
					finalOpNode = OpNode_Object.Construct(this.parent, reg);
					goto runit;
				}
			}

			//a little hack for gumps - to make possible to use dialog layout methods without the "argo."
			if ((vars.self == vars.defaultObject) && (vars.scriptArgs != null) && (vars.scriptArgs.Argv.Length > 0)) {
				ScriptedGump sgi = vars.scriptArgs.Argv[0] as ScriptedGump;
				if (sgi != null) {
					if (ScriptedGump.IsMethodName(this.name)) {
						desc = null;
						memberNameMatched = resolver.Resolve(typeof(ScriptedGump), BindingFlags.Instance, MemberTypes.Method, out desc);
						this.ResolveAsClassMember(desc, out finalOpNode);
						if (finalOpNode != null) {
							OpNode_MethodWrapper onmw = (OpNode_MethodWrapper) finalOpNode;
							OpNode_RunOnArgo newNode = new OpNode_RunOnArgo(this.parent, this.filename, this.line, this.column, this.OrigNode, onmw);
							onmw.parent = newNode;
							finalOpNode = newNode;
							goto runit;
						}
					}
				}
			}



			//whateva else?


			//check matches:

			//			if (matches.Count > 1) {
			//				StringBuilder sb = new StringBuilder
			//					("Ambiguity detected when resolving expression. From the following possibilities, "
			//					+"the first one has been chosen as the one with highest priority. Try to write your code more descriptive.");
			//				foreach (OpNode opNode in matches) {
			//					sb.Append(Environment.NewLine);
			//					sb.Append(opNode.ToString());
			//				}
			//				Logger.WriteWarning(filename, line, sb.ToString());
			//			} else if (matches.Count < 1) {
			if (this.mustEval)
			{
				if (memberNameMatched) {
					throw new InterpreterException(
						"Class member (method/property/field/constructor) '" + LogStr.Ident(this.name) + "' is getting wrong arguments",
						this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
				}
				if (skillKeyMatched) {
					throw new InterpreterException(
						"The skill is getting wrong number or type of arguments",
						this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
				}
				throw new InterpreterException("Undefined identifier '" + LogStr.Ident(this.name) + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
			}
			string thisNodeString = this.OrigString;
			//Console.WriteLine("OpNode_Object.Construct from !mustEval");
			OpNode finalStringOpNode = OpNode_Object.Construct(this.parent, thisNodeString);
			this.ReplaceSelf(finalStringOpNode);
			return thisNodeString;
			//			}



runit:	//I know that goto is usually considered dirty, but I find this case quite suitable for it...
			if (resolver != null) {
				this.results = resolver.results;
				MemberResolver.ReturnInstance(resolver);
			}
			//finally run it
			IOpNodeHolder finalAsHolder = finalOpNode as IOpNodeHolder;
			if (finalAsHolder != null) {
				this.SetNewParentToArgs(finalAsHolder);
			}
			this.ReplaceSelf(finalOpNode);
			if ((this.results != null) && (this.results.Length > 0)) {
				return ((ITriable) finalOpNode).TryRun(vars, this.results);
			}
			return finalOpNode.Run(vars);
		}

		[SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
		private bool ResolveAsClassMember(MemberDescriptor desc, out OpNode finalOpNode) {
			//Console.WriteLine("ResolveAsClassMember: "+desc);
			finalOpNode = null;
			if (desc == null) {
				return false;
			}
			MemberInfo info = desc.info;
			MethodInfo methodInfo;
			ConstructorInfo constructorInfo;
			FieldInfo fieldInfo;

			switch (desc.specialType) {
				case SpecialType.Normal:
					if (MemberResolver.IsMethod(info)) {
						methodInfo = MemberWrapper.GetWrapperFor((MethodInfo) info);
						finalOpNode = new OpNode_MethodWrapper(this.parent, this.filename, this.line, this.column,
							this.OrigNode, methodInfo, this.args);
					} else if (MemberResolver.IsConstructor(info)) {
						constructorInfo = MemberWrapper.GetWrapperFor((ConstructorInfo) info);
						finalOpNode = new OpNode_ConstructorWrapper(this.parent, this.filename, this.line, this.column,
							this.OrigNode, constructorInfo, this.args);
					} else if (MemberResolver.IsField(info)) {
						fieldInfo = (FieldInfo) info;
						if ((fieldInfo.Attributes & FieldAttributes.Literal) == FieldAttributes.Literal) {
							object retVal = fieldInfo.GetValue(null);
							finalOpNode = OpNode_Object.Construct(this.parent, retVal);
						} else {
							fieldInfo = MemberWrapper.GetWrapperFor((FieldInfo) info);
							if (this.args.Length == 0) {
								finalOpNode = new OpNode_GetField(this.parent, this.filename, this.line, this.column,
									this.OrigNode, fieldInfo);
							} else {
								finalOpNode = new OpNode_SetField(this.parent, this.filename, this.line, this.column,
									this.OrigNode, fieldInfo, this.args[0]);
							}
						}
					}
					break;
				case SpecialType.String:
					if ((this.args.Length == 1) && (MemberResolver.ReturnsString(this.args[0]))) {
						goto case SpecialType.Normal; //is it not nice? :)
					}
					if (MemberResolver.IsMethod(info)) {
						methodInfo = MemberWrapper.GetWrapperFor((MethodInfo) info);
						finalOpNode = new OpNode_MethodWrapper_String(this.parent, this.filename, this.line, this.column,
							this.OrigNode, methodInfo, this.args, this.formatString);
					} else if (MemberResolver.IsConstructor(info)) {
						constructorInfo = MemberWrapper.GetWrapperFor((ConstructorInfo) info);
						finalOpNode = new OpNode_ConstructorWrapper_String(this.parent, this.filename, this.line, this.column,
							this.OrigNode, constructorInfo, this.args, this.formatString);
					} else if (MemberResolver.IsField(info)) {
						fieldInfo = MemberWrapper.GetWrapperFor((FieldInfo) info);
						finalOpNode = new OpNode_InitField_String(this.parent, this.filename, this.line, this.column,
							this.OrigNode, fieldInfo, this.args, this.formatString);
					}
					break;
				case SpecialType.Params:
					MethodBase methOrCtor = (MethodBase) info;
					ParameterInfo[] pars = methOrCtor.GetParameters();//these are guaranteed to have the right number of arguments...
					int methodParamLength = pars.Length;
					Type paramsElementType = pars[methodParamLength - 1].ParameterType.GetElementType();
					int normalArgsLength = methodParamLength - 1;
					int paramArgsLength = this.args.Length - normalArgsLength;
					OpNode[] normalArgs = new OpNode[normalArgsLength];
					OpNode[] paramArgs = new OpNode[paramArgsLength];
					Array.Copy(this.args, normalArgs, normalArgsLength);
					Array.Copy(this.args, normalArgsLength, paramArgs, 0, paramArgsLength);

					if (MemberResolver.IsMethod(info)) {
						methodInfo = MemberWrapper.GetWrapperFor((MethodInfo) info);
						finalOpNode = new OpNode_MethodWrapper_Params(this.parent, this.filename, this.line, this.column,
							this.OrigNode, methodInfo, normalArgs, paramArgs, paramsElementType);
					} else if (MemberResolver.IsConstructor(info)) {
						constructorInfo = MemberWrapper.GetWrapperFor((ConstructorInfo) info);
						finalOpNode = new OpNode_ConstructorWrapper_Params(this.parent, this.filename, this.line, this.column,
							this.OrigNode, constructorInfo, normalArgs, paramArgs, paramsElementType);
					}

					break;
			}
			return true;
		}

		private void SetNewParentToArgs(IOpNodeHolder newParent) {
			for (int i = 0, n = this.args.Length; i < n; i++) {
				OpNode node = this.args[i];
				//Console.WriteLine("Setting new parent {0} (type {1}) to {2} ({3})", newParent, newParent.GetType(), node, node.GetType());
				node.parent = newParent;
			}
			//str.parent = newParent;
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder(this.name).Append("(");
			for (int i = 0, n = this.args.Length; i < n; i++) {
				str.Append(this.args[i]).Append(", ");
			}
			return str.Append(")").ToString();
		}
	}
}