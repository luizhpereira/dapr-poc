namespace GrpcServer
{
    public class WeighingRegistry
    {
        public DateTime Date { get; set; }

        public int Weigh { get; set; }

        public string Tenant { get; set; } = string.Empty;

        public string Kiosk { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public List<WeighingRegistry> LastWeighing { get; set; } = new List<WeighingRegistry>();
    }
}
