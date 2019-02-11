using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.Lambda.Core;
using Amazon.SimpleEmail;
using Microsoft.Extensions.Configuration;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Zyborg.IAMNagBot
{
    public class Function
    {
        public const string EnvVarsConfigPrefix = "NAGBOT_";
        public const string GenerateReportCommand = "generate-report";

        public const string RootAccountName = "<root_account>";
        public const string EmailUserTag = "email";
        public const string SlackUserTag = "slack";


        private IConfiguration _config;
        private FunctionSettings _settings;

        private IAmazonIdentityManagementService _iamClient;
        private IAmazonSimpleEmailService _sesClient;

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance.
        /// When invoked in a Lambda environment the AWS credentials will come from the IAM role
        /// associated with the function and the AWS region will be set to the region the Lambda
        /// function is executed in.
        /// </summary>
        public Function() : this(
            new AmazonIdentityManagementServiceClient(),
            new AmazonSimpleEmailServiceClient())
        { }

        /// <summary>
        /// Constructs an instance with preconfigured client(s).
        /// This variation of the constructor can be used for testing and invocation
        /// outside of the Lambda execution environment.
        /// </summary>
        /// <param name="iamClient"></param>
        public Function(
            IAmazonIdentityManagementService iamClient,
            IAmazonSimpleEmailService sesClient)
        {
            _iamClient = iamClient;
            _sesClient = sesClient;

            _config = new ConfigurationBuilder()
                .AddEnvironmentVariables(EnvVarsConfigPrefix)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            System.Console.WriteLine("GOT CONFIG: "
                + Newtonsoft.Json.JsonConvert.SerializeObject(
                    _config, Newtonsoft.Json.Formatting.Indented));

            _settings = _config.Get<FunctionSettings>();
            System.Console.WriteLine("GOT SETTINGS: "
                + Newtonsoft.Json.JsonConvert.SerializeObject(
                    _settings, Newtonsoft.Json.Formatting.Indented));
        }

        /// <summary>
        /// This method is called for every Lambda invocation.
        /// This method does not expect any event-specific input object
        /// and only relies on the Lambda invocation context.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(string command, ILambdaContext context)
        {
            context.Logger.LogLine($"Got Command: [{command??"(null)"}]");

            var action = new FunctionAction(_settings, _iamClient, _sesClient, context);
            switch (command)
            {
                case GenerateReportCommand:
                    await action.GenerateReport();
                    break;
                default:
                    await action.ProcessReport();
                    break;
            }
        }
    }
}
