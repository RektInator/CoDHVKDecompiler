using CoDHavokTool.Common;

namespace CoDHavokTool.LuaDecompiler
{
    public interface IDecompiler
    {
        string Decompile(ILuaFile luaFile);
    }
}
