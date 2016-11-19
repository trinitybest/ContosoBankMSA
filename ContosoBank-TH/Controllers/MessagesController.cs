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

                if (userData.GetProperty<bool>("SetAppointmentWaiting"))
                {
                    userData.SetProperty<bool>("SetAppointmentWaiting", false);
                    userData.SetProperty<bool>("SetAppointment", true);
                    userData.SetProperty<string>("RequestDescription", userMessage);
                    //userData.SetProperty<bool>("YesOrNoWaiting", true);
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
                        userData.SetProperty<bool>("SetAppointment", false);
                        userData.SetProperty<string>("RequestDescription", "");
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        
                    }
                    else
                    {
                        output = "You answered no! Appointment is not saved.";
                        userData.SetProperty<bool>("SetAppointment", false);
                        userData.SetProperty<string>("RequestDescription", "");
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        
                    }
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
                            output = "Appointment is saved.";
                            
                        }
                    }
                    else
                    {
                        output = "Please tell me your full name please:";
                        userData.SetProperty<bool>("SetUserWaiting", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        
                    }
                }

                if (userMessage.ToLower().Contains("name"))
                {
                    if (userData.GetProperty<bool>("SetUserWaiting"))
                    {
                        string name = userMessage;
                        userData.SetProperty<bool>("SetUserWaiting", false);
                        userData.SetProperty<bool>("SetUser", true);
                        userData.SetProperty<string>("FirstName", name.Split(' ')[name.Split(' ').Length-2]);
                        userData.SetProperty<string>("LastName", name.Split(' ')[name.Split(' ').Length-1]);
                        userData.SetProperty<string>("UserName", name.Split(' ')[name.Split(' ').Length - 2] + name.Split(' ')[name.Split(' ').Length - 1]);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

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
                            Value ="http://google.com",
                            Type = "openUrl",
                            Title = "user name"
                        };
                        cardButtons.Add(cardButton);
                        ThumbnailCard Card = new ThumbnailCard()
                        {
                            Title = "user info",
                            Subtitle = userinfo[0].Email,
                            Images = cardImages,
                            Buttons = cardButtons
                        };
                        Attachment oneAttachment = Card.ToAttachment();
                        cardToConversation.Attachments.Add(oneAttachment);
                        await connector.Conversations.SendToConversationAsync(cardToConversation);
                        return Request.CreateResponse(HttpStatusCode.OK);
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
                    //else
                    //{
                    //    userData.SetProperty<bool>("SetUserWaiting", true);
                    //    userData.SetProperty<bool>("SetUser", false);
                    //    output = "Please tell me your full name please?";
                    //}

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
    }
}