// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.6.2

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DigitRecognizerBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace DigitRecognizerBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private readonly IDigitRecognizer _digitRecognizer;

        public EchoBot(IDigitRecognizer digitRecognizer)
        {
            _digitRecognizer = digitRecognizer ?? throw new ArgumentNullException(nameof(digitRecognizer));
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            if (activity.Type == ActivityTypes.Message)
            {
                if (activity.Attachments != null && activity.Attachments.Any())
                {
                    // We know the user is sending an attachment as there is at least one item
                    // in the Attachments list.
                    await HandleIncomingAttachmentAsync(turnContext, activity);
                    return;
                }

                if (activity.Text?.Trim()?.ToLowerInvariant() == "hi")
                {
                    await turnContext.SendActivitiesAsync(
                        new IActivity[]
                        {
                            new Activity(type: ActivityTypes.Message, text: "Hi! 🙋‍"),
                            new Activity(type: ActivityTypes.Message, text: "Send me a picture of a handwritten digit and I'll tell you what the number is!"),
                            new Activity(type: ActivityTypes.Message, text: "Yeah, I'm that smart! 😎"),
                        },
                        cancellationToken);
                }
            }
        }

        /// <summary>
        /// Handle attachments uploaded by users. The bot receives an <see cref="Attachment"/> in an <see cref="Activity"/>.
        /// The activity has a <see cref="IList{T}"/> of attachments.
        /// </summary>
        /// <remarks>
        /// Not all channels allow users to upload files. Some channels have restrictions
        /// on file type, size, and other attributes. Consult the documentation for the channel for
        /// more information. For example Skype's limits are here
        /// <see ref="https://support.skype.com/en/faq/FA34644/skype-file-sharing-file-types-size-and-time-limits"/>.
        /// </remarks>
        private async Task HandleIncomingAttachmentAsync(ITurnContext<IMessageActivity> context, IMessageActivity activity)
        {
            foreach (var file in activity.Attachments)
            {
                if (file.ContentType != "image/png" && file.ContentType != "image/jpeg")
                {
                    await context.SendActivityAsync("Sorry, I cannot process images other than png/jpeg.");
                }

                // Download the actual attachment
                using (var client = new HttpClient())
                {
                    var stream = await client.GetStreamAsync(file.ContentUrl);
                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);

                    var byteArray = memoryStream.ToArray();

                    var prediction = await _digitRecognizer.PredictAsync(byteArray);

                    await SendPredictionAnswer(context, prediction.Tag, prediction.Probability);
                }
            }
        }

        private static async Task SendPredictionAnswer(ITurnContext<IMessageActivity> context, int digit, double probability)
        {
            var digitWithArticle = string.Empty;
            switch (digit)
            {
                case 0:
                    digitWithArticle = "a zero";
                    break;
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 9:
                    digitWithArticle = $"a {digit}";
                    break;
                case 8:
                    digitWithArticle = $"an {digit}";
                    break;
                default:
                    break;
            }

            if (probability < 0.5)
            {
                await context.SendActivityAsync($"I'm not 100% sure, but I think this is {digitWithArticle}.");
                return;
            }

            var random = new Random();
            switch (random.Next(0, 4))
            {
                case 0:
                    await context.SendActivityAsync($"I know! I know! It's {digitWithArticle}!");
                    break;
                case 1:
                    await context.SendActivityAsync($"OK, this should be {digitWithArticle}!");
                    break;
                case 2:
                    await context.SendActivityAsync($"This is {digitWithArticle}!");
                    break;
                case 3:
                    await context.SendActivityAsync($"Easy-peasy! This is {digitWithArticle}.");
                    break;
            }
        }
    }
}
