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
            string name = "6579E530E1D99EB0";
            //string path = @"C:\Users\Bitcoder\Desktop\fancade\fancade\assets\blocks\";
            string path = @"C:\dev\fancade level format\level samples\" + name;
            //string path = @"C:\dev\" + name;

            Game g;
            using (SaveReader reader = new SaveReader(path))
                g = Game.Load(reader, path);

            g.Author = "Unknown Author";
            g.CustomBlocks.EnumerateBlocks(item =>
            {
                item.Value.Attribs.Uneditable = false;
                foreach (KeyValuePair<Vector3I, BlockSection> item2 in item.Value.Blocks)
                    item2.Value.Attribs.Uneditable = false;
            });
            foreach (Level level in g.Levels)
                level.LevelUnEditable = false;

            Console.WriteLine(g.SaveVersion);
            Console.WriteLine("Loadded");

            File.WriteAllBytes(@"C:\dev\" + name, new byte[0]);
            using (FileStream fs = new FileStream(@"C:\dev\" + name, FileMode.OpenOrCreate, FileAccess.Write,
                FileShare.Read))
                g.Save(fs);// File.ReadAllBytes(path + " fix"));

            Console.WriteLine("Saved");
            Console.ReadKey(true);
        }
    }
#endif
}
