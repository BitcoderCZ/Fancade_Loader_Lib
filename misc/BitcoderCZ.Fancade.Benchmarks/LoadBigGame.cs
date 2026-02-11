using BenchmarkDotNet.Attributes;
using BitcoderCZ.Fancade.Raw;

namespace BitcoderCZ.Fancade.Benchmarks;

[MemoryDiagnoser]
public class LoadBigGame
{
    private byte[] _contents = null!;
    private byte[] _contentsDecompressed = null!;

    [GlobalSetup]
    public void Setup()
    {
        _contents = File.ReadAllBytes("../../../../../../../bigestFile");
        using (var src = new MemoryStream(_contents))
        using (var dst = new MemoryStream())
        {
            Zlib.Decompress(src, dst);
            _contentsDecompressed = dst.ToArray();
        }
    }

    [Benchmark]
    public RawGame LoadRaw()
    {
        using (var stream = new MemoryStream(_contents))
        {
            return RawGame.LoadCompressed(stream);
        }
    }

    [Benchmark]
    public RawGame LoadRawDecompressed()
    {
        using (var reader = new FcBinaryReader(_contentsDecompressed))
        {
            return RawGame.Load(reader);
        }
    }

    [Benchmark]
    public Game Load()
    {
        using (var stream = new MemoryStream(_contents))
        {
            return Game.LoadCompressed(stream);
        }
    }

    [Benchmark]
    public Game LoadDecompressed()
    {
        using (var reader = new FcBinaryReader(_contentsDecompressed))
        {
            return Game.Load(reader);
        }
    }
}