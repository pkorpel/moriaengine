//using System;
//using System.Text;
//using System.Collections;
////using PerCederberg.Grammatica.Parser;
//using SteamEngine.Common;


////these classes are currently unused... 
////I have to yet think out some syntax in LScript to create arrays -tar
//namespace SteamEngine.LScript {
//    internal class OpNode_ConstructArray_Length : OpNode, IOpNodeHolder, ITriable {
//        private Type type;
//        private OpNode lengthNode;

//        internal OpNode_ConstructArray_Length(IOpNodeHolder parent, string filename,
//                    int line, int column, Node origNode, Type type, OpNode lengthNode)
//            : base(parent, filename, line, column, origNode) {
//            this.type = type;
//            this.lengthNode = lengthNode;
//        }

//        public virtual void Replace(OpNode oldNode, OpNode newNode) {
//            if (lengthNode == oldNode) {
//                lengthNode = newNode;
//            } else {
//                throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
//            }
//        }

//        internal override object Run(ScriptVars vars) {
//            object oSelf = vars.self;
//            vars.self = vars.defaultObject;
//            object result;
//            try {
//                result = lengthNode.Run(vars);
//            } finally {
//                vars.self = oSelf;
//            }
//            try {
//                return Array.CreateInstance(type, Convert.ToInt32(result));
//            } catch (Exception e) {
//                throw new InterpreterException("Exception while creating array",
//                    this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
//            }
//        }

//        public object TryRun(ScriptVars vars, object[] results) {
//            try {
//                return Array.CreateInstance(type, Convert.ToInt32(results[0]));
//            } catch (Exception e) {
//                throw new InterpreterException("Exception while creating array",
//                    this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
//            }
//        }

//        public override string ToString() {
//            return string.Format("Array.CreateInstance({0}, {1})", type, lengthNode);
//        }
//    }

//    internal class OpNode_ConstructArray_LengthConstant : OpNode, ITriable {
//        private Type type;
//        private int length;

//        internal OpNode_ConstructArray_LengthConstant(IOpNodeHolder parent, string filename,
//                    int line, int column, Node origNode, Type type, int length)
//            : base(parent, filename, line, column, origNode) {
//            this.type = type;
//            this.length = length;
//        }

//        internal override object Run(ScriptVars vars) {
//            try {
//                return Array.CreateInstance(type, length);
//            } catch (Exception e) {
//                throw new InterpreterException("Exception while creating array",
//                    this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
//            }
//        }

//        public object TryRun(ScriptVars vars, object[] results) {
//            try {
//                return Array.CreateInstance(type, length);
//            } catch (Exception e) {
//                throw new InterpreterException("Exception while creating array",
//                    this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
//            }
//        }

//        public override string ToString() {
//            return string.Format("ARRAY.CREATEINSTANCE({0}, {1})", type, length);
//        }
//    }

//    internal class OpNode_ConstructArray_Elements : OpNode, IOpNodeHolder, ITriable {
//        private Type type;
//        private OpNode[] elementNodes;

//        internal OpNode_ConstructArray_Elements(IOpNodeHolder parent, string filename,
//                    int line, int column, Node origNode, Type type, OpNode[] elementNodes)
//            : base(parent, filename, line, column, origNode) {
//            this.type = type;
//            this.elementNodes = elementNodes;
//        }

//        public virtual void Replace(OpNode oldNode, OpNode newNode) {
//            int index = Array.IndexOf(elementNodes, oldNode);
//            if (index < 0) {
//                throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
//            } else {
//                elementNodes[index] = newNode;
//            }
//        }

//        internal override object Run(ScriptVars vars) {
//            try {
//                int n = elementNodes.Length;
//                Array arr = Array.CreateInstance(type, n);
//                for (int i = 0; i < n; i++) {
//                    arr.SetValue(elementNodes[i].Run(vars), i);
//                }
//                return arr;
//            } catch (InterpreterException) {
//                throw;
//            } catch (FatalException) {
//                throw;
//            } catch (Exception e) {
//                throw new InterpreterException("Exception while creating array",
//                    this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
//            }
//        }

//        public object TryRun(ScriptVars vars, object[] results) {
//            try {
//                int n = elementNodes.Length;
//                Array arr = Array.CreateInstance(type, n);
//                for (int i = 0; i < n; i++) {
//                    arr.SetValue(results[i], i);
//                }
//                return arr;
//            } catch (Exception e) {
//                throw new InterpreterException("Exception while creating array",
//                    this.line, this.column, this.filename, ParentScriptHolder.GetDecoratedName(), e);
//            }
//        }

//        public override string ToString() {
//            return string.Format("ARRAY.CREATEINSTANCE({0}, {1})", type, Tools.ObjToString(elementNodes));
//        }
//    }
//}