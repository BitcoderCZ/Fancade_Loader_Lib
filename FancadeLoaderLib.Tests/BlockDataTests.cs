using MathUtils.Vectors;

namespace FancadeLoaderLib.Tests;

[TestFixture]
public class BlockDataTests
{
	[Test]
	public void Move_PositiveOffset_ShiftsDataCorrectly()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));
		blockData.SetBlock(new int3(1, 1, 1), 42);

		blockData.Move(new int3(1, 1, 1));

		Assert.That(blockData.Size, Is.EqualTo(new int3(5, 5, 5)));
		using (Assert.EnterMultipleScope())
		{
			Assert.That(blockData.GetBlock(new int3(2, 2, 2)), Is.EqualTo(42));
			Assert.That(blockData.GetBlock(new int3(1, 1, 1)), Is.EqualTo(0));
		}
	}

	[Test]
	public void Move_NegativeOffset_ThrowsException()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));

		using (Assert.EnterMultipleScope())
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => blockData.Move(new int3(-1, 0, 0)));
			Assert.Throws<ArgumentOutOfRangeException>(() => blockData.Move(new int3(0, -1, 0)));
			Assert.Throws<ArgumentOutOfRangeException>(() => blockData.Move(new int3(0, 0, -1)));
		}
	}

	[Test]
	public void Move_ZeroOffset_DoesNothing()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));
		blockData.SetBlock(new int3(1, 1, 1), 42);

		blockData.Move(new int3(0, 0, 0));

		Assert.That(blockData.Size, Is.EqualTo(new int3(4, 4, 4)));
		Assert.That(blockData.GetBlock(new int3(1, 1, 1)), Is.EqualTo(42));
	}

	[Test]
	public void Move_WithStartPosition_ShiftsPartialDataCorrectly()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(3, 3, 3)));

		blockData.SetBlock(new int3(0, 0, 0), 1);
		blockData.SetBlock(new int3(1, 1, 1), 2);
		blockData.SetBlock(new int3(2, 2, 2), 3);

		blockData.Move(new int3(1, 0, 0), new int3(1, 1, 1));

		Assert.That(blockData.Size, Is.EqualTo(new int3(4, 3, 3)));
		using (Assert.EnterMultipleScope())
		{
			Assert.That(blockData.GetBlock(new int3(0, 0, 0)), Is.EqualTo(1));
			Assert.That(blockData.GetBlock(new int3(1, 1, 1)), Is.EqualTo(0));
			Assert.That(blockData.GetBlock(new int3(2, 1, 1)), Is.EqualTo(2));
			Assert.That(blockData.GetBlock(new int3(3, 2, 2)), Is.EqualTo(3));
		}
	}

	[Test]
	public void Move_WithOutOfBoundsStartPosition_ThrowsException()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(2, 2, 2)));

		using (Assert.EnterMultipleScope())
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => blockData.Move(new int3(1, 1, 1), new int3(3, 3, 3)));
			Assert.Throws<ArgumentOutOfRangeException>(() => blockData.Move(new int3(1, 1, 1), new int3(-1, 0, 0)));
			Assert.Throws<ArgumentOutOfRangeException>(() => blockData.Move(new int3(1, 1, 1), new int3(0, -1, 0)));
			Assert.Throws<ArgumentOutOfRangeException>(() => blockData.Move(new int3(1, 1, 1), new int3(0, 0, -1)));
		}
	}

	[Test]
	public void Move_WithOutOfBoundsMove_ThrowsException()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(2, 2, 2)));

		using (Assert.EnterMultipleScope())
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => blockData.Move(new int3(-2, 0, 0), new int3(1, 1, 1)));
			Assert.Throws<ArgumentOutOfRangeException>(() => blockData.Move(new int3(0, -2, 0), new int3(1, 1, 1)));
			Assert.Throws<ArgumentOutOfRangeException>(() => blockData.Move(new int3(0, 0, -2), new int3(1, 1, 1)));
		}
	}

	[Test]
	public void TrimNegative_ShiftsBlocksToOrigin()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));
		blockData.SetBlock(new int3(3, 2, 1), 99);

		blockData.TrimNegative();

		Assert.That(blockData.Size, Is.EqualTo(new int3(1, 4, 3)));
		Assert.That(blockData.GetBlock(new int3(0, 2, 0)), Is.EqualTo(99));
	}

	[Test]
	public void TrimNegative_TrimY_ShiftsBlocksToOrigin()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));
		blockData.SetBlock(new int3(3, 2, 1), 99);

		blockData.TrimNegative(trimY: true);

		Assert.That(blockData.Size, Is.EqualTo(new int3(1, 2, 3)));
		Assert.That(blockData.GetBlock(new int3(0, 0, 0)), Is.EqualTo(99));
	}

	[Test]
	public void TrimNegative_EmptyArray_RemainsUnchanged()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));

		blockData.TrimNegative();

		Assert.That(blockData.Size, Is.EqualTo(int3.Zero));
	}

	[Test]
	public void Trim_RemovesEmptySpace()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(5, 5, 5)));
		blockData.SetBlock(new int3(1, 2, 3), 99);

		blockData.Trim();

		Assert.That(blockData.Size, Is.EqualTo(new int3(2, 3, 4)));
		Assert.That(blockData.GetBlock(new int3(1, 2, 3)), Is.EqualTo(99));
	}

	[Test]
	public void Trim_EmptyArray_RemainsUnchanged()
	{
		var blockData = new BlockData(new Array3D<ushort>(new int3(4, 4, 4)));

		blockData.Trim();

		Assert.That(blockData.Size, Is.EqualTo(int3.Zero));
	}
}

