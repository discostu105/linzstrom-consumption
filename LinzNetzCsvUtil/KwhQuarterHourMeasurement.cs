using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyCsvParser.Mapping;

namespace ConsumptionStorageInfluxDb;

public class KwhQuarterHourMeasurement {
    public string _01 { get; set; }
    public string _02 { get; set; }
    public string _03 { get; set; }
    public string _04 { get; set; }

    public DateTime Timestamp_From => DateTime.Parse(_01);
    public DateTime Timestamp_To => DateTime.Parse(_02);
    public decimal KWh => decimal.Parse(_03.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture);
    public decimal? ReplacementKwh => string.IsNullOrEmpty(_04) ? null : decimal.Parse(_04.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture);

    public override string ToString() {
        return $"{Timestamp_From} {Timestamp_To} {KWh} {ReplacementKwh}";
    }
}

public class KwhQuarterHourMeasurementMapping : CsvMapping<KwhQuarterHourMeasurement> {
    public KwhQuarterHourMeasurementMapping() : base() {
        MapProperty(0, x => x._01);
        MapProperty(1, x => x._02);
        MapProperty(2, x => x._03);
        MapProperty(3, x => x._04);
    }
}