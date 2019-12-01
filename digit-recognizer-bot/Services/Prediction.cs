using Newtonsoft.Json;

namespace DigitRecognizerBot.Services
{
    public class Prediction
    {
        public int Tag { get; set; }

        public double Probability { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}
