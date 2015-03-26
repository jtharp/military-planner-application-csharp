// Copyright 2015 Esri 
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
using System.Windows;
using Esri.ArcGISRuntime;
using Microsoft.Win32;
using MilitaryPlanner.Helpers;
using MilitaryPlanner.Models;
using MilitaryPlanner.Views;

namespace MilitaryPlanner.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        #region Properties

        private bool _isFromMediator = false;

        #region MyDateTime

        private DateTime _myDateTime;
        public DateTime MyDateTime
        {
            get { return _myDateTime; }
            set
            {
                if (_myDateTime != value)
                {
                    _myDateTime = value;
                    RaisePropertyChanged(() => MyDateTime);
                }
            }
        }

        #endregion

        private int _sliderMinimum = 0;
        public int SliderMinimum
        {
            get
            {
                return _sliderMinimum;
            }

            set
            {
                if (_sliderMinimum != value)
                {
                    _sliderMinimum = value;
                    RaisePropertyChanged(() => SliderMinimum);
                }
            }
        }

        private int _sliderMaximum = -1;
        public int SliderMaximum
        {
            get
            {
                return _sliderMaximum;
            }

            set
            {
                if (_sliderMaximum != value)
                {
                    _sliderMaximum = value;
                    RaisePropertyChanged(() => SliderMaximum);
                }
            }
        }

        private int _sliderValue = 0;
        public int SliderValue
        {
            get
            {
                return _sliderValue;
            }

            set
            {
                if (_sliderValue != value)
                {
                    if (!_isFromMediator)
                    {
                        Mediator.NotifyColleagues(
                            value > _sliderValue ? Constants.ACTION_PHASE_NEXT : Constants.ACTION_PHASE_BACK, value);
                    }

                    _isFromMediator = false;

                    _sliderValue = value;
                    RaisePropertyChanged(() => SliderValue);
                }
            }
        }

        private MapView _mapView;
        public MapView MapView
        {
            get { return _mapView; }
            set{
                if (_mapView != value)
                {
                    _mapView = value;
                    RaisePropertyChanged(() => MapView);
                }
            }
        }

        private OrderOfBattleView _OOBView;
        private MilSymMap _milSymMapView;

        public OrderOfBattleView OOBView
        {
            get { return _OOBView; }
            set
            {
                if (_OOBView != value)
                {
                    _OOBView = value;
                    RaisePropertyChanged(() => OOBView);
                }
            }
        }

        private MissionTimeLineView _MTLView;
        public MissionTimeLineView MTLView
        {
            get { return _MTLView; }
            set
            {
                if (_MTLView != value)
                {
                    _MTLView = value;
                    RaisePropertyChanged(() => MTLView);
                }
            }
        }

        private Visibility _mapViewVisibility = Visibility.Visible;
        public Visibility MapViewVisibility
        {
            get
            {
                return _mapViewVisibility;
            }

            set
            {
                _mapViewVisibility = value;
                RaisePropertyChanged(() => MapViewVisibility);
            }
        }

        private Visibility _timeLineViewVisibility = Visibility.Collapsed;
        public Visibility TimeLineViewVisibility
        {
            get
            {
                return _timeLineViewVisibility;
            }

            set
            {
                _timeLineViewVisibility = value;
                RaisePropertyChanged(() => TimeLineViewVisibility);
            }
        }
		
		public MilSymMap MilSymMapView
        {
            get { return _milSymMapView; }
            set
            {
                _milSymMapView = value;
                RaisePropertyChanged(() => MilSymMapView);
            }
        }

        #endregion
        
        #region Commands

        public RelayCommand CancelCommand { get; set; }
        public RelayCommand DeleteCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand OpenCommand { get; set; }
        public RelayCommand EditMissionPhasesCommand { get; set; }
        public RelayCommand EditGeometryCommand { get; set; }
        public RelayCommand EditGeometryUndoCommand { get; set; }
        public RelayCommand EditGeometryRedoCommand { get; set; }
        public RelayCommand SwitchViewCommand { get; set; }

        #endregion

        #region Ctor

        public MainWindowViewModel()
        {
            try
            {
                ArcGISRuntimeEnvironment.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Unable to initialize the ArcGIS Runtime with the client id provided: " + ex.Message);
            }

            Mediator.Register(Constants.ACTION_PHASE_ADDED, DoPhaseAdded);
            Mediator.Register(Constants.ACTION_PHASE_INDEX_CHANGED, DoPhaseIndexChanged);
            Mediator.Register(Constants.ACTION_MISSION_LOADED, DoMissionLoaded);

            CancelCommand = new RelayCommand(OnCancelCommand);
            DeleteCommand = new RelayCommand(OnDeleteCommand);
            SaveCommand = new RelayCommand(OnSaveCommand);
            OpenCommand = new RelayCommand(OnOpenCommand);
            EditMissionPhasesCommand = new RelayCommand(OnEditMissionPhases);
            EditGeometryCommand = new RelayCommand(OnEditGeometryCommand);
            EditGeometryRedoCommand = new RelayCommand(OnEditGeometryRedoCommand);
            EditGeometryUndoCommand = new RelayCommand(OnEditGeometryUndoCommand);
            SwitchViewCommand = new RelayCommand(OnSwitchViewCommand);
            
            MapView = new MapView();
            _milSymMapView = new MilSymMap();
            OOBView = new OrderOfBattleView();
            MTLView = new MissionTimeLineView();
        }

        private void OnSwitchViewCommand(object obj)
        {
            if (MapViewVisibility == Visibility.Visible)
            {
                Mediator.NotifyColleagues(Constants.ACTION_CLONE_MISSION, null);
                TimeLineViewVisibility = Visibility.Visible;
                MapViewVisibility = Visibility.Collapsed;
            }
            else
            {
                TimeLineViewVisibility = Visibility.Collapsed;
                MapViewVisibility = Visibility.Visible;
            }
        }

        private void OnEditGeometryUndoCommand(object obj)
        {
            Mediator.NotifyColleagues(Constants.ACTION_EDIT_UNDO, null);
        }

        private void OnEditGeometryRedoCommand(object obj)
        {
            Mediator.NotifyColleagues(Constants.ACTION_EDIT_REDO, null);
        }

        private void OnEditGeometryCommand(object obj)
        {
            Mediator.NotifyColleagues(Constants.ACTION_EDIT_GEOMETRY, null);
        }

        private void OnEditMissionPhases(object obj)
        {
            Mediator.NotifyColleagues(Constants.ACTION_EDIT_MISSION_PHASES, null);
        }

        private void DoPhaseIndexChanged(object obj)
        {
            int index = (int)obj;

            _isFromMediator = true;

            SliderValue = index;
        }

        private void DoPhaseAdded(object obj)
        {
            _isFromMediator = true;
            SliderMaximum++;
            SliderValue = SliderMaximum;
        }

        #endregion

        #region Command Handlers

        private void OnCancelCommand(object obj)
        {
            Mediator.NotifyColleagues(Constants.ACTION_CANCEL, obj);
        }

        private void OnDeleteCommand(object obj)
        {
            Mediator.NotifyColleagues(Constants.ACTION_DELETE, obj);
        }

        private void OnSaveCommand(object obj)
        {
            // file dialog
            var sfd = new SaveFileDialog
            {
                Filter = "Mission xml files (*.xml)|*.xml|Geomessage xml files (*.xml)|*.xml",
                RestoreDirectory = true
            };

            if (sfd.ShowDialog() == true)
            {
                Mediator.NotifyColleagues(Constants.ACTION_SAVE_MISSION, String.Format("{0}{1}{2}", sfd.FilterIndex, Constants.SAVE_AS_DELIMITER, sfd.FileName));
            }
        }

        private void OnOpenCommand(object obj)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "xml files (*.xml)|*.xml",
                RestoreDirectory = true,
                Multiselect = false
            };

            if (ofd.ShowDialog() == true)
            {
                Mediator.NotifyColleagues(Constants.ACTION_OPEN_MISSION, ofd.FileName);
            }
        }

        private void DoMissionLoaded(object obj)
        {
            var mission = obj as Mission;

            if (mission != null)
            {
                InitializeUI(mission);
            }
        }

        private void InitializeUI(Mission mission)
        {
            SliderMinimum = 0;
            SliderMaximum = mission.PhaseList.Count - 1;
            SliderValue = 0;
        }

        #endregion

    }
}