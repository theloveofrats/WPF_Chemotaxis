using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WPF_Chemotaxis.UX
{
    public class UIParameterLink
    {
        private PropertyInfo targetProperty;
        private object targetObject; 
        
        public UIParameterLink(PropertyInfo targetProperty, object targetObject)
        {
            this.targetProperty = targetProperty;
            this.targetObject = targetObject;
        }

        public string label {
            get
            {
                if (targetProperty == null) return "Apparently this doesn't even have a target property- deeply weird.";
                if (targetProperty.GetCustomAttribute<Param>() == null) return "There's no param attribute decorator, why is this being called?";
                return targetProperty.GetCustomAttribute<Param>().Name;
            }
        }
        
        public string value {
            get
            {


                if (targetProperty.PropertyType == typeof(CenteredDoubleRange))
                {
                    CenteredDoubleRange range = (CenteredDoubleRange)targetProperty.GetValue(targetObject);
                    return range.Value.ToString();
                }
                else if (targetProperty.PropertyType == typeof(CenteredIntRange))
                {
                    CenteredIntRange range = (CenteredIntRange)targetProperty.GetValue(targetObject);
                    return range.Value.ToString();
                }
                else if (targetProperty.PropertyType == typeof(string))
                {
                    return (string) targetProperty.GetValue(targetObject);
                }

                return targetProperty.GetValue(targetObject).ToString();
            }
            set
            {
                Param param = targetProperty.GetCustomAttribute<Param>();

                if (targetProperty.PropertyType == typeof(CenteredDoubleRange))
                {
                    CenteredDoubleRange currentRange = (CenteredDoubleRange) targetProperty.GetValue(targetObject);

                    double val;
                    if (Double.TryParse(value, out val))
                    {

                        if (param.Max < val) val = param.Max;
                        if (param.Min > val) val = param.Min;

                        CenteredDoubleRange newRange = new CenteredDoubleRange(val, currentRange.Range);
                        newRange.HardMax = param.Max;
                        newRange.HardMin = param.Min;

                        //Note- the reason we set, rather than updating fields, is to fire the update notification event on the targeted parameter.
                        targetProperty.SetValue(targetObject, newRange);
                    }
                }
                else if (targetProperty.PropertyType == typeof(CenteredIntRange))
                {
                    CenteredIntRange currentRange = (CenteredIntRange)targetProperty.GetValue(targetObject);

                    int val;
                    if (Int32.TryParse(value, out val))
                    {
                        if (param.Max < val) val = (int)param.Max;
                        if (param.Min > val) val = (int)param.Min;

                        CenteredIntRange newRange = new CenteredIntRange(val, currentRange.Range);
                        newRange.HardMax = (int?) param.Max;
                        newRange.HardMin = (int?) param.Min;

                        //Note- the reason we set, rather than updating fields, is to fire the update notification event on the targeted parameter.
                        targetProperty.SetValue(targetObject, newRange);
                    }
                }





                // Try to parse the passed-in value as an int, a double or a bool. Let's just say there aren't other types used! 
                if (targetProperty.PropertyType == typeof(double))
                {
                    double val;
                    if (Double.TryParse(value,out val)){

                        if (param.Max < val) val = param.Max;
                        if (param.Min > val) val = param.Min;

                        targetProperty.SetValue(targetObject,val);
                    }
                }
                else if (targetProperty.PropertyType == typeof(int))
                {
                    int val;
                    if (Int32.TryParse(value, out val))
                    {
                        if (param.Max < val) val = (int)param.Max;
                        if (param.Min > val) val = (int)param.Min;

                        targetProperty.SetValue(targetObject, val);
                    }
                }
                else if (targetProperty.PropertyType == typeof(bool))
                {
                    bool val;
                    if (Boolean.TryParse(value, out val))
                    {
                        targetProperty.SetValue(targetObject, val);
                    }
                }
                else if(targetProperty.PropertyType == typeof(Color)){
                    Color val = default(Color);

                    try
                    {
                        val = (Color)ColorConverter.ConvertFromString(value);
                        targetProperty.SetValue(targetObject, val);
                    }
                    catch(NotSupportedException e)
                    {
                        Console.WriteLine(e.StackTrace);
                    }
                }
            }
        }
        public string range
        {
            get
            {
                if (targetProperty.PropertyType == typeof(CenteredDoubleRange))
                {
                    CenteredDoubleRange range = (CenteredDoubleRange) targetProperty.GetValue(targetObject);
                    return range.Range.ToString();
                }
                else if (targetProperty.PropertyType == typeof(CenteredIntRange))
                {
                    CenteredIntRange range = (CenteredIntRange) targetProperty.GetValue(targetObject);
                    return range.Range.ToString();
                }
                else return "N/A";
            }
            set
            {
                Param param = targetProperty.GetCustomAttribute<Param>();

               

                if (targetProperty.PropertyType == typeof(CenteredDoubleRange))
                {
                    CenteredDoubleRange currentRange = (CenteredDoubleRange)targetProperty.GetValue(targetObject);

                    double val;
                    if (Double.TryParse(value, out val))
                    {
                        if (param.Max < val) val = param.Max;
                        if (param.Min > val) val = param.Min;

                        CenteredDoubleRange newRange = new CenteredDoubleRange(currentRange.Value, val);
                        newRange.HardMax = param.Max;
                        newRange.HardMin = param.Min;

                        //Note- the reason we set, rather than updating fields, is to fire the update notification event on the targeted parameter.
                        targetProperty.SetValue(targetObject, newRange);
                    }
                }
                else if (targetProperty.PropertyType == typeof(CenteredIntRange))
                {
                    CenteredIntRange currentRange = (CenteredIntRange)targetProperty.GetValue(targetObject);

                    int val;
                    if (Int32.TryParse(value, out val))
                    {
                        if (param.Max < val) val = (int)param.Max;
                        if (param.Min > val) val = (int)param.Min;

                        CenteredIntRange newRange = new CenteredIntRange(currentRange.Value, val);
                        newRange.HardMax = (int?) param.Max;
                        newRange.HardMin = (int?) param.Min;

                        //Note- the reason we set, rather than updating fields, is to fire the update notification event on the targeted parameter.
                        targetProperty.SetValue(targetObject, newRange);
                    }
                }

                else return;
            }
        }
    }
}
