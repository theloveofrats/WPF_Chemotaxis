using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis
{
    public enum CellDeathType { NONE, NECROTIC, APOPTOTIC}

    public class CellNotificationEventArgs : EventArgs
    {
        public CellDeathType DeathType = CellDeathType.NONE;
    }
}
