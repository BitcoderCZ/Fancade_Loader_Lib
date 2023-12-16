using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fancade.LevelEditor
{
#if DEBUG
    internal static class Program
    {
        static void Main(string[] args)
        {
            string name = "SFX_Fp";
            string path = @"C:\Users\Bitcoder\Desktop\fancade\fancade\assets\blocks\";
            //string path = @"C:\dev\fancade level format\level samples\" + name;
            //string path = @"C:\dev\" + name;

            
            /*g.Author = "Unknown Author";
            Dictionary<ushort, Block> customBlocks = g.GetCustomBlocks();
            foreach (KeyValuePair<ushort, Block> item in customBlocks)
            {
                item.Value.Attribs.Uneditable = false;
                foreach (var item2 in item.Value.Blocks)
                    item2.Value.Attribs.Uneditable = false;
            }

            Console.WriteLine(g.SaveVersion);
            Console.WriteLine("Loadded");

            File.WriteAllBytes(@"C:\dev\" + name, new byte[0]);
            using (FileStream fs = new FileStream(@"C:\dev\" + name, FileMode.OpenOrCreate, FileAccess.Write,
                FileShare.Read))
                g.Save(fs);// File.ReadAllBytes(path + " fix"));

            Console.WriteLine("Saved");
            Console.ReadKey(true);*/
        }
    }
#endif
}
