namespace OopsBlazorStudyWebApp.Client.Model
{
    public class WeightResponse
    {
        public bool Success { get; set; }

        public double Weight { get; set; }

        public string Unit { get; set; } = string.Empty;
    }
}
