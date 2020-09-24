using System;
using System.Collections.Generic;
using System.Text;
using CoDHavokTool.Common.Structures;

namespace CoDHavokTool.Common
{
    public interface ILuaConstant
    {
        ConstantType Type { get;}
        string StringValue { get; }
        double NumberValue { get; }
        bool BoolValue { get; }
        ulong HashValue { get; }
    }
}
