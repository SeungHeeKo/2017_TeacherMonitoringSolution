using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeacherMonitoringSolution
{
    class StringEnum
    {
        public static string GetStringValue(WindowVideoTitle value)
        {
            string output = null;
            Type type = value.GetType();
            System.Reflection.FieldInfo fi = type.GetField(value.ToString());
            StringValue[] attrs = fi.GetCustomAttributes(typeof(StringValue), false) as StringValue[];
            if (attrs.Length > 0) { output = attrs[0].Value; }
            return output;
        }
    }
}
