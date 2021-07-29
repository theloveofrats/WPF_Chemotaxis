using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using WPF_Chemotaxis.Simulations;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Base class for implementing a RegionRule to be adhered to in an environmental region.
    /// </summary>
    public abstract class RegionRule
    {
        public RegionRule()
        {

        }

        public delegate void UpdateOptions(object Sender, EventArgs e);

        public event UpdateOptions OptionsUpdated;

        public bool hasTick = false;

        public string DisplayName { get; set; }

        /// <summary>
        /// Rule actions when the simulation loads.
        /// </summary>
        public abstract void Init(Simulation sim, ICollection<Vector2Int> points);

        /// <summary>
        /// Rule actions when the simulation ticks.
        /// </summary>
        public abstract void Tick(Simulation sim, ICollection<Vector2Int> points);

        public abstract void OnLoaded();

        /// <summary>
        /// Mouse listener for clicks on a region (points) in environment that implements this rule.
        /// </summary>
        public virtual void OnClicked(MouseButtonEventArgs e, Simulations.Environment environment, ICollection<Vector2Int> points){}
        /// <summary>
        /// Alterations to the appearance of a region (points) implementing this rule.
        /// </summary>
        public virtual void Draw(WriteableBitmap targetCanvas, Simulations.Environment envionment, ICollection<Vector2Int> points) { }

        /// <summary>
        /// The rich text FlowDocument that describes this rule in the UI.
        /// </summary>
        [JsonIgnore]
        public abstract FlowDocument Document { get; set; }

        /// <summary>
        /// Base event allowing the UI to know that a rule has been updated. Should be called if rule options are changed.
        /// </summary>
        protected virtual void FireUpdate(object sender, EventArgs e)
        {
            if (OptionsUpdated != null)
            {
                OptionsUpdated(sender, e);
            }
        }
    }
}
