using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.SQSEvents;

using Zyborg.IAMNagBot;
using Microsoft.Extensions.Configuration;
using Amazon.Extensions.NETCore.Setup;
using Amazon.IdentityManagement;
using Amazon.SimpleEmail;

namespace Zyborg.IAMNagBot.Tests
{
    public class FunctionTest
    {
        private IConfiguration _config;
        private AWSOptions _awsOptions;

        public FunctionTest()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");
            
            _config = builder.Build();
            _awsOptions = _config.GetAWSOptions();
        }

        //[Fact]
        public async Task TestGenerateCredentialReport()
        {
            IAmazonIdentityManagementService iamClient =
                _awsOptions.CreateServiceClient<IAmazonIdentityManagementService>();
            IAmazonSimpleEmailService sesClient =
                _awsOptions.CreateServiceClient<IAmazonSimpleEmailService>();

            var logger = new TestLambdaLogger();
            var context = new TestLambdaContext
            {
                Logger = logger
            };

            var function = new Function(iamClient, sesClient);
            await function.FunctionHandler(Function.GenerateReportCommand, context);
        }

        [Fact]
        public async Task TestProcessCredentialReport()
        {
            IAmazonIdentityManagementService iamClient =
                _awsOptions.CreateServiceClient<IAmazonIdentityManagementService>();
            IAmazonSimpleEmailService sesClient =
                _awsOptions.CreateServiceClient<IAmazonSimpleEmailService>();

            var logger = new TestLambdaLogger();
            var context = new TestLambdaContext
            {
                Logger = logger
            };

            var function = new Function(iamClient, sesClient);
            await function.FunctionHandler(null, context);
        }

        // To invoke this main entry point, be sure to specify the <StartupObject> in the .csproj
        public static async Task Main(string[] args)
        {
            var ft = new FunctionTest();
            await ft.TestGenerateCredentialReport();
            await ft.TestProcessCredentialReport();
        }
    }
}
