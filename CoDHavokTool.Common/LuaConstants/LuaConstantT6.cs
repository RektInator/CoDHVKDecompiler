using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CoDHavokTool.Common.LuaConstants
{
    public class LuaConstantT6 : LuaConstant
    {
        public LuaConstantT6(BinaryReader reader) : base(reader)
        {
        }

        protected override string ReadString()
        {
            var length = Reader.ReadInt32();
            return Encoding.ASCII.GetString(Reader.ReadBytes(length));
        }

        protected override double ReadNumber()
        {
            return Math.Round(Reader.ReadSingle(), 2, MidpointRounding.ToEven);
        }

        protected override bool ReadBool()
        {
            return Reader.ReadByte() == 1;
        }

        protected override ulong ReadHash()
        {
            return Reader.ReadUInt64();
        }
    }
}
