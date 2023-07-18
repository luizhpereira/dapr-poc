using System.Linq.Expressions;

namespace WebAPI
{
    public class WeighingRegistry
    {
        public DateTime Date { get; set; }

        public int Weigh { get; set; }

        public string Tenant { get; set; } = string.Empty;

        public string Kiosk { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public List<WeighingRegistry> LastWeighing { get; set; } = new List<WeighingRegistry>();

        #region Methods

        public WeighingRegistry ValidateWeighing(WeighingRegistry weighingRegistry)
        {
            try
            {
                if (weighingRegistry.Weigh > 100) weighingRegistry.State = "VB";
                if (weighingRegistry.Weigh < 100) weighingRegistry.State = "AG";                

                return new WeighingRegistry
                {
                    Date = DateTime.Now,
                    Weigh = weighingRegistry.Weigh,
                    Tenant = weighingRegistry.Tenant,
                    State = weighingRegistry.State,
                    Kiosk = weighingRegistry.Kiosk
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #endregion
    }
}
