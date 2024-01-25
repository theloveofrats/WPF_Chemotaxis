using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WPF_Chemotaxis.Simulations;
using System.Diagnostics;

namespace WPF_Chemotaxis.Model
{
    public class RegionRule_PrintConcentrations : RegionRule
    {
        private FileStream stream;
       
        public RegionRule_PrintConcentrations()
        {
            this.DisplayName = "Save concentrations";
            hasTick = false;
        }

        public override void Init(Simulation sim, ICollection<Vector2Int> points)
        {
            //Debug.WriteLine("INIT called on concentration printer");

            if (sim.Settings.SaveDirectory != null && sim.Settings.out_freq > 0) 
            {
                try
                {
                    this.stream = new FileStream(sim.TargetDirectory + "\\ConcentrationProfile.csv", FileMode.Create, FileAccess.Write);
                    //Debug.WriteLine("Creating output file for concentration printer");
                    

                    string line = string.Format("X, Y, T");
                    foreach(var lig in Model.MasterElementList)
                    {
                        if(lig is Ligand)
                        {
                            line += String.Format(", {0}", lig.Name);
                        }
                    }
                    line += "\n";
                    byte[] bytes = Encoding.UTF8.GetBytes(line);
                    stream.Write(bytes);

                    sim.WriteToFile += (s, e, args) => this.ExportPointConcentrationsToFile(s, e, points);
                    sim.Close += (s,e,args)=>{
                        this.stream.Flush();
                        this.stream.Close();
                    };
                }
                catch(IOException e)
                {
                    Trace.WriteLine("Failed to create save file for concentration printing Region");
                    Trace.WriteLine(e.Message);
                    Trace.WriteLine(e.StackTrace);
                }
            }
        }
        private void ExportPointConcentrationsToFile(Simulation sim, Simulations.Environment env, ICollection<Vector2Int> points)
        {
            double cnc;
            foreach (var point in points) {
                string line = string.Format("{0}, {1}, {2:0.000}", point.X*env.settings.DX, point.Y*env.settings.DX, sim.Time);
                foreach(var lig in Model.MasterElementList)
                {
                    if(lig is Ligand)
                    {
                        cnc = env.GetConcentration(lig as Ligand, point.X, point.Y);
                        line += string.Format(", {0:0.000000}", cnc);
                   
                    }
                }
                line += "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(line);
                stream.Write(bytes);
            }
            this.stream.Flush();
        }

        public override void Tick(Simulation sim, ICollection<Vector2Int> points)
        {
            throw new NotImplementedException();
        }

        public override void OnClicked(MouseButtonEventArgs e, Simulations.Environment environment, ICollection<Vector2Int> points)
        {
            
        }

        public override void OnLoaded()
        {
            
        }

        public override void Draw(WriteableBitmap targetCanvas, Simulations.Environment environment, ICollection<Vector2Int> points)
        {

        }

        public override FlowDocument Document
        {
            get
            {
                FlowDocument fd = new FlowDocument();
                Paragraph p = new Paragraph();
                TextBlock block = new TextBlock();
                block.Text = "Debug print all ligand concentrations.";

                p.Inlines.Add(block);
                fd.Blocks.Add(p);
                return fd;
            }
            set
            {
                this.Document = value;
            }
        }
    }
}
