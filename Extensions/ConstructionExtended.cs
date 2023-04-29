using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModShelter.Extensions
{
    public class ConstructionExtended : Construction
    {
		public override void SetUpperLevel(bool set, int level)
		{
			m_UpperLevel = set;
			m_Level = 0;
			base.OnSetUpperLevel(set);
		}
	}
}
