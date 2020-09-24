using System;
using System.Collections.Generic;
using System.Text;

namespace CoDHavokTool.Common.Structures
{
    public enum ConstantType : byte
    {
        TNil,
        TBoolean,
        TLightUserData,
        TNumber,
        TString,
        TTable,
        TFunction,
        TUserData,
        TThread,
        TIFunction,
        TCFunction,
        TUI64,
        TStruct,
        THash,
        TUnk,
    }
}
