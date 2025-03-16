using MathUtils.Vectors;
using TUnit.Assertions.AssertConditions.Throws;

namespace FancadeLoaderLib.Tests;

public class BlockDataTests
{
	[Test]
	public async Task Move_PositiveOffset_ShiftsDataCorrectly()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));
		blockData.SetBlock(new int3(1, 1, 1), 42);

		blockData.Move(new int3(1, 1, 1));

		await Assert.That(blockData.Size).IsEqualTo(new int3(5, 5, 5));
		using (Assert.Multiple())
		{
			await Assert.That(blockData.GetBlock(new int3(2, 2, 2))).IsEqualTo((ushort)42);
			await Assert.That(blockData.GetBlock(new int3(1, 1, 1))).IsEqualTo((ushort)0);
		}
	}

	[Test]
	public async Task Move_NegativeOffset_ThrowsException()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));

		using (Assert.Multiple())
		{
			await Assert.That(() => blockData.Move(new int3(-1, 0, 0))).Throws<ArgumentOutOfRangeException>();
			await Assert.That(() => blockData.Move(new int3(0, -1, 0))).Throws<ArgumentOutOfRangeException>();
			await Assert.That(() => blockData.Move(new int3(0, 0, -1))).Throws<ArgumentOutOfRangeException>();
		}
	}

	[Test]
	public async Task Move_ZeroOffset_DoesNothing()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));
		blockData.SetBlock(new int3(1, 1, 1), 42);

		blockData.Move(new int3(0, 0, 0));

		await Assert.That(blockData.Size).IsEqualTo(new int3(4, 4, 4));
		await Assert.That(blockData.GetBlock(new int3(1, 1, 1))).IsEqualTo((ushort)42);
	}

	[Test]
	public async Task Move_WithStartPosition_ShiftsPartialDataCorrectly()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(3, 3, 3)));

		blockData.SetBlock(new int3(0, 0, 0), 1);
		blockData.SetBlock(new int3(1, 1, 1), 2);
		blockData.SetBlock(new int3(2, 2, 2), 3);

		blockData.Move(new int3(1, 0, 0), new int3(1, 1, 1));

		await Assert.That(blockData.Size).IsEqualTo(new int3(4, 3, 3));
		using (Assert.Multiple())
		{
			await Assert.That(blockData.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)1);
			await Assert.That(blockData.GetBlock(new int3(1, 1, 1))).IsEqualTo((ushort)0);
			await Assert.That(blockData.GetBlock(new int3(2, 1, 1))).IsEqualTo((ushort)2);
			await Assert.That(blockData.GetBlock(new int3(3, 2, 2))).IsEqualTo((ushort)3);
		}
	}

	[Test]
	public async Task Move_WithOutOfBoundsStartPosition_ThrowsException()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(2, 2, 2)));

		using (Assert.Multiple())
		{
			await Assert.That(() => blockData.Move(new int3(1, 1, 1), new int3(3, 3, 3))).Throws<ArgumentOutOfRangeException>();
			await Assert.That(() => blockData.Move(new int3(1, 1, 1), new int3(-1, 0, 0))).Throws<ArgumentOutOfRangeException>();
			await Assert.That(() => blockData.Move(new int3(1, 1, 1), new int3(0, -1, 0))).Throws<ArgumentOutOfRangeException>();
			await Assert.That(() => blockData.Move(new int3(1, 1, 1), new int3(0, 0, -1))).Throws<ArgumentOutOfRangeException>();
		}
	}

	[Test]
	public async Task Move_WithOutOfBoundsMove_ThrowsException()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(2, 2, 2)));

		using (Assert.Multiple())
		{
			await Assert.That(() => blockData.Move(new int3(-2, 0, 0), new int3(1, 1, 1))).Throws<ArgumentOutOfRangeException>();
			await Assert.That(() => blockData.Move(new int3(0, -2, 0), new int3(1, 1, 1))).Throws<ArgumentOutOfRangeException>();
			await Assert.That(() => blockData.Move(new int3(0, 0, -2), new int3(1, 1, 1))).Throws<ArgumentOutOfRangeException>();
		}
	}

	[Test]
	public async Task TrimNegative_ShiftsBlocksToOrigin()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));
		blockData.SetBlock(new int3(3, 2, 1), 99);

		blockData.TrimNegative();

		await Assert.That(blockData.Size).IsEqualTo(new int3(1, 4, 3));
		await Assert.That(blockData.GetBlock(new int3(0, 2, 0))).IsEqualTo((ushort)99);
	}

	[Test]
	public async Task TrimNegative_TrimY_ShiftsBlocksToOrigin()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));
		blockData.SetBlock(new int3(3, 2, 1), 99);

		blockData.TrimNegative(trimY: true);

		await Assert.That(blockData.Size).IsEqualTo(new int3(1, 2, 3));
		await Assert.That(blockData.GetBlock(new int3(0, 0, 0))).IsEqualTo((ushort)99);
	}

	[Test]
	public async Task TrimNegative_EmptyArray_RemainsUnchanged()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));

		blockData.TrimNegative();

		await Assert.That(blockData.Size).IsEqualTo(int3.Zero);
	}

	[Test]
	public async Task Trim_RemovesEmptySpace()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(5, 5, 5)));
		blockData.SetBlock(new int3(1, 2, 3), 99);

		blockData.Trim();

		await Assert.That(blockData.Size).IsEqualTo(new int3(2, 3, 4));
		await Assert.That(blockData.GetBlock(new int3(1, 2, 3))).IsEqualTo((ushort)99);
	}

	[Test]
	public async Task Trim_EmptyArray_RemainsUnchanged()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));

		blockData.Trim();

		await Assert.That(blockData.Size).IsEqualTo(int3.Zero);
	}
}
