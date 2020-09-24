using System;
using System.Collections.Generic;
using System.Text;

namespace CoDHavokTool.Common.Extensions
{
    public static class ByteExtensions
    {
        public static bool GetBit(this byte b, int bitNumber)
        {
            return (b & (1 << bitNumber-1)) != 0;
        }
    }
}
