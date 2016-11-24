using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using ContosoBank_TH.Managers;
using System.Collections.Generic;
using ContosoBank_TH.Models;
using ContosoBank_TH.API;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

namespace ContosoBank_TH
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
        
                var userMessage = activity.Text;

                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                
                // Set greeting 
                string output = "Hello! What can I do for you?";
                if ( (!userData.GetProperty<bool>("SetAppointmentWaiting")) && (!userData.GetProperty<bool>("SetUserWaiting")) ) 
                {
                    if (userData.GetProperty<bool>("Greeting"))
                    {
                        output = "Hello again! What can I do for you?";
                    }
                    else
                    {
                        userData.SetProperty<bool>("Greeting", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }
                }


                if (userData.GetProperty<bool>("ImageWaiting"))
                    {

                        VisionServiceClient VisionServiceClient = new VisionServiceClient("72eb5e0c1cdb41b380e77b67c1797b81");

                        AnalysisResult analysisResult = await VisionServiceClient.DescribeAsync(activity.Attachments[0].ContentUrl, 3);

                        string visionReply = $"{analysisResult.Description.Captions[0].Text}";
                        if (visionReply.ToLower().Contains("coin"))
                        {
                            userData.SetProperty<bool>("ImageWaiting", false);
                            userData.SetProperty<bool>("ImageYesOrNoWaiting", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            Activity reply1 = activity.CreateReply($"Your image shows: {visionReply}. Is your request money related?");
                            await connector.Conversations.ReplyToActivityAsync(reply1);

                            return Request.CreateResponse(HttpStatusCode.OK);
                        }
                        else
                        {
                            userData.SetProperty<bool>("ImageWaiting", false);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            Activity reply1 = activity.CreateReply($"Your image shows: {visionReply}. Sorry it's not related to our bank service.");
                            await connector.Conversations.ReplyToActivityAsync(reply1);

                            return Request.CreateResponse(HttpStatusCode.OK);
                        }


                    }

                    if (userData.GetProperty<bool>("ImageYesOrNoWaiting"))
                    {
                        if (userMessage.ToLower() == "yes")
                        {
                            output = "You answered yes! Tell me more about your appointment please:";
                            userData.SetProperty<bool>("ImageYesOrNoWaiting", false);
                            userData.SetProperty<bool>("SetAppointmentWaiting", true);
                            userData.SetProperty<string>("RequestType", "Money");
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        }
                        else
                        {
                            output = "You answered no! Tell me more about your appointment please:";
                            userData.SetProperty<bool>("ImageYesOrNoWaiting", false);
                            userData.SetProperty<bool>("SetAppointmentWaiting", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        }
                        Activity reply1 = activity.CreateReply(output);
                        await connector.Conversations.ReplyToActivityAsync(reply1);
                        return Request.CreateResponse(HttpStatusCode.OK);

                    }
                    

                    //HttpClient LUISClient = new HttpClient();
                    //string LUISReply = await LUISClient.GetStringAsync();

                if (userData.GetProperty<bool>("SetAppointmentWaiting"))
                {
                    userData.SetProperty<bool>("SetAppointmentWaiting", false);
                    userData.SetProperty<bool>("SetAppointment", true);
                    userData.SetProperty<string>("RequestDescription", userMessage);
                    if(userData.GetProperty<string>("RequestType") == "")
                    {
                        userData.SetProperty<string>("RequestType", "General");
                    }
                    
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    output = "We have received your appointment, do you want to save it?";
                    Activity reply1 = activity.CreateReply(output);
                    await connector.Conversations.ReplyToActivityAsync(reply1);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userData.GetProperty<bool>("SetAppointment"))
                {
                    if(userMessage.ToLower() == "yes")
                    {
                        output = "You answered yes! Appointment is saved.";
                        // save appointment to database
                        ServiceRequest request = new ServiceRequest();
                        request.UserId = userData.GetProperty<string>("UserId");
                        request.RequestDescription = userData.GetProperty<string>("RequestDescription");
                        request.ServiceType = userData.GetProperty<string>("RequestType");
                        await AzureManager.AzureManagerInstace.SetRequest(request);

                        userData.SetProperty<bool>("SetAppointment", false);
                        userData.SetProperty<string>("RequestDescription", "");
                        userData.SetProperty<string>("RequestType", "");
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        
                    }
                    else
                    {
                        output = "You answered no! Appointment is not saved.";
                        userData.SetProperty<bool>("SetAppointment", false);
                        userData.SetProperty<string>("RequestDescription", "");
                        userData.SetProperty<string>("RequestType", "");
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        
                    }
                    Activity reply1 = activity.CreateReply(output);
                    await connector.Conversations.ReplyToActivityAsync(reply1);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userData.GetProperty<bool>("CurrencyWaiting"))
                {
                    
                    string baseCurrency = userMessage.Split(' ')[0];
                    string compareCurrency = userMessage.Split(' ')[1];
                    HttpClient client = new HttpClient();
                    string x = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base="+baseCurrency+"&symbols="+compareCurrency));
                    Currency.RootObject rootObject;
                    rootObject = JsonConvert.DeserializeObject<Currency.RootObject>(x);
                    double rate = -1;
                    switch (compareCurrency)
                    {
                        case "AUD":
                            rate = rootObject.rates.AUD;
                            break;
                        case "GBP":
                            rate = rootObject.rates.GBP;
                            break;
                        case "EUR":
                            rate = rootObject.rates.EUR;
                            break;
                        case "CAD":
                            rate = rootObject.rates.CAD;
                            break;
                        case "USD":
                            rate = rootObject.rates.USD;
                            break;
                        default:
                            rate = -1;
                            break;

                    }
                    userData.SetProperty<bool>("CurrencyWaiting", false);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    output = $"1 {rootObject.@base} = {rate} {compareCurrency} ";
                    Activity reply1 = activity.CreateReply(output);
                    await connector.Conversations.ReplyToActivityAsync(reply1);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if (userMessage.ToLower().Contains("image"))
                {
                    userData.SetProperty<bool>("ImageWaiting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    output = "Please upload an image:";
                    Activity reply1 = activity.CreateReply(output);
                    await connector.Conversations.ReplyToActivityAsync(reply1);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                    if (userMessage.ToLower().Contains("appointment"))
                {
                    //output = "appointment?";
                    //userData.GetProperty<bool>("SetAppointment")
                    if (userData.GetProperty<bool>("SetUser"))
                    {
                        if (!userData.GetProperty<bool>("SetAppointment"))
                        {
                            output = "Please tell me about your appointment please:";
                            userData.SetProperty<bool>("SetAppointmentWaiting", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            
                        }
                        else 
                        {
                            // save appointment
                            ServiceRequest request = new ServiceRequest();
                            request.UserId = userData.GetProperty<string>("UserId");
                            request.RequestDescription = userData.GetProperty<string>("RequestDescription");
                            await AzureManager.AzureManagerInstace.SetRequest(request);
                            output = "Appointment is saved.";
                            Activity reply1 = activity.CreateReply(output);
                            await connector.Conversations.ReplyToActivityAsync(reply1);
                            return Request.CreateResponse(HttpStatusCode.OK);

                        }
                    }
                    else
                    {
                        output = "Please tell me your full name please:";
                        userData.SetProperty<bool>("SetUserWaiting", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity reply1 = activity.CreateReply(output);
                        await connector.Conversations.ReplyToActivityAsync(reply1);
                        return Request.CreateResponse(HttpStatusCode.OK);

                    }
                }

                if (userMessage.ToLower().Contains("name"))
                {
                    if (userData.GetProperty<bool>("SetUserWaiting"))
                    {
                        string name = userMessage;
                        
                        userData.SetProperty<string>("FirstName", name.Split(' ')[name.Split(' ').Length-2]);
                        userData.SetProperty<string>("LastName", name.Split(' ')[name.Split(' ').Length-1]);
                        userData.SetProperty<string>("UserName", name.Split(' ')[name.Split(' ').Length - 2] + name.Split(' ')[name.Split(' ').Length - 1]);
                        //await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                        List<User> userinfo = await AzureManager.AzureManagerInstace.GetUsers(userData.GetProperty<string>("UserName"));
                        if (userinfo.Count > 0)
                        {
                            userData.SetProperty<string>("UserId", userinfo[0].ID);
                            userData.SetProperty<bool>("SetUserWaiting", false);
                            userData.SetProperty<bool>("SetUser", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            output = $"Hey, {userData.GetProperty<string>("FirstName")} {userData.GetProperty<string>("LastName")}!";
                            Activity cardToConversation = activity.CreateReply(output);
                            cardToConversation.Recipient = activity.From;
                            cardToConversation.Type = "message";
                            cardToConversation.Attachments = new List<Attachment>();
                            List<CardImage> cardImages = new List<CardImage>();
                            cardImages.Add(new CardImage(url: "http://www.drawinghowtodraw.com/drawing-lessons/drawing-animals-creatures-lessons/images/howtodrawducksdrawinglessons_html_5f6ac075.png"));
                            List<CardAction> cardButtons = new List<CardAction>();
                            CardAction cardButton = new CardAction()
                            {
                                Value = "http://google.com",
                                Type = "openUrl",
                                Title = "user name"
                            };
                            cardButtons.Add(cardButton);
                            ThumbnailCard Card = new ThumbnailCard()
                            {
                                Title = $"{userData.GetProperty<string>("FirstName")}'s Info",
                                //Subtitle = userinfo[0].ToString(),
                                Images = cardImages,
                                //Buttons = cardButtons,
                                Text = userinfo[0].Gender + " " + userinfo[0].Email + " " + userinfo[0].IpAddress
                            };
                            Attachment oneAttachment = Card.ToAttachment();
                            cardToConversation.Attachments.Add(oneAttachment);
                            await connector.Conversations.SendToConversationAsync(cardToConversation);
                            return Request.CreateResponse(HttpStatusCode.OK);
                        }
                        else
                        {
                            output = "No matching name from our database, please try again.";
                            Activity reply1 = activity.CreateReply(output);
                            await connector.Conversations.ReplyToActivityAsync(reply1);
                            return Request.CreateResponse(HttpStatusCode.OK);

                        }
                    }
                    if (userData.GetProperty<bool>("SetUser"))
                    {
                        List<User> userinfo = await AzureManager.AzureManagerInstace.GetUsers(userData.GetProperty<string>("UserName"));
                        output = $"Hey, {userData.GetProperty<string>("FirstName")} {userData.GetProperty<string>("LastName")}!";
                        Activity cardToConversation = activity.CreateReply(output);
                        cardToConversation.Recipient = activity.From;
                        cardToConversation.Type = "message";
                        cardToConversation.Attachments = new List<Attachment>();
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: "http://www.drawinghowtodraw.com/drawing-lessons/drawing-animals-creatures-lessons/images/howtodrawducksdrawinglessons_html_5f6ac075.png"));
                        List<CardAction> cardButtons = new List<CardAction>();
                        CardAction cardButton = new CardAction()
                        {
                            Value = "http://google.com",
                            Type = "openUrl",
                            Title = "user name"
                        };
                        cardButtons.Add(cardButton);
                        ThumbnailCard Card = new ThumbnailCard()
                        {
                            Title = $"{userData.GetProperty<string>("FirstName")}'s Info",
                            //Subtitle = userinfo[0].ToString(),
                            Images = cardImages,
                            //Buttons = cardButtons,
                            Text = userinfo[0].Gender + " " + userinfo[0].Email + " " + userinfo[0].IpAddress
                        };
                        Attachment oneAttachment = Card.ToAttachment();
                        cardToConversation.Attachments.Add(oneAttachment);
                        await connector.Conversations.SendToConversationAsync(cardToConversation);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else
                    {
                        output = "Please tell me your full name please:";
                        userData.SetProperty<bool>("SetUserWaiting", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity reply1 = activity.CreateReply(output);
                        await connector.Conversations.ReplyToActivityAsync(reply1);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    //else
                    //{
                    //    userData.SetProperty<bool>("SetUserWaiting", true);
                    //    userData.SetProperty<bool>("SetUser", false);
                    //    output = "Please tell me your full name please?";
                    //}

                }

                if (userMessage.ToLower().Contains("clear"))
                {
                    output = "User Info is cleared!";
                    userData.SetProperty<bool>("SetUserWaiting", false);
                    userData.SetProperty<bool>("SetUser", false);
                    userData.SetProperty<string>("FirstName", "");
                    userData.SetProperty<string>("LastName", "");
                    userData.SetProperty<string>("UserName", "");
                    userData.SetProperty<string>("UserId", "");
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    Activity reply1 = activity.CreateReply(output);
                    await connector.Conversations.ReplyToActivityAsync(reply1);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                    if (userMessage.ToLower().Contains("currency"))
                {
                    userData.SetProperty<bool>("CurrencyWaiting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    output= "Could you tell us the two currencies please?";
                    Activity reply1 = activity.CreateReply(output);
                    await connector.Conversations.ReplyToActivityAsync(reply1);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                
                Stock.RootObject rootObjectLUIS = await GetEntityFromLUIS(userMessage);
                if(rootObjectLUIS.topScoringIntent.intent != "None")
                { 
                switch (rootObjectLUIS.topScoringIntent.intent)
                {
                    case "StockPrice":
                        output = await GetStock(rootObjectLUIS.entities[0].entity);
                        break;
                    case "StockPrice2":
                        output = await GetStock(rootObjectLUIS.entities[0].entity);
                        break;
                    default:
                        break;
                }
                    Activity reply1 = activity.CreateReply(output);
                    await connector.Conversations.ReplyToActivityAsync(reply1);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                Activity reply = activity.CreateReply(output);

                //List<User> users = await AzureManager.AzureManagerInstace.GetUsers();
                //Activity reply = activity.CreateReply($"{users[0].LastName}");

                // return our reply to the user
                //Activity reply = activity.CreateReply($"You sent {activity.Text} which was {length} characters");
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        private async Task<string> GetStock(string StockSymbol)
        {
            double? dblStockValue = await YahooAPI.GetStockRateAsync(StockSymbol);
            if (dblStockValue == null)
            {
                return string.Format("This \"{0}\" is not an valid stock symbol", StockSymbol);
            }
            else
            {
                return string.Format("Stock Price of {0} is {1}", StockSymbol, dblStockValue);
            }
        }

        private static async Task<Stock.RootObject> GetEntityFromLUIS(string Query)
        {
            Query = Uri.EscapeDataString(Query);
            Stock.RootObject Data = new Stock.RootObject();
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://api.projectoxford.ai/luis/v2.0/apps/3d39d203-9f0b-4d16-84cd-f38ffefff37f?subscription-key=1842e783fce74b328964c942cb26b698&q=" + Query;
;               HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                    Data = JsonConvert.DeserializeObject<Stock.RootObject>(JsonDataResponse);
                }
            }
            return Data;
        }
    }
}