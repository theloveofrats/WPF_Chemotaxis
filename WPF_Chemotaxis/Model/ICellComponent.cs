using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WPF_Chemotaxis.Simulations;

namespace WPF_Chemotaxis.Model
{
    /// <summary>
    /// Interface for logical components that determine a cell's response to its environmental inputs.
    /// ICellComponents can modify values for these inputs from the base values, and apply custom update logic
    /// to, for example, change how a cell moves in response to inputs.
    /// </summary>
    public interface ICellComponent
    {
        /// <summary>
        /// Called on every component each dt to allow ongoing custom logic synced with the simulation. 
        /// If you don't need updates each dt consider running an internal timer with a longer tick that
        /// reads its time from this.
        /// </summary>
        /// <param name="cell">The Cell that is performing an Update().</param>
        /// <param name="sim">The running Simulation, of which cell is a part/</param>
        /// <param name="env">The Environment that sim is acting in, which contains the ligand grids and motion/diffusion rules.</param>
        /// <param name="flow">The IFluidModel describing advection in env. Currently unimplemented!</param>
        public void Update(Cell cell, Simulation sim, Simulations.Environment env, IFluidModel flow);
        /// <summary>
        /// Called when Simulation sim starts. Use this to perform any required set-up for the component,
        /// e.g. subscribing to "Cell added" and "Cell removed" events.
        /// </summary>
        /// <param name="sim">The Simulation that is initialising.</param>
        public void Initialise(Simulation sim);

        /// <summary>
        /// When the occupancy difference of Receptor receptor is called, the base value calculated by Cell cell 
        /// is passed through this method on all components first, in case they need to amend the value.
        /// </summary>
        /// <param name="cell">The Cell for which receptor occ. diff. is being requested.</param>
        /// <param name="receptor">The Receptor type being requested.</param>
        /// <param name="base_vector">The original value supplied by cell, unmodified by other components.</param>
        /// <param name="modified_vector">The value to be used for altered output, possibly already altered by other ICellComponents.</param>
        public void ModifyReceptorDifference(Cell cell, Receptor receptor, Vector base_vector, ref Vector modified_vector)
        {

        }

        /// <summary>
        /// When the weight (i.e. the influence or expression level) of Receptor receptor is called, the base value calculated by Cell cell 
        /// is passed through this method on all components first, in case they need to amend the value.
        /// </summary>
        /// <param name="cell">The Cell for which receptor weight is being requested.</param>
        /// <param name="receptor">The Receptor type being requested.</param>
        /// <param name="base_value">The original value supplied by cell, unmodified by other components.</param>
        /// <param name="modified_value">The value to be used for altered output, possibly already altered by other ICellComponents.</param>
        public void ModifyReceptorWeight(Cell cell, Receptor receptor, double base_value, ref double modified_value)
        {

        }
        /// <summary>
        /// When the activity (occupancy * bound ligand efficacy) of Receptor receptor is called, the base value 
        /// calculated by Cell cell is passed through this method on all components first, in case they need to amend the value.
        /// </summary>
        /// <param name="cell">The cell for which receptor occ. diff. is being requested.</param>
        /// <param name="receptor">The receptor type being requested.</param>
        /// <param name="base_value">The original value supplied by cell, unmodified by other components.</param>
        /// <param name="modified_value">The value to be used for altered output, possibly already altered by other ICellComponents.</param>
        public void ModifyReceptorActivity(Cell cell, Receptor receptor, double base_value, ref double modified_value)
        {

        }

        /// <summary>
        /// When Cell cell's Speed is called, the base value is passed through this method on all components first for ammendment.
        /// </summary>
        /// <param name="cell">The cell for which receptor occ. diff. is being requested.</param>
        /// <param name="base_value">The original value supplied by cell, unmodified by other components.</param>
        /// <param name="modified">The value to be used for altered output, possibly already altered by other ICellComponents.</param>
        public void ModifySpeed(Cell cell, double base_value, ref double modified)
        {
            
        }





        /// <summary>
        /// When the cell draw handler is called, it may (or may not- it is up to the handler!) pass the primary and secondary draw 
        /// colours to each component via this method. This allows a cell component to modify the appearance of cells it is in contact with.
        /// Other components may do the same, of course, so the original colour is supplied along with the modified versions.
        /// </summary>
        /// <param name="cell">The cell being drawn</param>
        /// <param name="base_primary">Primary colour supplied by the draw handler</param>
        /// <param name="base_secondary">Secondary colour supplied by the draw handler</param>
        /// <param name="modified_primary">Primary colour for ICellComponent modification</param>
        /// <param name="modified_secondary">Secondary colour for ICellComponent modification</param>
        public void ModifyDrawColour(Cell cell, Color base_primary, Color base_secondary, ref Color modified_primary, ref Color modified_secondary)
        {

        }
    }
}
