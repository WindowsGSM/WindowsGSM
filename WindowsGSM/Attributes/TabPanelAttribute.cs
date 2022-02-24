using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsGSM.Attributes
{
    public sealed class TabPanelAttribute : Attribute
    {
        public string Text { get; set; } = string.Empty;
    }
}
