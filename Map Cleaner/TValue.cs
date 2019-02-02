﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Map_Cleaner
{
    class TValue
    {
        public string StringValue { get; set; }
        public dynamic Value { get => GetValue(); set => SetValue(value); }

        public TValue(string str)
        {
            StringValue = str;
        }

        public dynamic GetValue()
        {
            if (double.TryParse(StringValue, out double d))
            {
                if (StringValue.Split('.').Count() > 1)
                {
                    return d;
                }
                else
                {
                    return int.Parse(StringValue);
                }
            }
            else
            {
                return StringValue;
            }
        }

        public void SetValue(dynamic value)
        {
            StringValue = value.ToString();
        }

        public string GetStringValue()
        {
            return StringValue;
        }
    }
}
