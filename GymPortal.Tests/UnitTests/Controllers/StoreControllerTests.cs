using FluentAssertions;
using GymPortal.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GymPortal.Tests.UnitTests.Controllers
{
    public class StoreControllerTests
    {
        private readonly StoreController _controller;

        public StoreControllerTests()
        {
            _controller = new StoreController();
        }

        [Fact]
        public void Index_Get_ReturnsView()
        {
            // Act
            var result = _controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Product_Get_ReturnsViewWithProductId()
        {
            // Arrange
            var productId = 5;

            // Act
            var result = _controller.Product(productId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(productId, _controller.ViewData["ProductId"]);
        }

        [Fact]
        public void Category_Get_ReturnsViewWithCategory()
        {
            // Arrange
            var category = "weights";

            // Act
            var result = _controller.Category(category);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(category, _controller.ViewData["Category"]);
        }
    }
}