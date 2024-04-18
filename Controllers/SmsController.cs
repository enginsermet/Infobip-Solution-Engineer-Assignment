using Infobip.Api.Client;
using Infobip.Api.Client.Api;
using Infobip.Api.Client.Model;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace InfobipAssignment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SmsController : ControllerBase
    {
        public IConfiguration Configuration { get; set; }

        private readonly IHttpContextAccessor Context;

        public DateTime Date { get; set; } = DateTime.Now;

        public SmsController(IConfiguration configuration, IHttpContextAccessor context)
        {
            Configuration = configuration;
            Context = context;
        }


        //This method is used for sending the sms
        [HttpPost("send")]
        public IActionResult Send()
        {
            var configuration = new Configuration()
            {
                BasePath = Configuration["BASE_URL"],
                ApiKeyPrefix = "App",
                ApiKey = Configuration["API_KEY"]
            };

            var sendSmsApi = new SendSmsApi(configuration);

            var baseUrl = Context.HttpContext.Request.Host;

            var methodUrl = "api/Sms/delivery-reports";

            var notifyUrl = string.Format("https://{0}/{1}", baseUrl, methodUrl);

            var smsMessage = new SmsTextualMessage()
            {
                From = "InfoSMS",
                SendAt = Date.AddDays(1),
                IntermediateReport = true,
                NotifyContentType = "application/json",
                NotifyUrl = notifyUrl,
                Destinations = new List<SmsDestination>()
        {
            new SmsDestination(to: "41793026727")
        },
                Text = "This is a dummy SMS message sent using Infobip.Api.Client for the Infobip Solution Engineer Assignment"
            };

            var smsRequest = new SmsAdvancedTextualRequest()
            {
                Messages = new List<SmsTextualMessage>() { smsMessage }
            };


            try
            {
                var smsResponse = sendSmsApi.SendSmsMessage(smsRequest);

                System.Diagnostics.Debug.WriteLine($"Status: {smsResponse.Messages.First().Status}");

                string bulkId = smsResponse.BulkId;
                string messageId = smsResponse.Messages.First().MessageId;

                int numberOfReportsLimit = 10;
                var smsDeliveryResult = sendSmsApi.GetOutboundSmsMessageDeliveryReports(bulkId, messageId, numberOfReportsLimit);
                foreach (var smsReport in smsDeliveryResult.Results)
                {
                    Console.WriteLine($"{smsReport.MessageId} - {smsReport.Status.Name}");
                }

                return Ok(smsResponse);
            }
            catch (ApiException apiException)
            {
                var errorCode = apiException.ErrorCode;
                var errorHeaders = apiException.Headers;
                var errorContent = apiException.ErrorContent;

                return BadRequest(errorContent);
            }

        }

        /* Future Development */
        //This method is used for real time delivery reports of the sent sms, such as message delivered status or incoming message status
        //Because this app is hosted on localhost it doesn't work at the moment
        //PS: Microsoft Dev Tunnel can be used to implement webhooks
        [HttpPost("delivery-reports")]
        public IActionResult ReceiveDeliveryReport([FromBody] SmsDeliveryResult deliveryResult)
        {
            foreach (var result in deliveryResult.Results)
            {
                System.Diagnostics.Debug.WriteLine($"{result.MessageId} - {result.Status.Name}");
            }
            return Ok();
        }


    }
}
