using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSL_Manager
{
    public class DistroData
    {
        public string DistroImage { get; set; }
        public string DistroName { get; set; }
        public string DistroState { get; set; }
        public int DistroWslVersion { get; set; }
    }
}
