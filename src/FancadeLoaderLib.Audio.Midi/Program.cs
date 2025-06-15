using FancadeLoaderLib.Data;
using FancadeLoaderLib.Editing;
using FancadeLoaderLib.Editing.Scripting.Settings;
using MathUtils.Vectors;
using Melanchall.DryWetMidi.Core;
using Serilog;
using Serilog.Events;
using System.Diagnostics.Tracing;
using System.Reflection.Emit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FancadeLoaderLib.Audio.Midi;

internal class Program
{
    static void Main(string[] args)
    {
        var loggger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Information()
            .CreateLogger();

        Log.Logger = loggger;

        var settings = new MidiConvertSettings()
        {
            DoubleNoteOn = MidiConvertSettings.DoubleNoteOnBehaviour.IgnoreNew,
        };

        Game game;
        using (var fs = File.OpenRead("audio_in.fcg"))
        {
            game = Game.LoadCompressed(fs);
        }

        FcAudio audio;
        bool build = false;
        if (build)
        {
            FcAudio.Builder builder = new(Log.Logger);

            int prevChannel = -1;
            for (int i = 0; i <= 20; i++)
            {
                builder.TryPlaySound(TimeSpan.FromSeconds(i / 2.0), (byte)(i % 2 == 0 ? 48 : 60), 1d, FcSound.Piano, out int newPrevChannel);
                if (prevChannel != -1)
                {
                    builder.StopSound(TimeSpan.FromSeconds(i / 2.0), prevChannel);
                }

                prevChannel = newPrevChannel;
            }

            audio = builder.Build();
        }
        else
        {
            string path = "a.mid";
            MidiFile file;
            using (FileStream stream = File.OpenRead(path))
            {
                file = MidiFile.Read(stream, new ReadingSettings());
            }

            audio = MidiConverter.Convert(file, Log.Logger, settings);
        }

        Console.WriteLine("Wait");
        Console.ReadKey(true);
        Write(audio, game, Log.Logger);

        using (var fs = File.OpenWrite("out.fcg"))
        {
            game.SaveCompressed(fs, 9);
        }

        Console.WriteLine("Done");
        Console.ReadKey(true);
    }

    private static void Write(FcAudio audio, Game game, ILogger logger)
    {
        var prefabs = game.Prefabs;
        var level = prefabs.First();

        Span<ushort> ids = stackalloc ushort[64]; // value (index) to prefab id

        FcDataWriter.AddDataBlocks(game.Prefabs, ids, true);

        FcDataWriter.WriteData(game.Prefabs, level, audio.ToData(out int eventCount, logger), int3.Zero, int3.Zero, ids, "A", out int dataBlockCount);

        // write info
        int3 infoPos = new int3(0, 0, 1);
        int3 dataSize = FcDataWriter.DataSizeWithBase;

        // data max
        level.Blocks.SetPrefab(infoPos, StockBlocks.Values.Vector.Prefab);
        level.Settings.Add((ushort3)infoPos, new PrefabSetting(0, SettingType.Vec3, (ushort3)infoPos, (float3)dataSize / 2f));
        infoPos += new int3(0, 0, 2);

        // data min
        level.Blocks.SetPrefab(infoPos, StockBlocks.Values.Vector.Prefab);
        level.Settings.Add((ushort3)infoPos, new PrefabSetting(0, SettingType.Vec3, (ushort3)infoPos, -(float3)dataSize / 2f));
        infoPos += new int3(0, 0, 2);

        // data pos
        level.Blocks.SetPrefab(infoPos, StockBlocks.Values.Vector.Prefab);
        level.Settings.Add((ushort3)infoPos, new PrefabSetting(0, SettingType.Vec3, (ushort3)infoPos, (float3)dataSize / 2f));
        infoPos += new int3(0, 0, 2);

        // sound data block count
        level.Blocks.SetPrefab(infoPos, StockBlocks.Values.Number.Prefab);
        level.Settings.Add((ushort3)infoPos, new PrefabSetting(0, SettingType.Float, (ushort3)infoPos, (float)dataBlockCount));
        infoPos += new int3(0, 0, 1);

        // data per block
        level.Blocks.SetPrefab(infoPos, StockBlocks.Values.Number.Prefab);
        level.Settings.Add((ushort3)infoPos, new PrefabSetting(0, SettingType.Float, (ushort3)infoPos, (float)FcDataWriter.MaxBlocksPerPrefab));
        infoPos += new int3(0, 0, 1);

        // event count
        level.Blocks.SetPrefab(infoPos, StockBlocks.Values.Number.Prefab);
        level.Settings.Add((ushort3)infoPos, new PrefabSetting(0, SettingType.Float, (ushort3)infoPos, (float)eventCount));
        infoPos += new int3(0, 0, 1);
    }
}
