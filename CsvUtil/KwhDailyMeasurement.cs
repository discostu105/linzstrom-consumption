using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyCsvParser.Mapping;

namespace ConsumptionStorageInfluxDb;

public class KwhDailyMeasurement {
    public string _01 { get; set; }
    public string _02 { get; set; }
    public string _03 { get; set; }

    public DateTime Timestamp => DateTime.Parse(_01);
    public decimal KWh => decimal.Parse(_02.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture);
    public decimal? ReplacementKwh => string.IsNullOrEmpty(_03) ? null : decimal.Parse(_03.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture);

    public override string ToString() {
        return $"{Timestamp} {KWh} {ReplacementKwh}";
    }
}

public class KwhDailyMeasurementMapping : CsvMapping<KwhDailyMeasurement> {
    public KwhDailyMeasurementMapping() : base() {
        MapProperty(0, x => x._01);
        MapProperty(1, x => x._02);
        MapProperty(2, x => x._03);
    }
}