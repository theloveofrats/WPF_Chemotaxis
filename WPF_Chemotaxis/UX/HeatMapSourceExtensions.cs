using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.UX
{
    public static class HeatMapSourceExtensions
    {

        public static void CacheHeatmapMethods(this IHeatMapSource self, out List<Func<Simulations.Environment, int, int, byte>> heatMethods, out List<HeatMapMethodAttribute> heatMethodTags)
        {
            IEnumerable<MethodInfo> mis = self.GetType().GetMethods().Where(m => m.GetCustomAttribute<HeatMapMethodAttribute>() != null);

            heatMethods = new();
            heatMethodTags = new();
            foreach (MethodInfo mi in mis)
            {
                heatMethods.Add((Func<Simulations.Environment, int, int, byte>)Delegate.CreateDelegate(typeof(Func<Simulations.Environment, int, int, byte>), self, mi));
            }
            heatMethodTags = mis.ToList().Select(m => m.GetCustomAttribute<HeatMapMethodAttribute>()).ToList();
        }
    }        
}
