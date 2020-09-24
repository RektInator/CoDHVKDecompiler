using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CoDHavokTool.Common.Structures;

namespace CoDHavokTool.Common.LuaConstants
{
    public abstract class LuaConstant : ILuaConstant
    {
        // properties
        public ConstantType Type { get; private set; }
        public string StringValue { get; private set; }
        public double NumberValue { get; private set; }
        public bool BoolValue { get; private set; }
        public ulong HashValue { get; private set; }

        // private variables
        protected readonly BinaryReader Reader;

        protected LuaConstant(BinaryReader reader)
        {
            Reader = reader;
            Parse();
        }

        private void Parse()
        {
            Type = (ConstantType) Reader.ReadByte();

            switch (Type)
            {
                case ConstantType.TString:
                    StringValue = ReadString();
                    break;
                case ConstantType.TNumber:
                    NumberValue = ReadNumber();
                    break;
                case ConstantType.TBoolean:
                    BoolValue = ReadBool();
                    break;
                case ConstantType.THash:
                    HashValue = ReadHash();
                    break;
                case ConstantType.TNil:
                    break;
                case ConstantType.TLightUserData:
                    break; // throw new NotImplementedException();
                case ConstantType.TTable:
                    break; // throw new NotImplementedException();
                case ConstantType.TFunction:
                    break; // throw new NotImplementedException();
                case ConstantType.TUserData:
                    break; // throw new NotImplementedException();
                case ConstantType.TThread:
                    break; // throw new NotImplementedException();
                case ConstantType.TIFunction:
                    break; // throw new NotImplementedException();
                case ConstantType.TCFunction:
                    break; // throw new NotImplementedException();
                case ConstantType.TUI64:
                    break; // throw new NotImplementedException();
                case ConstantType.TStruct:
                    break; // throw new NotImplementedException();
                case ConstantType.TUnk:
                    break; // throw new NotImplementedException();
                default:
                    break; // throw new ArgumentOutOfRangeException();
            }
        }

        protected abstract string ReadString();
        protected abstract double ReadNumber();
        protected abstract bool ReadBool();
        protected abstract ulong ReadHash();

        public override string ToString()
        {
            return Type switch
            {
                ConstantType.TString => StringValue,
                ConstantType.TNumber => NumberValue.ToString(CultureInfo.InvariantCulture),
                ConstantType.TNil => "nil",
                ConstantType.TBoolean => BoolValue ? "true" : "false",
                ConstantType.THash => $"0x{HashValue & 0xFFFFFFFFFFFFFFF:X}",
                _ => "NULL"
            };
        }
    }
}
