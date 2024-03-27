using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace FancadeLoaderLib
{
    public static class Utils
    {
        public static bool ArrayEquals<T> (this T[] a, T[] b, Func<T, T, bool> compare)
        {
            if (a is null && b is null)
                return true;
            else if (a is null || b is null)
                return false;
            else if (a.Length != b.Length) 
                return false;
            else {
                for (int i = 0; i < a.Length; i++)
                    if (!compare(a[i], b[i]))
                        return false;

                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="seek"></param>
        /// <param name="o"></param>
        /// <returns>0-nothing left, 1-Level, 2-Block, 3-Custom block subblock group, <see cref="int.MaxValue"/>-Unknown</returns>
        public static int NextThing(this SaveReader reader, bool seek, out object o)
        {
            o = null;

            byte[] levelStart = new byte[] { 0x18, 0x03 };
            byte[] levelStart2 = new byte[] { 0x19, 0x03 }; // 0x19 - has non default background color
            if (reader.BytesLeft < 3)
                return 0;

            byte b0 = reader.ReadUInt8();
            byte[] bytes = reader.ReadBytes(2);
            if (levelStart.ArrayEquals(bytes, (a, b) => a == b)
                || levelStart2.ArrayEquals(bytes, (a, b) => a == b)) {
                if (seek)
                    reader.Position -= 3;
                o = new byte[] { b0, bytes[0], bytes[1] };
                return 1;
            }
            reader.Position -= 3;

            if (BlockAttribs.TryLoad(reader, seek, out BlockAttribs attribs, out _)) {
                o = attribs;
                return 2;
            }

            Console.WriteLine($"{reader.BytesLeft} bytes left");
            return int.MaxValue;
        }
    }
}
