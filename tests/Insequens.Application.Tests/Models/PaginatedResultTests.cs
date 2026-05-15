using FluentAssertions;
using Insequens.Application.Models;

namespace Insequens.Application.Tests.Models;

public class PaginatedResultTests
{
    [Fact]
    public void Constructor_OnFirstPage_ComputesPaginationMetadata()
    {
        var result = new PaginatedResult<int>([1, 2], 5, 1, 2);

        result.Items.Should().Equal(1, 2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public void Constructor_OnLastPage_ComputesPaginationMetadata()
    {
        var result = new PaginatedResult<int>([5], 5, 3, 2);

        result.TotalPages.Should().Be(3);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEmptyItems_ComputesEmptyPaginationMetadata()
    {
        var result = new PaginatedResult<int>([], 0, 1, 20);

        result.Items.Should().BeEmpty();
        result.TotalPages.Should().Be(0);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();
    }
}
