using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using NLog.Config;
using NLog;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using NLog.Targets;
using System;


namespace EventManagement.Tests.ProgramTests
{
    public class ProgramTests
    {       

        [Fact]
        public async Task Application_StartsSuccessfully()
        {
            var _factory = new WebApplicationFactory<Program>();
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/swagger");  // Testing with the Swagger endpoint

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
