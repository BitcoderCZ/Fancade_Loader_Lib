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

            List<(ushort id, Block block)> blocks = new List<(ushort id, Block block)>();

            foreach (string file in Directory.EnumerateFiles(path))
            {
                Game g;
                try
                {
                    using (SaveReader reader = new SaveReader(file))
                        g = Game.Load(reader, file);
                } catch (Exception ex)
                {
                    Console.WriteLine($"{Path.GetFileName(file)} was skipped");
                    continue;
                }

                blocks.Add((g.Levels[0].BlockIds[0], g.CustomSegments.First().Value.Block));
            }

            blocks.Sort((a, b) => a.id.CompareTo(b.id));

            foreach ((ushort id, Block block) item in blocks)
            {
                Console.WriteLine($"{item.id, 3}:{item.block.Blocks.Count, 2} " +
                    $"\"{item.block.Name}\"");
            }
            Console.WriteLine("Done");
            Console.ReadKey(true);

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
