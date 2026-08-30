using AtlasLetrero.Domain;
using Xunit;

namespace AtlasLetrero.Domain.Tests;

public sealed class MatrixMapperExtendedTests
{
    [Fact]
    public void RectangularTileRotates90DegreesIntoPhysicalDimensions()
    {
        var tile = new MatrixTile(0, 0, 3, 2, Rotation: MatrixRotation.Clockwise90);
        Assert.Equal(2, tile.PhysicalWidth);
        Assert.Equal(3, tile.PhysicalHeight);
        var mapper = new MatrixMapper(new MatrixTopology(3, 2, [tile]));
        Assert.Equal(1, mapper.Map(0, 0).PhysicalIndex);
        Assert.Equal(5, mapper.Map(2, 0).PhysicalIndex);
        Assert.Equal(0, mapper.Map(0, 1).PhysicalIndex);
        Assert.Equal(4, mapper.Map(2, 1).PhysicalIndex);
    }

    [Fact]
    public void RectangularTileRotates270Degrees()
    {
        var mapper = new MatrixMapper(new MatrixTopology(3, 2,
            [new(0, 0, 3, 2, Rotation: MatrixRotation.Clockwise270)]));
        Assert.Equal(4, mapper.Map(0, 0).PhysicalIndex);
        Assert.Equal(0, mapper.Map(2, 0).PhysicalIndex);
        Assert.Equal(5, mapper.Map(0, 1).PhysicalIndex);
    }

    [Fact]
    public void ColumnSerpentineReversesEveryOtherColumn()
    {
        var mapper = new MatrixMapper(new MatrixTopology(3, 4,
            [new(0, 0, 3, 4, Traversal: MatrixTraversal.Column, Layout: MatrixLayout.Serpentine)]));
        Assert.Equal(0, mapper.Map(0, 0).PhysicalIndex);
        Assert.Equal(3, mapper.Map(0, 3).PhysicalIndex);
        Assert.Equal(4, mapper.Map(1, 3).PhysicalIndex);
        Assert.Equal(7, mapper.Map(1, 0).PhysicalIndex);
        Assert.Equal(8, mapper.Map(2, 0).PhysicalIndex);
    }

    [Theory]
    [InlineData(ChannelOrder.Rbg, new byte[] { 1, 3, 2 })]
    [InlineData(ChannelOrder.Gbr, new byte[] { 2, 3, 1 })]
    [InlineData(ChannelOrder.Bgr, new byte[] { 3, 2, 1 })]
    public void EncodesAllRgbPermutations(ChannelOrder order, byte[] expected)
    {
        var frame = new FrameBuffer(1, 1); frame[0, 0] = new(1, 2, 3);
        var mapper = new MatrixMapper(new MatrixTopology(1, 1, [new(0, 0, 1, 1)], order));
        Assert.Equal(expected, mapper.Encode(frame));
    }

    [Theory]
    [InlineData(ChannelOrder.Brgw, new byte[] { 3, 1, 2, 4 })]
    [InlineData(ChannelOrder.Wrgb, new byte[] { 4, 1, 2, 3 })]
    public void EncodesAdditionalRgbwOrders(ChannelOrder order, byte[] expected)
    {
        var frame = new FrameBuffer(1, 1, ColorModel.Rgbw); frame[0, 0] = new(1, 2, 3, 4);
        var mapper = new MatrixMapper(new MatrixTopology(1, 1, [new(0, 0, 1, 1)], order));
        Assert.Equal(expected, mapper.Encode(frame));
    }

    [Fact]
    public void ChainedTilesExposeLocalAndAbsoluteAddress()
    {
        var mapper = new MatrixMapper(new MatrixTopology(4, 2,
            [new(2, 0, 2, 2, ChainIndex: 1), new(0, 0, 2, 2, ChainIndex: 0)]));
        var address = mapper.Map(2, 1);
        Assert.Equal(1, address.TileIndex);
        Assert.Equal(2, address.PhysicalIndex);
        Assert.Equal(6, address.AbsoluteIndex);
    }

    [Fact]
    public void RejectsGapsOverlapsAndFrameSizeMismatch()
    {
        Assert.Throws<ArgumentException>(() => new MatrixTopology(3, 2, [new(0, 0, 2, 2)]));
        Assert.Throws<ArgumentException>(() => new MatrixTopology(2, 2,
            [new(0, 0, 2, 2), new(0, 0, 2, 2, ChainIndex: 1)]));
        var mapper = new MatrixMapper(new MatrixTopology(2, 2, [new(0, 0, 2, 2)]));
        Assert.Throws<ArgumentException>(() => mapper.Encode(new FrameBuffer(1, 1)));
    }
}
