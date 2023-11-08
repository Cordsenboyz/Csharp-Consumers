using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerConsumer
{
    internal class Model
    {
        public int houseId { get; set; }

        public Dictionary<string, double>? seasonPowerConsumption = new Dictionary<string, double>
        {
            { "Winter", 0 },
            { "Spring", 0 },
            { "Summer", 0 },
            { "Fall", 0 }
        };

        public Dictionary<int, double>? monthlyPowerConsumption = new Dictionary<int, double>
        {
            { 1, 0},
            { 2, 0},
            { 3, 0},
            { 4, 0},
            { 5, 0},
            { 6, 0},
            { 7, 0},
            { 8, 0},
            { 9, 0},
            { 10, 0},
            { 11, 0},
            { 12, 0},
        };
    }
}
