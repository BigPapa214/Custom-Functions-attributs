using System;
using System.Linq;
using System.Reflection;

namespace Mfundo_Zenzile_Project_2___Fun_with_Attributes
{
    class Program
    {
        static void Main(string[] args)
        {
            Mymodel m = new Mymodel()
            {
                prop1 = 0,
                prop2 = 16d,
                prop3 = "text",
                Prop4 = DateTime.Now,
                ShortCircuitOnInvalid = false

            };
            try
            {
                //====================================
                Console.WriteLine("Initial Configuration");
                if(m.IssValid)
                {
                    Console.WriteLine("Object is valid");
                }
                else
                {
                    Console.WriteLine("Object Not valid");
                }
                //===================================
                m.Prop4 = m.TriggerDate.AddDays(-1);
                Console.WriteLine("After correcting Prop4 (a Date Time):");
               if(m.IssValid)
                {
                    Console.WriteLine("Object is Valid");
                }
               else
                {
                    Console.WriteLine(m.InvalidPropertyMessage);
                }
                m.prop2 = 7d;
                Console.WriteLine("After Correcting Prop2 (a Double):");
                if(m.IssValid)
                {
                    Console.WriteLine("Object is valid");
                }
                else
                {
                    Console.WriteLine(m.InvalidPropertyMessage);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadLine();
        }
    }
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public class InvalidValueAttribute : System.Attribute
    {
        public enum TrigType
        {
            Valid,
            Equal,
            NotEqual,
            over,
            Under
        };
        // This implement the configuration properties.
        public TrigType Trigger { get; protected set; }

        public object TrigValue { get; protected set; }
        public Type ExpectedType { get; protected set; }

        public object PropertyValue { get; protected set; }

        //This Property allows the object that contains the decorated property to display the validity status.

        public string TriggerMsg
        {
            get
            {
                string formart = string.Empty;
                switch (this.Trigger)
                {
                    case TrigType.Valid:
                    case TrigType.Equal:
                        formart = "Equal to";
                        break;
                    case TrigType.NotEqual:
                        formart = "Not Equal to";
                        break;
                    case TrigType.over:
                        formart = "greater than";
                        break;
                    case TrigType.Under:
                        formart = "Less than";
                        break;
                }
                if (!string.IsNullOrEmpty(formart))
                {
                    formart = string.Concat("Cannot be", formart, "'{0}'.\r\n Current VAlue is '{1}'.\r\n");
                }
                return (!string.IsNullOrEmpty(formart)) ? string.Format(formart, this.TrigValue, this.PropertyValue)
                         : string.Empty;
            }
        }

        public InvalidValueAttribute(object triggerValue, TrigType TrigT = TrigType.Valid, Type ExpectedTypes = null)
        {
            if (this.IsIntrinsic(triggerValue.GetType()))
            {
                this.Trigger = TrigT;
                if (ExpectedTypes != null)
                {
                    if (this.IsDateTime(ExpectedTypes))
                    {
                        // Let's try to avoid stupid programming tricks
                        long ticks = Math.Min(Math.Max(0, Convert.ToInt64(triggerValue)), Int64.MaxValue);
                        // instantiate a date time with the ticks
                        this.TrigValue = new DateTime(ticks);
                    }
                    else
                    {
                        this.TrigValue = triggerValue;
                    }
                    this.ExpectedType = ExpectedTypes;
                }
                else
                {
                    this.TrigValue = triggerValue;
                    this.ExpectedType = triggerValue.GetType();
                }
            }
            else
            {
                throw new ArgumentException("The triggerValue parameter must be a primitive, string," +
                    " or DateTime, and must match the type of the attributed property");
            }
        }
        public bool IsValid(object value)
        {
            // assume the value is not valid
            bool result = false;
            //Save the value for use in the trigger message
            this.PropertyValue = value;
            //get type represented by the value
            Type ValueType = value.GetType();

            if (this.IsDateTime(ValueType))
            {
                //ensure that the trigger value is a date time
                this.TrigValue = this.MakeNormalizedDate();
                //and set the expected type of the fallowing comparison
                this.ExpectedType = typeof(DateTime);
            }
            if (ValueType == this.ExpectedType)
            {
                switch (this.Trigger)
                {
                    case TrigType.Valid:
                    case TrigType.Equal:
                        result = this.IsEqual(value, this.TrigValue);
                        break;
                    case TrigType.NotEqual:
                        result = this.isNotEqual(value, this.TrigValue);
                        break;
                    case TrigType.over:
                        result = !this.GreaterThan(value, this.TrigValue);
                        break;
                    case TrigType.Under:
                        result = !this.LessThan(value, this.TrigValue);
                        break;
                }
            }
            else
            {
                throw new ArgumentException("The property type value and trigger value are not of compatible type");
            }
            return result;
        }
        //Adjust the trigger value to a date time  if the trigger value is an integer
        private DateTime MakeNormalizedDate()
        {
            DateTime date = new DateTime();
            if (this.IsInteger(this.TrigValue.GetType()))
            {
                long trick = Math.Min(Math.Max(0, Convert.ToInt64(this.TrigValue)), Int64.MaxValue);
                date = new DateTime(trick);
            }
            else if (this.IsDateTime(this.TrigValue.GetType()))
            {
                date = Convert.ToDateTime(this.TrigValue);
            }
            return date;
        }
        protected bool IsUnsignedInteger(Type type)
        {
            return (type != null) && (type == typeof(uint) || type == typeof(ushort) || type == typeof(ulong));
        }
        protected bool IsInteger(Type type)
        {
            return (type != null) && (this.IsUnsignedInteger(type) || type == typeof(byte) || type == typeof(sbyte)
                || type == typeof(int) || type == typeof(short) || type == typeof(long));
        }
        public bool IsDecimal(Type type)
        {
            return (type != null && type == typeof(decimal));
        }
        public bool IsString(Type type)
        {
            return (type != null && type == typeof(string));
        }
        protected bool IsDateTime(Type type)
        {
            return (type != null && type == typeof(DateTime));
        }
        protected bool IsFloatingPoint(Type type)
        {
            return (type != null && (type == typeof(double) || type == typeof(float)));
        }
        protected bool IsIntrinsic(Type type)
        {
            return (this.IsInteger(type) || this.IsDecimal(type) || this.IsFloatingPoint(type)
                || this.IsString(type) || this.IsDateTime(type));
        }
        protected bool LessThan(object obj1, object obj2)
        {
            bool result = false;
            Type objType = obj1.GetType();

            if (this.IsInteger(objType))
            {
                result = (this.IsUnsignedInteger(objType) && this.IsUnsignedInteger(obj2.GetType())) ?
                (Convert.ToUInt64(obj1) < Convert.ToUInt64(obj2)) : (Convert.ToUInt64(obj1) < Convert.ToUInt64(obj2));
            }
            else if (this.IsFloatingPoint(objType))
            {
                result = (Convert.ToDouble(obj1) < Convert.ToDouble(obj2));
            }
            else if (this.IsDateTime(objType))
            {
                result = (Convert.ToDateTime(obj1) < Convert.ToDateTime(obj2));
            }
            else if (this.IsDecimal(objType))
            {
                result = (Convert.ToDecimal(obj1) < Convert.ToDecimal(obj1));
            }
            else if (this.IsString(objType))
            {
                result = (Convert.ToString(obj1).CompareTo(Convert.ToString(obj2)) < 0);
            }
            return result;
        }
        protected bool GreaterThan(object obj1, object obj2)
        {
            bool result = false;
            Type objType = obj1.GetType();

            if (this.IsInteger(objType))
            {
                result = (this.IsUnsignedInteger(objType) && this.IsUnsignedInteger(obj2.GetType())) ?
                (Convert.ToUInt64(obj1) < Convert.ToUInt64(obj2)) : (Convert.ToUInt64(obj1) > Convert.ToUInt64(obj2));
            }
            else if (this.IsFloatingPoint(objType))
            {
                result = (Convert.ToDouble(obj1) > Convert.ToDouble(obj2));
            }
            else if (this.IsDateTime(objType))
            {
                result = (Convert.ToDateTime(obj1) > Convert.ToDateTime(obj2));
            }
            else if (this.IsDecimal(objType))
            {
                result = (Convert.ToDecimal(obj1) > Convert.ToDecimal(obj1));
            }
            else if (this.IsString(objType))
            {
                result = (Convert.ToString(obj1).CompareTo(Convert.ToString(obj2)) > 0);
            }
            return result;
        }
        protected bool IsEqual(object obj1, object obj2)
        {
            bool result = false;
            Type objType = obj1.GetType();

            if (this.IsInteger(objType))
            {
                result = (this.IsUnsignedInteger(objType) && this.IsUnsignedInteger(obj2.GetType())) ?
                (Convert.ToUInt64(obj1) < Convert.ToUInt64(obj2)) : (Convert.ToUInt64(obj1) == Convert.ToUInt64(obj2));
            }
            else if (this.IsFloatingPoint(objType))
            {
                result = (Convert.ToDouble(obj1) == Convert.ToDouble(obj2));
            }
            else if (this.IsDateTime(objType))
            {
                result = (Convert.ToDateTime(obj1) == Convert.ToDateTime(obj2));
            }
            else if (this.IsDecimal(objType))
            {
                result = (Convert.ToDecimal(obj1) == Convert.ToDecimal(obj1));
            }
            else if (this.IsString(objType))
            {
                result = (Convert.ToString(obj1).CompareTo(Convert.ToString(obj2)) == 0);
            }
            return result;
        }

        protected bool isNotEqual(object obj1, object obj2)
        {
            return (!this.IsEqual(obj1, obj2));
        }
    }

        class Mymodel
        {
            public const long TRIGGER_DATE = 630822816000000000;

            public const string TRIGGER_STRING = "ERROR";

            [InvalidValue(-1, InvalidValueAttribute.TrigType.Valid)]
            public int prop1 { get; set; }
            [InvalidValue(5d, InvalidValueAttribute.TrigType.Under)]
            [InvalidValue(10d, InvalidValueAttribute.TrigType.over)]

            public double prop2 { get; set; }
            [InvalidValue(TRIGGER_STRING, InvalidValueAttribute.TrigType.Valid)]
            public string prop3 { get; set; }
            [InvalidValue(TRIGGER_DATE, InvalidValueAttribute.TrigType.over, typeof(DateTime))]
            public DateTime Prop4 { get; set; }

            public DateTime TriggerDate { get { return new DateTime(TRIGGER_DATE); } }
            public bool ShortCircuitOnInvalid { get; set; }
            public string InvalidPropertyMessage { get; private set; }

            public bool IssValid
            {
                get
                {
                    this.InvalidPropertyMessage = string.Empty;
                    bool isValid = true;

                    PropertyInfo[] infos = this.GetType().GetProperties();

                    foreach (var item in infos)
                    {
                        var attribs = item.GetCustomAttributes(typeof(InvalidValueAttribute), true);

                        if (attribs.Count() > 1)
                        {
                            var distinct = attribs.Select(x => ((InvalidValueAttribute)(x)).Trigger).Distinct();
                            if (attribs.Count() != distinct.Count())
                            {
                                throw new Exception(string.Format("{0} has at least one duplicate Invalid" +
                                    " value attribute specified.", item.Name));
                            }
                        }
                        foreach (InvalidValueAttribute attr in attribs)
                        {
                            object value = item.GetValue(this, null);

                            bool PropValid = attr.IsValid(value);

                            if (!PropValid)
                            {
                                isValid = false;

                                this.InvalidPropertyMessage = string.Format("{0}\r\n{1}",
                                this.InvalidPropertyMessage, string.Format("{0} is invalid. {1}", item.Name, attr.TriggerMsg));
                                if (this.ShortCircuitOnInvalid)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    return isValid;
                }
            }

        }
    
}
