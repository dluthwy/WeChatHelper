using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace WeChatHelper
{
    public class SendWeek : INotifyPropertyChanged
    {
        private bool _isSelected = false;
        private string _theText = "NULL";
        private string _enText = "NULL";

        public SendWeek(bool isSelected, string theText, string enText)
        {
            this._isSelected = isSelected;
            this._theText = theText;
            this._enText = enText;
        }

        public bool IsSelected {
            get { return _isSelected; }
            set {
                if (value == _isSelected)
                    return;
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        public string TheText {
            get { return _theText; }
        }

        public string EnText {
            get { return _enText; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TaskCron : INotifyPropertyChanged
    {
        private bool _useCustomCron = false;
        private string _customCron = "";
        private DateTime _taskTime = DateTime.Now;
        private List<SendWeek> _sendWeeksList = new List<SendWeek>(new SendWeek[]
            {
                new SendWeek(false, "周日", "SUN"),
                new SendWeek(false, "周一", "MON"),
                new SendWeek(false, "周二", "TUE"),
                new SendWeek(false, "周三", "WED"),
                new SendWeek(false, "周四", "THU"),
                new SendWeek(false, "周五", "FRI"),
                new SendWeek(false, "周六", "SAT"),
            });

        public TaskCron(DateTime taskTime)
        {
            this._taskTime = taskTime;
        }

        public TaskCron(string custom)
        {
            this._customCron = custom;
        }

        public DateTime TaskTime {
            get { return _taskTime; }
            set {
                if (value == _taskTime) return;
                _taskTime = value;
                OnPropertyChanged("TaskTime");
            }
        }

        public List<SendWeek> SendWeeksList {
            get { return _sendWeeksList; }
            set {
                if (value == _sendWeeksList) return;
                _sendWeeksList = value;
                OnPropertyChanged("SendWeeksList");
            }
        }

        public string CustomCron {
            get { return _customCron; }
            set {
                if (value == _customCron) return;
                _customCron = value;
                OnPropertyChanged("CustomCron");
            }
        }

        public bool UseCustomCron {
            get { return _useCustomCron; }
            set {
                if (value == _useCustomCron) return;
                _useCustomCron = value;
                OnPropertyChanged("UseCustomCron");
            }
        }

        public override string ToString()
        {
            if (_useCustomCron && !String.IsNullOrEmpty(_customCron))
            {
                return _customCron;
            }
            var str = new StringBuilder();
            var weeks = new StringBuilder();
            str.Append(_taskTime.ToString("ss mm HH"));
            for (int i = 0; i < 7 && i < _sendWeeksList.Count(); i++)
            {
                if (_sendWeeksList[i].IsSelected)
                {
                    if (weeks.Length > 0)
                    {
                        weeks.Append(",");
                    }
                    weeks.Append(_sendWeeksList[i].EnText);
                }
            }
            if (weeks.Length == 0)
            {
                str.Append(" * * ? *");
            } else
            {
                str.Append(String.Format(" ? * {0} *", weeks.ToString()));
            }
            return str.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
