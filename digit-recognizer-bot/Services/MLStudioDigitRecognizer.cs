//
// The code below was created and published by Mark Szabo and has been
// modified in minor ways. I would like to share all my gratitude for
// the presentation he and his colleagues at Microsoft has held for us
// and made it possible for me to create this application today!
//
// https://github.com/mark-szabo/digit-recognizer-bot
//

using DigitRecognizerBot.Graphics;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DigitRecognizerBot.Services
{
    public class MLStudioDigitRecognizer : IDigitRecognizer
    {
        private readonly string _apiUrl;
        private readonly string _apiKey;

        /// <summary>
        /// Recognize digits using Azure ML Studio.
        /// </summary>
        public MLStudioDigitRecognizer(IOptions<MLStudioDigitRecognizerConfiguration> configuration)
        {
            _apiUrl = configuration.Value.ServiceUrl ?? throw new ArgumentNullException(nameof(configuration.Value.ServiceUrl));
            _apiKey = configuration.Value.ApiKey ?? throw new ArgumentNullException(nameof(configuration.Value.ApiKey));
        }

        public async Task<Prediction> PredictAsync(byte[] image) => await PredictAsync(Image.Load(image));

        public async Task<Prediction> PredictAsync(Image<Rgba32> image)
        {
            var preprocessor = new ImagePreprocessor(Rgba32.White, Rgba32.Black);
            image = preprocessor.Preprocess(image);

            var inputs = PrepareMLStudioInput(image);

            using (var client = new HttpClient())
            {
                var requestContent = new StringContent(inputs);
                requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await client.PostAsync(_apiUrl, requestContent);

                var responseContent = response.Content is HttpContent c ? await c.ReadAsStringAsync() : null;
                var prediction = JsonConvert.DeserializeObject<MLStudioResponseObject>(responseContent);

                var tag = prediction.results.WebServiceOutput0.FirstOrDefault()?["Scored Labels"];

                return new Prediction
                {
                    Tag = Convert.ToInt32(tag),
                    Probability = 1
                };
            }
        }

        private static string PrepareMLStudioInput(Image<Rgba32> image)
        {
            var pixels = new Dictionary<string, int>();
            pixels.Add("Label", 0);

            int i = 0;
            for (int j = 0; j < image.Height; j++)
            {
                for (int k = 0; k < image.Width; k++)
                {
                    pixels[$"f{i}"] = (255 - ((image[k, j].R + image[k, j].G + image[k, j].B) / 3));
                    i++;
                }
            }

            var scoreRequest = new
            {
                Inputs = new Dictionary<string, List<Dictionary<string, int>>>() 
                {
                    {
                        "WebServiceInput0",
                        new List<Dictionary<string, int>>() {
                            pixels
                        }
                    }
                }
            };

            return JsonConvert.SerializeObject(scoreRequest);
        }

        private class MLStudioResponseObject
        {
            public Results results { get; set; }

            public class Results
            {
                public List<Dictionary<string, string>> WebServiceOutput0 { get; set; }
            }
        }
    }
}
