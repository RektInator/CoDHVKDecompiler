using System;
using System.Collections.Generic;
using System.Text;
using CoDHavokTool.Common.Structures;

namespace CoDHavokTool.Common
{
    public interface ILuaFunction
    {
        ILuaFile LuaFile { get; }
        FunctionHeader Header { get; }
        long FunctionPos { get; }
        long FunctionLength { get; }
        IList<Instruction> Instructions { get; }
        IList<ILuaConstant> Constants { get; }
        IList<ILuaFunction> ChildFunctions { get; }
        IList<Upvalue> Upvalues { get; }
        IList<Local> Locals { get; }
        Dictionary<int, List<Local>> LocalMap { get; }          // should be private?
        List<Local> LocalsAt(int i);
    }
}
