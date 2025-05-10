namespace LavenderLine.Validate
{
    public class StockValidationResult
    {
        public bool IsValid { get; set; }
        public int Stock { get; set; }
        public List<string> AvailableColors { get; set; }
        public List<string> AvailableSizes { get; set; }
        public string ErrorMessage { get; set; }
    }
}
