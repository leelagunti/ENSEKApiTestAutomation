namespace ENSEKApiTests.Models
{
    public class EnergyResponse
    {
        public EnergyType electric { get; set; }
        public EnergyType gas { get; set; }
        public EnergyType nuclear { get; set; }
        public EnergyType oil { get; set; }
    }

    public class EnergyType
    {
        public int energy_id { get; set; }
        public double price_per_unit { get; set; }
        public int quantity_of_units { get; set; }
        public string unit_type { get; set; }
    }
}
