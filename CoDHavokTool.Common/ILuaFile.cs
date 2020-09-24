using System;
using System.Collections.Generic;
using System.Text;
using CoDHavokTool.Common.Structures;

namespace CoDHavokTool.Common
{
    public interface ILuaFile
    {
        string FilePath { get; }
        FileHeader Header { get; }
        IList<ILuaConstant> Constants { get; }
        ILuaFunction MainFunction { get; }
    }
}
