using System;
using System.Collections.Generic;
using System.Text;

namespace CoDHavokTool.Common
{
    public interface ILuaOpCodeTable
    {
        LuaOpCode GetValue(int parsedOpCode);
        bool TryGetValue(int parsedOpCode, out LuaOpCode luaOpCode);
    }
}
