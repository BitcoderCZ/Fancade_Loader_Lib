using BitcoderCZ.Maths.Vectors;

namespace BitcoderCZ.Fancade.Tests;

public class PrefabSegmentTests
{
    [Test]
    public async Task IsEmpty_NullVoxels_ReturnsTrue()
    {
        PrefabSegment segment = new PrefabSegment(1, int3.Zero, Voxels.Empty);

        await Assert.That(segment.IsEmpty).IsTrue();
    }

    [Test]
    public async Task IsEmpty_EmptyVoxels_ReturnsTrue()
    {
        PrefabSegment segment = new PrefabSegment(1, int3.Zero, new Voxels());

        await Assert.That(segment.IsEmpty).IsTrue();
    }

    [Test]
    public async Task IsEmpty_NonEmptyVoxels_ReturnsFalse()
    {
        PrefabSegment segment = new PrefabSegment(1, int3.Zero, new Voxels());

        segment.Voxels[int3.One] = new Voxel(FcColor.Blue, false);

        await Assert.That(segment.IsEmpty).IsFalse();
    }
}
