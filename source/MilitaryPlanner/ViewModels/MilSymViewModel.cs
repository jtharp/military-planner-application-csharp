using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using GeoJSON.Net;
using GeoJSON.Net.Geometry;
using MilitaryPlanner.Controllers;
using MilitaryPlanner.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Geometry = Esri.ArcGISRuntime.Geometry.Geometry;

namespace MilitaryPlanner.ViewModels
{
    public class MilSymViewModel : BaseViewModel
    {
        private MapView _mapView;
        private Map _map;
        private GraphicsOverlay _graphicsOverlay;
        private CoordinateReadoutController _coordinateReadoutController;
        private int _scale;

        public RelayCommand SetMapCommand { get; set; }

        public int Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                RaisePropertyChanged(() => Scale);
            }
        }

        public MilSymViewModel()
        {
            SetMapCommand = new RelayCommand(OnSetMap);
            ESRIGraphicAdded.Register(this, HandleESRIGraphicAdded);
            Scale = 50000;
        }

        private void HandleESRIGraphicAdded(ESRIGraphicAdded esriGraphicAdded)
        {
            AddGraphic(esriGraphicAdded.SID, esriGraphicAdded.points, esriGraphicAdded.Bbox);
        }

        private void OnSetMap(object param)
        {
            var mapView = param as MapView;
            mapView.WrapAround = false;

            if (mapView == null)
            {
                return;
            }
            _mapView = mapView;
            _map = mapView.Map;

            _graphicsOverlay = _mapView.GraphicsOverlays["graphicsOverlay"];
//            _coordinateReadoutController = new CoordinateReadoutController(mapView, this);

//            mapView.MouseLeftButtonDown += mapView_MouseLeftButtonDown;
//            mapView.MouseLeftButtonUp += mapView_MouseLeftButtonUp;
//            mapView.MouseMove += mapView_MouseMove;

            // setup any controllers that use the map view
//            _gotoXYToolController = new GotoXYToolController(mapView, this);
//            _networkingToolController = new NetworkingToolController(mapView, this);
//            _viewShedToolController = new ViewShedToolController(mapView, this);
//            _coordinateReadoutController = new CoordinateReadoutController(mapView, this);

            // add default message layer
//            AddNewMilitaryMessagelayer();
        }

        public void AddGraphic(string sid, List<MapPoint> controlPoints, Envelope bbox)
        {
            var client = new RestClient("http://localhost:3000");
            var request = new RestRequest("tg", Method.POST) {RequestFormat = DataFormat.Json};

            var tgReq = new TgRequest() { SID = sid, Scale = Convert.ToInt32(_mapView.UnitsPerPixel * 111319.49079327357264771338267056 * _mapView.ActualWidth), wp = _mapView.ActualWidth, hp = _mapView.ActualHeight };
            tgReq.SetControlPoints(controlPoints);
            tgReq.SetBbox(_mapView.Extent);
            
            request.AddJsonBody(tgReq);

            var response = client.Execute(request);
            var json = response.Content;

            try
            {

//            var obj = JObject.Parse(json);
//            var coords = obj["features"].First;
                var featureCollection = JsonConvert.DeserializeObject<GeoJSON.Net.Feature.FeatureCollection>(json);
                if (featureCollection == null || featureCollection.Features == null ||
                    featureCollection.Features.Count == 0)
                {
                    return;
                }
                var feature = featureCollection.Features.First();
                var geoType = feature.Type;
                var geometry = feature.Geometry;
                Geometry outGeometry = null;
                Symbol symbol = null;
                if (geometry is MultiLineString || geometry is LineString)
                {
                    symbol = new SimpleLineSymbol() {Color = Colors.Red, Width = 2};
                    if (geometry.Type == GeoJSONObjectType.MultiLineString)
                    {
                        var seglist = new List<IEnumerable<Segment>>();
                        var mls = geometry as MultiLineString;
                        foreach (var lineString in mls.Coordinates)
                        {
                            var segment = new SegmentCollection(new SpatialReference(4236));
                            foreach (var pos in lineString.Coordinates)
                            {
                                var gpos = pos as GeographicPosition;
                                segment.AddPoint(gpos.Longitude, gpos.Latitude);
                            }
                            seglist.Add(segment);
                        }
                        outGeometry = new Polyline(seglist);
                    }
                    else
                    {
                        var ls = geometry as LineString;
                        var segment = new SegmentCollection(new SpatialReference(4236));
                        foreach (var pos in ls.Coordinates)
                        {
                            var gpos = pos as GeographicPosition;
                            segment.AddPoint(gpos.Longitude, gpos.Latitude);
                        }

                        outGeometry = new Polyline(segment);
                    }
                }
                var graphic = new Graphic(outGeometry, symbol);
                _graphicsOverlay.Graphics.Add(graphic);
            }
            catch (Exception)
            {

            }
        }
    }

    class TgRequest
    {
        public string SID { get; set; }
        public string ControlPoints { get; set; }
        public string Bbox { get; set; }
        public int Scale { get; set; }
        public double wp { get; set; }
        public double hp { get; set; }

        public void SetControlPoints(List<MapPoint> points)
        {
            var str = "";
            foreach (var point in points)
            {
                var x = GeometryEngine.Project(point, new SpatialReference(4326)) as MapPoint;
                var lon = ((x.X + 180)%360) - 180;
                var lat = x.Y;
                str = str + (lon+ "," + lat + " ");
            }

            str = str.TrimEnd(' ');
            ControlPoints = str;
        }

        public void SetBbox(Envelope envelope)
        {
            
            var x = GeometryEngine.Project(envelope, new SpatialReference(4326));
            var str = "";
            var ymin = x.Extent.YMin;
            var ymax = x.Extent.YMax;
            if (x.Extent.YMin < -90)
                ymin = -90;
            if (x.Extent.YMax > 90)
                ymax = 90;
            str = x.Extent.XMin + "," + ymin + "," + x.Extent.XMax + "," +
                  ymax;
            
            Bbox = str;
            var tmp = x.Extent.ToString();
        }

        private string _coordinateReadout = "";
        public string CoordinateReadout
        {
            get
            {
                return _coordinateReadout;
            }

            set
            {
                _coordinateReadout = value;
//                RaisePropertyChanged(() => CoordinateReadout);
            }
        }
    }
}
