using BitcoderCZ.Fancade;
using MathUtils.Vectors;

namespace BitcoderCZ.Fancade.Tests;

public class PrefabSegmentTests
{
    [Test]
    public async Task IsEmpty_NullVoxels_ReturnsTrue()
    {
        PrefabSegment segment = new PrefabSegment(1, int3.Zero, null);

        await Assert.That(segment.IsEmpty).IsTrue();
    }

    [Test]
    public async Task IsEmpty_EmptyVoxels_ReturnsTrue()
    {
        PrefabSegment segment = new PrefabSegment(1, int3.Zero, new Voxel[8 * 8 * 8]);

        await Assert.That(segment.IsEmpty).IsTrue();
    }

    [Test]
    public async Task IsEmpty_NonEmptyVoxels_ReturnsFalse()
    {
        PrefabSegment segment = new PrefabSegment(1, int3.Zero, new Voxel[8 * 8 * 8]);

        segment.Voxels![1] = new Voxel(FcColor.Blue, false);

        await Assert.That(segment.IsEmpty).IsFalse();
    }
}
