using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModShelter.Extensions
{
    class ItemInfoExtended : ItemInfo
    {
        /// <summary>
        /// Replacing existing with mine
        /// </summary>
        /// <returns></returns>
        public new bool IsShelter()
        {
            return true;
        }

    }
}
