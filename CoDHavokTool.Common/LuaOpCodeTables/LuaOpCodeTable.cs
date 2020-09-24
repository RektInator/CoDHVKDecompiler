using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace CoDHavokTool.Common.LuaOpCodeTables
{
    public abstract class LuaOpCodeTable : ILuaOpCodeTable
    {
        protected Dictionary<int, LuaOpCode> OpCodeTable;

        protected LuaOpCodeTable()
        {
            Parse();
        }

        private void Parse()
        {
            OpCodeTable = ParseLuaOpCodes();
        }

        protected abstract Dictionary<int, LuaOpCode> ParseLuaOpCodes();

        public LuaOpCode GetValue(int parsedOpCode)
        {
            return OpCodeTable[parsedOpCode];
        }

        public bool TryGetValue(int parsedOpCode, out LuaOpCode luaOpCode)
        {
            try
            {
                luaOpCode = GetValue(parsedOpCode);
                return true;
            }
            catch (Exception)
            {
                // ignored
            }

            luaOpCode = LuaOpCode.HKS_OPCODE_UNK;
            return false;
        }
    }
}
