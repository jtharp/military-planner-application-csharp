﻿// Copyright 2015 Esri 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.NetworkAnalyst;
using MilitaryPlanner.ViewModels;
using MilitaryPlanner.Views;
using MapView = Esri.ArcGISRuntime.Controls.MapView;

namespace MilitaryPlanner.Controllers
{
    public class NetworkingToolController
    {
        private readonly MapView _mapView;
        private readonly NetworkingToolView _networkingToolView;

        private const string OnlineRoutingService = "http://sampleserver6.arcgisonline.com/arcgis/rest/services/NetworkAnalysis/SanDiego/NAServer/Route";

        private readonly OnlineRouteTask _routeTask;
        private readonly Symbol _directionPointSymbol;
        private readonly GraphicsOverlay _stopsOverlay;
        private readonly GraphicsOverlay _routesOverlay;
        private readonly GraphicsOverlay _directionsOverlay;

        public NetworkingToolController(MapView mapView, MapViewModel mapViewModel)
        {
            this._mapView = mapView;

            _networkingToolView = new NetworkingToolView {PlacementTarget = mapView, ViewModel = {mapView = mapView}};

            var owner = Window.GetWindow(mapView);

            if (owner != null)
            {
                owner.LocationChanged += (sender, e) =>
                {
                    _networkingToolView.HorizontalOffset += 1;
                    _networkingToolView.HorizontalOffset -= 1;
                };
            }

            // hook mapview events
            mapView.MapViewTapped += mapView_MapViewTapped;
            mapView.MapViewDoubleTapped += mapView_MapViewDoubleTapped;

            // hook listDirections
            _networkingToolView.listDirections.SelectionChanged += listDirections_SelectionChanged;

            // hook view resources
            _directionPointSymbol = _networkingToolView.LayoutRoot.Resources["directionPointSymbol"] as Symbol;

            mapView.GraphicsOverlays.Add(new GraphicsOverlay { ID="RoutesOverlay", Renderer=_networkingToolView.LayoutRoot.Resources["routesRenderer"] as Renderer});
            mapView.GraphicsOverlays.Add(new GraphicsOverlay { ID="StopsOverlay" });
            mapView.GraphicsOverlays.Add(new GraphicsOverlay { ID = "DirectionsOverlay", Renderer=_networkingToolView.LayoutRoot.Resources["directionsRenderer"] as Renderer, SelectionColor=Colors.Red });

            _stopsOverlay = mapView.GraphicsOverlays["StopsOverlay"];
            _routesOverlay = mapView.GraphicsOverlays["RoutesOverlay"];
            _directionsOverlay = mapView.GraphicsOverlays["DirectionsOverlay"];

            _routeTask = new OnlineRouteTask(new Uri(OnlineRoutingService));
        }

        public void Toggle()
        {
            _networkingToolView.ViewModel.Toggle();

            if (!_networkingToolView.ViewModel.IsToolOpen)
            {
                Reset();
            }
        }

        private void Reset()
        {
            // clean up
            _networkingToolView.ViewModel.PanelResultsVisibility = Visibility.Collapsed;

            _stopsOverlay.Graphics.Clear();
            _routesOverlay.Graphics.Clear();
            _directionsOverlay.GraphicsSource = null;
        }

        private void mapView_MapViewTapped(object sender, MapViewInputEventArgs e)
        {
            if (!_networkingToolView.ViewModel.IsToolOpen)
                return;

            try
            {
                e.Handled = true;

                if (_directionsOverlay.Graphics.Any())
                {
                    Reset();
                }

                var graphicIdx = _stopsOverlay.Graphics.Count + 1;
                _stopsOverlay.Graphics.Add(CreateStopGraphic(e.Location, graphicIdx));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sample Error");
            }
        }

