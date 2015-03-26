using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;

namespace MilitaryPlanner.Helpers
{
    class ESRIGraphicAdded : BaseMessage<ESRIGraphicAdded>
    {
        public string SID { get; set; }
        public List<MapPoint> points { get; set; }
        public Envelope Bbox { get; set; }
    }
}
