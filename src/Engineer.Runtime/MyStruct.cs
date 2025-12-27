#pragma warning disable CA1707 // Identifiers should not contain underscores
using System;
using System.Runtime.InteropServices;

namespace Engineer
{
    public unsafe struct MyStruct
    {
        public const int BUFFER_SIZE = 1000;
        public const int Count = BUFFER_SIZE;

        public MyStruct(uint* value)
        {
            var res = new int[Count];
            Marshal.Copy(new IntPtr(value), res, 0, Count);
            MyUints = res;
        }

        public int[] MyUints
        {
            get; private set;
        }
    }
}
