using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModShelter.Extensions
{
    public class ConstructionGhostExtended : ConstructionGhost
    {
        public override void UpdateProhibitionType(bool check_is_snapped = true)
        {
            m_ProhibitionType = ProhibitionType.None;
            return;
        }
    }
}
