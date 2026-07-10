using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShiftPlanner.Utility
{
    class UtilityClass
    {


        static public double GetTimeFromString(string UhrzeitString, out TimeOnly OutNormalizedTime)
        {

            TimeOnly NormalizedTime = new();
            double outdouble = 0.0f;
            bool isValid = TryParseFlexibleTime(UhrzeitString, out NormalizedTime);
            if (isValid)
            {
                int hour = NormalizedTime.Hour;

                int min = NormalizedTime.Minute;

                outdouble = hour * 60 + min;
            }

            OutNormalizedTime = NormalizedTime;
            return outdouble;
        }
        static public bool IsValidTimeString(string UhrzeitString)
        {
            TimeOnly NormalizedTime = new();
            bool isValid = TryParseFlexibleTime(UhrzeitString, out NormalizedTime);


            if (isValid)
            {
                int hour = NormalizedTime.Hour;

                int min = NormalizedTime.Minute;

                if (hour > 23 || hour < 0 || min > 59 || min < 0) return false;

            }
            return isValid;
        }
        public static bool TryParseFlexibleTime(string input, out TimeOnly time)
        {
            time = default;

            input = input.Trim();

            // 8 -> 08:00
            if (Regex.IsMatch(input, @"^\d{1,2}$"))
            {
                input += ":00";
            }
            // 800 -> 08:00, 0800 -> 08:00
            else if (Regex.IsMatch(input, @"^\d{3,4}$"))
            {
                input = input.PadLeft(4, '0');
                input = input.Insert(2, ":");
            }

            return TimeOnly.TryParseExact(
                input,
                "H:mm",
                out time);
        }





    }
}
