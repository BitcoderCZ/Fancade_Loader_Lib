using BitcoderCZ.Maths.Vectors;
using Array3D = BitcoderCZ.Fancade.Array3D<ushort>;

namespace BitcoderCZ.Fancade.Tests;

#pragma warning disable IDE0022
public class Array3DTests
{
    [Test]
    [MethodDataSource(nameof(GetInvalidSizes))]
    public async Task Constructor_Size_InvalidSize_Throws(int3 size)
    {
        await Assert.That(() => new Array3D(size)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments(0, 0, 0)]
    [Arguments(1, 1, 1)]
    [Arguments(1, 2, 3)]
    public async Task Constructor_Size_ValidSize_DoesNotThrow(int sizeX, int sizeY, int sizeZ)
    {
        var size = new int3(sizeX, sizeY, sizeZ);

        var array = new Array3D(size);

        await Assert.That(array.Size).IsEqualTo(size);
    }

    [Test]
    [Arguments(0, 0, 0, 1)]
    [Arguments(1, 1, 1, 0)]
    [Arguments(-1, 1, -1, 1)]
    public async Task Constructor_Collection_SizeNotMatch_Throws(int sizeX, int sizeY, int sizeZ, int length)
    {
        var size = new int3(sizeX, sizeY, sizeZ);

        await Assert.That(() => new Array3D(Enumerable.Range(0, length).Select(item => (ushort)item), size)).Throws<ArgumentException>();
    }

    [Test]
    [MethodDataSource("GetInvalidSizes")]
    public async Task Constructor_Collection_InvalidSize_Throws(int3 size)
    {
        await Assert.That(() => new Array3D(Enumerable.Empty<ushort>(), size)).Throws<ArgumentException>();
    }

    [Test]
    [Arguments(0, 0, 0, 0)]
    [Arguments(1, 1, 1, 1)]
    [Arguments(1, 2, 3, 6)]
    public async Task Constructor_Collection_SizeMatch_DoesNotThrow(int sizeX, int sizeY, int sizeZ, int length)
    {
        var size = new int3(sizeX, sizeY, sizeZ);

        var array = new Array3D(Enumerable.Range(0, length).Select(item => (ushort)item), size);

        await Assert.That(array.Size).IsEqualTo(size);
    }

    [Test]
    [Arguments(0, 0, 0, 1)]
    [Arguments(1, 1, 1, 0)]
    [Arguments(-1, 1, -1, 1)]
    public async Task Constructor_Array_SizeNotMatch_Throws(int sizeX, int sizeY, int sizeZ, int length)
    {
        var size = new int3(sizeX, sizeY, sizeZ);

        await Assert.That(() => new Array3D(new ushort[length], size)).Throws<ArgumentException>();
    }

    [Test]
    [MethodDataSource("GetInvalidSizes")]
    public async Task Constructor_Array_InvalidSize_Throws(int3 size)
    {
        await Assert.That(() => new Array3D([], size)).Throws<ArgumentException>();
    }

    [Test]
    [Arguments(0, 0, 0, 0)]
    [Arguments(1, 1, 1, 1)]
    [Arguments(1, 2, 3, 6)]
    public async Task Constructor_Array_SizeMatch_DoesNotThrow(int sizeX, int sizeY, int sizeZ, int length)
    {
        var size = new int3(sizeX, sizeY, sizeZ);

        var array = new Array3D(new ushort[length], size);

        await Assert.That(array.Size).IsEqualTo(size);
    }

    [Test]
    [Arguments(0, 0, 0)]
    [Arguments(9, 9, 9)]
    public async Task InBounds_PosInBounds_ReturnsTrue(int x, int y, int z)
    {
        int3 pos = new int3(x, y, z);

        var array = new Array3D(int3.One * 10);

        await Assert.That(array.InBounds(pos)).IsTrue();
    }

    [Test]
    [Arguments(-1, 0, 0)]
    [Arguments(0, -1, 0)]
    [Arguments(0, 0, -1)]
    [Arguments(10, 0, 0)]
    [Arguments(0, 10, 0)]
    [Arguments(0, 0, 10)]
    public async Task InBounds_PosNotInBounds_ReturnsFalse(int x, int y, int z)
    {
        int3 pos = new int3(x, y, z);

        var array = new Array3D(int3.One * 10);

        await Assert.That(array.InBounds(pos)).IsFalse();
    }

    [Test]
    [Arguments(0, 0, 0, 0)]
    [Arguments(1, 0, 0, 1)]
    [Arguments(0, 1, 0, 4)]
    [Arguments(0, 0, 1, 4 * 4)]
    [Arguments(3, 3, 3, 4 * 4 * 4 - 1)]
    public async Task Index_int3_ReturnsCorrectIndex(int x, int y, int z, int expected)
    {
        int3 pos = new int3(x, y, z);

        var array = new Array3D(int3.One * 4);

        await Assert.That(array.Index(pos)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(0, 0, 0, 0)]
    [Arguments(1, 1, 0, 0)]
    [Arguments(4, 0, 1, 0)]
    [Arguments(4 * 4, 0, 0, 1)]
    [Arguments(4 * 4 * 4 - 1, 3, 3, 3)]
    public async Task Index_int_ReturnsCorrectIndex(int index, int expectedX, int expectedY, int expectedZ)
    {
        int3 expected = new int3(expectedX, expectedY, expectedZ);

        var array = new Array3D(int3.One * 4);

        await Assert.That(array.Index(index)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(-1, 0, 0)]
    [Arguments(0, -1, 0)]
    [Arguments(0, 0, -1)]
    [Arguments(10, 0, 0)]
    [Arguments(0, 10, 0)]
    [Arguments(0, 0, 10)]
    public async Task Get_PosOutOfBounds_Throws(int x, int y, int z)
    {
        int3 pos = new int3(x, y, z);

        var array = new Array3D(int3.One * 10);

        await Assert.That(() => array.Get(pos)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [MatrixDataSource]
    public async Task GetSet_Returns_Id(
        [MatrixRange<ushort>(0, 3)] int x,
        [MatrixRange<ushort>(0, 3)] int y,
        [MatrixRange<ushort>(0, 3)] int z)
    {
        int3 pos = new int3(x, y, z);

        var array = new Array3D(int3.One * 4);

        array.Set(pos, 5);

        await Assert.That(array.Get(pos)).IsEqualTo((ushort)5);
    }

    [Test]
    [MethodDataSource("GetInvalidSizes")]
    public async Task Resize_InvalidSize_Throws(int3 size)
    {
        var array = new Array3D(int3.One * 10);

        await Assert.That(() => array.Resize(size)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments(0, 0, 0)]
    [Arguments(1, 1, 1)]
    [Arguments(20, 20, 20)]
    public async Task Resize_Resizes(int x, int y, int z)
    {
        int3 size = new int3(x, y, z);

        var array = new Array3D(int3.One * 10);

        array.Resize(size);

        await Assert.That(array.Size).IsEqualTo(size);
    }

    [Test]
    public async Task Resize_KeepsData()
    {
        int3 size = new int3(2, 3, 4);

        var array = new Array3D(size);

        ushort id = 1;
        for (int z = 0; z < size.Z; z++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int x = 0; x < size.X; x++)
                {
                    array.Set(new int3(x, y, z), id++);
                }
            }
        }

        array.Resize(int3.One * 8);

        id = 1;
        for (int z = 0; z < size.Z; z++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int x = 0; x < size.X; x++)
                {
                    await Assert.That(array.Get(new int3(x, y, z))).IsEqualTo(id++);
                }
            }
        }
    }

    [Test]
    public async Task Clear_Clears()
    {
        int3 size = new int3(4, 4, 4);

        var array = new Array3D(size);

        ushort id = 1;
        for (int z = 0; z < size.Z; z++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int x = 0; x < size.X; x++)
                {
                    array.Set(new int3(x, y, z), id++);
                }
            }
        }

        array.Clear();

        for (int z = 0; z < size.Z; z++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                for (int x = 0; x < size.X; x++)
                {
                    await Assert.That(array.Get(new int3(x, y, z))).IsEqualTo((ushort)0);
                }
            }
        }
    }

    #region Data Sources
    public static IEnumerable<int3> GetInvalidSizes()
    {
        for (int x = -1; x <= 2; x++)
        {
            for (int y = -1; y <= 2; y++)
            {
                for (int z = -1; z <= 2; z++)
                {
                    int3 size = new int3(x, y, z);

                    if (size.LengthSquared > 0 && (size.X == 0 || size.Y == 0 || size.Z == 0))
                    {
                        yield return size;
                    }
                }
            }
        }
    }
    #endregion
}