using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeacherMonitoringSolution
{
    class Constants
    {
        public int maxClient = 20;

        public Constants()
        {
            clientIP = new List<int>();

            for (int i = 100; i < 100 + maxClient; i++)
            {
                clientIP.Add(i);
            }
        }
        
        public List<int> clientIP
        {
            get;
            private set;
        }
    }
}
