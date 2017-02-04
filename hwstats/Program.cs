using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;
using InfluxData.Net.InfluxDb;
using InfluxData.Net.Common.Enums;
using InfluxData.Net.InfluxDb.Models;

namespace hwstats
{
    class Program
    {
        static void Main(string[] args)
        {
            var influxDbClient = new InfluxDbClient("http://192.168.1.112:8086/", "chris", "password", InfluxDbVersion.Latest);

            Computer comp = new Computer();
            comp.GPUEnabled = true;
            comp.CPUEnabled = true;
            comp.MainboardEnabled = true;
            comp.RAMEnabled = true;
            comp.HDDEnabled = true;
            comp.FanControllerEnabled = true;
            Console.WriteLine("opening...");
            comp.Open();
            Console.WriteLine("opened computer successfully!");

            while (true) {
                List<Point> points = new List<Point>();
                var seriesName = "ohwm";

                foreach (var hardware in comp.Hardware)
                {
                    hardware.Update();
                    // Sensors
                    foreach (var sensor in hardware.Sensors)
                    {
                        var measureName = convertToMeasureName(sensor.Name, sensor.SensorType.ToString());
                        var point = createPoint(seriesName, measureName, sensor.Value);
                        points.Add(point);
                    }

                    // Sub-hardwares
                    foreach (var subh in hardware.SubHardware)
                    {
                        subh.Update();
                        foreach (var sensor in subh.Sensors)
                        {
                            var measureName = convertToMeasureName(sensor.Name, sensor.SensorType.ToString());
                            var point = createPoint(seriesName, measureName, sensor.Value);
                            points.Add(point);
                        }

                    }
                }
                writePointsToInflux(influxDbClient, points);

                System.Threading.Thread.Sleep(1000);
            }
        }

        private static Point createPoint(string seriesName, string name, float? value)
        {
            if (value == null)
            {
                value = 0;
            }

            return new Point()
            {
                Name = seriesName,
                Tags = new Dictionary<string, object>()
                {
                    { "host", "Penguin" },
                },
                Fields = new Dictionary<string, object>()
                {
                    { name, value },
                },
                Timestamp = DateTime.UtcNow
            };
        }

        private static String convertToMeasureName(string name, string type)
        {
            return name.Replace(" ", "_") + "_" + type;
        }

        private static async void writePointsToInflux(InfluxDbClient client, IEnumerable<Point> points)
        {
            var response = await client.Client.WriteAsync("telegraf", points);
            if (response == null)
            {
                Console.WriteLine("Failed to write points to influx");
            }
            if (!response.Success)
            {
                Console.WriteLine("Failed to write points to influx: " + response.Body);
            }
        }
    }
}
