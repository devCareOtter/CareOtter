using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleProject
{
    /// <summary>
    /// Handles conversions specified in bond schema file
    /// </summary>
    public static class BondTypeAliasConverter
    {
        public static long? Convert(DateTime? value, long? unused)
        {
            if (value.HasValue)
                return value.Value.Ticks;
            else
                return null;
        }

        public static DateTime? Convert(long? value, DateTime? unused)
        {
            if (value.HasValue)
                return new DateTime(value.Value);
            else
                return null;
        }

        public static long Convert(DateTime value, long unused)
        {
            return value.Ticks;
        }

        public static DateTime Convert(long value, DateTime unused)
        {
            return new DateTime(value);
        }

        public static Guid Convert(string value, Guid unused)
        {
            return new Guid(value);
        }

        public static string Convert(Guid value, string unused)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("Empty guid has issues");

            return value.ToString();
        }
    }
}
