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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Symbology.Specialized;
using MilitaryPlanner.Helpers;

namespace MilitaryPlanner.ViewModels
{
    public class OrderOfBattleViewModel : BaseViewModel
    {
        public static SymbolDictionary MilitarySymbolDictionary;

        // Public members for data binding
        public ObservableCollection<SymbolViewModel> Symbols { get; private set; }
        public string SearchString { get; private set; }

        // commands
        public RelayCommand SearchCommand { get; set; }
        public RelayCommand SymbolChangedCommand { get; set; }
        public RelayCommand SymbolDragCommand { get; set; }

        private readonly int _imageSize;

        // Currently selected symbol 
        SymbolViewModel _selectedSymbol = null;
        public SymbolViewModel SelectedSymbol
        {
            get
            {
                return _selectedSymbol;
            }

            set
            {
                _selectedSymbol = value;

                RaisePropertyChanged(() => SelectedSymbol);

                Mediator.NotifyColleagues(Constants.ACTION_SELECTED_SYMBOL_CHANGED, value);
            }
        }

        public SymbolGroupViewModel GroupSymbol
        {
            get
            {
                return _groupSymbol;
            }
        }
        readonly SymbolGroupViewModel _groupSymbol;

        public OrderOfBattleViewModel()
        {
            Mediator.Register(Constants.ACTION_CANCEL, DoActionCancel);
            Mediator.Register(Constants.ACTION_ITEM_WITH_GUID_REMOVED, DoActionItemWithGuidRemoved);
            Mediator.Register(Constants.ACTION_ITEM_WITH_GUID_ADDED, DoActionItemWithGuidAdded);

            // Check the ArcGIS Runtime is initialized
            if (!ArcGISRuntimeEnvironment.IsInitialized)
            {
                ArcGISRuntimeEnvironment.Initialize();
            }
            var p1 = ArcGISRuntimeEnvironment.SymbolsPath;
            // hook the commands
            SearchCommand = new RelayCommand(OnSearch);
            SymbolChangedCommand = new RelayCommand(OnSymbolChanged);

            

            // Create a new MilitarySymbolDictionary instance 
            MilitarySymbolDictionary = new SymbolDictionary(SymbolDictionaryType.Mil2525c);

            // Collection of view models for the displayed list of symbols
            Symbols = new ObservableCollection<SymbolViewModel>();

            // Set the image size
            _imageSize = 96;

            // org tree view
            _groupSymbol = new SymbolGroupViewModel(SymbolLoader.LoadSymbolWrapper());

            ExpandGroupSymbol(_groupSymbol);
        }

        private void SetAllNodesToDraggable()
        {
            foreach (var sym in _groupSymbol.FirstGeneration)
            {
                SetAllLeavesToDraggable(sym);
            }
        }

        private void SetAllLeavesToDraggable(SymbolTreeViewModel stvm)
        {
            stvm.HasBeenDragged = false;

            if (stvm.Children != null && stvm.Children.Count > 0)
            {
                foreach (var stvm2 in stvm.Children)
                {
                    SetAllLeavesToDraggable(stvm2);
                }
            }
        }

        /// <summary>
        /// Method that handles the addition of a symbol to the map view
        /// Sets the property that controls the objects dragability
        /// </summary>
        /// <param name="obj"></param>
        private void DoActionItemWithGuidAdded(object obj)
        {
            var guid = obj as string;

            // object here is a guid
            if (guid == null)
            {
                return;
            }

            foreach (var sym in _groupSymbol.FirstGeneration)
            {
                var temp = FindChildWithGuid(sym, guid);

                if (temp != null)
                {
                    temp.HasBeenDragged = true;
                }
            }
        }

        /// <summary>
        /// Method handles the removal of a symbol from the entire mission
        /// Reset HasBeenDragged property in OOB Tree so that it can be dragged/dropped again
        /// </summary>
        /// <param name="obj"></param>
        private void DoActionItemWithGuidRemoved(object obj)
        {
            var guid = obj as string;

            // object here is a guid
            if (guid == null)
            {
                return;
            }

            foreach (var sym in _groupSymbol.FirstGeneration)
            {
                var temp = FindChildWithGuid(sym, guid);

                if (temp != null)
                {
                    temp.HasBeenDragged = false;
                }
            }
        }

        /// <summary>
        /// Method finds the first child node with the given GUID
        /// </summary>
        /// <param name="stvm"></param>
        /// <param name="guid"></param>
        /// <returns>Tree Symbol object with the given GUID</returns>
        private SymbolTreeViewModel FindChildWithGuid(SymbolTreeViewModel stvm, string guid)
        {
            if (stvm == null)
            {
                return null;
            }

            if (stvm.GUID.CompareTo(guid) == 0)
            {
                return stvm;
            }
            else
            {
                return stvm.Children.Select(stvm2 => FindChildWithGuid(stvm2, guid)).FirstOrDefault(result => result != null);
            }
        }

        private void ExpandGroupSymbol(SymbolGroupViewModel groupSymbol)
        {
            foreach (var svm in groupSymbol.FirstGeneration)
            {
                ExpandSymbolTreeViewModelRecursive(svm);
            }
        }

        private void ExpandSymbolTreeViewModelRecursive(SymbolTreeViewModel svm)
        {
            svm.IsExpanded = true;

            foreach (var item in svm.Children)
            {
                ExpandSymbolTreeViewModelRecursive(item);
            }
        }

        private void DoActionCancel(object obj)
        {
            
        }

        /// <summary>
        /// Handler for when a selection of a symbol has changed
        /// </summary>
        /// <param name="param"></param>
        private void OnSymbolChanged(object param)
        {
            var e = param as SelectionChangedEventArgs;
            if (e == null)
            {
                return;
            }
            if (e.AddedItems.Count != 1)
                return;
            if (e.AddedItems[0].GetType() != typeof(SymbolViewModel))
                return;

            SelectedSymbol = e.AddedItems[0] as SymbolViewModel;
        }

        private void OnSearch(object parameter)
        {
            Dictionary<string, string> filters = new Dictionary<string, string>();

            // Clear the current Symbols collection
            Symbols.Clear();

            // Perform the search applying any selected keywords and filters 
            IEnumerable<SymbolProperties> symbols = MilitarySymbolDictionary.FindSymbols(filters);

            if (!String.IsNullOrWhiteSpace(SearchString))
            {
                foreach (var ss in SearchString.Split(new char[] {';',','}))
                {
                    if (!String.IsNullOrWhiteSpace(ss))
                    {
                        symbols = symbols.Where(s => s.Name.ToLower().Contains(ss.ToLower().Trim()) || s.Keywords.Count(kw => kw.ToLower().Contains(ss.ToLower().Trim())) > 0);
                    }
                }
            }

            var allSymbols = symbols.ToList();

            // Add symbols to UI collection
            foreach (var s in from symbol in allSymbols select new SymbolViewModel(symbol, _imageSize))
            {
                Symbols.Add(s);
            }
        }
    }
}
