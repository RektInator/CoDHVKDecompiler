using System;
using System.Collections.Generic;
using System.Text;

namespace CoDHavokTool.Common.Structures
{
    public class FunctionHeader
    {
        public int UpvalCount { get; set; }
        public int ParameterCount { get; set; }
        public bool UsesVarArg { get; set; }
        public int RegisterCount { get; set; }
        public int InstructionCount { get; set; }
        public int ConstantCount { get; set; }
        public int SubFunctionCount { get; set; }
    }
}
