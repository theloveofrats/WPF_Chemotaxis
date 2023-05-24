using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Chemotaxis.Simulations
{
    public enum CellEventType { JUST_APPEARED, MITOTIC, DIFFERENTIATED, NONE, NECROTIC, APOPTOTIC }

    public class CellNotificationEventArgs : EventArgs
    {
        public CellEventType EventType { get; private set; } = CellEventType.NONE;
        public Cell OldCell { get; private set; }
        public Cell NewCell { get; private set; }

        public CellNotificationEventArgs(CellEventType eventType, Cell oldCell, Cell newCell)
        {
            EventType = eventType;
            OldCell = oldCell;
            NewCell = newCell;
        }
    }
}