        private async void mapView_MapViewDoubleTapped(object sender, MapViewInputEventArgs e)
        {
            if (!_networkingToolView.ViewModel.IsToolOpen)
                return;

            if (_stopsOverlay.Graphics.Count() < 2)
                return;

            try
            {
                e.Handled = true;

                _networkingToolView.ViewModel.PanelResultsVisibility = Visibility.Collapsed;
                _networkingToolView.ViewModel.ProgressVisibility = Visibility.Visible;

                RouteParameters routeParams = await _routeTask.GetDefaultParametersAsync();
                routeParams.OutSpatialReference = _mapView.SpatialReference;
                routeParams.ReturnDirections = true;
                routeParams.DirectionsLengthUnit = LinearUnits.Miles;
                routeParams.DirectionsLanguage = new CultureInfo("en-Us"); // CultureInfo.CurrentCulture;
                routeParams.SetStops(_stopsOverlay.Graphics);

                var routeResult = await _routeTask.SolveAsync(routeParams);
                if (routeResult == null || routeResult.Routes == null || !routeResult.Routes.Any())
                    throw new Exception("No route could be calculated");

                var route = routeResult.Routes.First();
                _routesOverlay.Graphics.Add(new Graphic(route.RouteFeature.Geometry));

                _directionsOverlay.GraphicsSource = route.RouteDirections.Select(rd => GraphicFromRouteDirection(rd));
                _networkingToolView.ViewModel.Graphics = _directionsOverlay.Graphics;

                var totalTime = route.RouteDirections.Select(rd => rd.Time).Aggregate(TimeSpan.Zero, (p, v) => p.Add(v));
                var totalLength = route.RouteDirections.Select(rd => rd.GetLength(LinearUnits.Miles)).Sum();
                _networkingToolView.ViewModel.RouteTotals = string.Format("Time: {0:h':'mm':'ss} / Length: {1:0.00} mi", totalTime, totalLength);

                if (!route.RouteFeature.Geometry.IsEmpty)
                    await _mapView.SetViewAsync(route.RouteFeature.Geometry.Extent.Expand(1.25));
            }
            catch (AggregateException ex)
            {
                var innermostExceptions = ex.Flatten().InnerExceptions;
                if (innermostExceptions != null && innermostExceptions.Count > 0)
                {
                    MessageBox.Show(innermostExceptions[0].Message, "Sample Error");
                }
                else
                {
                    MessageBox.Show(ex.Message, "Sample Error");
                }
                Reset();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Sample Error");
                Reset();
            }
            finally
            {
                _networkingToolView.ViewModel.ProgressVisibility = Visibility.Collapsed;

                if (_directionsOverlay.Graphics.Any())
                {
                    _networkingToolView.ViewModel.PanelResultsVisibility = Visibility.Visible;
                }
            }
        }

        void listDirections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _directionsOverlay.ClearSelection();

            if (e.AddedItems != null && e.AddedItems.Count == 1)
            {
                var graphic = e.AddedItems[0] as Graphic;
                if (graphic != null) graphic.IsSelected = true;
            }
        }

        private Graphic CreateStopGraphic(MapPoint location, int id)
        {
            var symbol = new CompositeSymbol();
            symbol.Symbols.Add(new SimpleMarkerSymbol { Style = SimpleMarkerStyle.Circle, Color = Colors.Blue, Size = 16 });
            symbol.Symbols.Add(new TextSymbol
            {
                Text = id.ToString(),
                Color = Colors.White,
                VerticalTextAlignment = VerticalTextAlignment.Middle,
                HorizontalTextAlignment = HorizontalTextAlignment.Center,
                YOffset = -1
            });

            var graphic = new Graphic
            {
                Geometry = location,
                Symbol = symbol
            };

            return graphic;
        }

        private Graphic GraphicFromRouteDirection(RouteDirection rd)
        {
            var graphic = new Graphic(rd.Geometry);
            graphic.Attributes.Add("Direction", rd.Text);
            graphic.Attributes.Add("Time", string.Format("{0:h\\:mm\\:ss}", rd.Time));
            graphic.Attributes.Add("Length", string.Format("{0:0.00}", rd.GetLength(LinearUnits.Miles)));
            if (rd.Geometry is MapPoint)
                graphic.Symbol = _directionPointSymbol;

            return graphic;
        }

    }
}
