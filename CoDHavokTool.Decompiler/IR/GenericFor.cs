using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoDHavokTool.LuaDecompiler.IR
{
    /// <summary>
    /// A numeric for statement
    /// for var in exp do
    ///     something
    /// end
    /// </summary>
    public class GenericFor : IInstruction
    {
        public Assignment Iterator;

        public CFG.BasicBlock Body;
        public CFG.BasicBlock Follow;

        public override string WriteLua(int indentLevel)
        {
            string ret = "";
            if (Iterator is Assignment a)
            {
                a.IsLocalDeclaration = false;
                a.IsGenericForAssignment = true;
            }
            ret = $@"for {Iterator} do" + "\n";

            Function.IndentLevel += 1;
            ret += Body.PrintBlock(indentLevel + 1);
            Function.IndentLevel -= 1;
            ret += "\n";
            for (int i = 0; i < indentLevel; i++)
            {
                ret += "\t";
            }
            ret += "end";
            if (Follow != null && Follow.Instructions.Count() > 0)
            {
                ret += "\n";
                ret += Follow.PrintBlock(indentLevel);
            }
            return ret;
        }
    }
}
