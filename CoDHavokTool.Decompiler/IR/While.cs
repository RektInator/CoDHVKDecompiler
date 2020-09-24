using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoDHavokTool.LuaDecompiler.IR
{
    public class While : IInstruction
    {
        public Expression Condition;

        public CFG.BasicBlock Body;
        public CFG.BasicBlock Follow;
        
        public bool IsPostTested = false;
        public bool IsBlockInlined = false;

        public override string WriteLua(int indentLevel)
        {
            string ret = "";
            if (IsPostTested)
            {
                ret = $@"repeat" + "\n";
            }
            else
            {
                ret = $@"while {Condition} do" + "\n";
            }
            
            Function.IndentLevel += 1;
            ret += Body.PrintBlock(indentLevel + 1, IsBlockInlined);
            Function.IndentLevel -= 1;
            ret += "\n";
            for (int i = 0; i < indentLevel; i++)
            {
                ret += "\t";
            }
            if (IsPostTested)
            {
                ret += $@"until {Condition}";
            }
            else
            {
                ret += "end";
            }
            if (Follow != null && Follow.Instructions.Count() > 0)
            {
                ret += "\n";
                ret += Follow.PrintBlock(indentLevel);
            }
            return ret;
        }
    }
}
