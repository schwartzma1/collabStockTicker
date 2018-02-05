using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace StockTicker
{
    public class Function
    {
    
        public async Task<string> getLatestPriceAsync(ILambdaContext context, String name)
        {
            Random r = new Random();
            var log = context.Logger;
            if (name != null && name.Length > 0)
            {
                String symbol = "";
                name = name.ToLower();
                if (name == "apple")
                {
                    symbol = "aapl";
                }
                else if (name == "microsoft")
                {
                    symbol = "msft";
                }
                else if (name == "amazon")
                {
                    symbol = "amzn";
                }
                else
                    return "I don't know that company symbol";

                using (var client = new HttpClient())
                {
                    var result = await client.GetAsync("http://dev.markitondemand.com/MODApis/Api/v2/Quote?symbol=" + symbol);
                    string resultContent = await result.Content.ReadAsStringAsync();
                    log.LogLine(resultContent);
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(resultContent);
                    XmlNode node = xml.SelectSingleNode("/StockQuote/LastPrice");
                    String last = node.InnerText;
                    String[] dollarsAndCents = last.Split(".");
                    String dollars = dollarsAndCents[0];
                    String cents = dollarsAndCents[1];
                    return "The stock price for " + name + " was " + dollars + " dollars and " + cents + " cents.";
                }
            }
            else
            {
                return "You can say get me the quote for Apple or another company name";
            }
        }

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            SkillResponse response = new SkillResponse();
            response.Response = new ResponseBody();
            response.Response.ShouldEndSession = false;
            Reprompt reprompt = new Reprompt();
            reprompt.OutputSpeech = new PlainTextOutputSpeech();
            ((PlainTextOutputSpeech)reprompt.OutputSpeech).Text = "How about another one?";
            response.Response.Reprompt = reprompt;
            IOutputSpeech innerResponse = null;
            var log = context.Logger;

            if (input.GetRequestType() == typeof(LaunchRequest))
            {
                log.LogLine($"Default LaunchRequest made: 'Alexa, open Collab Stock Ticker");
                innerResponse = new PlainTextOutputSpeech();
                (innerResponse as PlainTextOutputSpeech).Text = getLatestPriceAsync(context, null).Result;
            }
            else if (input.GetRequestType() == typeof(IntentRequest))
            {
                var intentRequest = (IntentRequest)input.Request;
                var stockName = "";
                if (intentRequest.Intent.Slots.ContainsKey("name"))
                {
                    if (intentRequest.Intent.Slots["name"] != null)
                    {
                        log.LogLine($"trying to get name");
                        log.LogLine("name: " + intentRequest.Intent.Slots["name"].Name);
                        log.LogLine("value: " + intentRequest.Intent.Slots["name"].Value);
                        stockName = intentRequest.Intent.Slots["name"].Value;
                        log.LogLine(stockName);
                    }
                }
                switch (intentRequest.Intent.Name)
                {
                    case "AMAZON.CancelIntent":
                        log.LogLine($"AMAZON.CancelIntent: send StopMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = "Stopped";
                        response.Response.ShouldEndSession = true;
                        break;
                    case "AMAZON.StopIntent":
                        log.LogLine($"AMAZON.StopIntent: send StopMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = "Stopped";
                        response.Response.ShouldEndSession = true;
                        break;
                    case "AMAZON.HelpIntent":
                        log.LogLine($"AMAZON.HelpIntent: send HelpMessage");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = "Some help text";
                        break;
                    case "GetLatestTickerIntent":
                        log.LogLine($"GetLatestTickerIntent sent");
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = getLatestPriceAsync(context, stockName).Result;
                        break;
                    default:
                        log.LogLine($"Unknown intent: " + intentRequest.Intent.Name);
                        innerResponse = new PlainTextOutputSpeech();
                        (innerResponse as PlainTextOutputSpeech).Text = "Try again";
                        break;
                }
            }
            response.Response.OutputSpeech = innerResponse;
            response.Version = "1.0";
            return response;
        }
    }
}
