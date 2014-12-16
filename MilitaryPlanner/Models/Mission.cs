﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MilitaryPlanner.Helpers;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using ESRI.ArcGIS.Client.AdvancedSymbology;
using ESRI.ArcGIS.Client;

namespace MilitaryPlanner.Models
{
    public class Mission : NotificationObject
    {
        public Mission()
        {

        }

        public Mission(string name)
        {
            Name = name;
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged(() => Name);
                }
            }
        }

        private List<MissionPhase> _phaseList = new List<MissionPhase>();
        public List<MissionPhase> PhaseList
        {
            get { return _phaseList; }
            set
            {
                if (_phaseList != value)
                {
                    _phaseList = value;
                    RaisePropertyChanged(() => PhaseList);
                }
            }
        }

        public bool Save(string filename)
        {
            if (String.IsNullOrWhiteSpace(filename))
            {
                return false;
            }

            XmlSerializer x = new XmlSerializer(this.GetType());
            XmlWriter writer = new XmlTextWriter(filename, System.Text.Encoding.UTF8);

            x.Serialize(writer, this);

            return true;
        }

        public static Mission Load(string filename)
        {
            var xmlOver = new XmlAttributeOverrides();
            var xmlAttr = new XmlAttributes();
            xmlAttr.XmlIgnore = true;

            xmlOver.Add(typeof(ESRI.ArcGIS.Client.Layer), "InitializationFailure", xmlAttr);
            xmlOver.Add(typeof(ESRI.ArcGIS.Client.Layer), "FullExtent", xmlAttr);
            xmlOver.Add(typeof(ESRI.ArcGIS.Client.Layer), "IsInitialized", xmlAttr);
            xmlOver.Add(typeof(ESRI.ArcGIS.Client.Layer), "SpatialReference", xmlAttr);
            xmlOver.Add(typeof(ESRI.ArcGIS.Client.AdvancedSymbology.MessageLayer), "SubLayers", xmlAttr);

            XmlSerializer x = new XmlSerializer(typeof(Mission), xmlOver);

            FileStream fs = new FileStream(filename, FileMode.Open);

            XmlReader reader = XmlReader.Create(fs);

            Mission mission;

            mission = x.Deserialize(reader) as Mission;

            return mission;
        }

        public void DoMessageLayerAdded(object obj)
        {
            var msgLayer = obj as MessageLayer;

            if (msgLayer != null)
            {
                var tempPhase = new MissionPhase("Temp Phase");
                //tempPhase.MessageLayers.Add(msgLayer);
                tempPhase.ID = msgLayer.ID;
                this.PhaseList.Add(tempPhase);
            }
        }

        public void DoMessageProcessed(object obj)
        {
            var kvp = (KeyValuePair<string, Message>)obj;

            if (!String.IsNullOrWhiteSpace(kvp.Key) && kvp.Value != null)
            {
                // find phase
                var phase = PhaseList.First(s => s.ID.Equals(kvp.Key));

                if (phase != null)
                {
                    var pm = new PersistentMessage();

                    pm.ID = kvp.Value.Id;
                    //pm.Properties = kvp.Value.
                }
            }
        }

    }

    public class MissionPhase : NotificationObject
    {
        public MissionPhase()
        {
        }

        public MissionPhase(string name)
        {
            Name = name;
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged(() => Name);
                }
            }
        }

        public string ID { get; set; }
        public TimeExtent VisibleTimeExtent { get; set; }

        private List<PersistentMessage> _persistentMessages = new List<PersistentMessage>();

        public List<PersistentMessage> PersistentMessages
        {
            get
            {
                return _persistentMessages;
            }
            set
            {
                if (value != _persistentMessages)
                {
                    _persistentMessages = value;
                }
            }
        }

        //private List<MessageLayer> _messageLayers = new List<MessageLayer>();

        //public List<MessageLayer> MessageLayers
        //{
        //    get { return _messageLayers; }
        //    set
        //    {
        //        if (_messageLayers != value)
        //        {
        //            _messageLayers = value;
        //            RaisePropertyChanged(() => MessageLayers);
        //        }
        //    }
        //}
    }

    public class PropertyItem
    {
        public PropertyItem() { }

        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class PersistentMessage
    {
        public PersistentMessage()
        {

        }

        public string ID
        {
            get;
            set;
        }
        public List<PropertyItem> PropertyItems
        {
            get;
            set;
        }
        //private List<KeyValuePair<string, string>> _Properties = new List<KeyValuePair<string, string>>();
        //public List<KeyValuePair<string, string>> Properties
        //{
        //    get
        //    {
        //        return _Properties;
        //    }
        //    set
        //    {
        //        if (_Properties != value)
        //        {
        //            _Properties = value;
        //        }
        //    }
        //}
    }
}